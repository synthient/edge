using System.Net;
using Synthient.Edge.Models;

namespace Synthient.Edge.Services;

public interface IEventRepository
{
    Task InsertAsync(BucketedEvent bucketedEvt, CancellationToken cancellationToken);
    Task<BucketResult?> GetBucketAsync(IPAddress ip, string bucketName, CancellationToken cancellationToken);
    Task<IReadOnlyList<BucketResult>> GetAllBucketsAsync(IPAddress ip, CancellationToken cancellationToken);
}