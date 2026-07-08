using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a single front matter key/value option line (e.g., key: value).
/// </summary>
public sealed class FrontMatterEntry : SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="FrontMatterEntry"/> where the raw and normalized values equal <paramref name="value"/>.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    /// <param name="key">The option key.</param>
    /// <param name="value">The option value.</param>
    public FrontMatterEntry(FileLocation location, int start, int length, string key, string value)
        : base(location, start, length)
    {
        Key = key ?? string.Empty;
        Value = value ?? string.Empty;
        RawValue = value ?? string.Empty;
        NormalizedValue = value ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new <see cref="FrontMatterEntry"/> with explicit raw and normalized values.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    /// <param name="key">The option key.</param>
    /// <param name="value">The parsed option value.</param>
    /// <param name="rawValue">The raw source text of the value, including any surrounding quotes.</param>
    /// <param name="normalizedValue">The value after outside-quote whitespace trimming.</param>
    public FrontMatterEntry(FileLocation location, int start, int length, string key, string value, string rawValue, string normalizedValue)
        : base(location, start, length)
    {
        Key = key ?? string.Empty;
        Value = value ?? string.Empty;
        RawValue = rawValue ?? string.Empty;
        NormalizedValue = normalizedValue ?? (value ?? string.Empty);
    }

    /// <summary>
    /// Gets the option key identifying which template setting this entry configures.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Value as interpreted by the parser (may preserve intra-quote whitespace).
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Raw value text as captured from the source line (including quotes if present).
    /// </summary>
    public string RawValue { get; }

    /// <summary>
    /// Parser-provided normalized value (e.g., outside-quote trimming). Binder may use this.
    /// </summary>
    public string NormalizedValue { get; }
}


