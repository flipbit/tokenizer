using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tests.Diagnostics;

public class DiagnosticLoggingTests : TokenizerTestBase
{
    public DiagnosticLoggingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenDiagnosticTokenizer_WhenTokenizing_ThenDiagnosticsArePopulated()
    {
        // Arrange
        var tokenizer = CreateDiagnosticTokenizer();

        // Act
        var result = tokenizer.Tokenize("Name: { Name }", "Name: John");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics!.Summary.Verdict);
    }

    [Fact]
    public void GivenDiagnosticTokenizer_WhenTokenizingWithFailure_ThenSummaryHasIssues()
    {
        // Arrange
        var tokenizer = CreateDiagnosticTokenizer();

        // Act
        var result = tokenizer.Tokenize("Name: { Name }\nAge: { Age }", "Name: John");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics!.Summary.Issues);
    }
}
