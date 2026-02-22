using System.Collections.Frozen;
using Synthient.Edge.Exceptions;
using YamlDotNet.Serialization;

namespace Synthient.Edge.Models.Config.Definitions;

public class FilterDefinition : IConfigDefinition<FilterConfig>
{
    private const string MmdbPrefix = "mmdb.";

    /// <summary>
    /// Arbitrary MMDB path filters injected after deserialization (e.g. "mmdb.country.iso_code").
    /// Keys must start with <c>mmdb.</c> and contain no spaces.
    /// </summary>
    [YamlIgnore]
    public Dictionary<string, List<string>> MmdbFilters { get; set; } = [];

    public List<string> Provider { get; set; } = [];

    public FilterConfig Build()
    {
        foreach (var key in MmdbFilters.Keys)
            ValidateMmdbKey(key);

        return new FilterConfig
        {
            Providers = Provider.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            MmdbFilters = MmdbFilters.ToFrozenDictionary(
                kv => kv.Key,
                kv => kv.Value.ToFrozenSet(StringComparer.OrdinalIgnoreCase)
            )
        };
    }

    private static void ValidateMmdbKey(string key)
    {
        if (!key.StartsWith(MmdbPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ConfigValidationException(
                $"filters.{key}",
                $"Only custom '{MmdbPrefix}*' keys are supported (e.g. '{MmdbPrefix}country.iso_code')."
            );
    }
}