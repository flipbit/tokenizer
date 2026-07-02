using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Parsing.Binding;

/// <summary>
/// Tests for semantic validation during binding
/// </summary>
public class TemplateBinderValidationTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenOptionalAndRequired_WhenBinding_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("{name?!}"));
    }

    [Fact]
    public void GivenRequiredAndOptional_WhenBinding_ThenThrowsParsingException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ParsingException>(() => _parser.Parse("{name!?}"));
    }

    [Fact]
    public void GivenEmptyTokenName_WhenBinding_ThenAllowsForTrailingPreamble()
    {
        // Arrange & Act
        var template = _parser.Parse("{name} trailing text");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("name", template.Tokens[0].Name);
        Assert.Equal(string.Empty, template.Tokens[1].Name);
        Assert.Equal(" trailing text", template.Tokens[1].Preamble);
    }

    [Fact]
    public void GivenReservedTokenNameNull_WhenBinding_ThenHandlesSpecially()
    {
        // Arrange & Act
        var template = _parser.Parse("{Null}");

        // Assert
        var token = Assert.Single(template.Tokens);
        Assert.Equal("Null", token.Name);
        Assert.True(token.IsNull);
    }

    [Fact]
    public void GivenDuplicateTokenNames_WhenBinding_ThenAllowsBoth()
    {
        // Arrange & Act
        var template = _parser.Parse("{name}{name}");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("name", template.Tokens[0].Name);
        Assert.Equal("name", template.Tokens[1].Name);
    }
}
