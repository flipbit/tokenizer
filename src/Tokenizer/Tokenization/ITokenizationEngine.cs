using System.Text;
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Defines the core tokenization engine that processes input text and matches tokens
/// according to template patterns.
/// </summary>
internal interface ITokenizationEngine
{
    /// <summary>
    /// Processes the main tokenization algorithm, matching tokens from input text
    /// and assigning values to the target object.
    /// </summary>
    /// <param name="template">The template containing token definitions</param>
    /// <param name="input">The input text to tokenize</param>
    /// <param name="targetObject">The object to populate with matched token values</param>
    /// <param name="context">The tokenization context containing shared state</param>
    /// <param name="result">The result object to populate with matches and misses</param>
    void ProcessTokenization(Template template, string input, object? targetObject, ITokenizationContext context, TokenizeResultBase result, IDiagnosticCollector collector);

    /// <summary>
    /// Processes candidate tokens and attempts to assign values to the target object.
    /// </summary>
    /// <param name="candidates">The list of candidate tokens to process</param>
    /// <param name="targetObject">The object to populate with matched token values</param>
    /// <param name="replacement">The StringBuilder containing the token value</param>
    /// <param name="options">The tokenizer options</param>
    /// <param name="location">The location where the token was found</param>
    /// <param name="result">The result object to populate with matches</param>
    /// <param name="template">The template containing token definitions</param>
    /// <param name="matchIds">The set of matched token IDs</param>
    /// <returns>True if any tokens were successfully assigned</returns>
    bool TryAssignCandidateTokens(CandidateTokenList candidates, object? targetObject, StringBuilder replacement,
        TokenizerOptions options, FileLocation location, TokenizeResultBase result, Template template, HashSet<int> matchIds, IDiagnosticCollector collector);

    /// <summary>
    /// Processes front matter tokens that don't require input text matching.
    /// </summary>
    /// <param name="template">The template containing front matter token definitions</param>
    /// <param name="targetObject">The object to populate with front matter token values</param>
    /// <param name="location">The current file location</param>
    /// <param name="result">The result object to populate with matches</param>
    void ProcessFrontMatterTokens(Template template, object? targetObject, FileLocation location, TokenizeResultBase result, IDiagnosticCollector collector);

    /// <summary>
    /// Handles the processing of repeated tokens and manages disabled repeating tokens.
    /// </summary>
    /// <param name="candidates">The list of candidate tokens</param>
    /// <param name="enumerator">The token enumerator</param>
    /// <param name="replacement">The StringBuilder containing the token value</param>
    /// <param name="result">The result object</param>
    /// <param name="disabledRepeatingTokens">The set of disabled repeating token IDs</param>
    /// <param name="matchIds">The set of matched token IDs</param>
    /// <param name="template">The template containing token definitions</param>
    /// <returns>True if processing should continue, false if candidates were cleared</returns>
    bool ProcessRepeatedTokens(CandidateTokenList candidates, TokenEnumerator enumerator, StringBuilder replacement,
        TokenizeResultBase result, HashSet<int> disabledRepeatingTokens, HashSet<int> matchIds, Template template, IDiagnosticCollector collector);

    /// <summary>
    /// Handles newline-terminated token processing.
    /// </summary>
    /// <param name="candidates">The list of candidate tokens</param>
    /// <param name="targetObject">The object to populate with matched token values</param>
    /// <param name="replacement">The StringBuilder containing the token value</param>
    /// <param name="options">The tokenizer options</param>
    /// <param name="location">The current file location</param>
    /// <param name="result">The result object to populate with matches</param>
    /// <param name="template">The template containing token definitions</param>
    /// <param name="matchIds">The set of matched token IDs</param>
    /// <param name="enumerator">The token enumerator</param>
    /// <param name="disabledRepeatingTokens">The set of disabled repeating token IDs</param>
    void ProcessNewlineTerminatedTokens(CandidateTokenList candidates, object? targetObject, StringBuilder replacement,
        TokenizerOptions options, FileLocation location, TokenizeResultBase result, Template template,
        HashSet<int> matchIds, TokenEnumerator enumerator, HashSet<int> disabledRepeatingTokens, IDiagnosticCollector collector);
}
