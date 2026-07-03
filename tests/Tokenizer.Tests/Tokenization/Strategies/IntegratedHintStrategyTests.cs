using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization.Strategies;

public class IntegratedHintStrategyTests
{
    private readonly IntegratedHintStrategy _strategy = new();

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
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithNoHints_WhenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Act
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenRequiredHintAndMatchingPreamble_WhenOnTokenMatchedThenPostProcess_ThenReturnsFalse()
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
        _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        var token = new TokenBuilder()
            .WithName("TestToken")
            .WithPreamble("Hello World")
            .Build();

        // Act
        _strategy.OnTokenMatched(token);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenRequiredHintAndNoMatchingToken_WhenPostProcess_ThenReturnsTrue()
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
        _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Act - no OnTokenMatched calls
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenOptionalHintOnly_WhenNoMatchingToken_ThenPostProcessReturnsFalse()
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
        _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Act - no OnTokenMatched calls
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenAnyTemplate_WhenPreProcess_ThenAlwaysReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Missing")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Assert - PreProcess never skips tokenization for single-pass strategies
        Assert.False(hintsMissing);
    }
}
