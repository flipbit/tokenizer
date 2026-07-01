using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsDateTimeValidatorTests : Tests.TokenizerTestBase
{
    public IsDateTimeValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsDateTimeValidator validator = new();

    [Fact]
    public void GivenValidDateString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "1 May 2019";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidIsoDateString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "2019-05-01";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidDateTimeString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "2019-05-01 14:00:00";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenInvalidDateString_WhenValidating_ThenReturnsFalse()
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
    public void GivenTemplateWithDateTimeValidator_WhenInputHasInvalidThenValidDate_ThenUsesValidDate()
    {
        // Arrange
        var template = "Date: { Date : IsDateTime('yyyy-MM-dd') }";
        var input = "Date: 3rd Oct 2019 Date: 2019-10-04";

        // Act
        var result = Tokenizer.Create().Tokenize(template, input);

        // Assert
        Assert.Equal("2019-10-04", result.First("Date"));
    }
}
