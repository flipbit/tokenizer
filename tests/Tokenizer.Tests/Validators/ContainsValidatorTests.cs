using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class ContainsValidatorTests : TokenizerTestBase
{
    public ContainsValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly ContainsValidator _validator = new();

    [Fact]
    public void GivenStringThatContainsSubstring_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "hello world";
        var substring = "o wor";

        // Act
        var result = _validator.IsValid(input, substring);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenStringThatDoesNotContainSubstring_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";
        var substring = "spoon";

        // Act
        var result = _validator.IsValid(input, substring);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValidatorWithMissingArgument_WhenValidating_ThenThrowsTokenizerException()
    {
        // Arrange
        var input = "hello world";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _validator.IsValid(input));
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        string input = null!;

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = _validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithContainsValidator_WhenInputHasMultipleValues_ThenUsesFirstMatchingValue()
    {
        // Arrange
        var template = "Name: { Name : Contains('B') }";
        var input = "Name: Alice Name: Bob";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("Bob", result.First("Name"));
    }

    [Theory]
    [InlineData("Hello World", "World", true)]
    [InlineData("Hello World", "world", false)]
    [InlineData("Hello World", "lo Wo", true)]
    public void GivenStringAndSubstring_WhenValidating_ThenMatchesOrdinal(string input, string substring, bool expected)
    {
        // Arrange / Act
        var result = _validator.IsValid(input, substring);

        // Assert
        Assert.Equal(expected, result);
    }
}
