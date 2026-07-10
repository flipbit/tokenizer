using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class CausalityChainTests : TokenizerTestBase
{
    public CausalityChainTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenOrderedTokens_WhenNonOptionalTokenMissing_ThenSubsequentTokensAreBlocked()
    {
        // Arrange — B is non-optional, so when B is missing, C is never searched for
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "A: one";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Output.WriteLine(diagnostics.RenderAlignment());

        var tokenA = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "A", StringComparison.Ordinal));
        var tokenB = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "B", StringComparison.Ordinal));
        var tokenC = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "C", StringComparison.Ordinal));

        Assert.Equal(TokenOutcome.Matched, tokenA.Outcome);
        Assert.Equal(TokenOutcome.NeverFound, tokenB.Outcome);
        // C should be blocked because non-optional B was not found
        Assert.Equal(TokenOutcome.Blocked, tokenC.Outcome);
        Assert.Equal("B", tokenC.BlockedBy);
    }

    [Fact]
    public void GivenOrderedTokens_WhenOptionalTokenMissing_ThenSubsequentTokensNotBlocked()
    {
        // Arrange — B is optional, so engine continues past it
        var template = "A: { A }\nB: { B? }\nC: { C }";
        var input = "A: one\nC: three";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Output.WriteLine(diagnostics.RenderAlignment());

        var tokenA = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "A", StringComparison.Ordinal));
        var tokenC = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "C", StringComparison.Ordinal));

        Assert.Equal(TokenOutcome.Matched, tokenA.Outcome);
        Assert.Equal(TokenOutcome.Matched, tokenC.Outcome);
    }

    [Fact]
    public void GivenOutOfOrderEnabled_WhenTokenMissing_ThenNoTokensBlocked()
    {
        // Arrange — OutOfOrder means all tokens are optional, no blocking
        var template = "---\nOutOfOrder: true\n---\nA: { A }\nB: { B }\nC: { C }";
        var input = "A: one\nC: three";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Output.WriteLine(diagnostics.RenderAlignment());

        // No token should be Blocked — OutOfOrder makes all tokens optional
        Assert.DoesNotContain(diagnostics.Tokens, t => t.Outcome == TokenOutcome.Blocked);
    }

    [Fact]
    public void GivenBlockedToken_WhenRendered_ThenShowsBlockedByMessage()
    {
        // Arrange
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "A: one";

        // Act
        var result = TokenizeWithDiagnostics(template, input);
        var alignment = result.Diagnostics!.RenderAlignment();

        // Assert
        Output.WriteLine(alignment);
        Assert.True(alignment.Contains("blocked", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GivenMultipleBlockedTokens_WhenFirstNonOptionalMissing_ThenAllSubsequentBlocked()
    {
        // Arrange — B missing blocks C, D, E
        var template = "A: { A }\nB: { B }\nC: { C }\nD: { D }\nE: { E }";
        var input = "A: one";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var tokenB = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "B", StringComparison.Ordinal));
        var tokenC = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "C", StringComparison.Ordinal));
        var tokenD = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "D", StringComparison.Ordinal));
        var tokenE = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "E", StringComparison.Ordinal));

        Assert.Equal(TokenOutcome.NeverFound, tokenB.Outcome);
        Assert.Equal(TokenOutcome.Blocked, tokenC.Outcome);
        Assert.Equal(TokenOutcome.Blocked, tokenD.Outcome);
        Assert.Equal(TokenOutcome.Blocked, tokenE.Outcome);
        // All blocked by B (the root cause)
        Assert.Equal("B", tokenC.BlockedBy);
        Assert.Equal("B", tokenD.BlockedBy);
        Assert.Equal("B", tokenE.BlockedBy);
    }

    [Fact]
    public void GivenBlockedToken_WhenIssuesChecked_ThenHintSuggestsFixingBlockingToken()
    {
        // Arrange
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "A: one";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var tokenC = diagnostics.Tokens.First(t => string.Equals(t.TokenName, "C", StringComparison.Ordinal));
        Assert.Equal(TokenOutcome.Blocked, tokenC.Outcome);
        var issue = Assert.Single(tokenC.Issues);
        Assert.NotNull(issue.Hint);
        Assert.True(issue.Hint!.Contains("B", StringComparison.Ordinal));
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
