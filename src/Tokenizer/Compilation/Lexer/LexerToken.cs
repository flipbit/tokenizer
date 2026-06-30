using System;
using Tokens.Enumerators;

namespace Tokens.Compilation.Lexer
{
    /// <summary>
    /// Represents a single lexical token produced by the <see cref="TemplateLexer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="LexerToken"/> encapsulates all information about a recognized token including its
    /// <see cref="Kind"/>, textual <see cref="Value"/>, and the <see cref="Location"/> in the source text.
    /// Tokens are immutable after construction to keep behavior predictable and thread-safe.
    /// </para>
    /// <para>
    /// Location tracking uses the existing <see cref="FileLocation"/> class for consistency across the
    /// Tokenizer library. The <see cref="Location"/> represents where the token started; it is cloned on
    /// construction to avoid external mutations affecting the token.
    /// This aligns with PRD FR-4 (location tracking) and SM-5 (documentation quality).
    /// </para>
    /// <para>
    /// The <see cref="RawText"/> property preserves the original text representation of the token (including
    /// delimiters where applicable). For quoted strings, the <see cref="Value"/> excludes the quote characters
    /// (per design decision: quoted strings are a single token), while <see cref="RawText"/> keeps the quotes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var token = new LexerToken(
    ///     LexerTokenKind.QuotedString,
    ///     value: "hello world",
    ///     rawText: "'hello world'",
    ///     location: currentLocation,
    ///     start: 10,
    ///     length: 13);
    /// 
    /// Console.WriteLine(token.Value);     // hello world
    /// Console.WriteLine(token.RawText);   // 'hello world'
    /// Console.WriteLine(token.Location);  // Ln: 1 Col: 11 Para: 1
    /// Console.WriteLine(token.End);       // 23
    /// </code>
    /// </example>
    public record LexerToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LexerToken"/> class.
        /// </summary>
        /// <param name="kind">The token kind.</param>
        /// <param name="value">The token value. For quoted strings this excludes the quote characters.</param>
        /// <param name="rawText">The raw token text including any delimiters (e.g., quotes).</param>
        /// <param name="location">The file location where the token starts.</param>
        /// <param name="start">The absolute start position (0-based) in the input.</param>
        /// <param name="length">The token length in characters.</param>
        public LexerToken(
            LexerTokenKind kind,
            string value,
            string rawText,
            FileLocation location,
            int start,
            int length)
        {
            Kind = kind;
            Value = value ?? string.Empty;
            RawText = rawText ?? string.Empty;
            Location = (location == null ? new FileLocation() : location.Clone());
            Start = start;
            Length = length;
        }

        /// <summary>
        /// Gets the token kind.
        /// </summary>
        public LexerTokenKind Kind { get; }

        /// <summary>
        /// Gets the string value of the token.
        /// </summary>
        /// <remarks>
        /// For quoted strings, the value excludes the quote delimiters per design decision.
        /// For all other token kinds, this is the literal character sequence associated with the token.
        /// </remarks>
        public string Value { get; }

        /// <summary>
        /// Gets the original text for the token including any delimiters (e.g., quotes for quoted strings).
        /// </summary>
        public string RawText { get; }

        /// <summary>
        /// Gets the file location where the token starts. This instance is a clone of the source location.
        /// </summary>
        public FileLocation Location { get; }

        /// <summary>
        /// Gets the absolute start position (0-based) of the token in the input.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the length of the token in characters.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the absolute end position (0-based, exclusive) of the token in the input.
        /// </summary>
        public int End
        {
            get { return Start + Length; }
        }
    }
}


