using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics;

public class DiagnosticIntegrationTests : TokenizerTestBase
{
    public DiagnosticIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenizingSimpleMatch_ThenDiagnosticsArePopulated()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics!.RawEvents.Count > 0);
        Assert.Contains(result.Diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenizationStarted);
        Assert.Contains(result.Diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenizationCompleted);
        Assert.Contains(result.Diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenDiagnosticsDisabled_WhenTokenizing_ThenDiagnosticsAreNull()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.Null(result.Diagnostics);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenValidatorFails_ThenValidatorFailedEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Email: { Email : IsEmail }";
        var input = "Email: notanemail";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.RawEvents,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTransformerSucceeds_ThenTransformerEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name : ToUpper }";
        var input = "Name: john";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.RawEvents,
            e => e.Type == DiagnosticEventType.TransformerSucceeded
              && string.Equals(e.DecoratorName, "ToUpperTransformer", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenMissed_ThenTokenMissedEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.RawEvents,
            e => e.Type == DiagnosticEventType.TokenMissed && string.Equals(e.TokenName, "Age", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenPreambleMatches_ThenPreambleMatchedEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.RawEvents,
            e => e.Type == DiagnosticEventType.PreambleMatched);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenizing_ThenRuntimeDiagnosticsContainNoCompilationEvents()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == DiagnosticEventType.TokenCreated);
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == DiagnosticEventType.DecoratorApplied);
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == DiagnosticEventType.OptionApplied);
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == DiagnosticEventType.ConcatenationApplied);
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == DiagnosticEventType.TagAdded);
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == DiagnosticEventType.HintAdded);
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == DiagnosticEventType.RepeatingTokenLinked);
        Assert.DoesNotContain(diagnostics.RawEvents, e => e.Type == DiagnosticEventType.CompilationCompleted);
    }
}
