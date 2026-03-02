using System.Net;
using Synthient.Edge.Config;
using Synthient.Edge.Config.Definitions;
using Synthient.Edge.Models;
using MmdbData = System.Collections.Generic.Dictionary<string, object>;

namespace Synthient.Edge.Tests.Config;

[TestFixture]
[TestOf(typeof(FilterConfig))]
public sealed class FilterConfigTests
{
    private static readonly ProxyEvent Event = new(IPAddress.Loopback, "provider", DateTimeOffset.UtcNow);

    private static FilterConfig MmdbFilter(string key, params string[] values) =>
        new FilterDefinition { MmdbFilters = { [key] = [..values] } }.Build("test");

    [Test]
    public void Matches_WithProviderFilter_IsCaseInsensitive()
    {
        var config = new FilterDefinition { Provider = ["PrOvIdEr"] }.Build("test");
        Assert.That(config.Matches(Event, null), Is.True);
    }

    [Test]
    public void Matches_WithNullMmdb_Rejects()
    {
        var config = MmdbFilter("mmdb.country.iso_code", "US");
        Assert.That(config.Matches(Event, null), Is.False);
    }

    [Test]
    public void Matches_WithMissingLeafKey_Rejects()
    {
        var config = MmdbFilter("mmdb.country.iso_code", "US");
        var mmdb = new MmdbData { ["country"] = new MmdbData { ["other_key"] = "US" } };
        Assert.That(config.Matches(Event, mmdb), Is.False);
    }

    [Test]
    public void Matches_WithNonDictIntermediate_Rejects()
    {
        var config = MmdbFilter("mmdb.country.iso_code", "US");
        var mmdb = new MmdbData { ["country"] = "not-a-dict" };
        Assert.That(config.Matches(Event, mmdb), Is.False);
    }

    [Test]
    public void Matches_WithMatchingStringValue_Passes()
    {
        var config = MmdbFilter("mmdb.country.iso_code", "US");
        var mmdb = new MmdbData { ["country"] = new MmdbData { ["iso_code"] = "US" } };
        Assert.That(config.Matches(Event, mmdb), Is.True);
    }

    [Test]
    public void Matches_WithNonMatchingStringValue_Rejects()
    {
        var config = MmdbFilter("mmdb.country.iso_code", "US");
        var mmdb = new MmdbData { ["country"] = new MmdbData { ["iso_code"] = "FR" } };
        Assert.That(config.Matches(Event, mmdb), Is.False);
    }

    [Test]
    public void Matches_WithMatchingNonStringValue_Passes()
    {
        var config = MmdbFilter("mmdb.confidence", "95");
        var mmdb = new MmdbData { ["confidence"] = 95 };
        Assert.That(config.Matches(Event, mmdb), Is.True);
    }

    [Test]
    public void Matches_WithMmdbFilter_IsCaseInsensitive()
    {
        var config = MmdbFilter("mmdb.country.iso_code", "us");
        var mmdb = new MmdbData { ["country"] = new MmdbData { ["iso_code"] = "US" } };
        Assert.That(config.Matches(Event, mmdb), Is.True);
    }

    [Test]
    public void Matches_WithMultipleAllowedValues_PassesIfAnyMatches()
    {
        var config = MmdbFilter("mmdb.country.iso_code", "US", "CA");
        var mmdb = new MmdbData { ["country"] = new MmdbData { ["iso_code"] = "CA" } };
        Assert.That(config.Matches(Event, mmdb), Is.True);
    }

    [Test]
    public void Matches_WithProviderAndMmdbFilter_RequiresBothToMatch()
    {
        const string providerName = "PrOvIdeR";

        var config = new FilterDefinition
        {
            Provider = [providerName],
            MmdbFilters = { ["mmdb.country.iso_code"] = ["US"] }
        }.Build("test");

        var evt = new ProxyEvent(IPAddress.Loopback, providerName, DateTimeOffset.UtcNow);

        var usMmdb = new MmdbData { ["country"] = new MmdbData { ["iso_code"] = "US" } };
        var frMmdb = new MmdbData { ["country"] = new MmdbData { ["iso_code"] = "FR" } };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.Matches(evt, usMmdb), Is.True); // Provider and MMDB match.
            Assert.That(config.Matches(evt, frMmdb), Is.False); //  Provider matches, but MMDB does not.
        }
    }
}