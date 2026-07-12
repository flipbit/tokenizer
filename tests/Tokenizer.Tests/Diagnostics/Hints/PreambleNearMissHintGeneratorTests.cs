using Xunit;

namespace Tokens.Diagnostics.Hints;

public class PreambleNearMissHintGeneratorTests
{
    private readonly PreambleNearMissHintGenerator _generator = new();

    [Fact]
    public void GivenCaseInsensitiveNearMiss_WhenGeneratingHint_ThenReturnsHintWithLineNumber()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.PreambleNeverFound, TokenName = "Registrar" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Registrar",
            Detail = "Registrar:",
        };
        var collector = new RuntimeDiagnosticCollector("Line one\nREGISTRAR:\nLine three");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("2", hint, StringComparison.Ordinal);
        Assert.Contains("case difference", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenSubstringNearMiss_WhenGeneratingHint_ThenReturnsHint()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.PreambleNeverFound, TokenName = "Server" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Server",
            Detail = "Name Server:",
        };
        var collector = new RuntimeDiagnosticCollector("First line\n  Name Server:  extra text\nThird line");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("2", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenNoNearMiss_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.PreambleNeverFound, TokenName = "Registrar" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Registrar",
            Detail = "Registrar:",
        };
        var collector = new RuntimeDiagnosticCollector("Completely unrelated text\nNothing here");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonPreambleNeverFoundIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.TransformerFailure, TokenName = "Registrar" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TransformerFailed,
            TokenName = "Registrar",
            Detail = "Registrar:",
        };
        var collector = new RuntimeDiagnosticCollector("REGISTRAR: some value");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNoPreambleInEvent_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.PreambleNeverFound, TokenName = "Token" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Token",
        };
        var collector = new RuntimeDiagnosticCollector("Some input text");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
