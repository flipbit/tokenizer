using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Active diagnostic collector that records all events during a tokenization call.
/// Create one instance per tokenization call and pass it through the pipeline.
/// </summary>
internal sealed class DiagnosticCollector : IDiagnosticCollector
{
    private readonly DiagnosticResult? _diagnostics;
    private readonly CompilationDiagnostics? _compilationDiagnostics;

    /// <summary>
    /// Initialises a collector for runtime tokenization.
    /// </summary>
    /// <param name="inputContent">The input text being tokenized.</param>
    public DiagnosticCollector(string? inputContent)
    {
        _diagnostics = new DiagnosticResult(inputContent);
    }

    /// <summary>
    /// Initialises a collector for compilation.
    /// </summary>
    public DiagnosticCollector()
    {
        _compilationDiagnostics = new CompilationDiagnostics();
    }

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
        var evt = new DiagnosticEvent
        {
            Type = type,
            TokenName = tokenName,
            TokenId = tokenId,
            Location = location?.Clone(),
            Value = value,
            Detail = detail,
            DecoratorName = decoratorName,
            DecoratorArgs = decoratorArgs,
        };

        if (_diagnostics != null)
            _diagnostics.AddEvent(evt);
        else
            _compilationDiagnostics!.AddEvent(evt);
    }

    /// <inheritdoc />
    public DiagnosticResult? GetResult() => _diagnostics;

    /// <inheritdoc />
    public CompilationDiagnostics? GetCompilationResult() => _compilationDiagnostics;
}
