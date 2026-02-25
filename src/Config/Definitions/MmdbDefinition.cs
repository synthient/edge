using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Config.Definitions;

public sealed class MmdbDefinition : IConfigDefinition<MmdbConfig>
{
    public string? Path { get; set; }

    public MmdbConfig Build()
    {
        ConfigValidationException.ThrowIfNullOrEmpty("mmdb.path", Path);

        if (!File.Exists(Path))
            throw new ConfigValidationException("mmdb.path", $"The specified MMDB file does not exist at '{Path}'.");

        return new MmdbConfig(Path);
    }
}