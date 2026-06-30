using Tokens.Enumerators;

namespace Tokens.Diagnostics
{
    /// <summary>
    /// A single issue identified during tokenization, with an optional
    /// adaptive hint suggesting how to fix it.
    /// </summary>
    public class DiagnosticIssue
    {
        /// <summary>
        /// Category of the issue for programmatic filtering.
        /// </summary>
        public DiagnosticIssueType Type { get; init; }

        /// <summary>
        /// The token that failed, if applicable.
        /// </summary>
        public string? TokenName { get; init; }

        /// <summary>
        /// Human-readable explanation of what went wrong.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Location in the input where the issue occurred.
        /// </summary>
        public FileLocation? Location { get; init; }

        /// <summary>
        /// Adaptive hint suggesting how to fix the issue, if available.
        /// Null when no hint can be generated.
        /// </summary>
        public string? Hint { get; init; }
    }
}
