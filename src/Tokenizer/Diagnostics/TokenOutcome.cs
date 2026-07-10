namespace Tokens.Diagnostics;

/// <summary>
/// The final outcome of a token during tokenization.
/// </summary>
public enum TokenOutcome
{
    /// <summary>
    /// Token was successfully matched and assigned a value.
    /// </summary>
    Matched,

    /// <summary>
    /// Token's preamble was found but all values were rejected
    /// by validators or transformers.
    /// </summary>
    Rejected,

    /// <summary>
    /// Token's preamble was never found in the input.
    /// </summary>
    NeverFound,

    /// <summary>
    /// Token was not searched for because a prior required token
    /// failed to match.
    /// </summary>
    Blocked,
}
