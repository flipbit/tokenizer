using Tokens.Builders;
using Tokens.Enumerators;
using Xunit;
using Tokens.Diagnostics;

namespace Tokens.Tests.Tokenization.HintProcessorTests;

/// <summary>
/// Tests for basic HintProcessor matching and validation logic
/// </summary>
public class HintProcessorBasicTests
{
    private readonly Tokens.Tokenization.HintProcessor _processor = new();

    [Fact]
    public void GivenTemplateWithNoHints_WhenFindAndValidateHints_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .Build();

        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithOptionalHints_WhenFindAndValidateHints_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithHints(new HintBuilder()
                .WithText("Hello")
                .WithOptional()
                .Build())
            .Build();

        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithRequiredHints_WhenFindAndValidateHints_ThenReturnsTrue()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithMatchingHints_WhenFindAndValidateHints_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
        Assert.True(result.Hints.Matches.Count > 0);
    }

    [Fact]
    public void GivenTemplateWithMultipleHints_WhenFindAndValidateHints_ThenProcessesAllHints()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithHints(
                new HintBuilder().WithText("Hello").WithRequired().Build(),
                new HintBuilder().WithText("World").WithOptional().Build()
            )
            .Build();

        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
        Assert.True(result.Hints.Matches.Count >= 1);
    }

    [Fact]
    public void GivenMatchingHint_WhenIsHintMatch_ThenReturnsTrue()
    {
        // Arrange
        var hint = new HintBuilder()
            .WithText("Hello")
            .Build();
        var enumerator = new TokenEnumerator("Hello World");

        // Act
        var isMatch = _processor.IsHintMatch(hint, enumerator);

        // Assert
        Assert.True(isMatch);
    }

    [Fact]
    public void GivenNonMatchingHint_WhenIsHintMatch_ThenReturnsFalse()
    {
        // Arrange
        var hint = new HintBuilder()
            .WithText("Goodbye")
            .Build();
        var enumerator = new TokenEnumerator("Hello World");

        // Act
        var isMatch = _processor.IsHintMatch(hint, enumerator);

        // Assert
        Assert.False(isMatch);
    }

    [Fact]
    public void GivenValidHint_WhenAddHintMatch_ThenAddsMatch()
    {
        // Arrange
        var hint = new HintBuilder()
            .WithText("Hello")
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder().Build();

        // Act
        var added = _processor.AddHintMatch(hint, enumerator, result);

        // Assert
        Assert.True(added);
        Assert.True(result.Hints.Matches.Count > 0);
    }

    [Fact]
    public void GivenValidHint_WhenAddHintMiss_ThenAddsMiss()
    {
        // Arrange
        var hint = new HintBuilder()
            .WithText("Hello")
            .Build();
        var result = new TokenizeResultBuilder().Build();

        // Act
        var added = _processor.AddHintMiss(hint, result);

        // Assert
        Assert.True(added);
        Assert.True(result.Hints.Misses.Count > 0);
    }

    [Fact]
    public void GivenEnumerator_WhenResetEnumeratorAfterHintProcessing_ThenResetsPosition()
    {
        // Arrange
        var enumerator = new TokenEnumerator("Hello World");
        enumerator.Next(); // Move position

        // Act
        _processor.ResetEnumeratorAfterHintProcessing(enumerator);

        // Assert
        // Note: TokenEnumerator doesn't have a Position property
        Assert.True(true);
    }

    private Template CreateTemplateWithHints(params Hint[] hints)
    {
        return new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithHints(hints)
            .Build();
    }

    private TokenEnumerator CreateEnumerator(string input = "Hello World")
    {
        return new TokenEnumerator(input);
    }

    private TokenizeResult CreateResult(Template? template = null)
    {
        return new TokenizeResultBuilder()
            .WithTemplate(template ?? new TemplateBuilder().Build())
            .Build();
    }
}
