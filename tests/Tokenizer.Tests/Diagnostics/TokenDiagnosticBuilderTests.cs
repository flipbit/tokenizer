using Tokens.Enumerators;
using Xunit;

namespace Tokens.Diagnostics;

public class TokenDiagnosticBuilderTests
{
    [Fact]
    public void GivenSingleMatchedToken_WhenBuilding_ThenTokenHasMatchedOutcome()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Name: John");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Name", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssignmentAttempted, tokenName: "Name", value: "John");
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Name", value: "John", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        Assert.Single(tokens);
        Assert.Equal("Name", tokens[0].TokenName);
        Assert.Equal(TokenOutcome.Matched, tokens[0].Outcome);
        Assert.Equal("John", tokens[0].AssignedValue);
        Assert.Single(tokens[0].Attempts);
        Assert.Equal(AttemptOutcome.Assigned, tokens[0].Attempts[0].Outcome);
        Assert.Empty(tokens[0].Issues);
    }

    [Fact]
    public void GivenMissedToken_WhenBuilding_ThenTokenHasNeverFoundOutcome()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("nothing");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Name");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        Assert.Single(tokens);
        Assert.Equal("Name", tokens[0].TokenName);
        Assert.Equal(TokenOutcome.NeverFound, tokens[0].Outcome);
        Assert.Null(tokens[0].AssignedValue);
        Assert.Empty(tokens[0].Attempts);
        Assert.Single(tokens[0].Issues);
        Assert.Equal(DiagnosticIssueType.PreambleNeverFound, tokens[0].Issues[0].Type);
    }

    [Fact]
    public void GivenValidatorRejection_WhenBuilding_ThenTokenHasRejectedOutcomeWithAttempts()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Email: bad");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Email", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssignmentAttempted, tokenName: "Email", value: "bad");
        collector.Record(TokenizationEventType.ValidatorFailed, tokenName: "Email",
            decoratorName: "IsEmailValidator", value: "bad", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssignmentFailed, tokenName: "Email", value: "bad");
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Email");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        Assert.Single(tokens);
        Assert.Equal(TokenOutcome.Rejected, tokens[0].Outcome);
        Assert.Single(tokens[0].Attempts);
        Assert.Equal(AttemptOutcome.ValidatorRejected, tokens[0].Attempts[0].Outcome);
        Assert.Equal("IsEmailValidator", tokens[0].Attempts[0].DecoratorName);
        Assert.Single(tokens[0].Issues);
        Assert.Equal(DiagnosticIssueType.ValidatorRejection, tokens[0].Issues[0].Type);
    }

    [Fact]
    public void GivenTransformerFailure_WhenBuilding_ThenTokenHasRejectedOutcomeWithAttempt()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Date: not-a-date");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Date", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssignmentAttempted, tokenName: "Date", value: "not-a-date");
        collector.Record(TokenizationEventType.TransformerFailed, tokenName: "Date",
            decoratorName: "ToDateTimeTransformer", value: "not-a-date", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssignmentFailed, tokenName: "Date", value: "not-a-date");
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Date");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        Assert.Single(tokens);
        Assert.Equal(TokenOutcome.Rejected, tokens[0].Outcome);
        Assert.Single(tokens[0].Attempts);
        Assert.Equal(AttemptOutcome.TransformerFailed, tokens[0].Attempts[0].Outcome);
    }

    [Fact]
    public void GivenMultipleAttemptsOneSuccess_WhenBuilding_ThenMatchedWithMultipleAttempts()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Email: bad\nEmail: good@email.com");
        collector.Record(TokenizationEventType.TokenizationStarted);
        // First attempt — rejected
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Email", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssignmentAttempted, tokenName: "Email", value: "bad");
        collector.Record(TokenizationEventType.ValidatorFailed, tokenName: "Email",
            decoratorName: "IsEmailValidator", value: "bad", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssignmentFailed, tokenName: "Email", value: "bad");
        // Second attempt — accepted
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Email", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssignmentAttempted, tokenName: "Email", value: "good@email.com");
        collector.Record(TokenizationEventType.ValidatorPassed, tokenName: "Email",
            decoratorName: "IsEmailValidator", value: "good@email.com");
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Email",
            value: "good@email.com", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        Assert.Single(tokens);
        Assert.Equal(TokenOutcome.Matched, tokens[0].Outcome);
        Assert.Equal("good@email.com", tokens[0].AssignedValue);
        Assert.Equal(2, tokens[0].Attempts.Count);
        Assert.Equal(AttemptOutcome.ValidatorRejected, tokens[0].Attempts[0].Outcome);
        Assert.Equal(AttemptOutcome.Assigned, tokens[0].Attempts[1].Outcome);
    }

    [Fact]
    public void GivenMixedTokens_WhenBuilding_ThenVerdictReflectsMatchAndMiss()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Name: John");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Name", value: "John");
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Age");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, verdict, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal("Matched 1 of 2 tokens (1 missed).", verdict);
    }

    [Fact]
    public void GivenBacktrackEvent_WhenBuilding_ThenAttemptHasBacktrackedOutcome()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Name: bad\nName: John");
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Name");
        collector.Record(TokenizationEventType.TokenAssignmentAttempted, tokenName: "Name", value: "bad");
        collector.Record(TokenizationEventType.BacktrackStarted, tokenName: "Name", value: "bad");
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Name");
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Name", value: "John");
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        var token = Assert.Single(tokens);
        Assert.Equal(TokenOutcome.Matched, token.Outcome);
        Assert.Equal(2, token.Attempts.Count);
        Assert.Equal(AttemptOutcome.Backtracked, token.Attempts[0].Outcome);
        Assert.Equal(AttemptOutcome.Assigned, token.Attempts[1].Outcome);
    }

    [Fact]
    public void GivenHintMissingWithTokenName_WhenBuilding_ThenIssueAttachedToToken()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("input");
        collector.Record(TokenizationEventType.HintMissing, tokenName: "Name", value: "Expected hint");
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Name");
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        var nameToken = tokens.First(t => string.Equals(t.TokenName, "Name", StringComparison.Ordinal));
        Assert.Contains(nameToken.Issues, i => i.Type == DiagnosticIssueType.HintMissing);
    }

    [Fact]
    public void GivenHintMissing_WhenBuilding_ThenHintMissingIssueCreated()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("no hint");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.HintMissing, value: "Expected text");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        // Hint missing is a global issue, not per-token — it produces a TokenDiagnostic
        // with no token name if there's no token associated
        var hintIssues = tokens.SelectMany(t => t.Issues)
            .Where(i => i.Type == DiagnosticIssueType.HintMissing).ToList();
        Assert.Single(hintIssues);
    }

    [Fact]
    public void GivenValidatorFailedWithNullTokenName_WhenBuilding_ThenDoesNotThrow()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("input");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.ValidatorFailed, tokenName: null,
            decoratorName: "IsEmailValidator", value: "bad");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        Assert.Empty(tokens);
    }

    [Fact]
    public void GivenTransformerFailedWithNullTokenName_WhenBuilding_ThenDoesNotThrow()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("input");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.TransformerFailed, tokenName: null,
            decoratorName: "ToDateTimeTransformer", value: "bad");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        Assert.Empty(tokens);
    }

    [Fact]
    public void GivenRepeatingTokenDisabled_WhenBuilding_ThenRepeatingTokenCutShortIssueCreated()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Items: one\nItems: two");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Items", tokenId: 1, value: "one");
        collector.Record(TokenizationEventType.RepeatingTokenDisabled, tokenName: "Items",
            detail: "Line gap exceeded maximum");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        var items = tokens.First(t => string.Equals(t.TokenName, "Items", StringComparison.Ordinal));
        Assert.Contains(items.Issues, i => i.Type == DiagnosticIssueType.RepeatingTokenCutShort);
    }

    [Fact]
    public void GivenMatchedValueContainsMissedPreamble_WhenBuilding_ThenValueMismatchIssueAdded()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Name: Alice Age: 30");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Name", detail: "Name: ");
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Name", value: "Alice Age: 30");
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Age", detail: "Age: ");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        var nameToken = tokens.First(t => string.Equals(t.TokenName, "Name", StringComparison.Ordinal));
        Assert.Contains(nameToken.Issues, i => i.Type == DiagnosticIssueType.ValueMismatch);
    }

    [Fact]
    public void GivenOutOfOrderTokens_WhenBuilding_ThenNoBlockedAnnotationsApplied()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("nothing", outOfOrderTokens: true);
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "First");
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Second");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        Assert.All(tokens, t => Assert.NotEqual(TokenOutcome.Blocked, t.Outcome));
        Assert.All(tokens, t => Assert.Null(t.BlockedBy));
    }
}
