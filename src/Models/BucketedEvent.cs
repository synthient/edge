using System.Buffers;

namespace Synthient.Edge.Models;

public sealed record BucketedEvent(ProxyEvent Event, BucketMatch[] Buckets, int Matches)
{
    public void ReturnToPool() => ArrayPool<BucketMatch>.Shared.Return(Buckets);
    public static BucketMatch[] RentFromPool(int count) => ArrayPool<BucketMatch>.Shared.Rent(count);
}