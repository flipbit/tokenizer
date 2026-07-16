using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class FrontMatterTests : TokenizerTestBase
{
    public FrontMatterTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenFrontMatterSetToken_WhenValueProvided_ThenFrontMatterTokenAssigned()
    {
        // Arrange — Set directive assigns a value at compile time
        var template = "---\nSet: MyToken = Hello\n---\nName: { Name }";
        var input = "Name: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == TokenizationEventType.FrontMatterTokenAssigned
              && string.Equals(e.TokenName, "MyToken", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenFrontMatterSetToken_WhenTransformerFails_ThenFrontMatterTokenFailed()
    {
        // Arrange — Set directive with a transformer that will fail on the hardcoded value
        var template = "---\nSet: MyDate = 'not-a-date' : ToDateTime('yyyy-MM-dd')\n---\nName: { Name }";
        var input = "Name: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: what events are produced when a Set token's transformer fails?
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        var frontMatterEvents = diagnostics.RawEvents
            .Where(e => e.Type == TokenizationEventType.FrontMatterTokenAssigned
                     || e.Type == TokenizationEventType.FrontMatterTokenFailed)
            .ToList();
        foreach (var evt in frontMatterEvents)
        {
            Output.WriteLine($"{evt.Type}: {evt.TokenName} = {evt.Value}");
        }
        Assert.NotNull(diagnostics);
        Assert.Equal(1, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.MissedCount);
        Assert.Contains(frontMatterEvents,
            e => e.Type == TokenizationEventType.FrontMatterTokenFailed
              && string.Equals(e.TokenName, "MyDate", StringComparison.Ordinal));
        Assert.DoesNotContain(frontMatterEvents,
            e => e.Type == TokenizationEventType.FrontMatterTokenAssigned
              && string.Equals(e.TokenName, "MyDate", StringComparison.Ordinal));
    }

}
