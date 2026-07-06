using Tokens.Enumerators;
using Xunit;

namespace Tokens;

public class HintMatchTests
{
    [Fact]
    public void GivenTwoHintMatchesWithSameValues_WhenCompared_ThenAreEqual()
    {
        // Arrange
        var location = new FileLocation();
        var a = new HintMatch("test", false, location);
        var b = new HintMatch("test", false, location);

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void GivenTwoHintMatchesWithDifferentText_WhenCompared_ThenAreNotEqual()
    {
        // Arrange
        var location = new FileLocation();
        var a = new HintMatch("test", false, location);
        var b = new HintMatch("other", false, location);

        // Act & Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GivenTwoHintMatchesWithDifferentOptional_WhenCompared_ThenAreNotEqual()
    {
        // Arrange
        var location = new FileLocation();
        var a = new HintMatch("test", false, location);
        var b = new HintMatch("test", true, location);

        // Act & Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GivenTwoEqualHintMatches_WhenHashed_ThenHashCodesMatch()
    {
        // Arrange
        var location = new FileLocation();
        var a = new HintMatch("test", false, location);
        var b = new HintMatch("test", false, location);

        // Act & Assert
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GivenHintMatch_WhenComparedToNull_ThenIsNotEqual()
    {
        // Arrange
        var match = new HintMatch("test", false, new FileLocation());

        // Act & Assert
#pragma warning disable CA1508 // Avoid dead conditional code — testing Equals(null) behavior
        Assert.False(match.Equals(null));
#pragma warning restore CA1508
    }

    [Fact]
    public void GivenHintMatch_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var match = new HintMatch("Domain Name", false, new FileLocation());

        // Act
        var result = match.ToString();

        // Assert
        Assert.Equal("HintMatch('Domain Name' @ Ln: 1 Col: 1 Para: 1)", result);
    }
}
