using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Defines the result builder that creates and populates tokenization result objects
/// with matches, misses, and exception information.
/// </summary>
internal interface IResultBuilder
{
    /// <summary>
    /// Builds the collection of unmatched tokens by comparing template tokens
    /// against the tokens that were successfully matched.
    /// </summary>
    /// <param name="template">The template containing all token definitions</param>
    /// <param name="result">The result object to populate with unmatched tokens</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    public void BuildUnmatchedTokens(Template template, TokenizeResult result, IDiagnosticCollector collector);
}
