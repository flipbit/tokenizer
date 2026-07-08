namespace Tokens.Diagnostics;

/// <summary>
/// A high-level summary of a tokenization run, computed from the collected events.
/// </summary>
public sealed class DiagnosticSummary
{
    /// <summary>
    /// A human-readable verdict describing the overall outcome of the tokenization run.
    /// </summary>
    public string Verdict { get; init; } = string.Empty;

    /// <summary>
    /// The list of issues identified during the tokenization run.
    /// An empty list indicates a clean run with no warnings or errors.
    /// </summary>
    public IReadOnlyList<DiagnosticIssue> Issues { get; init; } = new List<DiagnosticIssue>();
}
