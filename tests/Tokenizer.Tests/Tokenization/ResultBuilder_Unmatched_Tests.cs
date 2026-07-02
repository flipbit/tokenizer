using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization;

/// <summary>
/// Tests for ResultBuilder BuildUnmatchedTokens logic
/// </summary>
public class ResultBuilder_Unmatched_Tests
{
    private readonly ResultBuilder _builder = new();

    [Fact]
    public void GivenTemplateWithTokens_WhenBuildUnmatchedTokens_ThenAddsUnmatchedTokens()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Hello {Name} and {Age}");

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _builder.BuildUnmatchedTokens(template, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Misses.Count > 0);
    }

    [Fact]
    public void GivenTemplateWithMatchedTokens_WhenBuildUnmatchedTokens_ThenOnlyAddsUnmatchedTokens()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Hello {Name} and {Age}");

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Add a match for the Name token
        var nameToken = template.Tokens.First(t => t.Name == "Name");
        result.Tokens.AddMatch(nameToken, "TestName", new FileLocation());

        // Act
        _builder.BuildUnmatchedTokens(template, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Age", result.Tokens.Misses[0].Name);
    }

    private Template CreateTemplate(string name = "TestTemplate", string content = "Hello {Name}")
    {
        return new TemplateBuilder()
            .WithName(name)
            .WithContent(content)
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .Build())
            .Build();
    }

    private TokenizeResult CreateResult(Template? template = null)
    {
        return new TokenizeResultBuilder()
            .WithTemplate(template ?? CreateTemplate())
            .Build();
    }
}
