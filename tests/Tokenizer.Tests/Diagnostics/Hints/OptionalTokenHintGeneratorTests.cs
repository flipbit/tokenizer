using Xunit;

namespace Tokens.Diagnostics.Hints;

public class OptionalTokenHintGeneratorTests
{
    private readonly OptionalTokenHintGenerator _generator = new();

    [Fact]
    public void GivenPreambleNeverFoundForOptionalToken_WhenGeneratingHint_ThenMentionsOptional()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.PreambleNeverFound, TokenName = "MiddleName" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "MiddleName",
        };
        var optionalNames = new HashSet<string>(StringComparer.Ordinal) { "MiddleName" };
        var trace = new RuntimeDiagnosticCollector("input", optionalTokenNames: optionalNames).GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("MiddleName", hint, StringComparison.Ordinal);
        Assert.Contains("optional", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenPreambleNeverFoundForNonOptionalToken_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.PreambleNeverFound, TokenName = "LastName" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "LastName",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonPreambleIssueForOptionalToken_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "MiddleName" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "MiddleName",
            DecoratorName = "IsEmailValidator",
            Value = "test",
        };
        var optionalNames = new HashSet<string>(StringComparer.Ordinal) { "MiddleName" };
        var trace = new RuntimeDiagnosticCollector("input", optionalTokenNames: optionalNames).GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
