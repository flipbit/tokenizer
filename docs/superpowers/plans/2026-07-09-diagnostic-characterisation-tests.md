# Phase 0: Diagnostic Characterisation Test Suite

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Write an exhaustive end-to-end test suite that documents the current diagnostic system behaviour, providing a safety net for the redesign phases that follow.

**Architecture:** 10 xUnit test fixture files in `tests/Tokenizer.Tests/Diagnostics/Characterisation/`, each covering one failure category. Every test creates a real `Tokenizer` with `EnableDiagnostics = true`, compiles a template string, tokenizes real input, and asserts on diagnostic output (events, issues, verdict, rendered alignment). These are characterisation tests — they document actual behaviour, including known bugs.

**Tech Stack:** C# / .NET 10.0, xUnit 2.9.3, Serilog.Sinks.XUnit for test output

## Global Constraints

- All test classes inherit from `TokenizerTestBase` and accept `ITestOutputHelper output`
- Test naming: Gherkin style `GivenScenario_WhenAction_ThenResult()`
- Test structure: `// Arrange` / `// Act` / `// Assert` comments required
- Namespace: `Tokens.Diagnostics.Characterisation`
- Use `CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true })` for all tests
- Use `StringComparison.Ordinal` for all token name comparisons
- Decorator names in events use full class names: `"IsEmailValidator"`, `"ToUpperTransformer"`, `"ToDateTimeTransformer"`, etc.
- Template syntax: `{ TokenName : Decorator }` (spaces around name/decorators), or `{TokenName:Decorator}` (no spaces) — both work
- Token modifiers: `?` optional, `!` required, `*` repeating, `$` terminate on newline
- Front matter between `---` delimiters for template options (Hint, OutOfOrder, Set, etc.)
- Where a test documents a **known bug**, add a comment `// BUG: <description>` so it's easy to find when fixing in later phases
- Every test that exercises diagnostic output should log it to test output for debugging:
  ```csharp
  Output.WriteLine(result.Diagnostics!.RenderAlignment());
  ```

## Helper Method

Each fixture will share a common tokenize-with-diagnostics pattern. To keep tests DRY, each fixture should have a private helper at the bottom of the class:

```csharp
private TokenizeResult TokenizeWithDiagnostics(string template, string input)
{
    var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
    var compiled = tokenizer.Compile(template).Template;
    var result = tokenizer.Tokenize(compiled, input);
    Output.WriteLine(result.Diagnostics!.RenderAlignment());
    return result;
}
```

For tests needing custom options (e.g. OutOfOrder), add an overload:

```csharp
private TokenizeResult TokenizeWithDiagnostics(string template, string input, TokenizerOptions options)
{
    options = options with { EnableDiagnostics = true };
    var tokenizer = CreateTokenizer(options);
    var compiled = tokenizer.Compile(template).Template;
    var result = tokenizer.Tokenize(compiled, input);
    Output.WriteLine(result.Diagnostics!.RenderAlignment());
    return result;
}
```

---

### Task 1: Preamble Matching Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/PreambleMatchingTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase` (base class), `TokenizerOptions`, `DiagnosticResult`, `DiagnosticEventType`, `DiagnosticIssueType`
- Produces: 12 characterisation tests documenting preamble matching behaviour

- [ ] **Step 1: Create the test fixture with the first 4 tests**

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class PreambleMatchingTests : TokenizerTestBase
{
    public PreambleMatchingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenSimpleTemplate_WhenInputMatches_ThenTokenMatchedAndNoIssues()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "Name: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        Assert.DoesNotContain(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed);
        Assert.Empty(diagnostics.Summary.Issues);
        Assert.Equal("Matched 1 of 1 tokens.", diagnostics.Summary.Verdict);
    }

    [Fact]
    public void GivenMultipleTokens_WhenAllMatch_ThenAllTokensMatchedAndCleanVerdict()
    {
        // Arrange
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: Alice\nAge: 30";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Equal("Matched 2 of 2 tokens.", diagnostics.Summary.Verdict);
        Assert.Empty(diagnostics.Summary.Issues);
    }

    [Fact]
    public void GivenTemplate_WhenPreambleNotFoundInInput_ThenPreambleNeverFoundIssue()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "Foo: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "Name", StringComparison.Ordinal));
        Assert.Equal("Matched 0 of 1 tokens (1 missed).", diagnostics.Summary.Verdict);
    }

    [Fact]
    public void GivenTemplate_WhenPreambleCaseMismatches_ThenPreambleNeverFoundWithNearMissHint()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "name: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var issue = Assert.Single(diagnostics.Summary.Issues);
        Assert.Equal(DiagnosticIssueType.PreambleNeverFound, issue.Type);
        // Near-miss hint generator should suggest the case difference
        Assert.NotNull(issue.Hint);
    }

    private TokenizeResult TokenizeWithDiagnostics(string template, string input)
    {
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        Output.WriteLine(result.Diagnostics!.RenderAlignment());
        return result;
    }

    private TokenizeResult TokenizeWithDiagnostics(string template, string input, TokenizerOptions options)
    {
        options = options with { EnableDiagnostics = true };
        var tokenizer = CreateTokenizer(options);
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);
        Output.WriteLine(result.Diagnostics!.RenderAlignment());
        return result;
    }
}
```

- [ ] **Step 2: Run the first 4 tests to verify they pass (or document failures)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PreambleMatchingTests" -v n`

These are characterisation tests — if any fail, adjust assertions to match actual behaviour and add a `// BUG:` comment if the behaviour is wrong.

- [ ] **Step 3: Add tests 5-8 (whitespace mismatch, partial match, out-of-order)**

Add these tests to the same class, before the helper methods:

```csharp
    [Fact]
    public void GivenTemplate_WhenPreambleWhitespaceMismatches_ThenPreambleNeverFound()
    {
        // Arrange
        var template = "Name:  { Name }"; // 2 spaces after colon (preamble is "Name:  ")
        var input = "Name: Alice";        // 1 space after colon

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenTemplate_WhenPreamblePartiallyMatches_ThenPreambleNeverFound()
    {
        // Arrange
        var template = "Username: { User }";
        var input = "User: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.PreambleNeverFound
              && string.Equals(i.TokenName, "User", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenTokensInOrder_WhenInputIsReversed_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "A: { A }\nB: { B }";
        var input = "B: Two\nA: One";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise actual behaviour
        var diagnostics = result.Diagnostics!;
        // Document: which tokens match and which are missed when input order differs
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var issue in diagnostics.Summary.Issues)
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
        }
        // At minimum, verify diagnostics are populated
        Assert.NotNull(diagnostics);
        Assert.True(diagnostics.Events.Count > 0);
    }

    [Fact]
    public void GivenOutOfOrderEnabled_WhenInputIsReversed_ThenBothTokensMatch()
    {
        // Arrange
        var template = "---\nOutOfOrder: true\n---\nA: { A }\nB: { B }";
        var input = "B: Two\nA: One";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "B", StringComparison.Ordinal));
        Assert.Empty(diagnostics.Summary.Issues);
    }
```

- [ ] **Step 4: Run tests 5-8**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PreambleMatchingTests" -v n`

Adjust assertions to match actual behaviour if needed.

- [ ] **Step 5: Add tests 9-12 (shared prefix, duplicate preamble, empty preamble, empty value)**

```csharp
    [Fact]
    public void GivenTokensSharingSamePreamblePrefix_WhenInputContainsShorterPrefix_ThenDocumentWhichTokenMatches()
    {
        // Arrange
        var template = "Email: { Email }\nEmail Address: { FullEmail }";
        var input = "Email: a@b.com";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise which token matches
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e => e.Type == DiagnosticEventType.TokenAssigned))
        {
            Output.WriteLine($"Assigned: {evt.TokenName} = {evt.Value}");
        }
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenNonRepeatingToken_WhenPreambleAppearsMultipleTimes_ThenFirstOccurrenceMatches()
    {
        // Arrange
        var template = "Name: { Name }";
        var input = "Name: Alice\nName: Bob";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var assigned = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Name", StringComparison.Ordinal))
            .ToList();
        Assert.Single(assigned);
        Assert.Equal("Alice", assigned[0].Value);
    }

    [Fact]
    public void GivenTokenAtStartOfTemplate_WhenInputStartsWithValue_ThenTokenMatches()
    {
        // Arrange
        var template = "{ Name } is here";
        var input = "Alice is here";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenTemplate_WhenPreambleFoundButValueIsEmpty_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "A: { A }\nB: { B }";
        var input = "A: \nB: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: does A match with empty value? Is an issue raised?
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e => e.Type == DiagnosticEventType.TokenAssigned))
        {
            Output.WriteLine($"Assigned: {evt.TokenName} = \"{evt.Value}\"");
        }
        foreach (var issue in diagnostics.Summary.Issues)
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
        }
        Assert.NotNull(diagnostics);
    }
```

- [ ] **Step 6: Run all 12 preamble tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PreambleMatchingTests" -v n`

Adjust any assertions needed. Commit once all 12 pass.

- [ ] **Step 7: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/PreambleMatchingTests.cs
git commit -m "Add preamble matching characterisation tests (12 tests)"
```

---

### Task 2: Validator Rejection Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/ValidatorRejectionTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase`, `DiagnosticResult`, `DiagnosticEventType`, `DiagnosticIssueType`
- Produces: 10 characterisation tests documenting validator rejection behaviour

- [ ] **Step 1: Create the fixture with tests 13-18**

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class ValidatorRejectionTests : TokenizerTestBase
{
    public ValidatorRejectionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenIsEmailValidator_WhenValueIsInvalid_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: notanemail";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.ValidatorRejection
              && string.Equals(i.TokenName, "Email", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenIsEmailValidator_WhenValueIsValid_ThenTokenMatchedNoIssues()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: user@example.com";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorPassed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Email", StringComparison.Ordinal));
        Assert.Empty(diagnostics.Summary.Issues);
    }

    [Fact]
    public void GivenIsNumericValidator_WhenValueIsText_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Count: { Count : IsNumeric }";
        var input = "Count: twelve";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsNumericValidator", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenIsPhoneNumberValidator_WhenValueIsGibberish_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Phone: { Phone : IsPhoneNumber }";
        var input = "Phone: abc";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.ValidatorRejection
              && string.Equals(i.TokenName, "Phone", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenIsDomainNameValidator_WhenValueIsInvalid_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Host: { Host : IsDomainName }";
        var input = "Host: not a domain";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.ValidatorRejection
              && string.Equals(i.TokenName, "Host", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenValidatorRejectsValue_WhenPreambleWasFound_ThenIssueIsValidatorRejectionNotPreambleNeverFound()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: bad";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;

        // Preamble WAS found
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.PreambleMatched);

        // Validator DID reject
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed);

        // Summary issues should report ValidatorRejection, NOT PreambleNeverFound
        // BUG: The AlignmentRenderer currently says "preamble never found" for this case.
        // The Summary.Issues correctly classifies this as ValidatorRejection, but the
        // rendered alignment output is misleading.
        var issues = diagnostics.Summary.Issues;
        Assert.Contains(issues, i => i.Type == DiagnosticIssueType.ValidatorRejection);
        Assert.DoesNotContain(issues, i => i.Type == DiagnosticIssueType.PreambleNeverFound);
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
```

- [ ] **Step 2: Run tests 13-18**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ValidatorRejectionTests" -v n`

If test 18 fails because Summary.Issues has PreambleNeverFound instead of ValidatorRejection, adjust the assertion to document the current (buggy) behaviour and mark with `// BUG:`.

- [ ] **Step 3: Add tests 19-22 (multiple validators, repeating, every rejection, empty value)**

```csharp
    [Fact]
    public void GivenMultipleValidators_WhenFirstPassesAndSecondRejects_ThenValidatorFailedForSecond()
    {
        // Arrange — IsNumeric passes on "123", IsEmail rejects it
        var template = "Val: { Val : IsNumeric, IsEmail }";
        var input = "Val: 123";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        // Document: does IsNumeric pass then IsEmail fail? Or does the engine short-circuit?
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e =>
            e.Type == DiagnosticEventType.ValidatorPassed || e.Type == DiagnosticEventType.ValidatorFailed))
        {
            Output.WriteLine($"{evt.Type}: {evt.DecoratorName} on value '{evt.Value}'");
        }
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenRepeatingTokenWithValidator_WhenSomeOccurrencesRejected_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "Item: { Item : Repeating, IsNumeric }";
        var input = "Item: 1\nItem: two\nItem: 3";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: does repeating stop at first failure or continue?
        var diagnostics = result.Diagnostics!;
        var validatorPassed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.ValidatorPassed
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        var validatorFailed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.ValidatorFailed
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        Output.WriteLine($"ValidatorPassed count: {validatorPassed.Count}");
        Output.WriteLine($"ValidatorFailed count: {validatorFailed.Count}");
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenValidator_WhenEveryOccurrenceRejected_ThenTokenMissedWithValidatorRejection()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: bad1\nEmail: bad2";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        // Preamble was found (at least once), so this should NOT be PreambleNeverFound
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.PreambleMatched);
        // Document: is the issue ValidatorRejection or PreambleNeverFound?
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var issue in diagnostics.Summary.Issues)
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
        }
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenValidator_WhenValueIsEmpty_ThenValidatorRejectionIssue()
    {
        // Arrange
        var template = "Name: { Name : IsEmail }";
        var input = "Name: ";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e =>
            e.Type == DiagnosticEventType.ValidatorFailed || e.Type == DiagnosticEventType.ValidatorPassed))
        {
            Output.WriteLine($"{evt.Type}: {evt.DecoratorName} on value '{evt.Value}'");
        }
        Assert.NotNull(diagnostics);
    }
```

- [ ] **Step 4: Run all 10 validator tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ValidatorRejectionTests" -v n`

- [ ] **Step 5: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/ValidatorRejectionTests.cs
git commit -m "Add validator rejection characterisation tests (10 tests)"
```

---

### Task 3: Transformer Failure Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/TransformerFailureTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase`, `DiagnosticResult`, `DiagnosticEventType`, `DiagnosticIssueType`
- Produces: 6 characterisation tests documenting transformer failure behaviour

- [ ] **Step 1: Create the fixture with all 6 tests**

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class TransformerFailureTests : TokenizerTestBase
{
    public TransformerFailureTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenToDateTimeTransformer_WhenFormatIsWrong_ThenTransformerFailureIssue()
    {
        // Arrange
        var template = "Date: { Date : ToDateTime('yyyy-MM-dd') }";
        var input = "Date: 13/01/2024";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TransformerFailed);
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.TransformerFailure
              && string.Equals(i.TokenName, "Date", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenToDateTimeTransformer_WhenFormatIsCorrect_ThenTokenMatchedNoIssues()
    {
        // Arrange
        var template = "Date: { Date : ToDateTime('yyyy-MM-dd') }";
        var input = "Date: 2024-01-13";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TransformerSucceeded);
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Date", StringComparison.Ordinal));
        Assert.Empty(diagnostics.Summary.Issues);
    }

    [Fact]
    public void GivenToDateTimeTransformer_WhenFormatDiffers_ThenHintSuggestsMatchingFormat()
    {
        // Arrange
        var template = "Date: { Date : ToDateTime('yyyy-MM-dd') }";
        var input = "Date: 01/13/2024";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var issue = diagnostics.Summary.Issues
            .FirstOrDefault(i => i.Type == DiagnosticIssueType.TransformerFailure
                              && string.Equals(i.TokenName, "Date", StringComparison.Ordinal));
        Assert.NotNull(issue);
        // DateFormatHintGenerator should produce a hint
        Output.WriteLine($"Hint: {issue!.Hint ?? "(none)"}");
    }

    [Fact]
    public void GivenTransformerFails_WhenPreambleWasFound_ThenIssueIsTransformerFailureNotPreambleNeverFound()
    {
        // Arrange
        var template = "Date: { Date : ToDateTime('yyyy-MM-dd') }";
        var input = "Date: not-a-date";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;

        // Preamble WAS found
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.PreambleMatched);

        // Transformer DID fail
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TransformerFailed);

        // BUG: AlignmentRenderer says "preamble never found" for this case (same bug as test 18)
        var issues = diagnostics.Summary.Issues;
        Assert.Contains(issues, i => i.Type == DiagnosticIssueType.TransformerFailure);
        Assert.DoesNotContain(issues, i => i.Type == DiagnosticIssueType.PreambleNeverFound);
    }

    [Fact]
    public void GivenChainedTransformerAndValidator_WhenTransformerSucceedsAndValidatorFails_ThenValidatorRejection()
    {
        // Arrange — ToUpper succeeds, IsEmail rejects the uppercased value
        var template = "Val: { Val : ToUpper, IsEmail }";
        var input = "Val: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TransformerSucceeded
              && string.Equals(e.DecoratorName, "ToUpperTransformer", StringComparison.Ordinal));
        // The failing decorator should be the validator, not the transformer
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && string.Equals(e.DecoratorName, "IsEmailValidator", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenChainedTransformers_WhenSecondFails_ThenTransformerFailureForSecond()
    {
        // Arrange — ToUpper succeeds, ToDateTime fails on the uppercased text
        var template = "Val: { Val : ToUpper, ToDateTime('yyyy-MM-dd') }";
        var input = "Val: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TransformerSucceeded
              && string.Equals(e.DecoratorName, "ToUpperTransformer", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TransformerFailed);
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
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
```

- [ ] **Step 2: Run all 6 transformer tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TransformerFailureTests" -v n`

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/TransformerFailureTests.cs
git commit -m "Add transformer failure characterisation tests (6 tests)"
```

---

### Task 4: Repeating Token Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/RepeatingTokenTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase`, `DiagnosticResult`, `DiagnosticEventType`, `DiagnosticIssueType`
- Produces: 5 characterisation tests documenting repeating token diagnostic behaviour

- [ ] **Step 1: Create the fixture with all 5 tests**

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class RepeatingTokenTests : TokenizerTestBase
{
    public RepeatingTokenTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenRepeatingToken_WhenAllOccurrencesMatch_ThenAllMatchedNoIssues()
    {
        // Arrange
        var template = "Item: { Item : Repeating }";
        var input = "Item: A\nItem: B\nItem: C";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var assigned = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        Output.WriteLine($"Matched {assigned.Count} occurrences");
        Assert.True(assigned.Count >= 1);
    }

    [Fact]
    public void GivenRepeatingTokenWithValidator_WhenMiddleOccurrenceFails_ThenRepeatingTokenCutShort()
    {
        // Arrange
        var template = "Item: { Item : Repeating, IsNumeric }";
        var input = "Item: 1\nItem: two\nItem: 3";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var issue in diagnostics.Summary.Issues)
        {
            Output.WriteLine($"Issue: {issue.Type} — {issue.TokenName}: {issue.Description}");
            if (issue.Hint != null) Output.WriteLine($"  Hint: {issue.Hint}");
        }
        // Document: is RepeatingTokenDisabled event raised?
        var disabled = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.RepeatingTokenDisabled)
            .ToList();
        Output.WriteLine($"RepeatingTokenDisabled events: {disabled.Count}");
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenRepeatingToken_WhenLineGapExists_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "Item: { Item : Repeating }";
        var input = "Item: A\n\n\nItem: B";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: does a line gap disable repeating?
        var diagnostics = result.Diagnostics!;
        var assigned = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        Output.WriteLine($"Matched {assigned.Count} occurrences");
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenRepeatingToken_WhenPreambleNeverFound_ThenPreambleNeverFoundIssue()
    {
        // Arrange
        var template = "Item: { Item : Repeating }";
        var input = "Nothing here";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "Item", StringComparison.Ordinal));
        // No RepeatingTokenDisabled — it was never started
        Assert.DoesNotContain(diagnostics.Events,
            e => e.Type == DiagnosticEventType.RepeatingTokenDisabled
              && string.Equals(e.TokenName, "Item", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenRepeatingTokenWithValidator_WhenOneMatchThenFailure_ThenOneMatchAndCutShort()
    {
        // Arrange
        var template = "Item: { Item : Repeating, IsNumeric }";
        var input = "Item: 1\nItem: nope";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var assigned = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Item", StringComparison.Ordinal))
            .ToList();
        Output.WriteLine($"Matched {assigned.Count} occurrences");
        Assert.True(assigned.Count >= 1);
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
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
```

- [ ] **Step 2: Run all 5 repeating token tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "RepeatingTokenTests" -v n`

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/RepeatingTokenTests.cs
git commit -m "Add repeating token characterisation tests (5 tests)"
```

---

### Task 5: Hint Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/HintTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase`, `DiagnosticResult`, `DiagnosticEventType`, `DiagnosticIssueType`
- Produces: 3 characterisation tests documenting hint diagnostic behaviour

- [ ] **Step 1: Create the fixture with all 3 tests**

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class HintTests : TokenizerTestBase
{
    public HintTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenRequiredHint_WhenPresentInInput_ThenHintMatchedAndNormalProcessing()
    {
        // Arrange
        var template = "---\nHint: Invoice\n---\nAmount: { Amount }";
        var input = "Invoice #1234\nAmount: $50.00";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.HintMatched);
        Assert.DoesNotContain(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.HintMissing);
    }

    [Fact]
    public void GivenRequiredHint_WhenMissingFromInput_ThenHintMissingIssue()
    {
        // Arrange
        var template = "---\nHint: Invoice\n---\nAmount: { Amount }";
        var input = "Receipt #1234\nAmount: $50.00";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.HintMissing);
        Assert.Contains(diagnostics.Summary.Issues,
            i => i.Type == DiagnosticIssueType.HintMissing);
    }

    [Fact]
    public void GivenRequiredHint_WhenCaseDiffers_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "---\nHint: Invoice\n---\nAmount: { Amount }";
        var input = "invoice #1234\nAmount: $50.00";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: is hint matching case-sensitive or case-insensitive?
        var diagnostics = result.Diagnostics!;
        var hintMatched = diagnostics.Events.Any(e => e.Type == DiagnosticEventType.HintMatched);
        var hintMissing = diagnostics.Events.Any(e => e.Type == DiagnosticEventType.HintMissing);
        Output.WriteLine($"HintMatched: {hintMatched}, HintMissing: {hintMissing}");
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
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
```

- [ ] **Step 2: Run all 3 hint tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "Characterisation.HintTests" -v n`

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/HintTests.cs
git commit -m "Add hint characterisation tests (3 tests)"
```

---

### Task 6: Front Matter Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/FrontMatterTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase`, `DiagnosticResult`, `DiagnosticEventType`
- Produces: 2 characterisation tests documenting front matter diagnostic behaviour

- [ ] **Step 1: Create the fixture with both tests**

```csharp
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
        // Arrange — Set directive with a transformer that will fail
        var template = "---\nSet: MyDate : ToDateTime('yyyy-MM-dd') = not-a-date\n---\nName: { Name }";
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
```

- [ ] **Step 2: Run both front matter tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "Characterisation.FrontMatterTests" -v n`

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/FrontMatterTests.cs
git commit -m "Add front matter characterisation tests (2 tests)"
```

---

### Task 7: Multi-Token Interaction Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/MultiTokenInteractionTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase`, `DiagnosticResult`, `DiagnosticEventType`, `DiagnosticIssueType`
- Produces: 5 characterisation tests documenting multi-token interaction behaviour

- [ ] **Step 1: Create the fixture with all 5 tests**

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class MultiTokenInteractionTests : TokenizerTestBase
{
    public MultiTokenInteractionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenFirstTokenMissing_WhenSecondTokenCouldMatch_ThenDocumentBehaviour()
    {
        // Arrange
        var template = "A: { A }\nB: { B }";
        var input = "B: hello";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise: does B match despite A being missed?
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        foreach (var evt in diagnostics.Events.Where(e =>
            e.Type == DiagnosticEventType.TokenAssigned || e.Type == DiagnosticEventType.TokenMissed))
        {
            Output.WriteLine($"{evt.Type}: {evt.TokenName}");
        }
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenFirstTokenValidatorFails_WhenSecondTokenAvailable_ThenSecondTokenMatches()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }\nName: { Name }";
        var input = "Email: Alice\nName: Bob";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        // Email should fail validation ("Alice" is not an email)
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && string.Equals(e.TokenName, "Email", StringComparison.Ordinal));
        // Name should still match
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenAllTokens_WhenInputIsUnrelated_ThenAllTokensMissed()
    {
        // Arrange
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "completely unrelated text";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var missed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenMissed)
            .ToList();
        Assert.Equal(3, missed.Count);
        Assert.Equal("Matched 0 of 3 tokens (3 missed).", diagnostics.Summary.Verdict);
    }

    [Fact]
    public void GivenThreeTokens_WhenMiddleTokenMissing_ThenFirstAndThirdMatch()
    {
        // Arrange
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "A: one\nC: three";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "A", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenMissed
              && string.Equals(e.TokenName, "B", StringComparison.Ordinal));
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "C", StringComparison.Ordinal));
        Assert.Equal("Matched 2 of 3 tokens (1 missed).", diagnostics.Summary.Verdict);
    }

    [Fact]
    public void GivenPreambleAppearsTwice_WhenFirstIsWrongContextAndSecondIsCorrect_ThenDocumentBacktracking()
    {
        // Arrange — "Name:" appears as part of a value, then as a real preamble
        var template = "Label: { Label }\nName: { Name }";
        var input = "Label: Name: fake\nName: real";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise backtracking behaviour
        var diagnostics = result.Diagnostics!;
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        var backtracks = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.BacktrackStarted)
            .ToList();
        Output.WriteLine($"Backtrack events: {backtracks.Count}");
        foreach (var evt in diagnostics.Events.Where(e =>
            e.Type == DiagnosticEventType.TokenAssigned || e.Type == DiagnosticEventType.TokenMissed))
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
```

- [ ] **Step 2: Run all 5 multi-token tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "MultiTokenInteractionTests" -v n`

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/MultiTokenInteractionTests.cs
git commit -m "Add multi-token interaction characterisation tests (5 tests)"
```

---

### Task 8: Edge Case Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/EdgeCaseTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase`, `DiagnosticResult`, `DiagnosticEventType`, `DiagnosticIssueType`
- Produces: 9 characterisation tests documenting edge case diagnostic behaviour

- [ ] **Step 1: Create the fixture with tests 44-49 (empty/whitespace/single-char/long/cross-preamble/unicode)**

```csharp
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
        Assert.Contains(diagnostics.Events,
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
        Assert.Contains(diagnostics.Events,
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
        Assert.Contains(diagnostics.Events,
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
        var assigned = diagnostics.Events
            .First(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        Assert.Equal(longValue, assigned.Value);
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
        foreach (var evt in diagnostics.Events.Where(e => e.Type == DiagnosticEventType.TokenAssigned))
        {
            Output.WriteLine($"Assigned: {evt.TokenName} = \"{evt.Value}\"");
        }
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenTemplate_WhenInputContainsUnicode_ThenTokenMatchedWithUnicodeValue()
    {
        // Arrange
        var template = "Nom: { Name }";
        var input = "Nom: Jos\u00e9"; // José with precomposed é

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
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
```

- [ ] **Step 2: Run tests 44-49**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "EdgeCaseTests" -v n`

- [ ] **Step 3: Add tests 50-52 (newline-terminated, single-use removed, optional token)**

```csharp
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
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        // Document: is NewlineTerminatedTokenProcessed event recorded?
        var newlineEvents = diagnostics.Events
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
        var removed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.SingleUseTokenRemoved)
            .ToList();
        Output.WriteLine($"SingleUseTokenRemoved events: {removed.Count}");
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenOptionalToken_WhenNotPresent_ThenNoIssueRaised()
    {
        // Arrange
        var template = "Name: { Name }\nNickname: { Nickname? }";
        var input = "Name: Alice";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        // Name should match
        Assert.Contains(diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned
              && string.Equals(e.TokenName, "Name", StringComparison.Ordinal));
        // Nickname is optional — should NOT appear as an issue
        Assert.DoesNotContain(diagnostics.Summary.Issues,
            i => string.Equals(i.TokenName, "Nickname", StringComparison.Ordinal));
        // Document: does verdict count optional tokens?
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");
    }
```

- [ ] **Step 4: Run all 9 edge case tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "EdgeCaseTests" -v n`

- [ ] **Step 5: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/EdgeCaseTests.cs
git commit -m "Add edge case characterisation tests (9 tests)"
```

---

### Task 9: Attempt Counting Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/AttemptCountingTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase`, `DiagnosticResult`, `DiagnosticEventType`
- Produces: 3 characterisation tests documenting token attempt/rejection history

- [ ] **Step 1: Create the fixture with all 3 tests**

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics.Characterisation;

public class AttemptCountingTests : TokenizerTestBase
{
    public AttemptCountingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenTokenWithValidator_WhenConsideredThreeTimesAndMatchesOnce_ThenThreeAttemptsVisible()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: bad1\nEmail: bad2\nEmail: a@b.com";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var preambleMatches = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.PreambleMatched
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();
        var validatorFailed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.ValidatorFailed
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();
        var validatorPassed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.ValidatorPassed
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();
        var assigned = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();

        Output.WriteLine($"PreambleMatched: {preambleMatches.Count}");
        Output.WriteLine($"ValidatorFailed: {validatorFailed.Count}");
        Output.WriteLine($"ValidatorPassed: {validatorPassed.Count}");
        Output.WriteLine($"TokenAssigned: {assigned.Count}");

        // Document the counts — this is what Phase 4 will aggregate into TokenAttempts
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void GivenTokenWithValidator_WhenConsideredMultipleTimesAndNeverMatches_ThenAllRejectionsVisible()
    {
        // Arrange
        var template = "Email: { Email : IsEmail }";
        var input = "Email: x\nEmail: y\nEmail: z";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var validatorFailed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.ValidatorFailed
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();
        var tokenMissed = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenMissed
                     && string.Equals(e.TokenName, "Email", StringComparison.Ordinal))
            .ToList();

        Output.WriteLine($"ValidatorFailed: {validatorFailed.Count}");
        Output.WriteLine($"TokenMissed: {tokenMissed.Count}");
        Output.WriteLine($"Verdict: {diagnostics.Summary.Verdict}");

        Assert.True(tokenMissed.Count >= 1, "Token should be missed");
    }

    [Fact]
    public void GivenMultipleCandidateTokensAtSamePosition_ThenDocumentWhichCandidatesAreTried()
    {
        // Arrange — two tokens with similar preambles competing at same position
        var template = "Name: { FirstName }\nName: { LastName }";
        var input = "Name: Alice\nName: Smith";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert — characterise how multiple candidates are handled
        var diagnostics = result.Diagnostics!;
        var attempted = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenAssignmentAttempted)
            .ToList();
        var assigned = diagnostics.Events
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned)
            .ToList();

        Output.WriteLine($"TokenAssignmentAttempted: {attempted.Count}");
        foreach (var evt in attempted)
        {
            Output.WriteLine($"  Attempted: {evt.TokenName} with value '{evt.Value}'");
        }
        Output.WriteLine($"TokenAssigned: {assigned.Count}");
        foreach (var evt in assigned)
        {
            Output.WriteLine($"  Assigned: {evt.TokenName} = '{evt.Value}'");
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
```

- [ ] **Step 2: Run all 3 attempt counting tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "AttemptCountingTests" -v n`

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/AttemptCountingTests.cs
git commit -m "Add attempt counting characterisation tests (3 tests)"
```

---

### Task 10: Diagnostic Output Format Tests

**Files:**
- Create: `tests/Tokenizer.Tests/Diagnostics/Characterisation/DiagnosticOutputFormatTests.cs`

**Interfaces:**
- Consumes: `TokenizerTestBase`, `DiagnosticResult`, `DiagnosticEventType`, `DiagnosticIssueType`
- Produces: 6 characterisation tests documenting diagnostic output format

- [ ] **Step 1: Create the fixture with all 6 tests**

```csharp
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
        Assert.Contains("Matched Tokens", alignment);
        Assert.Contains("Name", alignment);
        Assert.Contains("Age", alignment);
        Assert.DoesNotContain("Unmatched Tokens", alignment);
        Assert.Contains("Matched: 2", alignment);
        Assert.Contains("Missed: 0", alignment);
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
        Assert.Contains("Matched Tokens", alignment);
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
        Assert.Contains("preamble never found", alignment);
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
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "A: one\nC: three";

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
```

- [ ] **Step 2: Run all 6 output format tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticOutputFormatTests" -v n`

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/DiagnosticOutputFormatTests.cs
git commit -m "Add diagnostic output format characterisation tests (6 tests)"
```

---

### Task 11: Final Verification

**Files:**
- No new files

- [ ] **Step 1: Run the full characterisation test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "Tokens.Diagnostics.Characterisation" -v n`

Expected: All 61 tests pass. If any fail, go back and adjust assertions to match actual behaviour, adding `// BUG:` comments where the behaviour is incorrect.

- [ ] **Step 2: Run the full test suite to ensure no regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v n`

Expected: All existing tests still pass. The characterisation tests are additive — they don't modify any production code.

- [ ] **Step 3: Review and tighten characterisation assertions**

After running all tests with output, go back to any test that used loose assertions (`Assert.NotNull(diagnostics)`) and tighten them based on the actual observed behaviour. The goal is that every test locks down current behaviour so that later phases can't accidentally change it without the test failing.

For each test, the assertions should answer: "If someone changes the diagnostic output, will this test catch it?"

- [ ] **Step 4: Final commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/
git commit -m "Tighten characterisation test assertions based on observed behaviour"
```
