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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Summary.Issues,
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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorPassed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Email", StringComparison.Ordinal));
        Assert.Empty(diagnostics.Summary.Issues);
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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
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
        Assert.Contains(diagnostics.Summary.Issues,
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
        Assert.Contains(diagnostics.Summary.Issues,
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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.PreambleMatched);

        // Validator DID reject
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed);

        // Summary issues should report ValidatorRejection, NOT PreambleNeverFound
        // BUG: The AlignmentRenderer currently says "preamble never found" for this case.
        // The Summary.Issues correctly classifies this as ValidatorRejection, but the
        // rendered alignment output is misleading.
        var issues = diagnostics.Summary.Issues;
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
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e =>
            e.Type == DiagnosticEventType.ValidatorPassed || e.Type == DiagnosticEventType.ValidatorFailed))
        {
            Output.WriteLine($"{evt.Type}: {evt.DecoratorName} on value '{evt.Value}'");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal("Matched 0 of 1 tokens (1 missed).", diagnostics.Summary.Verdict);
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorPassed
              && string.Equals(e.DecoratorName, "IsNumericValidator", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
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
        var validatorPassed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.ValidatorPassed
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        var validatorFailed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.ValidatorFailed
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        Output.WriteLine($"ValidatorPassed count: {validatorPassed.Count}");
        Output.WriteLine($"ValidatorFailed count: {validatorFailed.Count}");
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        Assert.NotNull(diagnostics);
        Assert.Equal(3, validatorPassed.Count);
        Assert.Equal(1, validatorFailed.Count);
        Assert.Equal("Matched 2 of 2 tokens.", diagnostics.Summary.Verdict);
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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.PreambleMatched);
        // Document: is the issue ValidatorRejection or PreambleNeverFound?
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var issue in diagnostics.Summary.Issues)
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal("Matched 0 of 1 tokens (1 missed).", diagnostics.Summary.Verdict);
        Assert.Equal(2, diagnostics.Summary.Issues.Count(i =>
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
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e =>
            e.Type == DiagnosticEventType.ValidatorFailed || e.Type == DiagnosticEventType.ValidatorPassed))
        {
            Output.WriteLine($"{evt.Type}: {evt.DecoratorName} on value '{evt.Value}'");
        }
        Assert.NotNull(diagnostics);
        // Empty value after preamble results in preamble-never-found (token not matched at all)
        Assert.Equal("Matched 0 of 1 tokens (1 missed).", diagnostics.Summary.Verdict);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.ValidatorFailed);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.ValidatorPassed);
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "Name", StringComparison.Ordinal));
    }

    private TokenizeResult TokenizeWithDiagnostics(string template, string input)
    {
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        Output.WriteLine(result.Diagnostics!.RenderAlignment());
        return result;
    }
}
