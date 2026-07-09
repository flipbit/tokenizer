using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class HintTests : TokenizerTestBase
{
    public HintTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenRequiredHint_WhenPresentInInput_ThenHintMatchedAndNormalProcessing()
    {
        // Arrange
        var template = "---\nHint: Invoice\n---\nAmount: { Amount }";
        var input = "Invoice #1234\nAmount: $50.00";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.HintMatched);
        Assert.DoesNotContain(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.HintMissing);
    }

    [Fact]
    public void GivenRequiredHint_WhenMissingFromInput_ThenHintMissingIssue()
    {
        // Arrange
        var template = "---\nHint: Invoice\n---\nAmount: { Amount }";
        var input = "Receipt #1234\nAmount: $50.00";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.HintMissing);
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.HintMissing);
    }

    [Fact]
    public void GivenRequiredHint_WhenCaseDiffers_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "---\nHint: Invoice\n---\nAmount: { Amount }";
        var input = "invoice #1234\nAmount: $50.00";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: is hint matching case-sensitive or case-insensitive?
        var diagnostics = result.Diagnostics!;
        var hintMatched = diagnostics.Events.Any(e => e.Type == DiagnosticEventType.HintMatched);
        var hintMissing = diagnostics.Events.Any(e => e.Type == DiagnosticEventType.HintMissing);
        Output.WriteLine($"HintMatched: {hintMatched}, HintMissing: {hintMissing}");
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        Assert.NotNull(diagnostics);
        Assert.False(hintMatched);
        Assert.True(hintMissing);
        Assert.Equal("Matched 0 of 1 tokens (1 missed).", diagnostics.Summary.Verdict);
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
