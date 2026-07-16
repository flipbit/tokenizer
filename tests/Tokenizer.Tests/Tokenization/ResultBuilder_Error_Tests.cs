using Tokens.Builders;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Tokenization;

/// <summary>
/// Tests for ResultBuilder error handling and validation
/// </summary>
public class ResultBuilder_Error_Tests
{
    private readonly ResultBuilder _builder = new();

    [Fact]
    public void GivenNullResult_WhenBuildUnmatchedTokens_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _builder.BuildUnmatchedTokens(template, null!, NullTokenizationDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenNullTemplate_WhenBuildUnmatchedTokens_ThenThrowsException()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _builder.BuildUnmatchedTokens(null!, result, NullTokenizationDiagnosticCollector.Instance));
    }
}
