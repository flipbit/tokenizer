using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Active diagnostic collector that records events during tokenization.
/// Create one instance per tokenization call and pass it through the pipeline.
/// </summary>
internal sealed class TokenizationDiagnosticCollector : ITokenizationDiagnosticCollector
{
    private readonly DiagnosticResult _diagnostics;

    /// <summary>
    /// Initialises a collector for runtime tokenization.
    /// </summary>
    /// <param name="inputContent">The input text being tokenized.</param>
    /// <param name="outOfOrderTokens">Whether the template uses out-of-order token matching.</param>
    /// <param name="optionalTokenNames">Token names that are optional.</param>
    public TokenizationDiagnosticCollector(string? inputContent, bool outOfOrderTokens = false, HashSet<string>? optionalTokenNames = null)
    {
        _diagnostics = new DiagnosticResult(inputContent, outOfOrderTokens, optionalTokenNames);
    }

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public void Record(TokenizationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
        _diagnostics.AddEvent(new TokenizationEvent
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
}
