using System;
using System.Collections.Generic;
using Tokens.Compilation.Lexer;
using Tokens.Compilation.Nodes;
using Tokens.Enumerators;

namespace Tokens.Compilation.Parsing
{
    /// <summary>
    /// Coordinates parsing of a template document.
    /// Phase 1: front matter + content stub. Phase 2: parse tokens/preamble into AST.
    /// </summary>
    internal sealed class TemplateParser
    {
        private readonly TemplateLexer lexer = new TemplateLexer();

        public TemplateDocument Parse(string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            return Parse(lexer.Tokenize(input));
        }

        public TemplateDocument Parse(IEnumerable<LexerToken> tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            var reader = new TokenReader(tokens);
            var first = reader.Peek(0);
            // Allow leading whitespace/newlines before front matter block
            while (first.Kind == LexerTokenKind.Whitespace || first.Kind == LexerTokenKind.Newline)
            {
                reader.Consume();
                first = reader.Peek(0);
            }
            var docStartLoc = first.Location.Clone();
            var docStart = first.Start;

            FrontMatterBlock frontMatter = null;
            if (first.Kind == LexerTokenKind.FrontMatterDelimiter)
            {
                var fmParser = new FrontMatterParser();
                frontMatter = fmParser.Parse(reader);
            }

            var contentNodes = ParseContent(reader);

            var endTok = reader.Peek(0);
            var docLength = Math.Max(0, endTok.End - docStart);
            return new TemplateDocument(docStartLoc, docStart, docLength, frontMatter, contentNodes);
        }

        private static List<ContentNode> ParseContent(TokenReader reader)
        {
            var nodes = new List<ContentNode>();
            while (true)
            {
                var next = reader.Peek(0);
                if (next.Kind == LexerTokenKind.EndOfInput) break;
                if (next.Kind == LexerTokenKind.OpenBrace)
                {
                    nodes.Add(ParseToken(reader));
                    continue;
                }
                nodes.Add(ParseText(reader));
            }
            return nodes;
        }

        private static ContentNode ParseText(TokenReader reader)
        {
            var startTok = reader.Peek(0);
            var start = startTok.Start;
            var loc = startTok.Location.Clone();
            var sb = new System.Text.StringBuilder();
            int end = start;
            while (true)
            {
                var k = reader.Peek(0).Kind;
                if (k == LexerTokenKind.EndOfInput || k == LexerTokenKind.OpenBrace) break;
                var t = reader.Consume();
                // Convert escaped braces to literal
                if (t.Kind == LexerTokenKind.EscapedOpenBrace) { sb.Append('{'); end = t.End; continue; }
                if (t.Kind == LexerTokenKind.EscapedCloseBrace) { sb.Append('}'); end = t.End; continue; }
                // Bare '}' is invalid in text (must be escaped as '}}')
                if (t.Kind == LexerTokenKind.CloseBrace)
                {
                    throw TokenReader.CreateError(t, "Unescaped '}' in text.");
                }
                sb.Append(t.Value);
                end = t.End;
            }
            var length = System.Math.Max(0, end - start);
            return new TextNode(loc, start, length, sb.ToString());
        }

        private static TokenNode ParseToken(TokenReader reader)
        {
            var open = reader.Expect(LexerTokenKind.OpenBrace, "Expected '{' to start token.");
            var start = open.Start;
            var loc = open.Location.Clone();
            reader.SkipWhitespace();
            var nameTok = reader.Expect(LexerTokenKind.Identifier, "Expected token name.");
            var name = new TokenName(nameTok.Value);
            // Allow whitespace before inline modifier symbols
            reader.SkipWhitespace();
            var modifiers = ParseModifiers(reader);
            ValueNode value = null;
            reader.SkipWhitespace();
            if (reader.TryConsume(LexerTokenKind.Equals, out _))
            {
                reader.SkipWhitespace();
                var v = reader.Peek(0);
                if (v.Kind == LexerTokenKind.QuotedString)
                {
                    var qs = reader.Consume();
                    value = new ValueNode(qs.Value, isQuoted: true);
                }
                else if (v.Kind == LexerTokenKind.Identifier)
                {
                    var id = reader.Consume();
                    value = new ValueNode(id.Value, isQuoted: false);
                }
                else
                {
                    throw TokenReader.CreateError(v, "Expected quoted string or identifier as value.");
                }
            }

            var decorators = new List<DecoratorNode>();
            while (true)
            {
                reader.SkipWhitespace();
                if (reader.TryConsume(LexerTokenKind.Colon, out _))
                {
                    // After a colon, allow one or more decorators separated by commas
                    while (true)
                    {
                        reader.SkipWhitespace();
                        bool isNot = false;
                        if (reader.TryConsume(LexerTokenKind.Exclamation, out _))
                        {
                            isNot = true;
                        }
                        var dname = reader.Expect(LexerTokenKind.Identifier, "Expected decorator name.");
                        var args = ParseDecoratorArgs(reader);
                        decorators.Add(new DecoratorNode(new TokenName(dname.Value), args, isNot));
                        reader.SkipWhitespace();
                        if (reader.TryConsume(LexerTokenKind.Comma, out _))
                        {
                            // Next decorator in the same ':' group
                            continue;
                        }
                        break;
                    }
                    // Look for another ':' group
                    continue;
                }
                break;
            }

            reader.SkipWhitespace();
            var close = reader.Expect(LexerTokenKind.CloseBrace, "Expected '}' to close token.");
            var length = System.Math.Max(0, close.End - start);
            return new TokenNode(loc, start, length, name, modifiers, value, decorators);
        }

        private static ModifierSet ParseModifiers(TokenReader reader)
        {
            bool opt = false, rep = false, req = false, term = false;
            while (true)
            {
                var k = reader.Peek(0).Kind;
                if (k == LexerTokenKind.Whitespace) { reader.Consume(); continue; }
                if (k == LexerTokenKind.Question) { reader.Consume(); opt = true; continue; }
                if (k == LexerTokenKind.Asterisk) { reader.Consume(); rep = true; continue; }
                if (k == LexerTokenKind.Exclamation) { reader.Consume(); req = true; continue; }
                if (k == LexerTokenKind.Dollar) { reader.Consume(); term = true; continue; }
                break;
            }
            return new ModifierSet(opt, rep, req, term);
        }

        private static IReadOnlyList<ArgumentNode> ParseDecoratorArgs(TokenReader reader)
        {
            var args = new List<ArgumentNode>();
            reader.SkipWhitespace();
            if (reader.TryConsume(LexerTokenKind.OpenParen, out _))
            {
                reader.SkipWhitespace();
                while (true)
                {
                    var next = reader.Peek(0);
                    if (next.Kind == LexerTokenKind.CloseParen)
                    {
                        reader.Consume();
                        break;
                    }
                    // Disallow a leading comma before any argument
                    if (next.Kind == LexerTokenKind.Comma && args.Count == 0)
                    {
                        throw TokenReader.CreateError(next, "Expected argument or ')' in decorator.");
                    }
                    // Argument can be quoted string...
                    if (next.Kind == LexerTokenKind.QuotedString)
                    {
                        var qs = reader.Consume();
                        args.Add(new ArgumentNode(qs.Value, isQuoted: true));
                        reader.SkipWhitespace();
                        if (reader.TryConsume(LexerTokenKind.Comma, out _)) { reader.SkipWhitespace(); continue; }
                        continue;
                    }
                    // ...or unquoted: collect tokens until ',' or ')'
                    var sb = new System.Text.StringBuilder();
                    var lastWasSpace = false;
                    while (true)
                    {
                        next = reader.Peek(0);
                        if (next.Kind == LexerTokenKind.Comma || next.Kind == LexerTokenKind.CloseParen) break;
                        if (next.Kind == LexerTokenKind.Newline || next.Kind == LexerTokenKind.EndOfInput) break;
                        // Disallow nested parentheses inside unquoted arguments
                        if (next.Kind == LexerTokenKind.OpenParen)
                        {
                            throw TokenReader.CreateError(next, "Expected argument or ')' in decorator.");
                        }
                        if (next.Kind == LexerTokenKind.Whitespace)
                        {
                            if (sb.Length > 0 && lastWasSpace == false)
                            {
                                sb.Append(' ');
                                lastWasSpace = true;
                            }
                            reader.Consume();
                            continue;
                        }
                        var tok = reader.Consume();
                        sb.Append(tok.Value);
                        lastWasSpace = false;
                    }
                    var text = sb.ToString().Trim();
                    if (text.Length > 0)
                    {
                        args.Add(new ArgumentNode(text, isQuoted: false));
                    }
                    else
                    {
                        // Empty argument between separators
                        var sep = reader.Peek(0);
                        if (sep.Kind == LexerTokenKind.Comma)
                        {
                            throw TokenReader.CreateError(sep, "Expected argument or ')' in decorator.");
                        }
                    }
                    reader.SkipWhitespace();
                    if (reader.TryConsume(LexerTokenKind.Comma, out _)) { reader.SkipWhitespace(); continue; }
                }
            }
            return args;
        }
    }
}


