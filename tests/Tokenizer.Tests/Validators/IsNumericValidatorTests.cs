using System;
using Xunit;

namespace Tokens.Validators;

public class IsNumericValidatorTests
{
    private readonly IsNumericValidator validator;

    public IsNumericValidatorTests()
    {
        SerilogConfig.Init();

        validator = new IsNumericValidator();
    }

    [Fact]
    public void GivenNumericIntegerString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "100";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNumericFloatString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "10.0";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNonNumericString_WhenValidating_ThenReturnsFalse()
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
    public void GivenPatternWithNotNumericValidator_WhenInputIsNonNumeric_ThenExtractsValue()
    {
        // Arrange
        var pattern = @"Age: { Age : !IsNumeric }";
        var input = "Age: ten";

        // Act
        var result = new Tokenizer().Tokenize(pattern, input);

        // Assert
        Assert.Equal("ten", result.First("Age"));
    }

    [Fact]
    public void GivenTemplateWithNumericValidator_WhenInputHasInvalidThenValidNumber_ThenUsesValidNumber()
    {
        // Arrange
        var template = "Age: { Age : IsNumeric }";
        var input = "Age: Ten  Age: 10";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("10", result.First("Age"));
    }
}