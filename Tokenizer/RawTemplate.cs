using System.Collections.Generic;

namespace Tokens
{
    public class RawTemplate
    {
        public RawTemplate()
        {
            Tokens = new List<RawToken>();
        }

        public TokenizerOptions Options { get; set; }

        public IList<RawToken> Tokens { get; }
    }
}
