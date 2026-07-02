using Tokens.Compilation.Parsing;
using Xunit;

namespace Tokens.Tests.Compilation.Parsing.Binding;

/// <summary>
/// Tests for decorator binding logic from AST to DecoratorDefinition
/// </summary>
public class TemplateBinderDecoratorTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenLonghandOptional_WhenBinding_ThenConvertsToFlag()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:Optional}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsOptional);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenLonghandRequired_WhenBinding_ThenConvertsToFlag()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:Required}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsRequired);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenLonghandRepeating_WhenBinding_ThenConvertsToFlag()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:Repeating}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsRepeating);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenLonghandEOL_WhenBinding_ThenConvertsToFlag()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:EOL}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.TerminateOnNewLine);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenCustomDecorator_WhenBinding_ThenCreatesDecoratorDefinition()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:ToUpper}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.Single(token.Decorators);
        Assert.Equal("ToUpper", token.Decorators[0].Name);
    }

    [Fact]
    public void GivenNotDecorator_WhenBinding_ThenSetsIsNotFlag()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:!IsNull}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.Single(token.Decorators);
        Assert.Equal("IsNull", token.Decorators[0].Name);
        Assert.True(token.Decorators[0].IsNotDecorator);
    }

    [Fact]
    public void GivenDecoratorWithArgs_WhenBinding_ThenCopiesArguments()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:Format(yyyy-MM-dd)}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.Single(token.Decorators);
        Assert.Equal("Format", token.Decorators[0].Name);
        Assert.Single(token.Decorators[0].Args);
        Assert.Equal("yyyy-MM-dd", token.Decorators[0].Args[0]);
    }

    [Fact]
    public void GivenMultipleDecorators_WhenBinding_ThenPreservesOrder()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:Trim,ToUpper,Substring(0,5)}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.Equal(3, token.Decorators.Count);
        Assert.Equal("Trim", token.Decorators[0].Name);
        Assert.Equal("ToUpper", token.Decorators[1].Name);
        Assert.Equal("Substring", token.Decorators[2].Name);
    }

    [Fact]
    public void GivenDuplicateDecorator_WhenBinding_ThenAllowsBoth()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:Trim,Trim}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.Equal(2, token.Decorators.Count);
        Assert.Equal("Trim", token.Decorators[0].Name);
        Assert.Equal("Trim", token.Decorators[1].Name);
    }

    [Fact]
    public void GivenMixedLonghandAndCustom_WhenBinding_ThenHandlesBoth()
    {
        // Arrange & Act
        var template = _parser.Parse("{name:Optional,Trim}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsOptional);
        Assert.Single(token.Decorators);
        Assert.Equal("Trim", token.Decorators[0].Name);
    }
}
