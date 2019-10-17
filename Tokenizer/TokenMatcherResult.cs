using System.Collections.Generic;
using System.Linq;

namespace Tokens
{
    /// <summary>
    /// Contains the result of running a match against multiple <see cref="Template"/>
    /// objects against an input string with the <see cref="TokenMatcher"/>. 
    /// </summary>
    public class TokenMatcherResult
    {
        public TokenMatcherResult()
        {
            Results = new List<TokenizeResult>();
        }

        /// <summary>
        /// Contains the result of processing each <see cref="Template"/> against the input text.
        /// </summary>
        public IList<TokenizeResult> Results { get; private set; } 

        /// <summary>
        /// Returns the best matching result
        /// </summary>
        public TokenizeResult BestMatch { get; internal set; }

        public bool Success => BestMatch != null;

        internal TokenizeResult GetBestMatch() => Results
            .Where(r => r.Success)
            .OrderByDescending(r => r.Hints.Matches.Count)
            .ThenByDescending(r => r.Tokens.Matches.Count)
            .ThenBy(r => r.Template.Tokens.Count)
            .ThenBy(r => r.Template.Name)
            .FirstOrDefault();
    }

    public class TokenMatcherResult<T> where T : class, new()
    {
        public TokenMatcherResult()
        {
            Results = new List<TokenizeResult<T>>();
        }

        /// <summary>
        /// Contains the result of processing each <see cref="Template"/> against the input text.
        /// </summary>
        public IList<TokenizeResult<T>> Results { get; private set; }

        /// <summary>
        /// Returns the best matching result
        /// </summary>
        public TokenizeResult<T> BestMatch { get; internal set; }

        public bool Success => BestMatch != null;

        internal TokenizeResult<T> GetBestMatch() => Results
            .Where(r => r.Success)
            .OrderByDescending(r => r.Hints.Matches.Count)
            .ThenByDescending(r => r.Tokens.Matches.Count)
            .ThenBy(r => r.Template.Tokens.Count)
            .ThenBy(r => r.Template.Name)
            .FirstOrDefault();
    }
}