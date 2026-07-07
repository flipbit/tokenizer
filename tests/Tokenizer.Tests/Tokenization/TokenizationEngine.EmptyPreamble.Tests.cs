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
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
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
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
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
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
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
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
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
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
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
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("x", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenConsecutiveTokensWithEmptyPreamble_WhenTargetHasNoSettableProperties_ThenThrowsArgumentException()
    {
        // Arrange — a read-only target with no settable properties is rejected at the entry-point
        // validation before the tokenization loop begins. The error message documents that the
        // empty-preambles guard (which would fire deeper in the loop) is pre-empted by this check.
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(
                new TokenBuilder()
                    .WithContent("{First}")
                    .WithName("First")
                    .WithPreamble("")
                    .Build(),
                new TokenBuilder()
                    .WithContent("{Second}")
                    .WithName("Second")
                    .WithPreamble("")
                    .Build())
            .WithDefaultOptions()
            .Build();

        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        var target = new ReadOnlyTarget("value");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _engine.CreateSession(template, target, result, NullDiagnosticCollector.Instance));

        Assert.Contains("no settable properties", ex.Message, StringComparison.Ordinal);
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
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert — the key thing is that this completes (does not hang)
        Assert.Equal(100, result.Tokens.Matches.Count);
    }

    private sealed class ReadOnlyTarget
    {
        public ReadOnlyTarget(string name) { Name = name; }
        public string Name { get; }
    }
}
