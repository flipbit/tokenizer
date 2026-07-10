namespace Tokens.Diagnostics;

/// <summary>
/// The outcome of a single attempt to match a token.
/// </summary>
public enum AttemptOutcome
{
    /// <summary>
    /// Value was accepted and assigned to the token.
    /// </summary>
    Assigned,

    /// <summary>
    /// A validator rejected the value.
    /// </summary>
    ValidatorRejected,

    /// <summary>
    /// A transformer failed to convert the value.
    /// </summary>
    TransformerFailed,

    /// <summary>
    /// The engine backtracked past this match.
    /// </summary>
    Backtracked,
}
