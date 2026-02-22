using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using StackExchange.Redis;
using Synthient.Edge.Models;

namespace Synthient.Edge.Serialization;

public static class ProxyEventSerializer
{
    public static bool TryDeserialize(RedisValue message, [NotNullWhen(true)] out ProxyEvent? evt)
    {
        try
        {
            var span = ((ReadOnlyMemory<byte>)message).Span;
            evt = JsonSerializer.Deserialize(span, ProxyEventJsonContext.Default.ProxyEvent);

            if (evt is null)
            {
                evt = null;
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            evt = null;
            return false;
        }
    }
}