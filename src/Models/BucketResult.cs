namespace Synthient.Edge.Models;

public sealed record BucketResult(string Bucket, TimeSpan? Ttl, IReadOnlyList<ProviderActivity> Providers);