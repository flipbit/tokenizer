using Tokens.Enumerators;
using Xunit;

namespace Tokens.Diagnostics;

public class TokenDiagnosticBuilderTests
{
    [Fact]
    public void GivenSingleMatchedToken_WhenBuilding_ThenTokenHasMatchedOutcome()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Name", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Name", value: "John");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name", value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

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
        var collector = new RuntimeDiagnosticCollector("nothing");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Name");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

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
        var collector = new RuntimeDiagnosticCollector("Email: bad");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Email", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Email", value: "bad");
        collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: "Email",
            decoratorName: "IsEmailValidator", value: "bad", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentFailed, tokenName: "Email", value: "bad");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Email");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

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
        var collector = new RuntimeDiagnosticCollector("Date: not-a-date");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Date", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Date", value: "not-a-date");
        collector.Record(DiagnosticEventType.TransformerFailed, tokenName: "Date",
            decoratorName: "ToDateTimeTransformer", value: "not-a-date", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentFailed, tokenName: "Date", value: "not-a-date");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Date");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

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
        var collector = new RuntimeDiagnosticCollector("Email: bad\nEmail: good@email.com");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        // First attempt — rejected
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Email", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Email", value: "bad");
        collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: "Email",
            decoratorName: "IsEmailValidator", value: "bad", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentFailed, tokenName: "Email", value: "bad");
        // Second attempt — accepted
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Email", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Email", value: "good@email.com");
        collector.Record(DiagnosticEventType.ValidatorPassed, tokenName: "Email",
            decoratorName: "IsEmailValidator", value: "good@email.com");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Email",
            value: "good@email.com", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

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
        var collector = new RuntimeDiagnosticCollector("Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name", value: "John");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Age");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, verdict, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal("Matched 1 of 2 tokens (1 missed).", verdict);
    }

    [Fact]
    public void GivenBacktrackEvent_WhenBuilding_ThenAttemptHasBacktrackedOutcome()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("Name: bad\nName: John");
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Name");
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Name", value: "bad");
        collector.Record(DiagnosticEventType.BacktrackStarted, tokenName: "Name", value: "bad");
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Name");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name", value: "John");
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

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
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.HintMissing, tokenName: "Name", value: "Expected hint");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Name");
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        var nameToken = tokens.First(t => string.Equals(t.TokenName, "Name", StringComparison.Ordinal));
        Assert.Contains(nameToken.Issues, i => i.Type == DiagnosticIssueType.HintMissing);
    }

    [Fact]
    public void GivenHintMissing_WhenBuilding_ThenHintMissingIssueCreated()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("no hint");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.HintMissing, value: "Expected text");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

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
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: null,
            decoratorName: "IsEmailValidator", value: "bad");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        Assert.Empty(tokens);
    }

    [Fact]
    public void GivenTransformerFailedWithNullTokenName_WhenBuilding_ThenDoesNotThrow()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TransformerFailed, tokenName: null,
            decoratorName: "ToDateTimeTransformer", value: "bad");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        Assert.Empty(tokens);
    }
}
