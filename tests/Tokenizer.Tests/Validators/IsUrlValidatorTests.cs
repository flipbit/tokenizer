using Xunit;

namespace Tokens.Validators;

public class IsUrlValidatorTests
{
    private readonly IsUrlValidator validator = new();

    [Fact]
    public void TestValidateValueWhenHttp()
    {
        var result = validator.IsValid("http://github.com");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenHttps()
    {
        var result = validator.IsValid("https://github.com");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenInvalidUrl()
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
        var template = "Server: { ServerUrl : IsUrl, EOL }";
        var input = "Server: 192.168.1.1\nServer: http://www.server.com";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("http://www.server.com", result.First("ServerUrl"));
    }
}