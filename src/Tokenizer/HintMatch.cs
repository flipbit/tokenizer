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

        public string Text { get; init; }

        public bool Optional { get; init; }

        public FileLocation Location { get; init; }
    }
}
