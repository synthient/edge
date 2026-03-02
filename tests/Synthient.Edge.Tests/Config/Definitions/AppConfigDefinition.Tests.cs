using Synthient.Edge.Config.Definitions;
using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Tests.Config.Definitions;

[TestFixture]
[TestOf(typeof(AppConfigDefinition))]
public sealed class AppConfigDefinitionTests
{
    private const string Endpoint = "127.0.0.1:6379";
    private string _mmdbPath;

    [SetUp]
    public void SetUp()
    {
        _mmdbPath = Path.GetTempFileName();
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_mmdbPath))
            File.Delete(_mmdbPath);
    }

    [Test]
    public void Build_WithNullSource_Throws()
    {
        var def = new AppConfigDefinition
        {
            Sink = new RedisSinkDefinition { Endpoint = Endpoint },
            Mmdb = new MmdbDefinition { Path = _mmdbPath }
        };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("source"));
    }

    [Test]
    public void Build_WithNullSink_Throws()
    {
        var def = new AppConfigDefinition
        {
            Source = new RedisSourceDefinition { Endpoint = Endpoint },
            Mmdb = new MmdbDefinition { Path = _mmdbPath }
        };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("sink"));
    }

    [Test]
    public void Build_WithNullMmdb_Throws()
    {
        var def = new AppConfigDefinition
        {
            Source = new RedisSourceDefinition { Endpoint = Endpoint },
            Sink = new RedisSinkDefinition { Endpoint = Endpoint }
        };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("mmdb"));
    }

    [Test]
    public void Build_WithNoBuckets_Throws()
    {
        var def = new AppConfigDefinition
        {
            Source = new RedisSourceDefinition { Endpoint = Endpoint },
            Sink = new RedisSinkDefinition { Endpoint = Endpoint },
            Mmdb = new MmdbDefinition { Path = _mmdbPath }
        };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("buckets"));
    }

    [Test]
    public void Build_BucketWithUndefinedFilter_ThrowsWithFilterName()
    {
        const string filterName = "undefined";

        var def = new AppConfigDefinition
        {
            Source = new RedisSourceDefinition { Endpoint = Endpoint },
            Sink = new RedisSinkDefinition { Endpoint = Endpoint },
            Mmdb = new MmdbDefinition { Path = _mmdbPath },
            Buckets = new Dictionary<string, BucketDefinition>
            {
                ["bucket"] = new() { Ttl = "00:05:00", All = [filterName] }
            }
        };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Message, Does.Contain(filterName));
    }

    [Test]
    public void Build_WithBadFilterPath_ThrowsWithFilterKey()
    {
        const string filterName = "filter_name";
        const string filterPath = "bad_filter_key";

        var def = new AppConfigDefinition
        {
            Source = new RedisSourceDefinition { Endpoint = Endpoint },
            Sink = new RedisSinkDefinition { Endpoint = Endpoint },
            Mmdb = new MmdbDefinition { Path = _mmdbPath },
            Filters = new Dictionary<string, FilterDefinition>
            {
                [filterName] = new()
                {
                    MmdbFilters = new Dictionary<string, List<string>>
                    {
                        [filterPath] = ["value"]
                    }
                }
            },
            Buckets = new Dictionary<string, BucketDefinition>
            {
                ["bucket"] = new() { Ttl = "00:05:00", All = [filterName] }
            }
        };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo($"filters.{filterName}.{filterPath}"));
    }

    [Test]
    public void Build_WithValidDefinition_ReturnsAppConfig()
    {
        var def = new AppConfigDefinition
        {
            Source = new RedisSourceDefinition { Endpoint = Endpoint },
            Sink = new RedisSinkDefinition { Endpoint = Endpoint },
            Mmdb = new MmdbDefinition { Path = _mmdbPath },
            Buckets = new Dictionary<string, BucketDefinition>
            {
                ["bucket"] = new() { Ttl = "00:05:00" }
            }
        };

        Assert.DoesNotThrow(() => def.Build());
    }
}