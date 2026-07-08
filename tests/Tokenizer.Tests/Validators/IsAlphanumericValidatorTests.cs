using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsAlphanumericValidatorTests : TokenizerTestBase
{
    public IsAlphanumericValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsAlphanumericValidator _validator = new();

    [Fact]
    public void GivenAlphanumericString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "abc123";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenAlphaOnlyString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "abcdef";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNumericOnlyString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "123456";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenStringWithSpaces_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "abc 123";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenStringWithSpecialChars_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "abc-123!";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = _validator.IsValid(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = _validator.IsValid(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithIsAlphanumericValidator_WhenInputIsAlphanumeric_ThenExtractsValue()
    {
        // Arrange
        var template = "Code: { Code : IsAlphanumeric }";
        var input = "Code: ABC123";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("ABC123", result.Matches.First(m => string.Equals(m.Token.Name, "Code", StringComparison.Ordinal)).Value);
    }
}
