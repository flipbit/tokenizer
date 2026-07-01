using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsUrlValidatorTests : Tests.TokenizerTestBase
{
    public IsUrlValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsUrlValidator validator = new();

    [Fact]
    public void GivenHttpUrl_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "http://github.com";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenHttpsUrl_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "https://github.com";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenInvalidUrl_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        string input = null!;

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithUrlValidator_WhenInputHasInvalidThenValidUrl_ThenUsesValidUrl()
    {
        // Arrange
        var template = "Server: { ServerUrl : IsUrl, EOL }";
        var input = "Server: 192.168.1.1\nServer: http://www.server.com";

        // Act
        var result = Tokenizer.Create().Tokenize(template, input);

        // Assert
        Assert.Equal("http://www.server.com", result.First("ServerUrl"));
    }
}
