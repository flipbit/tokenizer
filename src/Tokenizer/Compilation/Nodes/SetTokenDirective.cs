using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes
{
    /// <summary>
    /// Represents a set directive within front matter (e.g., set: Name = Value : Decorator(args...)).
    /// </summary>
    public sealed class SetTokenDirective : SyntaxNode
    {
        public SetTokenDirective(FileLocation location, int start, int length, string tokenName, string value = null, IReadOnlyList<SetDecorator> decorators = null)
            : base(location, start, length)
        {
            TokenName = tokenName ?? string.Empty;
            Value = value;
            Decorators = decorators ?? System.Array.Empty<SetDecorator>();
        }

        public string TokenName { get; }
        public string Value { get; }
        public IReadOnlyList<SetDecorator> Decorators { get; }
    }

    public sealed class SetDecorator
    {
        public SetDecorator(string name, IReadOnlyList<string> args = null)
        {
            Name = name ?? string.Empty;
            Args = args ?? System.Array.Empty<string>();
        }
        public string Name { get; }
        public IReadOnlyList<string> Args { get; }
    }
}


