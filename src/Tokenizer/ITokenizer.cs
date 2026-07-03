using System.IO;
using System.Text;

namespace Tokens;

/// <summary>
/// Extracts structured information from text using pattern matching.
/// </summary>
public interface ITokenizer
{
    /// <summary>Gets the options.</summary>
    TokenizerOptions Options { get; }

    /// <summary>
    /// Compiles a template pattern string into a reusable <see cref="Template"/>.
    /// Results are cached by pattern content.
    /// </summary>
    Template Compile(string pattern);

    /// <summary>
    /// Compiles a template pattern string with an explicit name.
    /// Results are cached by pattern content.
    /// </summary>
    Template Compile(string pattern, string name);

    /// <summary>
    /// Compiles a template from a <see cref="TextReader"/>. Not cached.
    /// </summary>
    Template Compile(TextReader reader);

    /// <summary>
    /// Compiles a template from a <see cref="TextReader"/> with an explicit name. Not cached.
    /// </summary>
    Template Compile(TextReader reader, string name);

    /// <summary>
    /// Parses the given template pattern and tokenizes the input string against it.
    /// </summary>
    TokenizeResult Tokenize(string template, string input);

    /// <summary>
    /// Tokenizes the input string using a pre-compiled template.
    /// </summary>
    TokenizeResult Tokenize(Template template, string input);

    /// <summary>
    /// Parses the given pattern and tokenizes the input, mapping values onto a new <typeparamref name="T"/>.
    /// </summary>
    TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new();

    /// <summary>
    /// Tokenizes the input using a pre-compiled template, mapping values onto a new <typeparamref name="T"/>.
    /// </summary>
    TokenizeResult<T> Tokenize<T>(Template template, string input) where T : class, new();

    /// <summary>
    /// Tokenizes the input from a <see cref="TextReader"/> using a pre-compiled template.
    /// The caller retains ownership of the reader; it is not disposed.
    /// </summary>
    TokenizeResult Tokenize(Template template, TextReader input);

    /// <summary>
    /// Tokenizes the input from a <see cref="TextReader"/> using a pre-compiled template,
    /// mapping values onto a new <typeparamref name="T"/>.
    /// The caller retains ownership of the reader; it is not disposed.
    /// </summary>
    TokenizeResult<T> Tokenize<T>(Template template, TextReader input) where T : class, new();

    /// <summary>
    /// Tokenizes the input from a <see cref="Stream"/> using a pre-compiled template.
    /// The stream is not disposed; it remains open for further use.
    /// </summary>
    TokenizeResult Tokenize(Template template, Stream input, Encoding encoding);

    /// <summary>
    /// Tokenizes the input from a <see cref="Stream"/> using a pre-compiled template,
    /// mapping values onto a new <typeparamref name="T"/>.
    /// The stream is not disposed; it remains open for further use.
    /// </summary>
    TokenizeResult<T> Tokenize<T>(Template template, Stream input, Encoding encoding) where T : class, new();

    /// <summary>
    /// Clears the compilation cache, forcing subsequent calls to recompile patterns.
    /// </summary>
    void ClearCompilationCache();
}
