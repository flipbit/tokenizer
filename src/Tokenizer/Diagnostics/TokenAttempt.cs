using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// A single attempt to match a token at a specific location in the input.
/// </summary>
public sealed class TokenAttempt
{
    /// <summary>
    /// Position in the input where this attempt occurred.
    /// </summary>
    public FileLocation? Location { get; internal init; }

    /// <summary>
    /// The value that was considered.
    /// </summary>
    public string? Value { get; internal init; }

    /// <summary>
    /// What happened with this attempt.
    /// </summary>
    public AttemptOutcome Outcome { get; internal init; }

    /// <summary>
    /// The decorator that rejected/failed, if applicable.
    /// </summary>
    public string? DecoratorName { get; internal init; }

    /// <summary>
    /// Human-readable explanation of why this attempt failed.
    /// </summary>
    public string? Reason { get; internal init; }
}
