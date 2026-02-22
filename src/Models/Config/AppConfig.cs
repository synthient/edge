namespace Synthient.Edge.Models.Config;

public class AppConfig
{
    public required ServerConfig Server { get; init; }
    public required List<string> ApiKeys { get; init; } = [];
    public required RedisSourceConfig Source { get; init; }
    public required RedisSinkConfig Sink { get; init; }
    public required MmDbConfig Mmdb { get; init; }
    public required Dictionary<string, FilterConfig> Filters { get; init; } = [];
}