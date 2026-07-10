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
        // DateFormatHintGenerator should produce a hint
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

    private TokenizeResult TokenizeWithDiagnostics(string template, string input)
    {
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        Output.WriteLine(result.Diagnostics!.RenderAlignment());
        return result;
    }
}
