using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class EndsWithValidatorTests : Tests.TokenizerTestBase
{
    public EndsWithValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly EndsWithValidator validator = new();

    [Fact]
    public void GivenStringThatEndsWithSuffix_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello world";
        var suffix = "world";

        // Act
        var result = validator.IsValid(input, suffix);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenStringThatDoesNotEndWithSuffix_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";
        var suffix = "hello";

        // Act
        var result = validator.IsValid(input, suffix);

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
        string input = null!;

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
    public void GivenTemplateWithEndsWithValidator_WhenInputHasMultipleValues_ThenUsesFirstMatchingValue()
    {
        // Arrange
        var template = "Email: { AdminEmail : EndsWith('admin.com') }";
        var input = "Email: alice@customer.com Email: bob@admin.com";

        // Act
        var result = Tokenizer.Create().Tokenize(template, input);

        // Assert
        Assert.Equal("bob@admin.com", result.First("AdminEmail"));
    }
}
