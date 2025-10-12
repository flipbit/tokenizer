using Xunit;

namespace Tokens.Validators;

public class IsNotValidatorTests
{
    private readonly IsNotValidator validator = new();

    [Fact]
    public void TestValidateValueWhenInvalid()
    {
        var result = validator.IsValid("hello world", "hello world");

        Assert.False(result);
    }

    [Fact]
    public void TestValidateValueWhenValid()
    {
        var result = validator.IsValid("hello world", "hello");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenNull()
    {
        var result = validator.IsValid(null, "hello");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenEmpty()
    {
        var result = validator.IsValid(string.Empty, "hello");

        Assert.True(result);
    }

    [Fact]
    public void TestForDocumentation()
    {
        var template = "Address: { Address : IsNot('N/A'), EOL }";
        var input = "Address: N/A\nAddress: 10 Acacia Avenue";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("10 Acacia Avenue", result.First("Address"));
    }
}