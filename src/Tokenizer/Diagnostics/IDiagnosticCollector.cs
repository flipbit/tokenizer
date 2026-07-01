using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Collects diagnostic events during tokenization.
/// Implementations must be safe for single-threaded use within one tokenization call.
/// Created per-tokenization-call in Tokenizer.Tokenize(), passed to the engine
/// and down into Token.Assign() as a method parameter.
/// </summary>
public interface IDiagnosticCollector
{
    /// <summary>
    /// Records a diagnostic event. Implementations may discard the event
    /// (NullDiagnosticCollector) or store it (DiagnosticCollector).
    /// </summary>
    void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);

    /// <summary>
    /// Returns the collected diagnostics, or null if collection is disabled.
    /// </summary>
    TokenizationDiagnostics? GetResult();
}
