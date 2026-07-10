using Xunit;

namespace Tokens.Diagnostics.Hints;

public class ChainedDecoratorHintGeneratorTests
{
    private readonly ChainedDecoratorHintGenerator _generator = new();

    [Fact]
    public void GivenValidatorRejectionWithPriorSuccess_WhenGeneratingHint_ThenDescribesChain()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Email" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Email",
            DecoratorName = "IsDomainNameValidator",
            Value = "bad value",
        };
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.ValidatorPassed,
            tokenName: "Email",
            decoratorName: "IsEmailValidator",
            value: "bad value");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email",
            decoratorName: "IsDomainNameValidator",
            value: "bad value");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("IsEmailValidator", hint, StringComparison.Ordinal);
        Assert.Contains("IsDomainNameValidator", hint, StringComparison.Ordinal);
        Assert.Contains("bad value", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenTransformerFailureWithPriorSuccess_WhenGeneratingHint_ThenDescribesChain()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.TransformerFailure, TokenName = "Date" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Date",
            DecoratorName = "ToDateTimeTransformer",
            Value = "2024-01-01",
        };
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.TransformerSucceeded,
            tokenName: "Date",
            decoratorName: "TrimTransformer",
            value: "2024-01-01");
        collector.Record(DiagnosticEventType.TransformerFailed,
            tokenName: "Date",
            decoratorName: "ToDateTimeTransformer",
            value: "2024-01-01");
        var trace = collector.GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("TrimTransformer", hint, StringComparison.Ordinal);
        Assert.Contains("ToDateTimeTransformer", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenValidatorRejectionWithNoPriorSuccess_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Email" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Email",
            DecoratorName = "IsEmailValidator",
            Value = "bad value",
        };
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email",
            decoratorName: "IsEmailValidator",
            value: "bad value");
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
}
