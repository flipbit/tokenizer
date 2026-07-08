using Xunit;

namespace Tokens.Transformers;

public class ReplaceTransformerTests
{
    private readonly ReplaceTransformer _transformer = new();

    [Fact]
    public void GivenStringWithReplacement_WhenTransforming_ThenReplacesSubstring()
    {
        // Arrange
        var input = "one two three";
        var oldValue = "two";
        var newValue = "four";

        // Act
        var result = _transformer.TryTransform(input, [oldValue, newValue], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("one four three", transformed);
    }

    [Fact]
    public void GivenTransformerWithMissingArgument_WhenTransforming_ThenThrowsArgumentException()
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
    public void GivenCaseMismatch_WhenTransforming_ThenDoesNotReplace()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = _transformer.TryTransform(input, ["hello", "bye"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello World", transformed);
    }

    [Fact]
    public void GivenMultipleOccurrences_WhenTransforming_ThenReplacesAll()
    {
        // Arrange
        var input = "one two one two";

        // Act
        var result = _transformer.TryTransform(input, ["one", "three"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("three two three two", transformed);
    }

    [Fact]
    public void GivenSubstringNotFound_WhenTransforming_ThenReturnsOriginal()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = _transformer.TryTransform(input, ["xyz", "abc"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello world", transformed);
    }
}
