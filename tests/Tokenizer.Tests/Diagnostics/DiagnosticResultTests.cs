using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics;

public class DiagnosticResultTests : TokenizerTestBase
{
    public DiagnosticResultTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenMatchedAndMissedTokens_WhenTokensAccessed_ThenPerTokenDiagnosticsAvailable()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Name", Value = "John" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Age" });

        // Act
        var tokens = result.Tokens;

        // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenOutcome.Matched, tokens[0].Outcome);
        Assert.Equal(TokenOutcome.NeverFound, tokens[1].Outcome);
    }

    [Fact]
    public void GivenEvents_WhenRawEventsAccessed_ThenAllEventsAvailable()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Name" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Age" });

        // Act & Assert
        Assert.Equal(2, result.RawEvents.Count);
    }

    [Fact]
    public void GivenEmptyResult_WhenQueried_ThenReturnsEmptyCollections()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);

        // Act & Assert
        Assert.Empty(result.RawEvents);
        Assert.Empty(result.Tokens);
        Assert.Equal("Matched 0 of 0 tokens.", result.Verdict);
    }

    [Fact]
    public void GivenResult_WhenVerdictAccessed_ThenReturnsVerdictString()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "First" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Second" });

        // Assert
        Assert.Equal("Matched 1 of 2 tokens (1 missed).", result.Verdict);
    }

    [Fact]
    public void GivenFullMatch_WhenCheckingCounts_ThenMatchedCountEqualsTotal()
    {
        // Arrange
        var tokenizer = CreateDiagnosticTokenizer();
        var compiled = tokenizer.Compile("Name: { Name }").Template;

        // Act
        var result = tokenizer.Tokenize(compiled, "Name: Alice");

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Equal(1, diagnostics.MatchedCount);
        Assert.Equal(0, diagnostics.MissedCount);
        Assert.Equal(1, diagnostics.TotalCount);
    }

    [Fact]
    public void GivenPartialMatch_WhenCheckingCounts_ThenMissedCountReflectsMisses()
    {
        // Arrange
        var tokenizer = CreateDiagnosticTokenizer();
        var compiled = tokenizer.Compile("A: { A }\nB: { B }").Template;

        // Act
        var result = tokenizer.Tokenize(compiled, "A: one");

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Equal(1, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
        Assert.Equal(2, diagnostics.TotalCount);
    }
}
