using Tokens.Compilation.Lexer;
using Tokens.Enumerators;
using Tokens.Exceptions;

namespace Tokens.Compilation.Parsing;

/// <summary>
/// Streaming token reader over <see cref="TemplateLexer"/> that provides minimal lookahead,
/// consumption, and convenience helpers for whitespace/newline skipping and error creation.
/// </summary>
internal sealed class TokenReader
{
    private readonly IEnumerator<LexerToken> _enumerator;
    private readonly Queue<LexerToken> _buffer = new Queue<LexerToken>(4);

    public TokenReader(IEnumerable<LexerToken> tokens)
    {
        if (tokens == null) throw new ArgumentNullException(nameof(tokens));
        _enumerator = tokens.GetEnumerator();
    }

    public LexerToken Peek(int lookahead = 0)
    {
        EnsureBuffered(lookahead + 1);
        if (_buffer.Count <= lookahead) return EndToken();
        var i = 0;
        foreach (var t in _buffer)
        {
            if (i == lookahead) return t;
            i++;
        }
        return EndToken();
    }

    public LexerToken Consume()
    {
        EnsureBuffered(1);
        if (_buffer.Count == 0) return EndToken();
        return _buffer.Dequeue();
    }

    public bool TryConsume(LexerTokenKind kind, out LexerToken? token)
    {
        var next = Peek(0);
        if (next.Kind == kind)
        {
            token = Consume();
            return true;
        }
        token = null;
        return false;
    }

    public LexerToken Expect(LexerTokenKind kind, string? messageWhenMissing = null)
    {
        var next = Peek(0);
        if (next.Kind != kind)
        {
            throw CreateError(next, messageWhenMissing ?? $"Expected {kind} but found {next.Kind}.");
        }
        return Consume();
    }

    public void SkipWhitespace()
    {
        while (true)
        {
            var k = Peek(0).Kind;
            if (k == LexerTokenKind.Whitespace) { Consume(); continue; }
            break;
        }
    }

    public void SkipNewlines()
    {
        while (true)
        {
            var k = Peek(0).Kind;
            if (k == LexerTokenKind.Newline) { Consume(); continue; }
            break;
        }
    }

    public string CaptureWindow(int _, int after)
    {
        // This simple implementation returns upcoming tokens' raw text; previous tokens are not stored to keep streaming.
        EnsureBuffered(after + 1);
        var result = new System.Text.StringBuilder();
        var i = 0;
        foreach (var t in _buffer)
        {
            if (i > after) break;
            if (i > 0) result.Append(' ');
            result.Append(t.RawText);
            i++;
        }
        return result.ToString();
    }

    public static ParsingException CreateError(LexerToken atToken, string message)
    {
        var loc = atToken?.Location ?? new FileLocation();
        return new ParsingException(message ?? "Parsing error.", loc);
    }

    private void EnsureBuffered(int count)
    {
        while (_buffer.Count < count)
        {
            if (_enumerator.MoveNext() == false) break;
            _buffer.Enqueue(_enumerator.Current);
        }
    }

    private static LexerToken EndToken()
    {
        return new LexerToken(LexerTokenKind.EndOfInput, string.Empty, string.Empty, new FileLocation(), 0, 0);
    }
}


