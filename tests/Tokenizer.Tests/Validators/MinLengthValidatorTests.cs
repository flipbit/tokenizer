using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class MinLengthValidatorTests : Tests.TokenizerTestBase
{
    public MinLengthValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly MinLengthValidator validator = new();

    [Fact]
    public void GivenStringMeetingMinimumLength_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello";
        var minLength = "3";

        // Act
        var result = validator.IsValid(input, minLength);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenStringBelowMinimumLength_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";
        var minLength = "255";

        // Act
        var result = validator.IsValid(input, minLength);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValidatorWithNoParameters_WhenValidating_ThenThrowsValidationException()
    {
        // Arrange
        var input = "hello world";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid(input));
    }

    [Fact]
    public void GivenValidatorWithNonIntegerParameter_WhenValidating_ThenThrowsValidationException()
    {
        // Arrange
        var input = "hello world";
        var invalidParameter = "hello";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid(input, invalidParameter));
    }

    [Fact]
    public void GivenTemplateWithMinLengthValidator_WhenInputHasInvalidThenValidValue_ThenUsesValidValue()
    {
        // Arrange
        var template = "Zip: { ZipCode : MinLength(5), EOL }";
        var input = "Zip: 123\nZip: 45678";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("45678", result.First("ZipCode"));
    }
}
