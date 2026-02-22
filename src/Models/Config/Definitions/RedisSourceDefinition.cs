using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Models.Config.Definitions;

public class RedisSourceDefinition : IConfigDefinition<RedisSourceConfig>
{
    public string? Endpoint { get; set; }
    public string? Password { get; set; }
    public bool Ssl { get; set; } = false;
    public string Channel { get; set; } = "proxy_feed";

    public RedisSourceConfig Build()
    {
        ConfigValidationException.ThrowIfNullOrEmpty("source.endpoint", Endpoint);
        ConfigValidationException.ThrowIfNullOrEmpty("source.channel", Channel);

        return new RedisSourceConfig
        {
            Endpoint = Endpoint,
            Password = Password,
            Ssl = Ssl,
            Channel = Channel
        };
    }
}