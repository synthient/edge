namespace Synthient.Edge.Models;

public sealed class ContextIpBucketResponse
{
    public required string Ip { get; init; }
    public required string Bucket { get; init; }
    public required TimeSpan? Ttl { get; init; }
    public required IReadOnlyList<ProviderActivity> Enriched { get; init; }
}