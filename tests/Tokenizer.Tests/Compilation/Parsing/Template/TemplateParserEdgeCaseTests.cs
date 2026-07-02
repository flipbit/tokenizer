using System.Text;
using Xunit;

namespace Tokens.Compilation.Parsing.Template;

/// <summary>
/// Tests for edge cases and boundary conditions
/// </summary>
public class TemplateParserEdgeCaseTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenVeryLongPreamble_WhenParsing_ThenHandlesCorrectly()
    {
        // Arrange
        var longPreamble = new string('x', 10000);
        var template = $"{longPreamble}{{name}}";

        // Act
        var result = _parser.Parse(template);

        // Assert
        Assert.Single(result.Tokens);
        Assert.Equal(longPreamble, result.Tokens[0].Preamble);
    }

    [Fact]
    public void GivenVeryLongTokenName_WhenParsing_ThenHandlesCorrectly()
    {
        // Arrange
        var longName = new string('a', 1000);
        var template = $"{{{longName}}}";

        // Act
        var result = _parser.Parse(template);

        // Assert
        Assert.Single(result.Tokens);
        Assert.Equal(longName, result.Tokens[0].Name);
    }

    [Fact]
    public void GivenVeryLongDecoratorChain_WhenParsing_ThenHandlesCorrectly()
    {
        // Arrange
        var decorators = string.Join(",", Enumerable.Range(1, 50).Select(i => $"dec{i}"));
        var template = $"{{name:{decorators}}}";

        // Act
        var result = _parser.Parse(template);

        // Assert
        Assert.Single(result.Tokens);
        Assert.Equal(50, result.Tokens[0].Decorators.Count);
    }

    [Fact]
    public void GivenUnicodeInPreamble_WhenParsing_ThenPreservesUnicode()
    {
        // Arrange
        var template = "Hello 世界 🌍 Ñoño{name}";

        // Act
        var result = _parser.Parse(template);

        // Assert
        Assert.Single(result.Tokens);
        Assert.Contains("世界", result.Tokens[0].Preamble);
        Assert.Contains("🌍", result.Tokens[0].Preamble);
        Assert.Contains("Ñoño", result.Tokens[0].Preamble);
    }

    [Fact]
    public void GivenEmojisInContent_WhenParsing_ThenPreservesEmojis()
    {
        // Arrange
        var template = "🎉{name}🎊";

        // Act
        var result = _parser.Parse(template);

        // Assert
        Assert.Equal(2, result.Tokens.Count);
        Assert.Contains("🎉", result.Tokens[0].Preamble);
        Assert.Contains("🎊", result.Tokens[1].Preamble);
    }

    [Fact]
    public void GivenSingleCharacterTokenName_WhenParsing_ThenAccepts()
    {
        // Arrange & Act
        var result = _parser.Parse("{x}");

        // Assert
        Assert.Single(result.Tokens);
        Assert.Equal("x", result.Tokens[0].Name);
    }

    [Fact]
    public void GivenConsecutiveTokens_WhenParsing_ThenHandlesCorrectly()
    {
        // Arrange & Act
        var result = _parser.Parse("{a}{b}{c}");

        // Assert
        Assert.Equal(3, result.Tokens.Count);
        Assert.Equal("a", result.Tokens[0].Name);
        Assert.Equal("b", result.Tokens[1].Name);
        Assert.Equal("c", result.Tokens[2].Name);
    }

    [Fact]
    public void GivenTokenAtVeryEndOfInput_WhenParsing_ThenHandlesCorrectly()
    {
        // Arrange & Act
        var result = _parser.Parse("start{end}");

        // Assert
        Assert.Single(result.Tokens);
        Assert.Equal("end", result.Tokens[0].Name);
        Assert.Equal("start", result.Tokens[0].Preamble);
    }

    [Fact]
    public void GivenManyTokens_WhenParsing_ThenHandlesAll()
    {
        // Arrange
        var sb = new StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            sb.Append($"{{token{i}}}");
        }

        // Act
        var result = _parser.Parse(sb.ToString());

        // Assert
        Assert.Equal(100, result.Tokens.Count);
        Assert.Equal("token0", result.Tokens[0].Name);
        Assert.Equal("token99", result.Tokens[99].Name);
    }

    [Fact]
    public void GivenTabCharactersInPreamble_WhenParsing_ThenHandlesWhitespace()
    {
        // Arrange & Act
        var result = _parser.Parse("Start\t\tIndented{name}");

        // Assert
        Assert.Single(result.Tokens);
        Assert.Contains("Start", result.Tokens[0].Preamble);
        Assert.Contains("Indented", result.Tokens[0].Preamble);
    }

    [Fact]
    public void GivenMultipleSpacesInPreamble_WhenParsing_ThenPreservesSpaces()
    {
        // Arrange & Act
        var result = _parser.Parse("Hello     World{name}");

        // Assert
        Assert.Single(result.Tokens);
        Assert.Equal("Hello     World", result.Tokens[0].Preamble);
    }

    [Fact]
    public void GivenTokenWithNumericName_WhenParsing_ThenAccepts()
    {
        // Arrange & Act
        var result = _parser.Parse("{123}");

        // Assert
        Assert.Single(result.Tokens);
        Assert.Equal("123", result.Tokens[0].Name);
    }

    [Fact]
    public void GivenTokenWithUnderscoreInName_WhenParsing_ThenAccepts()
    {
        // Arrange & Act
        var result = _parser.Parse("{user_name}");

        // Assert
        Assert.Single(result.Tokens);
        Assert.Equal("user_name", result.Tokens[0].Name);
    }

    [Fact]
    public void GivenTokenWithDotInName_WhenParsing_ThenAccepts()
    {
        // Arrange & Act
        var result = _parser.Parse("{user.name}");

        // Assert
        Assert.Single(result.Tokens);
        Assert.Equal("user.name", result.Tokens[0].Name);
    }
}
