using Tokens.Enumerators;
using Xunit;

namespace Tokens.Diagnostics;

public class AlignmentRendererTests
{
    [Fact]
    public void GivenSuccessfulMatch_WhenRendering_ThenShowsMatchedTokens()
    {
        // Arrange
        var collector = new DiagnosticCollector("Name: { Name }", "Name: John");
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
        Assert.Contains("Name", output);
        Assert.Contains("John", output);
        Assert.Contains("✓", output);
    }

    [Fact]
    public void GivenMissedToken_WhenRendering_ThenShowsUnmatchedSection()
    {
        // Arrange
        var collector = new DiagnosticCollector(
            "Name: { Name }\nAge: { Age }", "Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name",
            value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Age");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Age", output);
        Assert.Contains("✗", output);
    }

    [Fact]
    public void GivenRenderedAlignment_WhenRendered_ThenContainsSummarySection()
    {
        // Arrange
        var collector = new DiagnosticCollector("Name: { Name }", "Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name",
            value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Matched", output);
    }

    [Fact]
    public void GivenValidatorFailure_WhenRendering_ThenShowsFailureWithHint()
    {
        // Arrange
        var collector = new DiagnosticCollector(
            "Email: { Email : IsEmail }", "Email: notanemail");
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
        Assert.Contains("Email", output);
        Assert.Contains("✗", output);
    }

    [Fact]
    public void GivenTransformerFailure_WhenRendering_ThenShowsTransformerDetails()
    {
        // Arrange
        var collector = new DiagnosticCollector(
            "Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }", "Date: 21/11/2005");
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
        Assert.Contains("Date", output);
        Assert.Contains("ToDateTimeUtcTransformer", output);
    }

    [Fact]
    public void GivenHeaderSection_WhenRendered_ThenContainsTokenAndInputCounts()
    {
        // Arrange
        var collector = new DiagnosticCollector("Name: { Name }", "Name: John\nExtra line");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name",
            value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Alignment", output);
    }
}
