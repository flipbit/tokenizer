using System;

namespace Tokens.Compilation.Lexer
{
    /// <summary>
    /// Defines the types of tokens that can be recognized by the <see cref="TemplateLexer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each token kind represents a distinct lexical element in the template definition grammar.
    /// The lexer operates in a context-free manner — it identifies token types based purely on
    /// character patterns, not on semantic context. This design decision keeps the lexer
    /// simple and stateless; context interpretation is the parser's responsibility.
    /// </para>
    /// <para>
    /// Categories:
    /// - Structural: Delimiters and brackets that define template structure
    /// - Modifiers: Special characters that modify token behavior (?, *, !, $, #)
    /// - Literals: Text content and quoted strings
    /// - Whitespace: Spaces, tabs, and normalized line endings
    /// - Escape sequences: Escaped brace characters
    /// - Control: End-of-input marker
    /// </para>
    /// <para>
    /// Notes:
    /// - Quoted strings are represented as a single <see cref="QuotedString"/> token whose Value excludes
    ///   the quote delimiters; the RawText preserves the original quoted form (Decision #3).
    /// - Both "\n" and "\r\n" line endings are normalized to a single <see cref="Newline"/> token (Decision #5).
    /// </para>
    /// </remarks>
    /// <example>
    /// Given the input <c>{name:ToUpper}</c>, the lexer produces tokens:
    /// <code>
    /// OpenBrace, Identifier("name"), Colon, Identifier("ToUpper"), CloseBrace, EndOfInput
    /// </code>
    /// </example>
    public enum LexerTokenKind
    {
        // Structural
        /// <summary>Front matter delimiter (---)</summary>
        FrontMatterDelimiter,

        /// <summary>Opening brace '{' marking the start of a token</summary>
        OpenBrace,

        /// <summary>Closing brace '}' marking the end of a token</summary>
        CloseBrace,

        /// <summary>Colon ':' used to separate names from decorators/options</summary>
        Colon,

        /// <summary>Equals '=' used for value assignment</summary>
        Equals,

        /// <summary>Comma ',' used to separate decorator arguments</summary>
        Comma,

        /// <summary>Opening parenthesis '(' used to start decorator arguments</summary>
        OpenParen,

        /// <summary>Closing parenthesis ')' used to end decorator arguments</summary>
        CloseParen,

        // Modifiers
        /// <summary>Question mark '?' optional modifier</summary>
        Question,

        /// <summary>Asterisk '*' repeating modifier</summary>
        Asterisk,

        /// <summary>Exclamation '!' required (or not-decorator) modifier</summary>
        Exclamation,

        /// <summary>Dollar '$' terminate-on-newline modifier</summary>
        Dollar,

        /// <summary>Hash '#' comment marker (front matter)</summary>
        Hash,

        // Literals
        /// <summary>Quoted string token (single or double quotes); value excludes quotes</summary>
        QuotedString,

        /// <summary>Identifier token (e.g., token names, decorator names)</summary>
        Identifier,

        /// <summary>Generic text not matching other categories</summary>
        Text,

        // Whitespace
        /// <summary>Whitespace (spaces and tabs)</summary>
        Whitespace,

        /// <summary>Newline ("\n" or normalized "\r\n")</summary>
        Newline,

        // Escape sequences
        /// <summary>Escaped open brace sequence '{{'</summary>
        EscapedOpenBrace,

        /// <summary>Escaped close brace sequence '}}'</summary>
        EscapedCloseBrace,

        // Control
        /// <summary>End of input marker</summary>
        EndOfInput
    }
}


