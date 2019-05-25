using System.Collections.Generic;
using System.Linq;

namespace Tokens
{
    /// <summary>
    /// Contains the result of running a match against multiple <see cref="Template"/>
    /// objects against an input string with the <see cref="TokenMatcher"/>. 
    /// </summary>
    public class TokenMatcherResult<T> where T : class, new()
    {
        public TokenMatcherResult()
        {
            Results = new List<TokenizeResult<T>>();
        }

        /// <summary>
        /// Contains the result of processing each <see cref="Template"/> against the input text.
        /// </summary>
        public IList<TokenizeResult<T>> Results { get; }

        /// <summary>
        /// Returns the best matching result
        /// </summary>
        public TokenizeResult<T> BestMatch { get; internal set; } 

        internal TokenizeResult<T> GetBestMatch() => Results
            .Where(r => r.Success)
            .OrderByDescending(r => r.Hints.Matches.Count)
            .ThenByDescending(r => r.Tokens.Matches.Count)
            .FirstOrDefault();


    }
}