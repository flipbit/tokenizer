using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Tokenization.Engine;

public class TokenizationEngineEmptyPreambleTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenConsecutiveTokensWithNoPreambles_WhenTokenizing_ThenAssignsOneCharEach()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}{b}{c}");
        var context = new TokenizationContext();
        var input = "abc";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(3, result.Tokens.Matches.Count);
        Assert.Equal("a", result.Tokens.Matches[0].Value);
        Assert.Equal("b", result.Tokens.Matches[1].Value);
        Assert.Equal("c", result.Tokens.Matches[2].Value);
    }

    [Fact]
    public void GivenConsecutiveTokensWithNoPreambles_WhenInputLongerThanTokens_ThenLastTokenGetsRemainder()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}{b}{c}");
        var context = new TokenizationContext();
        var input = "abcdef";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(3, result.Tokens.Matches.Count);
        Assert.Equal("a", result.Tokens.Matches[0].Value);
        Assert.Equal("b", result.Tokens.Matches[1].Value);
        Assert.Equal("cdef", result.Tokens.Matches[2].Value);
    }

    [Fact]
    public void GivenConsecutiveTokensWithNoPreambles_WhenInputShorterThanTokens_ThenUnmatchedTokensAreMisses()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}{b}{c}");
        var context = new TokenizationContext();
        var input = "ab";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal("a", result.Tokens.Matches[0].Value);
        Assert.Equal("b", result.Tokens.Matches[1].Value);
    }

    [Fact]
    public void GivenSingleTokenWithNoPreamble_WhenTokenizing_ThenGetsEntireInput()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}");
        var context = new TokenizationContext();
        var input = "hello";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("hello", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenMixedPreambleAndNoPreambleTokens_WhenTokenizing_ThenMatchesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("X{a}{b}Y{c}");
        var context = new TokenizationContext();
        var input = "XabYc";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(3, result.Tokens.Matches.Count);
        Assert.Equal("a", result.Tokens.Matches[0].Value);
        Assert.Equal("b", result.Tokens.Matches[1].Value);
        Assert.Equal("c", result.Tokens.Matches[2].Value);
    }

    [Fact]
    public void GivenTwoConsecutiveTokens_WhenSingleCharInput_ThenFirstTokenMatchesSecondMisses()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}{b}");
        var context = new TokenizationContext();
        var input = "x";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("x", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenManyConsecutiveTokensWithNoPreambles_WhenTokenizing_ThenCompletes()
    {
        // Arrange
        var templateBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            templateBuilder.Append($"{{t{i}}}");
        }

        var parser = new TokenParser();
        var template = parser.Parse(templateBuilder.ToString());
        var context = new TokenizationContext();
        var input = new string('x', 100);
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert — the key thing is that this completes (does not hang)
        Assert.Equal(100, result.Tokens.Matches.Count);
    }
}
