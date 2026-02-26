using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Synthient.Edge.Config;
using Synthient.Edge.Models;
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
        group.MapGet("/{ip}/buckets/{bucket}", LookupIpAndBucketAsync);

        return group;
    }

    private static async Task<IResult> LookupIpAsync(
        [FromRoute] string ip,
        [FromServices] IEventRepository repo,
        [FromServices] IMmdbReader mmdbReader,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseIp(ip, out var ipAddress, out var error))
            return error;

        var bucketResults = await repo.GetAllBucketsAsync(ipAddress, cancellationToken);
        if (bucketResults.Count == 0)
            return Error(HttpStatusCode.NotFound, "IP not found in any bucket");

        var (network, location) = mmdbReader.LookupNetworkAndLocation(ipAddress);

        var response = new ContextIpResponse
        {
            Ip = ip,
            Network = network,
            Location = location,
            Enriched = bucketResults
        };

        return TypedResults.Ok(response);
    }

    private static async Task<IResult> LookupIpAndBucketAsync(
        [FromRoute] string ip,
        [FromRoute] string bucket,
        [FromServices] IEventRepository repo,
        CancellationToken cancellationToken
    )
    {
        if (!TryParseIp(ip, out var ipAddress, out var error))
            return error;

        var bucketResult = await repo.GetBucketAsync(ipAddress, bucket, cancellationToken);
        if (bucketResult is null)
            return Error(HttpStatusCode.NotFound, "IP not found in bucket");

        var response = new ContextIpBucketResponse
        {
            Ip = ip,
            Bucket = bucket,
            Ttl = bucketResult.Ttl,
            Enriched = bucketResult.Providers
        };

        return TypedResults.Ok(response);
    }

    private static bool TryParseIp(
        string ip,
        [NotNullWhen(true)] out IPAddress? address,
        [NotNullWhen(false)] out IResult? error
    )
    {
        if (!IPAddress.TryParse(ip, out address))
        {
            error = Error(HttpStatusCode.BadRequest, "Invalid IP address, expected IPv4 or IPv6 format");
            return false;
        }

        error = null;
        return true;
    }

    private static JsonHttpResult<MessageResponse> Error(HttpStatusCode statusCode, string message)
    {
        return TypedResults.Json(new MessageResponse(message), statusCode: (int)statusCode);
    }
}