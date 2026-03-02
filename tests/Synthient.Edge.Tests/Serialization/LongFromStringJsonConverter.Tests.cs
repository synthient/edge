using System.Text.Json;
using Synthient.Edge.Serialization;

namespace Synthient.Edge.Tests.Serialization;

[TestFixture]
[TestOf(typeof(LongFromStringJsonConverter))]
public sealed class LongFromStringJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new LongFromStringJsonConverter() }
    };

    [TestCase("\"0\"", 0L)]
    [TestCase("\"1234567890\"", 1234567890L)]
    [TestCase("\"-9999\"", -9999L)]
    public void Read_WithStringToken_ReturnsLong(string json, long expected)
    {
        Assert.That(JsonSerializer.Deserialize<long>(json, Options), Is.EqualTo(expected));
    }

    [TestCase("0", 0L)]
    [TestCase("1234567890", 1234567890L)]
    [TestCase("-9999", -9999L)]
    public void Read_WithNumberToken_ReturnsLong(string json, long expected)
    {
        Assert.That(JsonSerializer.Deserialize<long>(json, Options), Is.EqualTo(expected));
    }

    [Test]
    public void Read_WithInvalidString_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>("\"not-a-number\"", Options));
    }

    [Test]
    public void Read_WithBoolToken_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>("true", Options));
    }

    [Test]
    public void Write_WritesAsNumber()
    {
        Assert.That(JsonSerializer.Serialize(42L, Options), Is.EqualTo("42"));
    }
}