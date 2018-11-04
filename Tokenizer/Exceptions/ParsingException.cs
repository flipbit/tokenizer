using System.Text;
using Tokens.Enumerators;

namespace Tokens.Exceptions
{
    public class ParsingException : TokenizerException
    {
        internal ParsingException(string message, RawTokenEnumerator enumerator) : this(message, enumerator.Character, enumerator.Line)
        {
        }

        public ParsingException(string message, int character, int line) : base(message)
        {
            Character = character;
            Line = line;
        }

        public int Line { get; set; }

        public int Character { get; set; }

        public override string Message
        {
            get
            {
                var sb = new StringBuilder();

                sb.AppendLine(base.Message);
                sb.AppendLine();
                sb.AppendLine($"Character: {Character}");
                sb.AppendLine($"Line: {Line}");

                return sb.ToString();
            }
        }
    }
}
