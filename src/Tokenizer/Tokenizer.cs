using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Exceptions;
using Tokens.Tokenization;
using Tokens.Tokenization.Strategies;

namespace Tokens;

/// <summary>
/// Class that creates objects and populates their properties with values
/// from input strings
/// </summary>
public sealed class Tokenizer : ITokenizer
{
    private readonly TokenParser parser;
    private readonly ILogger<Tokenizer> log;
    private readonly ITokenizationEngine tokenizationEngine;
    private readonly IResultBuilder resultBuilder;
    private readonly TemplateCache compilationCache;

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
    public Tokenizer(TokenizerOptions options) : this(options, null)
    {
    }

    /// <summary>
    /// Creates a new Tokenizer with the specified options and logger factory.
    /// </summary>
    public Tokenizer(TokenizerOptions options, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        Options = options with { };
        log = loggerFactory.CreateLogger<Tokenizer>();
        parser = new TokenParser(Options, loggerFactory.CreateLogger<TokenParser>());
        tokenizationEngine = new TokenizationEngine(loggerFactory.CreateLogger<TokenizationEngine>());
        resultBuilder = new ResultBuilder(loggerFactory.CreateLogger<ResultBuilder>());
        compilationCache = new TemplateCache(Options.CompilationCacheMaxSize);
    }

    /// <summary>
    /// Internal constructor for dependency injection.
    /// </summary>
    internal Tokenizer(
        IOptions<TokenizerOptions> options,
        ILogger<Tokenizer> logger,
        TokenParser parser,
        ITokenizationEngine tokenizationEngine,
        IResultBuilder resultBuilder)
    {
        Options = options.Value with { };
        log = logger;
        this.parser = parser;
        this.tokenizationEngine = tokenizationEngine;
        this.resultBuilder = resultBuilder;
        compilationCache = new TemplateCache(Options.CompilationCacheMaxSize);
    }

    /// <summary>
    /// Parses the given <paramref name="template"/> pattern and tokenizes the <paramref name="input"/> string against it.
    /// </summary>
    /// <param name="template">The template pattern string to parse and match against.</param>
    /// <param name="input">The input text to extract values from.</param>
    /// <returns>A <see cref="TokenizeResult"/> containing the matched and unmatched tokens.</returns>
    public TokenizeResult Tokenize(string template, string input)
    {
        var t = Compile(template);

        return Tokenize(t, input);
    }

    /// <summary>
    /// Tokenizes the <paramref name="input"/> string using the provided compiled <paramref name="template"/>.
    /// </summary>
    /// <param name="template">The compiled template to match against.</param>
    /// <param name="input">The input text to extract values from.</param>
    /// <returns>A <see cref="TokenizeResult"/> containing the matched and unmatched tokens.</returns>
    public TokenizeResult Tokenize(Template template, string input)
    {
        var result = new TokenizeResult(template);

        Tokenize(result, null, template, input);

        return result;

    }

    /// <summary>
    /// Parses the given <paramref name="pattern"/> and tokenizes the <paramref name="input"/> string,
    /// mapping extracted values onto a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to populate with extracted values.</typeparam>
    /// <param name="pattern">The template pattern string to parse and match against.</param>
    /// <param name="input">The input text to extract values from.</param>
    /// <returns>A <see cref="TokenizeResult{T}"/> with the populated object and match details.</returns>
    public TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new()
    {
        var template = Compile(pattern);

        return Tokenize<T>(template, input);
    }

    /// <summary>
    /// Tokenizes the <paramref name="input"/> string using the provided compiled <paramref name="template"/>,
    /// mapping extracted values onto a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to populate with extracted values.</typeparam>
    /// <param name="template">The compiled template to match against.</param>
    /// <param name="input">The input text to extract values from.</param>
    /// <returns>A <see cref="TokenizeResult{T}"/> with the populated object and match details.</returns>
    public TokenizeResult<T> Tokenize<T>(Template template, string input) where T : class, new()
    {
        var result = new TokenizeResult<T>(template);

        Tokenize(result, result.Value, template, input);

        return result;
    }

    private void Tokenize(TokenizeResultBase result, object? value, Template template, string input)
    {
        // template.Options reflects merged instance + front matter overrides — intentionally
        // used instead of this.Options so per-template front matter settings take effect.
        if (template.Options.MaxInputLength > 0 && input.Length > template.Options.MaxInputLength)
        {
            throw new TokenizerException(
                $"Input length {input.Length:N0} exceeds maximum allowed length of {template.Options.MaxInputLength:N0}. " +
                "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
        }

        TokenizeCore(result, value, template, new StringReader(input), input);
    }

    /// <summary>
    /// Core tokenization logic.
    /// </summary>
    /// <param name="result">The result to populate.</param>
    /// <param name="value">The target object to assign values to, or null.</param>
    /// <param name="template">The compiled template.</param>
    /// <param name="reader">The reader to tokenize from.</param>
    /// <param name="rawInput">
    /// The raw input string. Drives length-dependent features: hint pre-filtering,
    /// input-length-based iteration cap, alignment rendering in diagnostics.
    /// </param>
    private void TokenizeCore(TokenizeResultBase result, object? value, Template template, TextReader reader, string? rawInput)
    {
        var hintStrategy = new ContainsHintStrategy();
        var scopeProperties = new Dictionary<string, object>
        {
            ["TemplateName"] = template.Name,
            ["TokenCount"] = template.Tokens.Count,
            ["Operation"] = "Tokenize"
        };

        if (rawInput != null)
        {
            scopeProperties["InputLength"] = rawInput.Length;
        }

        using (log.BeginScope(scopeProperties))
        {
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Starting tokenization for template {TemplateName}", template.Name);
            }
            if (log.IsEnabled(LogLevel.Debug))
            {
                if (rawInput != null)
                {
                    log.LogDebug("Template has {TokenCount} tokens, input length is {InputLength}",
                        template.Tokens.Count, rawInput.Length);
                }
                else
                {
                    log.LogDebug("Template has {TokenCount} tokens", template.Tokens.Count);
                }
            }

            // Create and initialize the tokenization context
            using (var context = new TokenizationContext())
            {
                context.Initialize(reader);

                IDiagnosticCollector collector = template.Options.EnableDiagnostics
                    ? new DiagnosticCollector(null, rawInput)
                    : NullDiagnosticCollector.Instance;

                // Process hints first — hint pre-filtering requires the full input string
                var hintsMissing = hintStrategy.PreProcess(template, context.Enumerator, rawInput, result, collector);

                if (hintsMissing)
                {
                    log.LogWarning("Required hints are missing, skipping tokenization");
                }
                else
                {
                    tokenizationEngine.ProcessTokenization(template, value, context, result, collector, hintStrategy);

                    if (hintStrategy.PostProcess(result))
                    {
                        log.LogWarning("Post-tokenization hint check failed");
                    }
                }

                // Build unmatched tokens collection
                resultBuilder.BuildUnmatchedTokens(template, result, collector);

                var requiredMissingCount = result.Tokens.Misses.Count(t => t.IsRequired);
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Tokenization complete: {MatchCount} matches, {MissCount} misses, {RequiredMissing} required missing",
                        result.Tokens.Matches.Count, result.Tokens.Misses.Count, requiredMissingCount);
                }

                if (requiredMissingCount > 0)
                {
                    log.LogWarning("{RequiredMissing} required tokens were missing", requiredMissingCount);
                }

                result.Diagnostics = collector.GetResult();

                if (result.Diagnostics != null)
                {
                    if (log.IsEnabled(LogLevel.Debug))
                    {
                        log.LogDebug("{Verdict}", result.Diagnostics.Summary.Verdict);
                    }
                    foreach (var issue in result.Diagnostics.Summary.Issues)
                    {
                        log.LogWarning("Token '{TokenName}': {Description}", issue.TokenName, issue.Description);
                        if (issue.Hint != null)
                        {
                            log.LogWarning("  → Hint: {Hint}", issue.Hint);
                        }
                    }
                    if (rawInput != null && log.IsEnabled(LogLevel.Debug))
                    {
                        log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
                    }
                }
            }

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Tokenization {Result} for template {TemplateName}",
                    result.Success ? "succeeded" : "failed", template.Name);
            }
        }
    }

    /// <inheritdoc />
    public Template Compile(string pattern) => compilationCache.GetOrAdd(pattern, p => parser.Parse(p));

    /// <inheritdoc />
    public Template Compile(string pattern, string name) => compilationCache.GetOrAdd(pattern, p => parser.Parse(p, name));

    /// <inheritdoc />
    public void ClearCompilationCache() => compilationCache.Clear();

    /// <inheritdoc />
    public async Task<Template> CompileAsync(TextReader reader, CancellationToken ct = default)
    {
        var content = await ReadToEndAsync(reader, ct, Options.MaxTemplateLength).ConfigureAwait(false);
        return parser.Parse(content);
    }

    /// <inheritdoc />
    public async Task<Template> CompileAsync(TextReader reader, string name, CancellationToken ct = default)
    {
        var content = await ReadToEndAsync(reader, ct, Options.MaxTemplateLength).ConfigureAwait(false);
        return parser.Parse(content, name);
    }

    /// <inheritdoc />
    public async Task<Template> CompileAsync(Stream input, Encoding encoding, CancellationToken ct = default)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await CompileAsync(reader, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Template> CompileAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await CompileAsync(reader, name, ct).ConfigureAwait(false);
    }

    private static async Task<string> ReadToEndAsync(TextReader reader, CancellationToken ct, int maxLength = 0)
    {
        var sb = new StringBuilder();
        var buffer = new char[4096];
        int read;
#if NET8_0_OR_GREATER
        while ((read = await reader.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false)) > 0)
#else
        while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
#endif
        {
            ct.ThrowIfCancellationRequested();
            sb.Append(buffer, 0, read);
            if (maxLength > 0 && sb.Length > maxLength)
            {
                throw new TokenizerException(
                    $"Template length {sb.Length:N0} exceeds maximum allowed length of {maxLength:N0}. " +
                    "Increase TokenizerOptions.MaxTemplateLength to allow larger templates.");
            }
        }
        return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<TokenizeResult> TokenizeAsync(Template template, TextReader input, CancellationToken ct = default)
    {
        var result = new TokenizeResult(template);
        await TokenizeAsyncCore(result, null, template, input, ct).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public async Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct = default) where T : class, new()
    {
        var result = new TokenizeResult<T>(template);
        await TokenizeAsyncCore(result, result.Value, template, input, ct).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public async Task<TokenizeResult> TokenizeAsync(Template template, Stream input, Encoding encoding, CancellationToken ct = default)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await TokenizeAsync(template, reader, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new()
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await TokenizeAsync<T>(template, reader, ct).ConfigureAwait(false);
    }

    // TokenizeAsyncCore intentionally diverges from TokenizeCore: async uses Begin/Continue/End
    // cooperative protocol with buffer refills, lacks rawInput for diagnostics alignment, and
    // adds cancellation-aware exception handling. These structural differences make shared
    // helper extraction awkward without introducing tangled abstractions.
    private async Task TokenizeAsyncCore(TokenizeResultBase result, object? value, Template template, TextReader reader, CancellationToken ct)
    {
        var hintStrategy = new IntegratedHintStrategy();
        var scopeProperties = new Dictionary<string, object>
        {
            ["TemplateName"] = template.Name,
            ["TokenCount"] = template.Tokens.Count,
            ["Operation"] = "TokenizeAsync"
        };

        using (log.BeginScope(scopeProperties))
        {
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Starting async tokenization for template {TemplateName}", template.Name);
            }
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Template has {TokenCount} tokens", template.Tokens.Count);
            }

            using var context = new TokenizationContext();
            context.Initialize(reader);

            IDiagnosticCollector collector = template.Options.EnableDiagnostics
                ? new DiagnosticCollector(null, null)
                : NullDiagnosticCollector.Instance;

            try
            {
                // Async path uses IntegratedHintStrategy directly — it tracks hints via
                // OnTokenMatched callbacks during single-pass tokenization, since the full
                // input string isn't available during streaming.
                var hintsMissing = hintStrategy.PreProcess(template, context.Enumerator, null, result, collector);

                if (hintsMissing)
                {
                    log.LogWarning("Required hints are missing, skipping tokenization");
                }
                else
                {
                    var continuation = tokenizationEngine.BeginTokenization(template, value, context, result, collector, hintStrategy);
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
                    while (!tokenizationEngine.ContinueTokenization(continuation, context, ct));
                    tokenizationEngine.EndTokenization(continuation, context);

                    if (hintStrategy.PostProcess(result))
                    {
                        log.LogWarning("Post-tokenization hint check failed");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                log.LogWarning("Async tokenization cancelled for template {TemplateName}", template.Name);
                throw;
            }
            catch (TokenizerException ex)
            {
                log.LogError(ex, "Async tokenization failed for template {TemplateName}: {Message}", template.Name, ex.Message);
                throw;
            }

            // Build unmatched tokens collection
            resultBuilder.BuildUnmatchedTokens(template, result, collector);

            var requiredMissingCount = result.Tokens.Misses.Count(t => t.IsRequired);
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Tokenization complete: {MatchCount} matches, {MissCount} misses, {RequiredMissing} required missing",
                    result.Tokens.Matches.Count, result.Tokens.Misses.Count, requiredMissingCount);
            }

            if (requiredMissingCount > 0)
            {
                log.LogWarning("{RequiredMissing} required tokens were missing", requiredMissingCount);
            }

            result.Diagnostics = collector.GetResult();

            if (result.Diagnostics != null)
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("{Verdict}", result.Diagnostics.Summary.Verdict);
                }
                foreach (var issue in result.Diagnostics.Summary.Issues)
                {
                    log.LogWarning("Token '{TokenName}': {Description}", issue.TokenName, issue.Description);
                    if (issue.Hint != null)
                    {
                        log.LogWarning("  → Hint: {Hint}", issue.Hint);
                    }
                }
            }

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Async tokenization {Result} for template {TemplateName}",
                    result.Success ? "succeeded" : "failed", template.Name);
            }
        }
    }

}
