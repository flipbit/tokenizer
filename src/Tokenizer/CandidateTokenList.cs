using System.Text;
using Tokens.Enumerators;
using Tokens.Tokenization;

namespace Tokens;

/// <summary>
/// Holds a list of candidate tokens to match during a Tokenize operation.
/// </summary>
internal sealed class CandidateTokenList
{
    private readonly List<Token> _tokens = new List<Token>();

    /// <summary>
    /// Adds a token to the candidate list. If this is the first token added, its preamble and
    /// newline-termination settings are adopted as the list-level defaults.
    /// </summary>
    /// <param name="token">The token to add.</param>
    public void Add(Token token)
    {
        if (_tokens.Count == 0)
        {
            Preamble = token.Preamble;
            TerminateOnNewLine = token.TerminateOnNewLine;
            IsNullToken = string.IsNullOrWhiteSpace(token.Name);
            _tokens.Add(token);
        }
        else
        {
            _tokens.Add(token);
        }
    }

    /// <summary>
    /// Adds a sequence of tokens to the candidate list.
    /// </summary>
    /// <param name="tokens">The tokens to add.</param>
    public void AddRange(IEnumerable<Token> tokens)
    {
        foreach (var token in tokens)
        {
            Add(token);
        }
    }

    /// <summary>
    /// Removes all tokens from the list and resets the preamble to an empty string.
    /// </summary>
    public void Clear()
    {
        Preamble = string.Empty;
        TerminateOnNewLine = false;
        IsNullToken = false;
        _tokens.Clear();
    }

    /// <summary>
    /// Evaluates the given string value against each candidate token using the decorator pipeline.
    /// Returns true if a candidate's decorators accept the value.
    /// </summary>
    /// <param name="value">The raw matched text to evaluate.</param>
    /// <param name="pipeline">The decorator pipeline that handles transformers and validators.</param>
    /// <param name="location">The location in the source input where the value was found.</param>
    /// <param name="evaluated">When this method returns <see langword="true"/>, the token that successfully accepted the value; otherwise <see langword="null"/>.</param>
    /// <param name="evaluatedValue">When this method returns <see langword="true"/>, the (potentially transformed) value; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a candidate token accepted the value; otherwise <see langword="false"/>.</returns>
    public bool TryEvaluate(StringBuilder value, DecoratorPipeline pipeline, FileLocation location, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Token? evaluated, out object? evaluatedValue)
    {
        evaluated = null;
        evaluatedValue = null;

        var valueString = value.ToString();

        foreach (var token in _tokens)
        {
            if (pipeline.Evaluate(token, valueString, location, out evaluatedValue))
            {
                evaluated = token;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if at least one candidate token's decorators would accept the given value.
    /// </summary>
    /// <param name="value">The value to test against the candidate tokens.</param>
    /// <param name="pipeline">The decorator pipeline that handles the decorator check.</param>
    /// <returns><see langword="true"/> if any candidate token can accept the value; otherwise <see langword="false"/>.</returns>
    public bool CanAnyEvaluate(string value, DecoratorPipeline pipeline)
    {
        foreach (var token in _tokens)
        {
            if (pipeline.CanEvaluate(token, value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether the list contains at least one candidate token.
    /// </summary>
    public bool HasCandidates => Count > 0;

    /// <summary>
    /// Gets the number of candidate tokens in the list.
    /// </summary>
    public int Count => _tokens.Count;

    /// <summary>
    /// Gets the preamble text that must appear in the input before a value can be extracted.
    /// This is taken from the first token added to the list.
    /// </summary>
    public string Preamble { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether value extraction should stop at the end of the current line.
    /// This is taken from the first token added to the list.
    /// </summary>
    public bool TerminateOnNewLine { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the leading token has no name and therefore captures no value.
    /// </summary>
    public bool IsNullToken { get; private set; }

    /// <summary>
    /// Gets the underlying list of candidate _tokens.
    /// </summary>
    public IList<Token> Tokens => _tokens;

    /// <summary>
    /// Removes the specified token from the candidate list.
    /// </summary>
    /// <param name="token">The token to remove.</param>
    public void Remove(Token token)
    {
        _tokens.Remove(token);
    }
}
