using Xunit;

namespace Tokens.Diagnostics.Hints;

public class RepeatingTokenHintGeneratorTests
{
    private readonly RepeatingTokenHintGenerator _generator = new();

    [Fact]
    public void GivenPriorValidatorFailure_WhenGeneratingHint_ThenMentionsValidatorAndValue()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "NameServers",
            decoratorName: "IsDomainNameValidator",
            value: "not a domain");
        var trace = collector.GetResult()!;

        // Pre-populate index (normally done by TokenDiagnosticBuilder)
        trace.RejectionsPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal)
        {
            ["NameServers"] = new List<DiagnosticEvent> { trace.RawEvents[0] },
        };

        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.RepeatingTokenDisabled,
            TokenName = "NameServers",
            Detail = "Line gap detected",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.RepeatingTokenCutShort, "NameServers", sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("NameServers", hint, StringComparison.Ordinal);
        Assert.Contains("IsDomainNameValidator", hint, StringComparison.Ordinal);
        Assert.Contains("not a domain", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenPriorTransformerFailure_WhenGeneratingHint_ThenMentionsTransformerAndValue()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.TransformerFailed,
            tokenName: "Dates",
            decoratorName: "ToDateTimeTransformer",
            value: "not-a-date");
        var trace = collector.GetResult()!;

        // Pre-populate index (normally done by TokenDiagnosticBuilder)
        trace.RejectionsPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal)
        {
            ["Dates"] = new List<DiagnosticEvent> { trace.RawEvents[0] },
        };

        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.RepeatingTokenDisabled,
            TokenName = "Dates",
            Detail = "Validation failure",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.RepeatingTokenCutShort, "Dates", sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("Dates", hint, StringComparison.Ordinal);
        Assert.Contains("ToDateTimeTransformer", hint, StringComparison.Ordinal);
        Assert.Contains("not-a-date", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenNoPriorFailure_WhenDetailPresent_ThenReturnsDetailBasedHint()
    {
        // Arrange
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;
        trace.RejectionsPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal);

        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.RepeatingTokenDisabled,
            TokenName = "NameServers",
            Detail = "Line gap detected",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.RepeatingTokenCutShort, "NameServers", sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("NameServers", hint, StringComparison.Ordinal);
        Assert.Contains("Line gap detected", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenNoPriorFailureAndNoDetail_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;
        trace.RejectionsPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal);

        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.RepeatingTokenDisabled,
            TokenName = "NameServers",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.RepeatingTokenCutShort, "NameServers", sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonRepeatingTokenIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;
        trace.RejectionsPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal);

        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Token",
            Detail = "Some detail",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Token", sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
