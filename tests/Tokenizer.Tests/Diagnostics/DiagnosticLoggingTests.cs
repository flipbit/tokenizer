using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics;

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
        var template = tokenizer.Compile("Name: { Name }").Template;
        var result = tokenizer.Tokenize(template, "Name: John");

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
        var template = tokenizer.Compile("Name: { Name }\nAge: { Age }").Template;
        var result = tokenizer.Tokenize(template, "Name: John");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics!.Summary.Issues);
    }
}
