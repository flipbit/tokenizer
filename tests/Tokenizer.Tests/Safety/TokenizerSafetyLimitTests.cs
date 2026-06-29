using System;
using Tokens;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Tests.Safety;

public class TokenizerSafetyLimitTests
{
    [Fact]
    public void GivenInputExceedingMaxLength_WhenTokenizing_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxInputLength = 100;
        var tokenizer = Tokenizer.Create(options);
        var input = new string('x', 101);

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() =>
            tokenizer.Tokenize("Name: {Name}", input));
        Assert.Contains("101", ex.Message);
        Assert.Contains("100", ex.Message);
        Assert.Contains("MaxInputLength", ex.Message);
    }

    [Fact]
    public void GivenInputAtMaxLength_WhenTokenizing_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxInputLength = 100;
        var tokenizer = Tokenizer.Create(options);
        var input = "Name: " + new string('x', 94);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", input);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenMaxInputLengthDisabled_WhenTokenizingLargeInput_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxInputLength = 0;
        var tokenizer = Tokenizer.Create(options);
        var input = "Name: " + new string('x', 200_000);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", input);

        // Assert
        Assert.NotNull(result);
    }
}
