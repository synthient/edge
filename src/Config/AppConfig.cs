using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Synthient.Edge.Models;

namespace Synthient.Edge.Config;

public sealed record AppConfig(
    ServerConfig Server,
    IReadOnlyList<string> ApiKeys,
    RedisSourceConfig Source,
    RedisSinkConfig Sink,
    MmdbConfig Mmdb,
    FrozenDictionary<string, BucketConfig> Buckets
)
{
    public bool FiltersRequireMmdb { get; } = Buckets.Values.Any(bucket => bucket.FiltersRequireMmdb);

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