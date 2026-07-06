using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Thin orchestrator that validates inputs and creates tokenization sessions.
/// All tokenization logic lives in <see cref="TokenizationSession"/> and its sub-components.
/// </summary>
internal class TokenizationEngine : ITokenizationEngine
{
    private readonly ILogger<TokenizationEngine> log;

    public TokenizationEngine() : this(null)
    {
    }

    public TokenizationEngine(ILogger<TokenizationEngine>? logger)
    {
        log = logger ?? NullLogger<TokenizationEngine>.Instance;
    }

    public TokenizationSession CreateSession(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null)
    {
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        InputValidator.ValidateTargetObject(targetObject, log);

        return new TokenizationSession(
            template, targetObject, result, collector, hintStrategy, log);
    }
}
