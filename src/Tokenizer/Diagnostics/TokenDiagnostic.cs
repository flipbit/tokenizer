using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// The complete diagnostic story for a single token during tokenization.
/// </summary>
public sealed class TokenDiagnostic
{
    /// <summary>
    /// Token name from the template.
    /// </summary>
    public string TokenName { get; init; } = string.Empty;

    /// <summary>
    /// Unique token ID within the template.
    /// </summary>
    public int TokenId { get; init; }

    /// <summary>
    /// Final outcome of this token.
    /// </summary>
    public TokenOutcome Outcome { get; init; }

    /// <summary>
    /// Every time this token was considered during tokenization.
    /// </summary>
    public IReadOnlyList<TokenAttempt> Attempts { get; init; } = [];

    /// <summary>
    /// The final assigned value, if Outcome is Matched.
    /// </summary>
    public string? AssignedValue { get; init; }

    /// <summary>
    /// Where in the input the token was matched, if Outcome is Matched.
    /// </summary>
    public FileLocation? AssignedLocation { get; init; }

    /// <summary>
    /// Issues identified for this token (with adaptive hints).
    /// </summary>
    public IReadOnlyList<DiagnosticIssue> Issues { get; init; } = [];
}
