using System.Text;
using Tokens.Compilation.Parsing;
using Tokens.Enumerators;

namespace Tokens.Exceptions
{
    /// <summary>
    /// Thrown when a template pattern fails to parse due to a syntax error.
    /// </summary>
    public class ParsingException : TokenizerException
    {
        internal ParsingException(string message, TemplateDefinitionEnumerator enumerator) : this(message, enumerator.Location)
        {
        }

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
}
