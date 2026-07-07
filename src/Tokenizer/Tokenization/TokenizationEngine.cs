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
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null)
    {
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        ValidateTargetObject(targetObject);

        return new TokenizationSession(
            template, targetObject, result, collector, hintStrategy, _log);
    }

    private void ValidateTargetObject(object? targetObject)
    {
        if (targetObject == null || targetObject is IDictionary<string, object>)
        {
            return;
        }

        var properties = targetObject.GetType().GetProperties();
        var hasSettableProperty = properties.Any(p => p.CanWrite && p.GetSetMethod() != null);

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Target object type: {TypeName}, Properties: {PropertyCount}, Settable: {SettableCount}",
                targetObject.GetType().Name,
                properties.Length,
                properties.Count(p => p.CanWrite && p.GetSetMethod() != null));
        }

        if (!hasSettableProperty)
        {
            throw new ArgumentException(
                $"Target object of type '{targetObject.GetType().Name}' has no settable properties. " +
                "Anonymous types and objects with read-only properties cannot be used as tokenization targets. " +
                "Consider using a class with writable properties or passing null as the target.",
                nameof(targetObject));
        }
    }
}
