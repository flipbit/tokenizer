using Tokens.Exceptions;
using Xunit;

namespace Tokens.Transformers;

public class SplitTransformerTests
{
    private readonly SplitTransformer transformer = new();

    [Fact]
    public void GivenCommaSeparatedString_WhenTransforming_ThenSplitsIntoArray()
    {
        // Arrange
        var input = "1,2,3,4";
        var separator = ",";

        // Act
        var result = transformer.CanTransform(input, [separator], out var transformed);
        var list = transformed as string[];

        // Assert
        Assert.True(result);
        Assert.NotNull(list);
        Assert.Equal(4, list!.Length);
        Assert.Equal("1", list[0]);
        Assert.Equal("2", list[1]);
        Assert.Equal("3", list[2]);
        Assert.Equal("4", list[3]);
    }

    [Fact]
    public void GivenStringWithoutSeparator_WhenTransforming_ThenReturnsOriginalString()
    {
        // Arrange
        var input = "1-2-3-4";
        var separator = ",";

        // Act
        var result = transformer.CanTransform(input, [separator], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("1-2-3-4", transformed);
    }

    [Fact]
    public void GivenTransformerWithMissingArgument_WhenTransforming_ThenThrowsTokenizerException()
    {
        // Arrange
        var input = "1,2,3,4";

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
