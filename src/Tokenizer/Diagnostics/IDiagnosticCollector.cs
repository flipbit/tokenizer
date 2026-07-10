using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Collects diagnostic events during tokenization.
/// Implementations must be safe for single-threaded use within one tokenization call.
/// Created per-tokenization-call in Tokenizer.Tokenize(), passed to the engine
/// and down into Token.Assign() as a method parameter.
/// </summary>
internal interface IDiagnosticCollector
{
    /// <summary>
    /// Returns true when this collector is actively recording events.
    /// Use this to guard expensive argument evaluation at call sites.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Records a diagnostic event. Implementations may discard the event
    /// (NullDiagnosticCollector) or store it (DiagnosticCollector).
    /// </summary>
    public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);

    /// <summary>
    /// Returns the collected diagnostics, or null if collection is disabled.
    /// </summary>
    public DiagnosticResult? GetResult();

    /// <summary>
    /// Returns the collected compilation diagnostics, or null if collection is disabled.
    /// </summary>
    public CompilationDiagnostics? GetCompilationResult();
}
