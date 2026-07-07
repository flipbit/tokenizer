using Xunit;

namespace Tokens.Diagnostics.Hints;

public class UnmatchedInputHintGeneratorTests
{
    [Fact]
    public void GivenAnyInput_WhenTryGenerateHint_ThenReturnsNull()
    {
        // Arrange
        var generator = new UnmatchedInputHintGenerator();
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.UnmatchedInputSection, TokenName = "Test" };
        var sourceEvent = new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Test" };
        var result = new DiagnosticResult(inputContent: "some input text");

        // Act
        var hint = generator.TryGenerateHint(issue, sourceEvent, result);

        // Assert
        Assert.Null(hint);
    }
}
