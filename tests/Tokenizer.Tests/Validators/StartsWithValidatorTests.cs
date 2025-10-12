using Tokens.Exceptions;
using Xunit;

namespace Tokens.Validators;

public class StartsWithValidatorTests
{
    private readonly StartsWithValidator validator = new();

    [Fact]
    public void GivenStringThatStartsWithPrefix_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello world";
        var prefix = "hello";

        // Act
        var result = validator.IsValid(input, prefix);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenStringThatDoesNotStartWithPrefix_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";
        var prefix = "world";

        // Act
        var result = validator.IsValid(input, prefix);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValidatorWithMissingArgument_WhenValidating_ThenThrowsTokenizerException()
    {
        // Arrange
        var input = "hello world";

        // Act & Assert
        Assert.Throws<TokenizerException>(() => validator.IsValid(input));
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        string input = null;

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithStartsWithValidator_WhenInputHasMultipleValues_ThenUsesFirstMatchingValue()
    {
        // Arrange
        var template = "Ip: { InternalIpAddress : StartsWith('192') }";
        var input = "Ip: 80.34.123.45  Ip: 192.168.1.1";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("192.168.1.1", result.First("InternalIpAddress"));
    }
}