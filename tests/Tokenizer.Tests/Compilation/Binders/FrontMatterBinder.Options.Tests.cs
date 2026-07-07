using Tokens.Compilation.Parsing;
using Xunit;

#pragma warning disable MA0048 // Scenario test: FrontMatterBinder.Options.Tests.cs

namespace Tokens.Compilation.Binders;

/// <summary>
/// Tests for front matter option parsing and binding
/// </summary>
public class FrontMatterOptionsTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenFrontMatter_WhenParsing_ThenSetsOptions()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nCaseSensitive: true\n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Equal(StringComparison.InvariantCulture, template.Options.TokenStringComparison);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void GivenFrontMatterWithWindowsLineEndings_WhenParsing_ThenSetsOptions()
    {
        // Arrange & Act
        var template = _parser.Parse("---\r\n# Comment\r\nCaseSensitive: false\r\n---\r\nPreamble\r\n{TokenName}\r\n");

        // Assert
        Assert.Equal(StringComparison.InvariantCultureIgnoreCase, template.Options.TokenStringComparison);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void GivenFrontMatterWithName_WhenParsing_ThenSetsTemplateName()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nName: My Template\n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Equal("My Template", template.Name);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void GivenFrontMatterWithRequiredHint_WhenParsing_ThenAddsRequiredHint()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nHint: My Hint   \n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Single(template.Hints);
        Assert.Equal("My Hint", template.Hints[0].Text);
        Assert.False(template.Hints[0].Optional);
    }

    [Fact]
    public void GivenFrontMatterWithOptionalHint_WhenParsing_ThenAddsOptionalHint()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nHint?: My Hint   \n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Single(template.Hints);
        Assert.Equal("My Hint", template.Hints[0].Text);
        Assert.True(template.Hints[0].Optional);
    }

    [Fact]
    public void GivenFrontMatterWithMultipleHints_WhenParsing_ThenAddsAllHints()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nHint: My Hint   \nHint: Second Hint\n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Equal(2, template.Hints.Count);
        Assert.Equal("My Hint", template.Hints[0].Text);
        Assert.False(template.Hints[0].Optional);
        Assert.Equal("Second Hint", template.Hints[1].Text);
        Assert.False(template.Hints[1].Optional);
    }

    [Fact]
    public void GivenFrontMatterWithTag_WhenParsing_ThenAddsTag()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nTag: My Tag   \n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Single(template.Tags);
        Assert.Equal("My Tag", template.Tags[0]);
    }

    [Fact]
    public void GivenFrontMatterWithMultipleTags_WhenParsing_ThenAddsAllTags()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment\nTag: Tag One   \nTag: Tag Two  \n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Equal(2, template.Tags.Count);
        Assert.Equal("Tag One", template.Tags[0]);
        Assert.Equal("Tag Two", template.Tags[1]);
    }

    [Fact]
    public void GivenFrontMatterWithMultipleComments_WhenParsing_ThenParsesCorrectly()
    {
        // Arrange
        var content = """
                      ---
                      #
                      # .capetown Parsing Template
                      #

                      # Use this template for queries to capetown-whois.registry.net.za:
                      tag: capetown-whois.registry.net.za
                      tag: capetown

                      # Set query response type:
                      set: Response = NotFound
                      ---

                      """;

        // Act
        var template = _parser.Parse(content);

        // Assert
        Assert.Equal(2, template.Tags.Count);
    }
}
