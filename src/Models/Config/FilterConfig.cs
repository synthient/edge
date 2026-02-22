using System.Collections.Frozen;

namespace Synthient.Edge.Models.Config;

using MmdbData = Dictionary<string, object>;

public class FilterConfig
{
    private const string MmdbPrefix = "mmdb.";

    public required FrozenSet<string> Providers { get; init; }
    public required FrozenDictionary<string, FrozenSet<string>> MmdbFilters { get; init; }

    public Func<ProxyEvent, MmdbData?, bool> Compile()
    {
        Func<ProxyEvent, MmdbData?, bool>? combined = null;

        if (Providers is { Count: > 0 })
        {
            var providers = Providers;
            combined = (evt, _) => providers.Contains(evt.Provider);
        }

        if (MmdbFilters is { Count: > 0 })
        {
            foreach (var (key, expectedValues) in MmdbFilters)
            {
                var segments = key[MmdbPrefix.Length..].Split('.');
                var lookup = BuildMmdbLookup(segments);
                var expected = expectedValues;

                Func<ProxyEvent, MmdbData?, bool> mmdbPredicate = (_, mmdb) =>
                    mmdb is not null
                    && lookup(mmdb) is { } actual
                    && MmdbValueMatches(actual, expected);

                combined = combined is null
                    ? mmdbPredicate
                    : And(combined, mmdbPredicate);
            }
        }

        return combined ?? (static (_, _) => true);
    }

    private static Func<ProxyEvent, MmdbData?, bool> And(
        Func<ProxyEvent, MmdbData?, bool> left,
        Func<ProxyEvent, MmdbData?, bool> right) =>
        (evt, mmdb) => left(evt, mmdb) && right(evt, mmdb);

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