using Synthient.Edge.Config.Definitions;
using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Tests.Config.Definitions;

[TestFixture]
[TestOf(typeof(MmdbDefinition))]
public sealed class MmdbDefinitionTests
{
    private string _tempFile = null!;

    [SetUp]
    public void SetUp() => _tempFile = Path.GetTempFileName();

    [TearDown]
    public void TearDown() => File.Delete(_tempFile);

    [Test]
    public void Build_WithExistingFile_ReturnsExpectedPath()
    {
        var config = new MmdbDefinition { Path = _tempFile }.Build();
        Assert.That(config.Path, Is.EqualTo(_tempFile));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Build_WithBlankPath_Throws(string? path)
    {
        var def = new MmdbDefinition { Path = path };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("mmdb.path"));
    }

    [Test]
    public void Build_WithNonExistentFile_Throws()
    {
        var def = new MmdbDefinition { Path = "/nonexistent/db.mmdb" };

        var ex = Assert.Throws<ConfigValidationException>(() => def.Build());
        Assert.That(ex.Field, Is.EqualTo("mmdb.path"));
    }
}