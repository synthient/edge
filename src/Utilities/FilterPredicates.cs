using Synthient.Edge.Models;

namespace Synthient.Edge.Utilities;

using MmdbData = Dictionary<string, object>;

internal static class FilterPredicates
{
    public static Func<ProxyEvent, MmdbData?, bool> And(
        Func<ProxyEvent, MmdbData?, bool> left,
        Func<ProxyEvent, MmdbData?, bool> right
    ) => (evt, mmdb) => left(evt, mmdb) && right(evt, mmdb);

    public static Func<ProxyEvent, MmdbData?, bool> Pass() => static (_, _) => true;
}