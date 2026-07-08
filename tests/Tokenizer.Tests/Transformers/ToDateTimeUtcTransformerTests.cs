#pragma warning disable CS0612, CS0618 // ToDateTimeUtcTransformer is obsolete
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Transformers;

public class ToDateTimeUtcTransformerTests : TokenizerTestBase
{
    public ToDateTimeUtcTransformerTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly ToDateTimeUtcTransformer _transformer = new();

    [Fact]
    public void GivenValidDateString_WhenTransforming_ThenReturnsDateTimeOffsetWithUtcOffset()
    {
        // Arrange
        var input = "2014-01-01";
        var format = "yyyy-MM-dd";

        // Act
        var result = _transformer.TryTransform(input, [format], out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2014, dto.Year);
        Assert.Equal(1, dto.Month);
        Assert.Equal(1, dto.Day);
        Assert.Equal(TimeSpan.Zero, dto.Offset);
    }

    [Fact]
    public void GivenValidDateTimeString_WhenTransforming_ThenReturnsDateTimeOffsetWithUtcOffset()
    {
        // Arrange
        var input = "2014-01-01 10:00:00";
        var format = "yyyy-MM-dd hh:mm:ss";

        // Act
        var result = _transformer.TryTransform(input, [format], out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2014, dto.Year);
        Assert.Equal(1, dto.Month);
        Assert.Equal(1, dto.Day);
        Assert.Equal(10, dto.Hour);
        Assert.Equal(TimeSpan.Zero, dto.Offset);
    }

    [Fact]
    public void GivenIsoFormatDateTimeString_WhenTransforming_ThenReturnsDateTimeOffsetWithUtcOffset()
    {
        // Arrange
        var input = "2014-01-01T10:00:00Z";
        var format = "yyyy-MM-ddThh:mm:ssZ";

        // Act
        var result = _transformer.TryTransform(input, [format], out var t);
        var dto = (DateTimeOffset)t;

        // Assert
        Assert.True(result);
        Assert.Equal(2014, dto.Year);
        Assert.Equal(1, dto.Month);
        Assert.Equal(1, dto.Day);
        Assert.Equal(10, dto.Hour);
        Assert.Equal(TimeSpan.Zero, dto.Offset);
    }

    [Fact]
    public void GivenDateWithUtcSuffix_WhenTokenizing_ThenTrimsUtcAndReturnsDateTimeOffset()
    {
        // Arrange
        var pattern = @"Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }";
        var input = "Date: 2000-01-01 UTC";

        // Act
        var _tok = new Tokenizer();
        var template = _tok.Compile(pattern).Template;
        var result = _tok.Tokenize(template, input);

        // Assert
        var matchValue = (DateTimeOffset)result.Matches.First(m => string.Equals(m.Token.Name, "Date", StringComparison.Ordinal)).Value;
        Assert.Equal(2000, matchValue.Year);
        Assert.Equal(1, matchValue.Month);
        Assert.Equal(1, matchValue.Day);
        Assert.Equal(TimeSpan.Zero, matchValue.Offset);
    }

    [Fact]
    public void GivenDateWithUtcInBrackets_WhenTokenizing_ThenTrimsUtcAndReturnsDateTimeOffset()
    {
        // Arrange
        var pattern = @"Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }";
        var input = "Date: 2000-01-01 (UTC)";

        // Act
        var _tok = new Tokenizer();
        var template = _tok.Compile(pattern).Template;
        var result = _tok.Tokenize(template, input);

        // Assert
        var matchValue = (DateTimeOffset)result.Matches.First(m => string.Equals(m.Token.Name, "Date", StringComparison.Ordinal)).Value;
        Assert.Equal(2000, matchValue.Year);
        Assert.Equal(1, matchValue.Month);
        Assert.Equal(1, matchValue.Day);
        Assert.Equal(TimeSpan.Zero, matchValue.Offset);
    }

    [Fact]
    public void GivenDateWithWrongFormat_WhenTokenizing_ThenDoesNotExtractDate()
    {
        // Arrange
        var pattern = @"Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }";
        var input = "Date: 2000-1-1 (UTC)";

        // Act
        var _tok = new Tokenizer();
        var template = _tok.Compile(pattern).Template;
        var result = _tok.Tokenize(template, input);

        // Assert
        Assert.False(result.Matches.Any(m => string.Equals(m.Token.Name, "Date", StringComparison.Ordinal)));
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
        var _tok = new Tokenizer();
        var template = _tok.Compile(pattern).Template;
        var result = _tok.Tokenize(template, input);

        // Assert
        var matchValue = (DateTimeOffset)result.Matches.First(m => string.Equals(m.Token.Name, "Date", StringComparison.Ordinal)).Value;
        Assert.Equal(2000, matchValue.Year);
        Assert.Equal(1, matchValue.Month);
        Assert.Equal(1, matchValue.Day);
        Assert.Equal(TimeSpan.Zero, matchValue.Offset);
    }
}
#pragma warning restore CS0612, CS0618
