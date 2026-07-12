using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization.Strategies;

public class UpfrontHintStrategyTests
{
    private readonly UpfrontHintStrategy _strategy = new();

    [Fact]
    public void GivenTemplateWithNoHints_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullTokenizationDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithRequiredHintPresent_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Hello")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullTokenizationDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithRequiredHintMissing_WhenPreProcess_ThenReturnsTrue()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Goodbye")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullTokenizationDiagnosticCollector.Instance);

        // Assert
        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithOptionalHintMissing_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Goodbye")
                .WithOptional()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullTokenizationDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenEnumerator_WhenPreProcess_ThenEnumeratorIsNotConsumed()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Hello")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _strategy.PreProcess(template, enumerator, "Hello World", result, NullTokenizationDiagnosticCollector.Instance);

        // Assert - enumerator should still be at the beginning
        Assert.Equal('H', enumerator.Peek());
    }

    [Fact]
    public void GivenNullRawInputWithHints_WhenPreProcess_ThenThrowsArgumentNullException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Hello")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act & Assert — sync path always provides rawInput; null is a programming error
        Assert.Throws<ArgumentNullException>(() =>
            _strategy.PreProcess(template, enumerator, rawInput: null, result, NullTokenizationDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenBuffer_WhenOnBufferFilled_ThenDoesNotThrow()
    {
        var buffer = "Hello World".ToCharArray();
        var exception = Record.Exception(() => _strategy.OnBufferFilled(buffer, buffer.Length));
        Assert.Null(exception);
    }

    [Fact]
    public void GivenResult_WhenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }
}
