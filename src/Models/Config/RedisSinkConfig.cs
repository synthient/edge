namespace Synthient.Edge.Models.Config;

public class RedisSinkConfig
{
    public required string Endpoint { get; init; }
    public required string? Password { get; init; }
    public required bool Ssl { get; init; }
    public required int Database { get; init; }
}