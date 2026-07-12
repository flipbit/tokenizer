using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// No-op tokenization diagnostic collector used when diagnostics are disabled.
/// All operations are discarded. Use <see cref="Instance"/> to avoid allocations.
/// </summary>
internal sealed class NullTokenizationDiagnosticCollector : ITokenizationDiagnosticCollector
{
    /// <summary>
    /// The singleton instance of the null collector.
    /// </summary>
    public static readonly NullTokenizationDiagnosticCollector Instance = new();

    private NullTokenizationDiagnosticCollector()
    {
    }

    /// <inheritdoc />
    public bool IsEnabled => false;

    /// <inheritdoc />
    public void Record(TokenizationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
    }

    /// <inheritdoc />
    public TokenizationDiagnostics? GetResult() => null;
}
