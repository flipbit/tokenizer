using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// The complete diagnostic story for a single token during tokenization.
/// </summary>
public sealed record TokenDiagnostic
{
    /// <summary>
    /// Token name from the template.
    /// </summary>
    public string TokenName { get; internal init; } = string.Empty;

    /// <summary>
    /// Unique token ID within the template.
    /// </summary>
    public int TokenId { get; internal init; }

    /// <summary>
    /// Final outcome of this token.
    /// </summary>
    public TokenOutcome Outcome { get; internal init; }

    /// <summary>
    /// Every time this token was considered during tokenization.
    /// </summary>
    public IReadOnlyList<TokenAttempt> Attempts { get; internal init; } = [];

    /// <summary>
    /// All assigned values, in input order. Single-element for non-repeating tokens,
    /// multiple elements for repeating tokens. Empty if Outcome is not Matched.
    /// </summary>
    public IReadOnlyList<string> AssignedValues { get; internal init; } = [];

    /// <summary>
    /// Locations where each value was matched, parallel to <see cref="AssignedValues"/>.
    /// </summary>
    public IReadOnlyList<FileLocation> AssignedLocations { get; internal init; } = [];

    /// <summary>
    /// The name of the token that blocked this one from being searched,
    /// or null if this token was not blocked. Only populated when
    /// <see cref="Outcome"/> is <see cref="TokenOutcome.Blocked"/>.
    /// </summary>
    public string? BlockedBy { get; internal init; }

    /// <summary>
    /// Issues identified for this token (with adaptive hints).
    /// </summary>
    public IReadOnlyList<DiagnosticIssue> Issues { get; internal init; } = [];
}
