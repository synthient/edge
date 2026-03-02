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
    private static readonly ProxyEvent CloudflareEvent = new(IPAddress.Loopback, "cloudflare", 0L);
    private static readonly ProxyEvent FastlyEvent = new(IPAddress.Loopback, "fastly", 0L);
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
            ["cf"] = ProviderFilter("cloudflare"),
            ["ff"] = ProviderFilter("fastly")
        };
        var config = new BucketDefinition { Ttl = "00:05:00", All = ["cf", "ff"] }.Build("bucket", filters);

        // CloudflareEvent matches "cf" but not "ff"
        Assert.That(config.Matches(CloudflareEvent, null), Is.False);
    }

    [Test]
    public void Build_WithAnyFilter_AcceptsIfAtLeastOneMatches()
    {
        var filters = new Dictionary<string, FilterConfig>
        {
            ["cf"] = ProviderFilter("cloudflare"),
            ["ff"] = ProviderFilter("fastly")
        };
        var config = new BucketDefinition { Ttl = "00:05:00", Any = ["cf", "ff"] }.Build("bucket", filters);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.Matches(CloudflareEvent, null), Is.True);
            Assert.That(config.Matches(FastlyEvent, null), Is.True);
        }
    }

    [Test]
    public void Build_WithNotFilter_ExcludesMatchingEvents()
    {
        var filters = new Dictionary<string, FilterConfig> { ["cf"] = ProviderFilter("cloudflare") };
        var config = new BucketDefinition { Ttl = "00:05:00", Not = ["cf"] }.Build("bucket", filters);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.Matches(CloudflareEvent, null), Is.False);
            Assert.That(config.Matches(FastlyEvent, null), Is.True);
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
        var filters = new Dictionary<string, FilterConfig> { ["cf"] = ProviderFilter("cloudflare") };
        var config = new BucketDefinition { Ttl = "00:05:00", All = ["cf"] }.Build("bucket", filters);

        Assert.That(config.FiltersRequireMmdb, Is.False);
    }
}