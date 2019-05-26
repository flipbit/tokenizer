using System.Collections.Generic;

namespace Tokens
{
    internal class PreTemplate
    {
        public PreTemplate()
        {
            Tokens = new List<PreToken>();
            Hints = new List<Hint>();
        }

        public TokenizerOptions Options { get; set; }

        public IList<PreToken> Tokens { get; }

        public IList<Hint> Hints { get; }

        public string Name { get; set; }
    }
}
