using System.Text;
using Tokens.Enumerators;

namespace Tokens.Compilation.Definitions;

/// <summary>
/// Intermediate data structure that holds the syntactically verified
/// template token data.
/// </summary>
public class TokenDefinition
{
    private readonly StringBuilder _preamble;
    private readonly StringBuilder _name;
    private readonly StringBuilder _value;

    /// <summary>
    /// Initializes a new empty <see cref="TokenDefinition"/>.
    /// </summary>
    public TokenDefinition()
    {
        Decorators = new List<DecoratorDefinition>();
        _preamble = new StringBuilder();
        _name = new StringBuilder();
        _value = new StringBuilder();
    }

    /// <summary>
    /// Gets or sets the unique identifier for this token within its template.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the token this one depends on, or <c>-1</c> if independent.
    /// </summary>
    public int DependsOnId { get; set; } = -1;

    /// <summary>
    /// Gets the static text that must appear before this token in the input.
    /// </summary>
    public string Preamble => _preamble.ToString();

    /// <summary>
    /// Gets the token name used to map the extracted value onto the target object.
    /// </summary>
    public string Name => _name.ToString();

    /// <summary>
    /// Gets the hard-coded value assigned to this token, or an empty string if the token captures from input.
    /// </summary>
    public string Value => _value.ToString();

    /// <summary>
    /// Gets or sets a value indicating whether this token is optional (may not match).
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether matching this token terminates on a new line.
    /// </summary>
    public bool TerminateOnNewLine { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this token can repeat across multiple input lines.
    /// </summary>
    public bool IsRepeating { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this token must produce a value for the template to match.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this token always binds a null value.
    /// </summary>
    public bool IsNull { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this token may only be matched once per tokenization pass.
    /// </summary>
    public bool IsSingleUse { get; set; }

    /// <summary>
    /// Gets or sets the raw source content of the token as it appeared in the template string.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source location where this token was defined.
    /// </summary>
    public FileLocation Location { get; set; } = new FileLocation();

    /// <summary>
    /// Gets the decorators (validators and transformers) applied to this token.
    /// </summary>
    public IList<DecoratorDefinition> Decorators { get; }

    /// <summary>
    /// Appends text to the preamble, skipping bare carriage-return characters.
    /// </summary>
    /// <param name="value">The text to append.</param>
    public void AppendPreamble(string value)
    {
        if (string.Equals(value, "\r", StringComparison.Ordinal)) return;

        _preamble.Append(value);
    }

    /// <summary>
    /// Appends text to the token name.
    /// </summary>
    /// <param name="value">The text to append.</param>
    public void AppendName(string value)
    {
        _name.Append(value);
    }

    /// <summary>
    /// Appends text to the token's hard-coded value.
    /// </summary>
    /// <param name="value">The text to append.</param>
    public void AppendValue(string value)
    {
        _value.Append(value);
    }

    /// <summary>
    /// Appends a collection of decorators to this token's decorator list.
    /// </summary>
    /// <param name="decorators">The decorators to append.</param>
    public void AppendDecorators(IEnumerable<DecoratorDefinition> decorators)
    {
        if (decorators == null) return;

        foreach (var decorator in decorators)
        {
            Decorators.Add(decorator);
        }
    }

    /// <summary>
    /// Gets a value indicating whether a hard-coded value has been set on this token.
    /// </summary>
    public bool HasValue => _value.Length > 0;

    /// <summary>
    /// Gets or sets a value indicating whether this token originated in the front matter block.
    /// </summary>
    public bool IsFrontMatterToken { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Content;
    }

    internal void TrimPreambleBeforeNewLine()
    {
        var preambleContent = _preamble.ToString();

        if (preambleContent.Contains("\n", StringComparison.Ordinal))
        {
            var trimmed = preambleContent.Substring(preambleContent.LastIndexOf('\n') + 1);

            _preamble.Clear();
            _preamble.Append(trimmed);
        }
    }
}
