using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Tokenization;

public class TokenizationSessionTests
{
    [Fact]
    public void GivenValidInput_WhenRun_ThenTokenizesSuccessfully()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var session = CreateSession(template, target: null, result);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));

        // Act
        session.Run(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Alice", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public async Task GivenValidInput_WhenRunAsync_ThenTokenizesSuccessfully()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var session = CreateSession(template, target: null, result);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));

        // Act
        await session.RunAsync(context, CancellationToken.None);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Alice", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public async Task GivenRunAndRunAsync_WhenSameInput_ThenProduceIdenticalResults()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("A:{First}B:{Second}").Template;

        var input = "A:helloB:world";

        // Act — sync
        var syncResult = new TokenizeResultBuilder().WithTemplate(template).Build();
        var syncSession = CreateSession(template, target: null, syncResult);
        var syncContext = new TokenizationContext();
        syncContext.Initialize(new System.IO.StringReader(input));
        syncSession.Run(syncContext);

        // Act — async
        var asyncResult = new TokenizeResultBuilder().WithTemplate(template).Build();
        var asyncSession = CreateSession(template, target: null, asyncResult);
        var asyncContext = new TokenizationContext();
        asyncContext.Initialize(new System.IO.StringReader(input));
        await asyncSession.RunAsync(asyncContext, CancellationToken.None);

        // Assert
        Assert.Equal(syncResult.Tokens.Matches.Count, asyncResult.Tokens.Matches.Count);
        for (var i = 0; i < syncResult.Tokens.Matches.Count; i++)
        {
            Assert.Equal(syncResult.Tokens.Matches[i].Token.Name, asyncResult.Tokens.Matches[i].Token.Name);
            Assert.Equal(syncResult.Tokens.Matches[i].Value, asyncResult.Tokens.Matches[i].Value);
        }
    }

    [Fact]
    public void GivenExplicitIterationLimit_WhenExceeded_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxIterations = 1 };
        var parser = new TemplateCompiler(options);
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var session = CreateSession(template, target: null, result);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: A long value that exceeds one iteration"));

        // Act & Assert
        Assert.Throws<TokenizerException>(() => session.Run(context));
    }

    [Fact]
    public async Task GivenCancelledToken_WhenRunAsync_ThenThrowsOperationCancelledException()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var session = CreateSession(template, target: null, result);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            session.RunAsync(context, cts.Token));
    }

    [Fact]
    public void GivenMaxInputLengthExceeded_WhenRunCalledWithReader_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxInputLength = 10 };
        var parser = new TemplateCompiler(options);
        var template = parser.Compile("Name: {Name}").Template;
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var engine = new TokenizationEngine();

        // Input exceeds MaxInputLength of 10
        var input = "Name: This is a very long input string that exceeds the limit";
        context.Initialize(new System.IO.StringReader(input));

        var session = engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() => session.Run(context));
        Assert.Contains("exceeds maximum allowed length", ex.Message, StringComparison.Ordinal);
    }

    private static TokenizationSession CreateSession(
        Template template, object? target, TokenizeResultBase result,
        IDiagnosticCollector? collector = null)
    {
        return new TokenizationSession(
            template, target, result,
            collector ?? NullDiagnosticCollector.Instance,
            hintStrategy: null,
            NullLogger<TokenizationEngine>.Instance);
    }
}
