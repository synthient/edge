using Synthient.Edge.Config.Definitions;
using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Tests.Config.Definitions;

[TestFixture]
[TestOf(typeof(ServerDefinition))]
public sealed class ServerDefinitionTests
{
    [Test]
    public void Build_WithDefaults_ReturnsExpectedValues()
    {
        var config = new ServerDefinition().Build();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(config.Port, Is.EqualTo(8080));
        }
    }

    [Test]
    public void Build_WithCustomValues_ReturnsExpectedValues()
    {
        var config = new ServerDefinition { Host = "0.0.0.0", Port = 443 }.Build();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.Host, Is.EqualTo("0.0.0.0"));
            Assert.That(config.Port, Is.EqualTo(443));
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Build_WithBlankHost_Throws(string? host)
    {
        var def = new ServerDefinition { Host = host!, Port = 8080 };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("server.host"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(65536)]
    public void Build_WithPortOutOfRange_Throws(int port)
    {
        var def = new ServerDefinition { Host = "localhost", Port = port };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("server.port"));
    }

    [TestCase(1)]
    [TestCase(65535)]
    public void Build_WithPortAtBoundaries_DoesNotThrow(int port) =>
        Assert.DoesNotThrow(() => new ServerDefinition { Host = "localhost", Port = port }.Build());
}