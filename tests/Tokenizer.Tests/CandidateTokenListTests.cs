using Xunit;

namespace Tokens;

public class CandidateTokenListTests
{
    [Fact]
    public void GivenTokenWithPreamble_WhenAddingToList_ThenListContainsTokenAndSetsPreamble()
    {
        // Arrange
        var token = new Token("foo") { Preamble = "bar" };
        var list = new CandidateTokenList();

        // Act
        list.Add(token);

        // Assert
        Assert.Equal(1, list.Count);
        Assert.Equal("bar", list.Preamble);
    }
}