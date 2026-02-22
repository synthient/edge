using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Synthient.Edge.Serialization;

public sealed class IpAddressJsonConverter : JsonConverter<IPAddress>
{
    private const int MaxIpAddressLength = 45;

    public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Unexpected token parsing IPAddress. Expected String, got {reader.TokenType}.");

        if (!reader.HasValueSequence && !reader.ValueIsEscaped)
        {
            if (IPAddress.TryParse(reader.ValueSpan, out var address))
                return address;

            throw new JsonException($"'{Encoding.UTF8.GetString(reader.ValueSpan)}' is not a valid IP address.");
        }

        var ipString = reader.GetString();
        if (IPAddress.TryParse(ipString, out var fallbackAddress))
            return fallbackAddress;

        throw new JsonException($"'{ipString}' is not a valid IP address.");
    }

    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        Span<char> buffer = stackalloc char[MaxIpAddressLength];

        if (!value.TryFormat(buffer, out var charsWritten))
            throw new JsonException($"Failed to format IPAddress '{value}'.");

        writer.WriteStringValue(buffer[..charsWritten]);
    }
}