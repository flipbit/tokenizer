using System.Linq;
using Tokens.Compilation.Lexer;
using Tokens.Compilation.Nodes;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Parsing
{
    public class FrontMatterParserTests
    {
        private static LexerToken Tok(LexerTokenKind kind, string value, string raw) =>
            new LexerToken(kind, value, raw, new FileLocation(), 0, raw?.Length ?? 0);

        private static FrontMatterBlock Parse(params LexerToken[] tokens)
        {
            var reader = new TokenReader(tokens);
            var parser = new FrontMatterParser();
            return parser.Parse(reader);
        }

        [Fact]
        public void GivenOptionLine_WhenParsing_ThenEmitsFrontMatterEntry()
        {
            // Arrange & Act
            var fm = Parse(
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.Identifier, "name", "name"), Tok(LexerTokenKind.Colon, ":", ":"), Tok(LexerTokenKind.Whitespace, " ", " "), Tok(LexerTokenKind.Identifier, "Template", "Template"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n")
            );
            // Assert
            var entry = Assert.IsType<FrontMatterEntry>(fm.Entries.Single());
            Assert.Equal("name", entry.Key);
            Assert.Equal("Template", entry.Value);
        }

        [Fact]
        public void GivenSetDirective_WhenParsing_ThenEmitsSetTokenDirective()
        {
            // Arrange & Act
            var fm = Parse(
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.Identifier, "set", "set"), Tok(LexerTokenKind.Colon, ":", ":"), Tok(LexerTokenKind.Whitespace, " ", " "),
                Tok(LexerTokenKind.Identifier, "value", "value"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n")
            );
            // Assert
            var dir = Assert.IsType<SetTokenDirective>(fm.Entries.Single());
            Assert.Equal("value", dir.TokenName);
        }

        [Fact]
        public void GivenCommentLine_WhenParsing_ThenEmitsFrontMatterComment()
        {
            // Arrange & Act
            var fm = Parse(
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.Hash, "#", "#"), Tok(LexerTokenKind.Identifier, "comment", "comment"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n")
            );
            // Assert
            Assert.IsType<FrontMatterComment>(fm.Entries.Single());
        }

        [Fact]
        public void GivenQuotedValue_WhenParsing_ThenPreservesInnerWhitespace()
        {
            // Arrange & Act
            var fm = Parse(
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.Identifier, "name", "name"), Tok(LexerTokenKind.Colon, ":", ":"), Tok(LexerTokenKind.Whitespace, " ", " "),
                Tok(LexerTokenKind.QuotedString, "  Hello World  ", "\"  Hello World  \""), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n")
            );
            // Assert
            var entry = Assert.IsType<FrontMatterEntry>(fm.Entries.Single());
            Assert.Equal("  Hello World  ", entry.Value);
        }

        [Fact]
        public void GivenMissingColon_WhenParsing_ThenThrows()
        {
            // Arrange
            var reader = new TokenReader(new []{
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.Identifier, "name", "name"), Tok(LexerTokenKind.Identifier, "oops", "oops"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n")
            });
            var parser = new FrontMatterParser();
            
            // Act & Assert
            Assert.Throws<ParsingException>(() => parser.Parse(reader));
        }

        [Fact]
        public void GivenUnterminatedBlock_WhenParsing_ThenThrows()
        {
            // Arrange
            var reader = new TokenReader(new []{
                Tok(LexerTokenKind.FrontMatterDelimiter, "---", "---"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.Identifier, "name", "name"), Tok(LexerTokenKind.Colon, ":", ":"), Tok(LexerTokenKind.Newline, "\n", "\n"),
                Tok(LexerTokenKind.EndOfInput, string.Empty, string.Empty)
            });
            var parser = new FrontMatterParser();
            
            // Act & Assert
            Assert.Throws<ParsingException>(() => parser.Parse(reader));
        }
    }
}


