using System.Linq;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tests.Diagnostics;

public class DiagnosticSummaryBuilderTests
{
    [Fact]
    public void GivenSuccessfulTokenization_WhenBuildingSummary_ThenVerdictReportsAllMatched()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted,
            detail: "Template: test, Tokens: 2, Input length: 20");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Second");
        collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: "Matches: 2, Misses: 0");

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        Assert.Contains("2", summary.Verdict);
        Assert.Empty(summary.Issues);
    }

    [Fact]
    public void GivenTransformerFailure_WhenBuildingSummary_ThenTransformerIssueIsCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TransformerFailed,
            tokenName: "Registered", decoratorName: "ToDateTimeUtc",
            decoratorArgs: new[] { "yyyy-MM-dd" }, value: "21/11/2005",
            location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Registered");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        var transformerIssues = summary.Issues
            .Where(i => i.Type == DiagnosticIssueType.TransformerFailure).ToList();
        Assert.Single(transformerIssues);
        Assert.Equal("Registered", transformerIssues[0].TokenName);
        Assert.Contains("ToDateTimeUtc", transformerIssues[0].Description);
        Assert.Contains("21/11/2005", transformerIssues[0].Description);
    }

    [Fact]
    public void GivenValidatorFailure_WhenBuildingSummary_ThenValidatorIssueIsCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsEmailValidator",
            value: "notanemail");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Email");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        var validatorIssues = summary.Issues
            .Where(i => i.Type == DiagnosticIssueType.ValidatorRejection).ToList();
        Assert.Single(validatorIssues);
        Assert.Equal("Email", validatorIssues[0].TokenName);
        Assert.Contains("IsEmailValidator", validatorIssues[0].Description);
    }

    [Fact]
    public void GivenMissedTokenWithNoPriorFailure_WhenBuildingSummary_ThenPreambleNeverFoundIssueCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Second");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        Assert.NotEmpty(summary.Issues);
        var preambleIssues = summary.Issues
            .Where(i => i.Type == DiagnosticIssueType.PreambleNeverFound).ToList();
        Assert.Single(preambleIssues);
        Assert.Equal("Second", preambleIssues[0].TokenName);
    }

    [Fact]
    public void GivenRepeatingTokenDisabled_WhenBuildingSummary_ThenRepeatingTokenIssueCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.RepeatingTokenDisabled,
            tokenName: "NameServers", detail: "Line gap detected");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        var repeatingIssues = summary.Issues
            .Where(i => i.Type == DiagnosticIssueType.RepeatingTokenCutShort).ToList();
        Assert.Single(repeatingIssues);
        Assert.Equal("NameServers", repeatingIssues[0].TokenName);
    }

    [Fact]
    public void GivenHintMissing_WhenBuildingSummary_ThenHintMissingIssueCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.HintMissing, value: "Expected hint text");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        var hintIssues = summary.Issues
            .Where(i => i.Type == DiagnosticIssueType.HintMissing).ToList();
        Assert.Single(hintIssues);
        Assert.Contains("Expected hint text", hintIssues[0].Description);
    }

    [Fact]
    public void GivenVerdict_WhenTokensMissed_ThenVerdictShowsMatchAndMissCount()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Second");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Third");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;

        // Assert
        Assert.Contains("2", diagnostics.Summary.Verdict); // matched count
        Assert.Contains("1", diagnostics.Summary.Verdict); // missed count
    }
}
