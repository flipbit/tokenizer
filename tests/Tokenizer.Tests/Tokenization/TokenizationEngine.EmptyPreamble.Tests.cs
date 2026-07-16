using System.Globalization;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Xunit;

#pragma warning disable MA0048 // Scenario test: TokenizationEngine.EmptyPreamble.Tests.cs
namespace Tokens.Tokenization;

public class TokenizationEngineEmptyPreambleTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenConsecutiveTokensWithNoPreambles_WhenTokenizing_ThenAssignsOneCharEach()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("{a}{b}{c}").Template;
        var context = new TokenizationContext();
        var input = "abc";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var session = _engine.CreateSession(template, result, NullTokenizationDiagnosticCollector.Instance);
        session.Run(context);

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
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("{a}{b}{c}").Template;
        var context = new TokenizationContext();
        var input = "abcdef";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var session = _engine.CreateSession(template, result, NullTokenizationDiagnosticCollector.Instance);
        session.Run(context);

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
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("{a}{b}{c}").Template;
        var context = new TokenizationContext();
        var input = "ab";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var session = _engine.CreateSession(template, result, NullTokenizationDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal("a", result.Tokens.Matches[0].Value);
        Assert.Equal("b", result.Tokens.Matches[1].Value);
    }

    [Fact]
    public void GivenSingleTokenWithNoPreamble_WhenTokenizing_ThenGetsEntireInput()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("{a}").Template;
        var context = new TokenizationContext();
        var input = "hello";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var session = _engine.CreateSession(template, result, NullTokenizationDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("hello", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenMixedPreambleAndNoPreambleTokens_WhenTokenizing_ThenMatchesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("X{a}{b}Y{c}").Template;
        var context = new TokenizationContext();
        var input = "XabYc";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var session = _engine.CreateSession(template, result, NullTokenizationDiagnosticCollector.Instance);
        session.Run(context);

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
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("{a}{b}").Template;
        var context = new TokenizationContext();
        var input = "x";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var session = _engine.CreateSession(template, result, NullTokenizationDiagnosticCollector.Instance);
        session.Run(context);

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
            templateBuilder.Append(CultureInfo.InvariantCulture, $"{{t{i}}}");
        }

        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile(templateBuilder.ToString()).Template;
        var context = new TokenizationContext();
        var input = new string('x', 100);
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        var session = _engine.CreateSession(template, result, NullTokenizationDiagnosticCollector.Instance);
        session.Run(context);

        // Assert — the key thing is that this completes (does not hang)
        Assert.Equal(100, result.Tokens.Matches.Count);
    }

}
