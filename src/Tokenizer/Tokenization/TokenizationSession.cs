using Microsoft.Extensions.Logging;
using Tokens.Diagnostics;
using Tokens.Exceptions;

namespace Tokens.Tokenization;

/// <summary>
/// Coordinates a single tokenization run. Holds all session-scoped state and sub-components.
/// Provides <see cref="Run"/> and <see cref="RunAsync"/> entry points that share a single
/// <see cref="ProcessChunk"/> algorithm.
/// </summary>
internal sealed class TokenizationSession
{
    private readonly Template _template;
    private readonly object? _targetObject;
    private readonly TokenizeResultBase _result;
    private readonly IDiagnosticCollector _collector;
    private readonly TokenMatchRouter _router;
    private readonly CandidateProcessor _candidateProcessor;
    private readonly bool _hasExplicitLimit;
    private int _iterationCount;

    public TokenizationSession(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy,
        ILogger logger)
    {
        _template = template;
        _targetObject = targetObject;
        _result = result;
        _collector = collector;
        _hasExplicitLimit = _template.Options.MaxIterations > 0;

        _candidateProcessor = new CandidateProcessor(
            targetObject, result, template, collector, logger);
        _router = new TokenMatchRouter(
            template, _candidateProcessor, collector, hintStrategy);
    }

    /// <summary>
    /// Runs tokenization synchronously.
    /// </summary>
    public void Run(TokenizationContext context)
    {
        Initialize(context);

        do
        {
            context.Enumerator.FillBuffer();

            if (_template.Options.MaxInputLength > 0 &&
                context.Enumerator.TotalCharactersSeen > _template.Options.MaxInputLength)
            {
                throw new TokenizerException(
                    $"Input length exceeds maximum allowed length of {_template.Options.MaxInputLength:N0}. " +
                    "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
            }
        }
        while (!ProcessChunk(context, CancellationToken.None));

        Finalize(context);
    }

    /// <summary>
    /// Runs tokenization asynchronously with cooperative buffer refills.
    /// </summary>
    public async Task RunAsync(TokenizationContext context, CancellationToken ct)
    {
        Initialize(context);

        do
        {
            await context.Enumerator.FillBufferAsync(ct).ConfigureAwait(false);

            if (_template.Options.MaxInputLength > 0 &&
                context.Enumerator.TotalCharactersSeen > _template.Options.MaxInputLength)
            {
                throw new TokenizerException(
                    $"Input length exceeds maximum allowed length of {_template.Options.MaxInputLength:N0}. " +
                    "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
            }
        }
        while (!ProcessChunk(context, ct));

        Finalize(context);
    }

    private void Initialize(TokenizationContext context)
    {
        _collector.Record(DiagnosticEventType.TokenizationStarted,
            detail: $"Template: {_template.Name}, Tokens: {_template.Tokens.Count}");
        context.MatchBuffer.Clear();
        _iterationCount = 0;
    }

    /// <summary>
    /// Processes the current buffer contents. Returns true when input is fully consumed,
    /// false when the enumerator needs a buffer refill.
    /// </summary>
    private bool ProcessChunk(TokenizationContext context, CancellationToken ct)
    {
        while (!context.Enumerator.IsEmpty)
        {
            if (context.Enumerator.NeedsRefill)
                return false;

            ct.ThrowIfCancellationRequested();

            _iterationCount++;
            if (_hasExplicitLimit && _iterationCount > _template.Options.MaxIterations)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded maximum iteration count of {_template.Options.MaxIterations:N0}. " +
                    "This may indicate a problematic template pattern. " +
                    "Increase TokenizerOptions.MaxIterations to allow more iterations.");
            }

            if (!_hasExplicitLimit && _iterationCount > context.Enumerator.CharactersConsumed * 2 + 100)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded derived iteration limit (iterations: {_iterationCount:N0}, " +
                    $"characters consumed: {context.Enumerator.CharactersConsumed:N0}). " +
                    "This may indicate a problematic template pattern. " +
                    "Set TokenizerOptions.MaxIterations to override the automatic limit.");
            }

            _router.RouteNext(context);
        }

        return true;
    }

    private void Finalize(TokenizationContext context)
    {
        _candidateProcessor.ProcessRemaining(context);
        FrontMatterProcessor.Process(_template, _targetObject, _result, _collector, context.Enumerator.Location);
        _collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: $"Matches: {_result.Tokens.Matches.Count}, Misses: {_result.Tokens.Misses.Count}");
    }
}
