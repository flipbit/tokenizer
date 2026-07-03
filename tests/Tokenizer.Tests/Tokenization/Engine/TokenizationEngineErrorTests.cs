using Tokens.Builders;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Tokenization.Engine;

/// <summary>
/// Tests for TokenizationEngine error handling and validation
/// </summary>
public class TokenizationEngineErrorTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenNullReader_WhenInitializingContext_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
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
        Assert.Throws<ArgumentNullException>(() => context.Initialize((System.IO.TextReader)null!));
    }

    [Fact]
    public void GivenNullTemplate_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("test"));

        var result = new TokenizeResultBuilder().Build();
        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.ProcessTokenization(null!, 4, value, context, result, NullDiagnosticCollector.Instance));
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
            _engine.ProcessTokenization(template, 4, value, null!, result, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenNullResult_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("test"));
        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.ProcessTokenization(template, 4, value, context, null!, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenExceptionDuringTokenAssignment_WhenTryAssignCandidateTokens_ThenHandlesException()
    {
        // Arrange
        var candidates = new CandidateTokenList();
        var value = new { Name = "" };
        var replacement = new System.Text.StringBuilder("test");
        var options = new TokenizerOptions();
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
        var assigned = _engine.TryAssignCandidateTokens(candidates, value, replacement, options, replacementLocation, result, template, matchIds, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(assigned);
        Assert.NotNull(result.Exceptions);
    }
}
