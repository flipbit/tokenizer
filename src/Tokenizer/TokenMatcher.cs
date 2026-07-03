using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Exceptions;

namespace Tokens;

/// <summary>
/// Matcher class that can hold multiple <see cref="Template"/> objects, and use
/// the best match to populate an object from an input string.
/// </summary>
public sealed class TokenMatcher : ITokenMatcher
{
    private readonly ITokenizer tokenizer;
    private readonly ILogger<TokenMatcher> log;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with default options.
    /// </summary>
    public TokenMatcher() : this(new TokenizerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with the specified options.
    /// </summary>
    /// <param name="options">The tokenizer options to apply during matching.</param>
    public TokenMatcher(TokenizerOptions options) : this(new Tokenizer(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with the specified options and logger factory.
    /// </summary>
    /// <param name="options">The tokenizer options to apply during matching.</param>
    /// <param name="loggerFactory">The logger factory to use for diagnostic output, or <see langword="null"/> to suppress logging.</param>
    public TokenMatcher(TokenizerOptions options, ILoggerFactory? loggerFactory)
        : this(new Tokenizer(options, loggerFactory), loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with the specified tokenizer.
    /// </summary>
    /// <param name="tokenizer">The tokenizer to use for compiling templates and tokenizing input.</param>
    public TokenMatcher(ITokenizer tokenizer) : this(tokenizer, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with the specified tokenizer and logger factory.
    /// </summary>
    /// <param name="tokenizer">The tokenizer to use for compiling templates and tokenizing input.</param>
    /// <param name="loggerFactory">The logger factory to use for diagnostic output, or <see langword="null"/> to suppress logging.</param>
    public TokenMatcher(ITokenizer tokenizer, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        this.tokenizer = tokenizer;
        log = loggerFactory.CreateLogger<TokenMatcher>();
        Templates = new TemplateCollection();
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

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Start: Matching: {TemplateName}", template.Name);
            }

            // Check template has tags
            if (CheckTemplateTags(template, tags) == false)
            {
                continue;
            }

            try
            {
                var result = tokenizer.Tokenize(template, input);

                results.AddResult(result);

                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Match Success: {Success}", result.Success);
                    log.LogTrace("Total Matches: {MatchCount}", result.Tokens.Matches.Count);
                    log.LogTrace("Total Errors : {ErrorCount}", result.Exceptions.Count);
                }

            }
            catch (Exception e)
            {
                var exception = new TokenMatcherException(e.Message, template, e);

                log.LogError(e, "Error processing template: {TemplateName}", template.Name);

                throw exception;
            }

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
            }
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

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Start: Matching: {TemplateName}", template.Name);
            }

            // Check template has tags
            if (CheckTemplateTags(template, tags) == false)
            {
                continue;
            }

            try
            {
                var result = tokenizer.Tokenize<T>(template, input);

                results.AddResult(result);

                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Match Success: {Success}", result.Success);
                    log.LogTrace("Total Matches: {MatchCount}", result.Tokens.Matches.Count);
                    log.LogTrace("Total Errors : {ErrorCount}", result.Exceptions.Count);
                }

            }
            catch (Exception e)
            {
                var exception = new TokenMatcherException(e.Message, template, e);

                log.LogError(e, "Error processing template: {TemplateName}", template.Name);

                throw exception;
            }

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
            }
        }

        // Assign best match
        results.BestMatch = results.GetBestMatch();

        return results;
    }

    /// <summary>
    /// Compiles and registers a template pattern string.
    /// The template name is derived from its front matter, if present.
    /// </summary>
    /// <param name="content">The raw template pattern string to compile.</param>
    /// <returns>This <see cref="ITokenMatcher"/> instance, to allow method chaining.</returns>
    public ITokenMatcher RegisterTemplate(string content)
    {
        var template = tokenizer.Compile(content);

        Templates.Add(template);

        return this;
    }

    /// <summary>
    /// Compiles and registers a template pattern string with the specified name.
    /// </summary>
    /// <param name="content">The raw template pattern string to compile.</param>
    /// <param name="name">The name to assign to the template.</param>
    /// <returns>This <see cref="ITokenMatcher"/> instance, to allow method chaining.</returns>
    public ITokenMatcher RegisterTemplate(string content, string name)
    {
        var template = tokenizer.Compile(content, name);

        Templates.Add(template);

        return this;
    }

    /// <summary>
    /// Compiles and registers a template from a <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">The reader containing the template pattern.</param>
    /// <returns>This <see cref="ITokenMatcher"/> instance, to allow method chaining.</returns>
    public ITokenMatcher RegisterTemplate(TextReader reader)
    {
        var template = tokenizer.Compile(reader);

        Templates.Add(template);

        return this;
    }

    /// <summary>
    /// Compiles and registers a template from a <see cref="TextReader"/> with the specified name.
    /// </summary>
    /// <param name="reader">The reader containing the template pattern.</param>
    /// <param name="name">The name to assign to the template.</param>
    /// <returns>This <see cref="ITokenMatcher"/> instance, to allow method chaining.</returns>
    public ITokenMatcher RegisterTemplate(TextReader reader, string name)
    {
        var template = tokenizer.Compile(reader, name);

        Templates.Add(template);

        return this;
    }

    /// <summary>
    /// Registers a pre-compiled template.
    /// </summary>
    /// <param name="template">The compiled template to register.</param>
    /// <returns>This <see cref="ITokenMatcher"/> instance, to allow method chaining.</returns>
    public ITokenMatcher RegisterTemplate(Template template)
    {
        Templates.Add(template);

        return this;
    }

    /// <inheritdoc />
    public TokenMatcherResult Match(TextReader input) => Match(input.ReadToEnd());

    /// <inheritdoc />
    public TokenMatcherResult Match(TextReader input, string[]? tags) => Match(input.ReadToEnd(), tags);

    /// <inheritdoc />
    public TokenMatcherResult<T> Match<T>(TextReader input) where T : class, new() => Match<T>(input.ReadToEnd());

    /// <inheritdoc />
    public TokenMatcherResult<T> Match<T>(TextReader input, string[]? tags) where T : class, new() => Match<T>(input.ReadToEnd(), tags);

    /// <inheritdoc />
    public TokenMatcherResult Match(Stream input, Encoding encoding)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return Match(reader);
    }

    /// <inheritdoc />
    public TokenMatcherResult Match(Stream input, Encoding encoding, string[]? tags)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return Match(reader, tags);
    }

    /// <inheritdoc />
    public TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding) where T : class, new()
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return Match<T>(reader);
    }

    /// <inheritdoc />
    public TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding, string[]? tags) where T : class, new()
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return Match<T>(reader, tags);
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
                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("No tags matching: {MissingTags}", missing);
                    log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
                }
                return false;
            }

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Found tag matching: {Tags}", string.Join(", ", tags));
            }
            return true;
        }

        return false;
    }
}
