using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Config.Definitions;

public sealed class RedisSinkDefinition : IConfigDefinition<RedisSinkConfig>
{
    public string? Endpoint { get; set; }
    public string? Password { get; set; }
    public bool Ssl { get; set; } = false;
    public int Database { get; set; } = 0;

    public RedisSinkConfig Build()
    {
        ConfigValidationException.ThrowIfNullOrEmpty("sink.endpoint", Endpoint);

        return new RedisSinkConfig(Endpoint, Password, Ssl, Database);
    }
}