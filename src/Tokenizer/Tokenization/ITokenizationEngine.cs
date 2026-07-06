using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Factory for creating tokenization sessions. Internal interface with a single
/// implementor, exposed for test substitution.
/// </summary>
internal interface ITokenizationEngine
{
    /// <summary>
    /// Creates a tokenization session that can be run synchronously or asynchronously.
    /// Validates the target object before returning.
    /// </summary>
    public TokenizationSession CreateSession(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);
}
