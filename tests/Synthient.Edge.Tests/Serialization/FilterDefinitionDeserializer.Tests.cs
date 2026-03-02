using Synthient.Edge.Config.Definitions;
using Synthient.Edge.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Synthient.Edge.Tests.Serialization;

[TestFixture]
[TestOf(typeof(FilterDefinitionDeserializer))]
public sealed class FilterDefinitionDeserializerTests
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithNodeDeserializer(
            inner => new FilterDefinitionDeserializer(inner),
            s => s.InsteadOf<ObjectNodeDeserializer>()
        )
        .Build();

    [Test]
    public void Deserialize_WithScalarProvider_ReturnsSingleItem()
    {
        var result = Deserializer.Deserialize<FilterDefinition>("provider: provider1");
        Assert.That(result.Provider, Is.EqualTo(["provider1"]));
    }

    [Test]
    public void Deserialize_WithSequenceProvider_ReturnsAllItems()
    {
        var result = Deserializer.Deserialize<FilterDefinition>("provider:\n  - provider1\n  - provider2");
        Assert.That(result.Provider, Is.EqualTo(["provider1", "provider2"]));
    }

    [Test]
    public void Deserialize_WithScalarMmdbFilter_ReturnsSingleItem()
    {
        var result = Deserializer.Deserialize<FilterDefinition>("mmdb.country.iso_code: US");
        Assert.That(result.MmdbFilters["mmdb.country.iso_code"], Is.EqualTo(["US"]));
    }

    [Test]
    public void Deserialize_WithSequenceMmdbFilter_ReturnsAllItems()
    {
        var result = Deserializer.Deserialize<FilterDefinition>("mmdb.country.iso_code:\n  - US\n  - CA");
        Assert.That(result.MmdbFilters["mmdb.country.iso_code"], Is.EqualTo(["US", "CA"]));
    }

    [Test]
    public void Deserialize_WithNonFilterDefinitionType_DelegatesToInner()
    {
        var result = Deserializer.Deserialize<OtherType>("name: test");
        Assert.That(result.Name, Is.EqualTo("test"));
    }

    private sealed class OtherType
    {
        public string? Name { get; set; }
    }
}