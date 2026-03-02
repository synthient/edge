using Synthient.Edge.Config.Definitions;
using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Tests.Config.Definitions;

[TestFixture]
[TestOf(typeof(RedisSourceDefinition))]
public sealed class RedisSourceDefinitionTests
{
    [Test]
    public void Build_WithAllFields_MapsCorrectly()
    {
        var def = new RedisSourceDefinition
        {
            Endpoint = "redis:6379",
            Password  = "s3cret",
            Ssl       = true,
            Channel   = "events"
        };

        var config = def.Build();

        Assert.That(config.Endpoint, Is.EqualTo("redis:6379"));
        Assert.That(config.Password, Is.EqualTo("s3cret"));
        Assert.That(config.Ssl,      Is.True);
        Assert.That(config.Channel,  Is.EqualTo("events"));
    }

    [Test]
    public void Build_WithNullPassword_Succeeds()
    {
        var config = new RedisSourceDefinition { Endpoint = "redis:6379", Password = null }.Build();
        Assert.That(config.Password, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    public void Build_WithBlankEndpoint_Throws(string? endpoint)
    {
        var def = new RedisSourceDefinition { Endpoint = endpoint };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("source.endpoint"));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Build_WithBlankChannel_Throws(string? channel)
    {
        var def = new RedisSourceDefinition { Endpoint = "redis:6379", Channel = channel! };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("source.channel"));
    }
}