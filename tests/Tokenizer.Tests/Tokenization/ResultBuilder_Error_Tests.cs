using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization;

/// <summary>
/// Tests for ResultBuilder error handling and validation
/// </summary>
public class ResultBuilder_Error_Tests
{
    private readonly ResultBuilder _builder = new();

    [Fact]
    public void GivenNullResult_WhenAddTokenMatch_ThenThrowsException()
    {
        // Arrange
        var token = new TokenBuilder().Build();
        var assignedValue = "TestValue";
        var location = new FileLocation();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _builder.AddTokenMatch(token, assignedValue, location, null!));
    }

    [Fact]
    public void GivenNullResult_WhenAddTokenMiss_ThenThrowsException()
    {
        // Arrange
        var token = new TokenBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _builder.AddTokenMiss(token, null!));
    }

    [Fact]
    public void GivenNullResult_WhenAddException_ThenThrowsException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _builder.AddException(exception, null!));
    }

    [Fact]
    public void GivenNullResult_WhenBuildUnmatchedTokens_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _builder.BuildUnmatchedTokens(template, null!, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenNullTemplate_WhenBuildUnmatchedTokens_ThenThrowsException()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _builder.BuildUnmatchedTokens(null!, result, NullDiagnosticCollector.Instance));
    }

}
