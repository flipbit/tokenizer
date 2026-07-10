using Xunit;

namespace Tokens.Diagnostics.Hints;

public class ValidatorValueHintGeneratorTests
{
    private readonly ValidatorValueHintGenerator _generator = new();

    [Fact]
    public void GivenEmailWithoutAtSign_WhenGeneratingHint_ThenMentionsMissingAtSign()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Email" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Email",
            DecoratorName = "IsEmailValidator",
            Value = "notanemail",
        };
        var trace = new RuntimeDiagnosticCollector("i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("@", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenDomainNameWithSpaces_WhenGeneratingHint_ThenMentionsSpaces()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Domain" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Domain",
            DecoratorName = "IsDomainNameValidator",
            Value = "not a domain",
        };
        var trace = new RuntimeDiagnosticCollector("i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("spaces", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenDomainNameWithoutSpaces_WhenGeneratingHint_ThenExplainsNotDomain()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Domain" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Domain",
            DecoratorName = "IsDomainNameValidator",
            Value = "notadomain!",
        };
        var trace = new RuntimeDiagnosticCollector("i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("domain name", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenIsNumericFailure_WhenGeneratingHint_ThenExplainsNotValidNumber()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Count" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Count",
            DecoratorName = "IsNumericValidator",
            Value = "abc",
        };
        var trace = new RuntimeDiagnosticCollector("i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("not a valid number", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenIsPhoneNumberFailure_WhenGeneratingHint_ThenMentionsNonPhoneCharacters()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Phone" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Phone",
            DecoratorName = "IsPhoneNumberValidator",
            Value = "not-a-phone",
        };
        var trace = new RuntimeDiagnosticCollector("i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("non-phone characters", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenUnknownValidator_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Token" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Token",
            DecoratorName = "SomeCustomValidator",
            Value = "value",
        };
        var trace = new RuntimeDiagnosticCollector("i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenNonValidatorRejectionIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.TransformerFailure, TokenName = "Email" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Email",
            DecoratorName = "IsEmailValidator",
            Value = "notanemail",
        };
        var trace = new RuntimeDiagnosticCollector("i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
