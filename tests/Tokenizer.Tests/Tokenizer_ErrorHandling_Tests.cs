using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Tokenization;
using Xunit;

namespace Tokens;

public class Tokenizer_ErrorHandling_Tests
{
    [Fact]
    public void GivenUnexpectedException_WhenTokenize_ThenRethrows()
    {
        // Arrange — use a throwing engine implementation to trigger unexpected exception
        var unexpectedException = new InvalidOperationException("something went wrong");
        var engine = new ThrowingTokenizationEngine(unexpectedException);

        var options = new TokenizerOptions();
        var compiler = new TemplateCompiler(options);
        var resultBuilder = new ResultBuilder();
        var logger = Substitute.For<ILogger<Tokenizer>>();
        logger.IsEnabled(logLevel: Arg.Any<LogLevel>()).Returns(returnThis: true);

        var tokenizer = new Tokenizer(
            Options.Create(options),
            logger,
            compiler,
            engine,
            resultBuilder);

        var template = new TemplateBuilder()
            .WithName("Test")
            .WithTokens(new TokenBuilder().WithName("Name").WithPreamble("Name: ").Build())
            .Build();

        // Act & Assert — the unexpected exception should propagate unchanged
        var thrown = Assert.Throws<InvalidOperationException>(
            () => tokenizer.Tokenize(template, "Name: Alice"));

        Assert.Same(unexpectedException, thrown);
    }

    private sealed class ThrowingTokenizationEngine : ITokenizationEngine
    {
        private readonly Exception _exception;

        public ThrowingTokenizationEngine(Exception exception) => _exception = exception;

        public TokenizationSession CreateSession(
            Template template, TokenizeResult result,
            IDiagnosticCollector collector, IHintStrategy? hintStrategy = null)
        {
            throw _exception;
        }
    }
}
