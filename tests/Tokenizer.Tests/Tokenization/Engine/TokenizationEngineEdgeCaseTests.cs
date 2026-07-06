using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Tokenization.Engine;

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
        var parser = new TemplateCompiler();
        var template = parser.Parse("Hello {Name}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var longInput = new string('a', 10000) + "Hello World" + new string('b', 10000);
        context.Initialize(new System.IO.StringReader(longInput));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenSpecialCharacters_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler();
        var template = parser.Parse("Hello {Name}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Hello @#$%^&*()_+-=";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenUnicodeInput_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler();
        var template = parser.Parse("Hello {Name}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Hello 你好世界 🌍";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }
}
