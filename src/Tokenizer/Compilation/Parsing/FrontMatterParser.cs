using Tokens.Compilation.Lexer;
using Tokens.Compilation.Nodes;
using Tokens.Enumerators;
using Tokens.Exceptions;

namespace Tokens.Compilation.Parsing;

/// <summary>
/// Parses front matter syntax delimited by lines containing '---' at the start and end.
/// Produces a <see cref="FrontMatterBlock"/> with ordered entries.
/// </summary>
internal sealed class FrontMatterParser
{
    public FrontMatterBlock Parse(TokenReader reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        // Must start with front matter delimiter followed by newline
        var startDelim = reader.Peek(0);
        if (startDelim.Kind != LexerTokenKind.FrontMatterDelimiter)
        {
            throw TokenReader.CreateError(startDelim, "Expected front matter opening delimiter '---'.");
        }
        var blockStartLoc = startDelim.Location.Clone();
        var blockStart = startDelim.Start;
        reader.Consume();
        var nl = reader.Peek(0);
        if (nl.Kind != LexerTokenKind.Newline)
        {
            throw TokenReader.CreateError(nl, "Expected newline after front matter opening delimiter '---'.");
        }
        reader.Consume();

        var entries = new List<SyntaxNode>();

        while (true)
        {
            // Start of a new line: allow leading indentation
            while (reader.Peek(0).Kind == LexerTokenKind.Whitespace)
            {
                reader.Consume();
            }
            var next = reader.Peek(0);
            // Skip empty lines
            if (next.Kind == LexerTokenKind.Newline)
            {
                reader.Consume();
                continue;
            }
            // Check for closing delimiter at start of line (after optional indentation)
            if (next.Kind == LexerTokenKind.FrontMatterDelimiter)
            {
                // Must be followed by newline to close
                var closeDelim = reader.Consume();
                var after = reader.Peek(0);
                if (after.Kind != LexerTokenKind.Newline)
                {
                    throw TokenReader.CreateError(after, "Expected newline after front matter closing delimiter '---'.");
                }
                reader.Consume();
                var blockEnd = after.End; // after newline
                var length = Math.Max(0, blockEnd - blockStart);
                return new FrontMatterBlock(blockStartLoc, blockStart, length, entries);
            }
            if (next.Kind == LexerTokenKind.EndOfInput)
            {
                throw TokenReader.CreateError(next, "Unterminated front matter. Missing closing '---'.");
            }

            // Parse a front matter line
            var lineNode = ParseLine(reader);
            if (lineNode != null)
            {
                entries.Add(lineNode);
            }
        }
    }

    private SyntaxNode? ParseLine(TokenReader reader)
    {
        // Capture start
        var first = reader.Peek(0);
        var startLoc = first.Location.Clone();
        var start = first.Start;

        // Skip leading whitespace on the line
        while (reader.Peek(0).Kind == LexerTokenKind.Whitespace)
        {
            reader.Consume();
        }

        var t = reader.Peek(0);
        if (t.Kind == LexerTokenKind.Newline)
        {
            // Empty line, consume and ignore
            reader.Consume();
            return null;
        }
        if (t.Kind == LexerTokenKind.Hash)
        {
            // Comment line: consume until newline
            var raw = new System.Text.StringBuilder();
            while (reader.Peek(0).Kind != LexerTokenKind.Newline && reader.Peek(0).Kind != LexerTokenKind.EndOfInput)
            {
                var tok = reader.Consume();
                raw.Append(tok.RawText);
            }
            // Consume newline
            var endTok = reader.Expect(LexerTokenKind.Newline, "Expected newline at end of comment line.");
            var length = Math.Max(0, endTok.End - start);
            return new FrontMatterComment(startLoc, start, length, raw.ToString());
        }

        // Option or directive: read key until colon
        var keyParts = new List<LexerToken>();
        while (true)
        {
            var k = reader.Peek(0);
            if (k.Kind == LexerTokenKind.Colon) break;
            if (k.Kind == LexerTokenKind.Newline || k.Kind == LexerTokenKind.EndOfInput)
            {
                throw TokenReader.CreateError(k, "Expected ':' after front matter option key.");
            }
            keyParts.Add(reader.Consume());
        }
        // consume colon
        reader.Expect(LexerTokenKind.Colon);

        // Read remainder of line as value tokens (until newline)
        var valueTokens = new List<LexerToken>();
        while (reader.Peek(0).Kind != LexerTokenKind.Newline && reader.Peek(0).Kind != LexerTokenKind.EndOfInput)
        {
            valueTokens.Add(reader.Consume());
        }
        // consume newline
        var eol = reader.Expect(LexerTokenKind.Newline, "Expected newline at end of front matter line.");

        // Build key string (trim outside whitespace)
        var keyRaw = string.Concat(keyParts.Select(p => p.RawText));
        var key = keyRaw.Trim();

        // Special-case: set directive
        if (string.Equals(key, "set", StringComparison.InvariantCultureIgnoreCase))
        {
            var length = Math.Max(0, eol.End - start);
            var directive = ParseSetDirective(valueTokens, startLoc, start, length);
            return directive;
        }

        // Build value strings
        var rawValue = string.Concat(valueTokens.Select(p => p.RawText));
        // Normalized: trim outside quotes; preserve inner quoted whitespace
        var normalized = NormalizeFrontMatterValue(valueTokens);
        var presentedValue = normalized; // Value property aligns with normalized interpretation

        var totalLength = Math.Max(0, eol.End - start);
        return new FrontMatterEntry(startLoc, start, totalLength, key, presentedValue, rawValue, normalized);
    }

    private static SetTokenDirective ParseSetDirective(List<LexerToken> tokens, FileLocation startLoc, int start, int length)
    {
        var i = 0;
        while (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Whitespace) i++;

        // Name
        var nameSb = new System.Text.StringBuilder();
        for (; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Kind == LexerTokenKind.Equals || t.Kind == LexerTokenKind.Colon || t.Kind == LexerTokenKind.Newline)
            {
                break;
            }
            if (t.Kind == LexerTokenKind.Whitespace)
            {
                continue;
            }
            nameSb.Append(t.Value);
        }
        var name = nameSb.ToString().Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new ParsingException("Expected token name after 'set:' in front matter.", new FileLocation());
        }

        // Optional value
        string? value = null;
        while (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Whitespace) i++;
        if (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Equals)
        {
            i++; // consume '='
            while (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Whitespace) i++;
            if (i < tokens.Count)
            {
                if (tokens[i].Kind == LexerTokenKind.QuotedString)
                {
                    value = tokens[i].Value;
                    i++;
                }
                else
                {
                    var valSb = new System.Text.StringBuilder();
                    var lastWasSpace = false;
                    for (; i < tokens.Count; i++)
                    {
                        var t = tokens[i];
                        if (t.Kind == LexerTokenKind.Colon || t.Kind == LexerTokenKind.Newline) break;
                        if (t.Kind == LexerTokenKind.Whitespace)
                        {
                            if (!lastWasSpace && valSb.Length > 0) { valSb.Append(' '); lastWasSpace = true; }
                            continue;
                        }
                        valSb.Append(t.Value);
                        lastWasSpace = false;
                    }
                    value = valSb.ToString().Trim();
                }
            }
            while (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Whitespace) i++;
        }

        // Optional decorators chain
        var decorators = new System.Collections.Generic.List<SetDecorator>();
        while (i < tokens.Count)
        {
            while (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Whitespace) i++;
            if (i >= tokens.Count || tokens[i].Kind != LexerTokenKind.Colon) break;
            i++; // consume ':'
            while (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Whitespace) i++;

            // Decorator name
            var dnameSb = new System.Text.StringBuilder();
            for (; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.Kind == LexerTokenKind.OpenParen || t.Kind == LexerTokenKind.Colon || t.Kind == LexerTokenKind.Newline) break;
                if (t.Kind == LexerTokenKind.Whitespace) { continue; }
                dnameSb.Append(t.Value);
            }
            var dname = dnameSb.ToString().Trim();
            var args = new System.Collections.Generic.List<string>();

            while (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Whitespace) i++;
            if (i < tokens.Count && tokens[i].Kind == LexerTokenKind.OpenParen)
            {
                i++; // consume '('
                while (i < tokens.Count)
                {
                    while (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Whitespace) i++;
                    if (i < tokens.Count && tokens[i].Kind == LexerTokenKind.CloseParen) { i++; break; }
                    if (i >= tokens.Count) break;

                    if (tokens[i].Kind == LexerTokenKind.QuotedString)
                    {
                        args.Add(tokens[i].Value);
                        i++;
                    }
                    else
                    {
                        var asb = new System.Text.StringBuilder();
                        for (; i < tokens.Count; i++)
                        {
                            var t = tokens[i];
                            if (t.Kind == LexerTokenKind.Comma || t.Kind == LexerTokenKind.CloseParen || t.Kind == LexerTokenKind.Newline) break;
                            if (t.Kind == LexerTokenKind.Whitespace)
                            {
                                if (asb.Length > 0) asb.Append(' ');
                                continue;
                            }
                            asb.Append(t.Value);
                        }
                        args.Add(asb.ToString().Trim());
                    }

                    if (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Comma) { i++; continue; }
                }
            }

            decorators.Add(new SetDecorator(dname, args));
        }

        return new SetTokenDirective(startLoc, start, length, name, value, decorators);
    }

    private static string NormalizeFrontMatterValue(List<LexerToken> tokens)
    {
        // Trim whitespace tokens only at the boundaries, preserve whitespace inside quoted strings.
        var i = 0;
        while (i < tokens.Count && tokens[i].Kind == LexerTokenKind.Whitespace) i++;
        var j = tokens.Count - 1;
        while (j >= i && tokens[j].Kind == LexerTokenKind.Whitespace) j--;

        var sb = new System.Text.StringBuilder();
        var lastWasSpace = false;
        for (var k = i; k <= j; k++)
        {
            var t = tokens[k];
            if (t.Kind == LexerTokenKind.Whitespace)
            {
                if (lastWasSpace == false)
                {
                    sb.Append(' '); // collapse runs to single space between parts
                    lastWasSpace = true;
                }
                continue;
            }

            // Non-whitespace
            if (t.Kind == LexerTokenKind.QuotedString)
            {
                sb.Append(t.Value); // include quoted content exactly
            }
            else
            {
                sb.Append(t.Value);
            }
            lastWasSpace = false;
        }
        return sb.ToString();
    }
}


