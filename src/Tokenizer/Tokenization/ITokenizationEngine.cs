using Tokens.Diagnostics;

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
    /// <param name="targetObject">The object to populate with matched token values</param>
    /// <param name="context">The tokenization context containing shared state (must be initialized by the caller)</param>
    /// <param name="result">The result object to populate with matches and misses</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    /// <param name="hintStrategy">Optional hint strategy to notify when token preambles match.</param>
    void ProcessTokenization(
        Template template,
        object? targetObject,
        ITokenizationContext context,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);
}
