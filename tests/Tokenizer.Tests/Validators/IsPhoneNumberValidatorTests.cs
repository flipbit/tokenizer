using Xunit;

namespace Tokens.Validators;

public class IsPhoneNumberValidatorTests
{
    private readonly IsPhoneNumberValidator validator = new();

    [Fact]
    public void TestValidateValueWhenValidUk()
    {
        var result = validator.IsValid("01603 123123");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenValidUkWithCountryCode()
    {
        var result = validator.IsValid("+44 (0) 1603 123123");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenValidWithDots()
    {
        var result = validator.IsValid("+44.1603.123123");

        Assert.True(result);
    }
            
    [Fact]
    public void TestValidateValueWhenValidWithDashes()
    {
        var result = validator.IsValid("+44-1603-123123");

        Assert.True(result);
    }
    [Fact]
    public void TestValidateValueWhenValidUkWithNoAreaCode()
    {
        var result = validator.IsValid("123123");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenInvalid()
    {
        var result = validator.IsValid("hello world");

        Assert.False(result);
    }

    [Fact]
    public void TestValidateValueWhenInvalidWithNumber()
    {
        var result = validator.IsValid("hello world 0123456789");

        Assert.False(result);
    }

    [Fact]
    public void TestValidateValueWhenTooShort()
    {
        var result = validator.IsValid("12345");

        Assert.False(result);
    }

    [Fact]
    public void TestValidateValueWhenNull()
    {
        var result = validator.IsValid(null);

        Assert.False(result);
    }

    [Fact]
    public void TestValidateValueWhenEmpty()
    {
        var result = validator.IsValid(string.Empty);

        Assert.False(result);
    }

    [Fact]
    public void TestForDocumentation()
    {
        var template = "Phone: { Phone : IsPhoneNumber }";
        var input = "Phone: Disconnected  Phone: +44 (0) 1603 555-1234";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("+44 (0) 1603 555-1234", result.First("Phone"));
    }
}