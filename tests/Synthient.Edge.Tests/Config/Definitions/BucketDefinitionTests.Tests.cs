using System.Collections.Frozen;
using System.Net;
using Synthient.Edge.Config;
using Synthient.Edge.Config.Definitions;
using Synthient.Edge.Exceptions;
using Synthient.Edge.Models;

namespace Synthient.Edge.Tests.Config.Definitions;

[TestFixture]
[TestOf(typeof(BucketDefinition))]
public sealed class BucketDefinitionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly ProxyEvent Event1 = new(IPAddress.Loopback, "provider1", Now);
    private static readonly ProxyEvent Event2 = new(IPAddress.Loopback, "provider2", Now);
    private static readonly Dictionary<string, FilterConfig> EmptyFilters = new();

    private static FilterConfig ProviderFilter(string provider) => new(
        providers: new[] { provider }.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
        mmdbFilters: FrozenDictionary<string, FrozenSet<string>>.Empty
    );

    [Test]
    public void Build_WithValidTtl_ParsesTimeSpan()
    {
        var config = new BucketDefinition { Ttl = "01:30:00" }.Build("bucket", EmptyFilters);
        Assert.That(config.Ttl, Is.EqualTo(TimeSpan.FromMinutes(90)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not-a-timespan")]
    public void Build_WithInvalidTtl_Throws(string? ttl)
    {
        var def = new BucketDefinition { Ttl = ttl };
        Assert.Throws<ConfigValidationException>(() => def.Build("bucket", EmptyFilters));
    }

    [Test]
    public void Build_WithUndefinedFilterReference_ThrowsWithFilterName()
    {
        var def = new BucketDefinition { Ttl = "00:05:00", All = ["ghost"] };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build("bucket", EmptyFilters));
        Assert.That(ex.Message, Does.Contain("ghost"));
    }

    [Test]
    public void Build_WithAllFilter_RequiresEveryFilterToMatch()
    {
        var filters = new Dictionary<string, FilterConfig>
        {
            ["p1"] = ProviderFilter("provider1"),
            ["p2"] = ProviderFilter("provider2")
        };
        var config = new BucketDefinition { Ttl = "00:05:00", All = ["p1", "p2"] }.Build("bucket", filters);

        Assert.That(config.Matches(Event1, null), Is.False);
    }

    [Test]
    public void Build_WithAnyFilter_AcceptsIfAtLeastOneMatches()
    {
        var filters = new Dictionary<string, FilterConfig>
        {
            ["p1"] = ProviderFilter(Event1.Provider),
            ["p2"] = ProviderFilter(Event2.Provider)
        };
        var config = new BucketDefinition { Ttl = "00:05:00", Any = ["p1", "p2"] }.Build("bucket", filters);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.Matches(Event1, null), Is.True);
            Assert.That(config.Matches(Event2, null), Is.True);
        }
    }

    [Test]
    public void Build_WithNotFilter_ExcludesMatchingEvents()
    {
        var filters = new Dictionary<string, FilterConfig> { ["p1"] = ProviderFilter(Event1.Provider) };
        var config = new BucketDefinition { Ttl = "00:05:00", Not = ["p1"] }.Build(Event2.Provider, filters);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.Matches(Event1, null), Is.False);
            Assert.That(config.Matches(Event2, null), Is.True);
        }
    }

    [Test]
    public void FiltersRequireMmdb_WhenReferencedFilterUsesMmdb_IsTrue()
    {
        var mmdbFilter = new FilterDefinition { MmdbFilters = { ["mmdb.country.iso_code"] = ["US"] } }.Build("geo");
        var filters = new Dictionary<string, FilterConfig> { ["geo"] = mmdbFilter };

        var config = new BucketDefinition { Ttl = "00:05:00", All = ["geo"] }.Build("bucket", filters);

        Assert.That(config.FiltersRequireMmdb, Is.True);
    }

    [Test]
    public void FiltersRequireMmdb_WhenNoMmdbFiltersReferenced_IsFalse()
    {
        var filters = new Dictionary<string, FilterConfig> { ["p1"] = ProviderFilter("provider1") };
        var config = new BucketDefinition { Ttl = "00:05:00", All = ["p1"] }.Build("bucket", filters);

        Assert.That(config.FiltersRequireMmdb, Is.False);
    }
}