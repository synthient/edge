namespace Synthient.Edge.Models.Config;

public sealed record RedisSourceConfig(string Endpoint, string? Password, bool Ssl, string Channel);