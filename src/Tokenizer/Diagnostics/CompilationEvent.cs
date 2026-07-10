using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// A single event recorded during template compilation.
/// </summary>
public sealed class CompilationEvent
{
    /// <summary>
    /// The type of compilation event.
    /// </summary>
    public CompilationEventType Type { get; init; }

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
    /// The position in the source text where this event occurred.
    /// </summary>
    public FileLocation? Location { get; init; }

    /// <summary>
    /// The value associated with this event.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Human-readable explanation providing additional context.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// The name of the decorator involved, or null for non-decorator events.
    /// </summary>
    public string? DecoratorName { get; init; }

    /// <summary>
    /// The parameters passed to the decorator, or null.
    /// </summary>
    public string[]? DecoratorArgs { get; init; }
}
