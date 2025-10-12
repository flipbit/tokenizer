using System;
using Xunit;

namespace Tokens.Transformers;

public class ToDateTimeTransformerTests
{
    private readonly ToDateTimeTransformer transformer = new();

    [Fact]
    public void TestParseDate()
    {
        var result =  transformer.CanTransform("2014-01-01", ["yyyy-MM-dd"], out var t);

        var dateTime = (DateTime) t;

        Assert.True(result);
        Assert.Equal(new DateTime(2014, 1, 1), dateTime);
        Assert.Equal(DateTimeKind.Unspecified, dateTime.Kind);
    }

    [Fact]
    public void TestParseDateWithFormat()
    {
        var result = transformer.CanTransform("2 Mar 2012", ["d MMM yyyy"], out var t);
        var dateTime = (DateTime) t;

        Assert.True(result);
        Assert.Equal(new DateTime(2012, 3, 2), dateTime);
    }

    [Fact]
    public void TestParseDateWithNoFormat()
    {
        var result = transformer.CanTransform("2012-05-06", null, out var t);
        var dateTime = (DateTime) t;

        Assert.True(result);
        Assert.Equal(new DateTime(2012, 5, 6), dateTime);
    }

    [Fact]
    public void TestParseDateWithInvalidFormat()
    {
        var result = transformer.CanTransform("2012-05-06", ["dd MMM yy"], out var t);
            
        Assert.False(result);
        Assert.Equal("2012-05-06", t);
    }

    [Fact]
    public void TestParseDateWithFormatList()
    {
        var result = transformer.CanTransform("2012-05-06", ["dd MMM yy", "yyyy-MM-dd"], out var t);
        var dateTime = (DateTime) t;

        Assert.True(result);
        Assert.Equal(new DateTime(2012, 5 ,6), dateTime);
    }

    [Fact]
    public void TestParseDateWithEmptyValue()
    {
        var result = transformer.CanTransform(string.Empty, null, out var t);

        Assert.False(result);
        Assert.Equal(string.Empty, t);
    }

    [Fact]
    public void TestParseDateWithNullValue()
    {
        var result = transformer.CanTransform(null, null, out var t);

        Assert.False(result);
        Assert.Null(t);
    }

    [Fact]
    public void TestParseDateWithUnixNewLine()
    {
        var result = transformer.CanTransform("2012-05-06\nHello", null, out var t);
        var dateTime = (DateTime) t;

        Assert.True(result);
        Assert.Equal(new DateTime(2012, 5, 6), t);
    }

    [Fact]
    public void TestParseDateWithWindowsNewLine()
    {
        var result = transformer.CanTransform("2012-05-06\r\nHello", null, out var t);

        Assert.True(result);
        Assert.Equal(new DateTime(2012, 5, 6), t);
    }

    [Fact]
    public void TestParseDateWithDayOrdinalAtStart()
    {
        var result = transformer.CanTransform("01st August 2001", ["dd MMMM yyyy"], out var t);
        var dateTime = (DateTime) t;

        Assert.True(result);
        Assert.Equal(new DateTime(2001, 8 , 1), dateTime);
    }

    [Fact]
    public void TestParseDateWithDayOrdinalInMiddle()
    {
        var result = transformer.CanTransform("August 2nd 2001", ["MMMM d yyyy"], out var t);
        var dateTime = (DateTime) t;

        Assert.True(result);
        Assert.Equal(new DateTime(2001, 8 , 2), dateTime);
    }

    [Fact]
    public void TestParseDateWithSpanishFullMonth()
    {
        var result = transformer.CanTransform("Agosto 2nd 2001", ["MMMM d yyyy"], out var t);
        var dateTime = (DateTime) t;

        Assert.True(result);
        Assert.Equal(new DateTime(2001, 8 , 2), dateTime);
    }

    [Fact]
    public void TestParseDateWithSpanishMonthAbbreviation()
    {
        var result = transformer.CanTransform("16-abr-1997", ["dd-MMM-yyyy"], out var t);
        var dateTime = (DateTime) t;

        Assert.True(result);
        Assert.Equal(new DateTime(1997, 4 , 16), dateTime);
    }
}