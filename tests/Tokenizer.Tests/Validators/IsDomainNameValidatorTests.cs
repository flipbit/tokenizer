using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsDomainNameValidatorTests : TokenizerTestBase
{
    public IsDomainNameValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsDomainNameValidator validator = new();

    [Fact]
    public void GivenValidDomainName_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "github.com";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidDomainNameWithNewTld_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello.ninja";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidDomainNameWithSubdomain_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "www.hello.ninja";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenInvalidDomainName_WhenValidating_ThenReturnsFalse()
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
    public void GivenTemplateWithDomainNameValidator_WhenInputHasInvalidThenValidDomain_ThenUsesValidDomain()
    {
        // Arrange
        var template = "Web: { Domain : IsDomainName }";
        var input = "Web: n/a Web: www.flipbit.co.uk";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("www.flipbit.co.uk", result.First("Domain"));
    }
}
