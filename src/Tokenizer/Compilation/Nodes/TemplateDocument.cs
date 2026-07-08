using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Root node representing an entire template document.
/// </summary>
public sealed class TemplateDocument : SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="TemplateDocument"/> with the given front matter and content.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    /// <param name="frontMatter">The optional front matter block, or <see langword="null"/> if absent.</param>
    /// <param name="content">The sequence of content nodes following the front matter.</param>
    public TemplateDocument(FileLocation location, int start, int length, FrontMatterBlock? frontMatter, IReadOnlyList<ContentNode> content)
        : base(location, start, length)
    {
        FrontMatter = frontMatter;
        Content = content ?? System.Array.Empty<ContentNode>();
    }

    /// <summary>
    /// Gets the optional front matter block if present.
    /// </summary>
    public FrontMatterBlock? FrontMatter { get; }

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
        return new TemplateDocument(location, start, length, frontMatter: null, content);
    }
}


