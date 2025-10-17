using System;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Transformers;

public class ToDateTimeUtcTransformerTests : Tests.TokenizerTestBase
{
    public ToDateTimeUtcTransformerTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly ToDateTimeUtcTransformer transformer = new();

    [Fact]
    public void GivenValidDateString_WhenTransforming_ThenReturnsDateTimeWithUtcKind()
    {
        // Arrange
        var input = "2014-01-01";
        var format = "yyyy-MM-dd";

        // Act
        var result = transformer.CanTransform(input, [format], out var t);
        var dateTime = (DateTime) t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2014, 1, 1), t);
        Assert.Equal(DateTimeKind.Utc, dateTime.Kind);
    }

    [Fact]
    public void GivenValidDateTimeString_WhenTransforming_ThenReturnsDateTimeWithUtcKind()
    {
        // Arrange
        var input = "2014-01-01 10:00:00";
        var format = "yyyy-MM-dd hh:mm:ss";

        // Act
        var result = transformer.CanTransform(input, [format], out var t);
        var dateTime = (DateTime) t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2014, 1, 1, 10, 0, 0), dateTime);
        Assert.Equal(DateTimeKind.Utc, dateTime.Kind);
    }

    [Fact]
    public void GivenIsoFormatDateTimeString_WhenTransforming_ThenReturnsDateTimeWithUtcKind()
    {
        // Arrange
        var input = "2014-01-01T10:00:00Z";
        var format = "yyyy-MM-ddThh:mm:ssZ";

        // Act
        var result = transformer.CanTransform(input, [format], out var t);
        var dateTime = (DateTime) t;

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2014, 1, 1, 10, 0, 0), dateTime);
        Assert.Equal(DateTimeKind.Utc, dateTime.Kind);
    }

    [Fact]
    public void GivenDateWithUtcSuffix_WhenTokenizing_ThenTrimsUtcAndReturnsDateTime()
    {
        // Arrange
        var pattern = @"Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }";
        var input = "Date: 2000-01-01 UTC";

        // Act
        var result = Tokenizer.Create().Tokenize(pattern, input);

        // Assert
        Assert.Equal(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.First("Date"));
    }

    [Fact]
    public void GivenDateWithUtcInBrackets_WhenTokenizing_ThenTrimsUtcAndReturnsDateTime()
    {
        // Arrange
        var pattern = @"Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }";
        var input = "Date: 2000-01-01 (UTC)";

        // Act
        var result = Tokenizer.Create().Tokenize(pattern, input);

        // Assert
        Assert.Equal(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.First("Date"));
    }

    [Fact]
    public void GivenDateWithWrongFormat_WhenTokenizing_ThenDoesNotExtractDate()
    {
        // Arrange
        var pattern = @"Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }";
        var input = "Date: 2000-1-1 (UTC)";

        // Act
        var result = Tokenizer.Create().Tokenize(pattern, input);

        // Assert
        Assert.False(result.Contains("Date"));
    }

    [Fact]
    public void GivenMultipleTokensWithDifferentFormats_WhenTokenizing_ThenUsesFirstMatchingFormat()
    {
        // Arrange
        var pattern = """
                      ---
                      # End tokens on new lines
                      outOfOrder: true

                      # End tokens on new lines
                      terminateOnNewLine: true
                      ---
                      Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }
                      Date: { Date : ToDateTimeUtc('yyyy-M-d') }
                      """;
        var input = "Date: 2000-1-1 (UTC)";

        // Act
        var result = Tokenizer.Create().Tokenize(pattern, input);

        // Assert
        Assert.Equal(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.First<DateTime>("Date"));
    }
}
