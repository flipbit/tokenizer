using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics;

public class CompilationDiagnosticsTests : TokenizerTestBase
{
    public CompilationDiagnosticsTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenCompiling_ThenCompilationDiagnosticsHasEvents()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });

        // Act
        var result = tokenizer.Compile("Name: { Name }");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics!.Events.Count > 0);
        Assert.Contains(result.Diagnostics.Events,
            e => e.Type == DiagnosticEventType.CompilationCompleted);
    }

    [Fact]
    public void GivenDiagnosticsDisabled_WhenCompiling_ThenCompilationDiagnosticsIsNull()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var result = tokenizer.Compile("Name: { Name }");

        // Assert
        Assert.Null(result.Diagnostics);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenCompiling_ThenEventsContainOnlyCompilationEvents()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });

        // Act
        var result = tokenizer.Compile("Name: { Name : IsEmail }");

        // Assert
        var diagnostics = result.Diagnostics!;
        // Should contain compilation events
        Assert.Contains(diagnostics.Events, e => e.Type == DiagnosticEventType.TokenCreated);
        Assert.Contains(diagnostics.Events, e => e.Type == DiagnosticEventType.DecoratorApplied);
        Assert.Contains(diagnostics.Events, e => e.Type == DiagnosticEventType.CompilationCompleted);
        // Should NOT contain runtime events
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.TokenizationStarted);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.PreambleMatched);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.TokenAssigned);
    }
}
