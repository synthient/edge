using System.Net;
using Microsoft.AspNetCore.Mvc;
using Synthient.Edge.Models;
using Synthient.Edge.Models.Config;
using Synthient.Edge.Services;
using Synthient.Edge.Utilities;

namespace Synthient.Edge.Endpoints;

public static class ContextEndpoints
{
    public static RouteGroupBuilder MapContextEndpoints(this WebApplication app, AppConfig appConfig)
    {
        var group = app.MapGroup("/context");

        // Add API key filter if keys are configured.
        if (appConfig.ApiKeys.Count > 0)
            group.AddEndpointFilter<ApiKeyFilter>();

        group.MapGet("/{ip}", LookupIpAsync);
        group.MapGet("/{ip}/buckets/{bucket}", LookupBucketAsync);

        return group;
    }

    private static async Task<IResult> LookupIpAsync(
        [FromRoute] string ip,
        [FromServices] IEventRepository repo,
        [FromServices] IMmdbReader fileMmdbReader,
        CancellationToken cancellationToken
    )
    {
        if (!IPAddress.TryParse(ip, out var ipAddress))
            return TypedResults.BadRequest($"'{ip}' is not a valid IP address.");

        var bucketResults = await repo.GetAllBucketsAsync(ipAddress, cancellationToken);
        if (bucketResults.Count == 0)
            return TypedResults.NotFound();

        var (network, location) = fileMmdbReader.LookupNetworkAndLocation(ipAddress);

        var response = new ContextIpResponse
        {
            Ip = ip,
            Network = network,
            Location = location,
            Enriched = bucketResults
        };

        return TypedResults.Ok(response);
    }

    private static async Task<IResult> LookupBucketAsync(
        [FromRoute] string ip,
        [FromRoute] string bucket,
        [FromServices] IEventRepository repo,
        CancellationToken cancellationToken
    )
    {
        if (!IPAddress.TryParse(ip, out var ipAddress))
            return TypedResults.BadRequest($"'{ip}' is not a valid IP address.");

        var bucketResult = await repo.GetBucketAsync(ipAddress, bucket, cancellationToken);
        if (bucketResult is null)
            return TypedResults.NotFound();

        var response = new ContextIpBucketResponse
        {
            Ip = ip,
            Bucket = bucket,
            Ttl = bucketResult.Ttl,
            Enriched = bucketResult.Providers
        };

        return TypedResults.Ok(response);
    }
}