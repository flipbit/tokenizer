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
    public bool PreProcess(Template template, TokenEnumerator enumerator,
                           string? rawInput, TokenizeResult result, IDiagnosticCollector collector);

    /// <summary>
    /// Called by the tokenization session after each buffer refill, passing the staging
    /// buffer contents before they are copied into the ring buffer.
    /// </summary>
    /// <param name="buffer">The staging buffer containing newly-read characters.</param>
    /// <param name="count">The number of valid characters in <paramref name="buffer"/>.</param>
    public void OnBufferFilled(char[] buffer, int count);

    /// <summary>
    /// Post-processes hints after tokenization completes.
    /// Returns true if required hints are missing.
    /// </summary>
    /// <param name="result">The result object containing tokenization results.</param>
    /// <returns>True if required hints are missing, false otherwise.</returns>
    public bool PostProcess(TokenizeResult result);
}
