namespace Synthient.Edge.Utilities;

internal static class FilterPredicates
{
    public static readonly FilterFunc PassAll = static (_, _) => true;

    public static FilterFunc Or(FilterFunc left, FilterFunc right) =>
        (evt, mmdb) => left(evt, mmdb) || right(evt, mmdb);

    public static FilterFunc Fold(FilterFunc[] filters, Func<FilterFunc, FilterFunc, FilterFunc> combine)
    {
        var result = filters[0];
        for (var i = 1; i < filters.Length; i++)
            result = combine(result, filters[i]);
        return result;
    }

    public static FilterFunc Not(FilterFunc filter) =>
        (evt, mmdb) => !filter(evt, mmdb);

    public static FilterFunc AndChain(FilterFunc? combined, FilterFunc next) =>
        combined is null ? next : And(combined, next);

    private static FilterFunc And(FilterFunc left, FilterFunc right) 
        => (evt, mmdb) => left(evt, mmdb) && right(evt, mmdb);
}