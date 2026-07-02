using Xunit;

namespace Tokens;

public class TokenizeResultTests
{
    [Fact]
    public void GivenTokenizeResult_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var template = new Template("test-template", "Name: {Name}");
        var result = new TokenizeResult(template);

        // Act
        var output = result.ToString();

        // Assert
        Assert.Equal("TokenizeResult('test-template': 0 matched, 0 missed)", output);
    }
}
