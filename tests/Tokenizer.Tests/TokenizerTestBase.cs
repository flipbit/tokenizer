using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Tokens;

/// <summary>
/// Base class for all tokenizer tests that provides logging infrastructure.
/// All test classes should inherit from this to automatically output logs to xUnit test output.
/// </summary>
public abstract class TokenizerTestBase
{
    protected ITestOutputHelper Output { get; }
    protected ILoggerFactory LoggerFactory { get; }

    protected TokenizerTestBase(ITestOutputHelper output)
    {
        Output = output;
        LoggerFactory = TestLoggerFactory.CreateFactory(output);
    }

    /// <summary>
    /// Creates a Tokenizer with default options and logging enabled.
    /// </summary>
    protected ITokenizer CreateTokenizer()
    {
        return new Tokenizer(new TokenizerOptions(), LoggerFactory);
    }

    /// <summary>
    /// Creates a Tokenizer with custom options and logging enabled.
    /// </summary>
    protected ITokenizer CreateTokenizer(TokenizerOptions options)
    {
        return new Tokenizer(options, LoggerFactory);
    }

    /// <summary>
    /// Creates a Tokenizer with diagnostics enabled and logging.
    /// </summary>
    protected ITokenizer CreateDiagnosticTokenizer()
    {
        return CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
    }

    /// <summary>
    /// Compiles the template and tokenizes the input with diagnostics enabled.
    /// Writes the alignment view to test output and returns the result.
    /// </summary>
    protected TokenizeResult TokenizeWithDiagnostics(string template, string input)
    {
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        Output.WriteLine(result.Diagnostics!.RenderAlignment());
        return result;
    }
}
