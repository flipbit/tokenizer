using System;

namespace Tokens
{
    /// <summary>
    /// Options for the <see cref="Tokenizer"/>.
    /// </summary>
    public class TokenizerOptions
    {
        public static TokenizerOptions Defaults => new TokenizerOptions();

        public TokenizerOptions()
        {
            ResetDefaults();
        }

        public bool IgnoreMissingProperties { get; set; }

        public bool TrimLeadingWhitespaceInTokenPreamble { get; set; }

        public bool TrimPreambleBeforeNewLine { get; set; }

        public bool TrimTrailingWhiteSpace { get; set; }

        public bool OutOfOrderTokens { get; set; }

        /// <summary>
        /// Determines the <see cref="StringComparison"/> type to use when matching Token names to object properties
        /// </summary>
        public StringComparison TokenStringComparison { get; set; }

        public bool EnableLogging { get; set; }

        /// <summary>
        /// If set, token values will be extracted up till the first new line character.
        /// </summary>
        public bool TerminateOnNewline { get; set; }

        /// <summary>
        /// Resets the options to their default values 
        /// </summary>
        public void ResetDefaults()
        {
            TrimLeadingWhitespaceInTokenPreamble = true;
            TrimPreambleBeforeNewLine = false;
            TrimTrailingWhiteSpace = true;
            TokenStringComparison = StringComparison.InvariantCulture;
            OutOfOrderTokens = false;
            EnableLogging = false;
            TerminateOnNewline = false;
            IgnoreMissingProperties = false;
        }

        public TokenizerOptions Clone()
        {
            return new TokenizerOptions
            {
                TrimTrailingWhiteSpace = TrimTrailingWhiteSpace,
                TrimLeadingWhitespaceInTokenPreamble = TrimLeadingWhitespaceInTokenPreamble,
                TokenStringComparison = TokenStringComparison,
                OutOfOrderTokens = OutOfOrderTokens,
                EnableLogging = EnableLogging,
                TrimPreambleBeforeNewLine = TrimPreambleBeforeNewLine,
                TerminateOnNewline = TerminateOnNewline,
                IgnoreMissingProperties = IgnoreMissingProperties
            };
        }
    }
}
