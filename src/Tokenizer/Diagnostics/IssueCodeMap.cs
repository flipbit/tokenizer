namespace Tokens.Diagnostics;

/// <summary>
/// Maps <see cref="DiagnosticIssueType"/> values to stable error codes.
/// Codes are fixed across versions for documentation linking and programmatic filtering.
/// </summary>
internal static class IssueCodeMap
{
    public static string GetCode(DiagnosticIssueType type) => type switch
    {
        DiagnosticIssueType.PreambleNeverFound => "TK001",
        DiagnosticIssueType.ValidatorRejection => "TK002",
        DiagnosticIssueType.TransformerFailure => "TK003",
        DiagnosticIssueType.ValueMismatch => "TK004",
        DiagnosticIssueType.RepeatingTokenCutShort => "TK005",
        DiagnosticIssueType.UnmatchedInputSection => "TK006",
        DiagnosticIssueType.HintMissing => "TK007",
        _ => "TK000",
    };
}
