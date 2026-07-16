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
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        foreach (var evt in diagnostics.RawEvents.Where(e =>
            e.Type == TokenizationEventType.TokenAssigned || e.Type == TokenizationEventType.TokenMissed))
        {
            Output.WriteLine($"{evt.Type}: {evt.TokenName}");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal(0, diagnostics.MatchedCount);
        Assert.Equal(2, diagnostics.MissedCount);
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenMissed
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenMissed
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
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.ValidatorFailed
              && string.Equals(e.TokenName, "Email", StringComparison.Ordinal));
        // Name should still match
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenAssigned
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
        var missed = diagnostics.RawEvents
            .Where(e => e.Type == TokenizationEventType.TokenMissed)
            .ToList();
        Assert.Equal(3, missed.Count);
        Assert.Equal(0, diagnostics.MatchedCount);
        Assert.Equal(3, diagnostics.MissedCount);
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
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenAssigned
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenMissed
              && string.Equals(e.TokenName, "B", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenMissed
              && string.Equals(e.TokenName, "C", StringComparison.Ordinal));
        Assert.Equal(1, diagnostics.MatchedCount);
        Assert.Equal(2, diagnostics.MissedCount);
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
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        var backtracks = diagnostics.RawEvents
            .Where(e => e.Type == TokenizationEventType.BacktrackStarted)
            .ToList();
        Output.WriteLine($"Backtrack events: {backtracks.Count}");
        foreach (var evt in diagnostics.RawEvents.Where(e =>
            e.Type == TokenizationEventType.TokenAssigned || e.Type == TokenizationEventType.TokenMissed))
        {
            Output.WriteLine($"{evt.Type}: {evt.TokenName} = {evt.Value}");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal(2, diagnostics.MatchedCount);
        Assert.Equal(0, diagnostics.MissedCount);
        Assert.Equal(0, backtracks.Count);
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenAssigned
              && string.Equals(e.TokenName, "Label", StringComparison.Ordinal)
              && string.Equals(e.Value, "Name: fake", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal)
              && string.Equals(e.Value, "real", StringComparison.Ordinal));
    }

}
