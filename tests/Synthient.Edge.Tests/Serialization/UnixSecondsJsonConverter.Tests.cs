using System.Text.Json;
using Synthient.Edge.Serialization;

namespace Synthient.Edge.Tests.Serialization;

[TestFixture]
[TestOf(typeof(UnixSecondsJsonConverter))]
public sealed class UnixSecondsJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UnixSecondsJsonConverter() }
    };

    [TestCase(0L)]
    [TestCase(1609459200L)]
    [TestCase(-1L)]
    public void Read_WithNumberToken_ReturnsDateTimeOffset(long seconds)
    {
        var result = JsonSerializer.Deserialize<DateTimeOffset>(seconds.ToString(), Options);
        Assert.That(result, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(seconds)));
    }

    [TestCase(0L)]
    [TestCase(1609459200L)]
    [TestCase(-1L)]
    public void Read_WithStringToken_ReturnsDateTimeOffset(long seconds)
    {
        var result = JsonSerializer.Deserialize<DateTimeOffset>($"\"{seconds}\"", Options);
        Assert.That(result, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(seconds)));
    }

    [Test]
    public void Read_WithInvalidString_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset>("\"not-a-timestamp\"", Options));
    }

    [Test]
    public void Read_WithBoolToken_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset>("true", Options));
    }

    [Test]
    public void Read_WithFloatNumber_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset>("1.5", Options));
    }

    [Test]
    public void Write_WritesAsJsonStringOfUnixSeconds()
    {
        Assert.That(JsonSerializer.Serialize(DateTimeOffset.UnixEpoch, Options), Is.EqualTo("\"0\""));
    }

    [TestCase(0L)]
    [TestCase(1609459200L)]
    public void Write_ThenRead_ReturnsOriginal(long seconds)
    {
        var original = DateTimeOffset.FromUnixTimeSeconds(seconds);
        var json = JsonSerializer.Serialize(original, Options);
        Assert.That(JsonSerializer.Deserialize<DateTimeOffset>(json, Options), Is.EqualTo(original));
    }
}