using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class CandidateTokenListTests : TokenizerTestBase
{
    public CandidateTokenListTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenTokenWithPreamble_WhenAddingToList_ThenListContainsTokenAndSetsPreamble()
    {
        // Arrange
        var token = new Token("foo", string.Empty, "bar", new Tokens.Enumerators.FileLocation());
        var list = new CandidateTokenList();

        // Act
        list.Add(token);

        // Assert
        Assert.Equal(1, list.Count);
        Assert.Equal("bar", list.Preamble);
    }
}
