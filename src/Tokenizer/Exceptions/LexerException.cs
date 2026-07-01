using System;
using System.Text;
using Tokens.Enumerators;

namespace Tokens.Exceptions
{
    /// <summary>
    /// Thrown by the lexer when invalid or unexpected input is encountered during lexical analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is specific to the lexing phase and is used to signal strict-mode failures such as
    /// unclosed quoted strings, malformed escape sequences, or otherwise unrecognized characters.
    /// </para>
    /// <para>
    /// When available, the exception includes line and column information derived from a <see cref="FileLocation"/>
    /// captured at the point of failure. This facilitates precise error reporting and diagnostics.
    /// </para>
    /// </remarks>
    public class LexerException : TokenizerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LexerException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public LexerException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexerException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public LexerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexerException"/> class with location information.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="location">The file location associated with this exception.</param>
        public LexerException(string message, FileLocation location) : base(message)
        {
            if (location != null)
            {
                Column = location.Column;
                Line = location.Line;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LexerException"/> class with location information.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="location">The file location associated with this exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public LexerException(string message, FileLocation location, Exception innerException) : base(message, innerException)
        {
            if (location != null)
            {
                Column = location.Column;
                Line = location.Line;
            }
        }

        /// <summary>
        /// Gets or sets the line where the error occurred.
        /// </summary>
        public int Line { get; internal set; }

        /// <summary>
        /// Gets or sets the column where the error occurred.
        /// </summary>
        public int Column { get; internal set; }

        /// <summary>
        /// Gets the full exception message including line and column information when available.
        /// </summary>
        public override string Message
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine(base.Message);
                if (Line > 0 || Column > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Column: {Column}");
                    sb.AppendLine($"Line: {Line}");
                }
                return sb.ToString();
            }
        }
    }
}


