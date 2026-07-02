using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Compilation;
using Tokens.Exceptions;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens;

/// <summary>
/// Matcher class that can hold multiple <see cref="Template"/> objects, and use
/// the best match to populate an object from an input string.
/// </summary>
public sealed class TokenMatcher
{
    private readonly Tokenizer tokenizer;
    private readonly TokenParser parser;
    private readonly ILogger<TokenMatcher> log;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with default options.
    /// </summary>
    public TokenMatcher() : this(new TokenizerOptions(), (ILoggerFactory?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with the specified options.
    /// </summary>
    /// <param name="options">The tokenizer options to apply during matching.</param>
    public TokenMatcher(TokenizerOptions options) : this(options, (ILoggerFactory?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with the specified options and logger factory.
    /// </summary>
    /// <param name="options">The tokenizer options to apply during matching.</param>
    /// <param name="loggerFactory">The logger factory to use for diagnostic output, or <see langword="null"/> to suppress logging.</param>
    public TokenMatcher(TokenizerOptions options, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        log = loggerFactory.CreateLogger<TokenMatcher>();
        parser = new TokenParser(options, loggerFactory.CreateLogger<TokenParser>());
        Templates = new TemplateCollection();
        tokenizer = new Tokenizer(options, loggerFactory);
    }

    /// <summary>
    /// The collection of templates that will be matched against input strings.
    /// </summary>
    public TemplateCollection Templates { get; }

    /// <summary>
    /// Matches the input string against all registered templates and returns the results.
    /// </summary>
    /// <param name="input">The input string to match.</param>
    /// <returns>A <see cref="TokenMatcherResult"/> containing results for each template, including the best match.</returns>
    public TokenMatcherResult Match(string input)
    {
        return Match(input, null);
    }

    /// <summary>
    /// Matches the input string against all registered templates that have the specified tags, and returns the results.
    /// </summary>
    /// <param name="input">The input string to match.</param>
    /// <param name="tags">Tags used to filter which templates are considered. Pass <see langword="null"/> or an empty array to consider all templates.</param>
    /// <returns>A <see cref="TokenMatcherResult"/> containing results for each matched template, including the best match.</returns>
    public TokenMatcherResult Match(string input, string[]? tags)
    {
        if (tags == null) tags = Array.Empty<string>();

        var results = new TokenMatcherResult();

        foreach (var name in Templates.Names)
        {
            if (!Templates.TryGet(name, out var template)) continue;

            log.LogTrace("Start: Matching: {TemplateName}", template.Name);

            // Check template has tags
            if (CheckTemplateTags(template, tags) == false)
            {
                continue;
            }

            try
            {
                var result = tokenizer.Tokenize(template, input);

                results.AddResult(result);

                log.LogTrace("Match Success: {Success}", result.Success);
                log.LogTrace("Total Matches: {MatchCount}", result.Tokens.Matches.Count);
                log.LogTrace("Total Errors : {ErrorCount}", result.Exceptions.Count);

            }
            catch (Exception e)
            {
                var exception = new TokenMatcherException(e.Message, template, e);

                log.LogError(e, "Error processing template: {TemplateName}", template.Name);

                throw exception;
            }

            log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
        }

        // Assign best match
        results.BestMatch = results.GetBestMatch();

        return results;

    }

    /// <summary>
    /// Matches the input string against all registered templates and populates a new instance of
    /// <typeparamref name="T"/> from the best match.
    /// </summary>
    /// <typeparam name="T">The type to populate from the matched tokens.</typeparam>
    /// <param name="input">The input string to match.</param>
    /// <returns>A <see cref="TokenMatcherResult{T}"/> containing typed results for each template, including the best match.</returns>
    public TokenMatcherResult<T> Match<T>(string input) where T : class, new()
    {
        return Match<T>(input, null);
    }

    /// <summary>
    /// Matches the input string against all registered templates that have the specified tags, and populates
    /// a new instance of <typeparamref name="T"/> from the best match.
    /// </summary>
    /// <typeparam name="T">The type to populate from the matched tokens.</typeparam>
    /// <param name="input">The input string to match.</param>
    /// <param name="tags">Tags used to filter which templates are considered. Pass <see langword="null"/> or an empty array to consider all templates.</param>
    /// <returns>A <see cref="TokenMatcherResult{T}"/> containing typed results for each matched template, including the best match.</returns>
    public TokenMatcherResult<T> Match<T>(string input, string[]? tags) where T : class, new()
    {
        if (tags == null) tags = Array.Empty<string>();

        var results = new TokenMatcherResult<T>();

        foreach (var name in Templates.Names)
        {
            if (!Templates.TryGet(name, out var template)) continue;

            log.LogTrace("Start: Matching: {TemplateName}", template.Name);

            // Check template has tags
            if (CheckTemplateTags(template, tags) == false)
            {
                continue;
            }

            try
            {
                var result = tokenizer.Tokenize<T>(template, input);

                results.AddResult(result);

                log.LogTrace("Match Success: {Success}", result.Success);
                log.LogTrace("Total Matches: {MatchCount}", result.Tokens.Matches.Count);
                log.LogTrace("Total Errors : {ErrorCount}", result.Exceptions.Count);

            }
            catch (Exception e)
            {
                var exception = new TokenMatcherException(e.Message, template, e);

                log.LogError(e, "Error processing template: {TemplateName}", template.Name);

                throw exception;
            }

            log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
        }

        // Assign best match
        results.BestMatch = results.GetBestMatch();

        return results;
    }

    /// <summary>
    /// Parses the given template content with the specified name and adds it to <see cref="Templates"/>.
    /// </summary>
    /// <param name="content">The raw template pattern string to parse.</param>
    /// <param name="name">The name to assign to the template.</param>
    /// <returns>This <see cref="TokenMatcher"/> instance, to allow method chaining.</returns>
    public TokenMatcher RegisterTemplate(string content, string name)
    {
        var template = parser.Parse(content, name);

        Templates.Add(template);

        return this;
    }

    /// <summary>
    /// Parses the given template content and adds it to <see cref="Templates"/>.
    /// The template name is derived from its front matter, if present.
    /// </summary>
    /// <param name="content">The raw template pattern string to parse.</param>
    /// <returns>This <see cref="TokenMatcher"/> instance, to allow method chaining.</returns>
    public TokenMatcher RegisterTemplate(string content)
    {
        var template = parser.Parse(content);

        Templates.Add(template);

        return this;
    }

    /// <summary>
    /// Registers a custom token transformer that can convert extracted values during tokenization.
    /// </summary>
    /// <typeparam name="T">The transformer type to register. Must implement <see cref="ITokenTransformer"/>.</typeparam>
    /// <returns>This <see cref="TokenMatcher"/> instance, to allow method chaining.</returns>
    public TokenMatcher RegisterTransformer<T>() where T : ITokenTransformer
    {
        parser.RegisterTransformer<T>();

        return this;
    }

    /// <summary>
    /// Registers a custom token validator that can accept or reject extracted values during tokenization.
    /// </summary>
    /// <typeparam name="T">The validator type to register. Must implement <see cref="ITokenValidator"/>.</typeparam>
    /// <returns>This <see cref="TokenMatcher"/> instance, to allow method chaining.</returns>
    public TokenMatcher RegisterValidator<T>() where T : ITokenValidator
    {
        parser.RegisterValidator<T>();

        return this;
    }

    private bool CheckTemplateTags(Template template, string[] tags)
    {
        // No tags specified, always match template
        if (tags.Length == 0) return true;

        // Check template has tags
        if (template.Tags.Any())
        {
            if (template.HasTags(tags, out var missing) == false)
            {
                log.LogTrace("No tags matching: {MissingTags}", missing);
                log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
                return false;
            }

            log.LogTrace("Found tag matching: {Tags}", string.Join(", ", tags));
            return true;
        }

        return false;
    }
}
