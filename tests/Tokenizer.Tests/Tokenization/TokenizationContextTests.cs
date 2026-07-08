using System.Text;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization;

public class TokenizationContextTests
{
    [Fact]
    public void GivenNewContext_WhenCreated_ThenInitializesCorrectly()
    {
        // Act
        var context = new TokenizationContext();

        // Assert
        Assert.NotNull(context.Candidates);
        Assert.NotNull(context.Replacement);
        Assert.NotNull(context.MatchIds);
        Assert.NotNull(context.DisabledRepeatingTokens);
        Assert.NotNull(context.ReplacementLocation);
    }

    [Fact]
    public void GivenContext_WhenInitialize_ThenSetsEnumeratorAndLocation()
    {
        // Arrange
        var context = new TokenizationContext();
        var input = "Hello World";

        // Act
        context.Initialize(new System.IO.StringReader(input));

        // Assert
        Assert.NotNull(context.Enumerator);
        Assert.NotNull(context.ReplacementLocation);
    }

    [Fact]
    public void GivenContext_WhenSetReplacementLocation_ThenUpdatesLocation()
    {
        // Arrange
        var context = new TokenizationContext();
        var newLocation = new FileLocation();

        // Act
        context.ReplacementLocation = newLocation;

        // Assert
        Assert.Equal(newLocation, context.ReplacementLocation);
    }

    [Fact]
    public void GivenContext_WhenAddToCandidates_ThenCandidatesUpdated()
    {
        // Arrange
        var context = new TokenizationContext();
        var token = new Token(string.Empty, string.Empty, new Tokens.Enumerators.FileLocation());

        // Act
        context.Candidates.Add(token);

        // Assert
        Assert.True(context.Candidates.HasCandidates);
        Assert.Contains(token, context.Candidates.Tokens);
    }

    [Fact]
    public void GivenContext_WhenAppendToReplacement_ThenReplacementUpdated()
    {
        // Arrange
        var context = new TokenizationContext();
        var text = "Hello";

        // Act
        context.Replacement.Append(text);

        // Assert
        Assert.Equal(text, context.Replacement.ToString());
    }

    [Fact]
    public void GivenContext_WhenAddToMatchIds_ThenMatchIdsUpdated()
    {
        // Arrange
        var context = new TokenizationContext();
        var tokenId = 42;

        // Act
        context.MatchIds.Add(tokenId);

        // Assert
        Assert.Contains(tokenId, context.MatchIds);
    }

    [Fact]
    public void GivenContext_WhenAddToDisabledRepeatingTokens_ThenDisabledTokensUpdated()
    {
        // Arrange
        var context = new TokenizationContext();
        var tokenId = 99;

        // Act
        context.DisabledRepeatingTokens.Add(tokenId);

        // Assert
        Assert.Contains(tokenId, context.DisabledRepeatingTokens);
    }

    [Fact]
    public void GivenContext_WhenClearCandidates_ThenCandidatesCleared()
    {
        // Arrange
        var context = new TokenizationContext();
        context.Candidates.Add(new Token(string.Empty, string.Empty, new Tokens.Enumerators.FileLocation()));

        // Act
        context.ClearCandidates();

        // Assert
        Assert.False(context.Candidates.HasCandidates);
    }

    [Fact]
    public void GivenContext_WhenClearReplacement_ThenReplacementCleared()
    {
        // Arrange
        var context = new TokenizationContext();
        context.Replacement.Append("test");

        // Act
        context.ClearReplacement();

        // Assert
        Assert.Equal(0, context.Replacement.Length);
    }

    [Fact]
    public void GivenContext_WhenReset_ThenAllStateReset()
    {
        // Arrange
        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("test"));
        context.Candidates.Add(new Token(string.Empty, string.Empty, new Tokens.Enumerators.FileLocation()));
        context.Replacement.Append("test");
        context.MatchIds.Add(1);
        context.DisabledRepeatingTokens.Add(2);
        context.ReplacementLocation = new FileLocation();

        // Act
        context.Reset();

        // Assert
        Assert.False(context.Candidates.HasCandidates);
        Assert.Equal(0, context.Replacement.Length);
        Assert.Empty(context.MatchIds);
        Assert.Empty(context.DisabledRepeatingTokens);
        Assert.NotNull(context.ReplacementLocation);
    }

    [Fact]
    public void GivenContext_WhenInitializeWithNullReader_ThenThrowsException()
    {
        // Arrange
        var context = new TokenizationContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.Initialize((System.IO.TextReader)null!));
    }

    [Fact]
    public void GivenContext_WhenInitializeMultipleTimes_ThenUpdatesEnumerator()
    {
        // Arrange
        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("first"));

        // Act
        context.Initialize(new System.IO.StringReader("second"));

        // Assert
        Assert.NotNull(context.Enumerator);
    }

    [Fact]
    public void GivenContext_WhenPropertiesAccessed_ThenReturnsCorrectTypes()
    {
        // Arrange
        var context = new TokenizationContext();

        // Act & Assert
        Assert.IsType<CandidateTokenList>(context.Candidates);
        Assert.IsType<StringBuilder>(context.Replacement);
        Assert.IsType<HashSet<int>>(context.MatchIds);
        Assert.IsType<HashSet<int>>(context.DisabledRepeatingTokens);
        Assert.IsType<FileLocation>(context.ReplacementLocation);
    }

    [Fact]
    public void GivenContext_WhenEnumeratorNotInitialized_ThenEnumeratorIsNull()
    {
        // Arrange
        var context = new TokenizationContext();

        // Act & Assert
        Assert.Null(context.Enumerator);
    }

    [Fact]
    public void GivenContext_WhenEnumeratorInitialized_ThenEnumeratorIsNotNull()
    {
        // Arrange
        var context = new TokenizationContext();

        // Act
        context.Initialize(new System.IO.StringReader("test"));

        // Assert
        Assert.NotNull(context.Enumerator);
        Assert.IsType<TokenEnumerator>(context.Enumerator);
    }
}
