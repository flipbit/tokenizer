using Xunit;

namespace Tokens.Diagnostics.Hints;

public class PreambleNearMissHintGeneratorTests
{
    private readonly PreambleNearMissHintGenerator _generator = new();

    [Fact]
    public void GivenCaseInsensitiveNearMiss_WhenGeneratingHint_ThenReturnsHintWithLineNumber()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Registrar",
            Detail = "Registrar:",
        };
        var context = new BuildContext("Line one\nREGISTRAR:\nLine three", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.PreambleNeverFound, "Registrar", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("2", hint, StringComparison.Ordinal);
        Assert.Contains("case difference", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenSubstringNearMiss_WhenGeneratingHint_ThenReturnsHint()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Server",
            Detail = "Name Server:",
        };
        var context = new BuildContext("First line\n  Name Server:  extra text\nThird line", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.PreambleNeverFound, "Server", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("2", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenNoNearMiss_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Registrar",
            Detail = "Registrar:",
        };
        var context = new BuildContext("Completely unrelated text\nNothing here", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.PreambleNeverFound, "Registrar", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonPreambleNeverFoundIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TransformerFailed,
            TokenName = "Registrar",
            Detail = "Registrar:",
        };
        var context = new BuildContext("REGISTRAR: some value", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Registrar", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNoPreambleInEvent_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Token",
        };
        var context = new BuildContext("Some input text", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.PreambleNeverFound, "Token", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }
}
