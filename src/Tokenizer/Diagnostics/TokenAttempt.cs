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
    public FileLocation? Location { get; init; }

    /// <summary>
    /// The value that was considered.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// What happened with this attempt.
    /// </summary>
    public AttemptOutcome Outcome { get; init; }

    /// <summary>
    /// The decorator that rejected/failed, if applicable.
    /// </summary>
    public string? DecoratorName { get; init; }

    /// <summary>
    /// Human-readable explanation of why this attempt failed.
    /// </summary>
    public string? Reason { get; init; }
}
