using System.IO;
using System.Text;
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
    private readonly IHintStrategy hintStrategy = new ContainsHintStrategy();

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
        // Safety limit: maximum input length
        if (template.Options.MaxInputLength > 0 && input.Length > template.Options.MaxInputLength)
        {
            throw new TokenizerException(
                $"Input length {input.Length:N0} exceeds maximum allowed length of {template.Options.MaxInputLength:N0}. " +
                "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
        }

        TokenizeCore(result, value, template, new StringReader(input), input);
    }

    /// <summary>
    /// Tokenizes the input from a <see cref="TextReader"/> using the provided compiled <paramref name="template"/>.
    /// The caller retains ownership of the reader; it is not disposed.
    /// </summary>
    public TokenizeResult Tokenize(Template template, TextReader input)
    {
        var result = new TokenizeResult(template);
        Tokenize(result, null, template, input);
        return result;
    }

    /// <summary>
    /// Tokenizes the input from a <see cref="TextReader"/> using the provided compiled <paramref name="template"/>,
    /// mapping extracted values onto a new instance of <typeparamref name="T"/>.
    /// The caller retains ownership of the reader; it is not disposed.
    /// </summary>
    public TokenizeResult<T> Tokenize<T>(Template template, TextReader input) where T : class, new()
    {
        var result = new TokenizeResult<T>(template);
        Tokenize(result, result.Value, template, input);
        return result;
    }

    /// <summary>
    /// Tokenizes the input from a <see cref="Stream"/> using the provided compiled <paramref name="template"/>.
    /// The stream is not disposed; it remains open for further use.
    /// </summary>
    public TokenizeResult Tokenize(Template template, Stream input, Encoding encoding)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return Tokenize(template, reader);
    }

    /// <summary>
    /// Tokenizes the input from a <see cref="Stream"/> using the provided compiled <paramref name="template"/>,
    /// mapping extracted values onto a new instance of <typeparamref name="T"/>.
    /// The stream is not disposed; it remains open for further use.
    /// </summary>
    public TokenizeResult<T> Tokenize<T>(Template template, Stream input, Encoding encoding) where T : class, new()
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return Tokenize<T>(template, reader);
    }

    private void Tokenize(TokenizeResultBase result, object? value, Template template, TextReader input)
    {
        TokenizeCore(result, value, template, input, rawInput: null);
    }

    /// <summary>
    /// Core tokenization logic shared by both the string and TextReader paths.
    /// </summary>
    /// <param name="result">The result to populate.</param>
    /// <param name="value">The target object to assign values to, or null.</param>
    /// <param name="template">The compiled template.</param>
    /// <param name="reader">The reader to tokenize from.</param>
    /// <param name="rawInput">
    /// The raw input string when tokenizing from a string, or null when tokenizing from a TextReader.
    /// Drives length-dependent features: hint pre-filtering, input-length-based iteration cap,
    /// alignment rendering in diagnostics.
    /// </param>
    private void TokenizeCore(TokenizeResultBase result, object? value, Template template, TextReader reader, string? rawInput)
    {
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
            log.LogInformation("Starting tokenization for template {TemplateName}", template.Name);
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
                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Tokenization context initialized");
                }

                IDiagnosticCollector collector = template.Options.EnableDiagnostics
                    ? new DiagnosticCollector(null, rawInput)
                    : NullDiagnosticCollector.Instance;

                // Process hints first (rawInput is null for TextReader inputs — hint pre-filtering requires the full string)
                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Processing hints");
                }
                var hintsMissing = hintStrategy.PreProcess(template, context.Enumerator, rawInput, result, collector);

                if (hintsMissing)
                {
                    log.LogWarning("Required hints are missing, skipping tokenization");
                }
                else
                {
                    if (log.IsEnabled(LogLevel.Trace))
                    {
                        log.LogTrace("Hints validated successfully, proceeding with tokenization");
                    }
                    // inputLength of 0 means "use default" when rawInput is unavailable
                    var inputLength = rawInput?.Length ?? 0;
                    tokenizationEngine.ProcessTokenization(template, inputLength, value, context, result, collector, hintStrategy);

                    if (hintStrategy.PostProcess(result))
                    {
                        log.LogWarning("Post-tokenization hint check failed");
                    }
                }

                // Build unmatched tokens collection
                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Building unmatched tokens collection");
                }
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
                    if (log.IsEnabled(LogLevel.Information))
                    {
                        log.LogInformation("{Verdict}", result.Diagnostics.Summary.Verdict);
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

            log.LogInformation("Tokenization {Result} for template {TemplateName}",
                result.Success ? "succeeded" : "failed", template.Name);
        }
    }

    /// <inheritdoc />
    public Template Compile(string pattern) => compilationCache.GetOrAdd(pattern, p => parser.Parse(p));

    /// <inheritdoc />
    public Template Compile(string pattern, string name) => compilationCache.GetOrAdd(pattern, p => parser.Parse(p, name));

    /// <inheritdoc />
    public Template Compile(TextReader reader) => parser.Parse(reader);

    /// <inheritdoc />
    public Template Compile(TextReader reader, string name) => parser.Parse(reader, name);

    /// <inheritdoc />
    public void ClearCompilationCache() => compilationCache.Clear();

}
