using Synthient.Edge.Exceptions;

namespace Synthient.Edge.Tests.Exceptions;

[TestFixture]
[TestOf(typeof(ConfigValidationException))]
public sealed class ConfigValidationExceptionTests
{
    private const string FieldName = "my.field";

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ThrowIfNullOrEmpty_WhenBlank_Throws(string? value)
    {
        var ex = Assert.Throws<ConfigValidationException>(() =>
            ConfigValidationException.ThrowIfNullOrEmpty(FieldName, value)
        );

        Assert.That(ex.Field, Is.EqualTo(FieldName));
    }

    [Test]
    public void ThrowIfNullOrEmpty_WhenValid_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => ConfigValidationException.ThrowIfNullOrEmpty("field", "value"));
    }

    [TestCase(0, 1, 10)]
    [TestCase(11, 1, 10)]
    [TestCase(-1, 0, 5)]
    public void ThrowIfOutOfRange_WhenOutOfBounds_Throws(int value, int min, int max)
    {
        var ex = Assert.Throws<ConfigValidationException>(() =>
            ConfigValidationException.ThrowIfOutOfRange(FieldName, value, min, max)
        );

        Assert.That(ex.Field, Is.EqualTo(FieldName));
    }

    [TestCase(1, 1, 10)]
    [TestCase(10, 1, 10)]
    [TestCase(5, 1, 10)]
    public void ThrowIfOutOfRange_WhenInBounds_DoesNotThrow(int value, int min, int max)
    {
        Assert.DoesNotThrow(() => ConfigValidationException.ThrowIfOutOfRange("field", value, min, max));
    }

    [Test]
    public void ThrowIfNull_WhenNull_Throws()
    {
        var ex = Assert.Throws<ConfigValidationException>(() =>
            ConfigValidationException.ThrowIfNull<string>(FieldName, null)
        );

        Assert.That(ex.Field, Is.EqualTo(FieldName));
    }

    [Test]
    public void ThrowIfNull_WhenNotNull_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => ConfigValidationException.ThrowIfNull("field", new object()));
    }
}