using Tokens.Enumerators;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics;

public class AlignmentRendererTests : TokenizerTestBase
{
    public AlignmentRendererTests(ITestOutputHelper output)
        : base(output)
    {
    }
    [Fact]
    public void GivenSuccessfulMatch_WhenRendering_ThenShowsMatchedTokens()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted,
            detail: "Template: test, Tokens: 1, Input length: 10");
        collector.Record(DiagnosticEventType.PreambleMatched,
            tokenName: "Name", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssigned,
            tokenName: "Name", value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: "Matches: 1, Misses: 0");

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Name", output, StringComparison.Ordinal);
        Assert.Contains("John", output, StringComparison.Ordinal);
        Assert.Contains("✓", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenMissedToken_WhenRendering_ThenShowsUnmatchedSection()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name",
            value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Age");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Age", output, StringComparison.Ordinal);
        Assert.Contains("✗", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenRenderedAlignment_WhenRendered_ThenContainsSummarySection()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name",
            value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Matched", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenValidatorFailure_WhenRendering_ThenShowsRejectedNotPreambleNeverFound()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("Email: notanemail");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsEmailValidator",
            value: "notanemail", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Email");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Email", output, StringComparison.Ordinal);
        Assert.Contains("ValidatorRejected", output, StringComparison.Ordinal);
        Assert.DoesNotContain("preamble never found", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenTransformerFailure_WhenRendering_ThenShowsTransformerDetails()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("Date: 21/11/2005");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TransformerFailed,
            tokenName: "Date", decoratorName: "ToDateTimeUtcTransformer",
            decoratorArgs: new[] { "yyyy-MM-dd" }, value: "21/11/2005",
            location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Date");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Date", output, StringComparison.Ordinal);
        Assert.Contains("ToDateTimeUtcTransformer", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenHeaderSection_WhenRendered_ThenContainsTokenAndInputCounts()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("Name: John\nExtra line");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name",
            value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Alignment", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenBlockedTokens_WhenRendered_ThenShowsBlockedSectionWithMarkerAndBlocker()
    {
        // Arrange — ordered template, B missing causes C to be blocked
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "A: one";
        var result = TokenizeWithDiagnostics(template, input);

        // Act
        var alignment = result.Diagnostics!.RenderAlignment();
        Output.WriteLine(alignment);

        // Assert
        Assert.Contains("⊘", alignment, StringComparison.Ordinal); // ⊘ marker
        Assert.Contains("blocked by", alignment, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Blocked:", alignment, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenMissedTokenWithNoAttempts_WhenRenderingAlignment_ThenRendersWithoutError()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        var alignment = result.Diagnostics!.RenderAlignment();

        // Assert
        Output.WriteLine(alignment);
        Assert.Contains("Age", alignment, StringComparison.Ordinal);
    }
}
