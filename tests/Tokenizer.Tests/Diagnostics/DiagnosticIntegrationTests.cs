using Tokens.Exceptions;
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
            e => e.Type == TokenizationEventType.TokenizationStarted);
        Assert.Contains(result.Diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenizationCompleted);
        Assert.Contains(result.Diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.TokenAssigned && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
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
            e => e.Type == TokenizationEventType.ValidatorFailed
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
            e => e.Type == TokenizationEventType.TransformerSucceeded
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
            e => e.Type == TokenizationEventType.TokenMissed && string.Equals(e.TokenName, "Age", StringComparison.Ordinal));
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
            e => e.Type == TokenizationEventType.PreambleMatched);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenizingWithDecorators_ThenDecoratorEventsRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Date: { Date : ToDateTime(yyyy-MM-dd) }";
        var input = "Date: 2026-01-15";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.RawEvents,
            e => e.Type == TokenizationEventType.TransformerSucceeded);
        var transformerEvent = result.Diagnostics.RawEvents
            .First(e => e.Type == TokenizationEventType.TransformerSucceeded);
        Assert.NotNull(transformerEvent.DecoratorArgs);
        Assert.Contains("yyyy-MM-dd", transformerEvent.DecoratorArgs);
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

        // Assert: runtime diagnostics contain only TokenizationEvent (runtime) types,
        // not CompilationEvent types — separation is enforced by the type system.
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents, e => e.Type == TokenizationEventType.TokenizationStarted);
        Assert.Contains(diagnostics.RawEvents, e => e.Type == TokenizationEventType.TokenizationCompleted);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenCompilationFails_ThenExceptionCarriesCompilationDiagnostics()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });

        // Act
        var ex = Assert.Throws<TokenizerException>(() =>
            tokenizer.Compile("{ Name : UnknownDecoratorThatDoesNotExist }"));

        // Assert
        Assert.NotNull(ex.Data["CompilationDiagnostics"]);
        var diagnostics = (CompilationDiagnostics)ex.Data["CompilationDiagnostics"]!;
        Assert.True(diagnostics.Events.Count > 0);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenizationThrows_ThenExceptionCarriesDiagnostics()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions
        {
            EnableDiagnostics = true,
            MaxIterations = 1,
        });
        var template = tokenizer.Compile("{ Name }").Template;

        // Act
        var ex = Assert.Throws<TokenizerException>(() =>
            tokenizer.Tokenize(template, "some input that needs processing"));

        // Assert
        Assert.NotNull(ex.Data["Diagnostics"]);
        var diagnostics = (DiagnosticResult)ex.Data["Diagnostics"]!;
        Assert.True(diagnostics.RawEvents.Count > 0);
    }
}
