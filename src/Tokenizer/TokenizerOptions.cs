using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens;

/// <summary>
/// Options for the <see cref="Tokenizer"/>.
/// </summary>
/// <remarks>
/// This record class cannot be sealed because C# record types require a protected copy
/// constructor for deep-copy semantics via <c>with {}</c> expressions.
/// </remarks>
public record class TokenizerOptions
{
    private readonly List<Type> _transformers = new List<Type>();
    private readonly List<Type> _validators = new List<Type>();

    /// <summary>
    /// Copy constructor used by the record's <c>with</c> expression. Deep-copies the
    /// transformer and validator lists so the original and the copy do not share references.
    /// </summary>
    protected TokenizerOptions(TokenizerOptions original)
    {
        IgnoreMissingProperties = original.IgnoreMissingProperties;
        EnableDiagnostics = original.EnableDiagnostics;
        TrimLeadingWhitespaceInTokenPreamble = original.TrimLeadingWhitespaceInTokenPreamble;
        TrimPreambleBeforeNewLine = original.TrimPreambleBeforeNewLine;
        TrimTrailingWhiteSpace = original.TrimTrailingWhiteSpace;
        OutOfOrderTokens = original.OutOfOrderTokens;
        TokenStringComparison = original.TokenStringComparison;
        TerminateOnNewLine = original.TerminateOnNewLine;
        MaxInputLength = original.MaxInputLength;
        MaxTemplateLength = original.MaxTemplateLength;
        MaxTokenCount = original.MaxTokenCount;
        MaxIterations = original.MaxIterations;
        AllowStreamBuffering = original.AllowStreamBuffering;
        _transformers = new List<Type>(original._transformers);
        _validators = new List<Type>(original._validators);
    }

    /// <summary>
    /// When true, tokens that do not map to a property on the target object are silently ignored.
    /// </summary>
    public bool IgnoreMissingProperties { get; init; }

    /// <summary>
    /// When true, tokenization results include a <see cref="Diagnostics.DiagnosticResult"/>
    /// property with a structured trace of every matching decision, a mismatch summary
    /// with adaptive hints, and a visual alignment diff.
    /// Default: false. Has no performance impact when disabled.
    /// </summary>
    public bool EnableDiagnostics { get; init; }

    /// <summary>
    /// When true, leading whitespace in the static text preceding a token is trimmed before matching.
    /// </summary>
    public bool TrimLeadingWhitespaceInTokenPreamble { get; init; } = true;

    /// <summary>
    /// When true, any portion of a token preamble that appears before a newline is discarded.
    /// </summary>
    public bool TrimPreambleBeforeNewLine { get; init; }

    /// <summary>
    /// When true, trailing whitespace is trimmed from each extracted token value.
    /// </summary>
    public bool TrimTrailingWhiteSpace { get; init; } = true;

    /// <summary>
    /// When true, tokens may be matched in any order rather than strictly left-to-right.
    /// </summary>
    public bool OutOfOrderTokens { get; init; }

    /// <summary>
    /// Determines the <see cref="StringComparison"/> type to use when matching Token names to object properties
    /// </summary>
    public StringComparison TokenStringComparison { get; init; } = StringComparison.InvariantCulture;

    /// <summary>
    /// If set, token values will be extracted up till the first new line character.
    /// </summary>
    public bool TerminateOnNewLine { get; init; }

    /// <summary>
    /// Maximum allowed length for input text. Default: 1,048,576 (1MB).
    /// Set to 0 to disable.
    /// </summary>
    public int MaxInputLength { get; init; } = 1_048_576;

    /// <summary>
    /// Maximum allowed length for template pattern text. Default: 65,536 (64KB).
    /// Set to 0 to disable.
    /// </summary>
    public int MaxTemplateLength { get; init; } = 65_536;

    /// <summary>
    /// Maximum number of tokens allowed in a template. Default: 500.
    /// Set to 0 to disable.
    /// </summary>
    public int MaxTokenCount { get; init; } = 500;

    /// <summary>
    /// Maximum number of iterations in the tokenization loop.
    /// Default: 0 (auto-calculated as input.Length * 2).
    /// Set to a positive value to override.
    /// </summary>
    public int MaxIterations { get; init; }

    /// <summary>
    /// When true, allows non-seekable streams (e.g. NetworkStream) to be buffered into memory
    /// for operations that require re-reading the input (such as TemplateMatcher matching against
    /// multiple templates). Default: false.
    /// When false, passing a non-seekable stream to such operations throws a TokenizerException.
    /// </summary>
    public bool AllowStreamBuffering { get; init; }

    /// <summary>
    /// Custom transformer types registered on this options instance.
    /// These are added after the default transformers when building a <see cref="Compilation.TemplateCompiler"/>.
    /// </summary>
    public IReadOnlyList<Type> Transformers => _transformers.AsReadOnly();

    /// <summary>
    /// Custom validator types registered on this options instance.
    /// These are added after the default validators when building a <see cref="Compilation.TemplateCompiler"/>.
    /// </summary>
    public IReadOnlyList<Type> Validators => _validators.AsReadOnly();

    /// <summary>
    /// Returns a new <see cref="TokenizerOptions"/> instance with the given transformer type appended.
    /// </summary>
    public TokenizerOptions WithTransformer<T>() where T : ITokenTransformer
    {
        var copy = this with { };
        copy._transformers.Add(typeof(T));
        return copy;
    }

    /// <summary>
    /// Returns a new <see cref="TokenizerOptions"/> instance with the given validator type appended.
    /// </summary>
    public TokenizerOptions WithValidator<T>() where T : ITokenValidator
    {
        var copy = this with { };
        copy._validators.Add(typeof(T));
        return copy;
    }

    /// <summary>
    /// Determines equality based on option settings only; <see cref="Transformers"/> and
    /// <see cref="Validators"/> are excluded because they are additive registrations,
    /// not part of the options identity.
    /// </summary>
    public virtual bool Equals(TokenizerOptions? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.EqualityContract != EqualityContract) return false;

        return IgnoreMissingProperties == other.IgnoreMissingProperties
            && EnableDiagnostics == other.EnableDiagnostics
            && TrimLeadingWhitespaceInTokenPreamble == other.TrimLeadingWhitespaceInTokenPreamble
            && TrimPreambleBeforeNewLine == other.TrimPreambleBeforeNewLine
            && TrimTrailingWhiteSpace == other.TrimTrailingWhiteSpace
            && OutOfOrderTokens == other.OutOfOrderTokens
            && TokenStringComparison == other.TokenStringComparison
            && TerminateOnNewLine == other.TerminateOnNewLine
            && MaxInputLength == other.MaxInputLength
            && MaxTemplateLength == other.MaxTemplateLength
            && MaxTokenCount == other.MaxTokenCount
            && MaxIterations == other.MaxIterations
            && AllowStreamBuffering == other.AllowStreamBuffering;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + IgnoreMissingProperties.GetHashCode();
            hash = hash * 31 + EnableDiagnostics.GetHashCode();
            hash = hash * 31 + TrimLeadingWhitespaceInTokenPreamble.GetHashCode();
            hash = hash * 31 + TrimPreambleBeforeNewLine.GetHashCode();
            hash = hash * 31 + TrimTrailingWhiteSpace.GetHashCode();
            hash = hash * 31 + OutOfOrderTokens.GetHashCode();
            hash = hash * 31 + TokenStringComparison.GetHashCode();
            hash = hash * 31 + TerminateOnNewLine.GetHashCode();
            hash = hash * 31 + MaxInputLength.GetHashCode();
            hash = hash * 31 + MaxTemplateLength.GetHashCode();
            hash = hash * 31 + MaxTokenCount.GetHashCode();
            hash = hash * 31 + MaxIterations.GetHashCode();
            hash = hash * 31 + AllowStreamBuffering.GetHashCode();
            return hash;
        }
    }
}
