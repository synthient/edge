using System.Net;
using Synthient.Edge.Models;

namespace Synthient.Edge.Services;

public interface IEventRepository
{
    ValueTask InsertAsync(BucketedEvent bucketedEvent, CancellationToken cancellationToken);
    Task<BucketResult?> GetBucketAsync(IPAddress ipAddress, string bucketName, CancellationToken cancellationToken);
    Task<IReadOnlyList<BucketResult>> GetAllBucketsAsync(IPAddress ip, CancellationToken cancellationToken);
}