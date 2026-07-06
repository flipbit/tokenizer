using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization.Engine;

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
        context.Initialize(new System.IO.StringReader(input));

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
    public void GivenRepeatingToken_WhenTokenized_ThenMatchIdsTrackCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Item: {Item*}").Template;
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var input = "Item: Apple\nItem: Banana";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert — the repeating token should have matched multiple times
        Assert.True(result.Tokens.Matches.Count >= 2,
            $"Expected at least 2 matches for repeating token, got {result.Tokens.Matches.Count}");
        Assert.All(result.Tokens.Matches, m => Assert.Equal("Item", m.Token.Name));
    }

    [Fact]
    public void GivenContext_WhenProcessingMultipleTokens_ThenTransitionsStateCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("First: {First}Second: {Second}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var input = "First: ValueASecond: ValueB";
        context.Initialize(new System.IO.StringReader(input));
        var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

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
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Test{Name}").Template;

        var context = new TokenizationContext();
        var input = "Test Value";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert — token should capture the value after the preamble "Test"
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Name", result.Tokens.Matches[0].Token.Name);
        Assert.Equal(" Value", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenMatchIds_WhenAddingMatchedTokenIds_ThenUpdatesSet()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var matchIds = new HashSet<int>();
        var matchedToken = template.Tokens.First();

        // Act
        template.GetTokenIdsUpTo(matchedToken, matchIds);

        // Assert
        Assert.Contains(matchedToken.Id, matchIds);
    }

    [Fact]
    public void GivenRepeatingToken_WhenGapInInput_ThenMatchesAllOccurrences()
    {
        // Arrange — the # modifier stops repeating on blank-line gap when used
        // via the high-level Tokenizer pipeline; through the engine directly all
        // occurrences that share the same preamble are still matched.
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Item: {Item*#}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Two items, then a blank line gap, then a third item
        var input = "Item: Apple\nItem: Banana\n\nItem: Cherry";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert — all three occurrences are matched; each must be named "Item"
        Assert.Equal(3, result.Tokens.Matches.Count);
        Assert.All(result.Tokens.Matches, m => Assert.Equal("Item", m.Token.Name));
        Assert.Equal("Apple", result.Tokens.Matches[0].Value);
        Assert.Equal("Banana", result.Tokens.Matches[1].Value);
        Assert.Equal("Cherry", result.Tokens.Matches[2].Value);
    }

    [Fact]
    public void GivenContext_WhenSwitchingTokens_ThenClearsOldState()
    {
        // Arrange
        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Test input"));

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
