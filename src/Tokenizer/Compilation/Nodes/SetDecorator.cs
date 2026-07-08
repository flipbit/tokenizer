namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents a decorator entry within a <see cref="SetTokenDirective"/>.
/// </summary>
public sealed record SetDecorator
{
    /// <summary>
    /// Initializes a new <see cref="SetDecorator"/> with the given name and arguments.
    /// </summary>
    /// <param name="name">The decorator name.</param>
    /// <param name="args">The optional arguments passed to the decorator.</param>
    public SetDecorator(string name, IReadOnlyList<string>? args = null)
    {
        Name = name ?? string.Empty;
        Args = args ?? System.Array.Empty<string>();
    }

    /// <summary>
    /// Gets the decorator name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the arguments passed to the decorator.
    /// </summary>
    public IReadOnlyList<string> Args { get; }
}
