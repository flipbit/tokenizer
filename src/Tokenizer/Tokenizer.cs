using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Tokenization;
using Tokens.Tokenization.Strategies;

namespace Tokens;

/// <summary>
/// Class that creates objects and populates their properties with values
/// from input strings
/// </summary>
public sealed class Tokenizer : ITokenizer
{
    private readonly TemplateCompiler _compiler;
    private readonly ILogger<Tokenizer> _log;
    private readonly ITokenizationEngine _tokenizationEngine;
    private readonly IResultBuilder _resultBuilder;

    /// <summary>Gets the options.</summary>
    public TokenizerOptions Options { get; }

    /// <summary>
    /// Creates a new Tokenizer with default options.
    /// </summary>
    public Tokenizer() : this(new TokenizerOptions())
    {
    }

    /// <summary>
    /// Creates a new Tokenizer with the specified options.
    /// </summary>
    public Tokenizer(TokenizerOptions options) : this(options, loggerFactory: null)
    {
    }

    /// <summary>
    /// Creates a new Tokenizer with the specified options and logger factory.
    /// </summary>
    public Tokenizer(TokenizerOptions options, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        Options = options with { };
        _log = loggerFactory.CreateLogger<Tokenizer>();
        _compiler = new TemplateCompiler(Options, loggerFactory);
        _tokenizationEngine = new TokenizationEngine(loggerFactory.CreateLogger<TokenizationEngine>());
        _resultBuilder = new ResultBuilder(loggerFactory.CreateLogger<ResultBuilder>());
    }

    /// <summary>
    /// Internal constructor for dependency injection.
    /// </summary>
    internal Tokenizer(
        IOptions<TokenizerOptions> options,
        ILogger<Tokenizer> logger,
        TemplateCompiler parser,
        ITokenizationEngine tokenizationEngine,
        IResultBuilder resultBuilder)
    {
        Options = options.Value with { };
        _log = logger;
        _compiler = parser;
        _tokenizationEngine = tokenizationEngine;
        _resultBuilder = resultBuilder;
    }

    /// <inheritdoc />
    public CompilationResult Compile(string pattern) => _compiler.Compile(pattern);

    /// <inheritdoc />
    public async Task<CompilationResult> CompileAsync(TextReader reader, CancellationToken ct = default)
    {
        var content = await reader.ReadToEndBoundedAsync(Options.MaxTemplateLength, ct).ConfigureAwait(false);
        return _compiler.Compile(content);
    }

    /// <inheritdoc />
    public async Task<CompilationResult> CompileAsync(Stream input, Encoding encoding, CancellationToken ct = default)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await CompileAsync(reader, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Tokenizes the <paramref name="input"/> string using the provided compiled <paramref name="template"/>.
    /// </summary>
    /// <param name="template">The compiled template to match against.</param>
    /// <param name="input">The input text to extract values from.</param>
    /// <returns>A <see cref="TokenizeResult"/> containing the matched and unmatched tokens.</returns>
    public TokenizeResult Tokenize(Template template, string input)
    {
        return Tokenize(template, input, CancellationToken.None);
    }

    /// <summary>
    /// Tokenizes the <paramref name="input"/> string using the provided compiled <paramref name="template"/>,
    /// mapping extracted values onto a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to populate with extracted values.</typeparam>
    /// <param name="template">The compiled template to match against.</param>
    /// <param name="input">The input text to extract values from.</param>
    /// <returns>A new instance of <typeparamref name="T"/> with populated properties, or null if matching fails.</returns>
    public T? Tokenize<T>(Template template, string input) where T : class, new()
    {
        var result = Tokenize(template, input);
        if (!result.Success) return null;
        return result.Assign<T>();
    }

    /// <summary>
    /// Tokenizes the <paramref name="input"/> string using the provided compiled <paramref name="template"/>
    /// with cancellation support.
    /// </summary>
    public TokenizeResult Tokenize(Template template, string input, CancellationToken cancellationToken)
    {
        var result = new TokenizeResult(template);

        // template.Options reflects merged instance + front matter overrides — intentionally
        // used instead of this.Options so per-template front matter settings take effect.
        if (template.Options.MaxInputLength > 0 && input.Length > template.Options.MaxInputLength)
        {
            throw new TokenizerException(
                $"Input length {input.Length.ToInvariant("N0")} exceeds maximum allowed length of {template.Options.MaxInputLength.ToInvariant("N0")}. " +
                "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
        }

        RunCoreAsync(result, template, new StringReader(input), input, cancellationToken)
            .GetAwaiter().GetResult();

        return result;
    }

    /// <summary>
    /// Tokenizes the <paramref name="input"/> string using the provided compiled <paramref name="template"/>
    /// with cancellation support, mapping extracted values onto a new instance of <typeparamref name="T"/>.
    /// </summary>
    public T? Tokenize<T>(Template template, string input, CancellationToken cancellationToken) where T : class, new()
    {
        var result = Tokenize(template, input, cancellationToken);
        if (!result.Success) return null;
        return result.Assign<T>();
    }

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/> using a pre-compiled template.
    /// </summary>
    /// <remarks>
    /// Hint matching in streaming mode scans buffer contents incrementally rather than
    /// searching the full input. Alignment rendering in diagnostics is unavailable.
    /// </remarks>
    public async Task<TokenizeResult> TokenizeAsync(Template template, TextReader input, CancellationToken ct = default)
    {
        var result = new TokenizeResult(template);
        await RunCoreAsync(result, template, input, rawInput: null, ct).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/>, mapping values onto a new <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Hint matching in streaming mode scans buffer contents incrementally rather than
    /// searching the full input. Alignment rendering in diagnostics is unavailable.
    /// </remarks>
    public async Task<T?> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct = default) where T : class, new()
    {
        var result = await TokenizeAsync(template, input, ct).ConfigureAwait(false);
        if (!result.Success) return null;
        return result.Assign<T>();
    }

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/> using a pre-compiled template.
    /// </summary>
    /// <remarks>
    /// Hint matching in streaming mode scans buffer contents incrementally rather than
    /// searching the full input. Alignment rendering in diagnostics is unavailable.
    /// </remarks>
    public async Task<TokenizeResult> TokenizeAsync(Template template, Stream input, Encoding encoding, CancellationToken ct = default)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await TokenizeAsync(template, reader, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/>, mapping values onto a new <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Hint matching in streaming mode scans buffer contents incrementally rather than
    /// searching the full input. Alignment rendering in diagnostics is unavailable.
    /// </remarks>
    public async Task<T?> TokenizeAsync<T>(Template template, Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new()
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await TokenizeAsync<T>(template, reader, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Unified tokenization core. Handles both sync and async paths.
    /// Sync callers pass <paramref name="rawInput"/> (non-null) and the method completes synchronously.
    /// Async callers pass <paramref name="rawInput"/> as null and await the result.
    /// </summary>
    private async Task RunCoreAsync(
        TokenizeResult result, Template template, TextReader reader,
        string? rawInput, CancellationToken ct)
    {
        var isSync = rawInput != null;
        IHintStrategy hintStrategy = isSync
            ? UpfrontHintStrategy.Instance
            : new StreamingHintStrategy();

        var scopeProperties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["TemplateName"] = template.Name,
            ["TokenCount"] = template.Tokens.Count,
            ["Operation"] = isSync ? "Tokenize" : "TokenizeAsync",
        };

        if (rawInput != null)
        {
            scopeProperties["InputLength"] = rawInput.Length;
        }

        using (_log.BeginScope(scopeProperties))
        {
            ITokenizationDiagnosticCollector collector = template.Options.EnableDiagnostics
                ? new TokenizationDiagnosticCollector(
                    rawInput,
                    template.Options.OutOfOrderTokens,
                    new HashSet<string>(template.Tokens.Where(t => t.IsOptional).Select(t => t.Name), StringComparer.Ordinal))
                : NullTokenizationDiagnosticCollector.Instance;

            try
            {
                ct.ThrowIfCancellationRequested();

                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug("Starting tokenization for template {TemplateName}", template.Name);
                    if (rawInput != null)
                    {
                        _log.LogDebug("Template has {TokenCount} tokens, input length is {InputLength}",
                            template.Tokens.Count, rawInput.Length);
                    }
                    else
                    {
                        _log.LogDebug("Template has {TokenCount} tokens", template.Tokens.Count);
                    }
                }

                var context = new TokenizationContext();
                context.Initialize(reader);

                var hintsMissing = hintStrategy.PreProcess(template, context.Enumerator, rawInput, result, collector);

                // The enumerator's constructor pre-fills the first buffer; report it now
                // so StreamingHintStrategy can scan for hints in the initial chunk.
                // UpfrontHintStrategy no-ops this call.
                hintStrategy.OnBufferFilled(context.Enumerator.StagingBuffer, context.Enumerator.LastReadCount);

                if (hintsMissing)
                {
                    var missingHintNames = result.Hints.Misses
                        .Where(h => !h.Optional)
                        .Select(h => h.Text)
                        .ToArray();
                    _log.LogWarning("Required hints are missing, skipping tokenization: {MissingHints}", missingHintNames);
                }
                else
                {
                    var session = _tokenizationEngine.CreateSession(template, result, collector, hintStrategy);

                    if (isSync)
                    {
#pragma warning disable MA0042 // Intentionally calling sync Run — sync path never awaits
                        session.Run(context);
#pragma warning restore MA0042
                    }
                    else
                    {
                        await session.RunAsync(context, ct).ConfigureAwait(false);
                    }

                    if (hintStrategy.PostProcess(result))
                    {
                        var failedHintNames = result.Hints.Misses
                            .Where(h => !h.Optional)
                            .Select(h => h.Text)
                            .ToArray();
                        _log.LogWarning("Post-tokenization hint check failed: {MissingHints}", failedHintNames);
                    }
                }

                FinalizeTokenization(result, template, collector, rawInput);

                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug("Tokenization {Result} for template {TemplateName}",
                        result.Success ? "succeeded" : "failed", template.Name);
                }
            }
            catch (OperationCanceledException)
            {
                _log.LogWarning("Async tokenization cancelled for template {TemplateName}", template.Name);
                throw;
            }
            catch (TokenizerException ex)
            {
                _log.LogError(ex, "Tokenization failed for template {TemplateName}: {Message}",
                    template.Name, ex.Message);
                ex.Data["Diagnostics"] = collector.GetResult();
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error during tokenization for template {TemplateName}: {Message}",
                    template.Name, ex.Message);
                ex.Data["Diagnostics"] = collector.GetResult();
                throw;
            }
        }
    }

    private void FinalizeTokenization(
        TokenizeResult result, Template template,
        ITokenizationDiagnosticCollector collector, string? rawInput)
    {
        _resultBuilder.BuildUnmatchedTokens(template, result, collector);

        var requiredMissingCount = result.Tokens.Misses.Count(t => t.IsRequired);
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Tokenization complete: {MatchCount} matches, {MissCount} misses, {RequiredMissing} required missing",
                result.Tokens.Matches.Count, result.Tokens.Misses.Count, requiredMissingCount);
        }

        if (requiredMissingCount > 0)
        {
            _log.LogWarning("{RequiredMissing} required tokens were missing", requiredMissingCount);
        }

        result.Diagnostics = collector.GetResult();

        if (result.Diagnostics != null)
        {
            if (_log.IsEnabled(LogLevel.Warning) && result.Diagnostics.MissedCount > 0)
            {
                foreach (var token in result.Diagnostics.Tokens)
                {
                    if (token.Outcome == TokenOutcome.Matched)
                        continue;

                    foreach (var issue in token.Issues)
                    {
                        _log.LogWarning("[{IssueCode}] Token '{TokenName}': {Description}",
                            issue.Code, issue.TokenName, issue.Description);
                    }
                }
            }

            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("{Verdict}", result.Diagnostics.Verdict);
                foreach (var token in result.Diagnostics.Tokens)
                {
                    foreach (var issue in token.Issues)
                    {
                        if (issue.Hint != null)
                        {
                            _log.LogDebug("  → Hint for '{TokenName}': {Hint}", issue.TokenName, issue.Hint);
                        }
                    }
                }
                if (rawInput != null)
                {
                    _log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
                }
            }
        }
    }
}
