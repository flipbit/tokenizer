using Xunit;

namespace Tokens.Validators;

public class IsNotEmptyValidatorTests
{
    private readonly IsNotEmptyValidator validator = new();

    [Fact]
    public void TestValidateValueWhenValid()
    {
        var result = validator.IsValid("hello world");

        Assert.True(result);
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
        var template = "Middle Name: { MiddleName : IsNotEmpty, EOL }";
        var input = "Middle Name:\nMiddle Name: Charles";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("Charles", result.First("MiddleName"));
    }
}