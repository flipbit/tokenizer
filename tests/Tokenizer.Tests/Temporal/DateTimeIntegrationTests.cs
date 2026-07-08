using Xunit;
using Xunit.Abstractions;

namespace Tokens.Temporal;

public class DateTimeIntegrationTests : TokenizerTestBase
{
    public DateTimeIntegrationTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void GivenIso8601Template_WhenTokenizingAndAssigning_ThenProducesCorrectDateTime()
    {
        // Arrange
        var pattern = "Created: { Created : ToDateTime('yyyy-MM-ddTHH:mm:ssZ') }";
        var input = "Created: 2024-01-15T14:30:00Z";

        // Act
        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert — tokenize produces DateTimeOffset
        var match = result.Matches.First(m => m.Token.Name == "Created");
        var dto = Assert.IsType<DateTimeOffset>(match.Value);
        Assert.Equal(System.TimeSpan.Zero, dto.Offset);
    }

    [Fact]
    public void GivenIso8601WithFractionalSeconds_WhenSingleFormatSpecified_ThenHandlesAllVariants()
    {
        // Arrange — the template author specifies one format, fractional seconds auto-tolerated
        var pattern = "Date: { Date : ToDateTime('yyyy-MM-ddTHH:mm:ssZ') }";

        var inputs = new[]
        {
            "Date: 2024-01-15T14:30:00Z",
            "Date: 2024-01-15T14:30:00.1Z",
            "Date: 2024-01-15T14:30:00.123Z",
            "Date: 2024-01-15T14:30:00.1234567Z",
        };

        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile(pattern).Template;

        foreach (var input in inputs)
        {
            // Act
            var result = tokenizer.Tokenize(template, input);

            // Assert
            Assert.True(result.Success, $"Failed for input: {input}");
        }
    }

    [Fact]
    public void GivenCultureInFrontMatter_WhenTokenizingSpanishDate_ThenParsesCorrectly()
    {
        // Arrange — Spanish uses "mar" for March (es-ES), verifying culture flows into the transformer
        var pattern = """
                      ---
                      culture: es-ES
                      terminateOnNewLine: true
                      ---
                      Fecha: { Date : ToDateTime('dd-MMM-yyyy') }
                      """;
        var input = "Fecha: 15-mar-2024";

        // Act
        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        var dto = (DateTimeOffset)result.Matches.First(m => m.Token.Name == "Date").Value;
        Assert.Equal(3, dto.Month);
        Assert.Equal(15, dto.Day);
    }

    [Fact]
    public void GivenNoFormatString_WhenTokenizingUnambiguousDate_ThenAutoDetects()
    {
        // Arrange
        var pattern = "Date: { Date : ToDateTime }";
        var input = "Date: 2024-01-15";

        // Act
        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.True(result.Success);
        var dto = (DateTimeOffset)result.Matches.First(m => m.Token.Name == "Date").Value;
        Assert.Equal(2024, dto.Year);
    }

    [Fact]
    public void GivenTimezoneAbbreviation_WhenTokenizing_ThenPreservesOffset()
    {
        // Arrange
        var pattern = "Date: { Date : ToDateTime }";
        var input = "Date: 2024-01-15 14:30:00 CEST";

        // Act
        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.True(result.Success);
        var dto = (DateTimeOffset)result.Matches.First(m => m.Token.Name == "Date").Value;
        Assert.Equal(System.TimeSpan.FromHours(2), dto.Offset);
    }

    [Fact]
    public void GivenDefaultOffset_WhenTokenizingDateWithoutOffset_ThenAppliesDefault()
    {
        // Arrange
        var pattern = """
                      ---
                      defaultOffset: +02:00
                      ---
                      Date: { Date : ToDateTime('yyyy-MM-dd') }
                      """;
        var input = "Date: 2024-01-15";

        // Act
        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        var dto = (DateTimeOffset)result.Matches.First(m => m.Token.Name == "Date").Value;
        Assert.Equal(System.TimeSpan.FromHours(2), dto.Offset);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void GivenToDateTransformer_WhenAssigning_ThenProducesDateOnly()
    {
        // Arrange
        var pattern = "Birthday: { Birthday : ToDate('yyyy-MM-dd') }";
        var input = "Birthday: 1990-06-15";

        // Act
        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        var match = result.Matches.First(m => m.Token.Name == "Birthday");
        Assert.IsType<DateOnly>(match.Value);
        Assert.Equal(new DateOnly(1990, 6, 15), match.Value);
    }
#endif

    // --- Whois-derived real-world format tests ---

    [Theory]
    [InlineData("2024-01-15", "yyyy-MM-dd")]
    [InlineData("15-Jan-2024", "dd-MMM-yyyy")]
    [InlineData("20240115", "yyyyMMdd")]
    [InlineData("2024.01.15 14:30:00", "yyyy.MM.dd HH:mm:ss")]
    [InlineData("2024/01/15", "yyyy/MM/dd")]
    [InlineData("15.01.2024", "dd.MM.yyyy")]
    [InlineData("15/01/2024", "dd/MM/yyyy")]
    public void GivenWhoisRealWorldFormat_WhenTokenizing_ThenParsesCorrectly(string dateValue, string format)
    {
        // Arrange
        var pattern = $"Date: {{ Date : ToDateTime('{format}') }}";
        var input = $"Date: {dateValue}";

        // Act
        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.True(result.Success, $"Failed for format {format} with value {dateValue}");
        Assert.IsType<DateTimeOffset>(result.Matches.First(m => m.Token.Name == "Date").Value);
    }
}
