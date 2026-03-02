using System.Net;
using System.Text.Json;
using Synthient.Edge.Serialization;

namespace Synthient.Edge.Tests.Serialization;

[TestFixture]
[TestOf(typeof(IpAddressJsonConverter))]
public sealed class IpAddressJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new IpAddressJsonConverter() }
    };

    [TestCase("127.0.0.1")]
    [TestCase("192.168.0.1")]
    [TestCase("::1")]
    [TestCase("2001:db8:1234:5678:abcd:ef01:2345:6789")]
    public void Read_WithValidIpString_ReturnsAddress(string ip)
    {
        var result = JsonSerializer.Deserialize<IPAddress>($"\"{ip}\"", Options);
        Assert.That(result, Is.EqualTo(IPAddress.Parse(ip)));
    }

    [Test]
    public void Read_WithNullToken_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<IPAddress?>("null", Options);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Read_WithInvalidString_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPAddress>("\"not-an-ip\"", Options));
    }

    [Test]
    public void Read_WithNonStringToken_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IPAddress>("123", Options));
    }

    [TestCase("127.0.0.1")]
    [TestCase("192.168.0.1")]
    [TestCase("::1")]
    [TestCase("2001:db8:1234:5678:abcd:ef01:2345:6789")]
    public void Serialize_ThenDeserialize_ReturnsOriginal(string ip)
    {
        var address = IPAddress.Parse(ip);
        var json = JsonSerializer.Serialize(address, Options);
        Assert.That(JsonSerializer.Deserialize<IPAddress>(json, Options), Is.EqualTo(address));
    }
}