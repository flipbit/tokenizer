using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens;

/// <summary>
/// Represents a single token in a string
/// </summary>
public sealed class Token
{
    private string _content;

    /// <summary>
    /// Creates a new instance of the <see cref="Token"/> class.
    /// </summary>
    private readonly List<TokenDecoratorContext> _decorators;

    /// <summary>
    /// Creates a new <see cref="Token"/> with the specified raw content, name, preamble, and source location.
    /// </summary>
    /// <param name="content">The raw token string as it appears in the template.</param>
    /// <param name="name">The token name used to map the extracted value to a target property.</param>
    /// <param name="preamble">The static text that must precede this token in the input.</param>
    /// <param name="location">The location of this token within the template pattern.</param>
    public Token(string content, string name, string preamble, FileLocation location)
    {
        this._content = content;
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

    /// <summary>
    /// Returns the string from which this token was created.
    /// </summary>
    public override string ToString()
    {
        return _content;
    }

    internal bool Assign(object? target, string value, TokenizerOptions options, FileLocation location, out object? assignedValue, IDiagnosticCollector collector)
    {
        assignedValue = null;

        var prepared = PrepareValue(value);
        if (prepared == null) return false;

        if (options.TrimTrailingWhiteSpace)
        {
            prepared = prepared.TrimEnd();
        }

        if (!RunDecoratorPipeline(prepared, collector, location, out assignedValue)) return false;

        if (target is IDictionary<string, object> dictionary)
        {
            return SetDictionaryValue(dictionary, assignedValue!);
        }

        // Target can be null if not reflecting onto an object
        if (target is null)
        {
            return true;
        }

        try
        {
            if (CanConcatenate)
            {
                if (assignedValue == null) return true;

                var current = target.GetValue(Name);

                if (CanConcatenateValues(current, assignedValue))
                {
                    var concatenated = ConcatenateValues(current, assignedValue, ConcatenationString);
                    if (concatenated != null) target.SetValue(Name, concatenated, StringComparison.Ordinal);
                }
                else
                {
                    throw new TokenAssignmentException(this, $"Unable to concatenate type {assignedValue.GetType().Name} to {Name}");
                }
            }
            else
            {
                target.SetValue(Name, assignedValue!, StringComparison.Ordinal);
            }
        }
        catch (MissingMemberException)
        {
            if (!options.IgnoreMissingProperties)
            {
                throw;
            }
        }
        catch (TypeConversionException ex)
        {
            collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                tokenName: Name, tokenId: Id,
                value: value,
                detail: $"Type conversion failed: {ex.Message}");
            return false;
        }
        catch (Exception e)
        {
            var ex = new TokenAssignmentException(this, e);

            throw ex;
        }

        return true;
    }

    private bool SetDictionaryValue(IDictionary<string, object> dictionary, object input)
    {
        if (IsRepeating)
        {
            List<object> list;
            if (dictionary.ContainsKey(Name))
            {
                list = dictionary[Name] as List<object> ?? new List<object>();
            }
            else
            {
                list = new List<object>();
            }
            list.Add(input);
            input = list;
        }

        if (dictionary.ContainsKey(Name))
        {
            dictionary[Name] = input;
        }
        else
        {
            dictionary.Add(Name, input);
        }

        return true;
    }

    internal bool CanAssign(string value)
    {
        var prepared = PrepareValue(value);
        if (prepared == null) return false;

        return RunDecoratorPipeline(prepared, collector: null, location: null, out _);
    }

    private string? PrepareValue(string value)
    {
        if (string.IsNullOrEmpty(value) && !IsFrontMatterToken) return null;
        if (IsNull) return null;
        if (string.IsNullOrWhiteSpace(Name)) return null;

        value = value.TrimTrailingNewLine();

        if (!string.IsNullOrEmpty(value) && TerminateOnNewLine)
        {
#pragma warning disable MA0001 // IndexOf(char) is inherently ordinal; no StringComparison overload exists
            var index = value.IndexOf('\n');
            if (index > 0)
            {
                value = value.Substring(0, index);
            }
#pragma warning restore MA0001
        }

        return value;
    }

    private bool RunDecoratorPipeline(object input, IDiagnosticCollector? collector,
        FileLocation? location, out object? assignedValue)
    {
        assignedValue = input;

        foreach (var decorator in Decorators)
        {
            if (decorator.IsTransformer)
            {
                if (!decorator.TryTransform(assignedValue!, out var output))
                {
                    collector?.Record(DiagnosticEventType.TransformerFailed,
                        tokenName: Name, tokenId: Id,
                        location: location,
                        value: assignedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name,
                        decoratorArgs: decorator.Parameters.ToArray());

                    return false;
                }

                collector?.Record(DiagnosticEventType.TransformerSucceeded,
                    tokenName: Name, tokenId: Id,
                    location: location,
                    value: assignedValue?.ToString(),
                    detail: output?.ToString(),
                    decoratorName: decorator.DecoratorType.Name,
                    decoratorArgs: decorator.Parameters.ToArray());

                assignedValue = output;
            }

            if (decorator.IsValidator)
            {
                if (decorator.Validate(assignedValue!))
                {
                    collector?.Record(DiagnosticEventType.ValidatorPassed,
                        tokenName: Name, tokenId: Id,
                        value: assignedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);
                }
                else
                {
                    collector?.Record(DiagnosticEventType.ValidatorFailed,
                        tokenName: Name, tokenId: Id,
                        value: input?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);

                    return false;
                }
            }
        }

        return true;
    }

    internal static bool CanConcatenateValues(object? existingValue, object newValue)
    {
        if (existingValue is string && newValue is string)
        {
            return true;
        }

        return false;
    }

    internal static object? ConcatenateValues(object? existingValue, object newValue, string? concatenationString)
    {
        if (existingValue is string && newValue is string)
        {
            var concatStringValue = (concatenationString ?? string.Empty).Replace("<CR>", Environment.NewLine, StringComparison.Ordinal);

            return $"{existingValue}{concatStringValue}{newValue}";
        }

        return existingValue;
    }
}
