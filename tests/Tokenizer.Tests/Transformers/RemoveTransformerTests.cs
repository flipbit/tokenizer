using Xunit;

namespace Tokens.Transformers;

public class RemoveTransformerTests
{
    private readonly RemoveTransformer _transformer = new();

    [Fact]
    public void GivenStringWithSubstring_WhenTransforming_ThenRemovesSubstring()
    {
        // Arrange
        var input = "one two three";
        var substringToRemove = "two";

        // Act
        var result = _transformer.TryTransform(input, [substringToRemove], out var transformed);

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
        Assert.Throws<ArgumentException>(() => _transformer.TryTransform(input, null!, out var t));
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = _transformer.TryTransform(input, null!, out var transformed);

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
        var result = _transformer.TryTransform(input, null!, out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenCaseMismatch_WhenTransforming_ThenDoesNotRemove()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = _transformer.TryTransform(input, ["hello"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello World", transformed);
    }

    [Fact]
    public void GivenMultipleOccurrences_WhenTransforming_ThenRemovesAll()
    {
        // Arrange
        var input = "one two one two";

        // Act
        var result = _transformer.TryTransform(input, ["one"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(" two  two", transformed);
    }

    [Fact]
    public void GivenSubstringNotFound_WhenTransforming_ThenReturnsOriginal()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = _transformer.TryTransform(input, ["xyz"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello world", transformed);
    }
}
