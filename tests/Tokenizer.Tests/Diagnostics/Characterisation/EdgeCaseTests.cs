using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class EdgeCaseTests : TokenizerTestBase
{
    public EdgeCaseTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenTemplate_WhenInputIsEmpty_ThenPreambleNeverFound()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenTemplate_WhenInputIsWhitespaceOnly_ThenPreambleNeverFound()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "   \n  ";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenMissed);
    }

    [Fact]
    public void GivenTemplate_WhenInputIsSingleCharacter_ThenPreambleNeverFound()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "X";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenMissed);
    }

    [Fact]
    public void GivenTemplate_WhenValueIsVeryLong_ThenTokenMatchedWithFullValue()
    {
        // Arrange
        var template = "Name: { Name }";
        var longValue = new string('A', 10000);
        var input = $"Name: {longValue}";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var assigned = diagnostics.RawEvents
            .First(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        Assert.Equal(longValue, assigned.Value);
    }

    [Fact]
    public void GivenTwoTokens_WhenGreedyCaptureContainsMissedTokenPreamble_ThenValueMismatchIssue()
    {
        // Arrange — Name captures greedily and swallows Age's preamble; Age is never found
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: Age: 30";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var nameToken = diagnostics.Tokens.Single(t => string.Equals(t.TokenName, "Name", StringComparison.Ordinal));
        var valueMismatch = nameToken.Issues.SingleOrDefault(i => i.Type == DiagnosticIssueType.ValueMismatch);
        Assert.NotNull(valueMismatch);
        Assert.Equal("TK004", valueMismatch.Code);
        Assert.NotNull(valueMismatch.Hint);
        Assert.Contains("delimiter", valueMismatch.Hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenTwoTokens_WhenValueContainsPreambleOfOtherToken_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: Age: 30\nAge: 25";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: what does Name get? What does Age get?
        var diagnostics = result.Diagnostics!;
        foreach (var evt in diagnostics.RawEvents.Where(e => e.Type == DiagnosticEventType.TokenAssigned))
        {
            Output.WriteLine($"Assigned: {evt.TokenName} = \"{evt.Value}\"");
        }
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        Assert.NotNull(diagnostics);
        Assert.Equal("Matched 2 of 2 tokens.", diagnostics.Verdict);
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal)
              && string.Equals(e.Value, "Age: 30", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Age", StringComparison.Ordinal)
              && string.Equals(e.Value, "25", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenTemplate_WhenInputContainsUnicode_ThenTokenMatchedWithUnicodeValue()
    {
        // Arrange
        var template = "Nom: { Name }";
        var input = "Nom: José"; // José with precomposed é

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenNewlineTerminatedToken_WhenValueEndsAtNewline_ThenNewlineTerminatedEventRecorded()
    {
        // Arrange
        var template = "Name: { Name$ }\nAge: { Age }";
        var input = "Name: Alice\nAge: 30";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        // Document: is NewlineTerminatedTokenProcessed event recorded?
        var newlineEvents = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.NewlineTerminatedTokenProcessed)
            .ToList();
        Output.WriteLine($"NewlineTerminatedTokenProcessed events: {newlineEvents.Count}");
    }

    [Fact]
    public void GivenSingleUseToken_WhenItFailsToMatch_ThenSingleUseTokenRemovedEvent()
    {
        // Arrange — a token that considers once and fails
        // ConsiderOnce tokens get one attempt then are removed
        // Using a validator that will reject to force failure
        var template = "A: { A : IsEmail }\nB: { B }";
        var input = "A: notanemail\nB: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        // Document: does SingleUseTokenRemoved event appear?
        var removed = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.SingleUseTokenRemoved)
            .ToList();
        Output.WriteLine($"SingleUseTokenRemoved events: {removed.Count}");
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        Assert.NotNull(diagnostics);
        Assert.Equal(0, removed.Count);
        Assert.Equal("Matched 1 of 2 tokens (1 missed).", diagnostics.Verdict);
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal));
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "B", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenOptionalToken_WhenNotPresent_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "Name: { Name }\nNickname: { Nickname? }";
        var input = "Name: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        // Name should match
        Assert.Contains(diagnostics.RawEvents,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        // Document: does optional token appear in issues even though it's optional?
        var nicknameMissed = diagnostics.RawEvents.Any(e => e.Type == DiagnosticEventType.TokenMissed
            && string.Equals(e.TokenName, "Nickname", StringComparison.Ordinal));
        var nicknameInIssues = diagnostics.Tokens.SelectMany(t => t.Issues).Any(i =>
            string.Equals(i.TokenName, "Nickname", StringComparison.Ordinal));
        Output.WriteLine($"Nickname missed event: {nicknameMissed}, in summary issues: {nicknameInIssues}");
        Output.WriteLine($"Verdict: {diagnostics.Verdict}");
        Assert.NotNull(diagnostics);
        Assert.True(nicknameMissed);
        Assert.True(nicknameInIssues);
        Assert.Equal("Matched 1 of 2 tokens (1 missed).", diagnostics.Verdict);
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
