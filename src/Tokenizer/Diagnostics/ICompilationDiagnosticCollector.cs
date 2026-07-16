using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Collects diagnostic events during template compilation.
/// Implementations must be safe for single-threaded use within one compilation call.
/// </summary>
internal interface ICompilationDiagnosticCollector
{
    /// <summary>
    /// Returns true when this collector is actively recording events.
    /// Use this to guard expensive argument evaluation at call sites.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Records a compilation diagnostic event.
    /// </summary>
    public void Record(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);

    /// <summary>
    /// Returns the collected compilation diagnostics, or null if collection is disabled.
    /// </summary>
    public CompilationDiagnostics? GetResult();
}
