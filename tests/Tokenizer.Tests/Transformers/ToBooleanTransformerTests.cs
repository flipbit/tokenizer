using Xunit;

namespace Tokens.Transformers;

public class ToBooleanTransformerTests
{
    private readonly ToBooleanTransformer _transformer = new();

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("Yes")]
    [InlineData("YES")]
    [InlineData("1")]
    public void GivenTruthyString_WhenTransforming_ThenReturnsTrueBoolean(string input)
    {
        // Act
        var result = _transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<bool>(transformed);
        Assert.True((bool)transformed);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("no")]
    [InlineData("No")]
    [InlineData("NO")]
    [InlineData("0")]
    public void GivenFalsyString_WhenTransforming_ThenReturnsFalseBoolean(string input)
    {
        // Act
        var result = _transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<bool>(transformed);
        Assert.False((bool)transformed);
    }

    [Fact]
    public void GivenUnrecognizedString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = _transformer.TryTransform("maybe", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("maybe", transformed);
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
