using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Xunit;

#pragma warning disable MA0048 // Scenario test: TokenizationEngine.Basic.Tests.cs
namespace Tokens.Tokenization;

/// <summary>
/// Tests for basic/happy path TokenizationEngine scenarios
/// </summary>
public class TokenizationEngineBasicTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenValidInput_WhenProcessingTokenization_ThenProcessesSuccessfully()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("First Name: {FirstName}").Template;

        var context = new TokenizationContext();
        var input = "First Name: Alice";
        context.Initialize(new System.IO.StringReader(input));

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var session = _engine.CreateSession(template, result, NullTokenizationDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.True(result.Tokens.Matches.Count > 0);
    }

    [Fact]
    public void GivenTemplateWithNoTokens_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Hello World").Template; // Template with no tokens

        var context = new TokenizationContext();
        var input = "Hello World";
        context.Initialize(new System.IO.StringReader(input));

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var session = _engine.CreateSession(template, result, NullTokenizationDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Tokens.Matches);
    }

}
