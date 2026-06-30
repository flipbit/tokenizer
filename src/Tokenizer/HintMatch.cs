using Tokens.Enumerators;

namespace Tokens
{
    public class HintMatch
    {
        public HintMatch(string text, bool optional, FileLocation location)
        {
            Text = text;
            Optional = optional;
            Location = location;
        }

        public string Text { get; set; }

        public bool Optional { get; set; }

        public FileLocation Location { get; set; }
    }
}
