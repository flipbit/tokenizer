using Xunit;

namespace Tokens.Diagnostics.Hints;

public class DateFormatHintGeneratorTests
{
    private readonly DateFormatHintGenerator _generator = new();

    [Fact]
    public void GivenDateWithWrongFormat_WhenGeneratingHint_ThenSuggestsCorrectFormat()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TransformerFailed,
            TokenName = "Registered",
            DecoratorName = "ToDateTimeUtcTransformer",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
            Value = "21/11/2005",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Registered", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("dd/MM/yyyy", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenDateWithTimeAndWrongFormat_WhenGeneratingHint_ThenSuggestsFormatWithTime()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TransformerFailed,
            TokenName = "Registered",
            DecoratorName = "ToDateTimeUtcTransformer",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
            Value = "21/11/2005 15:21:32",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Registered", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("dd/MM/yyyy", hint, StringComparison.Ordinal);
        Assert.Contains("HH:mm:ss", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenNonDateTransformer_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TransformerFailed,
            TokenName = "Name",
            DecoratorName = "ToUpperTransformer",
            Value = "test",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Name", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenUnparseableValue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TransformerFailed,
            TokenName = "Registered",
            DecoratorName = "ToDateTimeUtcTransformer",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
            Value = "not a date at all",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Registered", sourceEvent, context);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenIso8601Date_WhenGeneratingHint_ThenSuggestsIsoFormat()
    {
        // Arrange
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TransformerFailed,
            TokenName = "Created",
            DecoratorName = "ToDateTimeTransformer",
            DecoratorArgs = new[] { "dd/MM/yyyy" },
            Value = "2005-11-21T15:21:32",
        };
        var context = new BuildContext("i", outOfOrderTokens: false, new HashSet<string>(StringComparer.Ordinal));

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.TransformerFailure, "Created", sourceEvent, context);

        // Assert
        Assert.NotNull(hint);
    }
}
