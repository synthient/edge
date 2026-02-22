namespace Synthient.Edge.Models;

public sealed record ProviderActivity(string Provider, int Count, DateTime LastSeen);