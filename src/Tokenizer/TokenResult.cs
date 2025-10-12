using System;
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
            if (TryConcatMatch(token, value, location)) return;

            Matches.Add(new Match
            {
                Token = token,
                Value = value,
                Location = location.Clone()
            });
        }

        private bool TryConcatMatch(Token token, object value, FileLocation location)
        {
            if (token.Concatenate == false) return false;

            if (Matches.Any(m => m.Token.Name == token.Name) == false) return false;

            var match = Matches.First(m => m.Token.Name == token.Name);

            if (token.CanConcatenate(match.Value, value) == false) return false;

            match.Value = token.ConcatenateValues(match.Value, value, token.ConcatenationString);

            return true;
        }

        internal void AddMiss(Token token)
        {
            Misses.Add(token);
        }

        public bool HasMissingRequiredTokens => Misses.Any(m => m.Required);

        public bool HasMatches => Matches.Any();
    }
}
