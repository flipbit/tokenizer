using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization;

/// <summary>
/// Tests for HintProcessor edge cases (unicode, special chars, many hints, etc.)
/// </summary>
public class HintProcessor_EdgeCase_Tests
{
    private readonly HintProcessor _processor = new();

    [Fact]
    public void GivenEmptyHintText_WhenIsHintMatch_ThenReturnsFalse()
    {
        // Arrange
        var hint = new HintBuilder()
            .WithText("")
            .Build();
        var enumerator = new TokenEnumerator("Hello World");

        // Act
        var isMatch = _processor.IsHintMatch(hint, enumerator);

        // Assert
        Assert.False((bool)isMatch);
    }

    [Fact]
    public void GivenVeryLongHint_WhenFindAndValidateHints_ThenHandlesCorrectly()
    {
        // Arrange
        var longHint = new string('a', 1000);
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithHints(new HintBuilder()
                .WithText(longHint)
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
        Assert.True((bool)hintsMissing);
    }

    [Fact]
    public void GivenSpecialCharactersInHint_WhenFindAndValidateHints_ThenHandlesCorrectly()
    {
        // Arrange
        var specialHint = "Hello @#$%^&*()_+-=[]{}|;':\",./<>?";
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithHints(new HintBuilder()
                .WithText(specialHint)
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
        Assert.True((bool)hintsMissing);
    }

    [Fact]
    public void GivenUnicodeInHint_WhenFindAndValidateHints_ThenHandlesCorrectly()
    {
        // Arrange
        var unicodeHint = "你好世界 🌍";
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithHints(new HintBuilder()
                .WithText(unicodeHint)
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
        Assert.True((bool)hintsMissing);
    }

    [Fact]
    public void GivenManyHints_WhenFindAndValidateHints_ThenProcessesAllHints()
    {
        // Arrange
        var hints = new Hint[100];
        for (int i = 0; i < 100; i++)
        {
            hints[i] = new HintBuilder()
                .WithText($"Hint{i}")
                .WithOptional()
                .Build();
        }

        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithHints(hints)
            .Build();

        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False((bool)hintsMissing);
    }

    [Fact]
    public void GivenDuplicateHints_WhenFindAndValidateHints_ThenProcessesCorrectly()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithHints(
                new HintBuilder().WithText("Hello").WithRequired().Build(),
                new HintBuilder().WithText("Hello").WithRequired().Build()
            )
            .Build();

        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False((bool)hintsMissing);
    }

    [Fact]
    public void GivenEmptyInput_WhenFindAndValidateHints_ThenHandlesCorrectly()
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

        var enumerator = new TokenEnumerator("");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True((bool)hintsMissing);
    }
}
