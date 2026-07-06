using Tokens.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Compilation;

public class CompilationResultTests : TokenizerTestBase
{
    public CompilationResultTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenCompilationResult_WhenAccessed_ThenTemplateIsAvailable()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var result = tokenizer.Compile("Name: {Name}");

        // Assert
        Assert.NotNull(result.Template);
        Assert.Single(result.Template.Tokens);
    }

    [Fact]
    public void GivenDiagnosticsDisabled_WhenCompiling_ThenDiagnosticsIsNull()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var result = tokenizer.Compile("Name: {Name}");

        // Assert
        Assert.Null(result.Diagnostics);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenCompiling_ThenResultHasDiagnostics()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });

        // Act
        var result = tokenizer.Compile("Name: {Name}");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics.Events, e => e.Type == DiagnosticEventType.CompilationCompleted);
    }
}
