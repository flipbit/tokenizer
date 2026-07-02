using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Tokenization;
using Xunit;
using Tokens.Diagnostics;

namespace Tokens.Tests.Tokenization.Engine;

/// <summary>
/// Tests for basic/happy path TokenizationEngine scenarios
/// </summary>
public class TokenizationEngineBasicTests
{
    private readonly TokenizationEngine _engine = new();

    private class Person
    {
        public string FirstName { get; set; } = null!;
    }

    [Fact]
    public void GivenValidInput_WhenProcessingTokenization_ThenProcessesSuccessfully()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("First Name: {FirstName}");

        var context = new TokenizationContext();
        // Don't initialize the context here - ProcessTokenization will do it

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new Person { FirstName = "Alice" };

        // Act - Follow the same pattern as the original Tokenizer
        // Initialize context for hint processing
        _engine.ProcessTokenization(template, "First Name: Alice", value, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count > 0);
    }

    [Fact]
    public void GivenTemplateWithNoTokens_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Hello World"); // Template with no tokens

        var context = new TokenizationContext();
        context.Initialize("Hello World");

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, "Hello World", null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Tokens.Matches);
    }

    private Template CreateTemplate(string name = "TestTemplate", string content = "Hello {Name}")
    {
        return new TemplateBuilder()
            .WithName(name)
            .WithContent(content)
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();
    }

    private TokenizationContext CreateContext(string input = "Hello World")
    {
        var context = new TokenizationContext();
        context.Initialize(input);
        return context;
    }

    private TokenizeResult CreateResult(Template? template = null)
    {
        return new TokenizeResultBuilder()
            .WithTemplate(template ?? CreateTemplate())
            .Build();
    }
}
