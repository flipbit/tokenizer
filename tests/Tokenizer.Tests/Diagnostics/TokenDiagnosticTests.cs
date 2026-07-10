using Tokens.Enumerators;
using Xunit;

namespace Tokens.Diagnostics;

public class TokenDiagnosticTests
{
    [Fact]
    public void GivenMatchedToken_WhenCreated_ThenPropertiesAreAccessible()
    {
        // Arrange & Act
        var diagnostic = new TokenDiagnostic
        {
            TokenName = "Email",
            TokenId = 1,
            Outcome = TokenOutcome.Matched,
            AssignedValue = "user@example.com",
            AssignedLocation = new FileLocation(),
            Attempts = new[]
            {
                new TokenAttempt
                {
                    Value = "user@example.com",
                    Outcome = AttemptOutcome.Assigned,
                    Location = new FileLocation(),
                },
            },
        };

        // Assert
        Assert.Equal("Email", diagnostic.TokenName);
        Assert.Equal(TokenOutcome.Matched, diagnostic.Outcome);
        Assert.Equal("user@example.com", diagnostic.AssignedValue);
        Assert.Single(diagnostic.Attempts);
        Assert.Equal(AttemptOutcome.Assigned, diagnostic.Attempts[0].Outcome);
    }

    [Fact]
    public void GivenRejectedToken_WhenCreated_ThenAttemptsShowRejections()
    {
        // Arrange & Act
        var diagnostic = new TokenDiagnostic
        {
            TokenName = "Email",
            TokenId = 1,
            Outcome = TokenOutcome.Rejected,
            Attempts = new[]
            {
                new TokenAttempt
                {
                    Value = "bad1",
                    Outcome = AttemptOutcome.ValidatorRejected,
                    DecoratorName = "IsEmailValidator",
                    Reason = "Validator 'IsEmailValidator' rejected value 'bad1'.",
                },
                new TokenAttempt
                {
                    Value = "bad2",
                    Outcome = AttemptOutcome.ValidatorRejected,
                    DecoratorName = "IsEmailValidator",
                    Reason = "Validator 'IsEmailValidator' rejected value 'bad2'.",
                },
            },
            Issues = new[]
            {
                new DiagnosticIssue
                {
                    Type = DiagnosticIssueType.ValidatorRejection,
                    TokenName = "Email",
                    Description = "Validator 'IsEmailValidator' rejected value 'bad1'.",
                },
            },
        };

        // Assert
        Assert.Equal(TokenOutcome.Rejected, diagnostic.Outcome);
        Assert.Equal(2, diagnostic.Attempts.Count);
        Assert.All(diagnostic.Attempts, a => Assert.Equal(AttemptOutcome.ValidatorRejected, a.Outcome));
        Assert.Single(diagnostic.Issues);
    }

    [Fact]
    public void GivenNeverFoundToken_WhenCreated_ThenNoAttemptsAndNoAssignedValue()
    {
        // Arrange & Act
        var diagnostic = new TokenDiagnostic
        {
            TokenName = "Missing",
            TokenId = 2,
            Outcome = TokenOutcome.NeverFound,
        };

        // Assert
        Assert.Equal(TokenOutcome.NeverFound, diagnostic.Outcome);
        Assert.Null(diagnostic.AssignedValue);
        Assert.Empty(diagnostic.Attempts);
    }
}
