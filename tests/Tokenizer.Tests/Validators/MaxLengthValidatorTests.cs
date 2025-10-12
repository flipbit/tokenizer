using Xunit;
using Tokens.Exceptions;

namespace Tokens.Validators;

public class MaxLengthValidatorTests
{
    private readonly MaxLengthValidator validator = new();

    [Fact]
    public void GivenStringWithinMaximumLength_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello";
        var maxLength = "100";

        // Act
        var result = validator.IsValid(input, maxLength);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenStringExceedingMaximumLength_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";
        var maxLength = "5";

        // Act
        var result = validator.IsValid(input, maxLength);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValidatorWithNoParameters_WhenValidating_ThenThrowsValidationException()
    {
        // Arrange
        var input = "hello world";

        // Act & Assert
        Assert.Throws<ValidationException>(() => validator.IsValid(input));
    }

    [Fact]
    public void GivenValidatorWithNonIntegerParameter_WhenValidating_ThenThrowsValidationException()
    {
        // Arrange
        var input = "hello world";
        var invalidParameter = "hello";

        // Act & Assert
        Assert.Throws<ValidationException>(() => validator.IsValid(input, invalidParameter));
    }

    [Fact]
    public void GivenTemplateWithMaxLengthValidator_WhenInputHasMultipleValues_ThenUsesFirstValidValue()
    {
        // Arrange
        var template = "Zip: { ZipCode : MaxLength(5) }";
        var input = "Zip: 123456  Zip: 78912";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("78912", result.First("ZipCode"));
    }
}