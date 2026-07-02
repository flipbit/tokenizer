using Tokens.Exceptions;
using Tokens.Transformers;
using Xunit;

namespace Tokens.Compilation;

public class TokenParserTests
{
    private readonly TokenParser parser = new();

    [Fact]
    public void TestParseToken()
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
    public void TestParseTokenWithTrailingNewLine()
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
    public void TestParseTokenWithRequiredFlag()
    {
        var template = parser.Parse("Preamble{Token!}\n", "name");

        Assert.Equal("name", template.Name);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.True(token.IsRequired);
    }

    [Fact]
    public void TestParseSetName()
    {
        var template = parser.Parse("Preamble");

        Assert.Equal("Preamble", template.Name);
    }

    [Fact]
    public void TestParseSetNameLimitToThreeWords()
    {
        var template = parser.Parse("One Two Three Four");

        Assert.Equal("One Two Three...", template.Name);
    }

    [Fact]
    public void TestParseSetNameCountsNewLines()
    {
        var template = parser.Parse("One Two\r\nThree Four");


        Assert.Equal("One Two Three...", template.Name);
    }

    [Fact]
    public void TestParseSetNameIgnoresFrontmatterWithWindowsNewlines()
    {
        var template = parser.Parse("---\r\nOutOfOrder: true\r\n---\r\nOne Two\r\nThree Four");

        Assert.Equal("One Two Three...", template.Name);
    }

    [Fact]
    public void TestParseSetNameIgnoresFrontmatterWithUnixNewlines()
    {
        var template = parser.Parse("---\nOutOfOrder: true\n---\nOne Two\nThree Four");

        Assert.Equal("One Two Three...", template.Name);
    }

    [Fact]
    public void TestParseSetNameWhenEmpty()
    {
        var template = parser.Parse("");

        Assert.Equal("(empty)", template.Name);
    }

    [Fact]
    public void TestParseSetsTags()
    {
        var template = parser.Parse("---\nTag: tag\n---\nOne Two\nThree Four");

        Assert.Single(template.Tags);
        Assert.Equal("tag", template.Tags[0]);
    }

    [Fact]
    public void TestParseFrontMatterTokenWithoutSet()
    {
        Assert.Throws<TokenizerException>(() => parser.Parse("---\nset: Decorator\n---\nOne Two\nThree Four"));
    }

    [Fact]
    public void TestParseFrontMatterToken()
    {
        var template = parser.Parse("---\nset : Foo = tag\n---\nOne Two\nThree Four");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Foo", template.Tokens.First().Name);
    }
}
