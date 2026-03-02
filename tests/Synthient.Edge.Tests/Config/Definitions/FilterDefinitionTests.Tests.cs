using System.Net;
using Synthient.Edge.Config.Definitions;
using Synthient.Edge.Exceptions;
using Synthient.Edge.Models;

namespace Synthient.Edge.Tests.Config.Definitions;

[TestFixture]
[TestOf(typeof(FilterDefinition))]
public sealed class FilterDefinitionTests
{
    private const string Provider = "provider1";
    private static readonly ProxyEvent ProxyEvent = new(IPAddress.Loopback, Provider, DateTimeOffset.UtcNow);

    [Test]
    public void Build_WithNoFilters_PassesAllEvents()
    {
        var config = new FilterDefinition().Build("test");
        Assert.That(config.Matches(ProxyEvent, null), Is.True);
    }

    [Test]
    public void Build_WithMatchingProvider_Passes()
    {
        var config = new FilterDefinition { Provider = [Provider] }.Build("test");
        Assert.That(config.Matches(ProxyEvent, null), Is.True);
    }

    [Test]
    public void Build_WithProviderFilter_IsCaseInsensitive()
    {
        var config = new FilterDefinition { Provider = [Provider.ToUpper()] }.Build("test");
        Assert.That(config.Matches(ProxyEvent, null), Is.True);
    }

    [Test]
    public void Build_WithNonMatchingProvider_Rejects()
    {
        var config = new FilterDefinition { Provider = ["provider2"] }.Build("test");
        Assert.That(config.Matches(ProxyEvent, null), Is.False);
    }

    [Test]
    public void Build_WithInvalidMmdbKey_ThrowsWithFilterName()
    {
        var def = new FilterDefinition { MmdbFilters = { ["country.iso_code"] = ["US"] } };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build("geo-filter"));
        Assert.That(ex.Field, Does.Contain("geo-filter"));
    }

    [Test]
    public void Build_WithValidMmdbKey_DoesNotThrow() =>
        Assert.DoesNotThrow(() =>
            new FilterDefinition { MmdbFilters = { ["mmdb.country.iso_code"] = ["US"] } }.Build("test")
        );

    [Test]
    public void RequiresMmdb_WhenMmdbFilterPresent_IsTrue()
    {
        var config = new FilterDefinition { MmdbFilters = { ["mmdb.country.iso_code"] = ["US"] } }.Build("test");
        Assert.That(config.RequiresMmdb, Is.True);
    }

    [Test]
    public void RequiresMmdb_WhenOnlyProviderFilter_IsFalse()
    {
        var config = new FilterDefinition { Provider = [Provider] }.Build("test");
        Assert.That(config.RequiresMmdb, Is.False);
    }
}