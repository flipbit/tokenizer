namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for ValueMismatch issues, suggesting the user add an
/// end delimiter to prevent the token from greedily capturing too much input.
/// When the missed token name is known, includes it in the hint for clarity.
/// </summary>
internal sealed class ValueMismatchHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   DiagnosticEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.ValueMismatch)
            return null;

        var missedToken = sourceEvent.Detail;
        if (string.IsNullOrEmpty(missedToken))
            return "Consider adding an end delimiter (e.g. newline-terminated with '$') to prevent greedy capture.";

        return $"Matched value may have captured the preamble of token '{missedToken}'. " +
               "Consider adding an end delimiter (e.g. newline-terminated with '$') to prevent greedy capture.";
    }
}
