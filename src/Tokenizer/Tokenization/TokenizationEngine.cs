using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Thin orchestrator that validates inputs and creates tokenization sessions.
/// All tokenization logic lives in <see cref="TokenizationSession"/> and its sub-components.
/// </summary>
internal sealed class TokenizationEngine : ITokenizationEngine
{
    private readonly ILogger<TokenizationEngine> _log;

    public TokenizationEngine() : this(logger: null)
    {
    }

    public TokenizationEngine(ILogger<TokenizationEngine>? logger)
    {
        _log = logger ?? NullLogger<TokenizationEngine>.Instance;
    }

    public TokenizationSession CreateSession(
        Template template,
        TokenizeResult result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null)
    {
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        return new TokenizationSession(
            template, result, collector, hintStrategy, _log);
    }
}
