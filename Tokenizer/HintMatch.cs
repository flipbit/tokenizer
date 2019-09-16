using Tokens.Enumerators;

namespace Tokens
{
    public class HintMatch
    {
        public string Text { get; set; }

        public bool Optional { get; set; }

        public FileLocation Location { get; set; }
    }
}
