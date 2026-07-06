using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsGuidValidatorTests : TokenizerTestBase
{
    public IsGuidValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsGuidValidator validator = new();

    [Fact]
    public void GivenValidGuidWithHyphens_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidGuidWithoutHyphens_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "d3b07384d9a04e9b8a0d1e6b2a3c4d5e";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidGuidWithBraces_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "{d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e}";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenInvalidGuid_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "not-a-guid";

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
    public void GivenTemplateWithIsGuidValidator_WhenInputIsGuid_ThenExtractsValue()
    {
        // Arrange
        var template = "ID: { Id : IsGuid }";
        var input = "ID: d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template);
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e", result.First("Id"));
    }
}
