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
        Assert.Equal("John", tokens[0].AssignedValues[0]);
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
        Assert.Empty(tokens[0].AssignedValues);
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
        Assert.Equal("good@email.com", tokens[0].AssignedValues[0]);
        Assert.Equal(2, tokens[0].Attempts.Count);
        Assert.Equal(AttemptOutcome.ValidatorRejected, tokens[0].Attempts[0].Outcome);
        Assert.Equal(AttemptOutcome.Assigned, tokens[0].Attempts[1].Outcome);
    }

    [Fact]
    public void GivenRepeatingTokenWithThreeMatches_WhenBuilding_ThenAssignedValuesContainsAllInOrder()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Item: A\nItem: B\nItem: C");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Item", tokenId: 1,
            value: "A", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Item", tokenId: 1,
            value: "B", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Item", tokenId: 1,
            value: "C", location: new FileLocation());
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, matched, missed, total) = new TokenDiagnosticBuilder(diagnostics).Build();

        // Assert
        var item = Assert.Single(tokens);
        Assert.Equal(TokenOutcome.Matched, item.Outcome);
        Assert.Equal(3, item.AssignedValues.Count);
        Assert.Equal("A", item.AssignedValues[0]);
        Assert.Equal("B", item.AssignedValues[1]);
        Assert.Equal("C", item.AssignedValues[2]);
        Assert.Equal(3, item.AssignedLocations.Count);
        Assert.Equal(1, matched);  // still 1 unique token
        Assert.Equal(0, missed);
        Assert.Equal(1, total);
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
    public void GivenRepeatingTokenWithMultipleMatches_WhenBuilding_ThenCountsAsOneMatchedToken()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Items: one\nItems: two");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Items", tokenId: 1, value: "one");
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Items", tokenId: 2, value: "two");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var builder = new TokenDiagnosticBuilder(diagnostics);
        var (tokens, _, matched, missed, total) = builder.Build();

        // Assert
        Assert.Equal(1, tokens.Count);
        Assert.Equal(1, matched);
        Assert.Equal(0, missed);
        Assert.Equal(1, total);
        Assert.Equal(total, tokens.Count);
    }

    [Fact]
    public void GivenRepeatingTokenWithZeroMatches_WhenBuilding_ThenCountsAsOneMissedToken()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("nothing");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Items", tokenId: 1);
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Items", tokenId: 2);
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var builder = new TokenDiagnosticBuilder(diagnostics);
        var (tokens, _, matched, missed, total) = builder.Build();

        // Assert
        Assert.Equal(1, tokens.Count);
        Assert.Equal(0, matched);
        Assert.Equal(1, missed);
        Assert.Equal(1, total);
        Assert.Equal(total, tokens.Count);
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

    [Fact]
    public void GivenOptionalTokenBeforeBlocker_WhenBuilding_ThenOptionalIsNotBlocker()
    {
        // Arrange
        var optionalNames = new HashSet<string>(StringComparer.Ordinal) { "Middle" };
        var collector = new TokenizationDiagnosticCollector("input", optionalTokenNames: optionalNames);
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Middle");
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Required");
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Last");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var builder = new TokenDiagnosticBuilder(diagnostics);
        var (tokens, _, _, _, _) = builder.Build();

        // Assert — "Middle" is optional so "Required" is the blocker, "Last" is blocked
        var middle = tokens.First(t => t.TokenName == "Middle");
        var required = tokens.First(t => t.TokenName == "Required");
        var last = tokens.First(t => t.TokenName == "Last");
        Assert.Equal(TokenOutcome.NeverFound, middle.Outcome);
        Assert.Null(middle.BlockedBy);
        Assert.Equal(TokenOutcome.NeverFound, required.Outcome);
        Assert.Null(required.BlockedBy);
        Assert.Equal(TokenOutcome.Blocked, last.Outcome);
        Assert.Equal("Required", last.BlockedBy);
    }

    [Fact]
    public void GivenMatchedValueContainsRejectedTokenPreamble_WhenBuilding_ThenValueMismatchIssueAdded()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Email: notanemail Age: 30");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Email", detail: "Email: ");
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Email", value: "notanemail Age: 30");
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Age", detail: "Age: ");
        collector.Record(TokenizationEventType.ValidatorFailed, tokenName: "Age", decoratorName: "IsNumericValidator", value: "30");
        collector.Record(TokenizationEventType.TokenMissed, tokenName: "Age", detail: "Age: ");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var builder = new TokenDiagnosticBuilder(diagnostics);
        var (tokens, _, _, _, _) = builder.Build();

        // Assert
        var email = tokens.First(t => t.TokenName == "Email");
        Assert.Contains(email.Issues, i => i.Type == DiagnosticIssueType.ValueMismatch);
    }

    [Fact]
    public void GivenTokenWithOnlyPreambleMatch_WhenBuilding_ThenTokenNotIncludedInResult()
    {
        // Arrange
        var collector = new TokenizationDiagnosticCollector("Name: John");
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "Name", detail: "Name: ");
        // No TokenAssigned or TokenMissed — token has no terminal state
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var builder = new TokenDiagnosticBuilder(diagnostics);
        var (tokens, _, _, _, _) = builder.Build();

        // Assert
        Assert.Empty(tokens);
    }
}
