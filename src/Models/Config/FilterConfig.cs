using System.Collections.Frozen;
using Synthient.Edge.Utilities;

namespace Synthient.Edge.Models.Config;

using MmdbData = Dictionary<string, object>;

public class FilterConfig(
    FrozenSet<string> providers,
    FrozenDictionary<string, FrozenSet<string>> mmdbFilters
)
{
    private const string MmdbPrefix = "mmdb.";
    private readonly Func<ProxyEvent, MmdbData?, bool> _predicate = Compile(providers, mmdbFilters);

    public bool Matches(ProxyEvent evt, MmdbData? mmdb) => _predicate(evt, mmdb);

    private static Func<ProxyEvent, MmdbData?, bool> Compile(
        FrozenSet<string>? providers,
        FrozenDictionary<string, FrozenSet<string>>? mmdbFilters)
    {
        Func<ProxyEvent, MmdbData?, bool>? combined = null;

        if (providers is { Count: > 0 })
        {
            combined = (evt, _) => providers.Contains(evt.Provider);
        }

        if (mmdbFilters is { Count: > 0 })
        {
            foreach (var (key, expectedValues) in mmdbFilters)
            {
                var segments = key[MmdbPrefix.Length..].Split('.');
                var lookup = BuildMmdbLookup(segments);
                var expected = expectedValues;

                Func<ProxyEvent, MmdbData?, bool> mmdbPredicate = (_, mmdb) =>
                    mmdb is not null
                    && lookup(mmdb) is { } actual
                    && MmdbValueMatches(actual, expected);

                combined = combined is null ? mmdbPredicate : FilterPredicates.And(combined, mmdbPredicate);
            }
        }

        return combined ?? FilterPredicates.Pass();
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