using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class ValidatorRejectionTests : TokenizerTestBase
{
    public ValidatorRejectionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenIsEmailValidator_WhenValueIsInvalid_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: notanemail";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.ValidatorRejection
              && string.Equals(i.TokenName, "Email", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenIsEmailValidator_WhenValueIsValid_ThenTokenMatchedNoIssues()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: user@example.com";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.ValidatorPassed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenAssigned
              && string.Equals(e.TokenName, "Email", StringComparison.Ordinal));
        Assert.Empty(diagnostics.Tokens.SelectMany(t => t.Issues));
    }

    [Fact]
    public void GivenIsNumericValidator_WhenValueIsText_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Count: { Count : IsNumeric }";
        var input = "Count: twelve";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsNumericValidator", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenIsPhoneNumberValidator_WhenValueIsGibberish_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Phone: { Phone : IsPhoneNumber }";
        var input = "Phone: abc";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.ValidatorRejection
              && string.Equals(i.TokenName, "Phone", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenIsDomainNameValidator_WhenValueIsInvalid_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Host: { Host : IsDomainName }";
        var input = "Host: not a domain";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.ValidatorRejection
              && string.Equals(i.TokenName, "Host", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenValidatorRejectsValue_WhenPreambleWasFound_ThenIssueIsValidatorRejectionNotPreambleNeverFound()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: bad";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;

        // Preamble WAS found
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.PreambleMatched);

        // Validator DID reject
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.ValidatorFailed);

        // Summary issues should report ValidatorRejection, NOT PreambleNeverFound
        var issues = diagnostics.Tokens.SelectMany(t => t.Issues);
        Assert.Contains(issues, i => i.Type == DiagnosticIssueType.ValidatorRejection);
        Assert.DoesNotContain(issues, i => i.Type == DiagnosticIssueType.PreambleNeverFound);
    }

    [Fact]
    public void GivenMultipleValidators_WhenFirstPassesAndSecondRejects_ThenValidatorFailedForSecond()
    {
        // Arrange — IsNumeric passes on "123", IsEmail rejects it
        var template = "Val: { Val : IsNumeric, IsEmail }";
        var input = "Val: 123";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        // Document: does IsNumeric pass then IsEmail fail? Or does the engine short-circuit?
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        foreach (var evt in diagnostics.RawEvents.Where(e =>
            e.Type == TokenizationEventType.ValidatorPassed || e.Type == TokenizationEventType.ValidatorFailed))
        {
            Output.WriteLine($"{evt.Type}: {evt.DecoratorName} on value '{evt.Value}'");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal(0, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.ValidatorPassed
              && string.Equals(e.DecoratorName, "IsNumericValidator", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenRepeatingTokenWithValidator_WhenSomeOccurrencesRejected_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "Item: { Item : Repeating, IsNumeric }";
        var input = "Item: 1\nItem: two\nItem: 3";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: does repeating stop at first failure or continue?
        var diagnostics = result.Diagnostics!;
        var validatorPassed = diagnostics.RawEvents
            .Where(e => e.Type == TokenizationEventType.ValidatorPassed
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        var validatorFailed = diagnostics.RawEvents
            .Where(e => e.Type == TokenizationEventType.ValidatorFailed
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        Output.WriteLine($"ValidatorPassed count: {validatorPassed.Count}");
        Output.WriteLine($"ValidatorFailed count: {validatorFailed.Count}");
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        Assert.NotNull(diagnostics);
        Assert.Equal(3, validatorPassed.Count);
        Assert.Equal(1, validatorFailed.Count);
        Assert.Equal(2, diagnostics.MatchedCount);
        Assert.Equal(0, diagnostics.MissedCount);
    }

    [Fact]
    public void GivenValidator_WhenEveryOccurrenceRejected_ThenTokenMissedWithValidatorRejection()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: bad1\nEmail: bad2";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        // Preamble was found (at least once), so this should NOT be PreambleNeverFound
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.PreambleMatched);
        // Document: is the issue ValidatorRejection or PreambleNeverFound?
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        foreach (var issue in diagnostics.Tokens.SelectMany(t => t.Issues))
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal(0, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
        Assert.Equal(2, diagnostics.Tokens.SelectMany(t => t.Issues).Count(i =>
            i.Type == DiagnosticIssueType.ValidatorRejection
            && string.Equals(i.TokenName, "Email", StringComparison.Ordinal)));
    }

    [Fact]
    public void GivenValidator_WhenValueIsEmpty_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Name: { Name : IsEmail }";
        var input = "Name: ";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        foreach (var evt in diagnostics.RawEvents.Where(e =>
            e.Type == TokenizationEventType.ValidatorFailed || e.Type == TokenizationEventType.ValidatorPassed))
        {
            Output.WriteLine($"{evt.Type}: {evt.DecoratorName} on value '{evt.Value}'");
        }
        Assert.NotNull(diagnostics);
        // Empty value after preamble results in preamble-never-found (token not matched at all)
        Assert.Equal(0, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == TokenizationEventType.ValidatorFailed);
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == TokenizationEventType.ValidatorPassed);
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenValidator_WhenMultipleOccurrencesRejected_ThenMultipleRejectionHintGenerated()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: bad1\nEmail: bad2\nEmail: bad3";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var issues = diagnostics.Tokens.SelectMany(t => t.Issues)
            .Where(i => i.Type == DiagnosticIssueType.ValidatorRejection)
            .ToList();
        // The last rejection should have the multiple-rejection summary hint
        var summaryHint = issues.LastOrDefault(i => i.Hint != null
            && i.Hint.IndexOf("rejected", StringComparison.OrdinalIgnoreCase) >= 0);
        Assert.NotNull(summaryHint);
        Assert.True(summaryHint!.Hint!.IndexOf("3 times", StringComparison.Ordinal) >= 0);
    }

    [Fact]
    public void GivenMultipleValidators_WhenFirstFails_ThenEngineShortCircuits_OnlyFirstRejectionRecorded()
    {
        // Arrange — "hello" fails IsNumeric; engine short-circuits before IsEmail
        var template = "Val: { Val : IsNumeric, IsEmail }";
        var input = "Val: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Equal(0, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsNumericValidator", StringComparison.Ordinal));
    }

}
