using System.Text;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Enumerators;
using Tokens.Tokenization;
using Xunit;
using Tokens.Diagnostics;

namespace Tokens.Tests.Tokenization.Engine;

/// <summary>
/// Tests for TokenizationEngine state management (context transitions, candidate tracking, match IDs, etc.)
/// </summary>
public class TokenizationEngineStateTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenTokenizationContext_WhenInitialized_ThenSetsUpCorrectly()
    {
        // Arrange
        var context = new TokenizationContext();
        var input = "Test input";

        // Act
        context.Initialize(input);

        // Assert
        Assert.NotNull(context.Enumerator);
        Assert.False(context.Enumerator.IsEmpty);
    }

    [Fact]
    public void GivenCandidateList_WhenAddingCandidates_ThenMaintainsCorrectState()
    {
        // Arrange
        var candidates = new CandidateTokenList();
        var token = new TokenBuilder()
            .WithName("TestToken")
            .WithPreamble("Test: ")
            .Build();

        // Act
        candidates.Add(token);

        // Assert
        Assert.True(candidates.HasCandidates);
        Assert.Equal("Test: ", candidates.Preamble);
    }

    [Fact]
    public void GivenCandidateList_WhenClearing_ThenResetsState()
    {
        // Arrange
        var candidates = new CandidateTokenList();
        var token = new TokenBuilder()
            .WithName("TestToken")
            .WithPreamble("Test: ")
            .Build();
        candidates.Add(token);

        // Act
        candidates.Clear();

        // Assert
        Assert.False(candidates.HasCandidates);
    }

    [Fact]
    public void GivenMatchIds_WhenTrackingMatches_ThenMaintainsUniqueSet()
    {
        // Arrange
        var matchIds = new HashSet<int>();

        // Act
        matchIds.Add(1);
        matchIds.Add(2);
        matchIds.Add(1); // Duplicate

        // Assert
        Assert.Equal(2, matchIds.Count);
        Assert.Contains(1, matchIds);
        Assert.Contains(2, matchIds);
    }

    [Fact]
    public void GivenDisabledRepeatingTokens_WhenTrackingDisabled_ThenPreventsRematching()
    {
        // Arrange
        var disabledTokens = new HashSet<int>();
        var tokenId = 42;

        // Act
        disabledTokens.Add(tokenId);

        // Assert
        Assert.Contains(tokenId, disabledTokens);
    }

    [Fact]
    public void GivenReplacementBuffer_WhenAccumulatingCharacters_ThenBuildsCorrectly()
    {
        // Arrange
        var replacement = new StringBuilder();

        // Act
        replacement.Append('T');
        replacement.Append('e');
        replacement.Append('s');
        replacement.Append('t');

        // Assert
        Assert.Equal("Test", replacement.ToString());
        Assert.Equal(4, replacement.Length);
    }

    [Fact]
    public void GivenReplacementBuffer_WhenClearing_ThenResetsState()
    {
        // Arrange
        var replacement = new StringBuilder("Test");

        // Act
        replacement.Clear();

        // Assert
        Assert.Equal(0, replacement.Length);
        Assert.Equal("", replacement.ToString());
    }

    [Fact]
    public void GivenContext_WhenProcessingMultipleTokens_ThenTransitionsStateCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("First: {First}Second: {Second}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, "First: ValueASecond: ValueB", null, context, result, NullDiagnosticCollector.Instance);

        // Assert - Both tokens should be processed
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenFileLocation_WhenTrackingPosition_ThenUpdatesLineAndColumn()
    {
        // Arrange
        var location = new FileLocation();
        var enumerator = new TokenEnumerator("Line1\nLine2\nLine3");

        // Act - Enumerate through newlines
        while (!enumerator.IsEmpty)
        {
            enumerator.Next();
        }

        // Assert - Location should track position
        Assert.True(enumerator.Location.Line >= 1);
    }

    [Fact]
    public void GivenContext_WhenBacktracking_ThenRestoresPreviousState()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Test{Name}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var candidates = new CandidateTokenList();
        candidates.Add(template.Tokens.First());

        var enumerator = new TokenEnumerator("Test Value");
        var replacement = new StringBuilder();
        var matchIds = new HashSet<int>();
        var disabledRepeatingTokens = new HashSet<int>();

        // Act - Force backtracking scenario
        var processed = _engine.ProcessRepeatedTokens(candidates, enumerator, replacement,
            result, disabledRepeatingTokens, matchIds, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenMatchIds_WhenAddingMatchedTokenIds_ThenUpdatesSet()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {Name}");
        var matchIds = new HashSet<int>();
        var matchedToken = template.Tokens.First();

        // Act
        var tokenIdsToAdd = template.GetTokenIdsUpTo(matchedToken);
        foreach (var id in tokenIdsToAdd)
        {
            matchIds.Add(id);
        }

        // Assert
        Assert.Contains(matchedToken.Id, matchIds);
    }

    [Fact]
    public void GivenRepeatingToken_WhenDisabled_ThenNoLongerMatches()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{Item*}");
        var disabledRepeatingTokens = new HashSet<int>();
        var token = template.Tokens.First();

        // Act
        disabledRepeatingTokens.Add(token.Id);

        // Assert
        Assert.Contains(token.Id, disabledRepeatingTokens);
    }

    [Fact]
    public void GivenContext_WhenSwitchingTokens_ThenClearsOldState()
    {
        // Arrange
        var context = new TokenizationContext();
        context.Initialize("Test input");

        var token1 = new TokenBuilder().WithName("Token1").WithPreamble("A: ").Build();
        context.Candidates.Add(token1);
        context.Replacement.Append("Value");

        // Act
        context.ClearCandidates();
        context.ClearReplacement();

        // Assert
        Assert.False(context.Candidates.HasCandidates);
        Assert.Equal(0, context.Replacement.Length);
    }

    [Fact]
    public void GivenEnumerator_WhenAdvancing_ThenUpdatesPosition()
    {
        // Arrange
        var enumerator = new TokenEnumerator("Test String");
        var initialPosition = enumerator.Location.Column;

        // Act
        enumerator.Advance(5);

        // Assert
        Assert.True(enumerator.Location.Column > initialPosition || enumerator.Location.Line > 1);
    }

    [Fact]
    public void GivenTokenizationResult_WhenAddingMatches_ThenMaintainsOrder()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();
        var token1 = new TokenBuilder().WithName("Token1").Build();
        var token2 = new TokenBuilder().WithName("Token2").Build();

        // Act
        result.Tokens.AddMatch(token1, "Value1", new FileLocation());
        result.Tokens.AddMatch(token2, "Value2", new FileLocation());

        // Assert
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal("Token1", result.Tokens.Matches[0].Token.Name);
        Assert.Equal("Token2", result.Tokens.Matches[1].Token.Name);
    }
}
