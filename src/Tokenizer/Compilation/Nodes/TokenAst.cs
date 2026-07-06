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

/// <summary>
/// Represents the name identifier of a token or decorator.
/// </summary>
public sealed record TokenName
{
    /// <summary>
    /// Initializes a new <see cref="TokenName"/> with the given text.
    /// </summary>
    /// <param name="text">The raw name text.</param>
    public TokenName(string text)
    {
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// Gets the raw name text.
    /// </summary>
    public string Text { get; }
}

/// <summary>
/// Represents the set of modifier flags that can be applied to a token.
/// </summary>
/// <param name="IsOptional">Whether the token is optional (may not match).</param>
/// <param name="IsRepeating">Whether the token can repeat across multiple input lines.</param>
/// <param name="IsRequired">Whether the token must produce a value for the template to match.</param>
/// <param name="IsTerminate">Whether matching this token terminates further processing.</param>
public sealed record ModifierSet(bool IsOptional, bool IsRepeating, bool IsRequired, bool IsTerminate);

/// <summary>
/// Represents a literal value assigned to a token within the template source.
/// </summary>
public sealed record ValueNode
{
    /// <summary>
    /// Initializes a new <see cref="ValueNode"/> with the given text and quoting flag.
    /// </summary>
    /// <param name="text">The literal value text.</param>
    /// <param name="isQuoted">Whether the value was enclosed in quotes in the source.</param>
    public ValueNode(string text, bool isQuoted)
    {
        Text = text ?? string.Empty;
        IsQuoted = isQuoted;
    }

    /// <summary>
    /// Gets the literal value text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets a value indicating whether the value was enclosed in quotes in the source.
    /// </summary>
    public bool IsQuoted { get; }
}

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

/// <summary>
/// Represents a single argument passed to a decorator.
/// </summary>
public sealed record ArgumentNode
{
    /// <summary>
    /// Initializes a new <see cref="ArgumentNode"/> with the given text and quoting flag.
    /// </summary>
    /// <param name="text">The argument text.</param>
    /// <param name="isQuoted">Whether the argument was enclosed in quotes in the source.</param>
    public ArgumentNode(string text, bool isQuoted)
    {
        Text = text ?? string.Empty;
        IsQuoted = isQuoted;
    }

    /// <summary>
    /// Gets the argument text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets a value indicating whether the argument was enclosed in quotes in the source.
    /// </summary>
    public bool IsQuoted { get; }
}



