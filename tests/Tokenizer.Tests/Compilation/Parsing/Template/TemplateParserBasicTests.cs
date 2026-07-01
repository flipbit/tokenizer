using System;
using System.Linq;
using Tokens.Compilation.Parsing;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Tests.Compilation.Parsing.Template;

/// <summary>
/// Tests for basic template parsing (empty, single, multiple tokens)
/// </summary>
public class TemplateParserBasicTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenEmptyString_WhenParsing_ThenReturnsEmptyTemplate()
    {
        // Arrange & Act
        var template = _parser.Parse(string.Empty);

        // Assert
        Assert.Empty(template.Tokens);
    }

    [Fact]
    public void GivenNullString_WhenParsing_ThenThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void GivenSingleToken_WhenParsing_ThenReturnsCorrectToken()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{TokenName}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.Optional);
        Assert.False(token.TerminateOnNewline);
        Assert.False(token.Repeating);
    }

    [Fact]
    public void GivenTwoTokens_WhenParsing_ThenReturnsBothTokens()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{TokenName}Preamble 2 {TokenName2}");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token1 = template.Tokens.First();

        Assert.Equal("This is the preamble", token1.Preamble);
        Assert.Equal("TokenName", token1.Name);
        Assert.False(token1.Optional);
        Assert.False(token1.TerminateOnNewline);
        Assert.False(token1.Repeating);

        var token2 = template.Tokens.ElementAt(1);

        Assert.Equal("Preamble 2 ", token2.Preamble);
        Assert.Equal("TokenName2", token2.Name);
        Assert.False(token2.Optional);
        Assert.False(token2.TerminateOnNewline);
        Assert.False(token2.Repeating);
    }

    [Fact]
    public void GivenTokenWithTrailingText_WhenParsing_ThenCreatesSecondToken()
    {
        // Arrange & Act
        var template = _parser.Parse(@"Preamble{TokenName} Postamble");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();
        Assert.Equal("TokenName", token.Name);

        var second = template.Tokens[1];
        Assert.Equal(string.Empty, second.Name);
        Assert.Equal(" Postamble", second.Preamble);
    }
}
