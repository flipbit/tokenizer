using Xunit;

namespace Tokens;

public class HintResultTests
{
    [Fact]
    public void GivenEmptyHintResult_WhenToString_ThenReturnsZeroCounts()
    {
        // Arrange
        var result = new HintResult();

        // Act
        var output = result.ToString();

        // Assert
        Assert.Equal("HintResult(0 matched, 0 missed)", output);
    }
}
