using Tokens.Compilation.Binders;
using Tokens.Compilation.Definitions;
using Tokens.Compilation.Nodes;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Parsing;

public class FrontMatterBinderTests
{
    [Fact]
    public void GivenBooleanOptions_WhenBinding_ThenOptionsMapped()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "trimleadingwhitespace", "yes"),
            new FrontMatterEntry(loc, 0, 5, "casesensitive", "on"),
            new FrontMatterEntry(loc, 0, 5, "ignoremissingproperties", "false")
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.True(template.Options.TrimLeadingWhitespaceInTokenPreamble);
        Assert.Equal(System.StringComparison.InvariantCulture, template.Options.TokenStringComparison);
        Assert.False(template.Options.IgnoreMissingProperties);
    }

    [Fact]
    public void GivenNameHintTag_WhenBinding_ThenPropertiesSet()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "name", "My Template"),
            new FrontMatterEntry(loc, 0, 5, "hint", "A hint"),
            new FrontMatterEntry(loc, 0, 5, "hint?", "Optional hint"),
            new FrontMatterEntry(loc, 0, 5, "tag", "alpha")
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.Equal("My Template", template.Name);
        Assert.Equal(2, template.Hints.Count);
        Assert.Equal("alpha", template.Tags.Single());
    }

    [Fact]
    public void GivenSetDirective_WhenBinding_ThenTokenAdded()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[] { new SetTokenDirective(loc, 0, 5, "value") };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.Single(template.Tokens);
        Assert.Equal("value", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
    }

    [Fact]
    public void GivenUnknownOption_WhenBinding_ThenThrows()
    {
        // Arrange
        var loc = new FileLocation();
        var fm = new FrontMatterBlock(loc, 0, 10, new SyntaxNode[] { new FrontMatterEntry(loc, 0, 5, "unknown", "x") });
        var template = new TemplateDefinition();

        // Act & Assert
        Assert.Throws<ParsingException>(() => new FrontMatterBinder().Bind(template, fm));
    }

    [Fact]
    public void GivenDuplicateName_WhenBinding_ThenLastOneWins()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "name", "First"),
            new FrontMatterEntry(loc, 0, 5, "Name", "Second")
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.Equal("Second", template.Name);
    }

    [Fact]
    public void GivenDuplicateBoolean_WhenBinding_ThenLastOneWins()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "trimleadingwhitespace", "off"),
            new FrontMatterEntry(loc, 0, 5, "TrimLeadingWhitespace", "on")
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.True(template.Options.TrimLeadingWhitespaceInTokenPreamble);
    }

    [Fact]
    public void GivenMultipleHintsTagsSets_WhenBinding_ThenAccumulate()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "hint", "A"),
            new FrontMatterEntry(loc, 0, 5, "hint?", "B"),
            new FrontMatterEntry(loc, 0, 5, "tag", "alpha"),
            new FrontMatterEntry(loc, 0, 5, "tag", "beta"),
            new SetTokenDirective(loc, 0, 5, "value1"),
            new SetTokenDirective(loc, 0, 5, "value2")
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.Equal(2, template.Hints.Count);
        Assert.Equal(2, template.Tags.Count);
        Assert.Equal(2, template.Tokens.Count);
    }
}


