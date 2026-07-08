using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens;

/// <summary>
/// Holds the result of attempting to parse an input string against a
/// <see cref="Template"/>.
/// </summary>
public sealed class TokenizeResult
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
    public bool Success => Tokens.HasMatches &&
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
    /// Projects matches onto a new instance of <typeparamref name="T"/>,
    /// assigning matched values to the object's properties via reflection.
    /// </summary>
    /// <typeparam name="T">The type to populate with matched values.</typeparam>
    /// <returns>A new instance of <typeparamref name="T"/> with populated properties.</returns>
    /// <exception cref="AssignmentFailedException">
    /// Thrown when one or more matched values cannot be assigned to the target's properties.
    /// </exception>
    public T Assign<T>() where T : class, new()
    {
        var target = new T();
        var options = Template.Options;
        var errors = new List<Exception>();

        foreach (var match in Matches)
        {
            try
            {
                target.SetValue(match.Token.Name, match.Value, StringComparison.Ordinal);
            }
            catch (MissingMemberException)
            {
                if (!options.IgnoreMissingProperties)
                {
                    errors.Add(new MissingMemberException(
                        $"Property '{match.Token.Name}' not found on type '{target.GetType().Name}'."));
                }
            }
            catch (TypeConversionException ex)
            {
                errors.Add(ex);
            }
            catch (TokenAssignmentException ex)
            {
                errors.Add(ex);
            }
            catch (ArgumentException ex)
            {
                errors.Add(ex);
            }
        }

        if (errors.Count > 0)
        {
            throw new AssignmentFailedException(
                $"Failed to assign {errors.Count} value(s) to type '{typeof(T).Name}'.", errors);
        }

        return target;
    }
}
