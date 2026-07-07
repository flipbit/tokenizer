using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// AST node representing a token placeholder within a template (e.g., <c>{Name:Decorator}</c>).
/// </summary>
public sealed class TokenNode : ContentNode
{
    /// <summary>
    /// Initializes a new <see cref="TokenNode"/> with the given token constituents.
    /// </summary>
    /// <param name="location">The source location at the start of this node.</param>
    /// <param name="start">The absolute character offset where this node starts.</param>
    /// <param name="length">The number of characters spanned by this node.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="modifiers">The set of modifier flags applied to this token.</param>
    /// <param name="value">The optional hard-coded value node, or <see langword="null"/> if none.</param>
    /// <param name="decorators">The decorators (validators/transformers) applied to this token.</param>
    public TokenNode(FileLocation location, int start, int length, TokenName name, ModifierSet modifiers, ValueNode? value, IReadOnlyList<DecoratorNode> decorators)
        : base(location, start, length)
    {
        Name = name;
        Modifiers = modifiers ?? new ModifierSet(IsOptional: false, IsRepeating: false, IsRequired: false, IsTerminate: false);
        Value = value;
        Decorators = decorators ?? System.Array.Empty<DecoratorNode>();
    }

    /// <summary>
    /// Gets the name of the token.
    /// </summary>
    public TokenName Name { get; }

    /// <summary>
    /// Gets the modifier flags (optional, repeating, required, terminate) for this token.
    /// </summary>
    public ModifierSet Modifiers { get; }

    /// <summary>
    /// Gets the hard-coded value assigned to this token, or <see langword="null"/> if the token captures from input.
    /// </summary>
    public ValueNode? Value { get; }

    /// <summary>
    /// Gets the decorators (validators and transformers) applied to this token.
    /// </summary>
    public IReadOnlyList<DecoratorNode> Decorators { get; }
}
