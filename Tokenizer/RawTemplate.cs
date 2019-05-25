using System.Collections.Generic;

namespace Tokens
{
    internal class RawTemplate
    {
        public RawTemplate()
        {
            Tokens = new List<RawToken>();
            Hints = new List<Hint>();
        }

        public TokenizerOptions Options { get; set; }

        public IList<RawToken> Tokens { get; }

        public IList<Hint> Hints { get; }

        public string Name { get; set; }
    }
}
