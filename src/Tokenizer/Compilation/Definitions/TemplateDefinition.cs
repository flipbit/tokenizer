using System.Collections.Generic;

namespace Tokens.Compilation.Definitions
{
    /// <summary>
    /// Holds parsed template input before being transformed into a
    /// <see cref="Template"/>.
    /// </summary>
    public class TemplateDefinition
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TemplateDefinition"/> class.
        /// </summary>
        public TemplateDefinition()
        {
            Tokens = new List<TokenDefinition>();
            Hints = new List<Hint>();
            Tags = new List<string>();
        }

        /// <summary>
        /// Holds the <see cref="TokenizerOptions"/> that this instance was created with.
        /// </summary>
        public TokenizerOptions Options { get; set; }

        /// <summary>
        /// Contains a list of <see cref="TokenDefinition"/> objects that were found in the input string
        /// </summary>
        public IList<TokenDefinition> Tokens { get; }

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
