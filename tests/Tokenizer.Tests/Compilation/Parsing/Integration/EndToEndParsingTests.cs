using Xunit;

namespace Tokens.Compilation.Parsing.Integration;

/// <summary>
/// End-to-end tests for the full parsing pipeline (Lexer → Parser → Binder → Definition)
/// </summary>
public class EndToEndParsingTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenSimpleTemplate_WhenParsed_ThenProducesUsableDefinition()
    {
        // Arrange
        var template = "Hello {name}!";

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal(2, definition.Tokens.Count);
        Assert.Equal("name", definition.Tokens[0].Name);
        Assert.Equal("Hello ", definition.Tokens[0].Preamble);
        Assert.NotNull(definition.Tokens[0].Content);
        // Trailing "!" creates terminal token
        Assert.Equal(string.Empty, definition.Tokens[1].Name);
        Assert.Equal("!", definition.Tokens[1].Preamble);
    }

    [Fact]
    public void GivenTemplateWithAllFeatures_WhenParsed_ThenHandlesCorrectly()
    {
        // Arrange
        var template = """
                       ---
                       name: Complete Template
                       casesensitive: false
                       hint: Test hint
                       tag: test
                       set: Response = Success
                       ---
                       Start {id!:trim:format(00)} middle {name?="default":upper} end
                       """;

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal("Complete Template", definition.Name);
        Assert.Contains(definition.Tags, t => string.Equals(t, "test", StringComparison.Ordinal));
        Assert.Single(definition.Hints);
        Assert.Equal(4, definition.Tokens.Count); // Response, id, name, trailing token

        var responseToken = definition.Tokens[0];
        Assert.Equal("Response", responseToken.Name);
        Assert.True(responseToken.IsFrontMatterToken);
        Assert.Equal("Success", responseToken.Value);

        var idToken = definition.Tokens[1];
        Assert.Equal("id", idToken.Name);
        Assert.True(idToken.IsRequired);
        Assert.Equal(2, idToken.Decorators.Count);
    }

    [Fact]
    public void GivenComplexRealWorldTemplate_WhenParsed_ThenSucceeds()
    {
        // Arrange
        var template = """
                       Registered: {registered_date$:ToDateTime(dd-MMM-yyyy)}
                       Updated:    {updated_date$:ToDateTime(dd-MMM-yyyy)}
                       Expiry:     {expiry_date$:ToDateTime(dd-MMM-yyyy)}
                       Status:     {status$}
                       """;

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal(4, definition.Tokens.Count);
        foreach (var token in definition.Tokens)
        {
            Assert.True(token.TerminateOnNewLine);
            if (token.Name.Contains("date", StringComparison.Ordinal))
            {
                Assert.Single(token.Decorators);
                Assert.Equal("ToDateTime", token.Decorators[0].Name);
            }
        }
    }

    [Fact]
    public void GivenParsedTemplate_WhenTokenContentReParsed_ThenProducesSameToken()
    {
        // Arrange
        var template = "{name:trim,upper}";

        // Act
        var def1 = _parser.Parse(template);
        var tokenContent = def1.Tokens[0].Content; // Get raw token content: "{ name : trim, upper }"
        var def2 = _parser.Parse(tokenContent);

        // Assert
        Assert.Single(def2.Tokens);
        Assert.Equal(def1.Tokens[0].Name, def2.Tokens[0].Name);
        Assert.Equal(def1.Tokens[0].Decorators.Count, def2.Tokens[0].Decorators.Count);
        Assert.Equal("trim", def2.Tokens[0].Decorators[0].Name);
        Assert.Equal("upper", def2.Tokens[0].Decorators[1].Name);
    }

    [Fact]
    public void GivenTemplateWithFrontMatter_WhenParsed_ThenOptionsApplied()
    {
        // Arrange
        var template = """
                       ---
                       casesensitive: true
                       trimleadingwhitespace: true
                       ---
                       {name}
                       """;

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal(System.StringComparison.InvariantCulture, definition.Options.TokenStringComparison);
        Assert.True(definition.Options.TrimLeadingWhitespaceInTokenPreamble);
    }

    [Fact]
    public void GivenTemplateWithEscapedContent_WhenParsed_ThenUnescapesCorrectly()
    {
        // Arrange
        var template = "Use {{name}} for {name}";

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Single(definition.Tokens);
        Assert.Contains("{name}", definition.Tokens[0].Preamble, StringComparison.Ordinal);
    }
}
