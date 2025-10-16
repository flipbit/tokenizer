using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes
{
    /// <summary>
    /// Root node representing an entire template document.
    /// </summary>
    public sealed class TemplateDocument : SyntaxNode
    {
        public TemplateDocument(FileLocation location, int start, int length, FrontMatterBlock frontMatter, IReadOnlyList<ContentNode> content)
            : base(location, start, length)
        {
            FrontMatter = frontMatter;
            Content = content ?? System.Array.Empty<ContentNode>();
        }

        /// <summary>
        /// Gets the optional front matter block if present.
        /// </summary>
        public FrontMatterBlock FrontMatter { get; }

        /// <summary>
        /// Gets the sequence of content nodes (Phase 1: stub only).
        /// </summary>
        public IReadOnlyList<ContentNode> Content { get; }

        /// <summary>
        /// Creates a document with front matter only.
        /// </summary>
        public static TemplateDocument FromFrontMatter(FileLocation location, int start, int length, FrontMatterBlock frontMatter)
        {
            return new TemplateDocument(location, start, length, frontMatter, System.Array.Empty<ContentNode>());
        }

        /// <summary>
        /// Creates a document without front matter.
        /// </summary>
        public static TemplateDocument WithoutFrontMatter(FileLocation location, int start, int length, IReadOnlyList<ContentNode> content)
        {
            return new TemplateDocument(location, start, length, null, content);
        }
    }
}


