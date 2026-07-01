using Tokens.Enumerators;
using Xunit;

namespace Tokens.Enumerators;

public class TokenEnumeratorMatchTests
{
    [Fact]
    public void GivenMatchingPreamble_WhenMatch_ThenReturnsTrue()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        Assert.True(enumerator.Match("Name: "));
    }

    [Fact]
    public void GivenNonMatchingPreamble_WhenMatch_ThenReturnsFalse()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        Assert.False(enumerator.Match("Age: "));
    }

    [Fact]
    public void GivenEmptyValue_WhenMatch_ThenReturnsTrue()
    {
        var enumerator = new TokenEnumerator("anything");
        Assert.True(enumerator.Match(""));
        Assert.True(enumerator.Match(null!));
    }

    [Fact]
    public void GivenValueLongerThanRemaining_WhenMatch_ThenReturnsFalse()
    {
        var enumerator = new TokenEnumerator("hi");
        Assert.False(enumerator.Match("hello"));
    }

    [Fact]
    public void GivenAdvancedPosition_WhenMatch_ThenMatchesFromCurrentPosition()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        enumerator.Advance(6);
        Assert.True(enumerator.Match("Alice"));
        Assert.False(enumerator.Match("Name"));
    }

    [Fact]
    public void GivenCaseSensitiveInput_WhenMatch_ThenIsCaseSensitive()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        Assert.True(enumerator.Match("Name"));
        Assert.False(enumerator.Match("name"));
    }
}
