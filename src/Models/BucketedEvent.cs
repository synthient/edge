using Synthient.Edge.Models.Config;

namespace Synthient.Edge.Models;

public sealed record BucketedEvent(ProxyEvent Event, (string BucketName, BucketConfig Bucket)[] Buckets);