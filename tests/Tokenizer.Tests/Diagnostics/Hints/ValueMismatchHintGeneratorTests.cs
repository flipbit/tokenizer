using Xunit;

namespace Tokens.Diagnostics.Hints;

public class ValueMismatchHintGeneratorTests
{
    private readonly ValueMismatchHintGenerator _generator = new();

    [Fact]
    public void GivenValueMismatchIssueWithMissedToken_WhenGeneratingHint_ThenIncludesMissedTokenName()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenAssigned,
            TokenName = "Description",
            Value = "some greedy value",
            Detail = "Price",
        };
        var trace = new TokenizationDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValueMismatch, "Description", sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("Price", hint, StringComparison.Ordinal);
        Assert.Contains("end delimiter", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenValueMismatchIssueWithNoMissedToken_WhenGeneratingHint_ThenSuggestsEndDelimiter()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenAssigned,
            TokenName = "Description",
            Value = "some greedy value",
        };
        var trace = new TokenizationDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValueMismatch, "Description", sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("end delimiter", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenNonValueMismatchIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "Description",
            DecoratorName = "IsEmailValidator",
            Value = "test",
        };
        var trace = new TokenizationDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Description", sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
