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
    public void TestValidateValueWhenNumericInteger()
    {
        var result = validator.IsValid("100");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenNumericFloat()
    {
        var result = validator.IsValid("10.0");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenNotNumeric()
    {
        var result = validator.IsValid("hello world");

        Assert.False(result);
    }

    [Fact]
    public void TestValidateValueWhenNull()
    {
        var result = validator.IsValid(null);

        Assert.False(result);
    }

    [Fact]
    public void TestNotValidator()
    {
        var pattern = @"Age: { Age : !IsNumeric }";
        var input = "Age: ten";

        var result = new Tokenizer().Tokenize(pattern, input);

        Assert.Equal("ten", result.First("Age"));
    }

    [Fact]
    public void TestForDocumentation()
    {
        var template = "Age: { Age : IsNumeric }";
        var input = "Age: Ten  Age: 10";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("10", result.First("Age"));
    }
}