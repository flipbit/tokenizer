using System.Text;
using Tokens.Compilation.Parsing;
using Tokens.Enumerators;

namespace Tokens.Exceptions;

/// <summary>
/// Thrown when a template pattern fails to parse due to a syntax error.
/// </summary>
public class ParsingException : TokenizerException
{
    internal ParsingException(string message, TemplateDefinitionEnumerator enumerator) : this(message, enumerator.Location)
    {
    }

    /// <summary>
    /// Initializes a new instance with a message and the source location where the error occurred.
    /// </summary>
    /// <param name="message">The error message describing the syntax problem.</param>
    /// <param name="location">The line and column in the template pattern where the error was detected.</param>
    public ParsingException(string message, FileLocation location) : base(message)
    {
        Column = location.Column;
        Line = location.Line;
    }

    /// <summary>
    /// The one-based line number in the template pattern where the error occurred.
    /// </summary>
    public int Line { get; internal set; }

    /// <summary>
    /// The one-based column number in the template pattern where the error occurred.
    /// </summary>
    public int Column { get; internal set; }

    /// <summary>
    /// The full error message including the line and column number where the parse error occurred.
    /// </summary>
    public override string Message
    {
        get
        {
            var sb = new StringBuilder();

            sb.AppendLine(base.Message);
            sb.AppendLine();
            sb.AppendLine($"Column: {Column}");
            sb.AppendLine($"Line: {Line}");

            return sb.ToString();
        }
    }
}
