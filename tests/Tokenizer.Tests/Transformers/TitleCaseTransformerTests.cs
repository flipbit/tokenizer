using Xunit;

namespace Tokens.Transformers;

public class TitleCaseTransformerTests
{
    private readonly TitleCaseTransformer transformer = new();

    [Fact]
    public void GivenLowercaseString_WhenTransforming_ThenReturnsTitleCase()
    {
        // Act
        var result = transformer.TryTransform("hello world", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello World", transformed);
    }

    [Fact]
    public void GivenUppercaseString_WhenTransforming_ThenReturnsTitleCase()
    {
        // Act
        var result = transformer.TryTransform("HELLO WORLD", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello World", transformed);
    }

    [Fact]
    public void GivenMixedCaseString_WhenTransforming_ThenReturnsTitleCase()
    {
        // Act
        var result = transformer.TryTransform("hELLO wORLD", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello World", transformed);
    }

    [Fact]
    public void GivenSingleWord_WhenTransforming_ThenCapitalizesFirstLetter()
    {
        // Act
        var result = transformer.TryTransform("hello", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(null!, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }
}
