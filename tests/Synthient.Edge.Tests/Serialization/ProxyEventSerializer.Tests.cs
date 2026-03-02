using Synthient.Edge.Serialization;

namespace Synthient.Edge.Tests.Serialization;

[TestFixture]
[TestOf(typeof(ProxyEventSerializer))]
public sealed class ProxyEventSerializerTests
{
    private const string Provider = "provider1";
    private const long Timestamp = 1234567890L;

    private static readonly string ValidJson =
        $"{{\"ip\":\"127.0.0.1\",\"provider\":\"{Provider}\",\"timestamp\":{Timestamp}}}";

    [Test]
    public void TryDeserialize_WithValidJson_ReturnsTrueWithEvent()
    {
        var result = ProxyEventSerializer.TryDeserialize(ValidJson, out var evt);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(evt, Is.Not.Null);
            Assert.That(evt.Provider, Is.EqualTo(Provider));
            Assert.That(evt.Timestamp, Is.EqualTo(Timestamp));
        }
    }

    [Test]
    public void TryDeserialize_WithTimestampAsString_ParsesCorrectly()
    {
        const string json = """{"ip":"127.0.0.1","provider":"provider1","timestamp":"1234567890"}""";
        var result = ProxyEventSerializer.TryDeserialize(json, out var evt);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(evt, Is.Not.Null);
            Assert.That(evt!.Timestamp, Is.EqualTo(Timestamp));
        }
    }

    [Test]
    public void TryDeserialize_WithInvalidJson_ReturnsFalse()
    {
        var result = ProxyEventSerializer.TryDeserialize("not-json", out var evt);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(evt, Is.Null);
        }
    }

    [Test]
    public void TryDeserialize_WithInvalidIp_ReturnsFalse()
    {
        const string json = """{"ip":"999.999.999.999","provider":"provider1","timestamp":0}""";
        var result = ProxyEventSerializer.TryDeserialize(json, out var evt);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(evt, Is.Null);
        }
    }
}