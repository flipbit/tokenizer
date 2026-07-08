namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a literal value assigned to a token within the template source.
/// </summary>
public sealed record ValueNode
{
    /// <summary>
    /// Initializes a new <see cref="ValueNode"/> with the given text and quoting flag.
    /// </summary>
    /// <param name="text">The literal value text.</param>
    /// <param name="isQuoted">Whether the value was enclosed in quotes in the source.</param>
    public ValueNode(string text, bool isQuoted)
    {
        Text = text ?? string.Empty;
        IsQuoted = isQuoted;
    }

    /// <summary>
    /// Gets the literal value text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets a value indicating whether the value was enclosed in quotes in the source.
    /// </summary>
    public bool IsQuoted { get; }
}
