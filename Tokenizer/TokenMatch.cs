using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tokens
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TokenMatch<T> where T : class, new()
    {
        public TokenMatch()
        {
            Results = new List<TokenizeResult<T>>();
        }

        public IList<TokenizeResult<T>> Results { get; }

        /// <summary>
        /// Returns the best matching result
        /// </summary>
        public TokenizeResult<T> BestMatch => Results.OrderByDescending(r => r.Matches.Count).FirstOrDefault();
    }
}