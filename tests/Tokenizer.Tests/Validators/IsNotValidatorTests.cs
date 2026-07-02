using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsNotValidatorTests : TokenizerTestBase
{
    public IsNotValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsNotValidator validator = new();

    [Fact]
    public void GivenStringThatMatchesExcludedValue_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";
        var excludedValue = "hello world";

        // Act
        var result = validator.IsValid(input, excludedValue);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenStringThatDoesNotMatchExcludedValue_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello world";
        var excludedValue = "hello";

        // Act
        var result = validator.IsValid(input, excludedValue);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        string input = null!;
        var excludedValue = "hello";

        // Act
        var result = validator.IsValid(input, excludedValue);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = string.Empty;
        var excludedValue = "hello";

        // Act
        var result = validator.IsValid(input, excludedValue);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenTemplateWithIsNotValidator_WhenInputHasExcludedThenValidValue_ThenUsesValidValue()
    {
        // Arrange
        var template = "Address: { Address : IsNot('N/A'), EOL }";
        var input = "Address: N/A\nAddress: 10 Acacia Avenue";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("10 Acacia Avenue", result.First("Address"));
    }
}
