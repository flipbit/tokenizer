using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Tokenization;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens;

/// <summary>
/// Class that creates objects and populates their properties with values
/// from input strings
/// </summary>
public sealed class Tokenizer
{
    private readonly TokenParser parser;
    private readonly ILogger<Tokenizer> log;
    private readonly ITokenizationEngine tokenizationEngine;
    private readonly IHintProcessor hintProcessor;
    private readonly IResultBuilder resultBuilder;

    /// <summary>Gets the options.</summary>
    public TokenizerOptions Options { get; }

    /// <summary>
    /// Internal constructor for dependency injection.
    /// </summary>
    internal Tokenizer(
        TokenizerOptions options,
        ILogger<Tokenizer> logger,
        TokenParser parser,
        ITokenizationEngine tokenizationEngine,
        IHintProcessor hintProcessor,
        IResultBuilder resultBuilder)
    {
        Options = options;
        log = logger;
        this.parser = parser;
        this.tokenizationEngine = tokenizationEngine;
        this.hintProcessor = hintProcessor;
        this.resultBuilder = resultBuilder;
    }

    /// <summary>
    /// Creates a new Tokenizer with default options.
    /// </summary>
    public static Tokenizer Create()
    {
        return Create(TokenizerOptions.Defaults, null);
    }

    /// <summary>
    /// Creates a new Tokenizer with the specified options.
    /// </summary>
    public static Tokenizer Create(TokenizerOptions options)
    {
        return Create(options, null);
    }

    /// <summary>
    /// Creates a new Tokenizer with the specified options and logger factory.
    /// </summary>
    public static Tokenizer Create(TokenizerOptions options, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        var logger = loggerFactory.CreateLogger<Tokenizer>();
        var parser = new TokenParser(options, loggerFactory.CreateLogger<TokenParser>());
        var tokenizationEngine = new TokenizationEngine(loggerFactory.CreateLogger<TokenizationEngine>());
        var hintProcessor = new HintProcessor(loggerFactory.CreateLogger<HintProcessor>());
        var resultBuilder = new ResultBuilder(loggerFactory.CreateLogger<ResultBuilder>());

        return new Tokenizer(options, logger, parser, tokenizationEngine, hintProcessor, resultBuilder);
    }

    public TokenizeResult Tokenize(string template, string input)
    {
        var t = parser.Parse(template);

        return Tokenize(t, input);
    }

    public TokenizeResult Tokenize(Template template, string input)
    {
        var result = new TokenizeResult(template);

        Tokenize(result, null, template, input);

        return result;

    }

    public TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new()
    {
        var template = parser.Parse(pattern);

        return Tokenize<T>(template, input);
    }

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

        using (log.BeginScope(new Dictionary<string, object>
        {
            ["TemplateName"] = template.Name,
            ["InputLength"] = input.Length,
            ["TokenCount"] = template.Tokens.Count,
            ["Operation"] = "Tokenize"
        }))
        {
            log.LogInformation("Starting tokenization for template {TemplateName}", template.Name);
            log.LogDebug("Template has {TokenCount} tokens, input length is {InputLength}",
                template.Tokens.Count, input.Length);

            // Create and initialize the tokenization context
            using (var context = new TokenizationContext())
            {
                context.Initialize(input);
                log.LogTrace("Tokenization context initialized");

                IDiagnosticCollector collector = template.Options.EnableDiagnostics
                    ? new DiagnosticCollector(template.Content, input)
                    : NullDiagnosticCollector.Instance;

                // Process hints first
                log.LogTrace("Processing hints");
                var hintsMissing = hintProcessor.FindAndValidateHints(template, context.Enumerator, result, collector);

                if (hintsMissing)
                {
                    log.LogWarning("Required hints are missing, skipping tokenization");
                }
                else
                {
                    log.LogTrace("Hints validated successfully, proceeding with tokenization");
                    // Process the main tokenization using the engine
                    tokenizationEngine.ProcessTokenization(template, input, value, context, result, collector);
                }

                // Build unmatched tokens collection
                log.LogTrace("Building unmatched tokens collection");
                resultBuilder.BuildUnmatchedTokens(template, result, collector);

                var requiredMissingCount = result.Tokens.Misses.Count(t => t.Required);
                log.LogDebug("Tokenization complete: {MatchCount} matches, {MissCount} misses, {RequiredMissing} required missing",
                    result.Tokens.Matches.Count, result.Tokens.Misses.Count, requiredMissingCount);

                if (requiredMissingCount > 0)
                {
                    log.LogWarning("{RequiredMissing} required tokens were missing", requiredMissingCount);
                }

                result.Diagnostics = collector.GetResult();

                if (result.Diagnostics != null)
                {
                    log.LogInformation("{Verdict}", result.Diagnostics.Summary.Verdict);
                    foreach (var issue in result.Diagnostics.Summary.Issues)
                    {
                        log.LogWarning("Token '{TokenName}': {Description}", issue.TokenName, issue.Description);
                        if (issue.Hint != null)
                        {
                            log.LogWarning("  → Hint: {Hint}", issue.Hint);
                        }
                    }
                    log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
                }
            }

            log.LogInformation("Tokenization {Result} for template {TemplateName}",
                result.Success ? "succeeded" : "failed", template.Name);
        }
    }

    public Tokenizer RegisterTransformer<T>() where T : ITokenTransformer
    {
        parser.RegisterTransformer<T>();

        return this;
    }

    public Tokenizer RegisterValidator<T>() where T : ITokenValidator
    {
        parser.RegisterValidator<T>();

        return this;
    }

}
