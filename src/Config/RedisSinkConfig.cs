namespace Synthient.Edge.Config;

public sealed record RedisSinkConfig(string Endpoint, string? Password, bool Ssl, int Database);