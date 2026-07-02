using Xunit;

namespace Tokens.Transformers;

public class ToIntTransformerTests
{
    private readonly ToIntTransformer transformer = new();

    [Fact]
    public void GivenValidIntegerString_WhenTransforming_ThenReturnsInt()
    {
        // Act
        var result = transformer.TryTransform("42", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<int>(transformed);
        Assert.Equal(42, transformed);
    }

    [Fact]
    public void GivenNegativeIntegerString_WhenTransforming_ThenReturnsNegativeInt()
    {
        // Act
        var result = transformer.TryTransform("-100", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(-100, transformed);
    }

    [Fact]
    public void GivenZeroString_WhenTransforming_ThenReturnsZero()
    {
        // Act
        var result = transformer.TryTransform("0", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(0, transformed);
    }

    [Fact]
    public void GivenFloatString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform("10.5", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("10.5", transformed);
    }

    [Fact]
    public void GivenNonNumericString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform("hello", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(null!, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Null(transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenOverflowValue_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var input = "99999999999999999999";

        // Act
        var result = transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal(input, transformed);
    }
}
