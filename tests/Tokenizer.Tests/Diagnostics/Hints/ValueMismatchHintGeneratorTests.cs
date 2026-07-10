using Xunit;

namespace Tokens.Diagnostics.Hints;

public class ValueMismatchHintGeneratorTests
{
    private readonly ValueMismatchHintGenerator _generator = new();

    [Fact]
    public void GivenValueMismatchIssue_WhenGeneratingHint_ThenSuggestsEndDelimiter()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValueMismatch, TokenName = "Description" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenAssigned,
            TokenName = "Description",
            Value = "some greedy value",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("end delimiter", hint, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenNonValueMismatchIssue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.ValidatorRejection, TokenName = "Description" };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.ValidatorFailed,
            TokenName = "Description",
            DecoratorName = "IsEmailValidator",
            Value = "test",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent, trace);

        // Assert
        Assert.Null(hint);
    }
}
