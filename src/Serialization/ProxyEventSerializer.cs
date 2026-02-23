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
            ReadOnlyMemory<byte> messageBytes = message;
            evt = JsonSerializer.Deserialize(messageBytes.Span, ProxyEventJsonContext.Default.ProxyEvent);

            return evt is not null;
        }
        catch (JsonException)
        {
            evt = null;
            return false;
        }
    }
}