using Synthient.Edge.Models;
using Synthient.Edge.Utilities;

namespace Synthient.Edge.Config;

public sealed class BucketConfig(
    TimeSpan ttl,
    bool filtersRequireMmdb,
    EventFilter[] allFilters,
    EventFilter[] anyFilters,
    EventFilter[] notFilters
)
{
    private readonly EventFilter _predicate = Compile(allFilters, anyFilters, notFilters);

    public TimeSpan Ttl { get; } = ttl;
    public bool FiltersRequireMmdb { get; } = filtersRequireMmdb;

    public bool Matches(ProxyEvent evt, MmdbData? mmdb) => _predicate(evt, mmdb);

    private static EventFilter Compile(EventFilter[] all, EventFilter[] any, EventFilter[] not)
    {
        EventFilter? combined = null;

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