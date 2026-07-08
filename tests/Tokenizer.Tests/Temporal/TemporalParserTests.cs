using System.Globalization;
using Xunit;

namespace Tokens.Temporal;

public class TemporalParserTests
{
    [Fact]
    public void GivenIso8601Value_WhenParsingWithFormat_ThenReturnsDateTimeOffset()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15T14:30:00Z", ["yyyy-MM-ddTHH:mm:ssZ"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero), dto);
    }

    [Fact]
    public void GivenIso8601WithFractionalSeconds_WhenParsingBaseFormat_ThenToleratesFractionalSeconds()
    {
        // Arrange — format says "Z" but value has ".123Z", ISO 8601 tolerance should handle it
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15T14:30:00.123Z", ["yyyy-MM-ddTHH:mm:ssZ"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(123, dto.Millisecond);
    }

    [Fact]
    public void GivenNoFormat_WhenParsingIso8601_ThenAutoDetectsViaRecognizer()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15T14:30:00Z", formats: null, options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(2024, dto.Year);
        Assert.Equal(1, dto.Month);
        Assert.Equal(15, dto.Day);
    }

    [Fact]
    public void GivenTimezoneAbbreviation_WhenParsing_ThenNormalizesBeforeParsing()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15 14:30:00 CEST", formats: null, options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(2), dto.Offset);
    }

    [Fact]
    public void GivenCulture_WhenParsingFrenchMonthName_ThenParsesCorrectly()
    {
        // Arrange
        var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("fr-FR") };

        // Act
        var result = TemporalParser.TryParse("15-mars-2024", ["dd-MMM-yyyy"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(3, dto.Month);
        Assert.Equal(15, dto.Day);
    }

    [Fact]
    public void GivenDefaultOffset_WhenParsingValueWithoutOffset_ThenAppliesDefaultOffset()
    {
        // Arrange
        var options = new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) };

        // Act
        var result = TemporalParser.TryParse("2024-01-15 14:30:00", ["yyyy-MM-dd HH:mm:ss"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(2), dto.Offset);
    }

    [Fact]
    public void GivenDefaultOffset_WhenParsingValueWithExplicitOffset_ThenIgnoresDefault()
    {
        // Arrange
        var options = new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) };

        // Act
        var result = TemporalParser.TryParse("2024-01-15T14:30:00+05:00", ["yyyy-MM-ddTHH:mm:sszzz"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(5), dto.Offset); // explicit offset wins
    }

    [Fact]
    public void GivenDefaultTimezone_WhenParsingValueWithoutOffset_ThenAppliesDstAwareOffset()
    {
        // Arrange
        var options = new TokenizerOptions { DefaultTimezone = "Europe/Berlin" };

        // Act — January = CET (+01:00)
        var result = TemporalParser.TryParse("2024-01-15 14:30:00", ["yyyy-MM-dd HH:mm:ss"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(1), dto.Offset);
    }

    [Fact]
    public void GivenBothDefaultOffsetAndTimezone_WhenParsing_ThenOffsetTakesPrecedence()
    {
        // Arrange
        var options = new TokenizerOptions
        {
            DefaultOffset = TimeSpan.FromHours(5),
            DefaultTimezone = "Europe/Berlin",
        };

        // Act
        var result = TemporalParser.TryParse("2024-01-15 14:30:00", ["yyyy-MM-dd HH:mm:ss"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(5), dto.Offset); // DefaultOffset wins
    }

    [Fact]
    public void GivenCustomTimezoneAbbreviation_WhenParsing_ThenUsesCustomMapping()
    {
        // Arrange
        var options = new TokenizerOptions()
            .WithTimezoneAbbreviation("PST", TimeSpan.FromHours(-8));

        // Act
        var result = TemporalParser.TryParse("2024-01-15 14:30:00 PST", formats: null, options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(-8), dto.Offset);
    }

    [Fact]
    public void GivenOrdinalSuffix_WhenParsing_ThenStripsAndParses()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("1st August 2001", ["dd MMMM yyyy"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(1, dto.Day);
        Assert.Equal(8, dto.Month);
    }

    [Fact]
    public void GivenUnparseableValue_WhenParsing_ThenReturnsFalse()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("not a date", ["yyyy-MM-dd"], options, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValueWithNewline_WhenParsing_ThenParsesBeforeNewline()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15\nsome text", ["yyyy-MM-dd"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(15, dto.Day);
    }
}
