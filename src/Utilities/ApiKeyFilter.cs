using System.Collections.Frozen;
using Microsoft.Extensions.Primitives;
using Synthient.Edge.Config;

namespace Synthient.Edge.Utilities;

/// <summary>
/// Endpoint filter that enforces API key authentication using API keys defined in config.
/// </summary>
public sealed class ApiKeyFilter(AppConfig config) : IEndpointFilter
{
    private const string ApiKeyHeader = "x-api-key";
    private readonly FrozenSet<string> _apiKeys = config.ApiKeys.ToFrozenSet(StringComparer.Ordinal);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_apiKeys.Count == 0)
            return await next(context); // No API keys configured.

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var headerValues)
            || StringValues.IsNullOrEmpty(headerValues))
        {
            return Results.Unauthorized(); // No API key header set.
        }

        var providedApiKey = headerValues.FirstOrDefault()?.Trim();

        if (string.IsNullOrEmpty(providedApiKey) || !_apiKeys.Contains(providedApiKey))
            return Results.Unauthorized(); // Invalid API key provided.

        return await next(context);
    }
}