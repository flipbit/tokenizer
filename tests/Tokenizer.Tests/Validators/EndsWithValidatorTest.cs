using Tokens.Exceptions;
using Xunit;

namespace Tokens.Validators;

public class EndsWithValidatorTest
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
}