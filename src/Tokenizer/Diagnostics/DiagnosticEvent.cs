using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// A single diagnostic event recorded during tokenization, representing
/// one decision point in the matching process.
/// </summary>
public sealed class DiagnosticEvent
{
    /// <summary>
    /// The type of decision or event. See <see cref="DiagnosticEventType"/>
    /// for detailed documentation of each type's semantics.
    /// </summary>
    public DiagnosticEventType Type { get; init; }

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
    /// The position in the input text where this event occurred.
    /// </summary>
    public FileLocation? Location { get; init; }

    /// <summary>
    /// The value being tested, assigned, or accumulated.
    /// Meaning varies by event type — see <see cref="DiagnosticEventType"/> docs.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Human-readable explanation providing additional context.
    /// For TransformerSucceeded, contains the transformed output value.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// The name of the decorator (validator or transformer) involved,
    /// or null for non-decorator events. E.g. "ToDateTimeUtc", "IsEmail".
    /// </summary>
    public string? DecoratorName { get; init; }

    /// <summary>
    /// The parameters passed to the decorator, or null.
    /// E.g. ["yyyy-MM-dd HH:mm:ss"] for ToDateTimeUtc.
    /// </summary>
    public string[]? DecoratorArgs { get; init; }
}
