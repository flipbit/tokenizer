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
            _engine.ProcessTokenization(null!, value, context, result, NullDiagnosticCollector.Instance));
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
            _engine.ProcessTokenization(template, value, null!, result, NullDiagnosticCollector.Instance));
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
            _engine.ProcessTokenization(template, value, context, null!, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenReadOnlyTargetObject_WhenProcessingTokenization_ThenThrowsArgumentException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("test"));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        var readOnlyTarget = new ReadOnlyTarget("test");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _engine.ProcessTokenization(template, readOnlyTarget, context, result, NullDiagnosticCollector.Instance));

        Assert.Contains("no settable properties", ex.Message);
    }

    [Fact]
    public void GivenTokenizationWithNoMatch_WhenProcessingTokenization_ThenResultHasNoExceptionsAndFails()
    {
        // Arrange — template expects a required token whose preamble won't be found in input
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithContent("{TestToken}")
                .WithName("TestToken")
                .WithPreamble("PREAMBLE_NOT_IN_INPUT:")
                .WithRequired()
                .Build())
            .Build();

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("no match here"));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Empty(result.Exceptions);
        Assert.False(result.Success);
    }

    private sealed class ReadOnlyTarget
    {
        public ReadOnlyTarget(string name) { Name = name; }
        public string Name { get; }
    }
}
