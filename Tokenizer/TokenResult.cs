using System.Collections.Generic;
using System.Linq;
using Tokens.Enumerators;

namespace Tokens
{
    public class TokenResult
    {
        public TokenResult()
        {
            Matches = new List<Match>();
            Misses = new List<Token>();
        }

        public IList<Match> Matches { get; }

        public IList<Token> Misses { get; }

        internal void AddMatch(Token token, object value, FileLocation location)
        {
            Matches.Add(new Match
            {
                Token = token,
                Value = value,
                Location = location.Clone()
            });
        }

        internal void AddMiss(Token token)
        {
            Misses.Add(token);
        }

        public bool HasMissingRequiredTokens => Misses.Any(m => m.Required);

        public bool HasMatches => Matches.Any();
    }
}
