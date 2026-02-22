using System.Diagnostics.CodeAnalysis;

namespace Synthient.Edge.Exceptions;

public sealed class ConfigValidationException(string field, string message) : ConfigException(message)
{
    public string Field { get; } = field;

    public static void ThrowIfNullOrEmpty(string field, [NotNull] string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ConfigValidationException(field, "Field is required and cannot be empty.");
    }

    public static void ThrowIfOutOfRange(string field, int value, int min, int max)
    {
        if (value < min || value > max)
            throw new ConfigValidationException(field, $"Field must be between {min} and {max}.");
    }

    public static void ThrowIfNull<T>(string field, [NotNull] T? value)
    {
        if (value is null)
            throw new ConfigValidationException(field, "Field is required.");
    }
}