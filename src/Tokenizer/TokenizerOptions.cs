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
            TerminateOnNewline = false;
            IgnoreMissingProperties = false;
            EnableDiagnostics = false;
        }

        public bool IgnoreMissingProperties { get; set; }

        /// <summary>
        /// When true, tokenization results include a <see cref="Diagnostics.TokenizationDiagnostics"/>
        /// property with a structured trace of every matching decision, a mismatch summary
        /// with adaptive hints, and a visual alignment diff.
        /// Default: false. Has no performance impact when disabled.
        /// </summary>
        public bool EnableDiagnostics { get; set; }

        public bool TrimLeadingWhitespaceInTokenPreamble { get; set; }

        public bool TrimPreambleBeforeNewLine { get; set; }

        public bool TrimTrailingWhiteSpace { get; set; }

        public bool OutOfOrderTokens { get; set; }

        /// <summary>
        /// Determines the <see cref="StringComparison"/> type to use when matching Token names to object properties
        /// </summary>
        public StringComparison TokenStringComparison { get; set; }

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

        /// <summary>
        /// Maximum number of iterations in the tokenization loop.
        /// Default: 0 (auto-calculated as input.Length * 2).
        /// Set to a positive value to override.
        /// </summary>
        public int MaxIterations { get; set; } = 0;

        public TokenizerOptions Clone()
        {
            return new TokenizerOptions
            {
                TrimTrailingWhiteSpace = TrimTrailingWhiteSpace,
                TrimLeadingWhitespaceInTokenPreamble = TrimLeadingWhitespaceInTokenPreamble,
                TokenStringComparison = TokenStringComparison,
                OutOfOrderTokens = OutOfOrderTokens,
                TrimPreambleBeforeNewLine = TrimPreambleBeforeNewLine,
                TerminateOnNewline = TerminateOnNewline,
                IgnoreMissingProperties = IgnoreMissingProperties,
                MaxInputLength = MaxInputLength,
                MaxTemplateLength = MaxTemplateLength,
                MaxTokenCount = MaxTokenCount,
                MaxIterations = MaxIterations,
                EnableDiagnostics = EnableDiagnostics
            };
        }
    }
}
