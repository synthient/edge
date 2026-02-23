using Synthient.Edge.Models;

namespace Synthient.Edge.Utilities;

internal static class FilterPredicates
{
    public static readonly EventFilter PassAll = static (_, _) => true;

    public static EventFilter Or(EventFilter left, EventFilter right) =>
        (evt, mmdb) => left(evt, mmdb) || right(evt, mmdb);

    public static EventFilter Fold(EventFilter[] filters, Func<EventFilter, EventFilter, EventFilter> combine)
    {
        if (filters.Length == 0)
            throw new ArgumentException("At least one filter is required.", nameof(filters));

        var result = filters[0];
        for (var i = 1; i < filters.Length; i++)
            result = combine(result, filters[i]);
        return result;
    }

    public static EventFilter Not(EventFilter filter) =>
        (evt, mmdb) => !filter(evt, mmdb);

    public static EventFilter AndChain(EventFilter? combined, EventFilter next) =>
        combined is null ? next : And(combined, next);

    private static EventFilter And(EventFilter left, EventFilter right)
        => (evt, mmdb) => left(evt, mmdb) && right(evt, mmdb);
}