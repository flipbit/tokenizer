using System.Text;
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Holds a list of candidate tokens to match during a Tokenize operation.
/// </summary>
public class CandidateTokenList
{
    private readonly List<Token> tokens = new List<Token>();

    /// <summary>
    /// Adds a token to the candidate list. If this is the first token added, its preamble and
    /// newline-termination settings are adopted as the list-level defaults.
    /// </summary>
    /// <param name="token">The token to add.</param>
    public void Add(Token token)
    {
        if (tokens.Count == 0)
        {
            Preamble = token.Preamble;
            TerminateOnNewLine = token.TerminateOnNewLine;
            IsNullToken = string.IsNullOrWhiteSpace(token.Name);
            tokens.Add(token);
        }
        else
        {
            tokens.Add(token);
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
        tokens.Clear();
    }

    /// <summary>
    /// Attempts to assign the given string value to the target object using the first candidate token
    /// whose validators and transformers accept it.
    /// </summary>
    /// <param name="target">The object to assign the value to, or <see langword="null"/> for unbound matching.</param>
    /// <param name="value">The raw matched text to assign.</param>
    /// <param name="options">The tokenizer options governing assignment behaviour.</param>
    /// <param name="location">The location in the source input where the value was found.</param>
    /// <param name="assigned">When this method returns <see langword="true"/>, the token that successfully accepted the value; otherwise <see langword="null"/>.</param>
    /// <param name="assignedValue">When this method returns <see langword="true"/>, the (potentially transformed) value that was assigned; otherwise <see langword="null"/>.</param>
    /// <param name="collector">Collector that receives diagnostic events raised during assignment.</param>
    /// <returns><see langword="true"/> if a candidate token accepted the value; otherwise <see langword="false"/>.</returns>
    public bool TryAssign(object? target, StringBuilder value, TokenizerOptions options, FileLocation location, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Token? assigned, out object? assignedValue, IDiagnosticCollector collector)
    {
        assigned = null;
        assignedValue = null;

        var valueString = value.ToString();

        foreach (var token in tokens)
        {
            if (token.Assign(target, valueString, options, location, out assignedValue, collector))
            {
                assigned = token;

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if at least one candidate token could accept the given value
    /// (i.e. its validators would pass), without performing an actual assignment.
    /// </summary>
    /// <param name="value">The value to test against the candidate tokens.</param>
    /// <returns><see langword="true"/> if any candidate token can accept the value; otherwise <see langword="false"/>.</returns>
    public bool CanAnyAssign(string value)
    {
        foreach (var token in tokens)
        {
            if (token.CanAssign(value))
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
    public int Count => tokens.Count;

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
    /// Gets the underlying list of candidate tokens.
    /// </summary>
    public IList<Token> Tokens => tokens;

    /// <summary>
    /// Removes the specified token from the candidate list.
    /// </summary>
    /// <param name="token">The token to remove.</param>
    public void Remove(Token token)
    {
        tokens.Remove(token);
    }
}
