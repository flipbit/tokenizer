using System.Collections.Generic;
using System.Text;

namespace Tokens
{
    internal class PreTokenDecorator
    {
        private readonly StringBuilder name;

        public PreTokenDecorator()
        {
            name = new StringBuilder();
            Args = new List<string>();
        }

        public string Name => name.ToString();

        public IList<string> Args { get; }

        public bool IsNotDecorator { get; set; }

        public void AppendName(string value)
        {
            name.Append(value);
        }
    }
}