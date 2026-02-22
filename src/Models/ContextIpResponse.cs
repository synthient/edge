namespace Synthient.Edge.Models;

public sealed class ContextIpResponse
{
    public required string Ip { get; init; }
    public required MmdbNetwork Network { get; init; }
    public required MmdbLocation Location { get; init; }
    public required IReadOnlyList<BucketResult> Enriched { get; init; }
}