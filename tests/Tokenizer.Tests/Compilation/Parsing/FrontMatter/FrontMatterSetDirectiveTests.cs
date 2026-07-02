using Tokens.Compilation.Parsing;
using Xunit;

namespace Tokens.Tests.Compilation.Parsing.FrontMatter;

/// <summary>
/// Tests for "set:" directive parsing in front matter
/// </summary>
public class FrontMatterSetDirectiveTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenFrontMatterWithSetToken_WhenParsing_ThenCreatesFrontMatterToken()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecorator_WhenParsing_ThenCreatesFrontMatterTokenWithDecorator()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken : MyDecorator \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecoratorWithArgument_WhenParsing_ThenCreatesFrontMatterTokenWithDecoratorAndArgument()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken : MyDecorator(Arg1) \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Single(template.Tokens[0].Decorators[0].Args);
        Assert.Equal("Arg1", template.Tokens[0].Decorators[0].Args[0]);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecoratorWithMultipleArguments_WhenParsing_ThenCreatesFrontMatterTokenWithDecoratorAndAllArguments()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken : MyDecorator(Arg1, Arg2) \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Equal(2, template.Tokens[0].Decorators[0].Args.Count);
        Assert.Equal("Arg1", template.Tokens[0].Decorators[0].Args[0]);
        Assert.Equal("Arg2", template.Tokens[0].Decorators[0].Args[1]);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecoratorWithDoubleQuotedArgument_WhenParsing_ThenCreatesFrontMatterTokenWithDecoratorAndArgument()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken : MyDecorator(\"Arg1, Arg2\") \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Single(template.Tokens[0].Decorators[0].Args);
        Assert.Equal("Arg1, Arg2", template.Tokens[0].Decorators[0].Args[0]);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecoratorWithSingleQuotedArgument_WhenParsing_ThenCreatesFrontMatterTokenWithDecoratorAndArgument()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken : MyDecorator('Arg1, Arg2') \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Single(template.Tokens[0].Decorators[0].Args);
        Assert.Equal("Arg1, Arg2", template.Tokens[0].Decorators[0].Args[0]);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndAssignment_WhenParsing_ThenCreatesFrontMatterTokenWithValue()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken = Foo \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo", template.Tokens[0].Value);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndAssignmentInSingleQuotes_WhenParsing_ThenCreatesFrontMatterTokenWithValue()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken = 'Foo Bar' \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo Bar", template.Tokens[0].Value);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndAssignmentInDoubleQuotes_WhenParsing_ThenCreatesFrontMatterTokenWithValue()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken = \"Foo Bar\" \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo Bar", template.Tokens[0].Value);
    }

    [Fact]
    public void GivenFrontMatterWithMultipleSetTokens_WhenParsing_ThenCreatesAllFrontMatterTokens()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nset: MyToken = \"Foo Bar\" \n  Set  : this = that : ToUpper \n---\nPreamble\n");

        // Assert
        Assert.Equal(3, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo Bar", template.Tokens[0].Value);

        Assert.Equal("this", template.Tokens[1].Name);
        Assert.True(template.Tokens[1].IsFrontMatterToken);
        Assert.Equal("that", template.Tokens[1].Value);
        Assert.Single(template.Tokens[1].Decorators);
        Assert.Equal("ToUpper", template.Tokens[1].Decorators[0].Name);
    }
}
