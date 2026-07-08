using Tokens.Enumerators;
using Xunit;

namespace Tokens;

public class TokenResultTests
{
    [Fact]
    public void GivenEmptyTokenResult_WhenToString_ThenReturnsZeroCounts()
    {
        // Arrange
        var result = new TokenResult();

        // Act
        var output = result.ToString();

        // Assert
        Assert.Equal("TokenResult(0 matched, 0 missed)", output);
    }

    [Fact]
    public void GivenTokenResultWithMatchesAndMisses_WhenToString_ThenReturnsCounts()
    {
        // Arrange
        var result = new TokenResult();
        var token = new Token("name", "Name:", new FileLocation());
        result.AddMatch(token, "John", new FileLocation());
        result.AddMiss(token);

        // Act
        var output = result.ToString();

        // Assert
        Assert.Equal("TokenResult(1 matched, 1 missed)", output);
    }
}
