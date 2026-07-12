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
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.HintMatched);
        Assert.DoesNotContain(diagnostics.Tokens.SelectMany(t => t.Issues),
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
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.HintMissing);
        Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
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
        var hintMatched = diagnostics.RawEvents.Any(e => e.Type == TokenizationEventType.HintMatched);
        var hintMissing = diagnostics.RawEvents.Any(e => e.Type == TokenizationEventType.HintMissing);
        Output.WriteLine($"HintMatched: {hintMatched}, HintMissing: {hintMissing}");
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        Assert.NotNull(diagnostics);
        Assert.False(hintMatched);
        Assert.True(hintMissing);
        Assert.Equal(0, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
    }

}
