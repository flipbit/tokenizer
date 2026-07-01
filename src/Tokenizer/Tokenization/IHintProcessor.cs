using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Defines the hint processor that finds and validates hints in input text
/// according to template hint definitions.
/// </summary>
public interface IHintProcessor
{
    /// <summary>
    /// Finds all hints defined in the template within the input text and validates them.
    /// </summary>
    /// <param name="template">The template containing hint definitions</param>
    /// <param name="enumerator">The token enumerator positioned at the start of input</param>
    /// <param name="result">The result object to populate with hint matches and misses</param>
    /// <returns>True if any required hints are missing, false if all required hints are found</returns>
    bool FindAndValidateHints(Template template, TokenEnumerator enumerator, TokenizeResultBase result, IDiagnosticCollector collector);

    /// <summary>
    /// Checks if a specific hint text matches at the current enumerator position.
    /// </summary>
    /// <param name="hint">The hint to check for</param>
    /// <param name="enumerator">The token enumerator at the position to check</param>
    /// <returns>True if the hint matches at the current position</returns>
    bool IsHintMatch(Hint hint, TokenEnumerator enumerator);

    /// <summary>
    /// Adds a hint match to the result and advances the enumerator past the matched hint.
    /// </summary>
    /// <param name="hint">The hint that was matched</param>
    /// <param name="enumerator">The token enumerator positioned at the hint</param>
    /// <param name="result">The result object to add the match to</param>
    /// <returns>True if the hint was successfully added as a match</returns>
    bool AddHintMatch(Hint hint, TokenEnumerator enumerator, TokenizeResultBase result);

    /// <summary>
    /// Adds a hint miss to the result for hints that were not found in the input.
    /// </summary>
    /// <param name="hint">The hint that was not found</param>
    /// <param name="result">The result object to add the miss to</param>
    /// <returns>True if the hint was successfully added as a miss</returns>
    bool AddHintMiss(Hint hint, TokenizeResultBase result);

    /// <summary>
    /// Resets the enumerator to the beginning of the input after hint processing is complete.
    /// </summary>
    /// <param name="enumerator">The token enumerator to reset</param>
    void ResetEnumeratorAfterHintProcessing(TokenEnumerator enumerator);
}
