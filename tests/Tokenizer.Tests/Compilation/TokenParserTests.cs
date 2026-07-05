using Tokens.Exceptions;
using Tokens.Transformers;
using Xunit;

namespace Tokens.Compilation;

public class TokenParserTests
{
    private readonly TokenParser parser = new();

    [Fact]
    public void GivenTemplateWithDecorator_WhenParsing_ThenTokenHasDecorator()
    {
        var template = parser.Parse("Preamble{Token:ToDateTime(yyyy-MM-dd)}", "name");

        Assert.Equal("name", template.Name);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Single(token.Decorators);

        var tokenOperator = token.Decorators.First();

        Assert.Equal(typeof(ToDateTimeTransformer), tokenOperator.DecoratorType);
        Assert.Single(tokenOperator.Parameters);
        Assert.Equal("yyyy-MM-dd", tokenOperator.Parameters[0]);
    }

    [Fact]
    public void GivenTemplateWithTrailingNewLine_WhenParsing_ThenTokenHasDecorator()
    {
        var template = parser.Parse("Preamble{Token:ToDateTime(yyyy-MM-dd)}\n", "name");

        Assert.Equal("name", template.Name);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Single(token.Decorators);

        var tokenOperator = token.Decorators.First();

        Assert.Equal(typeof(ToDateTimeTransformer), tokenOperator.DecoratorType);
        Assert.Single(tokenOperator.Parameters);
        Assert.Equal("yyyy-MM-dd", tokenOperator.Parameters[0]);
    }

    [Fact]
    public void GivenTemplateWithRequiredFlag_WhenParsing_ThenTokenIsRequired()
    {
        var template = parser.Parse("Preamble{Token!}\n", "name");

        Assert.Equal("name", template.Name);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.True(token.IsRequired);
    }

    [Fact]
    public void GivenSimpleText_WhenParsing_ThenNameIsText()
    {
        var template = parser.Parse("Preamble");

        Assert.Equal("Preamble", template.Name);
    }

    [Fact]
    public void GivenTextWithManyWords_WhenParsing_ThenNameIsTruncated()
    {
        var template = parser.Parse("One Two Three Four");

        Assert.Equal("One Two Three...", template.Name);
    }

    [Fact]
    public void GivenTextWithNewLines_WhenParsing_ThenNewLinesCountAsWordBreaks()
    {
        var template = parser.Parse("One Two\r\nThree Four");


        Assert.Equal("One Two Three...", template.Name);
    }

    [Fact]
    public void GivenFrontMatterWithWindowsNewlines_WhenParsing_ThenNameIgnoresFrontMatter()
    {
        var template = parser.Parse("---\r\nOutOfOrder: true\r\n---\r\nOne Two\r\nThree Four");

        Assert.Equal("One Two Three...", template.Name);
    }

    [Fact]
    public void GivenFrontMatterWithUnixNewlines_WhenParsing_ThenNameIgnoresFrontMatter()
    {
        var template = parser.Parse("---\nOutOfOrder: true\n---\nOne Two\nThree Four");

        Assert.Equal("One Two Three...", template.Name);
    }

    [Fact]
    public void GivenEmptyContent_WhenParsing_ThenNameIsEmpty()
    {
        var template = parser.Parse("");

        Assert.Equal("(empty)", template.Name);
    }

    [Fact]
    public void GivenFrontMatterWithTag_WhenParsing_ThenTemplateHasTag()
    {
        var template = parser.Parse("---\nTag: tag\n---\nOne Two\nThree Four");

        Assert.Single(template.Tags);
        Assert.Equal("tag", template.Tags[0]);
    }

    [Fact]
    public void GivenFrontMatterTokenWithoutSetValue_WhenParsing_ThenThrowsException()
    {
        Assert.Throws<TokenizerException>(() => parser.Parse("---\nset: Decorator\n---\nOne Two\nThree Four"));
    }

    [Fact]
    public void GivenFrontMatterTokenWithSetValue_WhenParsing_ThenTokenHasName()
    {
        var template = parser.Parse("---\nset : Foo = tag\n---\nOne Two\nThree Four");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Foo", template.Tokens.First().Name);
    }
}
