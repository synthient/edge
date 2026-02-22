using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Models.Config.Definitions;

public sealed class MmDbDefinition : IConfigDefinition<MmDbConfig>
{
    public string? Path { get; set; }

    public MmDbConfig Build()
    {
        ConfigValidationException.ThrowIfNullOrEmpty("path", Path);

        if (!File.Exists(Path))
            throw new ConfigValidationException("path", $"The specified MMDB file does not exist at '{Path}'.");

        return new MmDbConfig(Path);
    }
}