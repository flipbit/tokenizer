using Xunit;

namespace Tokens.Diagnostics.Hints;

public class ChainedDecoratorHintGeneratorTests
{
    private readonly ChainedDecoratorHintGenerator _generator = new();

    [Fact]
    public void GivenValidatorRejectionWithPriorSuccess_WhenGeneratingHint_ThenDescribesChain()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.ValidatorPassed,
            tokenName: "Email", decoratorName: "IsEmailValidator", value: "bad value");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsDomainNameValidator", value: "bad value");
        var trace = collector.GetResult()!;

        // Pre-populate indexes (normally done by TokenDiagnosticBuilder)
        trace.DecoratorSuccessesPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal)
        {
            ["Email"] = new List<DiagnosticEvent> { trace.RawEvents[0] },
        };

        var sourceEvent = trace.RawEvents[1]; // the actual ValidatorFailed event

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Email", sourceEvent, trace);

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
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.TransformerSucceeded,
            tokenName: "Date", decoratorName: "TrimTransformer", value: "2024-01-01");
        collector.Record(DiagnosticEventType.TransformerFailed,
            tokenName: "Date", decoratorName: "ToDateTimeTransformer", value: "2024-01-01");
        var trace = collector.GetResult()!;

        // Pre-populate indexes (normally done by TokenDiagnosticBuilder)
        trace.DecoratorSuccessesPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal)
        {
            ["Date"] = new List<DiagnosticEvent> { trace.RawEvents[0] },
        };

        var sourceEvent = trace.RawEvents[1]; // the actual TransformerFailed event

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Date", sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("TrimTransformer", hint, StringComparison.Ordinal);
        Assert.Contains("ToDateTimeTransformer", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenValidatorRejectionWithNoPriorSuccess_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsEmailValidator", value: "bad value");
        var trace = collector.GetResult()!;

        // Pre-populate indexes with empty successes (normally done by TokenDiagnosticBuilder)
        trace.DecoratorSuccessesPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal);

        var sourceEvent = trace.RawEvents[0];

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Email", sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonRejectionIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenMissed,
            TokenName = "Email",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;
        trace.DecoratorSuccessesPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal);

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.PreambleNeverFound, "Email", sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
