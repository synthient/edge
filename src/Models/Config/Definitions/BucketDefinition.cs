using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Models.Config.Definitions;

public class BucketDefinition
{
    public string? Ttl { get; set; }
    public string[]? All { get; set; }
    public string[]? Any { get; set; }
    public string[]? Not { get; set; }

    public BucketConfig Build(string bucketName, IDictionary<string, FilterConfig> filters)
    {
        var bucketKey = $"buckets.{bucketName}";

        ConfigValidationException.ThrowIfNullOrEmpty(bucketKey, Ttl);

        if (!TimeSpan.TryParse(Ttl, out var ttl))
            throw new ConfigValidationException(
                $"{bucketKey}.ttl", $"TTL value '{Ttl}' is not a valid time format. Expected format: hh:mm:ss."
            );
        
        var (allFilters, allMmdb) = ResolveFilters(All, bucketKey, filters);
        var (anyFilters, anyMmdb) = ResolveFilters(Any, bucketKey, filters);
        var (notFilters, notMmdb) = ResolveFilters(Not, bucketKey, filters);

        return new BucketConfig(
            ttl,
            allFilters: allFilters,
            anyFilters: anyFilters,
            notFilters: notFilters,
            requiresMmdb: allMmdb || anyMmdb || notMmdb
        );
    }

    private static (FilterFunc[] filters, bool requiresMmdb) ResolveFilters(
        string[]? names,
        string bucketKey,
        IDictionary<string, FilterConfig> filters
    )
    {
        if (names is not { Length: > 0 })
            return ([], false);

        var resolved = new FilterFunc[names.Length];
        var requiresMmdb = false;

        for (var i = 0; i < names.Length; i++)
        {
            if (!filters.TryGetValue(names[i], out var filter))
                throw new ConfigValidationException(bucketKey, $"Undefined filter '{names[i]}' referenced in bucket.");

            resolved[i] = filter.Matches;
            requiresMmdb |= filter.RequiresMmdb;
        }

        return (resolved, requiresMmdb);
    }
}