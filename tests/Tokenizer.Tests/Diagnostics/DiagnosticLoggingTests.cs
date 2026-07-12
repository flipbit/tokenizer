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
        Assert.NotEmpty(result.Diagnostics!.Verdict);
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
        Assert.NotEmpty(result.Diagnostics!.Tokens.SelectMany(t => t.Issues));
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenMissed_ThenIssueHasStableTKCode()
    {
        // Arrange
        var tokenizer = CreateDiagnosticTokenizer();

        // Act
        var template = tokenizer.Compile("Name: { Name }\nAge: { Age }").Template;
        var result = tokenizer.Tokenize(template, "Name: John");

        // Assert
        var issues = result.Diagnostics!.Tokens.SelectMany(t => t.Issues).ToList();
        Assert.All(issues, issue =>
        {
            Assert.NotNull(issue.Code);
            Assert.Matches(@"^TK\d{3}$", issue.Code);
        });
        Assert.Contains(issues, i => string.Equals(i.Code, "TK001", StringComparison.Ordinal));
    }
}
