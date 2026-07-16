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
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "City",
            Detail = "Country",
        };
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("Country", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenBlockedIssueWithNoBlockerName_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.Blocked, TokenName = "City" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "City",
        };
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonBlockedIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.PreambleNeverFound, TokenName = "City" };
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "City",
            Detail = "Country",
        };
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }
}
