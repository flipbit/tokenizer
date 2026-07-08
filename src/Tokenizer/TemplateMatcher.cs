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
public sealed class TemplateMatcher : ITemplateMatcher
{
    private readonly ITokenizer _tokenizer;
    private readonly ILogger<TemplateMatcher> _log;

    /// <summary>
    /// Initializes a new instance of <see cref="TemplateMatcher"/> with default options.
    /// </summary>
    public TemplateMatcher() : this(new TokenizerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TemplateMatcher"/> with the specified options.
    /// </summary>
    /// <param name="options">The tokenizer options to apply during matching.</param>
    public TemplateMatcher(TokenizerOptions options) : this(new Tokenizer(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TemplateMatcher"/> with the specified options and logger factory.
    /// </summary>
    /// <param name="options">The tokenizer options to apply during matching.</param>
    /// <param name="loggerFactory">The logger factory to use for diagnostic output, or <see langword="null"/> to suppress logging.</param>
    public TemplateMatcher(TokenizerOptions options, ILoggerFactory? loggerFactory)
        : this(new Tokenizer(options, loggerFactory), loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TemplateMatcher"/> with the specified tokenizer.
    /// </summary>
    /// <param name="tokenizer">The tokenizer to use for compiling templates and tokenizing input.</param>
    public TemplateMatcher(ITokenizer tokenizer) : this(tokenizer, loggerFactory: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TemplateMatcher"/> with the specified tokenizer and logger factory.
    /// </summary>
    /// <param name="tokenizer">The tokenizer to use for compiling templates and tokenizing input.</param>
    /// <param name="loggerFactory">The logger factory to use for diagnostic output, or <see langword="null"/> to suppress logging.</param>
    public TemplateMatcher(ITokenizer tokenizer, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        _tokenizer = tokenizer;
        _log = loggerFactory.CreateLogger<TemplateMatcher>();
        Templates = new TemplateCollection();
    }

    /// <summary>
    /// The collection of templates that will be matched against input strings.
    /// </summary>
    public TemplateCollection Templates { get; }

    /// <summary>
    /// Tokenizes the input against all registered templates and returns the results.
    /// </summary>
    /// <param name="input">The input string to tokenize.</param>
    /// <returns>A <see cref="TemplateMatchResult"/> containing results for each template, including the best match.</returns>
    public TemplateMatchResult Tokenize(string input)
    {
        return Tokenize(input, tags: null);
    }

    /// <summary>
    /// Tokenizes the input against all registered templates that have the specified tags, and returns the results.
    /// </summary>
    /// <param name="input">The input string to tokenize.</param>
    /// <param name="tags">Tags used to filter which templates are considered. Pass <see langword="null"/> or an empty array to consider all templates.</param>
    /// <returns>A <see cref="TemplateMatchResult"/> containing results for each matched template, including the best match.</returns>
    public TemplateMatchResult Tokenize(string input, string[]? tags)
    {
        var results = new TemplateMatchResult();
        tags ??= Array.Empty<string>();

        foreach (var template in Templates)
        {
            if (!CheckTemplateTags(template, tags)) continue;

            try
            {
                var result = _tokenizer.Tokenize(template, input);
                results.AddResult(result);
            }
            catch (Exception e)
            {
                var exception = new TemplateMatcherException(e.Message, template, e);
                _log.LogError(e, "Error processing template: {TemplateName}", template.Name);
                throw exception;
            }
        }

        results.BestMatch = results.GetBestMatch();
        return results;
    }

    /// <inheritdoc />
    public T? Tokenize<T>(string input) where T : class, new()
    {
        return Tokenize<T>(input, tags: null);
    }

    /// <inheritdoc />
    public T? Tokenize<T>(string input, string[]? tags) where T : class, new()
    {
        var results = Tokenize(input, tags);
        if (results.BestMatch == null) return null;
        return results.BestMatch.Assign<T>();
    }

    /// <summary>
    /// Compiles and registers a template pattern string.
    /// The template name is derived from its front matter, if present.
    /// </summary>
    /// <param name="content">The raw template pattern string to compile.</param>
    /// <returns>This <see cref="ITemplateMatcher"/> instance, to allow method chaining.</returns>
    public ITemplateMatcher RegisterTemplate(string content)
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
    /// <returns>This <see cref="ITemplateMatcher"/> instance, to allow method chaining.</returns>
    public ITemplateMatcher RegisterTemplate(string content, string name)
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
    /// <returns>This <see cref="ITemplateMatcher"/> instance, to allow method chaining.</returns>
    public ITemplateMatcher RegisterTemplate(Template template)
    {
        Templates.Add(template);

        return this;
    }

    /// <inheritdoc />
    public async Task<ITemplateMatcher> RegisterTemplateAsync(TextReader reader, CancellationToken ct = default)
    {
        var result = await _tokenizer.CompileAsync(reader, ct).ConfigureAwait(false);
        Templates.Add(result.Template);
        return this;
    }

    /// <inheritdoc />
    public async Task<ITemplateMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, CancellationToken ct = default)
    {
        var result = await _tokenizer.CompileAsync(input, encoding, ct).ConfigureAwait(false);
        Templates.Add(result.Template);
        return this;
    }

    /// <inheritdoc />
    public Task<TemplateMatchResult> TokenizeAsync(TextReader input, CancellationToken ct = default)
        => TokenizeAsync(input, tags: null, ct);

    /// <inheritdoc />
    public async Task<TemplateMatchResult> TokenizeAsync(TextReader input, string[]? tags, CancellationToken ct = default)
    {
#if NETSTANDARD2_0
        using var stream = await BufferTextReaderAsync(input, ct).ConfigureAwait(false);
#else
        await using var stream = await BufferTextReaderAsync(input, ct).ConfigureAwait(false);
#endif
        return await TokenizeAsyncFromSeekableStream(
            stream, Encoding.UTF8, tags, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<TemplateMatchResult> TokenizeAsync(Stream input, Encoding encoding, CancellationToken ct = default)
        => TokenizeAsync(input, encoding, tags: null, ct);

    /// <inheritdoc />
    public async Task<TemplateMatchResult> TokenizeAsync(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default)
    {
        var seekable = await EnsureSeekableAsync(input, ct).ConfigureAwait(false);
        return await TokenizeAsyncFromSeekableStream(
            seekable, encoding, tags, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T?> TokenizeAsync<T>(TextReader input, CancellationToken ct = default) where T : class, new()
        => await TokenizeAsync<T>(input, tags: null, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T?> TokenizeAsync<T>(TextReader input, string[]? tags, CancellationToken ct = default) where T : class, new()
    {
        var results = await TokenizeAsync(input, tags, ct).ConfigureAwait(false);
        if (results.BestMatch == null) return null;
        return results.BestMatch.Assign<T>();
    }

    /// <inheritdoc />
    public async Task<T?> TokenizeAsync<T>(Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new()
        => await TokenizeAsync<T>(input, encoding, tags: null, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T?> TokenizeAsync<T>(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default) where T : class, new()
    {
        var results = await TokenizeAsync(input, encoding, tags, ct).ConfigureAwait(false);
        if (results.BestMatch == null) return null;
        return results.BestMatch.Assign<T>();
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

    private async Task<TemplateMatchResult> TokenizeAsyncFromSeekableStream(
        Stream stream,
        Encoding encoding,
        string[]? tags,
        CancellationToken ct)
    {
        tags ??= Array.Empty<string>();
        var results = new TemplateMatchResult();
        var startPos = stream.Position;

        foreach (var template in Templates)
        {
            if (!CheckTemplateTags(template, tags)) continue;

            stream.Position = startPos;
            using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024, leaveOpen: true);

            try
            {
                var result = await _tokenizer.TokenizeAsync(template, reader, ct).ConfigureAwait(false);
                results.AddResult(result);
            }
            catch (Exception e)
            {
                var exception = new TemplateMatcherException(e.Message, template, e);
                _log.LogError(e, "Error processing template: {TemplateName}", template.Name);
                throw exception;
            }
        }

        results.BestMatch = results.GetBestMatch();
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
