using Xunit;

namespace Tokens.Diagnostics.Hints;

public class RepeatingTokenHintGeneratorTests
{
    private readonly RepeatingTokenHintGenerator _generator = new();

    [Fact]
    public void GivenPriorValidatorFailure_WhenGeneratingHint_ThenMentionsValidatorAndValue()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.RepeatingTokenCutShort, TokenName = "NameServers" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.RepeatingTokenDisabled,
            TokenName = "NameServers",
            Detail = "Line gap detected"
        };
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "NameServers",
            decoratorName: "IsDomainNameValidator",
            value: "not a domain");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("NameServers", hint);
        Assert.Contains("IsDomainNameValidator", hint);
        Assert.Contains("not a domain", hint);
    }

    [Fact]
    public void GivenPriorTransformerFailure_WhenGeneratingHint_ThenMentionsTransformerAndValue()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.RepeatingTokenCutShort, TokenName = "Dates" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.RepeatingTokenDisabled,
            TokenName = "Dates",
            Detail = "Validation failure"
        };
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TransformerFailed,
            tokenName: "Dates",
            decoratorName: "ToDateTimeTransformer",
            value: "not-a-date");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("Dates", hint);
        Assert.Contains("ToDateTimeTransformer", hint);
        Assert.Contains("not-a-date", hint);
    }

    [Fact]
    public void GivenNoPriorFailure_WhenDetailPresent_ThenReturnsDetailBasedHint()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.RepeatingTokenCutShort, TokenName = "NameServers" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.RepeatingTokenDisabled,
            TokenName = "NameServers",
            Detail = "Line gap detected"
        };
        var trace = new DiagnosticCollector("template", "input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("NameServers", hint);
        Assert.Contains("Line gap detected", hint);
    }

    [Fact]
    public void GivenNoPriorFailureAndNoDetail_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.RepeatingTokenCutShort, TokenName = "NameServers" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.RepeatingTokenDisabled,
            TokenName = "NameServers",
        };
        var trace = new DiagnosticCollector("template", "input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonRepeatingTokenIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.TransformerFailure, TokenName = "Token" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Token",
            Detail = "Some detail"
        };
        var trace = new DiagnosticCollector("template", "input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
