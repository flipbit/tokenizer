using Xunit;

namespace Tokens;

public class HintUnitTests
{
    [Fact]
    public void GivenRequiredHint_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var hint = new Hint("Domain Name");

        // Act
        var result = hint.ToString();

        // Assert
        Assert.Equal("Hint('Domain Name')", result);
    }

    [Fact]
    public void GivenOptionalHint_WhenToString_ThenIncludesOptionalFlag()
    {
        // Arrange
        var hint = new Hint("Domain Name", Optional: true);

        // Act
        var result = hint.ToString();

        // Assert
        Assert.Equal("Hint('Domain Name', Optional)", result);
    }
}
