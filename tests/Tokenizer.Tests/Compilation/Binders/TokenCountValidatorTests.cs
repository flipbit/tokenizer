using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TokenCountValidatorTests
{
    [Fact]
    public void GivenTokenCountExceedingMax_WhenValidating_ThenThrowsParsingException()
    {
        var options = new TokenizerOptions { MaxTokenCount = 2 };
        var template = new TemplateBuilder()
            .WithOptions(options)
            .WithTokens(
                new Token("a", "A", "", new FileLocation()),
                new Token("b", "B", "", new FileLocation()),
                new Token("c", "C", "", new FileLocation()))
            .Build();

        var ex = Assert.Throws<ParsingException>(() => TokenCountValidator.Validate(template, options));
        Assert.Contains("exceeding maximum", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenTokenCountAtMax_WhenValidating_ThenDoesNotThrow()
    {
        var options = new TokenizerOptions { MaxTokenCount = 2 };
        var template = new TemplateBuilder()
            .WithOptions(options)
            .WithTokens(
                new Token("a", "A", "", new FileLocation()),
                new Token("b", "B", "", new FileLocation()))
            .Build();

        TokenCountValidator.Validate(template, options);
    }

    [Fact]
    public void GivenMaxTokenCountDisabled_WhenValidating_ThenDoesNotThrow()
    {
        var options = new TokenizerOptions { MaxTokenCount = 0 };
        var template = new TemplateBuilder()
            .WithOptions(options)
            .WithTokens(
                new Token("a", "A", "", new FileLocation()),
                new Token("b", "B", "", new FileLocation()),
                new Token("c", "C", "", new FileLocation()))
            .Build();

        TokenCountValidator.Validate(template, options);
    }
}
