using System.Collections.Frozen;
using Synthient.Edge.Models;
using Synthient.Edge.Utilities;

namespace Synthient.Edge.Config;

public sealed class FilterConfig(
    FrozenSet<string> providers,
    FrozenDictionary<string, FrozenSet<string>> mmdbFilters
)
{
    private const string MmdbPrefix = "mmdb.";
    private readonly EventFilter _predicate = Compile(providers, mmdbFilters);

    public bool RequiresMmdb { get; } = mmdbFilters.Count > 0;

    public bool Matches(ProxyEvent evt, MmdbData? mmdb) => _predicate(evt, mmdb);

    private static EventFilter Compile(
        FrozenSet<string>? providers,
        FrozenDictionary<string, FrozenSet<string>>? mmdbFilters
    )
    {
        EventFilter? combined = null;

        if (providers is { Count: > 0 })
            combined = FilterPredicates.AndChain(combined, (evt, _) => providers.Contains(evt.Provider));


        if (mmdbFilters is { Count: > 0 })
        {
            foreach (var (key, expectedValues) in mmdbFilters)
            {
                var segments = key[MmdbPrefix.Length..].Split('.');
                var lookup = BuildMmdbLookup(segments);
                var expected = expectedValues;

                combined = FilterPredicates.AndChain(combined, (_, mmdb) =>
                    mmdb is not null
                    && lookup(mmdb) is { } actual
                    && MmdbValueMatches(actual, expected));
            }
        }

        return combined ?? FilterPredicates.PassAll;
    }

    private static bool MmdbValueMatches(object actual, FrozenSet<string> expected) =>
        actual is string s
            ? expected.Contains(s)
            : Convert.ToString(actual) is { } converted && expected.Contains(converted);

    private static Func<MmdbData, object?> BuildMmdbLookup(string[] segments)
    {
        return dict =>
        {
            object? current = dict;

            foreach (var segment in segments)
            {
                if (current is not MmdbData node || !node.TryGetValue(segment, out current))
                    return null;
            }

            return current;
        };
    }
}