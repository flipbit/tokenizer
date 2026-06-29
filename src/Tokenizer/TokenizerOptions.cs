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
            // Set defaults
            TrimLeadingWhitespaceInTokenPreamble = true;
            TrimPreambleBeforeNewLine = false;
            TrimTrailingWhiteSpace = true;
            TokenStringComparison = StringComparison.InvariantCulture;
            OutOfOrderTokens = false;
            EnableLogging = false;
            EnableLineByLineLogging = true;
            TerminateOnNewline = false;
            IgnoreMissingProperties = false;
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
        /// If set, enables line-by-line summary logging showing matched and remaining tokens per line.
        /// </summary>
        public bool EnableLineByLineLogging { get; set; }

        /// <summary>
        /// If set, token values will be extracted up till the first new line character.
        /// </summary>
        public bool TerminateOnNewline { get; set; }

        /// <summary>
        /// Maximum allowed length for input text. Default: 1,048,576 (1MB).
        /// Set to 0 to disable.
        /// </summary>
        public int MaxInputLength { get; set; } = 1_048_576;

        /// <summary>
        /// Maximum allowed length for template pattern text. Default: 65,536 (64KB).
        /// Set to 0 to disable.
        /// </summary>
        public int MaxTemplateLength { get; set; } = 65_536;

        /// <summary>
        /// Maximum number of tokens allowed in a template. Default: 500.
        /// Set to 0 to disable.
        /// </summary>
        public int MaxTokenCount { get; set; } = 500;

        public TokenizerOptions Clone()
        {
            return new TokenizerOptions
            {
                TrimTrailingWhiteSpace = TrimTrailingWhiteSpace,
                TrimLeadingWhitespaceInTokenPreamble = TrimLeadingWhitespaceInTokenPreamble,
                TokenStringComparison = TokenStringComparison,
                OutOfOrderTokens = OutOfOrderTokens,
                EnableLogging = EnableLogging,
                EnableLineByLineLogging = EnableLineByLineLogging,
                TrimPreambleBeforeNewLine = TrimPreambleBeforeNewLine,
                TerminateOnNewline = TerminateOnNewline,
                IgnoreMissingProperties = IgnoreMissingProperties,
                MaxInputLength = MaxInputLength,
                MaxTemplateLength = MaxTemplateLength,
                MaxTokenCount = MaxTokenCount
            };
        }
    }
}
