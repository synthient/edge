using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Models.Config.Definitions;

public class RedisSinkDefinition : IConfigDefinition<RedisSinkConfig>
{
    public string? Endpoint { get; set; }
    public string? Password { get; set; }
    public bool Ssl { get; set; } = false;
    public int Database { get; set; } = 0;

    public RedisSinkConfig Build()
    {
        ConfigValidationException.ThrowIfNullOrEmpty("sink.endpoint", Endpoint);

        return new RedisSinkConfig
        {
            Endpoint = Endpoint,
            Password = Password,
            Ssl = Ssl,
            Database = Database
        };
    }
}