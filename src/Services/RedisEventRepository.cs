using System.Buffers.Binary;
using System.Net;
using StackExchange.Redis;
using Synthient.Edge.Models;
using Synthient.Edge.Models.Config;
using Synthient.Edge.Utilities;

namespace Synthient.Edge.Services;

public sealed partial class RedisEventRepository(
    AppConfig appConfig,
    IConnectionMultiplexer connection,
    IStringRegistry strings,
    ILogger<RedisEventRepository> logger
) : IEventRepository
{
    private readonly IDatabase _db = connection.GetDatabase(appConfig.Sink.Database);

    private static readonly LuaScript InsertScript = LuaScript.Prepare("""
                                                                       local current = redis.call('HGET', @bucketKey, @providerId)
                                                                       local count   = 1

                                                                       if current then
                                                                           -- Read count from first 4 bytes: big-endian int32
                                                                           count = struct.unpack('>i4', current) + 1
                                                                       end

                                                                       -- [count: 4B int32][timestamp: 8B int64]
                                                                       redis.call('HSET', @bucketKey, @providerId, struct.pack('>i4l', count, @timestamp))

                                                                       -- [timestamp: 8B int64]
                                                                       redis.call('HSET', @ipKey, @bucketId, struct.pack('>l', @timestamp))

                                                                       -- Set TTL on first write (NX), extend if the new expiry is later (GT)
                                                                       if redis.call('PEXPIREAT', @bucketKey, @ttlMs, 'NX') == 0 then
                                                                           redis.call('PEXPIREAT', @bucketKey, @ttlMs, 'GT')
                                                                       end

                                                                       if redis.call('PEXPIREAT', @ipKey, @ttlMs, 'NX') == 0 then
                                                                           redis.call('PEXPIREAT', @ipKey, @ttlMs, 'GT')
                                                                       end
                                                                       """);

    public async Task InsertAsync(BucketedEvent bucketedEvt, CancellationToken cancellationToken)
    {
        var eventTime = DateTimeOffset.FromUnixTimeSeconds(bucketedEvt.Event.Timestamp);
        var providerId = await strings.GetOrCreateIdAsync(bucketedEvt.Event.Provider, cancellationToken);

        foreach (var (bucketName, bucket) in bucketedEvt.Buckets)
        {
            var ttlAt = eventTime + bucket.Ttl;
            if (ttlAt <= DateTimeOffset.UtcNow)
            {
                LogExpiredEvent(logger);
                continue;
            }

            var bucketId = await strings.GetOrCreateIdAsync(bucketName, cancellationToken);

            // TODO: Writes IP to bytes twice.
            var ipKey = RedisKeyBuilder.IpKey(bucketedEvt.Event.IpAddress);
            var bucketKey = RedisKeyBuilder.BucketKey(bucketedEvt.Event.IpAddress, bucketId);

            _ = _db.ScriptEvaluateAsync(
                InsertScript,
                new
                {
                    bucketKey = bucketKey,
                    ipKey = ipKey,
                    providerId = (RedisValue)providerId,
                    bucketId = (RedisValue)bucketId,
                    ttlMs = ttlAt.ToUnixTimeMilliseconds(),
                    timestamp = bucketedEvt.Event.Timestamp
                },
                flags: CommandFlags.FireAndForget
            );
        }
    }

    public async Task<BucketResult?> GetBucketAsync(
        IPAddress ip,
        string bucketName,
        CancellationToken cancellationToken
    )
    {
        var bucketId = await strings.GetOrCreateIdAsync(bucketName, cancellationToken);
        var key = RedisKeyBuilder.BucketKey(ip, bucketId);

        var (ttl, entries) = await FetchBucketAsync(key);
        if (entries.Length == 0)
            return null;

        var providersActivity = await ResolveProvidersAsync(entries, cancellationToken);

        return new BucketResult(bucketName, ttl, providersActivity);
    }

    public async Task<IReadOnlyList<BucketResult>> GetAllBucketsAsync(IPAddress ip, CancellationToken cancellationToken)
    {
        var ipKey = RedisKeyBuilder.IpKey(ip);
        var bucketFields = await _db.HashGetAllAsync(ipKey);
        if (bucketFields.Length == 0)
            return [];

        var bucketIds = bucketFields.Select(e => (int)e.Name).ToArray();
        var bucketKeys = bucketIds.Select(bucketId => RedisKeyBuilder.BucketKey(ip, bucketId)).ToArray();

        // Pipeline all HGETALL + TTL lookups
        var batch = _db.CreateBatch();
        var hashTasks = bucketKeys.Select(k => batch.HashGetAllAsync(k)).ToArray();
        var ttlTasks = bucketKeys.Select(k => batch.KeyTimeToLiveAsync(k)).ToArray();
        batch.Execute();

        await Task.WhenAll(hashTasks);
        await Task.WhenAll(ttlTasks);

        var bucketNames =
            await Task.WhenAll(bucketIds.Select(id => strings.GetStringAsync(id, cancellationToken).AsTask()));

        var results = new List<BucketResult>(bucketIds.Length);

        for (var i = 0; i < bucketIds.Length; i++)
        {
            if (hashTasks[i].Result.Length == 0)
                continue;

            results.Add(new BucketResult(
                Bucket: bucketNames[i],
                Ttl: ttlTasks[i].Result,
                Providers: await ResolveProvidersAsync(hashTasks[i].Result, cancellationToken)
            ));
        }

        return results;
    }

    private async Task<(TimeSpan? Ttl, HashEntry[] Entries)> FetchBucketAsync(RedisKey key)
    {
        var ttlTask = _db.KeyTimeToLiveAsync(key);
        var entriesTask = _db.HashGetAllAsync(key);
        await Task.WhenAll(ttlTask, entriesTask);
        return (ttlTask.Result, entriesTask.Result);
    }

    private async ValueTask<IReadOnlyList<ProviderActivity>> ResolveProvidersAsync(
        HashEntry[] entries,
        CancellationToken cancellationToken
    )
    {
        var result = new List<ProviderActivity>(entries.Length);

        foreach (var entry in entries)
        {
            var name = await strings.GetStringAsync((int)entry.Name, cancellationToken);
            var bytes = (byte[])entry.Value!;

            var count = BinaryPrimitives.ReadInt32BigEndian(bytes);
            var ts = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(sizeof(int)));
            var lastSeen = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;

            var providerActivity = new ProviderActivity(name, count, lastSeen);
            result.Add(providerActivity);
        }

        return result;
    }

    [LoggerMessage(LogLevel.Debug, "Skipping inserting expired event.")]
    private static partial void LogExpiredEvent(ILogger logger);
}