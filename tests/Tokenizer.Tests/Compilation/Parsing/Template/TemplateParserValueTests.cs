using System.Linq;
using Tokens.Compilation.Parsing;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Tests.Compilation.Parsing.Template;

/// <summary>
/// Tests for token value assignment parsing
/// </summary>
public class TemplateParserValueTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenTokenWithSetValue_WhenParsing_ThenSetsValue()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName = Foo }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal("Foo", token.Value);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithSetValueAndDecorator_WhenParsing_ThenSetsValueAndDecorator()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName = Foo : Bar }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal("Foo", token.Value);
        Assert.Single(token.Decorators);
        Assert.Equal("Bar", token.Decorators[0].Name);
    }

    [Fact]
    public void GivenTokenWithSetValueContainingSpaces_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("This is the preamble{ TokenName = Foo Bar }"));
    }

    [Fact]
    public void GivenTokenWithSetValueContainingInvalidCharacters_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("This is the preamble{ TokenName = Foo{Bar }"));
    }

    [Fact]
    public void GivenTokenWithSetValueInDoubleQuotes_WhenParsing_ThenSetsValue()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName = \" { Foo } \" }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal(" { Foo } ", token.Value);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithSetValueInSingleQuotes_WhenParsing_ThenSetsValue()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName = ' { Foo } \" ' }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal(" { Foo } \" ", token.Value);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithSetValueInSingleQuotesAndDecorator_WhenParsing_ThenSetsValueAndDecorator()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName = ' { Foo } \" ' : Bar } Next preamble");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal(" { Foo } \" ", token.Value);
        Assert.Single(token.Decorators);
        Assert.Equal("Bar", token.Decorators[0].Name);
    }
}
