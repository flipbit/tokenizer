using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Xunit;

#pragma warning disable MA0048 // Scenario test: TokenizationEngine.EdgeCase.Tests.cs
namespace Tokens.Tokenization;

/// <summary>
/// Tests for TokenizationEngine edge cases (unicode, special chars, long input, etc.)
/// </summary>
public class TokenizationEngineEdgeCaseTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenVeryLongInput_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Hello {Name}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var longInput = new string('a', 10000) + "Hello World" + new string('b', 10000);
        context.Initialize(new System.IO.StringReader(longInput));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenSpecialCharacters_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Hello {Name}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Hello @#$%^&*()_+-=";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenUnicodeInput_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Hello {Name}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Hello 你好世界 🌍";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }
}
