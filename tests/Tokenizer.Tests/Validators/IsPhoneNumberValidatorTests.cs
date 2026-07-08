using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsPhoneNumberValidatorTests : TokenizerTestBase
{
    public IsPhoneNumberValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsPhoneNumberValidator _validator = new();

    [Fact]
    public void GivenValidUkPhoneNumber_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "01603 123123";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidUkPhoneNumberWithCountryCode_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "+44 (0) 1603 123123";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidPhoneNumberWithDots_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "+44.1603.123123";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidPhoneNumberWithDashes_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "+44-1603-123123";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }
    [Fact]
    public void GivenValidUkPhoneNumberWithNoAreaCode_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "123123";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenInvalidPhoneNumber_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenInvalidPhoneNumberWithNumbers_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world 0123456789";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenPhoneNumberTooShort_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "12345";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        string input = null!;

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithPhoneNumberValidator_WhenInputHasInvalidThenValidPhone_ThenUsesValidPhone()
    {
        // Arrange
        var template = "Phone: { Phone : IsPhoneNumber }";
        var input = "Phone: Disconnected  Phone: +44 (0) 1603 555-1234";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("+44 (0) 1603 555-1234", result.Matches.First(m => string.Equals(m.Token.Name, "Phone", StringComparison.Ordinal)).Value);
    }
}
