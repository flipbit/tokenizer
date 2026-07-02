using Xunit;

namespace Tokens.Transformers;

public class CultureInvariantTransformerTests
{
    [Fact]
    public void GivenToLowerTransformer_WhenTransforming_ThenUsesInvariantCulture()
    {
        // Arrange
        var transformer = new ToLowerTransformer();

        // Act
        transformer.TryTransform("TITLE", Array.Empty<string>(), out var result);

        // Assert
        Assert.Equal("title", result);
    }

    [Fact]
    public void GivenToUpperTransformer_WhenTransforming_ThenUsesInvariantCulture()
    {
        // Arrange
        var transformer = new ToUpperTransformer();

        // Act
        transformer.TryTransform("title", Array.Empty<string>(), out var result);

        // Assert
        Assert.Equal("TITLE", result);
    }
}
