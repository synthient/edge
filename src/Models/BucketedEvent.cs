using Synthient.Edge.Config;

namespace Synthient.Edge.Models;

public sealed record BucketedEvent(ProxyEvent Event, (string BucketName, BucketConfig Bucket)[] Buckets);