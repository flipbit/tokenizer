using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Enumerators;
using Tokens.Exceptions;

namespace Tokens.Compilation.Lexer;

/// <summary>
/// Performs lexical analysis on template definition input, producing a stream of <see cref="LexerToken"/>s.
/// </summary>
/// <remarks>
/// <para>
/// Background: The lexer separates character-by-character scanning from parsing to improve maintainability
/// and testability. It recognizes grammar elements (braces, modifiers, quoted strings, etc.) and emits
/// tokens with accurate <see cref="FileLocation"/> information.
/// </para>
/// <para>
/// Design decisions (PRD):
/// - Context-free (Decision #1): token kinds are recognized without semantic context (e.g., ':' is always <see cref="LexerTokenKind.Colon"/>)
/// - Single execution path (Decision #4): string and stream inputs are converted to <see cref="TextReader"/> and processed by a single core path
/// - Strict mode (Decision #5A): invalid/unrecognized input results in <see cref="LexerException"/>
/// - Newline normalization (Decision #5): both "\n" and "\r\n" produce a single <see cref="LexerTokenKind.Newline"/> token
/// </para>
/// </remarks>
/// <example>
/// Synchronous usage:
/// <code>
/// var lexer = new TemplateLexer();
/// foreach (var token in lexer.Tokenize("{name:ToUpper}"))
/// {
///     Console.WriteLine($"{token.Kind}: '{token.Value}' at {token.Location}");
/// }
/// </code>
/// </example>
public class TemplateLexer
{
    private readonly ILogger<TemplateLexer> log;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateLexer"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public TemplateLexer(ILogger<TemplateLexer>? logger = null)
    {
        log = logger ?? NullLogger<TemplateLexer>.Instance;
    }

    /// <summary>
    /// Internal reader that supports small lookahead via buffering.
    /// </summary>
    private sealed class LookaheadReader
    {
        private readonly TextReader inner;
#if NET8_0_OR_GREATER
        private char[] buffer;
        private int startIndex;
        private int length;
#else
        private readonly System.Collections.Generic.Queue<int> buffer = new System.Collections.Generic.Queue<int>(8);
#endif

        public LookaheadReader(TextReader inner)
        {
            this.inner = inner;
#if NET8_0_OR_GREATER
            buffer = new char[1024];
            startIndex = 0;
            length = 0;
#endif
        }

        public bool IsEof
        {
            get
            {
#if NET8_0_OR_GREATER
                if (length > 0) return false;
                return inner.Peek() == -1;
#else
                return buffer.Count == 0 && inner.Peek() == -1;
#endif
            }
        }

        public int PeekChar()
        {
#if NET8_0_OR_GREATER
            EnsureBuffered(1);
            return length > 0 ? buffer[startIndex] : -1;
#else
            EnsureBuffered(1);
            return buffer.Count > 0 ? buffer.Peek() : -1;
#endif
        }

        public string PeekString(int count)
        {
            if (count <= 0) return string.Empty;
#if NET8_0_OR_GREATER
            EnsureBuffered(count);
            if (length == 0) return string.Empty;
            var len = System.Math.Min(count, length);
            return new string(buffer, startIndex, len);
#else
            EnsureBuffered(count);
            if (buffer.Count == 0) return string.Empty;
            var arr = buffer.ToArray();
            var len = System.Math.Min(count, arr.Length);
            return new string(System.Array.ConvertAll(arr, c => (char)c), 0, len);
#endif
        }

        public int ReadChar()
        {
#if NET8_0_OR_GREATER
            EnsureBuffered(1);
            if (length == 0) return -1;
            var c = buffer[startIndex];
            startIndex++;
            length--;
            return c;
#else
            if (buffer.Count > 0)
            {
                return buffer.Dequeue();
            }
            return inner.Read();
#endif
        }

#if NET8_0_OR_GREATER
        private void EnsureBuffered(int count)
        {
            if (count <= 0) return;
            while (length < count)
            {
                // if there is space at the end, read into it
                if (startIndex + length < buffer.Length)
                {
                    var read = inner.Read(buffer, startIndex + length, buffer.Length - (startIndex + length));
                    if (read <= 0) break;
                    length += read;
                }
                else
                {
                    // compact existing data to the start
                    if (length > 0)
                    {
                        System.Array.Copy(buffer, startIndex, buffer, 0, length);
                    }
                    startIndex = 0;
                    var read = inner.Read(buffer, length, buffer.Length - length);
                    if (read <= 0) break;
                    length += read;
                }
            }
        }
#else
        private void EnsureBuffered(int count)
        {
            while (buffer.Count < count)
            {
                var next = inner.Read();
                if (next == -1) break;
                buffer.Enqueue(next);
            }
        }
#endif
    }

    /// <summary>
    /// Tokenizes the specified template definition string.
    /// </summary>
    /// <param name="input">The template definition string to tokenize.</param>
    /// <returns>An enumerable sequence of <see cref="LexerToken"/>.</returns>
    /// <remarks>
    /// This method converts the string to a <see cref="StringReader"/> and delegates to the
    /// <see cref="Tokenize(TextReader)"/> overload to ensure a single execution path.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public IEnumerable<LexerToken> Tokenize(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Lexing template pattern start: PatternLength={PatternLength}", input.Length);
        }
        using (var reader = new StringReader(input))
        {
            foreach (var token in Tokenize(reader))
            {
                yield return token;
            }
        }
        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Lexing template pattern complete: PatternLength={PatternLength}", input.Length);
        }
    }

    /// <summary>
    /// Tokenizes the specified <see cref="Stream"/> of template definition data.
    /// </summary>
    /// <param name="input">The input stream to tokenize.</param>
    /// <returns>An enumerable sequence of <see cref="LexerToken"/>.</returns>
    /// <remarks>
    /// This method wraps the stream in a <see cref="StreamReader"/> and delegates to the
    /// <see cref="Tokenize(TextReader)"/> overload to ensure a single execution path.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public IEnumerable<LexerToken> Tokenize(Stream input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        using (var reader = new StreamReader(input, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
        {
            foreach (var token in Tokenize(reader))
            {
                yield return token;
            }
        }
    }

    /// <summary>
    /// Tokenizes the specified <see cref="TextReader"/> of template definition data.
    /// </summary>
    /// <param name="input">The <see cref="TextReader"/> that provides the input characters.</param>
    /// <returns>An enumerable sequence of <see cref="LexerToken"/>.</returns>
    /// <remarks>
    /// Core streaming lexing path. All inputs are funneled through this method.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    /// <exception cref="LexerException">Thrown when invalid or unexpected input is encountered.</exception>
    public IEnumerable<LexerToken> Tokenize(TextReader input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        var reader = new LookaheadReader(input);
        var location = new FileLocation();
        var absolutePosition = 0;
        var braceDepth = 0;
        var inFrontMatter = false;

        if (log.IsEnabled(LogLevel.Trace))
        {
            log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                absolutePosition, location.Line, location.Column);
        }

        // Core streaming scanning loop; tokens are yielded lazily.
        while (reader.IsEof == false)
        {
            var currentPosition = absolutePosition;
            var peek = reader.PeekChar();
            if (peek != -1)
            {
                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Character consumed: Char='{Char}', Position={Position}, Line={Line}, Column={Column}",
                        (char)peek, absolutePosition, location.Line, location.Column);
                }
            }

            if (TryReadNewline(reader, location, ref absolutePosition, out var nl))
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                        nl.Kind, nl.Value, nl.RawText, nl.Start, nl.Length);
                    log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                }
                yield return nl;
                continue;
            }

            // Quoted strings are recognized inside token braces and front matter,
            // but NOT in preamble text between tokens.
            if ((braceDepth > 0 || inFrontMatter) && TryReadQuotedString(reader, location, ref absolutePosition, out var qs))
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                        qs.Kind, qs.Value, qs.RawText, qs.Start, qs.Length);
                    log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                }
                yield return qs;
                continue;
            }

            if (TryReadWhitespace(reader, location, ref absolutePosition, out var ws))
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                        ws.Kind, ws.Value, ws.RawText, ws.Start, ws.Length);
                    log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                }
                yield return ws;
                continue;
            }

            if (TryReadFrontMatter(reader, location, ref absolutePosition, out var fm))
            {
                inFrontMatter = !inFrontMatter;
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                        fm.Kind, fm.Value, fm.RawText, fm.Start, fm.Length);
                    log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                }
                yield return fm;
                continue;
            }

            if (TryReadEscapedBraces(reader, location, ref absolutePosition, out var esc))
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                        esc.Kind, esc.Value, esc.RawText, esc.Start, esc.Length);
                    log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                }
                yield return esc;
                continue;
            }

            if (TryReadStructural(reader, location, ref absolutePosition, out var st))
            {
                if (st.Kind == LexerTokenKind.OpenBrace) braceDepth++;
                else if (st.Kind == LexerTokenKind.CloseBrace && braceDepth > 0) braceDepth--;
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                        st.Kind, st.Value, st.RawText, st.Start, st.Length);
                    log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                }
                yield return st;
                continue;
            }

            if (TryReadModifier(reader, location, ref absolutePosition, out var md))
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                        md.Kind, md.Value, md.RawText, md.Start, md.Length);
                    log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                }
                yield return md;
                continue;
            }

            if (TryReadIdentifier(reader, location, ref absolutePosition, out var id))
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                        id.Kind, id.Value, id.RawText, id.Start, id.Length);
                    log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                }
                yield return id;
                continue;
            }

            if (TryReadText(reader, location, ref absolutePosition, out var tx))
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                        tx.Kind, tx.Value, tx.RawText, tx.Start, tx.Length);
                    log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                }
                yield return tx;
                continue;
            }

            // Fallback: consume unknown char as a single-character text token
            var fallbackLocation = location.Clone();
            var cfb = reader.ReadChar();
            if (cfb == -1) break;
            location.Increment((char)cfb);
            absolutePosition++;
            var sfb = ((char)cfb).ToString();
            var fallbackToken = new LexerToken(LexerTokenKind.Text, sfb, sfb, fallbackLocation, absolutePosition - 1, 1);
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Lexer token produced (fallback): Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                    fallbackToken.Kind, fallbackToken.Value, fallbackToken.RawText, fallbackToken.Start, fallbackToken.Length);
                log.LogTrace("Token boundary: scanning for next token at Position={Position}, Line={Line}, Column={Column}",
                    absolutePosition, location.Line, location.Column);
            }
            yield return fallbackToken;
        }

        // Emit EndOfInput token at the end of input
        var endToken = new LexerToken(
            LexerTokenKind.EndOfInput,
            value: string.Empty,
            rawText: string.Empty,
            location: location.Clone(),
            start: absolutePosition,
            length: 0);
        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Lexer token produced: Type={TokenType}, Value={Value}, RawText={RawText}, Position={Position}, Length={Length}",
                endToken.Kind, endToken.Value, endToken.RawText, endToken.Start, endToken.Length);
        }
        yield return endToken;
    }

    /// <summary>
    /// Advances one logical character, updating <see cref="FileLocation"/> with newline normalization.
    /// </summary>
    /// <param name="reader">The lookahead reader.</param>
    /// <param name="location">The current file location to update.</param>
    private static void Advance(LookaheadReader reader, FileLocation location)
    {
        var peek = reader.PeekChar();
        if (peek == -1) return;

        // Normalize CRLF and CR to a single newline step
        if (peek == '\r')
        {
            // consume '\r'
            reader.ReadChar();
            // consume optional '\n'
            if (reader.PeekChar() == '\n') reader.ReadChar();
            location.NewLine();
            return;
        }

        if (peek == '\n')
        {
            reader.ReadChar();
            location.NewLine();
            return;
        }

        // Regular character increments column
        var c = (char)reader.ReadChar();
        location.Increment(c);
    }

    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '.';
    }

    private static bool TryReadNewline(LookaheadReader reader, FileLocation location, ref int absolutePosition, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LexerToken? token)
    {
        token = null;
        var peek = reader.PeekChar();
        if (peek != '\r' && peek != '\n') return false;
        var tokenLocation = location.Clone();
        var raw = string.Empty;
        if (peek == '\r')
        {
            reader.ReadChar(); raw = "\r"; absolutePosition++;
            if (reader.PeekChar() == '\n') { reader.ReadChar(); raw = "\r\n"; absolutePosition++; }
        }
        else { reader.ReadChar(); raw = "\n"; absolutePosition++; }
        location.NewLine();
        token = new LexerToken(LexerTokenKind.Newline, "\n", raw, tokenLocation, absolutePosition - raw.Length, raw.Length);
        return true;
    }

    private bool TryReadQuotedString(LookaheadReader reader, FileLocation location, ref int absolutePosition, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LexerToken? token)
    {
        token = null;
        var peek = reader.PeekChar();
        if (peek != '\'' && peek != '"') return false;
        token = ReadQuotedStringToken(reader, ref absolutePosition, location, (char)peek);
        return true;
    }

    private static bool TryReadWhitespace(LookaheadReader reader, FileLocation location, ref int absolutePosition, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LexerToken? token)
    {
        token = null;
        var peek = reader.PeekChar();
        if (peek != ' ' && peek != '\t') return false;
        var tokenLocation = location.Clone();
        var start = absolutePosition;
        var sb = new System.Text.StringBuilder();
        while (reader.IsEof == false)
        {
            var p = reader.PeekChar(); if (p != ' ' && p != '\t') break;
            var ch = (char)reader.ReadChar(); sb.Append(ch); location.Increment(ch); absolutePosition++;
        }
        var text = sb.ToString(); if (text.Length == 0) return false;
        token = new LexerToken(LexerTokenKind.Whitespace, text, text, tokenLocation, start, text.Length);
        return true;
    }

    private static bool TryReadFrontMatter(LookaheadReader reader, FileLocation location, ref int absolutePosition, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LexerToken? token)
    {
        token = null;
        var next3 = reader.PeekString(3); if (next3 != "---") return false;
        var tokenLocation = location.Clone();
        reader.ReadChar(); location.Increment('-'); absolutePosition++;
        reader.ReadChar(); location.Increment('-'); absolutePosition++;
        reader.ReadChar(); location.Increment('-'); absolutePosition++;
        token = new LexerToken(LexerTokenKind.FrontMatterDelimiter, "---", "---", tokenLocation, absolutePosition - 3, 3);
        return true;
    }

    private static bool TryReadEscapedBraces(LookaheadReader reader, FileLocation location, ref int absolutePosition, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LexerToken? token)
    {
        token = null;
        var next2 = reader.PeekString(2);
        if (next2 == "{{")
        {
            var tokenLocation = location.Clone();
            reader.ReadChar(); location.Increment('{'); absolutePosition++;
            reader.ReadChar(); location.Increment('{'); absolutePosition++;
            token = new LexerToken(LexerTokenKind.EscapedOpenBrace, "{{", "{{", tokenLocation, absolutePosition - 2, 2);
            return true;
        }
        if (next2 == "}}")
        {
            var tokenLocation = location.Clone();
            reader.ReadChar(); location.Increment('}'); absolutePosition++;
            reader.ReadChar(); location.Increment('}'); absolutePosition++;
            token = new LexerToken(LexerTokenKind.EscapedCloseBrace, "}}", "}}", tokenLocation, absolutePosition - 2, 2);
            return true;
        }
        return false;
    }

    private static bool TryReadStructural(LookaheadReader reader, FileLocation location, ref int absolutePosition, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LexerToken? token)
    {
        token = null;
        var peek = reader.PeekChar();
        if (peek != '{' && peek != '}' && peek != ':' && peek != '=' && peek != ',' && peek != '(' && peek != ')') return false;
        var tokenLocation = location.Clone();
        var ch = (char)reader.ReadChar(); location.Increment(ch); absolutePosition++;
        var kind = ch == '{' ? LexerTokenKind.OpenBrace :
                   ch == '}' ? LexerTokenKind.CloseBrace :
                   ch == ':' ? LexerTokenKind.Colon :
                   ch == '=' ? LexerTokenKind.Equals :
                   ch == ',' ? LexerTokenKind.Comma :
                   ch == '(' ? LexerTokenKind.OpenParen : LexerTokenKind.CloseParen;
        var s = ch.ToString();
        token = new LexerToken(kind, s, s, tokenLocation, absolutePosition - 1, 1);
        return true;
    }

    private static bool TryReadModifier(LookaheadReader reader, FileLocation location, ref int absolutePosition, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LexerToken? token)
    {
        token = null;
        var peek = reader.PeekChar();
        if (peek != '?' && peek != '*' && peek != '!' && peek != '$' && peek != '#') return false;
        var tokenLocation = location.Clone();
        var ch = (char)reader.ReadChar(); location.Increment(ch); absolutePosition++;
        var kind = ch == '?' ? LexerTokenKind.Question :
                   ch == '*' ? LexerTokenKind.Asterisk :
                   ch == '!' ? LexerTokenKind.Exclamation :
                   ch == '$' ? LexerTokenKind.Dollar : LexerTokenKind.Hash;
        var s = ch.ToString();
        token = new LexerToken(kind, s, s, tokenLocation, absolutePosition - 1, 1);
        return true;
    }

    private static bool TryReadIdentifier(LookaheadReader reader, FileLocation location, ref int absolutePosition, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LexerToken? token)
    {
        token = null;
        var peek = reader.PeekChar(); if (peek == -1 || IsIdentifierChar((char)peek) == false) return false;
        var tokenLocation = location.Clone(); var start = absolutePosition;
        var sb = new System.Text.StringBuilder();
        while (reader.IsEof == false)
        {
            var p = reader.PeekChar(); if (p == -1 || IsIdentifierChar((char)p) == false) break;
            var ch = (char)reader.ReadChar(); sb.Append(ch); location.Increment(ch); absolutePosition++;
        }
        var text = sb.ToString(); if (text.Length == 0) return false;
        token = new LexerToken(LexerTokenKind.Identifier, text, text, tokenLocation, start, text.Length);
        return true;
    }

    private static bool TryReadText(LookaheadReader reader, FileLocation location, ref int absolutePosition, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LexerToken? token)
    {
        token = null;
        var tokenLocation = location.Clone(); var start = absolutePosition;
        var sb = new System.Text.StringBuilder();
        while (reader.IsEof == false)
        {
            var p = reader.PeekChar(); if (p == -1) break; var c = (char)p;
            if (c == '\r' || c == '\n' || c == ' ' || c == '\t') break;
            if (c == '{' || c == '}' || c == ':' || c == '=' || c == ',' || c == '(' || c == ')') break;
            if (c == '?' || c == '*' || c == '!' || c == '$' || c == '#') break;
            if (IsIdentifierChar(c)) break;
            var ch = (char)reader.ReadChar(); sb.Append(ch); location.Increment(ch); absolutePosition++;
        }
        var text = sb.ToString(); if (text.Length == 0) return false;
        token = new LexerToken(LexerTokenKind.Text, text, text, tokenLocation, start, text.Length);
        return true;
    }

    /// <summary>
    /// Reads a quoted string token, consuming the opening and closing quotes.
    /// Emits a QuotedString token where Value excludes quotes and RawText preserves them.
    /// Throws <see cref="LexerException"/> on EOF before closing quote.
    /// </summary>
    private LexerToken ReadQuotedStringToken(LookaheadReader reader, ref int absolutePosition, FileLocation location, char quote)
    {
        var tokenLocation = location.Clone();
        var start = absolutePosition;
        var raw = new System.Text.StringBuilder();
        var inner = new System.Text.StringBuilder();

        // consume opening quote
        reader.ReadChar();
        raw.Append(quote);
        location.Increment(quote);
        absolutePosition++;

        while (true)
        {
            var p = reader.PeekChar();
            if (p == -1)
            {
                // EOF before closing quote
                log.LogError("Lexing failure: Unclosed quoted string at Position={Position}, Line={Line}, Column={Column}, ExpectedQuote={Quote}",
                    absolutePosition, location.Line, location.Column, quote);
                throw new LexerException($"Unclosed quoted string. Expected closing '{quote}'.", location.Clone());
            }

            if (p == '\r')
            {
                reader.ReadChar();
                raw.Append('\r');
                absolutePosition++;
                if (reader.PeekChar() == '\n')
                {
                    reader.ReadChar();
                    raw.Append('\n');
                    absolutePosition++;
                }
                location.NewLine();
                continue;
            }

            if (p == '\n')
            {
                reader.ReadChar();
                raw.Append('\n');
                absolutePosition++;
                location.NewLine();
                continue;
            }

            if ((char)p == quote)
            {
                // consume closing quote
                reader.ReadChar();
                raw.Append(quote);
                location.Increment(quote);
                absolutePosition++;
                break;
            }

            // Handle escape sequences (only quote and backslash are supported)
            if ((char)p == '\\')
            {
                // consume backslash
                reader.ReadChar();
                raw.Append('\\');
                location.Increment('\\');
                absolutePosition++;

                var next = reader.PeekChar();
                if (next == -1)
                {
                    log.LogError("Lexing failure: Unclosed quoted string after escape at Position={Position}, Line={Line}, Column={Column}",
                        absolutePosition, location.Line, location.Column);
                    throw new LexerException("Unclosed quoted string after escape.", location.Clone());
                }

                var nextChar = (char)next;
                // Only allow escaping the quote character in use, or a backslash
                if (nextChar == quote || nextChar == '\\')
                {
                    reader.ReadChar();
                    raw.Append(nextChar);
                    inner.Append(nextChar);
                    location.Increment(nextChar);
                    absolutePosition++;
                    continue;
                }

                // Unknown escape sequence — treat as literal character
                log.LogWarning("Unknown escape sequence '\\{EscapeChar}' at Position={Position}, Line={Line}, Column={Column} — treating as literal '{LiteralChar}'",
                    nextChar, absolutePosition, location.Line, location.Column, nextChar);
                reader.ReadChar();
                raw.Append(nextChar);
                inner.Append(nextChar);
                location.Increment(nextChar);
                absolutePosition++;
                continue;
            }

            var ch = (char)reader.ReadChar();
            raw.Append(ch);
            inner.Append(ch);
            location.Increment(ch);
            absolutePosition++;
        }

        var rawText = raw.ToString();
        var value = inner.ToString();
        return new LexerToken(
            LexerTokenKind.QuotedString,
            value: value,
            rawText: rawText,
            location: tokenLocation,
            start: start,
            length: rawText.Length);
    }
}


