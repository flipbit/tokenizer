using Tokens.Diagnostics;
using Tokens.Diagnostics.Hints;
using Xunit;

namespace Tokens.Tests.Diagnostics.Hints;

public class DateFormatHintGeneratorTests
{
    private readonly DateFormatHintGenerator _generator = new();

    [Fact]
    public void GivenDateWithWrongFormat_WhenGeneratingHint_ThenSuggestsCorrectFormat()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Registered"
        };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Registered",
            DecoratorName = "ToDateTimeUtcTransformer",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
            Value = "21/11/2005"
        };
        var trace = new DiagnosticCollector("t", "i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("dd/MM/yyyy", hint);
    }

    [Fact]
    public void GivenDateWithTimeAndWrongFormat_WhenGeneratingHint_ThenSuggestsFormatWithTime()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Registered"
        };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Registered",
            DecoratorName = "ToDateTimeUtcTransformer",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
            Value = "21/11/2005 15:21:32"
        };
        var trace = new DiagnosticCollector("t", "i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("dd/MM/yyyy", hint);
        Assert.Contains("HH:mm:ss", hint);
    }

    [Fact]
    public void GivenNonDateTransformer_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Name"
        };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Name",
            DecoratorName = "ToUpperTransformer",
            Value = "test"
        };
        var trace = new DiagnosticCollector("t", "i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenUnparseableValue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Registered"
        };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Registered",
            DecoratorName = "ToDateTimeUtcTransformer",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
            Value = "not a date at all"
        };
        var trace = new DiagnosticCollector("t", "i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenIso8601Date_WhenGeneratingHint_ThenSuggestsIsoFormat()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Created"
        };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Created",
            DecoratorName = "ToDateTimeTransformer",
            DecoratorArgs = new[] { "dd/MM/yyyy" },
            Value = "2005-11-21T15:21:32"
        };
        var trace = new DiagnosticCollector("t", "i").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
    }
}
