using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Base type for all syntax nodes produced by the minimal AST parser.
/// </summary>
/// <remarks>
/// <para>
/// Nodes capture a snapshot of their source <see cref="FileLocation"/> along with absolute
/// character offsets (<see cref="Start"/> and <see cref="Length"/>). The location is cloned
/// to ensure immutability and avoid accidental mutation by downstream consumers.
/// </para>
/// <para>
/// This class is intentionally minimal for Phase 1 (front matter). Future phases may add
/// additional metadata or node kinds as the grammar coverage expands.
/// </para>
/// </remarks>
public abstract class SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="location"/> is null.</exception>
    protected SyntaxNode(FileLocation location, int start, int length)
    {
        if (location == null) throw new ArgumentNullException(nameof(location));
        Location = location.Clone();
        Start = start;
        Length = length;
    }

    /// <summary>
    /// Gets the source <see cref="FileLocation"/> captured at node start.
    /// </summary>
    public FileLocation Location { get; }

    /// <summary>
    /// Gets the absolute character offset where this node starts.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the number of characters spanned by this node.
    /// </summary>
    public int Length { get; }
}


