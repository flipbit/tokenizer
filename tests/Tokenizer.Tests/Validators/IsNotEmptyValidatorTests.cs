using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsNotEmptyValidatorTests : TokenizerTestBase
{
    public IsNotEmptyValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsNotEmptyValidator _validator = new();

    [Fact]
    public void GivenNonEmptyString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
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
    public void GivenTemplateWithIsNotEmptyValidator_WhenFirstMatchIsEmpty_ThenUsesSecondMatch()
    {
        // Arrange
        var template = "Middle Name: { MiddleName : IsNotEmpty, EOL }";
        var input = "Middle Name:\nMiddle Name: Charles";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("Charles", result.First("MiddleName"));
    }
}
