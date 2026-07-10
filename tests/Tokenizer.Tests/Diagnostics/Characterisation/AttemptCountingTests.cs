using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class AttemptCountingTests : TokenizerTestBase
{
    public AttemptCountingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenTokenWithValidator_WhenConsideredThreeTimesAndMatchesOnce_ThenThreeAttemptsVisible()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: bad1\nEmail: bad2\nEmail: a@b.com";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var preambleMatches = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.PreambleMatched
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();
        var validatorFailed = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.ValidatorFailed
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();
        var validatorPassed = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.ValidatorPassed
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();
        var assigned = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();

        Output.WriteLine($"PreambleMatched: {preambleMatches.Count}");
        Output.WriteLine($"ValidatorFailed: {validatorFailed.Count}");
        Output.WriteLine($"ValidatorPassed: {validatorPassed.Count}");
        Output.WriteLine($"TokenAssigned: {assigned.Count}");

        // Document the counts — this is what Phase 4 will aggregate into TokenAttempts
        Assert.NotNull(diagnostics);
        Assert.Equal(1, preambleMatches.Count);
        Assert.Equal(2, validatorFailed.Count);
        Assert.Equal(1, validatorPassed.Count);
        Assert.Equal(1, assigned.Count);
    }

    [Fact]
    public void GivenTokenWithValidator_WhenConsideredMultipleTimesAndNeverMatches_ThenAllRejectionsVisible()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: x\nEmail: y\nEmail: z";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var validatorFailed = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.ValidatorFailed
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();
        var tokenMissed = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.TokenMissed
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();

        Output.WriteLine($"ValidatorFailed: {validatorFailed.Count}");
        Output.WriteLine($"TokenMissed: {tokenMissed.Count}");
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");

        Assert.True(tokenMissed.Count >= 1, "Token should be missed");
    }

    [Fact]
    public void GivenMultipleCandidateTokensAtSamePosition_ThenDocumentWhichCandidatesAreTried()
    {
        // Arrange — two tokens with similar preambles competing at same position
        var template = "Name: { FirstName }\nName: { LastName }";
        var input = "Name: Alice\nName: Smith";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise how multiple candidates are handled
        var diagnostics = result.Diagnostics!;
        var attempted = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.TokenAssignmentAttempted)
            .ToList();
        var assigned = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned)
            .ToList();

        Output.WriteLine($"TokenAssignmentAttempted: {attempted.Count}");
        foreach (var evt in attempted)
        {
            Output.WriteLine($"  Attempted: {evt.TokenName} with value '{evt.Value}'");
        }
        Output.WriteLine($"TokenAssigned: {assigned.Count}");
        foreach (var evt in assigned)
        {
            Output.WriteLine($"  Assigned: {evt.TokenName} = '{evt.Value}'");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal(2, attempted.Count);
        Assert.Equal(2, assigned.Count);
        Assert.Contains(assigned, e => string.Equals(e.TokenName, "FirstName", StringComparison.Ordinal));
        Assert.Contains(assigned, e => string.Equals(e.TokenName, "LastName", StringComparison.Ordinal));
        Assert.Equal(2, diagnostics.MatchedCount);
        Assert.Equal(0, diagnostics.MissedCount);
    }

}
