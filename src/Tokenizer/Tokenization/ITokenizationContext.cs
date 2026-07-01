using System.Collections.Generic;
using System.Text;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Defines the tokenization context that encapsulates shared state during tokenization operations.
/// This context manages the state that needs to be shared between different tokenization services.
/// </summary>
public interface ITokenizationContext
{
    /// <summary>
    /// Gets the candidate token list containing tokens that are currently being considered for matching.
    /// </summary>
    CandidateTokenList Candidates { get; }

    /// <summary>
    /// Gets the token enumerator that provides access to the input text being processed.
    /// </summary>
    TokenEnumerator Enumerator { get; }

    /// <summary>
    /// Gets the StringBuilder used for building replacement values during tokenization.
    /// </summary>
    StringBuilder Replacement { get; }

    /// <summary>
    /// Gets the set of token IDs that have been successfully matched.
    /// </summary>
    HashSet<int> MatchIds { get; }

    /// <summary>
    /// Gets the set of token IDs for repeating tokens that have been disabled.
    /// </summary>
    HashSet<int> DisabledRepeatingTokens { get; }

    /// <summary>
    /// Gets a reusable buffer for filtering tokens in <see cref="Template.TokensExcluding"/>.
    /// </summary>
    List<Token> TokenFilterBuffer { get; }

    /// <summary>
    /// Gets a reusable set for tracking included token IDs during token filtering.
    /// </summary>
    HashSet<int> TokenFilterIds { get; }

    /// <summary>
    /// Gets a reusable set for building exclusion sets during token filtering.
    /// </summary>
    HashSet<int> ExclusionBuffer { get; }

    /// <summary>
    /// Gets the current replacement location in the input text.
    /// </summary>
    FileLocation ReplacementLocation { get; set; }

    /// <summary>
    /// Initializes the context with the input text to be tokenized.
    /// </summary>
    /// <param name="input">The input text to tokenize</param>
    void Initialize(string input);

    /// <summary>
    /// Clears the candidate token list.
    /// </summary>
    void ClearCandidates();

    /// <summary>
    /// Clears the replacement StringBuilder.
    /// </summary>
    void ClearReplacement();

    /// <summary>
    /// Resets the context to its initial state, clearing all collections and resetting the enumerator.
    /// </summary>
    void Reset();

    /// <summary>
    /// Disposes of the context and any resources it holds.
    /// </summary>
    void Dispose();
}
