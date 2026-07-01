using System;
using Xunit;

namespace Tokens.Transformers;

public class SubstringBeforeLastTransformerTests
{
    private readonly SubstringBeforeLastTransformer transformer = new();

    [Fact]
    public void GivenStringWithRepeatedSubstring_WhenTransforming_ThenReturnsTextBeforeLastOccurrence()
    {
        // Arrange
        var input = "one two two three";
        var substring = "two";

        // Act
        var result = transformer.CanTransform(input, [substring], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("one two ", transformed);
    }

    [Fact]
    public void GivenTransformerWithMissingArgument_WhenTransforming_ThenThrowsTokenizerException()
    {
        // Arrange
        var input = "one two three";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.CanTransform(input, null!, out var t));
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = transformer.CanTransform(input, null!, out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        string input = null!;

        // Act
        var result = transformer.CanTransform(input, null!, out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }
}
