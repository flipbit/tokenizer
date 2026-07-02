using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents the front matter block delimited by lines containing '---'.
/// </summary>
public sealed class FrontMatterBlock : SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="FrontMatterBlock"/> with the given entries.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    /// <param name="entries">The ordered entries (options, comments, directives) within the block.</param>
    public FrontMatterBlock(FileLocation location, int start, int length, IReadOnlyList<SyntaxNode> entries)
        : base(location, start, length)
    {
        Entries = entries ?? System.Array.Empty<SyntaxNode>();
    }

    /// <summary>
    /// Gets the ordered list of entries (options, comments, directives) within the block.
    /// </summary>
    public IReadOnlyList<SyntaxNode> Entries { get; }
}


