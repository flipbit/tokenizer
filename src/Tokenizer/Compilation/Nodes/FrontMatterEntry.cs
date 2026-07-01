using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a single front matter key/value option line (e.g., key: value).
/// </summary>
public sealed class FrontMatterEntry : SyntaxNode
{
    public FrontMatterEntry(FileLocation location, int start, int length, string key, string value)
        : base(location, start, length)
    {
        Key = key ?? string.Empty;
        Value = value ?? string.Empty;
        RawValue = value ?? string.Empty;
        NormalizedValue = value ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new entry with explicit raw and normalized values.
    /// </summary>
    public FrontMatterEntry(FileLocation location, int start, int length, string key, string value, string rawValue, string normalizedValue)
        : base(location, start, length)
    {
        Key = key ?? string.Empty;
        Value = value ?? string.Empty;
        RawValue = rawValue ?? string.Empty;
        NormalizedValue = normalizedValue ?? (value ?? string.Empty);
    }

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


