namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates an adaptive hint for a diagnostic issue by analyzing the
/// event context. Returns null if no actionable hint can be produced.
/// Implementations should prefer returning null over returning a
/// low-confidence or misleading hint.
/// </summary>
internal interface IHintGenerator
{
    /// <summary>
    /// Attempts to generate a hint for the given issue.
    /// </summary>
    /// <param name="type">The diagnostic issue type</param>
    /// <param name="tokenName">The name of the token associated with the issue</param>
    /// <param name="sourceEvent">The diagnostic event that caused the issue</param>
    /// <param name="trace">The full diagnostic trace for cross-referencing</param>
    /// <returns>A human-readable hint string, or null if no hint applies</returns>
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                            TokenizationEvent sourceEvent, DiagnosticResult trace);
}
