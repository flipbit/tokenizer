using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class MultiTokenInteractionTests : TokenizerTestBase
{
    public MultiTokenInteractionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenFirstTokenMissing_WhenSecondTokenCouldMatch_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "A: { A }\nB: { B }";
        var input = "B: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: does B match despite A being missed?
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e =>
            e.Type == DiagnosticEventType.TokenAssigned || e.Type == DiagnosticEventType.TokenMissed))
        {
            Output.WriteLine($"{evt.Type}: {evt.TokenName}");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal("Matched 0 of 2 tokens (2 missed).", diagnostics.Summary.Verdict);
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "B", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenFirstTokenValidatorFails_WhenSecondTokenAvailable_ThenSecondTokenMatches()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }\nName: { Name }";
        var input = "Email: Alice\nName: Bob";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        // Email should fail validation ("Alice" is not an email)
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && string.Equals(e.TokenName, "Email", StringComparison.Ordinal));
        // Name should still match
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenAllTokens_WhenInputIsUnrelated_ThenAllTokensMissed()
    {
        // Arrange
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "completely unrelated text";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var missed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenMissed)
            .ToList();
        Assert.Equal(3, missed.Count);
        Assert.Equal("Matched 0 of 3 tokens (3 missed).", diagnostics.Summary.Verdict);
    }

    [Fact]
    public void GivenThreeTokens_WhenMiddleTokenMissing_ThenFirstConsumesRestOfInput()
    {
        // Arrange — without explicit termination, A greedily consumes through end of input
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "A: one\nC: three";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: A consumes to end despite later preambles in input
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "B", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "C", StringComparison.Ordinal));
        Assert.Equal("Matched 1 of 3 tokens (2 missed).", diagnostics.Summary.Verdict);
    }

    [Fact]
    public void GivenPreambleAppearsTwice_WhenFirstIsWrongContextAndSecondIsCorrect_ThenDocumentBacktracking()
    {
        // Arrange — "Name:" appears as part of a value, then as a real preamble
        var template = "Label: { Label }\nName: { Name }";
        var input = "Label: Name: fake\nName: real";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise backtracking behaviour
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        var backtracks = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.BacktrackStarted)
            .ToList();
        Output.WriteLine($"Backtrack events: {backtracks.Count}");
        foreach (var evt in diagnostics.Events.Where(e =>
            e.Type == DiagnosticEventType.TokenAssigned || e.Type == DiagnosticEventType.TokenMissed))
        {
            Output.WriteLine($"{evt.Type}: {evt.TokenName} = {evt.Value}");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal("Matched 2 of 2 tokens.", diagnostics.Summary.Verdict);
        Assert.Equal(0, backtracks.Count);
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Label", StringComparison.Ordinal)
              && string.Equals(e.Value, "Name: fake", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal)
              && string.Equals(e.Value, "real", StringComparison.Ordinal));
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
