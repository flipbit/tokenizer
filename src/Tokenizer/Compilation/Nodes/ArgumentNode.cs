namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a single argument passed to a decorator.
/// </summary>
public sealed record ArgumentNode
{
    /// <summary>
    /// Initializes a new <see cref="ArgumentNode"/> with the given text and quoting flag.
    /// </summary>
    /// <param name="text">The argument text.</param>
    /// <param name="isQuoted">Whether the argument was enclosed in quotes in the source.</param>
    public ArgumentNode(string text, bool isQuoted)
    {
        Text = text ?? string.Empty;
        IsQuoted = isQuoted;
    }

    /// <summary>
    /// Gets the argument text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets a value indicating whether the argument was enclosed in quotes in the source.
    /// </summary>
    public bool IsQuoted { get; }
}
