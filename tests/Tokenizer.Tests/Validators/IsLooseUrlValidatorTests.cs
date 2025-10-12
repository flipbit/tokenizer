using Xunit;

namespace Tokens.Validators;

public class IsLooseUrlValidatorTests
{
    private readonly IsLooseUrlValidator validator = new();

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
    public void TestValidateValueWhenNoProtocol()
    {
        var result = validator.IsValid("github.com");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenRelativeUrl()
    {
        var result = validator.IsValid("/foo/bar.html");

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
        var template = "Server: { ServerUrl : IsLooseUrl, EOL }";
        var input = "Server: Not specified\nServer: www.server.com";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("www.server.com", result.First("ServerUrl"));
    }
}