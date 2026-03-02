using Synthient.Edge.Config.Definitions;
using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Tests.Config.Definitions;

[TestFixture]
[TestOf(typeof(RedisSinkDefinition))]
public sealed class RedisSinkDefinitionTests
{
    [Test]
    public void Build_WithAllFields_MapsCorrectly()
    {
        var def = new RedisSinkDefinition
        {
            Endpoint = "redis:6380",
            Password = "s3cret",
            Ssl      = true,
            Database = 3
        };

        var config = def.Build();

        Assert.That(config.Endpoint, Is.EqualTo("redis:6380"));
        Assert.That(config.Password, Is.EqualTo("s3cret"));
        Assert.That(config.Ssl,      Is.True);
        Assert.That(config.Database, Is.EqualTo(3));
    }

    [Test]
    public void Build_WithNullPassword_Succeeds()
    {
        var config = new RedisSinkDefinition { Endpoint = "redis:6380", Password = null }.Build();
        Assert.That(config.Password, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    public void Build_WithBlankEndpoint_Throws(string? endpoint)
    {
        var def = new RedisSinkDefinition { Endpoint = endpoint };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("sink.endpoint"));
    }
}