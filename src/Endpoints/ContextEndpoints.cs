using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Synthient.Edge.Models.Config;
using Synthient.Edge.Utilities;

namespace Synthient.Edge.Endpoints;

public static class ContextEndpoints
{
    public static RouteGroupBuilder MapContextEndpoints(this WebApplication app, AppConfig appConfig)
    {
        var context = app.MapGroup("/context");

        // Enable API key filter if keys are configured.
        if (appConfig.ApiKeys.Count > 0)
            context.AddEndpointFilter<ApiKeyFilter>();

        context.MapGet("/{ip}", LookupIpAsync);

        return context;
    }

    private static Results<Ok, NotFound, BadRequest> LookupIpAsync(
        [FromRoute] string ip,
        CancellationToken cancellationToken
    )
    {
        if (!IPAddress.TryParse(ip, out var ipAddress))
            return TypedResults.BadRequest();

        return TypedResults.Ok();
    }
}