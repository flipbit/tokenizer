using System.IO;
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
    TemplateCollection Templates { get; }

    /// <summary>
    /// Compiles and registers a template pattern string.
    /// </summary>
    ITokenMatcher RegisterTemplate(string content);

    /// <summary>
    /// Compiles and registers a template pattern string with an explicit name.
    /// </summary>
    ITokenMatcher RegisterTemplate(string content, string name);

    /// <summary>
    /// Compiles and registers a template from a <see cref="TextReader"/>.
    /// </summary>
    ITokenMatcher RegisterTemplate(TextReader reader);

    /// <summary>
    /// Compiles and registers a template from a <see cref="TextReader"/> with an explicit name.
    /// </summary>
    ITokenMatcher RegisterTemplate(TextReader reader, string name);

    /// <summary>
    /// Registers a pre-compiled template.
    /// </summary>
    ITokenMatcher RegisterTemplate(Template template);

    /// <summary>
    /// Matches the input string against all registered templates and returns the results.
    /// </summary>
    TokenMatcherResult Match(string input);

    /// <summary>
    /// Matches the input string against registered templates filtered by tags.
    /// </summary>
    TokenMatcherResult Match(string input, string[]? tags);

    /// <summary>
    /// Matches the input string against all registered templates and populates a new <typeparamref name="T"/>.
    /// </summary>
    TokenMatcherResult<T> Match<T>(string input) where T : class, new();

    /// <summary>
    /// Matches the input string against registered templates filtered by tags and populates a new <typeparamref name="T"/>.
    /// </summary>
    TokenMatcherResult<T> Match<T>(string input, string[]? tags) where T : class, new();

    /// <summary>
    /// Matches the input from a <see cref="TextReader"/> against all registered templates.
    /// The caller retains ownership of the reader; it is not disposed.
    /// </summary>
    TokenMatcherResult Match(TextReader input);

    /// <summary>
    /// Matches the input from a <see cref="TextReader"/> against registered templates filtered by tags.
    /// </summary>
    TokenMatcherResult Match(TextReader input, string[]? tags);

    /// <summary>
    /// Matches the input from a <see cref="TextReader"/> against all registered templates and populates a new <typeparamref name="T"/>.
    /// </summary>
    TokenMatcherResult<T> Match<T>(TextReader input) where T : class, new();

    /// <summary>
    /// Matches the input from a <see cref="TextReader"/> against registered templates filtered by tags and populates a new <typeparamref name="T"/>.
    /// </summary>
    TokenMatcherResult<T> Match<T>(TextReader input, string[]? tags) where T : class, new();

    /// <summary>
    /// Matches the input from a <see cref="Stream"/> against all registered templates.
    /// The stream is not disposed; it remains open for further use.
    /// </summary>
    TokenMatcherResult Match(Stream input, Encoding encoding);

    /// <summary>
    /// Matches the input from a <see cref="Stream"/> against registered templates filtered by tags.
    /// </summary>
    TokenMatcherResult Match(Stream input, Encoding encoding, string[]? tags);

    /// <summary>
    /// Matches the input from a <see cref="Stream"/> against all registered templates and populates a new <typeparamref name="T"/>.
    /// </summary>
    TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding) where T : class, new();

    /// <summary>
    /// Matches the input from a <see cref="Stream"/> against registered templates filtered by tags and populates a new <typeparamref name="T"/>.
    /// </summary>
    TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding, string[]? tags) where T : class, new();
}
