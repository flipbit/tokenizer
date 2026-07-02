using Tokens.Compilation.Lexer;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Parsing;

public class TokenReaderTests
{
    private static LexerToken Tok(LexerTokenKind kind, string value, string raw) =>
        new LexerToken(kind, value, raw, new FileLocation(), 0, raw?.Length ?? 0);

    [Fact]
    public void GivenTokens_WhenPeekingAndConsuming_ThenReturnsExpectedTokens()
    {
        // Arrange
        var tokens = new List<LexerToken>
        {
            Tok(LexerTokenKind.Identifier, "name", "name"),
            Tok(LexerTokenKind.Colon, ":", ":"),
            Tok(LexerTokenKind.Whitespace, " ", " "),
            Tok(LexerTokenKind.Identifier, "value", "value"),
            Tok(LexerTokenKind.EndOfInput, string.Empty, string.Empty)
        };
        var r = new TokenReader(tokens);

        // Act & Assert
        Assert.Equal(LexerTokenKind.Identifier, r.Peek().Kind);
        Assert.Equal(LexerTokenKind.Identifier, r.Consume().Kind);
        Assert.Equal(LexerTokenKind.Colon, r.Peek().Kind);
    }

    [Fact]
    public void GivenMatchingKind_WhenTryConsume_ThenReturnsTrueAndToken()
    {
        // Arrange
        var tokens = new List<LexerToken> { Tok(LexerTokenKind.Colon, ":", ":") };
        var r = new TokenReader(tokens);

        // Act
        var ok = r.TryConsume(LexerTokenKind.Colon, out var t);

        // Assert
        Assert.True(ok);
        Assert.Equal(LexerTokenKind.Colon, t!.Kind);
    }

    [Fact]
    public void GivenTrivia_WhenSkipping_ThenReaderAdvancesPastWhitespaceAndNewlines()
    {
        // Arrange
        var tokens = new List<LexerToken>
        {
            Tok(LexerTokenKind.Whitespace, " ", " "),
            Tok(LexerTokenKind.Newline, "\n", "\n"),
            Tok(LexerTokenKind.Identifier, "x", "x")
        };
        var r = new TokenReader(tokens);

        // Act
        r.SkipWhitespace();
        r.SkipNewlines();

        // Assert
        Assert.Equal(LexerTokenKind.Identifier, r.Peek().Kind);
    }

    [Fact]
    public void GivenMismatchedKind_WhenExpect_ThenThrowsParsingException()
    {
        // Arrange
        var tokens = new List<LexerToken> { Tok(LexerTokenKind.Identifier, "x", "x") };
        var r = new TokenReader(tokens);

        // Act & Assert
        Assert.Throws<ParsingException>(() => r.Expect(LexerTokenKind.Colon));
    }

    [Fact]
    public void GivenTokens_WhenCaptureWindow_ThenReturnsUpcomingRawText()
    {
        // Arrange
        var tokens = new List<LexerToken>
        {
            Tok(LexerTokenKind.Identifier, "name", "name"),
            Tok(LexerTokenKind.Colon, ":", ":"),
            Tok(LexerTokenKind.Identifier, "value", "value")
        };
        var r = new TokenReader(tokens);

        // Act
        var win = r.CaptureWindow(0, 2);

        // Assert
        Assert.Contains("name", win);
        Assert.Contains(":", win);
        Assert.Contains("value", win);
    }

    [Fact]
    public void GivenEmptyInput_WhenPeekingOrConsuming_ThenReturnsEndOfInput()
    {
        // Arrange
        var tokens = new List<LexerToken>();
        var r = new TokenReader(tokens);

        // Act & Assert
        Assert.Equal(LexerTokenKind.EndOfInput, r.Peek().Kind);
        Assert.Equal(LexerTokenKind.EndOfInput, r.Consume().Kind);
    }
}


