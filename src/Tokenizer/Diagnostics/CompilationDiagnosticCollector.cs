using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Active diagnostic collector that records events during template compilation.
/// Create one instance per compilation call and pass it through the pipeline.
/// </summary>
internal sealed class CompilationDiagnosticCollector : ICompilationDiagnosticCollector
{
    private readonly CompilationDiagnostics _compilationDiagnostics;

    /// <summary>
    /// Initialises a collector for template compilation.
    /// </summary>
    public CompilationDiagnosticCollector()
    {
        _compilationDiagnostics = new CompilationDiagnostics();
    }

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public void Record(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
        _compilationDiagnostics.AddEvent(new CompilationEvent
        {
            Type = type,
            TokenName = tokenName,
            TokenId = tokenId,
            Location = location?.Clone(),
            Value = value,
            Detail = detail,
            DecoratorName = decoratorName,
            DecoratorArgs = decoratorArgs,
        });
    }

    /// <inheritdoc />
    public CompilationDiagnostics? GetResult() => _compilationDiagnostics;
}
