using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class PreambleMatchingTests : TokenizerTestBase
{
    public PreambleMatchingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenSimpleTemplate_WhenInputMatches_ThenTokenMatchedAndNoIssues()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "Name: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        Assert.DoesNotContain(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenMissed);
        Assert.Empty(diagnostics.Tokens.SelectMany(t => t.Issues));
        Assert.Equal(1, diagnostics.MatchedCount);
        Assert.Equal(0, diagnostics.MissedCount);
    }

    [Fact]
    public void GivenMultipleTokens_WhenAllMatch_ThenAllTokensMatchedAndCleanVerdict()
    {
        // Arrange
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: Alice\nAge: 30";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Equal(2, diagnostics.MatchedCount);
        Assert.Equal(0, diagnostics.MissedCount);
        Assert.Empty(diagnostics.Tokens.SelectMany(t => t.Issues));
    }

    [Fact]
    public void GivenTemplate_WhenPreambleNotFoundInInput_ThenPreambleNeverFoundIssue()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "Foo: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "Name", StringComparison.Ordinal));
        Assert.Equal(0, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
    }

    [Fact]
    public void GivenTemplate_WhenPreambleCaseMismatches_ThenPreambleNeverFoundWithNearMissHint()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "name: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var issue = Assert.Single(diagnostics.Tokens.SelectMany(t => t.Issues));
        Assert.Equal(DiagnosticIssueType.PreambleNeverFound, issue.Type);
        Assert.Equal("TK001", issue.Code);
        // Near-miss hint generator should suggest the case difference
        Assert.NotNull(issue.Hint);
        Assert.Contains("case", issue.Hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenTemplate_WhenPreambleWhitespaceMismatches_ThenPreambleNeverFound()
    {
        // Arrange
        var template = "Name:  { Name }"; // 2 spaces after colon (preamble is "Name:  ")
        var input = "Name: Alice";        // 1 space after colon

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenTemplate_WhenPreamblePartiallyMatches_ThenPreambleNeverFound()
    {
        // Arrange
        var template = "Username: { User }";
        var input = "User: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "User", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenTokensInOrder_WhenInputIsReversed_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "A: { A }\nB: { B }";
        var input = "B: Two\nA: One";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise actual behaviour
        var diagnostics = result.Diagnostics!;
        // Document: which tokens match and which are missed when input order differs
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        foreach (var issue in diagnostics.Tokens.SelectMany(t => t.Issues))
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
        }
        // At minimum, verify diagnostics are populated
        Assert.NotNull(diagnostics);
        Assert.True(diagnostics.RawEvents.Count > 0);
        Assert.Equal(1, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "B", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenOutOfOrderEnabled_WhenInputIsReversed_ThenBothTokensMatch()
    {
        // Arrange
        var template = "---\nOutOfOrder: true\n---\nA: { A }\nB: { B }";
        var input = "B: Two\nA: One";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "B", StringComparison.Ordinal));
        Assert.Empty(diagnostics.Tokens.SelectMany(t => t.Issues));
    }

    [Fact]
    public void GivenTokensSharingSamePreamblePrefix_WhenInputContainsShorterPrefix_ThenDocumentWhichTokenMatches()
    {
        // Arrange
        var template = "Email: { Email }\nEmail Address: { FullEmail }";
        var input = "Email: a@b.com";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise which token matches
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        foreach (var evt in diagnostics.RawEvents.Where(e => e.Type == DiagnosticEventType.TokenAssigned))
        {
            Output.WriteLine($"Assigned: {evt.TokenName} = {evt.Value}");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal(1, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Email", StringComparison.Ordinal)
              && string.Equals(e.Value, "a@b.com", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenNonRepeatingToken_WhenPreambleAppearsMultipleTimes_ThenFirstOccurrenceMatches()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "Name: Alice\nName: Bob";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var assigned = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Name", StringComparison.Ordinal))
            .ToList();
        Assert.Single(assigned);
        // NOTE: Without an explicit end marker, token captures from preamble to end of input
        Assert.Equal("Alice\nName: Bob", assigned[0].Value);
    }

    [Fact]
    public void GivenTokenAtStartOfTemplate_WhenInputStartsWithValue_ThenTokenMatches()
    {
        // Arrange
        var template = "{ Name } is here";
        var input = "Alice is here";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenTemplate_WhenPreambleFoundButValueIsEmpty_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "A: { A }\nB: { B }";
        var input = "A: \nB: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: does A match with empty value? Is an issue raised?
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        foreach (var evt in diagnostics.RawEvents.Where(e => e.Type == DiagnosticEventType.TokenAssigned))
        {
            Output.WriteLine($"Assigned: {evt.TokenName} = \"{evt.Value}\"");
        }
        foreach (var issue in diagnostics.Tokens.SelectMany(t => t.Issues))
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.MatchedCount);
        Assert.Equal(0, diagnostics.MissedCount);
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal)
              && string.Equals(e.Value, string.Empty, StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "B", StringComparison.Ordinal)
              && string.Equals(e.Value, "hello", StringComparison.Ordinal));
    }

}
