using Xunit;

namespace Tokens.Diagnostics.Hints;

public class ValidatorValueHintGeneratorTests
{
    private readonly ValidatorValueHintGenerator _generator = new();

    [Fact]
    public void GivenEmailWithoutAtSign_WhenGeneratingHint_ThenMentionsMissingAtSign()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "Email",
            DecoratorName = "IsEmailValidator",
            Value = "notanemail",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Email", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("@", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenDomainNameWithSpaces_WhenGeneratingHint_ThenMentionsSpaces()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "Domain",
            DecoratorName = "IsDomainNameValidator",
            Value = "not a domain",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Domain", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("spaces", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenDomainNameWithoutSpaces_WhenGeneratingHint_ThenExplainsNotDomain()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "Domain",
            DecoratorName = "IsDomainNameValidator",
            Value = "notadomain!",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Domain", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("domain name", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenIsNumericFailure_WhenGeneratingHint_ThenExplainsNotValidNumber()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "Count",
            DecoratorName = "IsNumericValidator",
            Value = "abc",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Count", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("not a valid number", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenIsPhoneNumberFailure_WhenGeneratingHint_ThenMentionsNonPhoneCharacters()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "Phone",
            DecoratorName = "IsPhoneNumberValidator",
            Value = "not-a-phone",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Phone", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("non-phone characters", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenUnknownValidator_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "Token",
            DecoratorName = "SomeCustomValidator",
            Value = "value",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Token", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonValidatorRejectionIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TransformerFailed,
            TokenName = "Email",
            DecoratorName = "IsEmailValidator",
            Value = "notanemail",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Email", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }
}
