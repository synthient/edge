using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Synthient.Edge.Serialization;

public class UnixSecondsJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var seconds))
            return DateTimeOffset.FromUnixTimeSeconds(seconds);

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected number or string, found {reader.TokenType}.");

        var span = reader.ValueSpan;

        if (!Utf8Parser.TryParse(span, out seconds, out var bytesConsumed) || bytesConsumed != span.Length)
        {
            var stringValue = System.Text.Encoding.UTF8.GetString(span);
            throw new JsonException($"Invalid Unix timestamp (seconds): '{stringValue}'.");
        }

        return DateTimeOffset.FromUnixTimeSeconds(seconds);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[32];

        if (!Utf8Formatter.TryFormat(value.ToUnixTimeSeconds(), buffer, out var bytesWritten))
            throw new InvalidOperationException("Failed to format Unix timestamp.");

        writer.WriteStringValue(buffer[..bytesWritten]);
    }
}