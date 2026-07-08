using System.Globalization;
using Xunit;

namespace Tokens.Transformers;

public class ToDateTimeTransformerTests
{
    private readonly ToDateTimeTransformer _transformer = new();

    [Fact]
    public void GivenValidDateStringWithFormat_WhenTransforming_ThenReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var input = "2014-01-01";
        var format = "yyyy-MM-dd";
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, [format], options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2014, dto.Year);
        Assert.Equal(1, dto.Month);
        Assert.Equal(1, dto.Day);
    }

    [Fact]
    public void GivenDateStringWithCustomFormat_WhenTransforming_ThenReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var input = "2 Mar 2012";
        var format = "d MMM yyyy";
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, [format], options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2012, dto.Year);
        Assert.Equal(3, dto.Month);
        Assert.Equal(2, dto.Day);
    }

    [Fact]
    public void GivenDateStringWithNoFormat_WhenTransforming_ThenUsesDefaultParsing()
    {
        // Arrange
        var input = "2012-05-06";
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, null!, options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2012, dto.Year);
        Assert.Equal(5, dto.Month);
        Assert.Equal(6, dto.Day);
    }

    [Fact]
    public void GivenDateStringWithInvalidFormat_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var input = "2012-05-06";
        var format = "dd MMM yy";
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, [format], options, out var t);

        // Assert
        Assert.False(result);
        Assert.Equal("2012-05-06", t);
    }

    [Fact]
    public void GivenDateStringWithFormatList_WhenTransforming_ThenUsesFirstMatchingFormat()
    {
        // Arrange
        var input = "2012-05-06";
        string[] formats = ["dd MMM yy", "yyyy-MM-dd"];
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, formats, options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2012, dto.Year);
        Assert.Equal(5, dto.Month);
        Assert.Equal(6, dto.Day);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var input = string.Empty;
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, null!, options, out var t);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, t);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        string input = null!;
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, null!, options, out var t);

        // Assert
        Assert.False(result);
        Assert.Null(t);
    }

    [Fact]
    public void GivenDateStringWithUnixNewLine_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "2012-05-06\nHello";
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, null!, options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2012, dto.Year);
        Assert.Equal(5, dto.Month);
        Assert.Equal(6, dto.Day);
    }

    [Fact]
    public void GivenDateStringWithWindowsNewLine_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "2012-05-06\r\nHello";
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, null!, options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2012, dto.Year);
        Assert.Equal(5, dto.Month);
        Assert.Equal(6, dto.Day);
    }

    [Fact]
    public void GivenDateStringWithDayOrdinalAtStart_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "01st August 2001";
        var format = "dd MMMM yyyy";
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, [format], options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2001, dto.Year);
        Assert.Equal(8, dto.Month);
        Assert.Equal(1, dto.Day);
    }

    [Fact]
    public void GivenDateStringWithDayOrdinalInMiddle_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "August 2nd 2001";
        var format = "MMMM d yyyy";
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, [format], options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2001, dto.Year);
        Assert.Equal(8, dto.Month);
        Assert.Equal(2, dto.Day);
    }

    [Fact]
    public void GivenDateStringWithSpanishFullMonth_WhenTransformingWithCulture_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "Agosto 2nd 2001";
        var format = "MMMM d yyyy";
        var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("es-ES") };

        // Act
        var result = _transformer.TryTransform(input, [format], options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2001, dto.Year);
        Assert.Equal(8, dto.Month);
        Assert.Equal(2, dto.Day);
    }

    [Fact]
    public void GivenDateStringWithSpanishMonthAbbreviation_WhenTransformingWithCulture_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "16-abr-1997";
        var format = "dd-MMM-yyyy";
        var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("es-ES") };

        // Act
        var result = _transformer.TryTransform(input, [format], options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(1997, dto.Year);
        Assert.Equal(4, dto.Month);
        Assert.Equal(16, dto.Day);
    }

    [Theory]
    [InlineData("1st January 2020", "d MMMM yyyy")]
    [InlineData("22nd March 2020", "dd MMMM yyyy")]
    public void GivenDateWithOrdinalSuffix_WhenFormatStartsWithDaySpecifier_ThenParsesCorrectly(string input, string format)
    {
        // Arrange
        var transformer = new ToDateTimeTransformer();
        var options = new TokenizerOptions();

        // Act
        var result = transformer.TryTransform(input, [format], options, out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<DateTimeOffset>(transformed);
    }

    [Fact]
    public void GivenFrenchMonthName_WhenTransformingWithCulture_ThenParsesCorrectly()
    {
        // Arrange
        var input = "15-mars-2024";
        var format = "dd-MMM-yyyy";
        var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("fr-FR") };

        // Act
        var result = _transformer.TryTransform(input, [format], options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(3, dto.Month);
    }

    [Fact]
    public void GivenSpanishMonthName_WhenTransformingWithCulture_ThenParsesCorrectly()
    {
        // Arrange
        var input = "16-abr-1997";
        var format = "dd-MMM-yyyy";
        var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("es-ES") };

        // Act
        var result = _transformer.TryTransform(input, [format], options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(4, dto.Month);
    }

    [Fact]
    public void GivenNoFormat_WhenTransforming_ThenAutoDetectsViaRecognizer()
    {
        // Arrange
        var input = "2024-01-15T14:30:00Z";
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform(input, Array.Empty<string>(), options, out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2024, dto.Year);
        Assert.Equal(TimeSpan.Zero, dto.Offset);
    }

    [Fact]
    public void GivenValidDateString_WhenTransformingWithoutOptions_ThenReturnsDateTimeOffset()
    {
        // Arrange
        var input = "2014-01-01";
        var format = "yyyy-MM-dd";

        // Act — non-options-aware overload
        var result = _transformer.TryTransform(input, [format], out var t);

        // Assert
        Assert.True(result);
        Assert.IsType<DateTimeOffset>(t);
    }
}
