using Xunit;

namespace Tokens.Validators;

public class IsDomainNameValidatorTests
{
    private readonly IsDomainNameValidator validator = new();

    [Fact]
    public void TestValidateValueWhenValid()
    {
        var result = validator.IsValid("github.com");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenNewTld()
    {
        var result = validator.IsValid("hello.ninja");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenHasSubdomain()
    {
        var result = validator.IsValid("www.hello.ninja");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenInvalidDomain()
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
        var template = "Web: { Domain : IsDomainName }";
        var input = "Web: n/a Web: www.flipbit.co.uk";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("www.flipbit.co.uk", result.First("Domain"));
    }
}