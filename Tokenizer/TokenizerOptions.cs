using System;

namespace Tokens
{
    /// <summary>
    /// Options for the <see cref="Tokenizer"/>.
    /// </summary>
    public class TokenizerOptions
    {
        public static TokenizerOptions Defaults => new TokenizerOptions
        {
            // Don't throw exceptions by default
            ThrowExceptionOnMissingProperty = false,
            TrimLeadingWhitespaceInTokenPreamble = true,
            TrimTrailingWhiteSpace = true,
            TokenStringComparison = StringComparison.InvariantCulture,
            OutOfOrderTokens = false
        };

        /// <summary>
        /// Gets or sets a value indicating whether to throw an exception when a token can't
        /// be mapped to a property.
        /// </summary>
        /// <value>
        /// <c>true</c> if to throw an exception when a property is missing; otherwise, <c>false</c>.
        /// </value>
        public bool ThrowExceptionOnMissingProperty { get; set; }

        public bool TrimLeadingWhitespaceInTokenPreamble { get; set; }

        public bool TrimTrailingWhiteSpace { get; set; }

        public bool OutOfOrderTokens { get; set; }

        /// <summary>
        /// Determines the <see cref="StringComparison"/> type to use when matching Token names to object properties
        /// </summary>
        public StringComparison TokenStringComparison { get; set; }

        public TokenizerOptions Clone()
        {
            return new TokenizerOptions
            {
                ThrowExceptionOnMissingProperty = ThrowExceptionOnMissingProperty,
                TrimTrailingWhiteSpace = TrimTrailingWhiteSpace,
                TrimLeadingWhitespaceInTokenPreamble = TrimLeadingWhitespaceInTokenPreamble,
                TokenStringComparison = TokenStringComparison,
                OutOfOrderTokens = OutOfOrderTokens
            };
        }
    }
}
