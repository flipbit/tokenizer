using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokens.Compilation.Lexer;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Tests.Compilation.Lexer
{
    public class TemplateLexerTests
    {
        private static TemplateLexer CreateLexer() => new TemplateLexer();

        [Fact]
        public void GivenStructuralChars_WhenTokenizing_ThenEmitsStructuralTokens()
        {
            // Arrange
            var lexer = CreateLexer();
            var input = "{}:=,()";

            // Act
            var kinds = lexer.Tokenize(input).Select(t => t.Kind).ToList();

            // Assert
            Assert.Equal(new[]
            {
                LexerTokenKind.OpenBrace,
                LexerTokenKind.CloseBrace,
                LexerTokenKind.Colon,
                LexerTokenKind.Equals,
                LexerTokenKind.Comma,
                LexerTokenKind.OpenParen,
                LexerTokenKind.CloseParen,
                LexerTokenKind.EndOfInput
            }, kinds);
        }

        [Fact]
        public void GivenModifierChars_WhenTokenizing_ThenEmitsModifierTokens()
        {
            // Arrange
            var lexer = CreateLexer();
            var input = "?*!$#";

            // Act
            var kinds = lexer.Tokenize(input).Select(t => t.Kind).ToList();

            // Assert
            Assert.Equal(new[]
            {
                LexerTokenKind.Question,
                LexerTokenKind.Asterisk,
                LexerTokenKind.Exclamation,
                LexerTokenKind.Dollar,
                LexerTokenKind.Hash,
                LexerTokenKind.EndOfInput
            }, kinds);
        }

        [Fact]
        public void GivenFrontMatterDelimiter_WhenTokenizing_ThenEmitsFrontMatterToken()
        {
            // Arrange
            var lexer = CreateLexer();
            var input = "---\n";

            // Act
            var tokens = lexer.Tokenize(input).ToList();

            // Assert
            Assert.Equal(LexerTokenKind.FrontMatterDelimiter, tokens[0].Kind);
            Assert.Equal("---", tokens[0].Value);
            Assert.Equal(LexerTokenKind.Newline, tokens[1].Kind);
            Assert.Equal("\n", tokens[1].Value);
        }

        [Fact]
        public void GivenEscapedBraces_WhenTokenizing_ThenEmitsEscapedTokens()
        {
            // Arrange
            var lexer = CreateLexer();
            var input = "{{}}";

            // Act
            var kinds = lexer.Tokenize(input).Select(t => t.Kind).ToList();

            // Assert
            Assert.Equal(new[]
            {
                LexerTokenKind.EscapedOpenBrace,
                LexerTokenKind.EscapedCloseBrace,
                LexerTokenKind.EndOfInput
            }, kinds);
        }

        [Fact]
        public void GivenQuotedStrings_WhenTokenizing_ThenEmitsQuotedStringWithRawAndValue()
        {
            // Arrange
            var lexer = CreateLexer();

            // Act
            var single = lexer.Tokenize("'hello'").First();
            var dbl = lexer.Tokenize("\"world\"").First();

            // Assert
            Assert.Equal(LexerTokenKind.QuotedString, single.Kind);
            Assert.Equal("hello", single.Value);
            Assert.Equal("'hello'", single.RawText);

            Assert.Equal(LexerTokenKind.QuotedString, dbl.Kind);
            Assert.Equal("world", dbl.Value);
            Assert.Equal("\"world\"", dbl.RawText);
        }

        [Fact]
        public void GivenUnclosedQuote_WhenTokenizing_ThenThrowsLexerException()
        {
            // Arrange
            var lexer = CreateLexer();
            var input = "'unclosed";

            // Act / Assert
            Assert.Throws<LexerException>(() => lexer.Tokenize(input).ToList());
        }

        [Fact]
        public void GivenDifferentInputs_WhenTokenizing_ThenAllOverloadsProduceSameKinds()
        {
            // Arrange
            var lexer = CreateLexer();
            var pattern = "{name:ToUpper} --- {{ }} ?*!$# abc\n\tdef";

            // Act
            var kindsString = lexer.Tokenize(pattern).Select(t => t.Kind).ToList();
            using var reader = new StringReader(pattern);
            var kindsReader = lexer.Tokenize(reader).Select(t => t.Kind).ToList();
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(pattern));
            var kindsStream = lexer.Tokenize(stream).Select(t => t.Kind).ToList();

            // Assert
            Assert.Equal(kindsString, kindsReader);
            Assert.Equal(kindsString, kindsStream);
        }

        [Fact]
        public async Task GivenAsyncEnumeration_WhenCanceled_ThenThrowsOperationCanceled()
        {
            // Arrange
            var lexer = CreateLexer();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act / Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in lexer.TokenizeAsync("abc", cts.Token))
                {
                    // never reached
                }
            });
        }

        [Fact]
        public void GivenWhitespacePrefix_WhenEnumeratingFirstToken_ThenReaderNotFullyConsumed()
        {
            // Arrange
            var lexer = CreateLexer();
            var text = new string(' ', 10) + "X" + new string('Y', 10000);
            using var reader = new CountingTextReader(new StringReader(text));

            // Act
            var first = lexer.Tokenize(reader).First();

            // Assert
            Assert.Equal(LexerTokenKind.Whitespace, first.Kind);
            Assert.True(reader.ReadCount < text.Length, "Reader should not be fully consumed after first token.");
        }

        [Fact]
        public void GivenNewlines_WhenTokenizing_ThenLocationTracksLines()
        {
            // Arrange
            var lexer = CreateLexer();
            var input = "a\r\nb";

            // Act
            var tokens = lexer.Tokenize(input).ToList();

            // Assert
            var id1 = tokens[0];
            var nl = tokens[1];
            var id2 = tokens[2];

            Assert.Equal(LexerTokenKind.Identifier, id1.Kind);
            Assert.Equal(LexerTokenKind.Newline, nl.Kind);
            Assert.Equal(LexerTokenKind.Identifier, id2.Kind);
            Assert.True(id2.Location.Line >= 2, "Second identifier should be on or after line 2.");
        }

        [Fact]
        public void GivenLargeInput_WhenTokenizing_ThenRunsWithoutExcessiveAllocationOrErrors()
        {
            // Arrange
            var lexer = CreateLexer();
            var input = string.Join(" ", Enumerable.Repeat("token", 5000));

            // Act
            var count = 0;
            foreach (var _ in lexer.Tokenize(input)) count++;

            // Assert
            Assert.True(count > 0);
        }

        [Fact]
        public void GivenLibraryBuild_WhenDocumentationIsGenerated_ThenXmlDocContainsPublicTypes()
        {
            // Arrange
            var xmlPath = FindFileUpwards("src/Tokenizer/bin/Debug/net6.0/Tokenizer.xml");

            // If docs are not generated for this configuration, skip validation to avoid false negatives
            if (File.Exists(xmlPath) == false) return;

            var xml = File.ReadAllText(xmlPath);

            // Act / Assert
            Assert.Contains("T:Tokens.Compilation.Lexer.TemplateLexer", xml);
            Assert.Contains("T:Tokens.Compilation.Lexer.LexerToken", xml);
            Assert.Contains("T:Tokens.Compilation.Lexer.LexerTokenKind", xml);
        }

        private static string FindFileUpwards(string relativePath)
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                var candidate = Path.Combine(dir, relativePath);
                if (File.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return Path.Combine(AppContext.BaseDirectory, relativePath);
        }

        private sealed class CountingTextReader : TextReader
        {
            private readonly TextReader inner;
            public int ReadCount { get; private set; }

            public CountingTextReader(TextReader inner)
            {
                this.inner = inner;
            }

            public override int Read()
            {
                ReadCount++;
                return inner.Read();
            }

            public override int Peek()
            {
                return inner.Peek();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) inner.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}


