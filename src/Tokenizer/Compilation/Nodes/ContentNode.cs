using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Base content node for non-front-matter regions.
/// Phase 1 used as a stub; Phase 2 includes <see cref="TextNode"/> and <see cref="TokenNode"/>.
/// </summary>
public class ContentNode : SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="ContentNode"/> at the given source position.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    public ContentNode(FileLocation location, int start, int length)
        : base(location, start, length)
    {
    }
}


