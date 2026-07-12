using Tokens.Diagnostics.Hints;
using Xunit;

namespace Tokens.Diagnostics;

public class IssueFactoryTests
{
    [Fact]
    public void GivenNoHintGenerators_WhenCreatingIssue_ThenHintIsNull()
    {
        // Arrange
        var factory = new IssueFactory(Array.Empty<IHintGenerator>());
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = "Name",
        };
        var diagnostics = new TokenizationDiagnosticCollector("input").GetResult()!;

        // Act
        var issue = factory.Create(DiagnosticIssueType.PreambleNeverFound, sourceEvent, "Token 'Name' was never found.", diagnostics);

        // Assert
        Assert.Equal(DiagnosticIssueType.PreambleNeverFound, issue.Type);
        Assert.Equal("Name", issue.TokenName);
        Assert.Equal("Token 'Name' was never found.", issue.Description);
        Assert.Null(issue.Hint);
    }

    [Fact]
    public void GivenHintGeneratorReturnsHint_WhenCreatingIssue_ThenHintIsPopulated()
    {
        // Arrange
        var factory = new IssueFactory(new IHintGenerator[] { new ConstantHintGenerator("Check the value format.") });
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.ValidatorFailed,
            TokenName = "Email",
            Value = "bad",
        };
        var diagnostics = new TokenizationDiagnosticCollector("input").GetResult()!;

        // Act
        var issue = factory.Create(DiagnosticIssueType.ValidatorRejection, sourceEvent, "Validator rejected 'bad'.", diagnostics);

        // Assert
        Assert.Equal(DiagnosticIssueType.ValidatorRejection, issue.Type);
        Assert.Equal("Check the value format.", issue.Hint);
    }

    [Fact]
    public void GivenBlockedToken_WhenCreatingBlockedIssue_ThenTypeIsBlockedAndHintMentionsBlocker()
    {
        // Arrange
        var factory = new IssueFactory(new IHintGenerator[] { new BlockedTokenHintGenerator() });
        var diagnostics = new TokenizationDiagnosticCollector("input").GetResult()!;

        // Act
        var issue = factory.CreateBlocked(tokenName: "C", blockerName: "B", diagnostics);

        // Assert
        Assert.Equal(DiagnosticIssueType.Blocked, issue.Type);
        Assert.Equal("C", issue.TokenName);
        Assert.NotNull(issue.Hint);
        Assert.Contains("B", issue.Hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenValueMismatch_WhenCreatingIssue_ThenTypeIsValueMismatchAndDescriptionContainsMissedToken()
    {
        // Arrange
        var factory = new IssueFactory(new IHintGenerator[] { new ValueMismatchHintGenerator() });
        var diagnostics = new TokenizationDiagnosticCollector("input").GetResult()!;

        // Act
        var issue = factory.CreateValueMismatch("Description", "Price", diagnostics);

        // Assert
        Assert.Equal(DiagnosticIssueType.ValueMismatch, issue.Type);
        Assert.Equal("Description", issue.TokenName);
        Assert.Contains("Price", issue.Description, StringComparison.Ordinal);
        Assert.NotNull(issue.Hint);
        Assert.Contains("Price", issue.Hint, StringComparison.Ordinal);
    }

    /// <summary>
    /// Test double that always returns a fixed hint string.
    /// </summary>
    private sealed class ConstantHintGenerator : IHintGenerator
    {
        private readonly string _hint;

        internal ConstantHintGenerator(string hint) => _hint = hint;

        public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                       TokenizationEvent sourceEvent, DiagnosticResult trace)
            => _hint;
    }
}
