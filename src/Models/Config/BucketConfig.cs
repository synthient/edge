using Synthient.Edge.Utilities;

namespace Synthient.Edge.Models.Config;

public sealed class BucketConfig(
    TimeSpan ttl,
    bool requiresMmdb,
    FilterFunc[] allFilters,
    FilterFunc[] anyFilters,
    FilterFunc[] notFilters
)
{
    private readonly FilterFunc _predicate = Compile(allFilters, anyFilters, notFilters);

    public TimeSpan Ttl { get; } = ttl;
    public bool RequiresMmdb { get; } = requiresMmdb;

    public bool Matches(ProxyEvent evt, MmdbData? mmdb) => _predicate(evt, mmdb);

    private static FilterFunc Compile(FilterFunc[] all, FilterFunc[] any, FilterFunc[] not)
    {
        FilterFunc? combined = null;

        // Reject if any NOT filter matches
        foreach (var f in not)
            combined = FilterPredicates.AndChain(combined, FilterPredicates.Not(f));

        // Reject if any ALL filter fails
        foreach (var f in all)
            combined = FilterPredicates.AndChain(combined, f);

        // Reject if all ANY filters fail
        if (any.Length > 0)
            combined = FilterPredicates.AndChain(combined, FilterPredicates.Fold(any, FilterPredicates.Or));

        return combined ?? FilterPredicates.PassAll;
    }
}