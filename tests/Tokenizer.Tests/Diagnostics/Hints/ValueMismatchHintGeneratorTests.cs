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
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValueMismatch, "Description", sourceEvent, context);

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
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValueMismatch, "Description", sourceEvent, context);

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
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Description", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }
}
