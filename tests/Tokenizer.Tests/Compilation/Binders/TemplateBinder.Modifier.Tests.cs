using Tokens.Compilation.Parsing;
using Xunit;

#pragma warning disable MA0048 // Scenario test: TemplateBinder.Modifier.Tests.cs

namespace Tokens.Compilation.Binders;

/// <summary>
/// Tests for modifier binding logic from AST to TokenDefinition
/// </summary>
public class TemplateBinderModifierTests
{
    [Fact]
    public void GivenFrontMatterTerminateOnNewLine_WhenBinding_ThenAllTokensTerminateOnNewLine()
    {
        // Arrange
        var input = "---\nTerminateOnNewLine: true\n---\nA: {a}\nB: {b}";
        var parser = new TemplateParser();
        var doc = parser.Parse(input);

        // Act
        var def = TemplateBinder.Bind(doc);

        // Assert
        Assert.Equal(2, def.Tokens.Count);
        Assert.All(def.Tokens, t => Assert.True(t.TerminateOnNewLine));
    }
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenOptionalModifier_WhenBinding_ThenSetsOptionalFlag()
    {
        // Arrange & Act
        var template = _parser.Parse("{name?}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsOptional);
        Assert.False(token.IsRequired);
    }

    [Fact]
    public void GivenRequiredModifier_WhenBinding_ThenSetsRequiredFlag()
    {
        // Arrange & Act
        var template = _parser.Parse("{name!}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsRequired);
        Assert.False(token.IsOptional);
    }

    [Fact]
    public void GivenRepeatingModifier_WhenBinding_ThenSetsRepeatingAndOptional()
    {
        // Arrange & Act
        var template = _parser.Parse("{name*}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsRepeating);
        Assert.True(token.IsOptional);
    }

    [Fact]
    public void GivenTerminateModifier_WhenBinding_ThenSetsTerminateOnNewLine()
    {
        // Arrange & Act
        var template = _parser.Parse("{name$}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenAllModifiers_WhenBinding_ThenSetsAllFlags()
    {
        // Arrange & Act
        var template = _parser.Parse("{name?*$}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsOptional);
        Assert.True(token.IsRepeating);
        Assert.True(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenNoModifiers_WhenBinding_ThenUsesDefaults()
    {
        // Arrange & Act
        var template = _parser.Parse("{name}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.False(token.IsOptional);
        Assert.False(token.IsRequired);
        Assert.False(token.IsRepeating);
        Assert.False(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenOptionalAndTerminate_WhenBinding_ThenSetsBothFlags()
    {
        // Arrange & Act
        var template = _parser.Parse("{name?$}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsOptional);
        Assert.True(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenRepeatingImpliesOptional_WhenBinding_ThenOptionalIsSet()
    {
        // Arrange & Act
        var template = _parser.Parse("{name*}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsRepeating);
        Assert.True(token.IsOptional);
    }

    [Fact]
    public void GivenOnceDecorator_WhenBinding_ThenSetsConsiderOnce()
    {
        // Arrange & Act
        var template = _parser.Parse("{name : Once}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.True(token.IsSingleUse);
        Assert.Empty(token.Decorators);
    }
}
