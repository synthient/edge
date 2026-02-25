using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Synthient.Edge.Config;
using Synthient.Edge.Models;
using Synthient.Edge.Utilities;

namespace Synthient.Edge.Endpoints;

public static class HealthEndpoint
{
    private static readonly (string Version, string? Commit) Build = GetBuildMetadata();

    public static RouteGroupBuilder MapHealthEndpoint(this IEndpointRouteBuilder app, AppConfig config)
    {
        var group = app.MapGroup("/health");

        // Add API key filter if keys are configured.
        if (config.ApiKeys.Count != 0)
            group.AddEndpointFilter<ApiKeyFilter>();

        group.MapGet("/", GetHealthStatus);

        return group;
    }

    private static Ok<HealthResponse> GetHealthStatus()
    {
        var response = new HealthResponse(
            Status: "ok",
            Version: Build.Version,
            Commit: Build.Commit
        );

        return TypedResults.Ok(response);
    }

    private static (string Version, string? Commit) GetBuildMetadata()
    {
        var attribute = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (attribute?.InformationalVersion is not { } info)
        {
            return ("unknown", "unknown");
        }

        if (!info.Contains('+'))
        {
            // No commit hash present
            return (info, null);
        }

        var parts = info.Split('+', 2);
        var version = parts[0];
        var commit = parts[1].Length > 7 ? parts[1][..7] : parts[1];

        return (version, commit);
    }
}