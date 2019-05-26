using System;
using System.Collections.Generic;

namespace Tokens
{
    /// <summary>
    /// Base class that holds the result of attempting to parse an input string against a
    /// <see cref="Template"/>.
    /// </summary>
    public class TokenizeResultBase
    {
        /// <summary>
        ///  Creates a new instance of the <see cref="TokenizeResultBase"/> class.
        /// </summary>
        public TokenizeResultBase(Template template)
        {
            Exceptions = new List<Exception>();

            Hints = new HintResult();
            Tokens = new TokenResult();

            Template = template;
        }

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
