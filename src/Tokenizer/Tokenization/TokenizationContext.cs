using System;
using System.Collections.Generic;
using System.Text;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Tokenization context that encapsulates shared state during tokenization operations.
/// This context manages the state that needs to be shared between different tokenization services,
/// including candidate tokens, enumerator, replacement state, and tracking collections.
/// </summary>
public sealed class TokenizationContext : ITokenizationContext, IDisposable
{
    private bool _disposed = false;

    /// <summary>
    /// Gets the candidate token list containing tokens that are currently being considered for matching.
    /// </summary>
    public CandidateTokenList Candidates { get; private set; }

    /// <summary>
    /// Gets the token enumerator that provides access to the input text being processed.
    /// </summary>
    public TokenEnumerator Enumerator { get; private set; } = new TokenEnumerator(string.Empty);

    /// <summary>
    /// Gets the StringBuilder used for building replacement values during tokenization.
    /// </summary>
    public StringBuilder Replacement { get; private set; }

    /// <summary>
    /// Gets the set of token IDs that have been successfully matched.
    /// </summary>
    public HashSet<int> MatchIds { get; private set; }

    /// <summary>
    /// Gets the set of token IDs for repeating tokens that have been disabled.
    /// </summary>
    public HashSet<int> DisabledRepeatingTokens { get; private set; }

    /// <summary>
    /// Gets or sets the current replacement location in the input text.
    /// </summary>
    public FileLocation ReplacementLocation { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenizationContext"/> class.
    /// </summary>
    public TokenizationContext()
    {
        Candidates = new CandidateTokenList();
        Replacement = new StringBuilder();
        MatchIds = new HashSet<int>();
        DisabledRepeatingTokens = new HashSet<int>();
        ReplacementLocation = new FileLocation();
    }

    /// <summary>
    /// Initializes the context with the input text to be tokenized.
    /// </summary>
    /// <param name="input">The input text to tokenize</param>
    public void Initialize(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input cannot be null or empty", nameof(input));

        Enumerator = new TokenEnumerator(input);
        Reset();
    }

    /// <summary>
    /// Clears the candidate token list.
    /// </summary>
    public void ClearCandidates()
    {
        Candidates.Clear();
    }

    /// <summary>
    /// Clears the replacement StringBuilder.
    /// </summary>
    public void ClearReplacement()
    {
        Replacement.Clear();
    }

    /// <summary>
    /// Resets the context to its initial state, clearing all collections and resetting the enumerator.
    /// </summary>
    public void Reset()
    {
        ClearCandidates();
        ClearReplacement();
        MatchIds.Clear();
        DisabledRepeatingTokens.Clear();
        ReplacementLocation = new FileLocation();

        if (Enumerator != null)
        {
            Enumerator.Reset();
        }
    }

    /// <summary>
    /// Disposes of the context and any resources it holds.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        if (Enumerator is IDisposable disposableEnumerator)
        {
            disposableEnumerator.Dispose();
        }

        _disposed = true;
    }
}
