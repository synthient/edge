namespace Synthient.Edge.Models;

public sealed record HealthResponse(string Status, string Version, string? Commit);