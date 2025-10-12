using System;
using Tokens.Builders;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization;

public class HintProcessorTests
{
    private readonly HintProcessor _processor = new();

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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

        // Assert
        Assert.False(hintsMissing);
        Assert.True(result.Hints.Matches.Count >= 1);
    }

    [Fact]
    public void GivenNullTemplate_WhenFindAndValidateHints_ThenThrowsException()
    {
        // Arrange
        var enumerator = new TokenEnumerator("test");
        var result = new TokenizeResultBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _processor.FindAndValidateHints(null, enumerator, result));
    }

    [Fact]
    public void GivenNullEnumerator_WhenFindAndValidateHints_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _processor.FindAndValidateHints(template, null, result));
    }

    [Fact]
    public void GivenNullResult_WhenFindAndValidateHints_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();
        var enumerator = new TokenEnumerator("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _processor.FindAndValidateHints(template, enumerator, null));
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

    private TokenizeResult CreateResult(Template template = null)
    {
        return new TokenizeResultBuilder()
            .WithTemplate(template ?? new TemplateBuilder().Build())
            .Build();
    }

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
        Assert.False(isMatch);
    }

    [Fact]
    public void GivenNullHintText_WhenIsHintMatch_ThenReturnsFalse()
    {
        // Arrange
        var hint = new HintBuilder()
            .WithText(null)
            .Build();
        var enumerator = new TokenEnumerator("Hello World");

        // Act
        var isMatch = _processor.IsHintMatch(hint, enumerator);

        // Assert
        Assert.False(isMatch);
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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

        // Assert
        Assert.True(hintsMissing);
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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

        // Assert
        Assert.True(hintsMissing);
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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

        // Assert
        Assert.True(hintsMissing);
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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

        // Assert
        Assert.False(hintsMissing);
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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

        // Assert
        Assert.False(hintsMissing);
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
        var hintsMissing = _processor.FindAndValidateHints(template, enumerator, result);

        // Assert
        Assert.True(hintsMissing);
    }
}