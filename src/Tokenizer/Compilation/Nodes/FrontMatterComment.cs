using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a front matter comment line (starting with '#').
/// </summary>
public sealed class FrontMatterComment : SyntaxNode
{
    public FrontMatterComment(FileLocation location, int start, int length, string text)
        : base(location, start, length)
    {
        Text = text ?? string.Empty;
    }

    public string Text { get; }
}


