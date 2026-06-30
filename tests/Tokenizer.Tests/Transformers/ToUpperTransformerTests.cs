using Xunit;

namespace Tokens.Transformers;

public class ToUpperTransformerTests
{
    private readonly ToUpperTransformer transformer = new();

    [Fact]
    public void GivenLowerCaseString_WhenTransforming_ThenConvertsToUpperCase()
    {
        // Arrange
        var input = "test";

        // Act
        transformer.CanTransform(input, null!, out var t);

        // Assert
        Assert.Equal("TEST", t);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        transformer.CanTransform(input, null!, out var t);

        // Assert
        Assert.Equal(string.Empty, t);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        string input = null!;

        // Act
        transformer.CanTransform(input, null!, out var t);

        // Assert
        Assert.Equal(string.Empty, t);
    }
}
