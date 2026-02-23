using System.Collections.Frozen;
using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Models.Config.Definitions;

// ReSharper disable CollectionNeverUpdated.Global
public sealed class AppConfigDefinition : IConfigDefinition<AppConfig>
{
    public ServerDefinition Server { get; set; } = new();
    public List<string> ApiKeys { get; set; } = [];
    public RedisSourceDefinition? Source { get; set; }
    public RedisSinkDefinition? Sink { get; set; }
    public MmdbDefinition? Mmdb { get; set; }
    public Dictionary<string, FilterDefinition> Filters { get; set; } = [];
    public Dictionary<string, BucketDefinition> Buckets { get; set; } = [];

    public AppConfig Build()
    {
        ConfigValidationException.ThrowIfNull("source", Source);
        ConfigValidationException.ThrowIfNull("sink", Sink);
        ConfigValidationException.ThrowIfNull("mmdb", Mmdb);

        if (Buckets.Count == 0)
            throw new ConfigValidationException("buckets", "At least one bucket must be defined.");

        var filters = Filters.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.Build(kv.Key));

        var buckets = Buckets.ToFrozenDictionary(
            kv => kv.Key,
            kv => kv.Value.Build(kv.Key, filters)
        );

        return new AppConfig
        (
            Server: Server.Build(),
            ApiKeys: ApiKeys,
            Source: Source.Build(),
            Sink: Sink.Build(),
            Mmdb: Mmdb.Build(),
            Buckets: buckets
        );
    }
}