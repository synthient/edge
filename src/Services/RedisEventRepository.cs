using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using StackExchange.Redis;
using Synthient.Edge.Config;
using Synthient.Edge.Models;

namespace Synthient.Edge.Services;

public sealed class RedisEventRepository(
    AppConfig appConfig,
    IConnectionMultiplexer connection,
    IStringRegistry strings
) : IEventRepository
{
    private readonly IDatabase _database = connection.GetDatabase(appConfig.Sink.Database);

    private const string InsertScript = """
                                        local ip_key = KEYS[1]

                                        local provider_id = ARGV[1]
                                        local timestamp = tonumber(ARGV[2])

                                        local max_ttl = 0

                                        for i = 3, #ARGV, 2 do
                                            local bucket_id = tonumber(ARGV[i])
                                            local ttl_ms = tonumber(ARGV[i + 1])
                                            
                                            if ttl_ms > max_ttl then 
                                                max_ttl = ttl_ms 
                                            end
                                            
                                            local bucket_key = ip_key .. struct.pack('>i4', bucket_id)

                                            local current = redis.call('HGET', bucket_key, provider_id)
                                            local count = current and struct.unpack('>i4', current) + 1 or 1

                                            -- [count: 4B int32][timestamp: 8B int64]
                                            redis.call('HSET', bucket_key, provider_id, struct.pack('>i4i8', count, timestamp))
                                            redis.call('SADD', ip_key, bucket_id)

                                            -- Set TTL on first write (NX), extend if the new expiry is later (GT).
                                            if redis.call('PEXPIREAT', bucket_key, ttl_ms, 'NX') == 0 then
                                                redis.call('PEXPIREAT', bucket_key, ttl_ms, 'GT')
                                            end
                                        end

                                        if redis.call('PEXPIREAT', ip_key, max_ttl, 'NX') == 0 then
                                            redis.call('PEXPIREAT', ip_key, max_ttl, 'GT')
                                        end
                                        """;

    public async Task InsertAsync(BucketedEvent bucketedEvent, CancellationToken cancellationToken)
    {
        if (bucketedEvent.Matches == 0)
            return;

        var evt = bucketedEvent.Event;
        var eventTime = DateTimeOffset.FromUnixTimeSeconds(evt.Timestamp);
        var providerId = await strings.GetOrCreateIdAsync(evt.Provider, cancellationToken);

        var values = new RedisValue[2 + bucketedEvent.Matches * 2];
        values[0] = providerId;
        values[1] = evt.Timestamp;

        for (var i = 0; i < bucketedEvent.Matches; i++)
        {
            var match = bucketedEvent.Buckets[i];

            var ttlAt = eventTime + match.Bucket.Ttl;
            if (ttlAt <= DateTimeOffset.UtcNow)
                continue;

            var bucketId = await strings.GetOrCreateIdAsync(match.Name, cancellationToken);

            values[2 + i * 2] = bucketId;
            values[2 + i * 2 + 1] = ttlAt.ToUnixTimeMilliseconds();
        }

        var ipKey = GetIpKey(evt.IpAddress);

        await _database.ScriptEvaluateAsync(
            InsertScript,
            keys: [ipKey],
            values: values,
            flags: CommandFlags.FireAndForget
        );
    }

    public async Task<BucketResult?> GetBucketAsync(
        IPAddress ipAddress,
        string bucketName,
        CancellationToken cancellationToken
    )
    {
        var bucketId = await strings.GetOrCreateIdAsync(bucketName, cancellationToken);

        var (ttl, entries) = await FetchBucketAsync(ipAddress, bucketId);
        if (entries.Length == 0)
            return null;

        var providerEntries = await ResolveProvidersAsync(entries, cancellationToken);
        return new BucketResult(bucketName, ttl, providerEntries);
    }

    public async Task<IReadOnlyList<BucketResult>> GetAllBucketsAsync(IPAddress ip, CancellationToken cancellationToken)
    {
        var ipKey = GetIpKey(ip);

        var bucketIdMembers = await _database.SetMembersAsync(ipKey);
        if (bucketIdMembers.Length == 0)
            return [];

        var bucketIds = bucketIdMembers.Select(m => (int)m).ToArray();
        var (hashes, ttls) = await FetchAllBucketsAsync(ip, bucketIds);

        return await ResolveBucketResultsAsync(bucketIds, hashes, ttls, cancellationToken);
    }

    private async Task<(HashEntry[][] Hashes, TimeSpan?[] Ttls)> FetchAllBucketsAsync(
        IPAddress ipAddress,
        int[] bucketIds
    )
    {
        var ipBucketKeys = bucketIds.Select(id => GetIpBucketKey(ipAddress, id)).ToArray();

        var hashTasks = ipBucketKeys.Select(k => _database.HashGetAllAsync(k)).ToArray();
        var ttlTasks = ipBucketKeys.Select(k => _database.KeyTimeToLiveAsync(k)).ToArray();

        return (await Task.WhenAll(hashTasks), await Task.WhenAll(ttlTasks));
    }

    private async Task<(TimeSpan? Ttl, HashEntry[] Entries)> FetchBucketAsync(IPAddress ipAddress, int bucketId)
    {
        var ipBucketKey = GetIpBucketKey(ipAddress, bucketId);

        var ttlTask = _database.KeyTimeToLiveAsync(ipBucketKey);
        var entriesTask = _database.HashGetAllAsync(ipBucketKey);
        await Task.WhenAll(ttlTask, entriesTask);

        return (ttlTask.Result, entriesTask.Result);
    }

    private async Task<IReadOnlyList<BucketResult>> ResolveBucketResultsAsync(
        int[] bucketIds,
        HashEntry[][] hashes,
        TimeSpan?[] ttls,
        CancellationToken cancellationToken)
    {
        var results = new List<BucketResult>(bucketIds.Length);

        for (var i = 0; i < bucketIds.Length; i++)
        {
            if (hashes[i].Length == 0)
                continue;

            var bucketName = await strings.GetStringAsync(bucketIds[i], cancellationToken);
            var providers = await ResolveProvidersAsync(hashes[i], cancellationToken);

            results.Add(new BucketResult(bucketName, ttls[i], providers));
        }

        return results;
    }

    private async ValueTask<IReadOnlyList<ProviderActivity>> ResolveProvidersAsync(
        HashEntry[] entries,
        CancellationToken cancellationToken)
    {
        var result = new List<ProviderActivity>(entries.Length);

        foreach (var entry in entries)
        {
            var provider = await ParseProviderAsync(entry, cancellationToken);
            result.Add(provider);
        }

        return result;
    }

    private async ValueTask<ProviderActivity> ParseProviderAsync(HashEntry entry, CancellationToken cancellationToken)
    {
        if (!entry.Name.TryParse(out int providerId))
            throw new InvalidDataException("Unexpected Redis hash key type.");

        if (entry.Value.IsNullOrEmpty)
            throw new InvalidDataException("Redis hash value is null or empty.");

        var bytes = (byte[])entry.Value!;

        const int payloadSize = sizeof(int) + sizeof(long);

        if (bytes.Length != payloadSize)
            throw new InvalidDataException($"Expected {payloadSize} bytes, got {bytes.Length}.");

        var count = BinaryPrimitives.ReadInt32BigEndian(bytes);
        var lastSeen = DateTimeOffset
            .FromUnixTimeSeconds(BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(sizeof(int))))
            .UtcDateTime;

        var providerName = await strings.GetStringAsync(providerId, cancellationToken);
        return new ProviderActivity(providerName, count, lastSeen);
    }

    private static RedisKey GetIpKey(IPAddress ip)
    {
        var size = ip.AddressFamily == AddressFamily.InterNetwork ? 4 : 16;
        var key = new byte[size];

        ip.TryWriteBytes(key, out _);

        return key;
    }

    private static RedisKey GetIpBucketKey(IPAddress ip, int bucketId)
    {
        var ipSize = ip.AddressFamily == AddressFamily.InterNetwork ? 4 : 16;
        var key = new byte[ipSize + sizeof(int)];

        ip.TryWriteBytes(key, out _);
        BinaryPrimitives.WriteInt32BigEndian(key.AsSpan(ipSize), bucketId);

        return key;
    }
}