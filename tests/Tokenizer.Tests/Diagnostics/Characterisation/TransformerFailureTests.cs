using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class TransformerFailureTests : TokenizerTestBase
{
    public TransformerFailureTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenToDateTimeTransformer_WhenFormatIsWrong_ThenTransformerFailureIssue()
    {
        // Arrange
        var template = "Date: { Date : ToDateTime('yyyy-MM-dd') }";
        var input = "Date: 13/01/2024";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TransformerFailed);
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
            i => i.Type == DiagnosticIssueType.TransformerFailure
              && string.Equals(i.TokenName, "Date", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenToDateTimeTransformer_WhenFormatIsCorrect_ThenTokenMatchedNoIssues()
    {
        // Arrange
        var template = "Date: { Date : ToDateTime('yyyy-MM-dd') }";
        var input = "Date: 2024-01-13";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TransformerSucceeded);
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Date", StringComparison.Ordinal));
        Assert.Empty(diagnostics.Tokens.SelectMany(t => t.Issues));
    }

    [Fact]
    public void GivenToDateTimeTransformer_WhenFormatDiffers_ThenHintSuggestsMatchingFormat()
    {
        // Arrange
        var template = "Date: { Date : ToDateTime('yyyy-MM-dd') }";
        var input = "Date: 01/13/2024";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var issue = diagnostics.Tokens.SelectMany(t => t.Issues)
            .FirstOrDefault(i => i.Type == DiagnosticIssueType.TransformerFailure
                              && string.Equals(i.TokenName, "Date", StringComparison.Ordinal));
        Assert.NotNull(issue);
        Assert.Equal("TK003", issue!.Code);
        // DateFormatHintGenerator should produce a hint containing the detected format
        Assert.NotNull(issue!.Hint);
        Assert.Contains("MM/dd/yyyy", issue!.Hint, StringComparison.Ordinal);
        Output.WriteLine($"Hint: {issue!.Hint ?? "(none)"}");
    }

    [Fact]
    public void GivenTransformerFails_WhenPreambleWasFound_ThenIssueIsTransformerFailureNotPreambleNeverFound()
    {
        // Arrange
        var template = "Date: { Date : ToDateTime('yyyy-MM-dd') }";
        var input = "Date: not-a-date";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;

        // Preamble WAS found
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.PreambleMatched);

        // Transformer DID fail
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TransformerFailed);

        var issues = diagnostics.Tokens.SelectMany(t => t.Issues);
        Assert.Contains(issues, i => i.Type == DiagnosticIssueType.TransformerFailure);
        Assert.DoesNotContain(issues, i => i.Type == DiagnosticIssueType.PreambleNeverFound);
    }

    [Fact]
    public void GivenChainedTransformerAndValidator_WhenTransformerSucceedsAndValidatorFails_ThenValidatorRejection()
    {
        // Arrange — ToUpper succeeds, IsEmail rejects the uppercased value
        var template = "Val: { Val : ToUpper, IsEmail }";
        var input = "Val: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TransformerSucceeded
              && string.Equals(e.DecoratorName, "ToUpperTransformer", StringComparison.Ordinal));
        // The failing decorator should be the validator, not the transformer
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenChainedTransformers_WhenSecondFails_ThenTransformerFailureForSecond()
    {
        // Arrange — ToUpper succeeds, ToDateTime fails on the uppercased text
        var template = "Val: { Val : ToUpper, ToDateTime('yyyy-MM-dd') }";
        var input = "Val: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TransformerSucceeded
              && string.Equals(e.DecoratorName, "ToUpperTransformer", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TransformerFailed);
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
    }

    [Fact]
    public void GivenChainedTransformers_WhenFirstFails_ThenSecondNeverReached()
    {
        // Arrange
        var template = "Val: { Val : ToDateTime('yyyy-MM-dd'), ToUpper }";
        var input = "Val: bad";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents, e => e.Type == DiagnosticEventType.TransformerFailed);
        Assert.DoesNotContain(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TransformerSucceeded
              && string.Equals(e.DecoratorName, "ToUpperTransformer", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenChainedTransformerAndValidator_WhenValidatorFails_ThenChainedDecoratorHintGenerated()
    {
        // Arrange — ToUpper succeeds, IsEmail rejects the uppercased value
        var template = "Val: { Val : ToUpper, IsEmail }";
        var input = "Val: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var issues = diagnostics.Tokens.SelectMany(t => t.Issues)
            .Where(i => i.Type == DiagnosticIssueType.ValidatorRejection)
            .ToList();
        Assert.NotEmpty(issues);
        // ChainedDecoratorHintGenerator fires because ToUpper succeeded before IsEmail failed
        var chainHint = issues.FirstOrDefault(i => i.Hint != null
            && i.Hint.IndexOf("ToUpperTransformer", StringComparison.Ordinal) >= 0);
        Assert.NotNull(chainHint);
        Assert.True(chainHint!.Hint!.IndexOf("IsEmailValidator", StringComparison.Ordinal) >= 0);
    }

}
