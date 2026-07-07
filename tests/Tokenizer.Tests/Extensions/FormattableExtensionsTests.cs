using Tokens.Extensions;
using Xunit;

namespace Tokens.Tests.Extensions;

public class FormattableExtensionsTests
{
    [Fact]
    public void GivenInteger_WhenToInvariant_ThenReturnsInvariantString()
    {
        // Arrange
        int value = 1234;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("1234", result);
    }

    [Fact]
    public void GivenInteger_WhenToInvariantWithFormat_ThenReturnsFormattedInvariantString()
    {
        // Arrange
        int value = 1234;

        // Act
        var result = value.ToInvariant("N0");

        // Assert
        Assert.Equal("1,234", result);
    }

    [Fact]
    public void GivenLong_WhenToInvariant_ThenReturnsInvariantString()
    {
        // Arrange
        long value = 9876543210;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("9876543210", result);
    }

    [Fact]
    public void GivenNegativeInteger_WhenToInvariant_ThenReturnsInvariantString()
    {
        // Arrange
        int value = -42;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("-42", result);
    }
}
