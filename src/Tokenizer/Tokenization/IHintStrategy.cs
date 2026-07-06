using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Defines a strategy for processing template hints during tokenization.
/// </summary>
internal interface IHintStrategy
{
    /// <summary>
    /// Pre-processes hints before tokenization begins.
    /// Returns true if required hints are missing and tokenization should be skipped.
    /// </summary>
    /// <param name="template">The template containing hint definitions.</param>
    /// <param name="enumerator">The token enumerator positioned at the start of input.</param>
    /// <param name="rawInput">The original string when available, null for TextReader-only inputs.</param>
    /// <param name="result">The result object to populate with hint matches and misses.</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    /// <returns>True if required hints are missing, false if all required hints are found.</returns>
    // rawInput enables fast string-based hint pre-filtering on sync paths where the full
    // input is available. Async/streaming paths pass null and fall back to integrated
    // single-pass hint checking via OnTokenMatched callbacks.
    public bool PreProcess(Template template, TokenEnumerator enumerator,
                    string? rawInput, TokenizeResultBase result, IDiagnosticCollector collector);

    /// <summary>
    /// Called by the engine when a token preamble matches during tokenization (for single-pass strategies).
    /// </summary>
    /// <param name="token">The token whose preamble matched.</param>
    public void OnTokenMatched(Token token);

    /// <summary>
    /// Post-processes hints after tokenization completes.
    /// Returns true if required hints are missing (for single-pass strategies).
    /// </summary>
    /// <param name="result">The result object containing tokenization results.</param>
    /// <returns>True if required hints are missing, false otherwise.</returns>
    public bool PostProcess(TokenizeResultBase result);
}
