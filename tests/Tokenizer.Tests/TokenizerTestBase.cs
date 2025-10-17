using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Tokens.Tests
{
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
        protected Tokenizer CreateTokenizer()
        {
            return Tokenizer.Create(TokenizerOptions.Defaults, LoggerFactory);
        }

        /// <summary>
        /// Creates a Tokenizer with custom options and logging enabled.
        /// </summary>
        protected Tokenizer CreateTokenizer(TokenizerOptions options)
        {
            return Tokenizer.Create(options, LoggerFactory);
        }
    }
}
