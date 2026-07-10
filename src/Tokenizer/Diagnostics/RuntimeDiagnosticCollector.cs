using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Active diagnostic collector that records events during tokenization.
/// Create one instance per tokenization call and pass it through the pipeline.
/// </summary>
internal sealed class RuntimeDiagnosticCollector : IDiagnosticCollector
{
    private readonly DiagnosticResult _diagnostics;

    /// <summary>
    /// Initialises a collector for runtime tokenization.
    /// </summary>
    /// <param name="inputContent">The input text being tokenized.</param>
    public RuntimeDiagnosticCollector(string? inputContent)
    {
        _diagnostics = new DiagnosticResult(inputContent);
    }

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
        _diagnostics.AddEvent(new DiagnosticEvent
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
    public DiagnosticResult? GetResult() => _diagnostics;

    /// <inheritdoc />
    public CompilationDiagnostics? GetCompilationResult() => null;
}
