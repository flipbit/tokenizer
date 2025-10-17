using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsEmailValidatorTests : Tests.TokenizerTestBase
{
    public IsEmailValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsEmailValidator validator = new();

    [Fact]
    public void GivenValidEmailAddress_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello@example.com";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenInvalidEmailAddress_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
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
    public void GivenTemplateWithEmailValidator_WhenInputHasInvalidThenValidEmail_ThenUsesValidEmail()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: webmaster at host.com Email: hello@domain.com";

        // Act
        var result = Tokenizer.Create().Tokenize(template, input);

        // Assert
        Assert.Equal("hello@domain.com", result.First("Email"));
    }
}
