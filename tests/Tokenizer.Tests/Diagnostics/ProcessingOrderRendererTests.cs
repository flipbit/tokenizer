using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics;

public class ProcessingOrderRendererTests : TokenizerTestBase
{
    public ProcessingOrderRendererTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenSuccessfulMatch_WhenRendering_ThenShowsChronologicalSteps()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        var output = result.Diagnostics!.RenderProcessingOrder();

        // Assert
        Output.WriteLine(output);
        Assert.True(output.Contains("Processing Order", StringComparison.Ordinal));
        Assert.True(output.Contains("TokenizationStarted", StringComparison.Ordinal));
        Assert.True(output.Contains("TokenAssigned", StringComparison.Ordinal));
        Assert.True(output.Contains("TokenizationCompleted", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenValidatorFailure_WhenRendering_ThenShowsFailureInSequence()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Email: { Email : IsEmail }";
        var input = "Email: bad";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        var output = result.Diagnostics!.RenderProcessingOrder();

        // Assert
        Output.WriteLine(output);
        Assert.True(output.Contains("ValidatorFailed", StringComparison.Ordinal));
        Assert.True(output.Contains("IsEmailValidator", StringComparison.Ordinal));
        Assert.True(output.Contains("TokenMissed", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenMultipleTokens_WhenRendering_ThenEventsAreNumbered()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: Alice\nAge: 30";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        var output = result.Diagnostics!.RenderProcessingOrder();

        // Assert
        Output.WriteLine(output);
        // Events should be numbered sequentially
        Assert.True(output.Contains("[1]", StringComparison.Ordinal));
        Assert.True(output.Contains("[2]", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenRenderedOutput_WhenCached_ThenSecondCallReturnsSameInstance()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        var first = result.Diagnostics!.RenderProcessingOrder();
        var second = result.Diagnostics!.RenderProcessingOrder();

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void GivenDecoratorWithArgs_WhenRendering_ThenArgsAppearInOutput()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Date: { Date : ToDateTime(yyyy-MM-dd) }";
        var input = "Date: 2026-01-15";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        var output = result.Diagnostics!.RenderProcessingOrder();

        // Assert
        Output.WriteLine(output);
        Assert.Contains("yyyy-MM-dd", output, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenBlockedTokens_WhenRendering_ThenBlockedTokenAppears()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "First: { First }\nSecond: { Second }";
        var input = "nothing matching";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        var output = result.Diagnostics!.RenderProcessingOrder();

        // Assert
        Output.WriteLine(output);
        Assert.Contains("TokenMissed", output, StringComparison.Ordinal);
    }
}
