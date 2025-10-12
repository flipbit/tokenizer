using Xunit;
using Tokens.Exceptions;

namespace Tokens.Validators;

public class EndsWithValidatorTests
{
    private readonly EndsWithValidator validator = new();

    [Fact]
    public void TestValidateValueWhenTrue()
    {
        var result = validator.IsValid("hello world", "world");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenFalse()
    {
        var result = validator.IsValid("hello world", "hello");

        Assert.False(result);
    }

    [Fact]
    public void TestValidateValueWhenMissingArgument()
    {
        Assert.Throws<TokenizerException>(() => validator.IsValid("hello world"));
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
        var template = "Email: { AdminEmail : EndsWith('admin.com') }";
        var input = "Email: alice@customer.com Email: bob@admin.com";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("bob@admin.com", result.First("AdminEmail"));
    }
}