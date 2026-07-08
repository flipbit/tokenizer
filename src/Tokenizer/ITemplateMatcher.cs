using System.Text;

namespace Tokens;

/// <summary>
/// Matches input text against multiple registered templates and returns the best match.
/// </summary>
public interface ITemplateMatcher
{
    /// <summary>
    /// The collection of templates that will be matched against input strings.
    /// </summary>
    public TemplateCollection Templates { get; }

    /// <summary>
    /// Compiles and registers a template pattern string.
    /// </summary>
    public ITemplateMatcher RegisterTemplate(string content);

    /// <summary>
    /// Compiles and registers a template pattern string with an explicit name.
    /// </summary>
    public ITemplateMatcher RegisterTemplate(string content, string name);

    /// <summary>
    /// Registers a pre-compiled template.
    /// </summary>
    public ITemplateMatcher RegisterTemplate(Template template);

    /// <summary>
    /// Tokenizes the input against all registered templates and returns the results.
    /// </summary>
    public TemplateMatchResult Tokenize(string input);

    /// <summary>
    /// Tokenizes the input against registered templates filtered by tags.
    /// </summary>
    public TemplateMatchResult Tokenize(string input, string[]? tags);

    /// <summary>
    /// Tokenizes the input against all registered templates, returning the best match assigned to a new <typeparamref name="T"/>.
    /// Returns null if no template matched.
    /// </summary>
    public T? Tokenize<T>(string input) where T : class, new();

    /// <summary>
    /// Tokenizes the input against registered templates filtered by tags, returning the best match assigned to a new <typeparamref name="T"/>.
    /// Returns null if no template matched.
    /// </summary>
    public T? Tokenize<T>(string input, string[]? tags) where T : class, new();

    /// <summary>
    /// Compiles and registers a template read from a <see cref="TextReader"/>.
    /// </summary>
    public Task<ITemplateMatcher> RegisterTemplateAsync(TextReader reader, CancellationToken ct = default);

    /// <summary>
    /// Compiles and registers a template read from a <see cref="Stream"/>.
    /// </summary>
    public Task<ITemplateMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/> against all registered templates.
    /// </summary>
    public Task<TemplateMatchResult> TokenizeAsync(TextReader input, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/> filtered by tags.
    /// </summary>
    public Task<TemplateMatchResult> TokenizeAsync(TextReader input, string[]? tags, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/>, returning the best match assigned to a new <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> TokenizeAsync<T>(TextReader input, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/> filtered by tags, returning the best match assigned to a new <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> TokenizeAsync<T>(TextReader input, string[]? tags, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/> against all registered templates.
    /// </summary>
    public Task<TemplateMatchResult> TokenizeAsync(Stream input, Encoding encoding, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/> filtered by tags.
    /// </summary>
    public Task<TemplateMatchResult> TokenizeAsync(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/>, returning the best match assigned to a new <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> TokenizeAsync<T>(Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/> filtered by tags, returning the best match assigned to a new <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> TokenizeAsync<T>(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default) where T : class, new();
}
