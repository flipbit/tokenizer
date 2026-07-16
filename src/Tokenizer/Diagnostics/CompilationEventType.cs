namespace Tokens.Diagnostics;

/// <summary>
/// Identifies the type of event recorded during template compilation.
/// </summary>
public enum CompilationEventType
{
    /// <summary>
    /// A hint was added to the template during compilation.
    /// </summary>
    HintAdded,

    /// <summary>
    /// A tag was added to the template during compilation.
    /// </summary>
    TagAdded,

    /// <summary>
    /// A token was created from a token definition during compilation.
    /// </summary>
    TokenCreated,

    /// <summary>
    /// A template-level option was applied to a token during compilation.
    /// </summary>
    OptionApplied,

    /// <summary>
    /// A decorator (transformer or validator) was applied to a token during compilation.
    /// </summary>
    DecoratorApplied,

    /// <summary>
    /// A concatenation decorator was applied to a token during compilation.
    /// </summary>
    ConcatenationApplied,

    /// <summary>
    /// A repeating token was linked to its non-repeating counterpart during compilation.
    /// </summary>
    RepeatingTokenLinked,

    /// <summary>
    /// Template compilation has completed.
    /// </summary>
    CompilationCompleted,
}
