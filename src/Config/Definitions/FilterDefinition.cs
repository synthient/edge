using System.Collections.Frozen;
using Synthient.Edge.Exceptions;
using YamlDotNet.Serialization;

namespace Synthient.Edge.Config.Definitions;

public sealed class FilterDefinition : IKeyedConfigDefinition<FilterConfig>
{
    private const string MmdbPrefix = "mmdb.";

    /// <summary>
    /// Arbitrary MMDB path filters injected after deserialization (e.g. "mmdb.country.iso_code").
    /// Keys must start with <c>mmdb.</c> and contain no spaces.
    /// </summary>
    [YamlIgnore]
    public Dictionary<string, List<string>> MmdbFilters { get; set; } = [];

    public List<string> Provider { get; set; } = [];

    public FilterConfig Build(string filterName)
    {
        foreach (var key in MmdbFilters.Keys)
            ValidateMmdbKey(filterName, key);

        return new FilterConfig
        (
            providers: Provider.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            mmdbFilters: MmdbFilters.ToFrozenDictionary(
                kv => kv.Key,
                kv => kv.Value.ToFrozenSet(StringComparer.OrdinalIgnoreCase)
            )
        );
    }

    private static void ValidateMmdbKey(string filterName, string key)
    {
        if (!key.StartsWith(MmdbPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ConfigValidationException(
                $"filters.{filterName}.{key}",
                $"Only custom '{MmdbPrefix}*' keys are supported (e.g. '{MmdbPrefix}country.iso_code')."
            );
    }
}