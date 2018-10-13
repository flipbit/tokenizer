using System.Collections;
using System.Collections.Generic;

namespace Tokens
{
    public class FlatToken
    {
        public FlatToken()
        {
            Decorators = new List<FlatTokenDecorator>();
        }

        public string Preamble { get; set; }

        public string Name { get; set; }

        public bool Optional { get; set; }

        public bool TerminateOnNewline { get; set; }

        public bool Repeating { get; set; }

        public IList<FlatTokenDecorator> Decorators { get; private set; }
    }

    public class FlatTokenDecorator
    {
        public FlatTokenDecorator()
        {
            Args = new List<string>();
        }

        public string Name { get; set; }

        public IList<string> Args { get; private set; } 
    }
}
