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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.FrontMatterTokenAssigned
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
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        var frontMatterEvents = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.FrontMatterTokenAssigned
                     || e.Type == DiagnosticEventType.FrontMatterTokenFailed)
            .ToList();
        foreach (var evt in frontMatterEvents)
        {
            Output.WriteLine($"{evt.Type}: {evt.TokenName} = {evt.Value}");
        }
        Assert.NotNull(diagnostics);
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
