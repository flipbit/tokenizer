using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsInRangeValidatorTests : TokenizerTestBase
{
    public IsInRangeValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsInRangeValidator validator = new();

    [Fact]
    public void GivenValueInRange_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "50";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValueAtMinBoundary_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "1";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValueAtMaxBoundary_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "100";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValueBelowRange_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "0";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValueAboveRange_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "101";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenDecimalValueInRange_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "50.5";

        // Act
        var result = validator.IsValid(input, "0.0", "100.0");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNegativeValueInRange_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "-5";

        // Act
        var result = validator.IsValid(input, "-10", "10");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNonNumericValue_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "abc";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(null!, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(string.Empty, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenMissingArgs_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid("50"));
    }

    [Fact]
    public void GivenOnlyOneArg_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid("50", "1"));
    }

    [Fact]
    public void GivenNonNumericMinArg_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid("50", "abc", "100"));
    }

    [Fact]
    public void GivenNonNumericMaxArg_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid("50", "1", "abc"));
    }

    [Fact]
    public void GivenTemplateWithIsInRangeValidator_WhenInputIsInRange_ThenExtractsValue()
    {
        // Arrange
        var template = "Age: { Age : IsInRange(1, 120) }";
        var input = "Age: 25";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("25", result.First("Age"));
    }
}
