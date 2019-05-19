namespace Tokens
{
    /// <summary>
    /// Represent a <see cref="Token"/> match in a <see cref="Template"/>
    /// </summary>
    public class Match
    {
        /// <summary>
        /// Gets or sets the <see cref="Token"/> matched
        /// </summary>
        public Token Token { get; set; }

        /// <summary>
        /// Gets or sets the value of the match
        /// </summary>
        public object Value { get; set; }
    }
}
