using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Collects diagnostic events during tokenization.
/// Implementations must be safe for single-threaded use within one tokenization call.
/// </summary>
internal interface ITokenizationDiagnosticCollector
{
    /// <summary>
    /// Returns true when this collector is actively recording events.
    /// Use this to guard expensive argument evaluation at call sites.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Records a tokenization diagnostic event.
    /// </summary>
    public void Record(TokenizationEventType type, string? tokenName = null, int? tokenId = null,
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);

    /// <summary>
    /// Returns the collected diagnostics, or null if collection is disabled.
    /// </summary>
    public DiagnosticResult? GetResult();
}
