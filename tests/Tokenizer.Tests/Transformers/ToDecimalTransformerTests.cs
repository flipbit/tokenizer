using Xunit;

namespace Tokens.Transformers;

public class ToDecimalTransformerTests
{
    private readonly ToDecimalTransformer _transformer = new();

    [Fact]
    public void GivenValidDecimalString_WhenTransforming_ThenReturnsDecimal()
    {
        // Act
        var result = _transformer.TryTransform("123.45", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<decimal>(transformed);
        Assert.Equal(123.45m, transformed);
    }

    [Fact]
    public void GivenIntegerString_WhenTransforming_ThenReturnsDecimal()
    {
        // Act
        var result = _transformer.TryTransform("42", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<decimal>(transformed);
        Assert.Equal(42m, transformed);
    }

    [Fact]
    public void GivenNegativeDecimalString_WhenTransforming_ThenReturnsNegativeDecimal()
    {
        // Act
        var result = _transformer.TryTransform("-99.9", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(-99.9m, transformed);
    }

    [Fact]
    public void GivenNonNumericString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = _transformer.TryTransform("hello", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = _transformer.TryTransform(null!, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Null(transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = _transformer.TryTransform(string.Empty, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, transformed);
    }
}
