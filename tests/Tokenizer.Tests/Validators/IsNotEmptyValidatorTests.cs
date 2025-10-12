using Xunit;

namespace Tokens.Validators;

public class IsNotEmptyValidatorTests
{
    private readonly IsNotEmptyValidator validator = new();

    [Fact]
    public void GivenNonEmptyString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
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
    public void GivenTemplateWithIsNotEmptyValidator_WhenFirstMatchIsEmpty_ThenUsesSecondMatch()
    {
        // Arrange
        var template = "Middle Name: { MiddleName : IsNotEmpty, EOL }";
        var input = "Middle Name:\nMiddle Name: Charles";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("Charles", result.First("MiddleName"));
    }
}