using System.Text;

namespace Tokens;

/// <summary>
/// Matches input text against multiple registered templates and returns the best match.
/// </summary>
public interface ITokenMatcher
{
    /// <summary>
    /// The collection of templates that will be matched against input strings.
    /// </summary>
    public TemplateCollection Templates { get; }

    /// <summary>
    /// Compiles and registers a template pattern string.
    /// </summary>
    public ITokenMatcher RegisterTemplate(string content);

    /// <summary>
    /// Compiles and registers a template pattern string with an explicit name.
    /// </summary>
    public ITokenMatcher RegisterTemplate(string content, string name);

    /// <summary>
    /// Registers a pre-compiled template.
    /// </summary>
    public ITokenMatcher RegisterTemplate(Template template);

    /// <summary>
    /// Matches the input string against all registered templates and returns the results.
    /// </summary>
    public TokenMatcherResult Match(string input);

    /// <summary>
    /// Matches the input string against registered templates filtered by tags.
    /// </summary>
    public TokenMatcherResult Match(string input, string[]? tags);

    /// <summary>
    /// Matches the input string against all registered templates and populates a new <typeparamref name="T"/>.
    /// </summary>
    public TokenMatcherResult<T> Match<T>(string input) where T : class, new();

    /// <summary>
    /// Matches the input string against registered templates filtered by tags and populates a new <typeparamref name="T"/>.
    /// </summary>
    public TokenMatcherResult<T> Match<T>(string input, string[]? tags) where T : class, new();

    /// <summary>
    /// Compiles and registers a template read from a <see cref="TextReader"/>.
    /// </summary>
    public Task<ITokenMatcher> RegisterTemplateAsync(TextReader reader, CancellationToken ct = default);

    /// <summary>
    /// Compiles and registers a template read from a <see cref="Stream"/>.
    /// </summary>
    public Task<ITokenMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, CancellationToken ct = default);

    /// <summary>
    /// Matches input from a <see cref="TextReader"/> against all registered templates.
    /// </summary>
    public Task<TokenMatcherResult> MatchAsync(TextReader input, CancellationToken ct = default);

    /// <summary>
    /// Matches input from a <see cref="TextReader"/> against registered templates filtered by tags.
    /// </summary>
    public Task<TokenMatcherResult> MatchAsync(TextReader input, string[]? tags, CancellationToken ct = default);

    /// <summary>
    /// Matches input from a <see cref="TextReader"/> against all registered templates and populates a new <typeparamref name="T"/>.
    /// </summary>
    public Task<TokenMatcherResult<T>> MatchAsync<T>(TextReader input, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Matches input from a <see cref="TextReader"/> against registered templates filtered by tags and populates a new <typeparamref name="T"/>.
    /// </summary>
    public Task<TokenMatcherResult<T>> MatchAsync<T>(TextReader input, string[]? tags, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Matches input from a <see cref="Stream"/> against all registered templates.
    /// </summary>
    public Task<TokenMatcherResult> MatchAsync(Stream input, Encoding encoding, CancellationToken ct = default);

    /// <summary>
    /// Matches input from a <see cref="Stream"/> against registered templates filtered by tags.
    /// </summary>
    public Task<TokenMatcherResult> MatchAsync(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default);

    /// <summary>
    /// Matches input from a <see cref="Stream"/> against all registered templates and populates a new <typeparamref name="T"/>.
    /// </summary>
    public Task<TokenMatcherResult<T>> MatchAsync<T>(Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Matches input from a <see cref="Stream"/> against registered templates filtered by tags and populates a new <typeparamref name="T"/>.
    /// </summary>
    public Task<TokenMatcherResult<T>> MatchAsync<T>(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default) where T : class, new();
}
