using Xunit;

namespace Tokens.Transformers;

public class SubstringAfterTransformerTests
{
    private readonly SubstringAfterTransformer transformer = new();

    [Fact]
    public void GivenStringWithSubstring_WhenTransforming_ThenReturnsTextAfterSubstring()
    {
        // Arrange
        var input = "one two three";
        var substring = "two";

        // Act
        var result = transformer.TryTransform(input, [substring], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(" three", transformed);
    }

    [Fact]
    public void GivenTransformerWithMissingArgument_WhenTransforming_ThenThrowsTokenizerException()
    {
        // Arrange
        var input = "one two three";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.TryTransform(input, null!, out var t));
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = transformer.TryTransform(input, null!, out var transformed);

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
        var result = transformer.TryTransform(input, null!, out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }
}
