using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a front matter comment line (starting with '#').
/// </summary>
public sealed class FrontMatterComment : SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="FrontMatterComment"/> with the given comment text.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    /// <param name="text">The comment text, excluding the leading <c>#</c> character.</param>
    public FrontMatterComment(FileLocation location, int start, int length, string text)
        : base(location, start, length)
    {
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// Gets the comment text, excluding the leading <c>#</c> character.
    /// </summary>
    public string Text { get; }
}


