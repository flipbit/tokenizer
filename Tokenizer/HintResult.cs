using System.Collections.Generic;
using System.Linq;
using Tokens.Enumerators;

namespace Tokens
{
    /// <summary>
    /// Contains the results of processing a <see cref="Template"/> for
    /// <see cref="Hint"/> strings.
    /// </summary>
    public class HintResult
    {
        public HintResult()
        {
            Matches = new List<HintMatch>();
            Misses = new List<Hint>();
        }

        /// <summary>
        /// Gets the hint matches
        /// </summary>
        public IList<HintMatch> Matches { get; }

        /// <summary>
        /// Gets the hint misses
        /// </summary>
        public IList<Hint> Misses { get; }

        internal bool AddMatch(Hint hint, TokenEnumerator enumerator)
        {
            if (Matches.Any(m => m.Text == hint.Text)) return false;

            Matches.Add(new HintMatch
            {
                Text = hint.Text,
                Optional = hint.Optional,
                Location = enumerator.Location.Clone()
            });

            return true;
        }

        internal bool AddMiss(Hint hint)
        {
            if (Misses.Any(m => m.Text == hint.Text) ||
                Matches.Any(m => m.Text == hint.Text)) return false;

            Misses.Add(hint.Clone());

            return true;
        }

        public bool HasMissingRequiredHints => Misses.Any(m => m.Optional == false);
    }
}
