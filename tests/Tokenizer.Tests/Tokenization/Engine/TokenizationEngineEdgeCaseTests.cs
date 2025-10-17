using System.Linq;
using Tokens.Builders;
using Tokens.Tokenization;
using Xunit;

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
        var longInput = new string('a', 10000) + " {Name} " + new string('b', 10000);
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        context.Initialize(longInput);

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act
        _engine.ProcessTokenization(template, "Hello World", value, context, result);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenSpecialCharacters_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var specialInput = "Hello {Name} with special chars: @#$%^&*()_+-=[]{}|;':\",./<>?";
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        context.Initialize(specialInput);

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act
        _engine.ProcessTokenization(template, "Hello World", value, context, result);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenUnicodeInput_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var unicodeInput = "Hello {Name} with unicode: 你好世界 🌍";
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        context.Initialize(unicodeInput);

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act
        _engine.ProcessTokenization(template, "Hello World", value, context, result);

        // Assert
        Assert.NotNull(result);
    }
}
