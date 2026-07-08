using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsIntegerValidatorTests : TokenizerTestBase
{
    public IsIntegerValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsIntegerValidator _validator = new();

    [Fact]
    public void GivenIntegerString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "42";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNegativeIntegerString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "-100";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenLargeIntegerString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "9223372036854775807"; // long.MaxValue

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenFloatString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "10.5";

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNonNumericString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello";

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
    public void GivenTemplateWithIsIntegerValidator_WhenInputIsInteger_ThenExtractsValue()
    {
        // Arrange
        var template = "Count: { Count : IsInteger }";
        var input = "Count: 42";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("42", result.Matches.First(m => string.Equals(m.Token.Name, "Count", StringComparison.Ordinal)).Value);
    }
}
