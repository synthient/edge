using System.Collections.Frozen;

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
    public TimeSpan MaxBucketTtl { get; } = Buckets.Values.Max(bucket => bucket.Ttl);
}