using System.Collections.Generic;
using System.Text;

namespace Tokens
{
    internal class RawToken
    {
        private readonly StringBuilder preamble;
        private readonly StringBuilder name;

        public RawToken()
        {
            Decorators = new List<RawTokenDecorator>();
            preamble = new StringBuilder();
            name = new StringBuilder();
        }

        public string Preamble => preamble.ToString();

        public string Name => name.ToString();

        public bool Optional { get; set; }

        public bool TerminateOnNewline { get; set; }

        public bool Repeating { get; set; }

        public bool Required { get; set; }

        public IList<RawTokenDecorator> Decorators { get; private set; }

        public void AppendPreamble(string value)
        {
            if (value == "\r") return;

            preamble.Append(value);
        }

        public void AppendName(string value)
        {
            name.Append(value);
        }
    }
}
