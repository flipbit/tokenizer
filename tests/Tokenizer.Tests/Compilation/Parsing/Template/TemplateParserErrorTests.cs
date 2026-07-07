using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Parsing.Template;

/// <summary>
/// Tests for error handling and error message quality
/// </summary>
public class TemplateParserErrorTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenMissingCloseBrace_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange & Act
        var ex = Assert.Throws<ParsingException>(() => _parser.Parse("{name"));

        // Assert
        Assert.NotNull(ex.Message);
        Assert.Contains("}", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenMalformedDecoratorArgs_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("{name:regex(())}"));
        Assert.Throws<ParsingException>(() => _parser.Parse("{name:regex(, )}"));
    }

    [Fact]
    public void GivenMisplacedModifiers_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("{name=1?}"));
        Assert.Throws<ParsingException>(() => _parser.Parse("{name:trim?}"));
    }

    [Fact]
    public void GivenParsingError_WhenThrown_ThenIncludesLineNumber()
    {
        // Arrange & Act
        var ex = Assert.Throws<ParsingException>(() => _parser.Parse("Line1\n{name\nLine3"));

        // Assert
        Assert.True(ex.Line > 0, "Error should include line number");
        Assert.Equal(2, ex.Line);
    }

    [Fact]
    public void GivenParsingError_WhenThrown_ThenIncludesColumnNumber()
    {
        // Arrange & Act
        var ex = Assert.Throws<ParsingException>(() => _parser.Parse("{name"));

        // Assert
        Assert.True(ex.Column > 0, "Error should include column number");
    }

    [Fact]
    public void GivenUnclosedQuotedString_WhenParsing_ThenThrowsWithLocation()
    {
        // Arrange & Act - Lexer detects unclosed quotes during tokenization
        var ex = Assert.Throws<LexerException>(() => _parser.Parse("{name=\"unclosed}"));

        // Assert
        Assert.NotNull(ex.Message);
        Assert.True(ex.Line > 0);
        Assert.True(ex.Column > 0);
    }

    [Fact]
    public void GivenUnbalancedParentheses_WhenParsing_ThenThrowsWithLocation()
    {
        // Arrange & Act
        var ex = Assert.Throws<ParsingException>(() => _parser.Parse("{name:decorator(arg}"));

        // Assert
        Assert.NotNull(ex.Message);
        Assert.Contains(")", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenInvalidCharacterInToken_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("{name with spaces}"));
    }

    [Fact]
    public void GivenUnexpectedEndOfInput_WhenParsing_ThenThrowsWithContext()
    {
        // Arrange & Act
        var ex = Assert.Throws<ParsingException>(() => _parser.Parse("{name:decorator("));

        // Assert
        Assert.NotNull(ex.Message);
        Assert.True(ex.Message.Length > 0);
    }

    [Fact]
    public void GivenNestedOpenBraces_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("{name{nested}}"));
    }

    [Fact]
    public void GivenEmptyDecoratorName_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("{name:}"));
    }

    [Fact]
    public void GivenLeadingCommaInDecoratorArgs_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("{name:decorator(,arg)}"));
    }
}
