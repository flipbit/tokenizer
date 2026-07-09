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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        Assert.DoesNotContain(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed);
        Assert.Empty(diagnostics.Summary.Issues);
        Assert.Equal("Matched 1 of 1 tokens.", diagnostics.Summary.Verdict);
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
        Assert.Equal("Matched 2 of 2 tokens.", diagnostics.Summary.Verdict);
        Assert.Empty(diagnostics.Summary.Issues);
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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "Name", StringComparison.Ordinal));
        Assert.Equal("Matched 0 of 1 tokens (1 missed).", diagnostics.Summary.Verdict);
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
        var issue = Assert.Single(diagnostics.Summary.Issues);
        Assert.Equal(DiagnosticIssueType.PreambleNeverFound, issue.Type);
        // Near-miss hint generator should suggest the case difference
        Assert.NotNull(issue.Hint);
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
        Assert.Contains(diagnostics.Summary.Issues,
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
        Assert.Contains(diagnostics.Summary.Issues,
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
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var issue in diagnostics.Summary.Issues)
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
        }
        // At minimum, verify diagnostics are populated
        Assert.NotNull(diagnostics);
        Assert.True(diagnostics.Events.Count > 0);
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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "B", StringComparison.Ordinal));
        Assert.Empty(diagnostics.Summary.Issues);
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
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e => e.Type == DiagnosticEventType.TokenAssigned))
        {
            Output.WriteLine($"Assigned: {evt.TokenName} = {evt.Value}");
        }
        Assert.NotNull(diagnostics);
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
        var assigned = diagnostics.Events
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
        Assert.Contains(diagnostics.Events,
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
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e => e.Type == DiagnosticEventType.TokenAssigned))
        {
            Output.WriteLine($"Assigned: {evt.TokenName} = \"{evt.Value}\"");
        }
        foreach (var issue in diagnostics.Summary.Issues)
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
        }
        Assert.NotNull(diagnostics);
    }

    private TokenizeResult TokenizeWithDiagnostics(string template, string input)
    {
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        Output.WriteLine(result.Diagnostics!.RenderAlignment());
        return result;
    }

    private TokenizeResult TokenizeWithDiagnostics(string template, string input, TokenizerOptions options)
    {
        options = options with { EnableDiagnostics = true };
        var tokenizer = CreateTokenizer(options);
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        Output.WriteLine(result.Diagnostics!.RenderAlignment());
        return result;
    }
}
