namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a decorator (validator or transformer) applied to a token.
/// </summary>
public sealed record DecoratorNode
{
    /// <summary>
    /// Initializes a new <see cref="DecoratorNode"/> with the given name, arguments, and negation flag.
    /// </summary>
    /// <param name="name">The decorator name.</param>
    /// <param name="args">The arguments passed to the decorator.</param>
    /// <param name="isNot">Whether the decorator is negated (prefixed with <c>!</c>).</param>
    public DecoratorNode(TokenName name, IReadOnlyList<ArgumentNode> args, bool isNot = false)
    {
        Name = name;
        Args = args ?? System.Array.Empty<ArgumentNode>();
        IsNot = isNot;
    }

    /// <summary>
    /// Gets the decorator name.
    /// </summary>
    public TokenName Name { get; }

    /// <summary>
    /// Gets the arguments passed to the decorator.
    /// </summary>
    public IReadOnlyList<ArgumentNode> Args { get; }

    /// <summary>
    /// Gets a value indicating whether the decorator is negated (prefixed with <c>!</c>).
    /// </summary>
    public bool IsNot { get; }
}
