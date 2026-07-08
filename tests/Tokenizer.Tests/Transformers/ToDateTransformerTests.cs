#if NET6_0_OR_GREATER
using Xunit;

namespace Tokens.Transformers;

public class ToDateTransformerTests
{
    private readonly ToDateTransformer _transformer = new();

    [Fact]
    public void GivenDateString_WhenTransforming_ThenReturnsDateOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("2024-01-15", ["yyyy-MM-dd"], options, out var t);

        // Assert
        Assert.True(result);
        Assert.IsType<DateOnly>(t);
        Assert.Equal(new DateOnly(2024, 1, 15), t);
    }

    [Fact]
    public void GivenDateTimeString_WhenTransforming_ThenDropsTimeAndReturnsDateOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("2024-01-15 14:30:00", ["yyyy-MM-dd HH:mm:ss"], options, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal(new DateOnly(2024, 1, 15), t);
    }

    [Fact]
    public void GivenNoFormat_WhenTransforming_ThenAutoDetectsAndReturnsDateOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("2024-01-15", Array.Empty<string>(), options, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal(new DateOnly(2024, 1, 15), t);
    }

    [Fact]
    public void GivenInvalidString_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("not a date", ["yyyy-MM-dd"], options, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValidDateString_WhenTransformingWithoutOptions_ThenReturnsDateOnly()
    {
        // Arrange / Act — non-options-aware overload
        var result = _transformer.TryTransform("2024-01-15", ["yyyy-MM-dd"], out var t);

        // Assert
        Assert.True(result);
        Assert.IsType<DateOnly>(t);
    }
}
#endif
