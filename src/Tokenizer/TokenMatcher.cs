using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens;

/// <summary>
/// Matcher class that can hold multiple <see cref="Template"/> objects, and use
/// the best match to populate an object from an input string.
/// </summary>
public sealed class TokenMatcher : ITokenMatcher
{
    private readonly ITokenizer _tokenizer;
    private readonly ILogger<TokenMatcher> _log;

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
    /// Initializes a new instance of <see cref="TokenMatcher"/> with the specified _tokenizer.
    /// </summary>
    /// <param name="tokenizer">The tokenizer to use for compiling templates and tokenizing input.</param>
    public TokenMatcher(ITokenizer tokenizer) : this(tokenizer, loggerFactory: null)
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

        _tokenizer = tokenizer;
        _log = loggerFactory.CreateLogger<TokenMatcher>();
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
        return Match(input, tags: null);
    }

    /// <summary>
    /// Matches the input string against all registered templates that have the specified tags, and returns the results.
    /// </summary>
    /// <param name="input">The input string to match.</param>
    /// <param name="tags">Tags used to filter which templates are considered. Pass <see langword="null"/> or an empty array to consider all templates.</param>
    /// <returns>A <see cref="TokenMatcherResult"/> containing results for each matched template, including the best match.</returns>
    public TokenMatcherResult Match(string input, string[]? tags)
    {
        var results = new TokenMatcherResult();
        return MatchCore(
            tags, results,
            template => _tokenizer.Tokenize(template, input),
            (r, result) => r.AddResult((TokenizeResult)result),
            r => r.BestMatch = r.GetBestMatch());
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
        return Match<T>(input, tags: null);
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
        var results = new TokenMatcherResult<T>();
        return MatchCore(
            tags, results,
            template => _tokenizer.Tokenize<T>(template, input),
            (r, result) => r.AddResult((TokenizeResult<T>)result),
            r => r.BestMatch = r.GetBestMatch());
    }

    /// <summary>
    /// Compiles and registers a template pattern string.
    /// The template name is derived from its front matter, if present.
    /// </summary>
    /// <param name="content">The raw template pattern string to compile.</param>
    /// <returns>This <see cref="ITokenMatcher"/> instance, to allow method chaining.</returns>
    public ITokenMatcher RegisterTemplate(string content)
    {
        var result = _tokenizer.Compile(content);
        Templates.Add(result.Template);
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
        var result = _tokenizer.Compile(content);
        result.Template.Name = name;
        Templates.Add(result.Template);
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

    private TResult MatchCore<TResult>(
        string[]? tags,
        TResult results,
        Func<Template, TokenizeResultBase> tokenize,
        Action<TResult, TokenizeResultBase> addResult,
        Action<TResult> assignBestMatch)
    {
        tags ??= Array.Empty<string>();

        foreach (var template in Templates)
        {
            if (!CheckTemplateTags(template, tags)) continue;

            try
            {
                var result = tokenize(template);
                addResult(results, result);
            }
            catch (Exception e)
            {
                var exception = new TokenMatcherException(e.Message, template, e);
                _log.LogError(e, "Error processing template: {TemplateName}", template.Name);
                throw exception;
            }
        }

        assignBestMatch(results);
        return results;
    }

    /// <inheritdoc />
    public async Task<ITokenMatcher> RegisterTemplateAsync(TextReader reader, CancellationToken ct = default)
    {
        var result = await _tokenizer.CompileAsync(reader, ct).ConfigureAwait(false);
        Templates.Add(result.Template);
        return this;
    }

    /// <inheritdoc />
    public async Task<ITokenMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, CancellationToken ct = default)
    {
        var result = await _tokenizer.CompileAsync(input, encoding, ct).ConfigureAwait(false);
        Templates.Add(result.Template);
        return this;
    }

    /// <inheritdoc />
    public Task<TokenMatcherResult> MatchAsync(TextReader input, CancellationToken ct = default)
        => MatchAsync(input, tags: null, ct);

    /// <inheritdoc />
    public async Task<TokenMatcherResult> MatchAsync(TextReader input, string[]? tags, CancellationToken ct = default)
    {
#if NETSTANDARD2_0
        using var stream = await BufferTextReaderAsync(input, ct).ConfigureAwait(false);
#else
        await using var stream = await BufferTextReaderAsync(input, ct).ConfigureAwait(false);
#endif
        return await MatchAsyncFromSeekableStream<TokenMatcherResult, TokenizeResult>(
            stream, Encoding.UTF8, tags, ct,
            (template, reader, token) => _tokenizer.TokenizeAsync(template, reader, token),
            () => new TokenMatcherResult(),
            (r, result) => r.AddResult(result),
            r => r.BestMatch = r.GetBestMatch()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<TokenMatcherResult<T>> MatchAsync<T>(TextReader input, CancellationToken ct = default) where T : class, new()
        => MatchAsync<T>(input, tags: null, ct);

    /// <inheritdoc />
    public async Task<TokenMatcherResult<T>> MatchAsync<T>(TextReader input, string[]? tags, CancellationToken ct = default) where T : class, new()
    {
#if NETSTANDARD2_0
        using var stream = await BufferTextReaderAsync(input, ct).ConfigureAwait(false);
#else
        await using var stream = await BufferTextReaderAsync(input, ct).ConfigureAwait(false);
#endif
        return await MatchAsyncFromSeekableStream<TokenMatcherResult<T>, TokenizeResult<T>>(
            stream, Encoding.UTF8, tags, ct,
            (template, reader, token) => _tokenizer.TokenizeAsync<T>(template, reader, token),
            () => new TokenMatcherResult<T>(),
            (r, result) => r.AddResult(result),
            r => r.BestMatch = r.GetBestMatch()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<TokenMatcherResult> MatchAsync(Stream input, Encoding encoding, CancellationToken ct = default)
        => MatchAsync(input, encoding, tags: null, ct);

    /// <inheritdoc />
    public async Task<TokenMatcherResult> MatchAsync(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default)
    {
        var seekable = await EnsureSeekableAsync(input, ct).ConfigureAwait(false);
        return await MatchAsyncFromSeekableStream<TokenMatcherResult, TokenizeResult>(
            seekable, encoding, tags, ct,
            (template, reader, token) => _tokenizer.TokenizeAsync(template, reader, token),
            () => new TokenMatcherResult(),
            (r, result) => r.AddResult(result),
            r => r.BestMatch = r.GetBestMatch()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<TokenMatcherResult<T>> MatchAsync<T>(Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new()
        => MatchAsync<T>(input, encoding, tags: null, ct);

    /// <inheritdoc />
    public async Task<TokenMatcherResult<T>> MatchAsync<T>(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default) where T : class, new()
    {
        var seekable = await EnsureSeekableAsync(input, ct).ConfigureAwait(false);
        return await MatchAsyncFromSeekableStream<TokenMatcherResult<T>, TokenizeResult<T>>(
            seekable, encoding, tags, ct,
            (template, reader, token) => _tokenizer.TokenizeAsync<T>(template, reader, token),
            () => new TokenMatcherResult<T>(),
            (r, result) => r.AddResult(result),
            r => r.BestMatch = r.GetBestMatch()).ConfigureAwait(false);
    }

    // AllowStreamBuffering is intentionally not checked here. Unlike Stream (which can be
    // seekable), TextReader has no seek concept — buffering into a MemoryStream is the only
    // way to support rewinding between multiple template matches.
    private async Task<MemoryStream> BufferTextReaderAsync(TextReader reader, CancellationToken ct)
    {
        var maxInputLength = _tokenizer.Options.MaxInputLength;
        long totalChars = 0;
        var buffer = new MemoryStream();
#if NETSTANDARD2_0
        using var writer = new StreamWriter(buffer, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
#else
        await using var writer = new StreamWriter(buffer, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
#endif
        var charBuf = new char[4096];
        int read;
        while ((read = await reader.ReadAsync(charBuf, 0, charBuf.Length).ConfigureAwait(false)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            totalChars += read;
            if (maxInputLength > 0 && totalChars > maxInputLength)
            {
#if NET8_0_OR_GREATER
                await buffer.DisposeAsync().ConfigureAwait(false);
#else
                buffer.Dispose();
#endif
                throw new TokenizerException(
                    $"Input exceeds MaxInputLength ({maxInputLength.ToInvariant()}) during TextReader buffering.");
            }
            await writer.WriteAsync(charBuf, 0, read).ConfigureAwait(false);
        }
#if NETSTANDARD2_0
        await writer.FlushAsync().ConfigureAwait(false);
#else
        await writer.FlushAsync(ct).ConfigureAwait(false);
#endif
        buffer.Position = 0;
        return buffer;
    }

    private async Task<Stream> EnsureSeekableAsync(Stream input, CancellationToken ct)
    {
        if (input.CanSeek) return input;

        if (!_tokenizer.Options.AllowStreamBuffering)
        {
            throw new TokenizerException(
                "Stream is not seekable. Provide a seekable stream or " +
                "set TokenizerOptions.AllowStreamBuffering = true to allow buffering into memory.");
        }

        var maxInputLength = _tokenizer.Options.MaxInputLength;
        var buffer = new MemoryStream();
        var copyBuf = new byte[81920];
        long totalBytes = 0;
        int read;
        while ((read = await input.ReadAsync(copyBuf, 0, copyBuf.Length, ct).ConfigureAwait(false)) > 0)
        {
            totalBytes += read;
            if (maxInputLength > 0 && totalBytes > maxInputLength)
            {
#if NET8_0_OR_GREATER
                await buffer.DisposeAsync().ConfigureAwait(false);
#else
                buffer.Dispose();
#endif
                throw new TokenizerException(
                    $"Input stream exceeds MaxInputLength ({maxInputLength.ToInvariant()}) during buffering.");
            }
            await buffer.WriteAsync(copyBuf, 0, read, ct).ConfigureAwait(false);
        }
        buffer.Position = 0;
        return buffer;
    }

    private async Task<TResult> MatchAsyncFromSeekableStream<TResult, TTokenizeResult>(
        Stream stream,
        Encoding encoding,
        string[]? tags,
        CancellationToken ct,
        Func<Template, TextReader, CancellationToken, Task<TTokenizeResult>> tokenizeAsync,
        Func<TResult> createResult,
        Action<TResult, TTokenizeResult> addResult,
        Action<TResult> assignBestMatch)
        where TTokenizeResult : TokenizeResultBase
    {
        tags ??= Array.Empty<string>();
        var results = createResult();
        var startPos = stream.Position;

        foreach (var template in Templates)
        {
            if (!CheckTemplateTags(template, tags)) continue;

            stream.Position = startPos;
            using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024, leaveOpen: true);

            try
            {
                var result = await tokenizeAsync(template, reader, ct).ConfigureAwait(false);
                addResult(results, result);
            }
            catch (Exception e)
            {
                var exception = new TokenMatcherException(e.Message, template, e);
                _log.LogError(e, "Error processing template: {TemplateName}", template.Name);
                throw exception;
            }
        }

        assignBestMatch(results);
        return results;
    }

    private static bool CheckTemplateTags(Template template, string[] tags)
    {
        // No tags specified, always match template
        if (tags.Length == 0) return true;

        // Check template has tags
        if (template.Tags.Any())
        {
            if (!template.HasTags(tags, out var missing))
            {
                return false;
            }

            return true;
        }

        return false;
    }
}
