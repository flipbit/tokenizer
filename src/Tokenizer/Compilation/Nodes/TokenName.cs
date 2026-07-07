namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents the name identifier of a token or decorator.
/// </summary>
public sealed record TokenName
{
    /// <summary>
    /// Initializes a new <see cref="TokenName"/> with the given text.
    /// </summary>
    /// <param name="text">The raw name text.</param>
    public TokenName(string text)
    {
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// Gets the raw name text.
    /// </summary>
    public string Text { get; }
}
