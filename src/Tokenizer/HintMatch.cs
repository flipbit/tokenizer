using Tokens.Enumerators;

namespace Tokens
{
    /// <summary>
    /// Represents a hint string that a template uses to pre-filter candidate inputs.
    /// </summary>
    public sealed class HintMatch
    {
        public HintMatch(string text, bool optional, FileLocation location)
        {
            Text = text;
            Optional = optional;
            Location = location;
        }

        /// <summary>
        /// The hint string to search for in the input.
        /// </summary>
        public string Text { get; init; }

        /// <summary>
        /// When true, the hint is optional and a missing match does not disqualify the template.
        /// </summary>
        public bool Optional { get; init; }

        /// <summary>
        /// The location in the template pattern where this hint was declared.
        /// </summary>
        public FileLocation Location { get; init; }
    }
}
