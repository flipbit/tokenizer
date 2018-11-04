using System.Collections.Generic;

namespace Tokens
{
    internal class RawToken
    {
        public RawToken()
        {
            Decorators = new List<RawTokenDecorator>();
        }

        public string Preamble { get; set; }

        public string Name { get; set; }

        public bool Optional { get; set; }

        public bool TerminateOnNewline { get; set; }

        public bool Repeating { get; set; }

        public IList<RawTokenDecorator> Decorators { get; private set; }
    }
}
