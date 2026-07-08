using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class MatchesRegexValidatorTests : TokenizerTestBase
{
    public MatchesRegexValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly MatchesRegexValidator _validator = new();

    [Fact]
    public void GivenMatchingPattern_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "123-4567";

        // Act
        var result = _validator.IsValid(input, @"^\d{3}-\d{4}$");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNonMatchingPattern_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "abc";

        // Act
        var result = _validator.IsValid(input, @"^\d+$");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenPatternWithInlineCaseInsensitiveFlag_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "HELLO";

        // Act
        var result = _validator.IsValid(input, @"(?i)^hello$");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = _validator.IsValid(null!, @"\d+");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = _validator.IsValid(string.Empty, @"\d+");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenMissingArgs_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _validator.IsValid("test"));
    }

    [Fact]
    public void GivenCatastrophicBacktrackingPattern_WhenValidating_ThenThrowsRegexMatchTimeoutException()
    {
        // Arrange — (a+)+$ is a classic ReDoS pattern; this input causes catastrophic backtracking
        var input = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaab";

        // Act & Assert
        Assert.Throws<System.Text.RegularExpressions.RegexMatchTimeoutException>(
            () => _validator.IsValid(input, @"(a+)+$"));
    }

    [Fact]
    public void GivenTemplateWithMatchesRegexValidator_WhenInputMatches_ThenExtractsValue()
    {
        // Arrange
        var template = @"Phone: { Phone : MatchesRegex(^\d{3}-\d{4}$) }";
        var input = "Phone: 555-1234";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("555-1234", result.Matches.First(m => string.Equals(m.Token.Name, "Phone", StringComparison.Ordinal)).Value);
    }
}
