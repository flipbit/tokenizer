using System;
using Xunit;

namespace Tokens.Transformers;

public class ToGuidTransformerTests
{
    private readonly ToGuidTransformer transformer = new();

    [Fact]
    public void GivenValidGuidString_WhenTransforming_ThenReturnsGuid()
    {
        // Arrange
        var input = "d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e";

        // Act
        var result = transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<Guid>(transformed);
        Assert.Equal(Guid.Parse(input), transformed);
    }

    [Fact]
    public void GivenGuidWithoutHyphens_WhenTransforming_ThenReturnsGuid()
    {
        // Arrange
        var input = "d3b07384d9a04e9b8a0d1e6b2a3c4d5e";

        // Act
        var result = transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<Guid>(transformed);
    }

    [Fact]
    public void GivenInvalidGuidString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform("not-a-guid", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("not-a-guid", transformed);
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
}
