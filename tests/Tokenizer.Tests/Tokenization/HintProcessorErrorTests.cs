using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization;

/// <summary>
/// Tests for HintProcessor error handling and validation
/// </summary>
public class HintProcessorErrorTests
{
    private readonly HintProcessor _processor = new();

    [Fact]
    public void GivenNullTemplate_WhenFindAndValidateHints_ThenThrowsException()
    {
        // Arrange
        var enumerator = new TokenEnumerator("test");
        var result = new TokenizeResultBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _processor.FindAndValidateHints(null!, enumerator, result, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenNullEnumerator_WhenFindAndValidateHints_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _processor.FindAndValidateHints(template, null!, result, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenNullResult_WhenFindAndValidateHints_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();
        var enumerator = new TokenEnumerator("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _processor.FindAndValidateHints(template, enumerator, null!, NullDiagnosticCollector.Instance));
    }
}
