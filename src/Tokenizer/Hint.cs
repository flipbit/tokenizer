namespace Tokens
{
    /// <summary>
    /// Defines a string of text that can occur in a template's input.
    /// A hint can optionally be required to be present.
    /// Hints are used when determining whether the input is valid, and to determine
    /// tne best matched template for a given input.
    /// </summary>
    public class Hint
    {
        /// <summary>
        /// The text to appear in the input
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// If <c>true</c> then this hint must appear in the input in order for the
        /// <see cref="Template"/> to be considered successfully matched.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Clones this instance
        /// </summary>
        public Hint Clone()
        {
            return new Hint
            {
                Text = Text,
                Optional = Optional
            };
        }
    }
}
