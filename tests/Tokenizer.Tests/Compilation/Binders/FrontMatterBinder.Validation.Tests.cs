using Tokens.Compilation.Parsing;
using Tokens.Exceptions;
using Xunit;

#pragma warning disable MA0048 // Scenario test: FrontMatterBinder.Validation.Tests.cs

namespace Tokens.Compilation.Binders;

/// <summary>
/// Tests for front matter validation and error handling
/// </summary>
public class FrontMatterValidationTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenMissingOpeningDelimiter_WhenParsing_ThenParsesAsNormalContent()
    {
        // Arrange & Act
        var template = _parser.Parse("name: value\n---\n{token}");

        // Assert
        // Should parse as normal template, not front matter
        Assert.Single(template.Tokens);
    }

    [Fact]
    public void GivenMissingClosingDelimiter_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("---\nname: value\n{token}"));
    }

    [Fact]
    public void GivenEmptyFrontMatter_WhenParsing_ThenCreatesEmptyBlock()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n---\n{token}");

        // Assert
        Assert.Single(template.Tokens);
        // Front matter exists but has no entries
    }

    [Fact]
    public void GivenFrontMatterWithOnlyComments_WhenParsing_ThenSucceeds()
    {
        // Arrange & Act
        var template = _parser.Parse("---\n# Comment only\n---\n{token}");

        // Assert
        Assert.Single(template.Tokens);
    }

    [Fact]
    public void GivenFrontMatterNotAtStart_WhenParsing_ThenIgnores()
    {
        // Arrange & Act
        var template = _parser.Parse("Some text\n---\nname: value\n---\n{token}");

        // Assert
        Assert.Single(template.Tokens);
        // Front matter should be ignored because it's not at the start
    }

    [Fact]
    public void GivenLeadingWhitespaceBeforeFrontMatter_WhenParsing_ThenStillRecognizes()
    {
        // Arrange & Act
        var template = _parser.Parse("   ---\nname: value\n---\n{token}");

        // Assert
        // Should still recognize front matter even with leading whitespace
        Assert.Single(template.Tokens);
    }

    [Fact]
    public void GivenMultipleFrontMatterBlocks_WhenParsing_ThenOnlyParsesFirst()
    {
        // Arrange & Act
        var template = _parser.Parse("---\nname: first\n---\n---\nname: second\n---\n{token}");

        // Assert
        Assert.Single(template.Tokens);
        Assert.Equal("first", template.Name);
    }

    [Fact]
    public void GivenMalformedEntry_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("---\ninvalid entry without colon\n---\n{token}"));
    }

    [Fact]
    public void GivenUnterminatedBlock_WhenParsing_ThenThrowsWithContext()
    {
        // Arrange & Act
        var ex = Assert.Throws<ParsingException>(() => _parser.Parse("---\nname: value\n{token}"));

        // Assert
        Assert.NotNull(ex.Message);
    }
}
