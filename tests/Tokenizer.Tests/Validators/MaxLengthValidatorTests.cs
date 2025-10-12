using Xunit;
using Tokens.Exceptions;

namespace Tokens.Validators;

public class MaxLengthValidatorTests
{
    private readonly MaxLengthValidator validator = new();

    [Fact]
    public void TestValidMaximumLengthWhenValid()
    {
        var result = validator.IsValid("hello", "100");

        Assert.True(result);
    }

    [Fact]
    public void TestValidMaximumLengthWhenInvalid()
    {
        var result = validator.IsValid("hello world", "5");

        Assert.False(result);
    }

    [Fact]
    public void TestValidMaximumLengthWhenNoParameters()
    {
        Assert.Throws<ValidationException>(() => validator.IsValid("hello world"));
    }

    [Fact]
    public void TestValidMaximumLengthWhenParametersNotAnInteger()
    {
        Assert.Throws<ValidationException>(() => validator.IsValid("hello world", "hello"));
    }

    [Fact]
    public void TestForDocumentation()
    {
        var template = "Zip: { ZipCode : MaxLength(5) }";
        var input = "Zip: 123456  Zip: 78912";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("78912", result.First("ZipCode"));
    }
}