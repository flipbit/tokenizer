using System.Threading;
using System.Threading.Tasks;
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
    private readonly Template template;
    private readonly object? targetObject;
    private readonly TokenizeResultBase result;
    private readonly IDiagnosticCollector collector;
    private readonly TokenMatchRouter router;
    private readonly CandidateProcessor candidateProcessor;
    private readonly bool hasExplicitLimit;
    private int iterationCount;

    public TokenizationSession(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy,
        ILogger logger)
    {
        this.template = template;
        this.targetObject = targetObject;
        this.result = result;
        this.collector = collector;
        this.hasExplicitLimit = template.Options.MaxIterations > 0;

        candidateProcessor = new CandidateProcessor(
            targetObject, result, template, collector, logger);
        router = new TokenMatchRouter(
            template, candidateProcessor, collector, hintStrategy);
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

            if (template.Options.MaxInputLength > 0 &&
                context.Enumerator.TotalCharactersSeen > template.Options.MaxInputLength)
            {
                throw new TokenizerException(
                    $"Input length exceeds maximum allowed length of {template.Options.MaxInputLength:N0}. " +
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

            if (template.Options.MaxInputLength > 0 &&
                context.Enumerator.TotalCharactersSeen > template.Options.MaxInputLength)
            {
                throw new TokenizerException(
                    $"Input length exceeds maximum allowed length of {template.Options.MaxInputLength:N0}. " +
                    "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
            }
        }
        while (!ProcessChunk(context, ct));

        Finalize(context);
    }

    private void Initialize(TokenizationContext context)
    {
        collector.Record(DiagnosticEventType.TokenizationStarted,
            detail: $"Template: {template.Name}, Tokens: {template.Tokens.Count}");
        context.MatchBuffer.Clear();
        iterationCount = 0;
    }

    /// <summary>
    /// Processes the current buffer contents. Returns true when input is fully consumed,
    /// false when the enumerator needs a buffer refill.
    /// </summary>
    private bool ProcessChunk(TokenizationContext context, CancellationToken ct)
    {
        while (context.Enumerator.IsEmpty == false)
        {
            if (context.Enumerator.NeedsRefill)
                return false;

            ct.ThrowIfCancellationRequested();

            iterationCount++;
            if (hasExplicitLimit && iterationCount > template.Options.MaxIterations)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded maximum iteration count of {template.Options.MaxIterations:N0}. " +
                    "This may indicate a problematic template pattern. " +
                    "Increase TokenizerOptions.MaxIterations to allow more iterations.");
            }

            if (!hasExplicitLimit && iterationCount > context.Enumerator.CharactersConsumed * 2 + 100)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded derived iteration limit (iterations: {iterationCount:N0}, " +
                    $"characters consumed: {context.Enumerator.CharactersConsumed:N0}). " +
                    "This may indicate a problematic template pattern. " +
                    "Set TokenizerOptions.MaxIterations to override the automatic limit.");
            }

            router.RouteNext(context);
        }

        return true;
    }

    private void Finalize(TokenizationContext context)
    {
        candidateProcessor.ProcessRemaining(context);
        FrontMatterProcessor.Process(template, targetObject, result, collector, context.Enumerator.Location);
        collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: $"Matches: {result.Tokens.Matches.Count}, Misses: {result.Tokens.Misses.Count}");
    }
}
