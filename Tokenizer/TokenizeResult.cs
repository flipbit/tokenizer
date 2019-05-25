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

            Hints = new HintResult();
            Tokens = new TokenResult();

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
        public TokenResult Tokens { get; set; } 

        /// <summary>
        /// Gets the hints found in the input
        /// </summary>
        public HintResult Hints { get; set; }

        /// <summary>
        /// Determines whether the matching process was successful
        /// </summary>
        public bool Success => Tokens.HasMatches && 
                               Tokens.HasMissingRequiredTokens == false &&
                               Hints.HasMissingRequiredHints == false;
    }
}
