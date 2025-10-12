using Xunit;

namespace Tokens.Validators;

public class IsEmailValidatorTests
{
    private readonly IsEmailValidator validator = new();

    [Fact]
    public void TestValidateValueWhenValid()
    {
        var result = validator.IsValid("hello@example.com");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenInvalid()
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
    public void TestValidateValueWhenEmpty()
    {
        var result = validator.IsValid(string.Empty);

        Assert.False(result);
    }

    [Fact]
    public void TestForDocumentation()
    {
        var template = "Email: { Email : IsEmail }";
        var input = "Email: webmaster at host.com Email: hello@domain.com";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("hello@domain.com", result.First("Email"));
    }
}