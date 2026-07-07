using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a set directive within front matter (e.g., set: Name = Value : Decorator(args...)).
/// </summary>
public sealed class SetTokenDirective : SyntaxNode
{
    /// <summary>
    /// Initializes a new <see cref="SetTokenDirective"/> with the given token constituents.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    /// <param name="tokenName">The name of the token being set.</param>
    /// <param name="value">The optional literal value to assign to the token.</param>
    /// <param name="decorators">The optional decorators to apply to the token.</param>
    public SetTokenDirective(FileLocation location, int start, int length, string tokenName, string? value = null, IReadOnlyList<SetDecorator>? decorators = null)
        : base(location, start, length)
    {
        TokenName = tokenName ?? string.Empty;
        Value = value;
        Decorators = decorators ?? System.Array.Empty<SetDecorator>();
    }

    /// <summary>
    /// Gets the name of the token being set.
    /// </summary>
    public string TokenName { get; }

    /// <summary>
    /// Gets the literal value assigned to the token, or <see langword="null"/> if none.
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// Gets the decorators (validators and transformers) applied to this directive.
    /// </summary>
    public IReadOnlyList<SetDecorator> Decorators { get; }
}

