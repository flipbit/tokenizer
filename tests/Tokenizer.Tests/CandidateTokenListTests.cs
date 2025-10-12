using Xunit;

namespace Tokens;

public class CandidateTokenListTests
{
    [Fact]
    public void TestAddToken()
    {
        var token = new Token("foo") { Preamble = "bar" };

        var list = new CandidateTokenList();

        list.Add(token);

        Assert.Equal(1, list.Count);
        Assert.Equal("bar", list.Preamble);
    }
}