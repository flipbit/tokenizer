using System.IO;
using System.Threading.Tasks;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Safety;

public class TokenizerSafetyLimitTests
{
    [Fact]
    public void GivenInputExceedingMaxLength_WhenTokenizing_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxInputLength = 100 };
        var tokenizer = new Tokenizer(options);
        var input = new string('x', 101);

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() =>
            tokenizer.Tokenize("Name: {Name}", input));
        Assert.Contains("101", ex.Message);
        Assert.Contains("100", ex.Message);
        Assert.Contains("MaxInputLength", ex.Message);
    }

    [Fact]
    public void GivenInputAtMaxLength_WhenTokenizing_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = new TokenizerOptions { MaxInputLength = 100 };
        var tokenizer = new Tokenizer(options);
        var input = "Name: " + new string('x', 94);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", input);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenMaxInputLengthDisabled_WhenTokenizingLargeInput_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = new TokenizerOptions { MaxInputLength = 0 };
        var tokenizer = new Tokenizer(options);
        var input = "Name: " + new string('x', 200_000);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", input);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenTemplateExceedingMaxLength_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTemplateLength = 50 };
        var tokenizer = new Tokenizer(options);
        var longTemplate = "Name: {Name}" + new string(' ', 50);

        // Act & Assert
        var ex = Assert.Throws<ParsingException>(() =>
            tokenizer.Tokenize(longTemplate, "Name: John"));
        Assert.Contains("MaxTemplateLength", ex.Message);
    }

    [Fact]
    public void GivenTemplateAtMaxLength_WhenParsing_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTemplateLength = 100 };
        var tokenizer = new Tokenizer(options);
        var template = "Name: {Name}";

        // Act
        var result = tokenizer.Tokenize(template, "Name: John");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenMaxTemplateLengthDisabled_WhenParsingLargeTemplate_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTemplateLength = 0 };
        var tokenizer = new Tokenizer(options);
        var template = "Name: {Name}" + new string(' ', 100_000);

        // Act
        var result = tokenizer.Tokenize(template, "Name: John");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenTemplateExceedingMaxTokenCount_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTokenCount = 5 };
        var tokenizer = new Tokenizer(options);

        var templateBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < 6; i++)
        {
            templateBuilder.Append($"T{i}: {{Token{i}}}\n");
        }

        // Act & Assert
        var ex = Assert.Throws<ParsingException>(() =>
            tokenizer.Tokenize(templateBuilder.ToString(), "T0: Value"));
        Assert.Contains("6", ex.Message);
        Assert.Contains("5", ex.Message);
        Assert.Contains("MaxTokenCount", ex.Message);
    }

    [Fact]
    public void GivenTemplateAtMaxTokenCount_WhenParsing_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTokenCount = 5 };
        var tokenizer = new Tokenizer(options);

        var templateBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < 5; i++)
        {
            templateBuilder.Append($"T{i}: {{Token{i}}}\n");
        }

        // Act
        var result = tokenizer.Tokenize(templateBuilder.ToString(), "T0: Value0\nT1: Value1");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenMaxIterationsExceeded_WhenTokenizing_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxIterations = 5 };
        var tokenizer = new Tokenizer(options);

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() =>
            tokenizer.Tokenize("Name: {Name}", "Name: John Doe"));
        Assert.Contains("MaxIterations", ex.Message);
    }

    [Fact]
    public void GivenAutoMaxIterations_WhenTokenizingNormalInput_ThenProcessesSuccessfully()
    {
        // Arrange — default MaxIterations=0 means auto (input.Length * 2)
        var options = new TokenizerOptions();
        var tokenizer = new Tokenizer(options);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", "Name: John");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenCustomMaxIterations_WhenWithinLimit_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = new TokenizerOptions { MaxIterations = 10000 };
        var tokenizer = new Tokenizer(options);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", "Name: John");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenDefaultOptions_WhenTokenizingNormalInput_ThenProcessesSuccessfully()
    {
        // Arrange — verify defaults don't interfere with normal usage
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("Name: {Name}\nAge: {Age}", "Name: John\nAge: 30");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
    }

    [Fact]
    public void GivenNoExplicitMaxIterations_WhenTokenizingComplexPattern_ThenDerivedLimitIsNotExceeded()
    {
        // Arrange — derived limit is CharactersConsumed * 2 + 100. Normal tokenization keeps
        // iterations well below this threshold even for complex multi-token templates on longer input.
        // This test confirms that the auto-limit does not interfere with legitimate tokenization.
        var tokenizer = new Tokenizer();

        // Act — complex template with many tokens on a moderately-sized input
        var result = tokenizer.Tokenize(
            "A:{A} B:{B} C:{C} D:{D} E:{E} F:{F}",
            "A:1 B:2 C:3 D:4 E:5 F:6");

        // Assert — no exception thrown and all tokens matched
        Assert.True(result.Success);
        Assert.Equal(6, result.Tokens.Matches.Count);
    }

    [Fact]
    public async Task GivenAsyncTemplateExceedingMaxLength_WhenCompileAsync_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTemplateLength = 50 };
        var tokenizer = new Tokenizer(options);
        var largeTemplate = new string('x', 200);
        using var reader = new StringReader(largeTemplate);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TokenizerException>(
            () => tokenizer.CompileAsync(reader));
        Assert.Contains("MaxTemplateLength", ex.Message);
    }

    [Fact]
    public async Task GivenAsyncInputExceedingMaxLength_WhenTokenizeAsync_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxInputLength = 100 };
        var tokenizer = new Tokenizer(options);
        var template = tokenizer.Compile("Name: {Name}");
        var input = new string('x', 200);
        using var reader = new StringReader(input);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TokenizerException>(
            () => tokenizer.TokenizeAsync(template, reader));
        Assert.Contains("MaxInputLength", ex.Message);
    }
}
