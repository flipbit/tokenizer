using Tokens.Exceptions;
using Xunit;

namespace Tokens.Transformers;

public class RemoveTransformerTests
{
    private readonly RemoveTransformer transformer = new();

    [Fact]
    public void GivenStringWithSubstring_WhenTransforming_ThenRemovesSubstring()
    {
        // Arrange
        var input = "one two three";
        var substringToRemove = "two";

        // Act
        var result = transformer.CanTransform(input, [substringToRemove], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("one  three", transformed);
    }

    [Fact]
    public void GivenTransformerWithMissingArgument_WhenTransforming_ThenThrowsTokenizerException()
    {
        // Arrange
        var input = "one two three";

        // Act & Assert
        Assert.Throws<TokenizerException>(() => transformer.CanTransform(input, null!, out var t));
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
