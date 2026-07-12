using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// No-op compilation diagnostic collector used when diagnostics are disabled.
/// All operations are discarded. Use <see cref="Instance"/> to avoid allocations.
/// </summary>
internal sealed class NullCompilationDiagnosticCollector : ICompilationDiagnosticCollector
{
    /// <summary>
    /// The singleton instance of the null collector.
    /// </summary>
    public static readonly NullCompilationDiagnosticCollector Instance = new();

    private NullCompilationDiagnosticCollector()
    {
    }

    /// <inheritdoc />
    public bool IsEnabled => false;

    /// <inheritdoc />
    public void Record(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
    }

    /// <inheritdoc />
    public CompilationDiagnostics? GetResult() => null;
}
