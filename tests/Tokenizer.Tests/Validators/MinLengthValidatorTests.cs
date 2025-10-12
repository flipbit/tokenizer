using Xunit;
using Tokens.Exceptions;

namespace Tokens.Validators;

public class MinLengthValidatorTests
{
    private readonly MinLengthValidator validator = new();

    [Fact]
    public void TestValidMinimumLengthWhenValid()
    {
        var result = validator.IsValid("hello", "3");

        Assert.True(result);
    }

    [Fact]
    public void TestValidMinimumLengthWhenInvalid()
    {
        var result = validator.IsValid("hello world", "255");

        Assert.False(result);
    }

    [Fact]
    public void TestValidMinimumLengthWhenNoParameters()
    {
        Assert.Throws<ValidationException>(() => validator.IsValid("hello world"));
    }

    [Fact]
    public void TestValidMinimumLengthWhenParametersNotAnInteger()
    {
        Assert.Throws<ValidationException>(() => validator.IsValid("hello world", "hello"));
    }

    [Fact]
    public void TestForDocumentation()
    {
        var template = "Zip: { ZipCode : MinLength(5), EOL }";
        var input = "Zip: 123\nZip: 45678";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("45678", result.First("ZipCode"));
    }
}