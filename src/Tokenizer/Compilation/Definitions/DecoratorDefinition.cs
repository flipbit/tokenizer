using System.Text;

namespace Tokens.Compilation.Definitions;

/// <summary>
/// Intermediate data structure representing a decorator (validator or transformer) attached to a token.
/// </summary>
public class DecoratorDefinition
{
    private readonly StringBuilder name;

    /// <summary>
    /// Initializes a new empty <see cref="DecoratorDefinition"/>.
    /// </summary>
    public DecoratorDefinition()
    {
        name = new StringBuilder();
        Args = new List<string>();
    }

    /// <summary>
    /// Gets the decorator name identifying the validator or transformer to invoke.
    /// </summary>
    public string Name => name.ToString();

    /// <summary>
    /// Gets the arguments to pass to the decorator when it is invoked.
    /// </summary>
    public IList<string> Args { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this decorator is negated (i.e., the result is inverted).
    /// </summary>
    public bool IsNotDecorator { get; set; }

    /// <summary>
    /// Appends text to the decorator name.
    /// </summary>
    /// <param name="value">The text to append.</param>
    public void AppendName(string value)
    {
        name.Append(value);
    }
}
