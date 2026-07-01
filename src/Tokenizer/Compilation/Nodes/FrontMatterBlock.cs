using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents the front matter block delimited by lines containing '---'.
/// </summary>
public sealed class FrontMatterBlock : SyntaxNode
{
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


