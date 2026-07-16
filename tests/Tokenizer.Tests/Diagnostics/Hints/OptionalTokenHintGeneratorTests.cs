using Xunit;

namespace Tokens.Diagnostics.Hints;

public class OptionalTokenHintGeneratorTests
{
    private readonly OptionalTokenHintGenerator _generator = new();

    [Fact]
    public void GivenPreambleNeverFoundForOptionalToken_WhenGeneratingHint_ThenMentionsOptional()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "MiddleName",
        };
        var optionalNames = new HashSet<string>(StringComparer.Ordinal) { "MiddleName" };
        var context = new BuildContext("input", outOfOrderTokens: false, optionalNames);

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.PreambleNeverFound, "MiddleName", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("MiddleName", hint, StringComparison.Ordinal);
        Assert.Contains("optional", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenPreambleNeverFoundForNonOptionalToken_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "LastName",
        };
        var context = new BuildContext("input", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.PreambleNeverFound, "LastName", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonPreambleIssueForOptionalToken_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "MiddleName",
            DecoratorName = "IsEmailValidator",
            Value = "test",
        };
        var optionalNames = new HashSet<string>(StringComparer.Ordinal) { "MiddleName" };
        var context = new BuildContext("input", outOfOrderTokens: false, optionalNames);

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "MiddleName", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }
}
