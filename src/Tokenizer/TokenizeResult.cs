using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens;

/// <summary>
/// Holds the result of attempting to parse an input string against a
/// <see cref="Template"/>.
/// </summary>
public class TokenizeResult
{
    private readonly List<Exception> _exceptions;

    /// <summary>
    /// Creates a new result bound to the specified <paramref name="template"/>.
    /// </summary>
    public TokenizeResult(Template template)
    {
        _exceptions = new List<Exception>();
        Hints = new HintResult();
        Tokens = new TokenResult();
        Template = template;
    }

    /// <summary>
    /// Creates a projected result carrying forward state from a completed tokenization.
    /// </summary>
    internal TokenizeResult(Template template, TokenResult tokens, HintResult hints, Diagnostics.DiagnosticResult? diagnostics)
    {
        _exceptions = new List<Exception>();
        Template = template;
        Tokens = tokens;
        Hints = hints;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// The template used for the tokenization attempt.
    /// </summary>
    public Template Template { get; init; }

    /// <summary>
    /// A list of any exceptions that occurred during the matching process.
    /// </summary>
    public IReadOnlyList<Exception> Exceptions => _exceptions;

    /// <summary>
    /// The matches that were made during the tokenization process.
    /// </summary>
    public TokenResult Tokens { get; init; }

    /// <summary>
    /// Gets the hints found in the input.
    /// </summary>
    public HintResult Hints { get; init; }

    internal void AddException(Exception exception)
    {
        _exceptions.Add(exception);
    }

    /// <summary>
    /// Structured diagnostic output from the tokenization process.
    /// Null when <see cref="TokenizerOptions.EnableDiagnostics"/> is false.
    /// </summary>
    public Diagnostics.DiagnosticResult? Diagnostics { get; internal set; }

    /// <summary>
    /// Determines whether the matching process was successful.
    /// </summary>
    public virtual bool Success => Tokens.HasMatches &&
                                   !Tokens.HasMissingRequiredTokens &&
                                   !Hints.HasMissingRequiredHints &&
                                   (Template.HasOnlyFrontMatterTokens || Tokens.Matches.Any(m => !m.Token.IsFrontMatterToken));

    /// <summary>
    /// A read-only list of values extracted from the input string.
    /// </summary>
    public IReadOnlyList<TokenMatch> Matches => Tokens.Matches;

    /// <inheritdoc />
    public override string ToString() =>
        $"TokenizeResult('{Template.Name}': {Tokens.Matches.Count} matched, {Tokens.Misses.Count} missed)";

    /// <summary>
    /// Projects this result onto a new instance of <typeparamref name="T"/>,
    /// assigning matched values to the object's properties via reflection.
    /// The original result is not modified.
    /// </summary>
    /// <typeparam name="T">The type to populate with matched values.</typeparam>
    /// <returns>A new <see cref="TokenizeResult{T}"/> with the populated object.</returns>
    public TokenizeResult<T> Assign<T>() where T : class, new()
    {
        var typed = new TokenizeResult<T>(Template, Tokens, Hints, Diagnostics);
        var target = typed.Value;
        var options = Template.Options;

        AssignToObject(target, options, typed);

        return typed;
    }

    private static void AssignToObject(object target, TokenizerOptions options, TokenizeResult typed)
    {
        foreach (var match in typed.Tokens.Matches)
        {
            try
            {
                target.SetValue(match.Token.Name, match.Value, StringComparison.Ordinal);
            }
            catch (MissingMemberException)
            {
                if (!options.IgnoreMissingProperties)
                {
                    typed.AddException(new MissingMemberException(
                        $"Property '{match.Token.Name}' not found on type '{target.GetType().Name}'."));
                }
            }
            catch (TypeConversionException ex)
            {
                typed.AddException(ex);
            }
            catch (TokenAssignmentException ex)
            {
                typed.AddException(ex);
            }
            catch (ArgumentException ex)
            {
                typed.AddException(ex);
            }
        }
    }
}

/// <summary>
/// Holds the result of attempting to parse an input string against a
/// <see cref="Template"/> to generate an object of type <typeparamref name="T"/>.
/// </summary>
public sealed class TokenizeResult<T> : TokenizeResult where T : class, new()
{
    /// <summary>
    ///  Creates a new instance of the <see cref="TokenizeResult{T}"/> class.
    /// </summary>
    public TokenizeResult(Template template) : base(template)
    {
        Value = new T();
    }

    /// <summary>
    /// Creates a projected result carrying forward matching state from a completed tokenization.
    /// Stage 1 exceptions are not copied — only assignment exceptions belong on typed results.
    /// </summary>
    internal TokenizeResult(Template template, TokenResult tokens, HintResult hints, Diagnostics.DiagnosticResult? diagnostics)
        : base(template, tokens, hints, diagnostics)
    {
        Value = new T();
    }

    /// <summary>
    /// An instance of <typeparamref name="T"/> populated with data from the input string.
    /// </summary>
    public T Value { get; init; }

    /// <summary>
    /// True when matching succeeded and no assignment errors occurred.
    /// </summary>
    public override bool Success => base.Success && Exceptions.Count == 0;
}
