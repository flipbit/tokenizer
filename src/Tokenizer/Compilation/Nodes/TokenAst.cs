using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

public sealed class TokenNode : ContentNode
{
    public TokenNode(FileLocation location, int start, int length, TokenName name, ModifierSet modifiers, ValueNode? value, IReadOnlyList<DecoratorNode> decorators)
        : base(location, start, length)
    {
        Name = name;
        Modifiers = modifiers ?? new ModifierSet(false, false, false, false);
        Value = value;
        Decorators = decorators ?? System.Array.Empty<DecoratorNode>();
    }

    public TokenName Name { get; }
    public ModifierSet Modifiers { get; }
    public ValueNode? Value { get; }
    public IReadOnlyList<DecoratorNode> Decorators { get; }
}

public sealed record TokenName
{
    public TokenName(string text)
    {
        Text = text ?? string.Empty;
    }
    public string Text { get; }
}

public sealed record ModifierSet(bool IsOptional, bool IsRepeating, bool IsRequired, bool IsTerminate);

public sealed record ValueNode
{
    public ValueNode(string text, bool isQuoted)
    {
        Text = text ?? string.Empty;
        IsQuoted = isQuoted;
    }
    public string Text { get; }
    public bool IsQuoted { get; }
}

public sealed record DecoratorNode
{
    public DecoratorNode(TokenName name, IReadOnlyList<ArgumentNode> args, bool isNot = false)
    {
        Name = name;
        Args = args ?? System.Array.Empty<ArgumentNode>();
        IsNot = isNot;
    }
    public TokenName Name { get; }
    public IReadOnlyList<ArgumentNode> Args { get; }
    public bool IsNot { get; }
}

public sealed record ArgumentNode
{
    public ArgumentNode(string text, bool isQuoted)
    {
        Text = text ?? string.Empty;
        IsQuoted = isQuoted;
    }
    public string Text { get; }
    public bool IsQuoted { get; }
}



