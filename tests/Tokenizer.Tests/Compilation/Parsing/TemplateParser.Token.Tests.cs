using Tokens.Exceptions;
using Xunit;

#pragma warning disable MA0048 // Scenario test: TemplateParser.Token.Tests.cs
namespace Tokens.Compilation.Parsing;

/// <summary>
/// Tests for token name parsing and structure
/// </summary>
public class TemplateParserTokenTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenTokenWithInvalidName_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("This is the preamble{Token Name}"));
    }

    [Fact]
    public void GivenNullToken_WhenParsing_ThenSetsIsNull()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ Null } Next preamble");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("Null", token.Name);
        Assert.True(token.IsNull);
    }

    [Fact]
    public void GivenTokenWithWhitespace_WhenParsing_ThenAllowsWhitespace()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName $ ! * : IsDomain , IsUrl }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsOptional);
        Assert.True(token.TerminateOnNewLine);
        Assert.True(token.IsRepeating);
        Assert.True(token.IsRequired);
    }

    [Fact]
    public void GivenTemplateWithMultipleTokens_WhenParsing_ThenSetsCorrectLocations()
    {
        // Arrange
        var content = """
                      { First : Decorator('One'), Two , Three (" Four ") }
                      {Second} {Third}

                      {Fourth}
                      {Fifth}


                      {Sixth}
                      """;

        // Act
        var template = _parser.Parse(content);

        // Assert
        Assert.Equal(6, template.Tokens.Count);

        Assert.Equal("""{ First : Decorator('One'), Two, Three(' Four ') }""", template.Tokens[0].ToString());
        Assert.Equal(1, template.Tokens[0].Location.Column);
        Assert.Equal(1, template.Tokens[0].Location.Line);
        Assert.Equal(1, template.Tokens[0].Location.Paragraph);

        Assert.Equal(@"{ Second }", template.Tokens[1].ToString());
        Assert.Equal(1, template.Tokens[1].Location.Column);
        Assert.Equal(2, template.Tokens[1].Location.Line);
        Assert.Equal(1, template.Tokens[1].Location.Paragraph);

        Assert.Equal(@"{ Third }", template.Tokens[2].ToString());
        Assert.Equal(10, template.Tokens[2].Location.Column);
        Assert.Equal(2, template.Tokens[2].Location.Line);
        Assert.Equal(1, template.Tokens[2].Location.Paragraph);

        Assert.Equal(@"{ Fourth }", template.Tokens[3].ToString());
        Assert.Equal(1, template.Tokens[3].Location.Column);
        Assert.Equal(4, template.Tokens[3].Location.Line);
        Assert.Equal(2, template.Tokens[3].Location.Paragraph);

        Assert.Equal(@"{ Fifth }", template.Tokens[4].ToString());
        Assert.Equal(1, template.Tokens[4].Location.Column);
        Assert.Equal(5, template.Tokens[4].Location.Line);
        Assert.Equal(2, template.Tokens[4].Location.Paragraph);

        Assert.Equal(@"{ Sixth }", template.Tokens[5].ToString());
        Assert.Equal(1, template.Tokens[5].Location.Column);
        Assert.Equal(8, template.Tokens[5].Location.Line);
        Assert.Equal(3, template.Tokens[5].Location.Paragraph);
    }
}
