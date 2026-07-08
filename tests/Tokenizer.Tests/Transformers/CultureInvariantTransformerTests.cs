using Xunit;

namespace Tokens.Transformers;

public class CultureInvariantTransformerTests
{
    [Fact]
    public void GivenToLowerTransformer_WhenTransforming_ThenUsesInvariantCulture()
    {
        // Arrange
        var _transformer = new ToLowerTransformer();

        // Act
        _transformer.TryTransform("TITLE", Array.Empty<string>(), out var result);

        // Assert
        Assert.Equal("title", result);
    }

    [Fact]
    public void GivenToUpperTransformer_WhenTransforming_ThenUsesInvariantCulture()
    {
        // Arrange
        var _transformer = new ToUpperTransformer();

        // Act
        _transformer.TryTransform("title", Array.Empty<string>(), out var result);

        // Assert
        Assert.Equal("TITLE", result);
    }
}
