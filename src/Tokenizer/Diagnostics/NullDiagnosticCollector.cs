using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// No-op diagnostic collector used when diagnostics are disabled.
/// All operations are discarded. Use <see cref="Instance"/> to avoid allocations.
/// </summary>
internal sealed class NullDiagnosticCollector : IDiagnosticCollector
{
    /// <summary>
    /// The singleton instance of the null collector.
    /// </summary>
    public static readonly NullDiagnosticCollector Instance = new NullDiagnosticCollector();

    private NullDiagnosticCollector()
    {
    }

    /// <inheritdoc />
    public bool IsEnabled => false;

    /// <inheritdoc />
    public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
    }

    /// <inheritdoc />
    public DiagnosticResult? GetResult()
    {
        return null;
    }

    /// <inheritdoc />
    public CompilationDiagnostics? GetCompilationResult() => null;
}
