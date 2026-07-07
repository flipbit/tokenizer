using Xunit;

#pragma warning disable MA0048 // Scenario test: AstTemplateDefinitionParser.RealWorld.Tests.cs
namespace Tokens.Compilation.Parsing;

/// <summary>
/// Tests using real-world template patterns
/// </summary>
public class RealWorldTemplateTests
{
    private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

    [Fact]
    public void GivenWhoisTemplate_WhenParsing_ThenParsesCorrectly()
    {
        // Arrange - Real WHOIS response template
        var template = """
                       Domain Name: {domain_name$}
                       Registry Domain ID: {registry_domain_id$}
                       Registrar WHOIS Server: {registrar_whois_server$}
                       Registrar URL: {registrar_url$}
                       Updated Date: {updated_date$:ToDateTime(yyyy-MM-ddTHH:mm:ssZ)}
                       Creation Date: {creation_date$:ToDateTime(yyyy-MM-ddTHH:mm:ssZ)}
                       Registrar Registration Expiration Date: {expiration_date$:ToDateTime(yyyy-MM-ddTHH:mm:ssZ)}
                       """;

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal(7, definition.Tokens.Count);
        Assert.All(definition.Tokens, token => Assert.True(token.TerminateOnNewLine));

        var dateTokens = definition.Tokens.Where(t => t.Name.Contains("date", StringComparison.Ordinal)).ToList();
        Assert.Equal(3, dateTokens.Count);
        Assert.All(dateTokens, token => Assert.Single(token.Decorators));
    }

    [Fact]
    public void GivenLogFormatTemplate_WhenParsing_ThenParsesCorrectly()
    {
        // Arrange - Apache log format
        var template = """{remote_addr} - {remote_user?="-"} [{time_local$}] {status}""";

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal(4, definition.Tokens.Count);
        Assert.Equal("remote_user", definition.Tokens[1].Name);
        Assert.True(definition.Tokens[1].IsOptional);
        Assert.Equal("-", definition.Tokens[1].Value);
    }

    [Fact]
    public void GivenUrlTemplate_WhenParsing_ThenParsesCorrectly()
    {
        // Arrange - URL pattern
        var template = "https://{domain}/{path*}/{file?}";

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal(3, definition.Tokens.Count);
        Assert.Equal("domain", definition.Tokens[0].Name);
        Assert.Equal("path", definition.Tokens[1].Name);
        Assert.True(definition.Tokens[1].IsRepeating);
        Assert.Equal("file", definition.Tokens[2].Name);
        Assert.True(definition.Tokens[2].IsOptional);
    }

    [Fact]
    public void GivenEmailTemplate_WhenParsing_ThenParsesCorrectly()
    {
        // Arrange - Email format
        var template = """
                       From: {from_name} <{from_email:IsEmail}>
                       To: {to_name} <{to_email:IsEmail}>
                       Subject: {subject$}

                       {body*}
                       """;

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal(6, definition.Tokens.Count);

        var emailTokens = definition.Tokens.Where(t => t.Name.Contains("email", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, emailTokens.Count);
        Assert.All(emailTokens, token =>
        {
            Assert.Single(token.Decorators);
            Assert.Equal("IsEmail", token.Decorators[0].Name);
        });
    }

    [Fact]
    public void GivenComplexMultiLineTemplate_WhenParsing_ThenPreservesStructure()
    {
        // Arrange
        var template = """
                       ---
                       name: Multi-line Template
                       tag: complex
                       set: Type = Record
                       ---

                       Record Start
                       ============

                       Field 1: {field1!}
                       Field 2: {field2?}
                       Field 3: {field3*}

                       Nested Section:
                           {nested_field$}
                       """;

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal("Multi-line Template", definition.Name);
        Assert.Single(definition.Tags);
        Assert.Equal(5, definition.Tokens.Count); // Type + 4 fields

        Assert.Equal("Type", definition.Tokens[0].Name);
        Assert.True(definition.Tokens[0].IsFrontMatterToken);

        Assert.Equal("field1", definition.Tokens[1].Name);
        Assert.True(definition.Tokens[1].IsRequired);

        Assert.Equal("field2", definition.Tokens[2].Name);
        Assert.True(definition.Tokens[2].IsOptional);

        Assert.Equal("field3", definition.Tokens[3].Name);
        Assert.True(definition.Tokens[3].IsRepeating);

        Assert.Equal("nested_field", definition.Tokens[4].Name);
        Assert.True(definition.Tokens[4].TerminateOnNewLine);
    }

    [Fact]
    public void GivenCsvTemplate_WhenParsing_ThenParsesCorrectly()
    {
        // Arrange - CSV header line
        var template = "{name},{email:IsEmail},{age:IsInt},{city}";

        // Act
        var definition = _parser.Parse(template);

        // Assert
        Assert.Equal(4, definition.Tokens.Count);
        Assert.Equal("email", definition.Tokens[1].Name);
        Assert.Single(definition.Tokens[1].Decorators);
        Assert.Equal("age", definition.Tokens[2].Name);
        Assert.Single(definition.Tokens[2].Decorators);
    }

    [Fact]
    public void GivenJsonLikeTemplate_WhenParsing_ThenParsesCorrectly()
    {
        // Arrange - JSON-like structure
        var template = """
                       {{
                         "id": {id:IsInt},
                         "name": "{name}",
                         "active": {active:IsBool}
                       }}
                       """;

        // Act
        var definition = _parser.Parse(template);

        // Assert — 3 named tokens (id, name, active) + 1 trailing token for the closing }}
        // Note: quotes in preamble text are literal (not string delimiters), so "{name}"
        // correctly creates a token for 'name' with the surrounding quotes in the preamble
        var namedTokens = definition.Tokens.Where(t => !string.IsNullOrEmpty(t.Name)).ToList();
        Assert.Equal(3, namedTokens.Count);
        Assert.Equal("id", namedTokens[0].Name);
        Assert.Equal("name", namedTokens[1].Name);
        Assert.Equal("active", namedTokens[2].Name);
        Assert.Contains("{", definition.Tokens[0].Preamble, StringComparison.Ordinal);
    }
}
