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
    /// </summary>
    public TokenizationSession CreateSession(
        Template template,
        TokenizeResult result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);
}
