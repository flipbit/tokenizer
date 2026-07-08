using Xunit;

namespace Tokens.Temporal;

public class TimezoneNormalizerTests
{
    private static readonly IReadOnlyDictionary<string, TimeSpan> NoCustom =
        new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

    [Theory]
    [InlineData("2024-01-15 14:30:00 UTC", "2024-01-15 14:30:00 +00:00")]
    [InlineData("2024-01-15 14:30:00 GMT", "2024-01-15 14:30:00 +00:00")]
    [InlineData("2024-01-15 14:30:00 CEST", "2024-01-15 14:30:00 +02:00")]
    [InlineData("2024-01-15 14:30:00 CET", "2024-01-15 14:30:00 +01:00")]
    [InlineData("2024-01-15 14:30:00 JST", "2024-01-15 14:30:00 +09:00")]
    [InlineData("2024-01-15 14:30:00 MSK", "2024-01-15 14:30:00 +03:00")]
    public void GivenValueWithBuiltInAbbreviation_WhenNormalizing_ThenReplacesWithOffset(
        string input, string expected)
    {
        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenValueWithParenthesizedUtc_WhenNormalizing_ThenReplacesWithOffset()
    {
        // Act
        var result = TimezoneNormalizer.Normalize("2024-01-15 14:30:00 (UTC)", NoCustom);

        // Assert
        Assert.Equal("2024-01-15 14:30:00 +00:00", result);
    }

    [Fact]
    public void GivenValueWithNumericOffset_WhenNormalizing_ThenReturnsUnchanged()
    {
        // Arrange
        var input = "2024-01-15 14:30:00 +05:00";

        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void GivenValueWithNoTimezone_WhenNormalizing_ThenReturnsUnchanged()
    {
        // Arrange
        var input = "2024-01-15 14:30:00";

        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void GivenValueWithUnknownAbbreviation_WhenNormalizing_ThenReturnsUnchanged()
    {
        // Arrange
        var input = "2024-01-15 14:30:00 XYZ";

        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void GivenCustomAbbreviation_WhenNormalizing_ThenUsesCustomMapping()
    {
        // Arrange
        var custom = new Dictionary<string, TimeSpan>(StringComparer.Ordinal)
        {
            ["PST"] = TimeSpan.FromHours(-8),
        };

        // Act
        var result = TimezoneNormalizer.Normalize("2024-01-15 14:30:00 PST", custom);

        // Assert
        Assert.Equal("2024-01-15 14:30:00 -08:00", result);
    }

    [Fact]
    public void GivenCustomAbbreviationOverridingBuiltIn_WhenNormalizing_ThenCustomWins()
    {
        // Arrange
        var custom = new Dictionary<string, TimeSpan>(StringComparer.Ordinal)
        {
            ["UTC"] = TimeSpan.FromHours(5), // absurd, but proves custom overrides built-in
        };

        // Act
        var result = TimezoneNormalizer.Normalize("2024-01-15 14:30:00 UTC", custom);

        // Assert
        Assert.Equal("2024-01-15 14:30:00 +05:00", result);
    }

    [Fact]
    public void GivenAbbreviationIsCaseSensitive_WhenNormalizingLowercase_ThenReturnsUnchanged()
    {
        // Arrange — timezone abbreviations are uppercase by convention
        var input = "2024-01-15 14:30:00 utc";

        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(input, result);
    }
}
