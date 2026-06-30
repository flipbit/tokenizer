using System.Linq;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Tokenization;
using Xunit;
using Tokens.Diagnostics;

namespace Tokens.Tests.Tokenization.Engine;

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
        var parser = new TokenParser();
        var template = parser.Parse("Hello {Name}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var longInput = new string('a', 10000) + "Hello World" + new string('b', 10000);

        // Act
        _engine.ProcessTokenization(template, longInput, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenSpecialCharacters_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Hello {Name}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Hello @#$%^&*()_+-=";

        // Act
        _engine.ProcessTokenization(template, input, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenUnicodeInput_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Hello {Name}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Hello 你好世界 🌍";

        // Act
        _engine.ProcessTokenization(template, input, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }
}
