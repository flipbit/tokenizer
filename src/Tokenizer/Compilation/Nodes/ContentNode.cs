using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes
{
    /// <summary>
    /// Base content node for non-front-matter regions.
    /// Phase 1 used as a stub; Phase 2 includes <see cref="TextNode"/> and <see cref="TokenNode"/>.
    /// </summary>
    public class ContentNode : SyntaxNode
    {
        public ContentNode(FileLocation location, int start, int length)
            : base(location, start, length)
        {
        }
    }
}


