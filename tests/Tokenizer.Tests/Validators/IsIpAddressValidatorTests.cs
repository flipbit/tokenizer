using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsIpAddressValidatorTests : TokenizerTestBase
{
    public IsIpAddressValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsIpAddressValidator validator = new();

    [Fact]
    public void GivenValidIpv4Address_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "192.168.1.1";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidIpv6Address_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "::1";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidFullIpv6Address_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenInvalidIpAddress_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "999.999.999.999";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNonIpString_WhenValidating_ThenReturnsFalse()
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
        // Act
        var result = validator.IsValid(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithIsIpAddressValidator_WhenInputIsIpAddress_ThenExtractsValue()
    {
        // Arrange
        var template = "Server: { Ip : IsIpAddress }";
        var input = "Server: 10.0.0.1";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("10.0.0.1", result.First("Ip"));
    }
}
