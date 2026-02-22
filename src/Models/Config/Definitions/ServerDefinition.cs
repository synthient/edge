using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Models.Config.Definitions;

public class ServerDefinition : IConfigDefinition<ServerConfig>
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;

    public ServerConfig Build()
    {
        ConfigValidationException.ThrowIfNullOrEmpty("server.host", Host);
        ConfigValidationException.ThrowIfOutOfRange("server.port", Port, 1, 65535);

        return new ServerConfig
        {
            Host = Host,
            Port = Port
        };
    }
}