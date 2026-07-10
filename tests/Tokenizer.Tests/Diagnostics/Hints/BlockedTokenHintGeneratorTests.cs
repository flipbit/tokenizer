using Xunit;

namespace Tokens.Diagnostics.Hints;

public class BlockedTokenHintGeneratorTests
{
    private readonly BlockedTokenHintGenerator _generator = new();

    [Fact]
    public void GivenBlockedIssueWithBlockerName_WhenGeneratingHint_ThenMentionsBlockerName()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.Blocked, TokenName = "City" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenMissed,
            TokenName = "City",
            Detail = "Country",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("Country", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenBlockedIssueWithNoBlockerName_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.Blocked, TokenName = "City" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenMissed,
            TokenName = "City",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonBlockedIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.PreambleNeverFound, TokenName = "City" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenMissed,
            TokenName = "City",
            Detail = "Country",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
