using Xunit;

namespace Tokens.Diagnostics.Hints;

public class MultipleRejectionHintGeneratorTests
{
    private readonly MultipleRejectionHintGenerator _generator = new();

    [Fact]
    public void GivenTwoValidatorRejections_WhenGeneratingHintForLast_ThenSummarizesAllValues()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(TokenizationEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsEmailValidator", value: "first@bad");
        collector.Record(TokenizationEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsEmailValidator", value: "second@bad");
        var trace = collector.GetResult()!;

        // Pre-populate index (normally done by TokenDiagnosticBuilder)
        trace.RejectionsPerToken = new Dictionary<string, List<TokenizationEvent>>(StringComparer.Ordinal)
        {
            ["Email"] = new List<TokenizationEvent> { trace.RawEvents[0], trace.RawEvents[1] },
        };

        var sourceEvent = trace.RawEvents[1]; // the last rejection — must be the actual event for ReferenceEquals

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Email", sourceEvent, trace);

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
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(TokenizationEventType.TransformerFailed,
            tokenName: "Date", decoratorName: "ToDateTimeTransformer", value: "not-a-date-1");
        collector.Record(TokenizationEventType.TransformerFailed,
            tokenName: "Date", decoratorName: "ToDateTimeTransformer", value: "not-a-date-2");
        var trace = collector.GetResult()!;

        // Pre-populate index (normally done by TokenDiagnosticBuilder)
        trace.RejectionsPerToken = new Dictionary<string, List<TokenizationEvent>>(StringComparer.Ordinal)
        {
            ["Date"] = new List<TokenizationEvent> { trace.RawEvents[0], trace.RawEvents[1] },
        };

        var sourceEvent = trace.RawEvents[1]; // the last rejection

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Date", sourceEvent, trace);

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
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(TokenizationEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsEmailValidator", value: "bad@value");
        var trace = collector.GetResult()!;

        // Pre-populate index (normally done by TokenDiagnosticBuilder)
        trace.RejectionsPerToken = new Dictionary<string, List<TokenizationEvent>>(StringComparer.Ordinal)
        {
            ["Email"] = new List<TokenizationEvent> { trace.RawEvents[0] },
        };

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
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Email",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;
        trace.RejectionsPerToken = new Dictionary<string, List<TokenizationEvent>>(StringComparer.Ordinal);

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.PreambleNeverFound, "Email", sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenTwoRejectionsButSourceIsNotLastEvent_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(TokenizationEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsEmailValidator", value: "first@bad");
        collector.Record(TokenizationEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsEmailValidator", value: "second@bad");
        var trace = collector.GetResult()!;

        // Pre-populate index (normally done by TokenDiagnosticBuilder)
        trace.RejectionsPerToken = new Dictionary<string, List<TokenizationEvent>>(StringComparer.Ordinal)
        {
            ["Email"] = new List<TokenizationEvent> { trace.RawEvents[0], trace.RawEvents[1] },
        };

        var sourceEvent = trace.RawEvents[0]; // first rejection, not last

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Email", sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
