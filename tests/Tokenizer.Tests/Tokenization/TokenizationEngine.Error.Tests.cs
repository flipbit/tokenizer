using Tokens.Builders;
using Tokens.Diagnostics;
using Xunit;

#pragma warning disable MA0048 // Scenario test: TokenizationEngine.Error.Tests.cs
namespace Tokens.Tokenization;

/// <summary>
/// Tests for TokenizationEngine error handling and validation
/// </summary>
public class TokenizationEngineErrorTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenNullReader_WhenInitializingContext_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.Initialize((System.IO.TextReader)null!));
    }

    [Fact]
    public void GivenNullTemplate_WhenCreatingSession_ThenThrowsException()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.CreateSession(null!, result, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenNullResult_WhenCreatingSession_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.CreateSession(template, null!, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenTokenizationWithNoMatch_WhenProcessingTokenization_ThenResultHasNoExceptionsAndFails()
    {
        // Arrange — template expects a required token whose preamble won't be found in input
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithName("TestToken")
                .WithPreamble("PREAMBLE_NOT_IN_INPUT:")
                .WithRequired()
                .Build())
            .Build();

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("no match here"));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var session = _engine.CreateSession(template, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Empty(result.Exceptions);
        Assert.False(result.Success);
    }
}
