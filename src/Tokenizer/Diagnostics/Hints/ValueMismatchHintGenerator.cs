namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for ValueMismatch issues, suggesting the user add an
/// end delimiter to prevent the token from greedily capturing too much input.
/// </summary>
internal sealed class ValueMismatchHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                   DiagnosticResult trace)
    {
        if (issue.Type != DiagnosticIssueType.ValueMismatch)
            return null;

        return "Consider adding an end delimiter (e.g. newline-terminated with '$') to prevent greedy capture.";
    }
}
