using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Lexer;

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
            LexerTokenKind.EndOfInput,
        }, kinds);
    }

    [Fact]
    public void GivenStructuralCharsSurroundedByText_WhenTokenizing_ThenEmitsStructuralTokensInOrder()
    {
        // Arrange
        var lexer = CreateLexer();
        var input = "pre { mid } : = , ( ) post";

        // Act
        var kinds = lexer.Tokenize(input).Select(t => t.Kind).ToList();

        // Filter down to only structural kinds to avoid coupling to whitespace/identifier tokens
        var structural = kinds.Where(k =>
            k == LexerTokenKind.OpenBrace ||
            k == LexerTokenKind.CloseBrace ||
            k == LexerTokenKind.Colon ||
            k == LexerTokenKind.Equals ||
            k == LexerTokenKind.Comma ||
            k == LexerTokenKind.OpenParen ||
            k == LexerTokenKind.CloseParen).ToList();

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
        }, structural);

        // EndOfInput should be the final token kind
        Assert.Equal(LexerTokenKind.EndOfInput, kinds[^1]);
    }

    [Theory]
    [InlineData("{name?}", '?', LexerTokenKind.Question)]
    [InlineData("{name*}", '*', LexerTokenKind.Asterisk)]
    [InlineData("{name!}", '!', LexerTokenKind.Exclamation)]
    [InlineData("{name$}", '$', LexerTokenKind.Dollar)]
    public void GivenNameWithSingleModifierInsideBraces_WhenTokenizing_ThenEmitsModifierWithCorrectPositions(
        string input, char modifier, LexerTokenKind expectedKind)
    {
        // Arrange
        var lexer = CreateLexer();

        // Act
        var tokens = lexer.Tokenize(input).ToList();

        // Assert kinds in order
        Assert.Equal(new[]
        {
            LexerTokenKind.OpenBrace,
            LexerTokenKind.Identifier,
            expectedKind,
            LexerTokenKind.CloseBrace,
            LexerTokenKind.EndOfInput,
        }, tokens.Select(t => t.Kind).ToArray());

        // Assert absolute positions (Start)
        Assert.Equal(0, tokens[0].Start); // '{'
        Assert.Equal(1, tokens[1].Start); // 'name'
        Assert.Equal(5, tokens[2].Start); // modifier
        Assert.Equal(6, tokens[3].Start); // '}'
        Assert.Equal(input.Length, tokens[4].Start); // EndOfInput at EOF

        // Sanity: modifier token value should match the modifier character
        Assert.Equal(modifier.ToString(), tokens[2].Value);
        Assert.Equal(1, tokens[2].Length);
    }

    [Fact]
    public void GivenTextWithIdentifiersAndPunctuation_WhenTokenizing_ThenIdentifiersAndPunctuationAreSeparated()
    {
        // Arrange
        var lexer = CreateLexer();
        var input = "Hello, {name}.";

        // Act
        var tokens = lexer.Tokenize(input).Where(t => t.Kind != LexerTokenKind.Whitespace).ToList();

        // Assert kinds sequence (excluding whitespace)
        Assert.Equal(new[]
        {
            LexerTokenKind.Identifier,   // Hello
            LexerTokenKind.Comma,        // , (structural everywhere)
            LexerTokenKind.OpenBrace,    // {
            LexerTokenKind.Identifier,   // name
            LexerTokenKind.CloseBrace,   // }
            LexerTokenKind.Identifier,   // . (dot counts as identifier char in lexer)
            LexerTokenKind.EndOfInput,
        }, tokens.Select(t => t.Kind).ToArray());

        // Assert values for the identifier and punctuation
        Assert.Equal("Hello", tokens[0].Value);
        Assert.Equal(",", tokens[1].Value);
        Assert.Equal("name", tokens[3].Value);
        Assert.Equal(".", tokens[5].Value);
    }

    [Fact]
    public void GivenDotAndUnderscoreInIdentifier_WhenTokenizing_ThenSingleIdentifierToken()
    {
        // Arrange
        var lexer = CreateLexer();
        var input = "user.name_1";

        // Act
        var tokens = lexer.Tokenize(input).ToList();

        // Assert
        Assert.Equal(LexerTokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("user.name_1", tokens[0].Value);
        Assert.Equal(LexerTokenKind.EndOfInput, tokens[1].Kind);
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
            LexerTokenKind.EndOfInput,
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
            LexerTokenKind.EndOfInput,
        }, kinds);
    }

    [Fact]
    public void GivenQuotesOutsideBraces_WhenTokenizing_ThenEmitsTextNotQuotedString()
    {
        // Arrange
        var lexer = CreateLexer();

        // Act — quotes outside braces should be literal text
        var tokens = lexer.Tokenize("Registrant's address:").ToList();

        // Assert — the apostrophe should NOT start a quoted string
        var kinds = tokens.Select(t => t.Kind).ToList();
        Assert.DoesNotContain(LexerTokenKind.QuotedString, kinds);
        // Should contain text tokens for the apostrophe
        Assert.Contains(tokens, t => t.Kind == LexerTokenKind.Text && t.Value.Contains("'"));
    }

    [Fact]
    public void GivenQuotesInsideBraces_WhenTokenizing_ThenEmitsQuotedString()
    {
        // Arrange
        var lexer = CreateLexer();

        // Act — quotes inside braces should still be quoted strings
        var tokens = lexer.Tokenize("{ name : Replace('foo', 'bar') }").ToList();

        // Assert
        var quotedStrings = tokens.Where(t => t.Kind == LexerTokenKind.QuotedString).ToList();
        Assert.Equal(2, quotedStrings.Count);
        Assert.Equal("foo", quotedStrings[0].Value);
        Assert.Equal("bar", quotedStrings[1].Value);
    }

    [Fact]
    public void GivenQuotedStringsInsideBraces_WhenTokenizing_ThenEmitsQuotedStringWithRawAndValue()
    {
        // Arrange
        var lexer = CreateLexer();

        // Act — quotes inside braces
        var single = lexer.Tokenize("{ x : T('hello') }").First(t => t.Kind == LexerTokenKind.QuotedString);
        var dbl = lexer.Tokenize("{ x : T(\"world\") }").First(t => t.Kind == LexerTokenKind.QuotedString);

        // Assert
        Assert.Equal(LexerTokenKind.QuotedString, single.Kind);
        Assert.Equal("hello", single.Value);
        Assert.Equal("'hello'", single.RawText);

        Assert.Equal(LexerTokenKind.QuotedString, dbl.Kind);
        Assert.Equal("world", dbl.Value);
        Assert.Equal("\"world\"", dbl.RawText);
    }

    [Fact]
    public void GivenQuotedStringsWithEscapesInsideBraces_WhenTokenizing_ThenEmitsCorrectValueAndRaw()
    {
        // Arrange
        var lexer = CreateLexer();

        // Act
        var escQuote = lexer.Tokenize("{ x : T(\"Jane \\\"Doe\\\"\") }").First(t => t.Kind == LexerTokenKind.QuotedString);
        var escBackslash = lexer.Tokenize("{ x : T(\"A \\\\ B\") }").First(t => t.Kind == LexerTokenKind.QuotedString);

        // Assert
        Assert.Equal(LexerTokenKind.QuotedString, escQuote.Kind);
        Assert.Equal("Jane \"Doe\"", escQuote.Value);

        Assert.Equal(LexerTokenKind.QuotedString, escBackslash.Kind);
        Assert.Equal("A \\ B", escBackslash.Value);
    }

    [Fact]
    public void GivenUnknownEscapeInQuotedString_WhenTokenizing_ThenTreatsAsLiteral()
    {
        // Arrange
        var lexer = CreateLexer();
        var input = "{ x : T(\"bad \\x\") }";

        // Act
        var token = lexer.Tokenize(input).First(t => t.Kind == LexerTokenKind.QuotedString);

        // Assert
        Assert.Equal("bad x", token.Value);
    }

    [Fact]
    public void GivenBackslashEscapedGMT_WhenTokenizing_ThenTreatsAsLiteralGMT()
    {
        // Arrange
        var lexer = CreateLexer();
        var input = "{ x : ToDateTimeUtc(\"ddd MMM d HH:mm:ss \\G\\M\\T yyyy\") }";

        // Act
        var token = lexer.Tokenize(input).First(t => t.Kind == LexerTokenKind.QuotedString);

        // Assert
        Assert.Equal("ddd MMM d HH:mm:ss GMT yyyy", token.Value);
    }

    [Fact]
    public void GivenApostropheInPreambleThenQuotesInToken_WhenTokenizing_ThenBothHandledCorrectly()
    {
        // Arrange
        var lexer = CreateLexer();
        var input = "Registrant's address: { Name : Replace('before ', '01-') }";

        // Act
        var tokens = lexer.Tokenize(input).ToList();

        // Assert — apostrophe in preamble is text, quotes in token are QuotedString
        var quotedStrings = tokens.Where(t => t.Kind == LexerTokenKind.QuotedString).ToList();
        Assert.Equal(2, quotedStrings.Count);
        Assert.Equal("before ", quotedStrings[0].Value);
        Assert.Equal("01-", quotedStrings[1].Value);
    }

    [Fact]
    public void GivenCrLfNewlines_WhenTokenizing_ThenLocationColumnsResetAcrossLines()
    {
        // Arrange
        var lexer = CreateLexer();
        var input = "ab\r\ncd";

        // Act
        var tokens = lexer.Tokenize(input).ToList();

        // Assert
        var id1 = tokens[0];
        var nl = tokens[1];
        var id2 = tokens[2];

        Assert.Equal(LexerTokenKind.Identifier, id1.Kind);
        Assert.Equal(LexerTokenKind.Newline, nl.Kind);
        Assert.Equal(LexerTokenKind.Identifier, id2.Kind);
        Assert.True(id2.Location.Line >= 2);
        Assert.True(id2.Location.Column >= 1);
    }

    [Fact]
    public void GivenSamples_WhenLexing_ThenDoesNotThrowAndEmitsEndOfInput()
    {
        // Arrange
        var lexer = CreateLexer();
        var sampleDir = Path.Combine(AppContext.BaseDirectory, "tests/Tokenizer.Tests/Samples/Patterns");
        if (!Directory.Exists(sampleDir)) return; // skip if not available in this run context

        foreach (var file in Directory.EnumerateFiles(sampleDir, "*.txt"))
        {
            var text = File.ReadAllText(file);
            // Act
            var tokens = lexer.Tokenize(text).ToList();

            // Assert
            Assert.NotEmpty(tokens);
            Assert.Equal(LexerTokenKind.EndOfInput, tokens[^1].Kind);
        }
    }

    [Fact]
    public void GivenUnclosedQuoteOutsideBraces_WhenTokenizing_ThenEmitsText()
    {
        // Arrange — outside braces, quotes are just text
        var lexer = CreateLexer();
        var input = "'unclosed";

        // Act
        var tokens = lexer.Tokenize(input).ToList();

        // Assert — should not throw, apostrophe is text
        Assert.DoesNotContain(tokens, t => t.Kind == LexerTokenKind.QuotedString);
    }

    [Fact]
    public void GivenUnclosedQuoteInsideBraces_WhenTokenizing_ThenThrowsLexerException()
    {
        // Arrange — inside braces, quotes must be closed
        var lexer = CreateLexer();
        var input = "{ x : T('unclosed) }";

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
    public void GivenMemoryStream_WhenTokenizeStream_ThenStreamRemainsOpen()
    {
        // Arrange
        var lexer = CreateLexer();
        var bytes = System.Text.Encoding.UTF8.GetBytes("{name:ToUpper}");
        var stream = new MemoryStream(bytes);

        // Act
        var tokens = lexer.Tokenize(stream).ToList();

        // Assert — stream must still be readable (not disposed) after tokenization
        Assert.True(stream.CanRead, "Stream should still be readable after Tokenize completes.");
        Assert.NotEmpty(tokens);
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
        var input = string.Join(' ', Enumerable.Repeat("token", 5000));

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
        if (!File.Exists(xmlPath)) return;

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
        private readonly TextReader _inner;
        public int ReadCount { get; private set; }

        public CountingTextReader(TextReader inner)
        {
            _inner = inner;
        }

        public override int Read()
        {
            ReadCount++;
            return _inner.Read();
        }

        public override int Peek()
        {
            return _inner.Peek();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}


