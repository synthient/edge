namespace Synthient.Edge.Models;

public class ContextIpResponse
{
    public required string Ip { get; init; }
    public required IReadOnlyList<BucketResult> Enriched { get; init; }
}