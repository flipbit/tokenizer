using System.Linq;
using Tokens.Compilation.Parsing;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Tests.Compilation.Parsing.Template;

/// <summary>
/// Tests for decorator parsing
/// </summary>
public class TemplateParserDecoratorTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenTokenWithDecorator_WhenParsing_ThenAddsDecorator()
    {
        // Arrange & Act
        var template = _parser.Parse("Preamble{TokenName:ToDateTime}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.Optional);
        Assert.False(token.TerminateOnNewline);
        Assert.False(token.Repeating);
        Assert.Single(token.Decorators);

        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);
    }

    [Fact]
    public void GivenTokenWithMultipleDecorators_WhenParsing_ThenAddsAllDecorators()
    {
        // Arrange & Act
        var template = _parser.Parse("Preamble{TokenName:Trim,IsNotNullOrEmpty}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.Optional);
        Assert.False(token.TerminateOnNewline);
        Assert.False(token.Repeating);
        Assert.Equal(2, token.Decorators.Count);

        var decorator1 = token.Decorators.First();

        Assert.Equal("Trim", decorator1.Name);

        var decorator2 = token.Decorators.ElementAt(1);

        Assert.Equal("IsNotNullOrEmpty", decorator2.Name);
    }

    [Fact]
    public void GivenTokenWithDecoratorWithArgument_WhenParsing_ThenAddsDecoratorWithArgument()
    {
        // Arrange & Act
        var template = _parser.Parse("Preamble{TokenName:ToDateTime(yyyy-MM-dd)}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);

        Assert.Single(decorator.Args);
        Assert.Equal("yyyy-MM-dd", decorator.Args.First());
    }

    [Fact]
    public void GivenTokenWithDecoratorWithSingleQuotedArgument_WhenParsing_ThenAddsDecoratorWithArgument()
    {
        // Arrange & Act
        var template = _parser.Parse("Preamble{TokenName: ToDateTime ( 'yyyy-MM-dd' )}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);

        Assert.Single(decorator.Args);
        Assert.Equal("yyyy-MM-dd", decorator.Args.First());
    }

    [Fact]
    public void GivenTokenWithDecoratorWithDoubleQuotedArgument_WhenParsing_ThenAddsDecoratorWithArgument()
    {
        // Arrange & Act
        var template = _parser.Parse("""Preamble{TokenName: ToDateTime ( "yyyy-MM-dd" )}""");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);

        Assert.Single(decorator.Args);
        Assert.Equal("yyyy-MM-dd", decorator.Args.First());
    }

    [Fact]
    public void GivenTokenWithDecoratorWithThreeArguments_WhenParsing_ThenAddsDecoratorWithAllArguments()
    {
        // Arrange & Act
        var template = _parser.Parse(@"Preamble{TokenName:Decorator(One, Two Arg ,Three )}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("Decorator", decorator.Name);

        Assert.Equal(3, decorator.Args.Count);
        Assert.Equal("One", decorator.Args[0]);
        Assert.Equal("Two Arg", decorator.Args[1]);
        Assert.Equal("Three", decorator.Args[2]);
    }

    [Fact]
    public void GivenNotDecorator_WhenParsing_ThenSetsIsNotDecorator()
    {
        // Arrange & Act
        var template = _parser.Parse("{ MyToken : !MyDecorator }");

        // Assert
        Assert.Single(template.Tokens);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.True(template.Tokens[0].Decorators[0].IsNotDecorator);
    }

    [Fact]
    public void GivenInvalidNotDecorator_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("{ MyToken : Invalid!MyDecorator }"));
    }
}
