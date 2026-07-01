using System;
using Xunit;

namespace Tokens.Transformers;

public class ToDateTimeTransformerTests
{
    private readonly ToDateTimeTransformer transformer = new();

    [Fact]
    public void GivenValidDateStringWithFormat_WhenTransforming_ThenReturnsCorrectDateTime()
    {
        // Arrange
        var input = "2014-01-01";
        var format = "yyyy-MM-dd";

        // Act
        var result = transformer.TryTransform(input, [format], out var t);
        var dateTime = (DateTime)t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2014, 1, 1), dateTime);
        Assert.Equal(DateTimeKind.Unspecified, dateTime.Kind);
    }

    [Fact]
    public void GivenDateStringWithCustomFormat_WhenTransforming_ThenReturnsCorrectDateTime()
    {
        // Arrange
        var input = "2 Mar 2012";
        var format = "d MMM yyyy";

        // Act
        var result = transformer.TryTransform(input, [format], out var t);
        var dateTime = (DateTime)t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2012, 3, 2), dateTime);
    }

    [Fact]
    public void GivenDateStringWithNoFormat_WhenTransforming_ThenUsesDefaultParsing()
    {
        // Arrange
        var input = "2012-05-06";

        // Act
        var result = transformer.TryTransform(input, null!, out var t);
        var dateTime = (DateTime)t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2012, 5, 6), dateTime);
    }

    [Fact]
    public void GivenDateStringWithInvalidFormat_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var input = "2012-05-06";
        var format = "dd MMM yy";

        // Act
        var result = transformer.TryTransform(input, [format], out var t);

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

        // Act
        var result = transformer.TryTransform(input, formats, out var t);
        var dateTime = (DateTime)t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2012, 5, 6), dateTime);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = transformer.TryTransform(input, null!, out var t);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, t);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        string input = null!;

        // Act
        var result = transformer.TryTransform(input, null!, out var t);

        // Assert
        Assert.False(result);
        Assert.Null(t);
    }

    [Fact]
    public void GivenDateStringWithUnixNewLine_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "2012-05-06\nHello";

        // Act
        var result = transformer.TryTransform(input, null!, out var t);
        var dateTime = (DateTime)t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2012, 5, 6), t);
    }

    [Fact]
    public void GivenDateStringWithWindowsNewLine_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "2012-05-06\r\nHello";

        // Act
        var result = transformer.TryTransform(input, null!, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2012, 5, 6), t);
    }

    [Fact]
    public void GivenDateStringWithDayOrdinalAtStart_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "01st August 2001";
        var format = "dd MMMM yyyy";

        // Act
        var result = transformer.TryTransform(input, [format], out var t);
        var dateTime = (DateTime)t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2001, 8, 1), dateTime);
    }

    [Fact]
    public void GivenDateStringWithDayOrdinalInMiddle_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "August 2nd 2001";
        var format = "MMMM d yyyy";

        // Act
        var result = transformer.TryTransform(input, [format], out var t);
        var dateTime = (DateTime)t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2001, 8, 2), dateTime);
    }

    [Fact]
    public void GivenDateStringWithSpanishFullMonth_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "Agosto 2nd 2001";
        var format = "MMMM d yyyy";

        // Act
        var result = transformer.TryTransform(input, [format], out var t);
        var dateTime = (DateTime)t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2001, 8, 2), dateTime);
    }

    [Fact]
    public void GivenDateStringWithSpanishMonthAbbreviation_WhenTransforming_ThenParsesDateCorrectly()
    {
        // Arrange
        var input = "16-abr-1997";
        var format = "dd-MMM-yyyy";

        // Act
        var result = transformer.TryTransform(input, [format], out var t);
        var dateTime = (DateTime)t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(1997, 4, 16), dateTime);
    }
}
