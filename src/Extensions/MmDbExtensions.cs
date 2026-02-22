namespace Synthient.Edge.Extensions;

internal static class MmdbExtensions
{
    internal static T? TryFind<T>(this MmdbData data, params string[] keys)
    {
        if (keys.Length == 0) return default;

        object? current = data;

        foreach (var key in keys)
        {
            if (current is not MmdbData dict || !dict.TryGetValue(key, out current))
                return default;
        }

        return current is T result ? result : default;
    }
}