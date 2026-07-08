using Tokens.Enumerators;
using Xunit;

namespace Tokens;

public class TokenMatchTests
{
    [Fact]
    public void GivenTokenMatch_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var token = new Token("firstName", "Name:", new FileLocation());
        var match = new TokenMatch(token, "John", new FileLocation());

        // Act
        var result = match.ToString();

        // Assert
        Assert.Equal("TokenMatch('firstName' = 'John' @ Ln: 1 Col: 1 Para: 1)", result);
    }

    [Fact]
    public void GivenTokenMatchWithNullValue_WhenToString_ThenHandlesGracefully()
    {
        // Arrange
        var token = new Token("firstName", "Name:", new FileLocation());
        var match = new TokenMatch(token, null!, new FileLocation());

        // Act
        var result = match.ToString();

        // Assert
        Assert.Equal("TokenMatch('firstName' = '' @ Ln: 1 Col: 1 Para: 1)", result);
    }
}
