using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Synthient.Edge.Models.Config;

public class AppConfig
{
    public required ServerConfig Server { get; init; }
    public required List<string> ApiKeys { get; init; } = [];
    public required RedisSourceConfig Source { get; init; }
    public required RedisSinkConfig Sink { get; init; }
    public required MmDbConfig Mmdb { get; init; }
    public required FrozenDictionary<string, FilterConfig> Filters { get; init; }
    public required FrozenDictionary<string, BucketConfig> Buckets { get; init; }

    public bool TryMatchBuckets(
        ProxyEvent evt,
        MmdbData? mmdb,
        [NotNullWhen(true)] out (string Name, BucketConfig Bucket)[]? matched
    )
    {
        var buffer = new (string Name, BucketConfig Bucket)[Buckets.Count];
        var count = 0;

        foreach (var (name, bucket) in Buckets)
            if (bucket.Matches(evt, mmdb))
                buffer[count++] = (name, bucket);

        matched = count > 0 ? buffer[..count] : null;
        return count > 0;
    }
}