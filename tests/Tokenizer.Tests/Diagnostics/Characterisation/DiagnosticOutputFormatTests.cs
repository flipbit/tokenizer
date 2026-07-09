using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class DiagnosticOutputFormatTests : TokenizerTestBase
{
    public DiagnosticOutputFormatTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenAllTokensMatch_WhenRenderingAlignment_ThenMatchedSectionPopulatedAndNoFailures()
    {
        // Arrange
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: Alice\nAge: 30";

        // Act
        var result = TokenizeWithDiagnostics(template, input);
        var alignment = result.Diagnostics!.RenderAlignment();

        // Assert
        Assert.True(alignment.Contains("Matched Tokens", StringComparison.Ordinal));
        Assert.True(alignment.Contains("Name", StringComparison.Ordinal));
        Assert.True(alignment.Contains("Age", StringComparison.Ordinal));
        Assert.False(alignment.Contains("Unmatched Tokens", StringComparison.Ordinal));
        Assert.True(alignment.Contains("Matched: 2", StringComparison.Ordinal));
        Assert.True(alignment.Contains("Missed: 0", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenMixedResults_WhenRenderingAlignment_ThenAllSectionsPopulated()
    {
        // Arrange
        var template = "Name: { Name }\nEmail: { Email : IsEmail }\nAge: { Age }";
        var input = "Name: Alice\nEmail: notvalid\nAge: 30";

        // Act
        var result = TokenizeWithDiagnostics(template, input);
        var alignment = result.Diagnostics!.RenderAlignment();

        // Assert
        Output.WriteLine(alignment);
        Assert.True(alignment.Contains("Matched Tokens", StringComparison.Ordinal));
        // Document: which sections appear and what they contain
        Assert.NotEmpty(alignment);
    }

    [Fact]
    public void GivenValidatorRejection_WhenRenderingAlignment_ThenDocumentWhatRendererSays()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: bad";

        // Act
        var result = TokenizeWithDiagnostics(template, input);
        var alignment = result.Diagnostics!.RenderAlignment();

        // Assert
        Output.WriteLine(alignment);
        // BUG: Current renderer says "preamble never found" even though the preamble was found.
        // After Phase 2 fix, this should say "validator rejected" or similar.
        // For now, document the current (incorrect) behaviour.
        Assert.True(alignment.Contains("preamble never found", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenAllTokensMatch_WhenCheckingVerdict_ThenVerdictShowsFullMatch()
    {
        // Arrange
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: Alice\nAge: 30";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        Assert.Equal("Matched 2 of 2 tokens.", result.Diagnostics!.Summary.Verdict);
    }

    [Fact]
    public void GivenPartialMatch_WhenCheckingVerdict_ThenVerdictShowsMissedCount()
    {
        // Arrange
        var template = "Name: { Name }\nAge: { Age }\nCity: { City }";
        var input = "Name: Alice\nAge: 30";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        Assert.Equal("Matched 2 of 3 tokens (1 missed).", result.Diagnostics!.Summary.Verdict);
    }

    [Fact]
    public void GivenNoMatches_WhenCheckingVerdict_ThenVerdictShowsAllMissed()
    {
        // Arrange
        var template = "A: { A }\nB: { B }";
        var input = "nothing";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        Assert.Equal("Matched 0 of 2 tokens (2 missed).", result.Diagnostics!.Summary.Verdict);
    }

    private TokenizeResult TokenizeWithDiagnostics(string template, string input)
    {
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        Output.WriteLine(result.Diagnostics!.RenderAlignment());
        return result;
    }
}
