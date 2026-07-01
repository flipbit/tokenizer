using System.Collections.Generic;
using System.Linq;
using Tokens.Enumerators;

namespace Tokens
{
    /// <summary>
    /// Contains the results of processing a <see cref="Template"/> for
    /// <see cref="Hint"/> strings.
    /// </summary>
    public sealed class HintResult
    {
        private readonly List<HintMatch> _matches;
        private readonly List<Hint> _misses;

        public HintResult()
        {
            _matches = new List<HintMatch>();
            _misses = new List<Hint>();
        }

        /// <summary>
        /// Gets the hint matches
        /// </summary>
        public IReadOnlyList<HintMatch> Matches => _matches;

        /// <summary>
        /// Gets the hint misses
        /// </summary>
        public IReadOnlyList<Hint> Misses => _misses;

        internal bool AddMatch(Hint hint, TokenEnumerator enumerator)
        {
            if (_matches.Any(m => m.Text == hint.Text)) return false;

            _matches.Add(new HintMatch(hint.Text, hint.Optional, enumerator.Location.Clone()));

            return true;
        }

        internal bool AddMiss(Hint hint)
        {
            if (_misses.Any(m => m.Text == hint.Text) ||
                _matches.Any(m => m.Text == hint.Text)) return false;

            _misses.Add(hint.Clone());

            return true;
        }

        public bool HasMissingRequiredHints => Misses.Any(m => m.Optional == false);
    }
}
