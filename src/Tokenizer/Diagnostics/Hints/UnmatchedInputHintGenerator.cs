namespace Tokens.Diagnostics.Hints
{
    /// <summary>
    /// Placeholder hint generator for UnmatchedInputSection issues.
    /// Unmatched input gap analysis is not yet implemented, so this always returns null.
    /// </summary>
    internal sealed class UnmatchedInputHintGenerator : IHintGenerator
    {
        /// <inheritdoc />
        public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                       TokenizationDiagnostics trace)
        {
            return null;
        }
    }
}
