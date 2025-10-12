using System.Collections.Generic;

namespace Tokens
{
    /// <summary>
    /// Holds parsed template input before being transformed into a
    /// <see cref="Template"/>.
    /// </summary>
    internal class PreTemplate
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PreTemplate"/> class.
        /// </summary>
        public PreTemplate()
        {
            Tokens = new List<PreToken>();
            Hints = new List<Hint>();
            Tags = new List<string>();
        }

        /// <summary>
        /// Holds the <see cref="TokenizerOptions"/> that this instance was created with.
        /// </summary>
        public TokenizerOptions Options { get; set; }

        /// <summary>
        /// Contains a list of <see cref="PreToken"/> objects that were found in the input string
        /// </summary>
        public IList<PreToken> Tokens { get; }

        /// <summary>
        /// Contains a list of <see cref="Hint"/> objects that were found in the input string
        /// </summary>
        public IList<Hint> Hints { get; }

        /// <summary>
        /// Contains a list of tags that were found in the input string
        /// </summary>
        public IList<string> Tags { get; }

        /// <summary>
        /// Specifies the name of the template.
        /// </summary>
        public string Name { get; set; }
    }
}
