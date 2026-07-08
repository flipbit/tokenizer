using Xunit;

namespace Tokens.Transformers;

public class TruncateTransformerTests
{
    private readonly TruncateTransformer _transformer = new();

    [Fact]
    public void GivenStringLongerThanMaxLength_WhenTransforming_ThenTruncates()
    {
        // Act
        var result = _transformer.TryTransform("hello world", ["5"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenStringShorterThanMaxLength_WhenTransforming_ThenReturnsUnchanged()
    {
        // Act
        var result = _transformer.TryTransform("hi", ["10"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hi", transformed);
    }

    [Fact]
    public void GivenStringEqualToMaxLength_WhenTransforming_ThenReturnsUnchanged()
    {
        // Act
        var result = _transformer.TryTransform("hello", ["5"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = _transformer.TryTransform(null!, ["5"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = _transformer.TryTransform(string.Empty, ["5"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenMissingArgs_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _transformer.TryTransform("hello", null!, out var t));
    }

    [Fact]
    public void GivenNonIntegerArg_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _transformer.TryTransform("hello", ["abc"], out var t));
    }
}
