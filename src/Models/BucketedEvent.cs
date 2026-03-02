namespace Synthient.Edge.Models;

public sealed record BucketedEvent(ProxyEvent Event, BucketMatch[] Matches, int MatchCount);