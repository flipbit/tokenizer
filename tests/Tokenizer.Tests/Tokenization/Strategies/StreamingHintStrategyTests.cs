using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization.Strategies;

public class StreamingHintStrategyTests
{
    private readonly StreamingHintStrategy _strategy = new();

    [Fact]
    public void GivenTemplateWithNoHints_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("TestTemplate").Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenAnyTemplate_WhenPreProcess_ThenAlwaysReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Missing").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Assert — PreProcess never skips tokenization for streaming strategies
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithNoHints_WhenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("TestTemplate").Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Act
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenRequiredHintInBuffer_WhenOnBufferFilledThenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Hello").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Act
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenRequiredHintNotInBuffer_WhenPostProcess_ThenReturnsTrue()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Goodbye").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Act
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenOptionalHintMissing_WhenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Goodbye").WithOptional().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Act
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenHintSpanningTwoChunks_WhenOnBufferFilledTwice_ThenHintIsFound()
    {
        // Arrange — hint "Hello" spans across two buffer fills: "...Hel" and "lo..."
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Hello").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Act
        var chunk1 = "Some text Hel".ToCharArray();
        _strategy.OnBufferFilled(chunk1, chunk1.Length);
        var chunk2 = "lo more text".ToCharArray();
        _strategy.OnBufferFilled(chunk2, chunk2.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenHintInSecondChunk_WhenOnBufferFilledTwice_ThenHintIsFound()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("World").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Act
        var chunk1 = "Hello ".ToCharArray();
        _strategy.OnBufferFilled(chunk1, chunk1.Length);
        var chunk2 = "World".ToCharArray();
        _strategy.OnBufferFilled(chunk2, chunk2.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenMultipleHints_WhenSomeFoundSomeMissing_ThenRequiredMissingReturnsTrue()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(
                new HintBuilder().WithText("Hello").WithRequired().Build(),
                new HintBuilder().WithText("Missing").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Act
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenBufferLargerThanCount_WhenOnBufferFilled_ThenOnlyCountCharsScanned()
    {
        // Arrange — buffer is larger but count limits what's scanned
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("World").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        // Act — "Hello" is in first 5 chars, "World" starts at index 6, but count=5 limits scan
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, 5);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert — "World" should not be found
        Assert.True(hintsMissing);
    }
}
