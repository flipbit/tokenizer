using Xunit;

namespace Tokens.Diagnostics.Hints;

public class RepeatingTokenHintGeneratorTests
{
    private readonly RepeatingTokenHintGenerator _generator = new();

    [Fact]
    public void GivenPriorValidatorFailure_WhenGeneratingHint_ThenMentionsValidatorAndValue()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("input");
        collector.Record(TokenizationEventType.ValidatorFailed,
            tokenName: "NameServers",
            decoratorName: "IsDomainNameValidator",
            value: "not a domain");
        var trace = collector.GetResult()!;

        // Pre-populate index via BuildContext
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));
        context.RejectionsPerToken["NameServers"] = new List<TokenizationEvent> { trace.RawEvents[0] };

        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.RepeatingTokenDisabled,
            TokenName = "NameServers",
            Detail = "Line gap detected",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.RepeatingTokenCutShort, "NameServers", sourceEvent, context);

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
        var collector = new TokenizationDiagnosticCollector("input");
        collector.Record(TokenizationEventType.TransformerFailed,
            tokenName: "Dates",
            decoratorName: "ToDateTimeTransformer",
            value: "not-a-date");
        var trace = collector.GetResult()!;

        // Pre-populate index via BuildContext
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));
        context.RejectionsPerToken["Dates"] = new List<TokenizationEvent> { trace.RawEvents[0] };

        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.RepeatingTokenDisabled,
            TokenName = "Dates",
            Detail = "Validation failure",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.RepeatingTokenCutShort, "Dates", sourceEvent, context);

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
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.RepeatingTokenDisabled,
            TokenName = "NameServers",
            Detail = "Line gap detected",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.RepeatingTokenCutShort, "NameServers", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("NameServers", hint, StringComparison.Ordinal);
        Assert.Contains("Line gap detected", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenNoPriorFailureAndNoDetail_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.RepeatingTokenDisabled,
            TokenName = "NameServers",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.RepeatingTokenCutShort, "NameServers", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonRepeatingTokenIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TransformerFailed,
            TokenName = "Token",
            Detail = "Some detail",
        };

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Token", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }
}
