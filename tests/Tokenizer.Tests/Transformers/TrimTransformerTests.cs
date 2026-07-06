using Xunit;

namespace Tokens.Transformers;

public class TrimTransformerTests
{
    private readonly TrimTransformer _transformer = new();

    [Fact]
    public void GivenStringWithLeadingAndTrailingWhitespace_WhenTransforming_ThenTrimsWhitespace()
    {
        // Arrange
        var input = "  TEST  ";

        // Act
        _transformer.TryTransform(input, null!, out var t);

        // Assert
        Assert.Equal("TEST", t);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = _transformer.TryTransform(input, null!, out var t);

        // Assert
        Assert.Equal(string.Empty, t);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        string input = null!;

        // Act
        var result = _transformer.TryTransform(input, null!, out var t);

        // Assert
        Assert.Equal(string.Empty, t);
    }
}
