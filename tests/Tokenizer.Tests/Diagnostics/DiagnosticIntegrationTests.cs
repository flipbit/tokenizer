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
        var compiled = tokenizer.Compile(template);
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics!.Events.Count > 0);
        Assert.Contains(result.Diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenizationStarted);
        Assert.Contains(result.Diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenizationCompleted);
        Assert.Contains(result.Diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned && e.TokenName == "Name");
    }

    [Fact]
    public void GivenDiagnosticsDisabled_WhenTokenizing_ThenDiagnosticsAreNull()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template);
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
        var compiled = tokenizer.Compile(template);
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && e.DecoratorName == "IsEmailValidator");
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTransformerSucceeds_ThenTransformerEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name : ToUpper }";
        var input = "Name: john";

        // Act
        var compiled = tokenizer.Compile(template);
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Events,
            e => e.Type == DiagnosticEventType.TransformerSucceeded
              && e.DecoratorName == "ToUpperTransformer");
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenMissed_ThenTokenMissedEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template);
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Events,
            e => e.Type == DiagnosticEventType.TokenMissed && e.TokenName == "Age");
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenPreambleMatches_ThenPreambleMatchedEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template);
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Events,
            e => e.Type == DiagnosticEventType.PreambleMatched);
    }
}
