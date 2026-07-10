using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class RepeatingTokenTests : TokenizerTestBase
{
    public RepeatingTokenTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenRepeatingToken_WhenAllOccurrencesMatch_ThenAllMatchedNoIssues()
    {
        // Arrange
        var template = "Item: { Item : Repeating }";
        var input = "Item: A\nItem: B\nItem: C";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var assigned = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        Output.WriteLine($"Matched {assigned.Count} occurrences");
        Assert.Equal(3, assigned.Count);
    }

    [Fact]
    public void GivenRepeatingTokenWithValidator_WhenMiddleOccurrenceFails_ThenRepeatingTokenCutShort()
    {
        // Arrange
        var template = "Item: { Item : Repeating, IsNumeric }";
        var input = "Item: 1\nItem: two\nItem: 3";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        foreach (var issue in diagnostics.Tokens.SelectMany(t => t.Issues))
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
            if (issue.Hint != null) Output.WriteLine($"  Hint: {issue.Hint}");
        }
        // Document: is RepeatingTokenDisabled event raised?
        var disabled = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.RepeatingTokenDisabled)
            .ToList();
        Output.WriteLine($"RepeatingTokenDisabled events: {disabled.Count}");
        Assert.NotNull(diagnostics);
        Assert.Equal("Matched 2 of 2 tokens.", diagnostics.Verdict);
        Assert.Equal(0, disabled.Count);
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.ValidatorRejection
              && string.Equals(i.TokenName, "Item", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenRepeatingToken_WhenLineGapExists_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "Item: { Item : Repeating }";
        var input = "Item: A\n\n\nItem: B";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: does a line gap disable repeating?
        var diagnostics = result.Diagnostics!;
        var assigned = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        Output.WriteLine($"Matched {assigned.Count} occurrences");
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        Assert.NotNull(diagnostics);
        Assert.Equal(2, assigned.Count);
        Assert.Equal("Matched 2 of 2 tokens.", diagnostics.Verdict);
    }

    [Fact]
    public void GivenRepeatingToken_WhenPreambleNeverFound_ThenPreambleNeverFoundIssue()
    {
        // Arrange
        var template = "Item: { Item : Repeating }";
        var input = "Nothing here";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "Item", StringComparison.Ordinal));
        // No RepeatingTokenDisabled — it was never started
        Assert.DoesNotContain(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.RepeatingTokenDisabled
              && string.Equals(e.TokenName, "Item", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenRepeatingTokenWithValidator_WhenOneMatchThenFailure_ThenOneMatchAndCutShort()
    {
        // Arrange
        var template = "Item: { Item : Repeating, IsNumeric }";
        var input = "Item: 1\nItem: nope";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var assigned = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        Output.WriteLine($"Matched {assigned.Count} occurrences");
        Assert.Equal(1, assigned.Count);
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
    }

    private TokenizeResult TokenizeWithDiagnostics(string template, string input)
    {
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        Output.WriteLine(result.Diagnostics!.RenderAlignment());
        return result;
    }
}
