using Tokens.Builders;
using Xunit;

namespace Tokens.Enumerators;

public class TokenEnumeratorMatchTests
{
    [Fact]
    public void GivenMatchingPreamble_WhenMatch_ThenReturnsTrue()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        Assert.True(enumerator.TryMatch("Name: "));
    }

    [Fact]
    public void GivenNonMatchingPreamble_WhenMatch_ThenReturnsFalse()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        Assert.False(enumerator.TryMatch("Age: "));
    }

    [Fact]
    public void GivenEmptyValue_WhenMatch_ThenReturnsTrue()
    {
        var enumerator = new TokenEnumerator("anything");
        Assert.True(enumerator.TryMatch(""));
        Assert.True(enumerator.TryMatch(null!));
    }

    [Fact]
    public void GivenValueLongerThanRemaining_WhenMatch_ThenReturnsFalse()
    {
        var enumerator = new TokenEnumerator("hi");
        Assert.False(enumerator.TryMatch("hello"));
    }

    [Fact]
    public void GivenAdvancedPosition_WhenMatch_ThenMatchesFromCurrentPosition()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        enumerator.Advance(6);
        Assert.True(enumerator.TryMatch("Alice"));
        Assert.False(enumerator.TryMatch("Name"));
    }

    [Fact]
    public void GivenCaseSensitiveInput_WhenMatch_ThenIsCaseSensitive()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        Assert.True(enumerator.TryMatch("Name"));
        Assert.False(enumerator.TryMatch("name"));
    }

    [Fact]
    public void GivenOutOfOrderMode_WhenMultipleNonOptionalTokens_ThenEvaluatesAllTokens()
    {
        // Arrange — input starts with "Name: " which matches the second token
        var enumerator = new TokenEnumerator("Name: Alice");
        var tokens = new[]
        {
            new TokenBuilder().WithName("Age").WithPreamble("Age: ").Build(),
            new TokenBuilder().WithName("Name").WithPreamble("Name: ").Build(),
            new TokenBuilder().WithName("City").WithPreamble("City: ").Build(),
        };
        var matches = new List<Token>();

        // Act — out-of-order mode should scan all tokens, not break on first non-optional
        var found = enumerator.TryMatch(tokens, outOfOrderTokens: true, matches);

        // Assert
        Assert.True(found);
        Assert.Single(matches);
        Assert.Equal("Name", matches[0].Name);
    }

    [Fact]
    public void GivenSequentialMode_WhenFirstTokenIsNonOptional_ThenBreaksAfterFirstToken()
    {
        // Arrange — sequential mode should break after first non-optional token
        var enumerator = new TokenEnumerator("Name: Alice");
        var tokens = new[]
        {
            new TokenBuilder().WithName("Age").WithPreamble("Age: ").Build(),
            new TokenBuilder().WithName("Name").WithPreamble("Name: ").Build(),
        };
        var matches = new List<Token>();

        // Act — sequential mode breaks on first non-optional, so Name is never checked
        var found = enumerator.TryMatch(tokens, outOfOrderTokens: false, matches);

        // Assert
        Assert.False(found);
        Assert.Empty(matches);
    }
}
