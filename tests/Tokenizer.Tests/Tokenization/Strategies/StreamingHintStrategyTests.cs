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
        var template = new TemplateBuilder().WithName("TestTemplate").Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        var hintsMissing = _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenAnyTemplate_WhenPreProcess_ThenAlwaysReturnsFalse()
    {
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Missing").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        var hintsMissing = _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithNoHints_WhenPostProcess_ThenReturnsFalse()
    {
        var template = new TemplateBuilder().WithName("TestTemplate").Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        var hintsMissing = _strategy.PostProcess(result);

        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenRequiredHintInBuffer_WhenOnBufferFilledThenPostProcess_ThenReturnsFalse()
    {
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Hello").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenRequiredHintNotInBuffer_WhenPostProcess_ThenReturnsTrue()
    {
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Goodbye").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenOptionalHintMissing_WhenPostProcess_ThenReturnsFalse()
    {
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Goodbye").WithOptional().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenHintSpanningTwoChunks_WhenOnBufferFilledTwice_ThenHintIsFound()
    {
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("Hello").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        var chunk1 = "Some text Hel".ToCharArray();
        _strategy.OnBufferFilled(chunk1, chunk1.Length);
        var chunk2 = "lo more text".ToCharArray();
        _strategy.OnBufferFilled(chunk2, chunk2.Length);

        var hintsMissing = _strategy.PostProcess(result);
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenHintInSecondChunk_WhenOnBufferFilledTwice_ThenHintIsFound()
    {
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("World").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        var chunk1 = "Hello ".ToCharArray();
        _strategy.OnBufferFilled(chunk1, chunk1.Length);
        var chunk2 = "World".ToCharArray();
        _strategy.OnBufferFilled(chunk2, chunk2.Length);

        var hintsMissing = _strategy.PostProcess(result);
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenMultipleHints_WhenSomeFoundSomeMissing_ThenRequiredMissingReturnsTrue()
    {
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(
                new HintBuilder().WithText("Hello").WithRequired().Build(),
                new HintBuilder().WithText("Missing").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenBufferLargerThanCount_WhenOnBufferFilled_ThenOnlyCountCharsScanned()
    {
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder().WithText("World").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance);

        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, 5); // Only scan "Hello"
        var hintsMissing = _strategy.PostProcess(result);

        Assert.True(hintsMissing);
    }
}
