#if NET6_0_OR_GREATER
using Xunit;

namespace Tokens.Transformers;

public class ToTimeTransformerTests
{
    private readonly ToTimeTransformer _transformer = new();

    [Fact]
    public void GivenTimeString_WhenTransforming_ThenReturnsTimeOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("14:30:00", ["HH:mm:ss"], options, out var t);

        // Assert
        Assert.True(result);
        Assert.IsType<TimeOnly>(t);
        Assert.Equal(new TimeOnly(14, 30, 0), t);
    }

    [Fact]
    public void GivenTimeWithoutSeconds_WhenTransforming_ThenReturnsTimeOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("14:30", ["HH:mm"], options, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal(new TimeOnly(14, 30, 0), t);
    }

    [Fact]
    public void GivenInvalidString_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("not a time", ["HH:mm:ss"], options, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValidTimeString_WhenTransformingWithoutOptions_ThenReturnsTimeOnly()
    {
        // Arrange / Act — non-options-aware overload
        var result = _transformer.TryTransform("14:30:00", ["HH:mm:ss"], out var t);

        // Assert
        Assert.True(result);
        Assert.IsType<TimeOnly>(t);
    }
}
#endif
