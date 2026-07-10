using Xunit;

namespace Tokens.Diagnostics.Hints;

public class MultipleRejectionHintGeneratorTests
{
    private readonly MultipleRejectionHintGenerator _generator = new();

    [Fact]
    public void GivenTwoValidatorRejections_WhenGeneratingHintForLast_ThenSummarizesAllValues()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Email" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Email",
            DecoratorName = "IsEmailValidator",
            Value = "second@bad",
        };
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email",
            decoratorName: "IsEmailValidator",
            value: "first@bad");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email",
            decoratorName: "IsEmailValidator",
            value: "second@bad");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("2", hint, StringComparison.Ordinal);
        Assert.Contains("first@bad", hint, StringComparison.Ordinal);
        Assert.Contains("second@bad", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenTwoTransformerFailures_WhenGeneratingHintForLast_ThenSummarizesAllValues()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.TransformerFailure, TokenName = "Date" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Date",
            DecoratorName = "ToDateTimeTransformer",
            Value = "not-a-date-2",
        };
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.TransformerFailed,
            tokenName: "Date",
            decoratorName: "ToDateTimeTransformer",
            value: "not-a-date-1");
        collector.Record(DiagnosticEventType.TransformerFailed,
            tokenName: "Date",
            decoratorName: "ToDateTimeTransformer",
            value: "not-a-date-2");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("2", hint, StringComparison.Ordinal);
        Assert.Contains("not-a-date-1", hint, StringComparison.Ordinal);
        Assert.Contains("not-a-date-2", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenOnlyOneRejection_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Email" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Email",
            DecoratorName = "IsEmailValidator",
            Value = "bad@value",
        };
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email",
            decoratorName: "IsEmailValidator",
            value: "bad@value");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonRejectionIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.PreambleNeverFound, TokenName = "Email" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenMissed,
            TokenName = "Email",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenTwoRejectionsButSourceIsNotLastValue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Email" };

        // sourceEvent has the value of the FIRST rejection, not the last
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Email",
            DecoratorName = "IsEmailValidator",
            Value = "first@bad",
        };
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email",
            decoratorName: "IsEmailValidator",
            value: "first@bad");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email",
            decoratorName: "IsEmailValidator",
            value: "second@bad");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
