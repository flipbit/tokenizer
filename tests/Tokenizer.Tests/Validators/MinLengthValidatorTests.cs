using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class MinLengthValidatorTests : TokenizerTestBase
{
    public MinLengthValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly MinLengthValidator _validator = new();

    [Fact]
    public void GivenStringMeetingMinimumLength_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello";
        var minLength = "3";

        // Act
        var result = _validator.IsValid(input, minLength);

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
        var result = _validator.IsValid(input, minLength);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValidatorWithNoParameters_WhenValidating_ThenThrowsValidationException()
    {
        // Arrange
        var input = "hello world";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _validator.IsValid(input));
    }

    [Fact]
    public void GivenValidatorWithNonIntegerParameter_WhenValidating_ThenThrowsValidationException()
    {
        // Arrange
        var input = "hello world";
        var invalidParameter = "hello";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _validator.IsValid(input, invalidParameter));
    }

    [Fact]
    public void GivenTemplateWithMinLengthValidator_WhenInputHasInvalidThenValidValue_ThenUsesValidValue()
    {
        // Arrange
        var template = "Zip: { ZipCode : MinLength(5), EOL }";
        var input = "Zip: 123\nZip: 45678";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("45678", result.Matches.First(m => string.Equals(m.Token.Name, "ZipCode", StringComparison.Ordinal)).Value);
    }
}
