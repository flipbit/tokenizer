using System;
using System.Collections.Generic;
using System.Linq;

namespace Tokens
{
    /// <summary>
    /// Holds the result of attempting to parse an input string against a
    /// <see cref="Template"/> to generate an object of type <see cref="T"/>.
    /// </summary>
    public class TokenizeResult<T> where T : class, new()
    {
        /// <summary>
        ///  Creates a new instance of the <see cref="TokenizeResult{T}"/> class.
        /// </summary>
        public TokenizeResult(Template template)
        {
            Exceptions = new List<Exception>();
            Matches = new List<Match>();
            NotMatched = new List<Token>();

            Template = template;
        }

        /// <summary>
        /// An instance of <see cref="T"/> populated with data from the input string. 
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// The <see cref="Template"/> containing the mapping between tokens in the
        /// <see cref="Template"/> and properties on the object <see cref="T"/>.
        /// </summary>
        public Template Template { get; set; }

        /// <summary>
        /// A list of any exceptions that occurred during the matching process
        /// </summary>
        public IList<Exception> Exceptions { get; }

        /// <summary>
        /// The matches that where made during the tokenization process
        /// </summary>
        public IList<Match> Matches { get; }

        /// <summary>
        /// Contains a list of <see cref="Token"/> objects that were not
        /// matched.
        /// </summary>
        public IList<Token> NotMatched { get; }

        /// <summary>
        /// Determines whether the matching process was successful
        /// </summary>
        public bool Success
        {
            get
            {
                return Matches.Count > 0 && !NotMatched.Any(nm => nm.Required);
            }
        }

        internal void AddMatch(Token token, string value)
        {
            Matches.Add(new Match
            {
                Token = token,
                Value = value
            });
        }
    }
}
