using System.Collections.Generic;
using System.Text;

namespace Tokens
{
    public class RawTokenDecorator
    {
        private readonly StringBuilder name;

        public RawTokenDecorator()
        {
            name = new StringBuilder();
            Args = new List<string>();
        }

        public string Name => name.ToString();

        public IList<string> Args { get; }

        public void AppendName(string value)
        {
            name.Append(value);
        }
    }
}