using System.Diagnostics;
using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Represents a single token in a string.
/// Properties use <c>internal set</c> because they are populated by the compilation
/// pipeline (TokenBinder and OptionApplier) after construction.
/// </summary>
[DebuggerDisplay("{Name} (Id={Id}, Optional={IsOptional})")]
public sealed class Token
{
    private readonly List<TokenDecoratorContext> _decorators;

    /// <summary>
    /// Creates a new <see cref="Token"/> with the specified name, preamble, and source location.
    /// </summary>
    /// <param name="name">The token name used to map the extracted value to a target property.</param>
    /// <param name="preamble">The static text that must precede this token in the input.</param>
    /// <param name="location">The location of this token within the template pattern.</param>
    public Token(string name, string preamble, FileLocation location)
    {
        Name = name;
        Preamble = preamble;
        Location = location;
        _decorators = new List<TokenDecoratorContext>();
    }

    /// <summary>
    /// Gets or sets the preamble string that must appear before the token.
    /// </summary>
    public string Preamble { get; internal set; }

    /// <summary>
    /// Gets or sets the value of the token.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// Gets the decorators on this Token
    /// </summary>
    public IReadOnlyList<TokenDecoratorContext> Decorators => _decorators;

    internal void AddDecorator(TokenDecoratorContext decorator)
    {
        _decorators.Add(decorator);
    }

    /// <summary>
    /// If <see langword="true"/> then this <see cref="Token"/> is optional and can be skipped
    /// during processing.
    /// </summary>
    public bool IsOptional { get; internal set; }

    /// <summary>
    /// If <see langword="true"/> then this <see cref="Token"/> can map multiple instances onto
    /// an <see cref="IList{T}"/>.
    /// </summary>
    public bool IsRepeating { get; internal set; }

    /// <summary>
    /// If <see langword="true"/> then this <see cref="Token"/> will map a value up to the next
    /// newline.
    /// </summary>
    public bool TerminateOnNewLine { get; internal set; }

    /// <summary>
    /// If <see langword="true"/> then this <see cref="Token"/> must be present in the input for
    /// the processing to be successful.
    /// </summary>
    public bool IsRequired { get; internal set; }

    /// <summary>
    /// The unique id of this token in the <see cref="Template"/>.
    /// </summary>
    public int Id { get; internal set; }

    /// <summary>
    /// Defines a token that must have been matched in the input before this token
    /// can be considered.  Used with repeating tokens that would otherwise be
    /// to aggressive in their matching.
    /// </summary>
    public int DependsOnId { get; internal set; } = -1;

    /// <summary>
    /// Determines if this <see cref="Token"/> was defined in the template front matter section.
    /// </summary>
    public bool IsFrontMatterToken { get; internal set; }

    /// <summary>
    /// Determines if this token is a null placeholder
    /// </summary>
    public bool IsNull { get; internal set; }

    /// <summary>
    /// The location of this token in the template.
    /// </summary>
    public FileLocation Location { get; internal set; }

    /// <summary>
    /// If true, multiple instances of this token will be concatenated together
    /// on the target.
    /// </summary>
    public bool CanConcatenate { get; internal set; }

    /// <summary>
    /// Defines a joining string to use when concatenating two token values.
    /// </summary>
    public string? ConcatenationString { get; internal set; }

    /// <summary>
    /// If true, this token will only be attempted to be matched once.
    /// </summary>
    public bool IsSingleUse { get; internal set; }
}
