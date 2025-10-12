using Xunit;

namespace Tokens.Transformers;

public class ToLowerTransformerTests
{
    private readonly ToLowerTransformer transformer = new();

    [Fact]
    public void GivenUpperCaseString_WhenTransforming_ThenConvertsToLowerCase()
    {
        // Arrange
        var input = "TEST";

        // Act
        var result = transformer.CanTransform(input, null, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal("test", t);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = transformer.CanTransform(input, null, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, t);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        string input = null;

        // Act
        var result = transformer.CanTransform(input, null, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, t);
    }
}