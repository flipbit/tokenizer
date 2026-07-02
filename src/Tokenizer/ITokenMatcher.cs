using System.IO;

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
}
