using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a chunk of preamble or free-form text outside of token blocks.
/// </summary>
public sealed class TextNode : ContentNode
{
    /// <summary>
    /// Initializes a new <see cref="TextNode"/> with the given text content.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    /// <param name="text">The text content with escape sequences already resolved.</param>
    public TextNode(FileLocation location, int start, int length, string text)
        : base(location, start, length)
    {
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// Gets the text content for this node with escapes (e.g., "{{" → "{").
    /// </summary>
    public string Text { get; }
}



