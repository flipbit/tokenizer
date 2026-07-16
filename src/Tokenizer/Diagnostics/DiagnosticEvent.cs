using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// A single diagnostic event recorded during compilation or tokenization,
/// representing one decision point in the process.
/// </summary>
/// <typeparam name="TType">The enum type identifying the event kind.</typeparam>
public sealed class DiagnosticEvent<TType> where TType : struct, Enum
{
    /// <summary>
    /// The type of decision or event.
    /// </summary>
    public TType Type { get; init; }

    /// <summary>
    /// The name of the token this event relates to, or null for
    /// events not specific to a single token.
    /// </summary>
    public string? TokenName { get; init; }

    /// <summary>
    /// The unique ID of the token within its template, or null
    /// for events not specific to a single token.
    /// </summary>
    public int? TokenId { get; init; }

    /// <summary>
    /// The position in the input/source text where this event occurred.
    /// </summary>
    public FileLocation? Location { get; init; }

    /// <summary>
    /// The value being tested, assigned, or accumulated.
    /// Meaning varies by event type.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Human-readable explanation providing additional context.
    /// For TransformerSucceeded, contains the transformed output value.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// The name of the decorator (validator or transformer) involved,
    /// or null for non-decorator events.
    /// </summary>
    public string? DecoratorName { get; init; }

    /// <summary>
    /// The parameters passed to the decorator, or null.
    /// </summary>
    public string[]? DecoratorArgs { get; init; }
}
