using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    /// </summary>
    Template Compile(string pattern);

    /// <summary>
    /// Compiles a template pattern string with an explicit name.
    /// </summary>
    Template Compile(string pattern, string name);

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
    /// Asynchronously compiles a template from a <see cref="TextReader"/>.
    /// </summary>
    Task<Template> CompileAsync(TextReader reader, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously compiles a template from a <see cref="TextReader"/> with an explicit name.
    /// </summary>
    Task<Template> CompileAsync(TextReader reader, string name, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously compiles a template from a <see cref="Stream"/>.
    /// </summary>
    Task<Template> CompileAsync(Stream input, Encoding encoding, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously compiles a template from a <see cref="Stream"/> with an explicit name.
    /// </summary>
    Task<Template> CompileAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/> using a pre-compiled template.
    /// </summary>
    Task<TokenizeResult> TokenizeAsync(Template template, TextReader input, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/>, mapping values onto a new <typeparamref name="T"/>.
    /// </summary>
    Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/> using a pre-compiled template.
    /// </summary>
    Task<TokenizeResult> TokenizeAsync(Template template, Stream input, Encoding encoding, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/>, mapping values onto a new <typeparamref name="T"/>.
    /// </summary>
    Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new();
}
