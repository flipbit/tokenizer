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
    public void GivenNullTemplate_WhenCreatingSession_ThenThrowsException()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();
        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.CreateSession(null!, value, result, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenNullResult_WhenCreatingSession_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();

        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _engine.CreateSession(template, value, null!, NullDiagnosticCollector.Instance));
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

        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        var readOnlyTarget = new ReadOnlyTarget("test");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _engine.CreateSession(template, readOnlyTarget, result, NullDiagnosticCollector.Instance));

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
        var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

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
