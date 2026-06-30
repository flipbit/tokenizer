using System;
using Tokens.Builders;
using Tokens.Tokenization;
using Xunit;

namespace Tokens.Tests.Tokenization.Engine;

/// <summary>
/// Tests for TokenizationEngine error handling and validation
/// </summary>
public class TokenizationEngineErrorTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenEmptyInput_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.Initialize(""));
    }

    [Fact]
    public void GivenNullTemplate_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var context = new TokenizationContext();
        context.Initialize("test");

        var result = new TokenizeResultBuilder().Build();
        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.ProcessTokenization(null!, "test", value, context, result));
    }

    [Fact]
    public void GivenNullContext_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.ProcessTokenization(template, "test", value, null!, result));
    }

    [Fact]
    public void GivenNullResult_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();

        var context = new TokenizationContext();
        context.Initialize("test");
        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.ProcessTokenization(template, "test", value, context, null!));
    }

    [Fact]
    public void GivenExceptionDuringTokenAssignment_WhenTryAssignCandidateTokens_ThenHandlesException()
    {
        // Arrange
        var candidates = new CandidateTokenList();
        var value = new { Name = "" };
        var replacement = new System.Text.StringBuilder("test");
        var options = TokenizerOptions.Defaults;
        var replacementLocation = new Enumerators.FileLocation();
        var result = new TokenizeResultBuilder().Build();
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithContent("test")
                .WithName("TestToken")
                .Build())
            .Build();
        var matchIds = new System.Collections.Generic.HashSet<int>();

        // Act
        var assigned = _engine.TryAssignCandidateTokens(candidates, value, replacement, options, replacementLocation, result, template, matchIds);

        // Assert
        Assert.False(assigned);
        Assert.NotNull(result.Exceptions);
    }
}
