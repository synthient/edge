using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Synthient.Edge.Serialization;

public sealed class LongFromStringJsonConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetInt64();

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                $"Unexpected token parsing long. Expected String or Number, got {reader.TokenType}."
            );

        var span = reader.HasValueSequence
            ? reader.ValueSequence.ToArray()
            : reader.ValueSpan;
        
        return Utf8Parser.TryParse(span, out long value, out _)
            ? value
            : throw new JsonException($"'{reader.GetString()}' is not a valid long.");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}