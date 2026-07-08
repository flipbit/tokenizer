using Xunit;

namespace Tokens.Transformers;

public class DefaultValueTransformerTests
{
    private readonly DefaultValueTransformer _transformer = new();

    [Fact]
    public void GivenNonEmptyValue_WhenTransforming_ThenReturnsOriginalValue()
    {
        // Act
        var result = _transformer.TryTransform("hello", ["fallback"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFallback()
    {
        // Act
        var result = _transformer.TryTransform(null!, ["N/A"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("N/A", transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFallback()
    {
        // Act
        var result = _transformer.TryTransform(string.Empty, ["default"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("default", transformed);
    }

    [Fact]
    public void GivenWhitespaceOnlyString_WhenTransforming_ThenReturnsWhitespace()
    {
        // Act
        var result = _transformer.TryTransform("   ", ["fallback"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("   ", transformed);
    }

    [Fact]
    public void GivenMissingArgs_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _transformer.TryTransform(null!, null!, out var t));
    }

    [Fact]
    public void GivenEmptyArgs_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _transformer.TryTransform(null!, [], out var t));
    }
}
