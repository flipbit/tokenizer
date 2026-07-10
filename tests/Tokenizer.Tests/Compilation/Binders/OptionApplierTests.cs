using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Compilation.Binders;

public class OptionApplierTests
{
    [Fact]
    public void GivenOutOfOrderTokensEnabled_WhenApplying_ThenTokenIsOptional()
    {
        var options = new TokenizerOptions { OutOfOrderTokens = true };
        var token = new Token("Name", "Preamble", new FileLocation());

        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        Assert.True(token.IsOptional);
    }

    [Fact]
    public void GivenOutOfOrderTokensDisabled_WhenApplying_ThenTokenOptionalUnchanged()
    {
        var options = new TokenizerOptions { OutOfOrderTokens = false };
        var token = new Token("Name", "Preamble", new FileLocation());

        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        Assert.False(token.IsOptional);
    }

    [Fact]
    public void GivenGlobalTerminateOnNewLine_WhenTokenDoesNotSetIt_ThenTokenGetsNewLineTermination()
    {
        var options = new TokenizerOptions { TerminateOnNewLine = true };
        var token = new Token("Name", "Preamble", new FileLocation());
        token.TerminateOnNewLine = false;

        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        Assert.True(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenGlobalTerminateOnNewLine_WhenTokenAlreadySetsIt_ThenTokenUnchanged()
    {
        var options = new TokenizerOptions { TerminateOnNewLine = true };
        var token = new Token("Name", "Preamble", new FileLocation());
        token.TerminateOnNewLine = true;

        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        Assert.True(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenNoGlobalTerminateOnNewLine_WhenApplying_ThenTokenNewLineUnchanged()
    {
        var options = new TokenizerOptions { TerminateOnNewLine = false };
        var token = new Token("Name", "Preamble", new FileLocation());
        token.TerminateOnNewLine = false;

        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        Assert.False(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenOptionApplied_ThenRecordsEvent()
    {
        var options = new TokenizerOptions { OutOfOrderTokens = true };
        var token = new Token("Name", "Preamble", new FileLocation());
        var collector = new CompilationDiagnosticCollector();

        OptionApplier.Apply(token, options, collector);

        var diagnostics = collector.GetCompilationResult()!;
        Assert.Contains(diagnostics.Events, e => e.Type == CompilationEventType.OptionApplied);
    }
}
