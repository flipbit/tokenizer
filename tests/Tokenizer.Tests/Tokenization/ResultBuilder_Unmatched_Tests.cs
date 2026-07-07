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
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Hello {Name} and {Age}").Template;

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
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Hello {Name} and {Age}").Template;

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Add a match for the Name token
        var nameToken = template.Tokens.First(t => string.Equals(t.Name, "Name", StringComparison.Ordinal));
        result.Tokens.AddMatch(nameToken, "TestName", new FileLocation());

        // Act
        _builder.BuildUnmatchedTokens(template, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Age", result.Tokens.Misses[0].Name);
    }

    private static Template CreateTemplate(string name = "TestTemplate")
    {
        return new TemplateBuilder()
            .WithName(name)
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .Build())
            .Build();
    }

    private static TokenizeResult CreateResult(Template? template = null)
    {
        return new TokenizeResultBuilder()
            .WithTemplate(template ?? CreateTemplate())
            .Build();
    }
}
