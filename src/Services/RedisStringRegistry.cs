using System.Collections.Concurrent;
using AsyncKeyedLock;
using StackExchange.Redis;
using Synthient.Edge.Models.Config;

namespace Synthient.Edge.Services;

/// <summary>
/// Maaps strings to integer IDs, backed by Redis. Its purpose is to reduce bandwidth and storage by replacing repeated low-cardinality strings with (smaller) integers.
/// </summary>
public partial class RedisStringRegistry(
    IConnectionMultiplexer connection,
    AppConfig appConfig,
    ILogger<RedisStringRegistry> logger
) : IStringRegistry
{
    private readonly IDatabase _database = connection.GetDatabase(appConfig.Sink.Database);

    private readonly ConcurrentDictionary<string, int> _toId = new();
    private readonly ConcurrentDictionary<int, string> _fromId = new();

    private static readonly AsyncKeyedLockOptions LockOptions = new(poolInitialFill: 1, poolSize: 20);

    private readonly AsyncKeyedLocker<string> _writeLock = new(LockOptions);
    private readonly AsyncKeyedLocker<int> _readLock = new(LockOptions);

    private const string MappingKey = "{strings}:ids";
    private const string CounterKey = "{strings}:counter";

    private static readonly LuaScript GetOrCreateScript = LuaScript.Prepare("""
                                                                            local score = redis.call('ZSCORE', @mappingKey, @value)

                                                                            if score then 
                                                                                return tonumber(score) 
                                                                            end

                                                                            local nextId = redis.call('INCR', @counterKey)
                                                                            redis.call('ZADD', @mappingKey, nextId, @value)

                                                                            return nextId
                                                                            """);


    public async ValueTask<int> GetOrCreateIdAsync(string value, CancellationToken cancellationToken)
    {
        if (_toId.TryGetValue(value, out var cachedId))
            return cachedId;

        using (await _writeLock.LockAsync(value, cancellationToken))
        {
            if (_toId.TryGetValue(value, out cachedId))
                return cachedId;

            var result = await _database.ScriptEvaluateAsync(GetOrCreateScript,
                new { mappingKey = MappingKey, counterKey = CounterKey, value }
            );

            var id = (int)result;
            _toId[value] = id;
            _fromId[id] = value;
            return id;
        }
    }

    public async ValueTask<string> GetStringAsync(int id, CancellationToken cancellationToken)
    {
        if (_fromId.TryGetValue(id, out var cachedString))
            return cachedString;

        using (await _readLock.LockAsync(id, cancellationToken))
        {
            if (_fromId.TryGetValue(id, out cachedString)) return cachedString;

            var results = await _database.SortedSetRangeByScoreAsync(MappingKey, id, id);
            if (results.Length == 0)
                throw new KeyNotFoundException($"String ID {id} not found.");

            var value = (string)results[0]!;
            _toId[value] = id;
            _fromId[id] = value;
            return value;
        }
    }
    
    public async Task WarmAsync()
    {
        var strings = await _database.SortedSetRangeByRankWithScoresAsync(MappingKey);

        foreach (var entry in strings)
        {
            var value = (string)entry.Element!;
            var id = (int)entry.Score;

            _toId[value] = id;
            _fromId[id] = value;
        }

        LogWarmed(logger, strings.Length);
    }

    [LoggerMessage(LogLevel.Information, "String registry cache warmed with {count} entries")]
    private static partial void LogWarmed(ILogger<RedisStringRegistry> logger, int count);
}