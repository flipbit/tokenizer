using Xunit;

namespace Tokens.Validators;

public class IsLooseUrlValidatorTests
{
    private readonly IsLooseUrlValidator validator = new();

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
    public void GivenUrlWithoutProtocol_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "github.com";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenRelativeUrl_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "/foo/bar.html";

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
        string input = null;

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
    public void GivenTemplateWithLooseUrlValidator_WhenInputHasInvalidThenValidUrl_ThenUsesValidUrl()
    {
        // Arrange
        var template = "Server: { ServerUrl : IsLooseUrl, EOL }";
        var input = "Server: Not specified\nServer: www.server.com";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("www.server.com", result.First("ServerUrl"));
    }
}