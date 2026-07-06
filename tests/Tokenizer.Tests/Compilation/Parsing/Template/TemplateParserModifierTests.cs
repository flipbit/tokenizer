using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Compilation.Parsing.Template;

/// <summary>
/// Tests for modifier parsing (?, *, !, $)
/// </summary>
public class TemplateParserModifierTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();
    private readonly ITestOutputHelper _output;

    public TemplateParserModifierTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GivenTokenWithNewLineTerminator_WhenParsing_ThenSetsTerminateOnNewLine()
    {
        // Arrange & Act
        var template = _parser.Parse("Preamble{TokenName$}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.IsOptional);
        Assert.True(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
    }

    [Fact]
    public void GivenTokenWithInvalidNewLineTerminator_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("This is the preamble{Token Name$$}"));
    }

    [Fact]
    public void GivenTokenWithOptionalTerminator_WhenParsing_ThenSetsOptional()
    {
        // Arrange & Act
        var template = _parser.Parse("Preamble{TokenName?}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsOptional);
        Assert.False(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
        Assert.False(token.IsRequired);
    }

    [Fact]
    public void GivenTokenWithRequiredTerminator_WhenParsing_ThenSetsRequired()
    {
        // Arrange & Act
        var template = _parser.Parse("Preamble{TokenName!}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsRequired);
    }

    [Fact]
    public void GivenTokenWithRequiredAndOptionalCharacter_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        try
        {
            _parser.Parse("This is the preamble{TokenName!?}");

            Assert.Fail("No exception thrown.");
        }
        catch (ParsingException e)
        {
            _output.WriteLine(e.Message);
        }
        catch (Exception e)
        {
            Assert.Fail($"Incorrect Exception Thrown: {e.GetType().Name}");
        }
    }

    [Fact]
    public void GivenTokenWithOptionalAndRequiredCharacter_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        try
        {
            _parser.Parse("This is the preamble{TokenName?!}");

            Assert.Fail("No exception thrown.");
        }
        catch (ParsingException e)
        {
            _output.WriteLine(e.Message);
        }
        catch (Exception e)
        {
            Assert.Fail($"Incorrect Exception Thrown: {e.GetType().Name}");
        }
    }

    [Fact]
    public void GivenTokenWithOptionalAndNewLineTerminator_WhenParsing_ThenSetsBothFlags()
    {
        // Arrange & Act
        var template = _parser.Parse("Preamble{TokenName$?}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsOptional);
        Assert.True(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
    }

    [Fact]
    public void GivenRepeatingTokenWithNewLine_WhenParsing_ThenExpandsNewLine()
    {
        // Arrange & Act
        var template = _parser.Parse("Repeating Token:\n    { TokenName * }\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token1 = template.Tokens[0];

        Assert.Equal("Repeating Token:\n    ", token1.Preamble);
        Assert.Equal("TokenName", token1.Name);
        Assert.False(token1.IsRepeating);

        var token2 = template.Tokens[1];

        Assert.Equal("\n    ", token2.Preamble);
        Assert.Equal("TokenName", token2.Name);
        Assert.True(token2.IsRepeating);
    }

    [Fact]
    public void GivenRepeatingTokenWithoutNewLine_WhenParsing_ThenDoesNotExpandNewLine()
    {
        // Arrange & Act
        var template = _parser.Parse(@"Repeating Token:    { TokenName * }");

        // Assert
        Assert.Single(template.Tokens);

        var token1 = template.Tokens[0];

        Assert.Equal("Repeating Token:    ", token1.Preamble);
        Assert.Equal("TokenName", token1.Name);
        Assert.True(token1.IsRepeating);
    }

    [Fact]
    public void GivenTokenWithRequiredLonghand_WhenParsing_ThenSetsRequired()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName : Required }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsRequired);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithOptionalLonghand_WhenParsing_ThenSetsOptional()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName : Optional }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsOptional);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithRepeatingLonghand_WhenParsing_ThenSetsRepeating()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName : Repeating }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsRepeating);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithNewLineLonghand_WhenParsing_ThenSetsTerminateOnNewLine()
    {
        // Arrange & Act
        var template = _parser.Parse("This is the preamble{ TokenName : EOL }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.TerminateOnNewLine);
        Assert.Empty(token.Decorators);
    }
}
