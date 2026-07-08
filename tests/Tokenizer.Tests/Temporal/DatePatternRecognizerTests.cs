using System.Globalization;
using Xunit;

namespace Tokens.Temporal;

public class DatePatternRecognizerTests
{
    // -------------------------------------------------------------------------
    // Priority 1: ISO 8601 with offset
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-01-15T14:30:00+05:00")]
    [InlineData("2024-01-15T14:30:00.1+05:00")]
    [InlineData("2024-01-15T14:30:00.12+05:00")]
    [InlineData("2024-01-15T14:30:00.123+05:00")]
    [InlineData("2024-01-15T14:30:00.1234+05:00")]
    [InlineData("2024-01-15T14:30:00.12345+05:00")]
    [InlineData("2024-01-15T14:30:00.123456+05:00")]
    [InlineData("2024-01-15T14:30:00.1234567+05:00")]
    [InlineData("2024-01-15T14:30:00-05:00")]
    public void GivenIso8601WithOffset_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.True(DateTimeOffset.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 2: ISO 8601 with Z
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-01-15T14:30:00Z")]
    [InlineData("2024-01-15T14:30:00.1Z")]
    [InlineData("2024-01-15T14:30:00.12Z")]
    [InlineData("2024-01-15T14:30:00.123Z")]
    [InlineData("2024-01-15T14:30:00.1234Z")]
    [InlineData("2024-01-15T14:30:00.12345Z")]
    [InlineData("2024-01-15T14:30:00.123456Z")]
    [InlineData("2024-01-15T14:30:00.1234567Z")]
    public void GivenIso8601WithZ_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.True(DateTimeOffset.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 3: ISO 8601 no offset
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-01-15T14:30:00")]
    [InlineData("2024-01-15T14:30:00.1")]
    [InlineData("2024-01-15T14:30:00.123")]
    [InlineData("2024-01-15T14:30:00.1234567")]
    public void GivenIso8601NoOffset_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 4: RFC 2822 / asctime
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Tue Mar 5 14:30:00 GMT 2024")]
    [InlineData("Mon Jan 1 09:00:00 GMT 2024")]
    public void GivenRfc2822Asctime_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 5: Day/month/year with time + offset
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15/01/2024 14:30:00+05:00")]
    [InlineData("01/12/2024 09:00:00-03:00")]
    public void GivenDayMonthYearWithTimeAndOffset_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.True(DateTimeOffset.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 6: Year-month-day with time + offset (space before offset)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-01-15 14:30:00 +05:00")]
    [InlineData("2024-01-15 14:30:00 -03:00")]
    public void GivenYearMonthDayWithTimeAndOffset_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.True(DateTimeOffset.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 7: Year.month.day with time
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024.01.15 14:30:00")]
    public void GivenYearDotMonthDotDayWithTime_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyy.MM.dd HH:mm:ss", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 8: Year-month-day with time
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-01-15 14:30:00")]
    public void GivenYearMonthDayWithTime_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyy-MM-dd HH:mm:ss", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 9: Day-monthname-year with time
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15-Mar-2024 14:30:00")]
    [InlineData("01-Jan-2024 00:00:00")]
    public void GivenDayMonthNameYearWithTime_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd-MMM-yyyy HH:mm:ss", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 10: Dayname day monthname year
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Tuesday 15 October 2024")]
    [InlineData("Monday 1 January 2024")]
    public void GivenDaynameDayMonthnameYear_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dddd d MMMM yyyy", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 11: Day monthname year with time+offset
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15 Mar 2024 14:30+05:00")]
    [InlineData("01 Jan 2024 09:00-03:00")]
    public void GivenDayMonthnameYearWithTimeAndOffset_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd MMM yyyy HH:mmzzz", f);
        Assert.True(DateTimeOffset.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 12: Monthname day, year
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("March 15, 2024")]
    [InlineData("January 1, 2024")]
    public void GivenMonthnameDay_Year_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("MMMM d, yyyy", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 13: Day-monthname-year
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15-Mar-2024")]
    [InlineData("15-Jan-2024")]
    [InlineData("01-Dec-2024")]
    public void GivenDayMonthNameYear_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd-MMM-yyyy", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 14: Day monthname year
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15 Mar 2024")]
    [InlineData("01 Jan 2024")]
    public void GivenDayMonthnameYear_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd MMM yyyy", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 15: Day-fullmonth-year
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15-March-2024")]
    [InlineData("01-January-2024")]
    public void GivenDayFullMonthYear_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd-MMMM-yyyy", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 16: Day.month.year with time
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15.01.2024 14:30:00")]
    public void GivenDayDotMonthDotYearWithTime_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd.MM.yyyy HH:mm:ss", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 17: Year-month-day with fractional seconds (no T)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-01-15 14:30:00.5")]
    [InlineData("2024-01-15 14:30:00.50")]
    [InlineData("2024-01-15 14:30:00.500")]
    [InlineData("2024-01-15 14:30:00.5000000")]
    public void GivenYearMonthDayWithFractionalSeconds_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 18: Korean style
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024. 01. 15.")]
    public void GivenKoreanStyle_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyy. MM. dd.", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 19: Turkish style
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-Mar-15.")]
    [InlineData("2024-Jan-01.")]
    public void GivenTurkishStyle_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyy-MMM-dd.", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 20: Year/month/day with time
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024/01/15 14:30:00")]
    public void GivenYearSlashMonthSlashDayWithTime_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyy/MM/dd HH:mm:ss", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 21: Day/month/year with time
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15/01/2024 14:30:00")]
    public void GivenDaySlashMonthSlashYearWithTime_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd/MM/yyyy HH:mm:ss", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 22: Year-month-day
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024-01-15")]
    public void GivenYearMonthDay_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyy-MM-dd", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 23: Year.month.day
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024.01.15")]
    public void GivenYearDotMonthDotDay_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyy.MM.dd", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 24: Year/month/day
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("2024/01/15")]
    public void GivenYearSlashMonthSlashDay_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyy/MM/dd", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 25: Day.month.year
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15.01.2024")]
    public void GivenDayDotMonthDotYear_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd.MM.yyyy", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 26: Day/month/year (ambiguity tests below)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15/01/2024")]  // day > 12, unambiguously dd/MM
    public void GivenDaySlashMonthSlashYear_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd/MM/yyyy", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 27: Day-month-year
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("15-01-2024")]
    public void GivenDayDashMonthDashYear_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd-MM-yyyy", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 29: Compact with time
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("20240115143000")]
    public void GivenCompactWithTime_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyyMMddHHmmss", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 30: Compact date-time (space separated)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("20240115 14:30:00")]
    public void GivenCompactDateWithSpaceTime_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyyMMdd HH:mm:ss", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 31: Compact date
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("20240115")]
    public void GivenCompactDate_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyyMMdd", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Priority 32: Relaxed day.month.year
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("5.1.2024")]
    [InlineData("15.3.2024")]
    public void GivenRelaxedDayDotMonthDotYear_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("d.M.yyyy", f);
        Assert.True(DateTime.TryParseExact(input, f, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), $"TryParseExact failed for '{input}' with formats [{string.Join(", ", f)}]");
    }

    // -------------------------------------------------------------------------
    // Ambiguity: dd/MM vs MM/dd based on culture
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenAmbiguousNumericDate_WhenRecognizingWithInvariantCulture_ThenDefaultsToDdMm()
    {
        // Arrange — "01/02/2024" is ambiguous (both day and month <= 12)
        // With invariant culture, dd/MM takes priority over MM/dd

        // Act
        var result = DatePatternRecognizer.TryRecognize("01/02/2024", CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("dd/MM/yyyy", f);
    }

    [Fact]
    public void GivenAmbiguousNumericDate_WhenRecognizingWithUsCulture_ThenDefaultsToMmDd()
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize("01/02/2024", CultureInfo.GetCultureInfo("en-US"), out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        Assert.Contains("MM/dd/yyyy", f);
    }

    [Fact]
    public void GivenAmbiguousNumericDate_WhenRecognizingWithUsCulture_ThenMmDdIsFirst()
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize("01/02/2024", CultureInfo.GetCultureInfo("en-US"), out var formats);

        // Assert
        Assert.True(result);
        var f = formats!;
        // MM/dd should come first for en-US
        Assert.Equal("MM/dd/yyyy", f[0]);
    }

    // -------------------------------------------------------------------------
    // Non-date input
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenNonDateString_WhenRecognizing_ThenReturnsFalse()
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize("hello world", CultureInfo.InvariantCulture, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenRecognizing_ThenReturnsFalse()
    {
        var result = DatePatternRecognizer.TryRecognize(string.Empty, CultureInfo.InvariantCulture, out _);
        Assert.False(result);
    }

    [Fact]
    public void GivenWhitespaceString_WhenRecognizing_ThenReturnsFalse()
    {
        var result = DatePatternRecognizer.TryRecognize("   ", CultureInfo.InvariantCulture, out _);
        Assert.False(result);
    }

    // -------------------------------------------------------------------------
    // Priority ordering: more specific beats less specific
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenIso8601WithOffset_WhenRecognizing_ThenPrioritisedOverPlainYearMonthDay()
    {
        // 2024-01-15T14:30:00+05:00 must match priority-1 (ISO offset), NOT priority-22 (yyyy-MM-dd)
        var result = DatePatternRecognizer.TryRecognize("2024-01-15T14:30:00+05:00",
            CultureInfo.InvariantCulture, out var formats);

        Assert.True(result);
        var f = formats!;
        Assert.Contains("yyyy-MM-ddTHH:mm:sszzz", f);
    }
}
