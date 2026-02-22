using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Models.Config.Definitions;

public class AppConfigDefinition : IConfigDefinition<AppConfig>
{
    public ServerDefinition Server { get; set; } = new();
    public List<string> ApiKeys { get; set; } = [];
    public RedisSourceDefinition? Source { get; set; }
    public RedisSinkDefinition? Sink { get; set; }

    public AppConfig Build()
    {
        ConfigValidationException.ThrowIfNull("source", Source);
        ConfigValidationException.ThrowIfNull("sink", Sink);

        return new AppConfig
        {
            Server = Server.Build(),
            ApiKeys = ApiKeys,
            Source = Source.Build(),
            Sink = Sink.Build()
        };
    }
}