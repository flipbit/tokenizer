namespace Tokens
{
    /// <summary>
    /// Options for the <see cref="Tokenizer"/>.
    /// </summary>
    public class TokenizerOptions
    {
        public static TokenizerOptions Defaults
        {
            get
            {
                return new TokenizerOptions
                {
                    // Don't throw exceptions by default
                    ThrowExceptionOnMissingProperty = false
                };
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception when a token can't
        /// be mapped to a property.
        /// </summary>
        /// <value>
        /// <c>true</c> if to throw an exception when a property is missing; otherwise, <c>false</c>.
        /// </value>
        public bool ThrowExceptionOnMissingProperty { get; set; }
    }
}
