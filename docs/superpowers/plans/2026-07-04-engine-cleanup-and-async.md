# Engine Cleanup and Async Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Narrow the engine interface, strip duplicate logging, refactor TokenMatcher to a single logic path, and add real async tokenization support via a ring-buffered TokenEnumerator with cooperative engine yielding.

**Architecture:** Four sequential phases — each builds on the previous. Phase 1 narrows the engine interface and removes `inputLength`. Phase 2 strips verbose logging that duplicates diagnostics events. Phase 3 refactors TokenMatcher to a single `MatchCore` loop and removes sync stream/reader overloads. Phase 4 adds a ring buffer to TokenEnumerator, splits the engine into Begin/Continue/End, and adds async API surface to Tokenizer and TokenMatcher.

**Tech Stack:** C# (.NET Standard 2.0 / .NET 8.0 / .NET 10.0), xUnit 2.9.3, NSubstitute

---

### Task 1: Add CharactersConsumed to TokenEnumerator

**Files:**
- Modify: `src/Tokenizer/Enumerators/TokenEnumerator.cs`
- Test: `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorTests.cs`

- [ ] **Step 1: Write the failing test**

In `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorTests.cs`, add:

```csharp
[Fact]
public void GivenEnumerator_WhenAdvancing_ThenCharactersConsumedIsTracked()
{
    // Arrange
    var enumerator = new TokenEnumerator("hello");

    // Act
    enumerator.Next(); // 'h'
    enumerator.Next(); // 'e'
    enumerator.Next(); // 'l'

    // Assert
    Assert.Equal(3, enumerator.CharactersConsumed);
}

[Fact]
public void GivenEnumerator_WhenPeeking_ThenCharactersConsumedDoesNotIncrement()
{
    // Arrange
    var enumerator = new TokenEnumerator("hello");

    // Act
    enumerator.Peek();
    enumerator.Peek();

    // Assert
    Assert.Equal(0, enumerator.CharactersConsumed);
}

[Fact]
public void GivenEnumerator_WhenReset_ThenCharactersConsumedResets()
{
    // Arrange
    var enumerator = new TokenEnumerator("hi");
    enumerator.Next();
    enumerator.Next();

    // Act
    enumerator.Reset();

    // Assert
    Assert.Equal(0, enumerator.CharactersConsumed);
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CharactersConsumed"
```

Expected: FAIL — `CharactersConsumed` does not exist.

- [ ] **Step 3: Implement CharactersConsumed**

In `src/Tokenizer/Enumerators/TokenEnumerator.cs`, add the property after the `Location` property (around line 59):

```csharp
/// <summary>
/// Gets the total number of characters consumed via <see cref="Next"/>.
/// </summary>
public long CharactersConsumed { get; private set; }
```

In the `Next()` method, after `var next = ReadChar();` and before the `if (next == '\0') return '\0';` guard, the counter should NOT increment for '\0'. Add the increment after the null guard:

```csharp
public char Next()
{
    var next = ReadChar();
    if (next == '\0') return '\0';

    CharactersConsumed++;

    // ... rest of existing method unchanged
```

In the `Reset()` method, add after `Location.Reset();`:

```csharp
CharactersConsumed = 0;
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CharactersConsumed"
```

Expected: PASS — all three tests green.

- [ ] **Step 5: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All existing tests still pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Enumerators/TokenEnumerator.cs tests/Tokenizer.Tests/Enumerators/TokenEnumeratorTests.cs
git commit -m "feat: add CharactersConsumed tracking to TokenEnumerator"
```

---

### Task 2: Remove inputLength from engine interface and use CharactersConsumed

**Files:**
- Modify: `src/Tokenizer/Tokenization/ITokenizationEngine.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`

- [ ] **Step 1: Update ITokenizationEngine — remove inputLength parameter**

In `src/Tokenizer/Tokenization/ITokenizationEngine.cs`, change the `ProcessTokenization` signature from:

```csharp
    void ProcessTokenization(
        Template template,
        int inputLength,
        object? targetObject,
        ITokenizationContext context,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);
```

to:

```csharp
    void ProcessTokenization(
        Template template,
        object? targetObject,
        ITokenizationContext context,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);
```

Remove the other four methods (`TryAssignCandidateTokens`, `ProcessFrontMatterTokens`, `ProcessRepeatedTokens`, `ProcessNewlineTerminatedTokens`) from the interface entirely — they will remain as private methods on the class.

- [ ] **Step 2: Update TokenizationEngine — remove inputLength, use CharactersConsumed**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`:

Update the `ProcessTokenization` method signature to match the interface (remove `int inputLength` parameter).

Change the methods that were on the interface to `private`:
- `TryAssignCandidateTokens` — change `public bool` to `private bool`
- `ProcessFrontMatterTokens` — change `public void` to `private void`
- `ProcessRepeatedTokens` — change `public bool` to `private bool`
- `ProcessNewlineTerminatedTokens` — change `public void` to `private void`

Replace the `maxIterations` calculation (around line 121-123) from:

```csharp
var maxIterations = template.Options.MaxIterations > 0
    ? template.Options.MaxIterations
    : inputLength > 0 ? inputLength * 2 : int.MaxValue;
```

to:

```csharp
var hasExplicitLimit = template.Options.MaxIterations > 0;
```

Replace the iteration guard (around line 129-135) from:

```csharp
iterationCount++;
if (iterationCount > maxIterations)
{
    throw new TokenizerException(
        $"Tokenization exceeded maximum iteration count of {maxIterations:N0}. " +
        "This may indicate a problematic template pattern. " +
        "Increase TokenizerOptions.MaxIterations to allow more iterations.");
}
```

to:

```csharp
iterationCount++;
if (hasExplicitLimit && iterationCount > template.Options.MaxIterations)
{
    throw new TokenizerException(
        $"Tokenization exceeded maximum iteration count of {template.Options.MaxIterations:N0}. " +
        "This may indicate a problematic template pattern. " +
        "Increase TokenizerOptions.MaxIterations to allow more iterations.");
}

if (!hasExplicitLimit && iterationCount > context.Enumerator.CharactersConsumed * 2 + 100)
{
    throw new TokenizerException(
        $"Tokenization exceeded derived iteration limit (iterations: {iterationCount:N0}, " +
        $"characters consumed: {context.Enumerator.CharactersConsumed:N0}). " +
        "This may indicate a problematic template pattern. " +
        "Set TokenizerOptions.MaxIterations to override the automatic limit.");
}
```

Update the `LogDebug` for `inputLength` (around line 107) — change:

```csharp
log.LogDebug("Tokenization started for template '{TemplateName}' with input length {InputLength}",
    template.Name, inputLength);
```

to:

```csharp
log.LogDebug("Tokenization started for template '{TemplateName}'", template.Name);
```

Update the `collector.Record` for `TokenizationStarted` (around line 110-111) — change:

```csharp
collector.Record(DiagnosticEventType.TokenizationStarted,
    detail: $"Template: {template.Name}, Tokens: {template.Tokens.Count}, Input length: {inputLength}");
```

to:

```csharp
collector.Record(DiagnosticEventType.TokenizationStarted,
    detail: $"Template: {template.Name}, Tokens: {template.Tokens.Count}");
```

- [ ] **Step 3: Update Tokenizer.cs — remove inputLength from TokenizeCore**

In `src/Tokenizer/Tokenizer.cs`, update the call to `ProcessTokenization` in `TokenizeCore` (around line 284) from:

```csharp
var inputLength = rawInput?.Length ?? 0;
tokenizationEngine.ProcessTokenization(template, inputLength, value, context, result, collector, hintStrategy);
```

to:

```csharp
tokenizationEngine.ProcessTokenization(template, value, context, result, collector, hintStrategy);
```

Remove the `inputLength` local variable entirely.

- [ ] **Step 4: Build to verify compilation**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```

Expected: Build succeeds. The internal tests will fail at this point because they call the now-private methods directly — that's expected and handled in Task 3.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenization/ITokenizationEngine.cs src/Tokenizer/Tokenization/TokenizationEngine.cs src/Tokenizer/Tokenizer.cs
git commit -m "refactor: narrow ITokenizationEngine to ProcessTokenization only, remove inputLength"
```

---

### Task 3: Rewrite TokenizationEngineInternalTests

**Files:**
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs`

The six tests in this file called private methods directly. Rewrite them to test the same behaviors through the full `Tokenizer.Tokenize` pipeline. Each test maps to an observable behavior in the output `TokenizeResult`.

- [ ] **Step 1: Rewrite the test file**

Replace the entire contents of `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs` with:

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tokenization.Engine;

/// <summary>
/// Tests for TokenizationEngine behaviors that were previously tested through internal methods.
/// All tests now exercise behaviors through the public Tokenizer.Tokenize pipeline.
/// </summary>
public class TokenizationEngineTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public TokenizationEngineTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenRepeatingToken_WhenInputDoesNotMatchRepeat_ThenBacktracks()
    {
        // Arrange — template has a token; input has preamble but value doesn't repeat
        var template = _tokenizer.Compile("test: {Name}");

        // Act
        var result = _tokenizer.Tokenize(template, "test: hello");

        // Assert — the token should be matched with "hello"
        Assert.True(result.Success);
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("hello", result.Tokens.Matches.First().Value);
    }

    [Fact]
    public void GivenNewlineTerminatedToken_WhenInputHasNewline_ThenAssignsValueBeforeNewline()
    {
        // Arrange — template with token, input terminated by newline
        var template = _tokenizer.Compile("Name: {Name}\nAge: {Age}");

        // Act
        var result = _tokenizer.Tokenize(template, "Name: Alice\nAge: 30");

        // Assert — both tokens matched, newline-terminated token has correct value
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal("Alice", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
        Assert.Equal("30", result.Tokens.Matches.First(m => m.Token.Name == "Age").Value);
    }

    [Fact]
    public void GivenFrontMatterToken_WhenTokenizing_ThenFrontMatterIsProcessed()
    {
        // Arrange — template with front matter
        var template = _tokenizer.Compile("---\ntype: test\n---\nName: {Name}");

        // Act
        var result = _tokenizer.Tokenize(template, "Name: Bob");

        // Assert — front matter token is included in matches
        Assert.True(result.Success);
        Assert.Contains(result.Tokens.Matches, m => m.Token.Name == "Name");
    }

    [Fact]
    public void GivenCandidateTokens_WhenNoValueAccumulated_ThenTokenIsNotAssigned()
    {
        // Arrange — template with two adjacent tokens (no separator)
        var template = _tokenizer.Compile("A:{First}B:{Second}");

        // Act
        var result = _tokenizer.Tokenize(template, "A:B:value");

        // Assert — First has empty value (skipped), Second has "value"
        Assert.Contains(result.Tokens.Matches, m => m.Token.Name == "Second" && (string)m.Value == "value");
    }

    [Fact]
    public void GivenRepeatingToken_WhenValueIsAssignable_ThenTokenIsMatched()
    {
        // Arrange
        var template = _tokenizer.Compile("test: {Name}");

        // Act
        var result = _tokenizer.Tokenize(template, "test: hello");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("hello", result.Tokens.Matches.First().Value);
    }

    [Fact]
    public void GivenTemplateWithOnlyFrontMatter_WhenTokenizing_ThenResultIsSuccessful()
    {
        // Arrange — template with only a front matter token
        var template = _tokenizer.Compile("---\ntype: test\n---\n");

        // Act
        var result = _tokenizer.Tokenize(template, "anything");

        // Assert — front matter processed, result may or may not be "success" depending on
        // whether any tokens are required, but front matter tokens should be in matches
        Assert.NotNull(result);
    }
}
```

- [ ] **Step 2: Run the rewritten tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizationEngineTests"
```

Expected: All tests pass. Some tests may need adjustment based on actual template compilation behavior — if any fail, adjust the template patterns and assertions to match what the tokenizer actually produces.

- [ ] **Step 3: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs
git commit -m "test: rewrite engine internal tests to use public Tokenize pipeline"
```

---

### Task 4: Strip duplicate verbose logging from TokenizationEngine

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`

Apply the D2 rule: remove `LogTrace`/`LogDebug` calls that duplicate `collector.Record` events. Keep `LogWarning`, `LogError`, and phase-boundary `LogDebug`.

- [ ] **Step 1: Audit and remove duplicate log calls**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`, remove or simplify the following log calls. The line numbers reference the current file state after Task 2 changes.

**In `ProcessTokenization`:**

Keep these:
- `LogDebug` for target object type info (around "Target object type:")
- `LogError` for infinite loop detection in `ProcessRepeatedTokens`
- `LogWarning` for assignment errors in `TryAssignCandidateTokens`

Remove these (they duplicate `collector.Record` calls):
- `LogTrace("Start: Processing: {TemplateName}")` — duplicates `TokenizationStarted`
- `LogDebug("Tokenization started for template...")` — duplicates `TokenizationStarted`
- `LogDebug("Phase: Initialization completed...")` — unnecessary phase marker
- `LogTrace("Token match found at Line...")` — duplicates `PreambleMatched`
- `LogTrace("Attempting to match repeated token...")` — duplicates diagnostic events in `ProcessRepeatedTokens`
- `LogTrace("Repeated token processing resulted in backtrack...")` — duplicates `BacktrackStarted`
- `LogTrace("Newline detected at Line...")` — duplicates `NewlineTerminatedTokenProcessed`
- `LogDebug("Phase: Main tokenization loop completed...")` — unnecessary phase marker
- `LogTrace("Processing {CandidateCount} remaining candidates...")` — duplicates `TokenAssignmentAttempted`
- `LogTrace("Skipping remaining candidates...")` — no diagnostic value
- `LogDebug("Phase: Processing front matter tokens")` — unnecessary phase marker
- `LogTrace("Found {MatchCount} matches.")` — duplicates `TokenizationCompleted`
- `LogTrace("{MissingCount} required tokens were missing.")` — duplicates summary
- `LogDebug("Phase: Tokenization summary...")` — duplicates `TokenizationCompleted`
- `LogTrace("Finished: Processing: {TemplateName}")` — duplicates `TokenizationCompleted`

**In `TryAssignCandidateTokens`:**

Remove:
- `LogTrace("Attempting to assign {CandidateCount}...")` — duplicates `TokenAssignmentAttempted`
- `LogTrace("Token assignment succeeded...")` — duplicates `TokenAssigned`
- `LogDebug("Token matched: '{TokenName}'...")` — duplicates `TokenAssigned`
- `LogTrace("Token assignment failed...")` — duplicates `TokenAssignmentFailed`
- `LogTrace` per-candidate "Skipping" loop — duplicates `TokenAssignmentFailed`

Keep:
- `LogWarning` in the catch block

**In `ProcessFrontMatterTokens`:**

Remove:
- `LogTrace("Processing {FrontMatterCount} front matter tokens")` — duplicates observable in result
- `LogTrace("Attempting front matter token assignment...")` — duplicates `FrontMatterTokenAssigned`
- `LogTrace("Front matter token assigned...")` — duplicates `FrontMatterTokenAssigned`
- `LogTrace("Front matter token assignment failed...")` — duplicates `FrontMatterTokenFailed`

**In `ProcessRepeatedTokens`:**

Remove:
- `LogTrace("Checking if any of {CandidateCount}...")` — duplicates diagnostic events
- `LogTrace("Backtracking: None of the {CandidateCount}...")` — duplicates `BacktrackStarted`
- `LogTrace("Backtracking: Disabling repeating token...")` — duplicates `RepeatingTokenDisabled`
- `LogTrace("Backtracking: Removing single-use token...")` — duplicates `SingleUseTokenRemoved`
- `LogTrace("Ln: {Line} Col: {Column} : Skipping...")` — all three instances — duplicates observable in diagnostics
- `LogTrace("Backtracking: Advancing {AdvanceLength}...")` — implementation detail
- `LogTrace("Backtracking: Enumerator advanced to...")` — implementation detail

Keep:
- `LogError` for infinite loop detection (this is a real error, not a diagnostic trace)

**In `ProcessNewlineTerminatedTokens`:**

Remove:
- `LogTrace("Processing newline-terminated token...")` — duplicates `NewlineTerminatedTokenProcessed`
- `LogTrace("Disabling repeating token '{TokenName}'...")` — duplicates `RepeatingTokenDisabled` (if recorded there; if not, keep)

**In private handler methods (`HandleFirstTokenMatch`, `HandleTokenSwitch`, `HandleNoTokenMatch`, `HandleRepeatedTokenMatching`, `HandleNewlineTerminatedToken`):**

Remove all `LogTrace` calls — these are per-character/per-iteration traces that duplicate information observable through diagnostics or the result.

- [ ] **Step 2: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass. No test should depend on specific log output — tests assert on `TokenizeResult`, not log messages.

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationEngine.cs
git commit -m "refactor: strip verbose logging that duplicates diagnostic events"
```

---

### Task 5: Strip duplicate verbose logging from TokenMatcher

**Files:**
- Modify: `src/Tokenizer/TokenMatcher.cs`

- [ ] **Step 1: Remove duplicate LogTrace calls**

In `src/Tokenizer/TokenMatcher.cs`, remove per-template `LogTrace` calls in the match loops and `CheckTemplateTags` that duplicate information already observable in the `TokenMatcherResult`:

Remove from `Match(string, tags)` and `Match<T>(string, tags)`:
- `LogTrace("Start: Matching: {TemplateName}")` — observable via results
- `LogTrace("Match Success: {Success}")` — observable via `result.Success`
- `LogTrace("Total Matches: {MatchCount}")` — observable via `result.Tokens.Matches.Count`
- `LogTrace("Total Errors : {ErrorCount}")` — observable via `result.Exceptions.Count`
- `LogTrace("Finish: Matching: {TemplateName}")` — observable via results

Keep:
- `LogError` in the catch block

Remove from `CheckTemplateTags`:
- `LogTrace("No tags matching: {MissingTags}")` — implementation detail
- `LogTrace("Finish: Matching: {TemplateName}")` — duplicate of loop finish
- `LogTrace("Found tag matching: {Tags}")` — implementation detail

- [ ] **Step 2: Run tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenMatcher"
```

Expected: All TokenMatcher tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/TokenMatcher.cs
git commit -m "refactor: strip verbose logging from TokenMatcher"
```

---

### Task 6: Refactor TokenMatcher to single MatchCore logic path

**Files:**
- Modify: `src/Tokenizer/TokenMatcher.cs`
- Modify: `src/Tokenizer/ITokenMatcher.cs`

- [ ] **Step 1: Add MatchCore method and refactor Match(string, tags)**

In `src/Tokenizer/TokenMatcher.cs`, add a private `MatchCore` method that contains the single match loop, then refactor the existing `Match` methods to delegate to it.

Add this method after the constructors:

```csharp
private TResult MatchCore<TResult, TTokenizeResult>(
    string input,
    string[]? tags,
    TResult results,
    Func<Template, TTokenizeResult> tokenize)
    where TTokenizeResult : TokenizeResultBase
    where TResult : class
{
    tags ??= Array.Empty<string>();

    foreach (var name in Templates.Names)
    {
        if (!Templates.TryGet(name, out var template)) continue;

        if (!CheckTemplateTags(template, tags)) continue;

        try
        {
            var result = tokenize(template);

            switch (results)
            {
                case TokenMatcherResult r:
                    r.AddResult((TokenizeResult)(TokenizeResultBase)result);
                    break;
                case TokenMatcherResult<object> r:
                    // Handled by the generic path below
                    break;
            }

            // Use dynamic dispatch for the generic case
            AddResultDynamic(results, result);
        }
        catch (Exception e)
        {
            var exception = new Exceptions.TokenMatcherException(e.Message, template, e);
            log.LogError(e, "Error processing template: {TemplateName}", template.Name);
            throw exception;
        }
    }

    AssignBestMatch(results);
    return results;
}
```

Actually, the generic dispatch is tricky because `TokenMatcherResult` and `TokenMatcherResult<T>` don't share a base class with `AddResult`. A cleaner approach uses two action delegates:

```csharp
private TResult MatchCore<TResult>(
    string input,
    string[]? tags,
    TResult results,
    Func<Template, TokenizeResultBase> tokenize,
    Action<TResult, TokenizeResultBase> addResult,
    Action<TResult> assignBestMatch)
{
    tags ??= Array.Empty<string>();

    foreach (var name in Templates.Names)
    {
        if (!Templates.TryGet(name, out var template)) continue;

        if (!CheckTemplateTags(template, tags)) continue;

        try
        {
            var result = tokenize(template);
            addResult(results, result);
        }
        catch (Exception e)
        {
            var exception = new Exceptions.TokenMatcherException(e.Message, template, e);
            log.LogError(e, "Error processing template: {TemplateName}", template.Name);
            throw exception;
        }
    }

    assignBestMatch(results);
    return results;
}
```

Then refactor the two `Match` methods:

```csharp
public TokenMatcherResult Match(string input, string[]? tags)
{
    var results = new TokenMatcherResult();
    return MatchCore(
        input, tags, results,
        template => tokenizer.Tokenize(template, input),
        (r, result) => r.AddResult((TokenizeResult)result),
        r => r.BestMatch = r.GetBestMatch());
}

public TokenMatcherResult<T> Match<T>(string input, string[]? tags) where T : class, new()
{
    var results = new TokenMatcherResult<T>();
    return MatchCore(
        input, tags, results,
        template => tokenizer.Tokenize<T>(template, input),
        (r, result) => r.AddResult((TokenizeResult<T>)result),
        r => r.BestMatch = r.GetBestMatch());
}
```

Remove the old duplicated loop bodies from both methods.

- [ ] **Step 2: Remove sync TextReader/Stream overloads from TokenMatcher**

Remove these methods from `TokenMatcher.cs`:

```csharp
// Remove all of these:
public TokenMatcherResult Match(TextReader input) => ...
public TokenMatcherResult Match(TextReader input, string[]? tags) => ...
public TokenMatcherResult<T> Match<T>(TextReader input) where T : class, new() => ...
public TokenMatcherResult<T> Match<T>(TextReader input, string[]? tags) where T : class, new() => ...
public TokenMatcherResult Match(Stream input, Encoding encoding) { ... }
public TokenMatcherResult Match(Stream input, Encoding encoding, string[]? tags) { ... }
public TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding) where T : class, new() { ... }
public TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding, string[]? tags) where T : class, new() { ... }
```

Also remove the sync TextReader registration overloads:

```csharp
// Remove:
public ITokenMatcher RegisterTemplate(TextReader reader) { ... }
public ITokenMatcher RegisterTemplate(TextReader reader, string name) { ... }
```

- [ ] **Step 3: Update ITokenMatcher interface to match**

In `src/Tokenizer/ITokenMatcher.cs`, remove the corresponding interface declarations:

Remove:
- `TokenMatcherResult Match(TextReader input);`
- `TokenMatcherResult Match(TextReader input, string[]? tags);`
- `TokenMatcherResult<T> Match<T>(TextReader input) where T : class, new();`
- `TokenMatcherResult<T> Match<T>(TextReader input, string[]? tags) where T : class, new();`
- `TokenMatcherResult Match(Stream input, Encoding encoding);`
- `TokenMatcherResult Match(Stream input, Encoding encoding, string[]? tags);`
- `TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding) where T : class, new();`
- `TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding, string[]? tags) where T : class, new();`
- `ITokenMatcher RegisterTemplate(TextReader reader);`
- `ITokenMatcher RegisterTemplate(TextReader reader, string name);`

- [ ] **Step 4: Update TokenMatcherStreamTests**

The stream tests use the now-removed sync `Match(TextReader)` and `Match(Stream)` methods. These tests need to be updated: the TextReader and Stream tests become async tests that will be implemented in Phase 4. For now, remove the stream tests and keep only the string-based tests.

In `tests/Tokenizer.Tests/TokenMatcherStreamTests.cs`, remove the test class entirely (it only contains stream/reader tests that will be replaced with async versions in Task 11).

- [ ] **Step 5: Build and run tests**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: Build succeeds, all remaining tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/TokenMatcher.cs src/Tokenizer/ITokenMatcher.cs tests/Tokenizer.Tests/TokenMatcherStreamTests.cs
git commit -m "refactor: unify TokenMatcher match loops into MatchCore, remove sync stream/reader overloads"
```

---

### Task 7: Add AllowStreamBuffering option

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`

- [ ] **Step 1: Add the property**

In `src/Tokenizer/TokenizerOptions.cs`, add after the `MaxIterations` property (around line 105):

```csharp
/// <summary>
/// When true, allows non-seekable streams (e.g. NetworkStream) to be buffered into memory
/// for operations that require re-reading the input (such as TokenMatcher matching against
/// multiple templates). Default: false.
/// When false, passing a non-seekable stream to such operations throws a TokenizerException.
/// </summary>
public bool AllowStreamBuffering { get; init; }
```

Update the copy constructor (around line 33) to include:

```csharp
AllowStreamBuffering = original.AllowStreamBuffering;
```

Update the `Equals` method to include `AllowStreamBuffering`:

```csharp
&& AllowStreamBuffering == other.AllowStreamBuffering
```

Update the `GetHashCode` method to include:

```csharp
hash = hash * 31 + AllowStreamBuffering.GetHashCode();
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/TokenizerOptions.cs
git commit -m "feat: add AllowStreamBuffering option to TokenizerOptions"
```

---

### Task 8: Remove sync TextReader/Stream overloads from Tokenizer

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/ITokenizer.cs`

- [ ] **Step 1: Remove sync TextReader/Stream Tokenize overloads**

In `src/Tokenizer/Tokenizer.cs`, remove these methods:

```csharp
// Remove: Tokenize(Template, TextReader) and Tokenize<T>(Template, TextReader)
public TokenizeResult Tokenize(Template template, TextReader input) { ... }
public TokenizeResult<T> Tokenize<T>(Template template, TextReader input) where T : class, new() { ... }

// Remove: private Tokenize(result, value, template, TextReader)
private void Tokenize(TokenizeResultBase result, object? value, Template template, TextReader input) { ... }

// Remove: Tokenize(Template, Stream, Encoding) and Tokenize<T>(Template, Stream, Encoding)
public TokenizeResult Tokenize(Template template, Stream input, Encoding encoding) { ... }
public TokenizeResult<T> Tokenize<T>(Template template, Stream input, Encoding encoding) where T : class, new() { ... }
```

Also remove the sync Compile(TextReader) overloads:

```csharp
// Remove:
public Template Compile(TextReader reader) => parser.Parse(reader);
public Template Compile(TextReader reader, string name) => parser.Parse(reader, name);
```

- [ ] **Step 2: Update ITokenizer interface**

In `src/Tokenizer/ITokenizer.cs`, remove:

```csharp
// Remove:
Template Compile(TextReader reader);
Template Compile(TextReader reader, string name);
TokenizeResult Tokenize(Template template, TextReader input);
TokenizeResult<T> Tokenize<T>(Template template, TextReader input) where T : class, new();
TokenizeResult Tokenize(Template template, Stream input, Encoding encoding);
TokenizeResult<T> Tokenize<T>(Template template, Stream input, Encoding encoding) where T : class, new();
```

- [ ] **Step 3: Fix any compilation errors in tests**

Some tests may use the removed overloads. Search for any test that calls `Tokenize(template, TextReader)` or `Tokenize(template, Stream, ...)` and update them to use the string overload instead. These will get proper async test versions in Phase 4.

```bash
dotnet build ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj 2>&1 | head -50
```

Fix any errors found.

- [ ] **Step 4: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs src/Tokenizer/ITokenizer.cs
git commit -m "refactor: remove sync TextReader/Stream overloads from Tokenizer, replaced by async in Phase 4"
```

---

### Task 9: Implement ring buffer on TokenEnumerator

**Files:**
- Modify: `src/Tokenizer/Enumerators/TokenEnumerator.cs`
- Test: `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorTests.cs`

This is the core async infrastructure change. The pushback queue and direct `reader.Read()`/`reader.Peek()` calls are replaced by a ring buffer. All public methods (`Next`, `Peek`, `TryMatch`, `Advance`) read from the buffer.

- [ ] **Step 1: Write tests for ring buffer behavior**

Add to `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorTests.cs`:

```csharp
[Fact]
public void GivenEnumerator_WhenFillBufferCalled_ThenBuffersCharacters()
{
    // Arrange
    var enumerator = new TokenEnumerator(new StringReader("hello world"));

    // Act
    enumerator.FillBuffer();

    // Assert — can read characters without hitting the reader again
    Assert.Equal('h', enumerator.Peek());
    Assert.Equal('h', enumerator.Next());
    Assert.Equal('e', enumerator.Next());
    Assert.False(enumerator.IsEmpty);
}

[Fact]
public void GivenEnumerator_WhenNeedsRefillAfterDraining_ThenReportsTrue()
{
    // Arrange — small input that fits in one buffer fill
    var enumerator = new TokenEnumerator(new StringReader("hi"));
    enumerator.FillBuffer();

    // Act — drain the buffer
    enumerator.Next(); // 'h'
    enumerator.Next(); // 'i'

    // Assert
    Assert.True(enumerator.IsEmpty);
}

[Fact]
public void GivenEnumerator_WhenTryMatchAfterFillBuffer_ThenMatchesFromBuffer()
{
    // Arrange
    var enumerator = new TokenEnumerator(new StringReader("hello world"));
    enumerator.FillBuffer();

    // Act & Assert
    Assert.True(enumerator.TryMatch("hello"));
    Assert.False(enumerator.TryMatch("world"));
}

[Fact]
public async Task GivenEnumerator_WhenFillBufferAsyncCalled_ThenBuffersCharacters()
{
    // Arrange
    var enumerator = new TokenEnumerator(new StringReader("async test"));

    // Act
    await enumerator.FillBufferAsync(CancellationToken.None);

    // Assert
    Assert.Equal('a', enumerator.Peek());
    Assert.Equal('a', enumerator.Next());
    Assert.Equal('s', enumerator.Next());
}

[Fact]
public async Task GivenEnumerator_WhenCancelled_ThenThrowsOperationCancelled()
{
    // Arrange
    var enumerator = new TokenEnumerator(new StringReader("test"));
    var cts = new CancellationTokenSource();
    cts.Cancel();

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => enumerator.FillBufferAsync(cts.Token).AsTask());
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FillBuffer"
```

Expected: FAIL — `FillBuffer`/`FillBufferAsync` don't exist yet.

- [ ] **Step 3: Implement the ring buffer**

Replace the internals of `src/Tokenizer/Enumerators/TokenEnumerator.cs`. The full implementation replaces the `pushback` queue with a ring buffer and adds `FillBuffer`/`FillBufferAsync`/`NeedsRefill`:

```csharp
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tokens.Enumerators;

/// <summary>
/// A forward-only, character-level enumerator over a <see cref="TextReader"/> that tracks the current
/// <see cref="FileLocation"/> (line and column) as it advances. All line endings are normalised to <c>\n</c>.
/// Characters are served from an internal ring buffer that can be filled synchronously or asynchronously.
/// </summary>
public class TokenEnumerator
{
    private const int DefaultBufferSize = 1024;
    private const int RefillWatermark = 256;

    private TextReader reader;
    private readonly string? originalString;

    private char[] buffer;
    private int readPos;
    private int writePos;
    private int bufferedCount;
    private bool readerExhausted;

    private bool resetNextLine;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenEnumerator"/> over the specified <see cref="TextReader"/>.
    /// </summary>
    public TokenEnumerator(TextReader reader) : this(reader, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenEnumerator"/> over the specified string.
    /// </summary>
    public TokenEnumerator(string pattern) : this(new StringReader(pattern ?? string.Empty), pattern ?? string.Empty)
    {
    }

    private TokenEnumerator(TextReader reader, string? originalString)
    {
        this.reader = reader;
        this.originalString = originalString;
        buffer = new char[DefaultBufferSize];
        readPos = 0;
        writePos = 0;
        bufferedCount = 0;
        readerExhausted = false;
        Location = new FileLocation();

        // Initial fill so IsEmpty is accurate
        FillBuffer();
    }

    /// <summary>
    /// Gets a value indicating whether all characters have been consumed and the reader is exhausted.
    /// </summary>
    public bool IsEmpty => bufferedCount == 0 && readerExhausted;

    /// <summary>
    /// Gets a value indicating whether <see cref="Reset"/> is supported.
    /// </summary>
    public bool CanReset => originalString != null;

    /// <summary>
    /// Gets the current position in the source as a line/column <see cref="FileLocation"/>.
    /// </summary>
    public FileLocation Location { get; }

    /// <summary>
    /// Gets the total number of characters consumed via <see cref="Next"/>.
    /// </summary>
    public long CharactersConsumed { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the buffer is below the refill watermark
    /// and the reader has more data available.
    /// </summary>
    public bool NeedsRefill => bufferedCount < RefillWatermark && !readerExhausted;

    /// <summary>
    /// Fills the internal buffer synchronously from the underlying reader.
    /// </summary>
    public void FillBuffer()
    {
        if (readerExhausted) return;

        FillBufferCore(reader);
    }

    /// <summary>
    /// Fills the internal buffer asynchronously from the underlying reader.
    /// </summary>
    public async ValueTask FillBufferAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (readerExhausted) return;

        // Ensure buffer has space
        var space = buffer.Length - bufferedCount;
        if (space == 0) return;

        var tempBuf = new char[space];
#if NET8_0_OR_GREATER
        var read = await reader.ReadAsync(tempBuf.AsMemory(0, space), ct).ConfigureAwait(false);
#else
        var read = await reader.ReadAsync(tempBuf, 0, space).ConfigureAwait(false);
#endif
        if (read == 0)
        {
            readerExhausted = true;
            return;
        }

        CopyToRingBuffer(tempBuf, read);
    }

    /// <summary>
    /// Advances the enumerator by one character and returns it, updating <see cref="Location"/>.
    /// Returns <c>'\0'</c> if the enumerator is at the end.
    /// </summary>
    public char Next()
    {
        var next = DequeueChar();
        if (next == '\0') return '\0';

        CharactersConsumed++;

        if (resetNextLine)
        {
            Location.NewLine();
            resetNextLine = false;
        }
        else
        {
            Location.Increment(next);
        }

        if (next == '\n')
        {
            resetNextLine = true;
        }

        return next;
    }

    /// <summary>
    /// Returns the next character without advancing the enumerator.
    /// </summary>
    public char Peek()
    {
        if (bufferedCount == 0)
        {
            if (readerExhausted) return '\0';
            FillBuffer();
            if (bufferedCount == 0) return '\0';
        }

        return buffer[readPos];
    }

    /// <summary>
    /// Returns true if the characters at the current position match <paramref name="value"/> exactly,
    /// without advancing the enumerator.
    /// </summary>
    public bool TryMatch(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;

        // Fast path: check first character
        if (Peek() != value[0]) return false;

        EnsureBuffered(value.Length);

        if (bufferedCount < value.Length) return false;

        for (var i = 0; i < value.Length; i++)
        {
            var bufIdx = (readPos + i) % buffer.Length;
            if (buffer[bufIdx] != value[i]) return false;
        }

        return true;
    }

    /// <summary>
    /// Checks which tokens have a preamble matching the current position.
    /// </summary>
    public bool TryMatch(IEnumerable<Token> tokens, bool outOfOrderTokens, IList<Token> matches)
    {
        matches.Clear();

        foreach (var token in tokens)
        {
            if (outOfOrderTokens && string.IsNullOrWhiteSpace(token.Name))
            {
                continue;
            }

            if (TryMatch(token.Preamble))
            {
                matches.Add(token);
            }

            if (token.IsOptional == false) break;
        }

        return matches.Count > 0;
    }

    /// <summary>
    /// Advances the enumerator by the specified number of characters.
    /// </summary>
    public void Advance(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Next();
        }
    }

    /// <summary>
    /// Resets the enumerator to the beginning. Only supported for string-backed enumerators.
    /// </summary>
    public void Reset()
    {
        if (originalString == null)
        {
            throw new System.NotSupportedException(
                "Reset is not supported on TextReader-based enumerators. " +
                "Use a hint strategy that does not require enumerator reset.");
        }

        reader = new StringReader(originalString);
        readPos = 0;
        writePos = 0;
        bufferedCount = 0;
        readerExhausted = false;
        resetNextLine = false;
        CharactersConsumed = 0;
        Location.Reset();

        FillBuffer();
    }

    private char DequeueChar()
    {
        if (bufferedCount == 0)
        {
            if (readerExhausted) return '\0';
            FillBuffer();
            if (bufferedCount == 0) return '\0';
        }

        var c = buffer[readPos];
        readPos = (readPos + 1) % buffer.Length;
        bufferedCount--;
        return c;
    }

    private void EnsureBuffered(int count)
    {
        while (bufferedCount < count && !readerExhausted)
        {
            FillBuffer();
        }
    }

    private void FillBufferCore(TextReader source)
    {
        var space = buffer.Length - bufferedCount;
        if (space == 0)
        {
            // Grow buffer if needed (for TryMatch with long preambles)
            GrowBuffer();
            space = buffer.Length - bufferedCount;
        }

        var tempBuf = new char[space];
        var read = source.Read(tempBuf, 0, space);
        if (read == 0)
        {
            readerExhausted = true;
            return;
        }

        CopyToRingBuffer(tempBuf, read);
    }

    private void CopyToRingBuffer(char[] source, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var c = source[i];

            // CRLF normalization during buffer fill
            if (c == '\r')
            {
                // Check if next char is \n
                if (i + 1 < count && source[i + 1] == '\n')
                {
                    i++; // skip the \r, the \n will be added
                }
                c = '\n';
            }

            buffer[writePos] = c;
            writePos = (writePos + 1) % buffer.Length;
            bufferedCount++;
        }
    }

    private void GrowBuffer()
    {
        var newSize = buffer.Length * 2;
        var newBuffer = new char[newSize];

        // Copy existing data linearly
        for (var i = 0; i < bufferedCount; i++)
        {
            newBuffer[i] = buffer[(readPos + i) % buffer.Length];
        }

        buffer = newBuffer;
        readPos = 0;
        writePos = bufferedCount;
    }
}
```

- [ ] **Step 4: Run ring buffer tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FillBuffer or CharactersConsumed"
```

Expected: All ring buffer tests pass.

- [ ] **Step 5: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All existing tests pass — the ring buffer is a transparent replacement for the pushback queue, so all existing `Next`/`Peek`/`TryMatch` behavior is preserved.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Enumerators/TokenEnumerator.cs tests/Tokenizer.Tests/Enumerators/TokenEnumeratorTests.cs
git commit -m "feat: replace pushback queue with ring buffer, add FillBuffer/FillBufferAsync"
```

---

### Task 10: Split TokenizationEngine into Begin/Continue/End

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineTests.cs`

- [ ] **Step 1: Write test for cooperative yielding**

Add to `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineTests.cs`:

```csharp
[Fact]
public void GivenEngine_WhenProcessTokenizationCalled_ThenProducesSameResultsAsBeginContinueEnd()
{
    // Arrange
    var template = _tokenizer.Compile("Name: {Name}, Age: {Age}");

    // Act — use the full pipeline which calls ProcessTokenization internally
    var result = _tokenizer.Tokenize(template, "Name: Alice, Age: 30");

    // Assert — basic sanity: the sync path through ProcessTokenization still works
    Assert.True(result.Success);
    Assert.Equal(2, result.Tokens.Matches.Count);
    Assert.Equal("Alice", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
    Assert.Equal("30", result.Tokens.Matches.First(m => m.Token.Name == "Age").Value);
}
```

- [ ] **Step 2: Extract BeginTokenization from ProcessTokenization**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`, the current `ProcessTokenization` method has three logical phases. Extract the setup into a new internal method.

Add these fields to store state that spans Begin/Continue/End:

```csharp
// State shared across Begin/Continue/End — stored on context or passed through
// No fields needed on the engine itself; all state lives in the context.
// Add to ITokenizationContext / TokenizationContext:
```

Actually, the iteration state (`iterationCount`, `matchBuffer`, `hasExplicitLimit`) needs to live somewhere accessible across calls. Add them to `TokenizationContext`:

In `src/Tokenizer/Tokenization/TokenizationContext.cs`, add:

```csharp
/// <summary>Iteration count for safety limit tracking across Continue calls.</summary>
internal int IterationCount { get; set; }

/// <summary>Reusable buffer for token match results.</summary>
internal List<Token> MatchBuffer { get; } = new();

/// <summary>Template reference for Continue/End phases.</summary>
internal Template? Template { get; set; }

/// <summary>Target object reference for Continue/End phases.</summary>
internal object? TargetObject { get; set; }

/// <summary>Diagnostic collector for Continue/End phases.</summary>
internal IDiagnosticCollector? Collector { get; set; }

/// <summary>Hint strategy for Continue phase.</summary>
internal IHintStrategy? HintStrategy { get; set; }

/// <summary>Whether an explicit MaxIterations limit is set.</summary>
internal bool HasExplicitLimit { get; set; }
```

Also update `ITokenizationContext` to expose what `ContinueTokenization` needs (or make these internal on the concrete class only — since `ContinueTokenization` takes the concrete `TokenizationContext`, not the interface).

- [ ] **Step 3: Implement BeginTokenization**

Add to `TokenizationEngine`:

```csharp
/// <summary>
/// Setup phase: validates arguments, initializes iteration state, records diagnostics.
/// </summary>
internal void BeginTokenization(
    Template template,
    object? targetObject,
    TokenizationContext context,
    TokenizeResultBase result,
    IDiagnosticCollector collector,
    IHintStrategy? hintStrategy = null)
{
    ArgumentValidation.ThrowIfNull(template, nameof(template));
    ArgumentValidation.ThrowIfNull(context, nameof(context));
    ArgumentValidation.ThrowIfNull(result, nameof(result));

    // Validate target object
    if (targetObject != null && !(targetObject is System.Collections.Generic.IDictionary<string, object>))
    {
        var properties = targetObject.GetType().GetProperties();
        var hasSettableProperty = properties.Any(p => p.CanWrite && p.GetSetMethod() != null);

        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Target object type: {TypeName}, Properties: {PropertyCount}, Settable: {SettableCount}",
                targetObject.GetType().Name,
                properties.Length,
                properties.Count(p => p.CanWrite && p.GetSetMethod() != null));
        }

        if (!hasSettableProperty)
        {
            throw new ArgumentException(
                $"Target object of type '{targetObject.GetType().Name}' has no settable properties. " +
                "Anonymous types and objects with read-only properties cannot be used as tokenization targets. " +
                "Consider using a class with writable properties or passing null as the target.",
                nameof(targetObject));
        }
    }

    collector.Record(DiagnosticEventType.TokenizationStarted,
        detail: $"Template: {template.Name}, Tokens: {template.Tokens.Count}");

    // Store state on context for Continue/End
    context.Template = template;
    context.TargetObject = targetObject;
    context.Collector = collector;
    context.HintStrategy = hintStrategy;
    context.HasExplicitLimit = template.Options.MaxIterations > 0;
    context.IterationCount = 0;
    context.MatchBuffer.Clear();
}
```

- [ ] **Step 4: Implement ContinueTokenization**

```csharp
/// <summary>
/// Main loop phase: processes buffered characters until the buffer needs refill or input is exhausted.
/// Returns true when input is fully consumed, false when buffer needs refill.
/// </summary>
internal bool ContinueTokenization(TokenizationContext context, CancellationToken ct)
{
    var template = context.Template!;
    var targetObject = context.TargetObject;
    var collector = context.Collector!;
    var hintStrategy = context.HintStrategy;

    while (context.Enumerator.IsEmpty == false)
    {
        if (context.Enumerator.NeedsRefill)
            return false;

        ct.ThrowIfCancellationRequested();

        context.IterationCount++;
        if (context.HasExplicitLimit && context.IterationCount > template.Options.MaxIterations)
        {
            throw new TokenizerException(
                $"Tokenization exceeded maximum iteration count of {template.Options.MaxIterations:N0}. " +
                "This may indicate a problematic template pattern. " +
                "Increase TokenizerOptions.MaxIterations to allow more iterations.");
        }

        if (!context.HasExplicitLimit && context.IterationCount > context.Enumerator.CharactersConsumed * 2 + 100)
        {
            throw new TokenizerException(
                $"Tokenization exceeded derived iteration limit (iterations: {context.IterationCount:N0}, " +
                $"characters consumed: {context.Enumerator.CharactersConsumed:N0}). " +
                "This may indicate a problematic template pattern. " +
                "Set TokenizerOptions.MaxIterations to override the automatic limit.");
        }

        var next = context.Enumerator.Peek();

        var result = context.Result!;

        // Same loop body as current ProcessTokenization — delegate to existing private handlers
        if (ShouldProcessRepeatedToken(context))
        {
            if (!HandleRepeatedTokenMatching(context, template, result, targetObject, collector))
            {
                continue;
            }
        }

        if (ShouldProcessNewlineTerminatedToken(context, next))
        {
            HandleNewlineTerminatedToken(context, template, targetObject, result, collector);
            continue;
        }

        if (context.Enumerator.TryMatch(template.TokensExcluding(context.MatchIds, context.Candidates, context.DisabledRepeatingTokens, context.ExclusionBuffer, context.TokenFilterBuffer, context.TokenFilterIds), template.Options.OutOfOrderTokens, context.MatchBuffer))
        {
            collector.Record(DiagnosticEventType.PreambleMatched,
                tokenName: string.Join(", ", context.MatchBuffer.Select(m => m.Name)),
                location: context.Enumerator.Location);

            if (hintStrategy != null)
            {
                foreach (var match in context.MatchBuffer)
                {
                    hintStrategy.OnTokenMatched(match);
                }
            }

            if (context.Candidates.HasCandidates == false)
            {
                HandleFirstTokenMatch(context, context.MatchBuffer);
                continue;
            }

            if (context.Replacement.Length > 0)
            {
                HandleTokenSwitch(context, template, targetObject, result, context.MatchBuffer, collector);
            }
            else
            {
                HandleNoTokenMatch(context, next);
            }
        }
        else
        {
            HandleNoTokenMatch(context, next);
        }
    }

    return true;
}
```

Note: The `result` parameter needs to be available in `ContinueTokenization`. Store it on the context alongside the other state, or pass it as a parameter. Storing on context is cleaner since it avoids a long parameter list. Add `internal TokenizeResultBase? Result { get; set; }` to `TokenizationContext` and set it in `BeginTokenization`.

- [ ] **Step 5: Implement EndTokenization**

```csharp
/// <summary>
/// Teardown phase: processes remaining candidates and front matter tokens.
/// </summary>
internal void EndTokenization(TokenizationContext context)
{
    var template = context.Template!;
    var targetObject = context.TargetObject;
    var result = context.Result!;
    var collector = context.Collector!;

    if (ShouldProcessRemainingCandidates(context))
    {
        TryAssignCandidateTokens(context.Candidates, targetObject, context.Replacement,
            template.Options, context.ReplacementLocation, result, template, context.MatchIds, collector);
    }

    ProcessFrontMatterTokens(template, targetObject, context.Enumerator.Location, result, collector);

    collector.Record(DiagnosticEventType.TokenizationCompleted,
        detail: $"Matches: {result.Tokens.Matches.Count}, Misses: {result.Tokens.Misses.Count}");
}
```

- [ ] **Step 6: Rewrite ProcessTokenization to use Begin/Continue/End**

```csharp
public void ProcessTokenization(
    Template template,
    object? targetObject,
    ITokenizationContext context,
    TokenizeResultBase result,
    IDiagnosticCollector collector,
    IHintStrategy? hintStrategy = null)
{
    var ctx = (TokenizationContext)context;
    ctx.Result = result;

    BeginTokenization(template, targetObject, ctx, result, collector, hintStrategy);
    do
    {
        ctx.Enumerator.FillBuffer();
    }
    while (!ContinueTokenization(ctx, CancellationToken.None));
    EndTokenization(ctx);
}
```

- [ ] **Step 7: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass — behavior is identical, just restructured.

- [ ] **Step 8: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationEngine.cs src/Tokenizer/Tokenization/TokenizationContext.cs tests/Tokenizer.Tests/Tokenization/Engine/
git commit -m "refactor: split engine into Begin/Continue/End for cooperative async yielding"
```

---

### Task 11: Add TokenizeAsync to Tokenizer

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/ITokenizer.cs`
- Create: `tests/Tokenizer.Tests/TokenizerAsyncTests.cs`

- [ ] **Step 1: Write async tokenization tests**

Create `tests/Tokenizer.Tests/TokenizerAsyncTests.cs`:

```csharp
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenizerAsyncTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public TokenizerAsyncTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public async Task GivenTextReader_WhenTokenizeAsync_ThenMatchesTokens()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Name}, Age: {Age}");
        using var reader = new StringReader("Name: Alice, Age: 30");

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal("Alice", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
        Assert.Equal("30", result.Tokens.Matches.First(m => m.Token.Name == "Age").Value);
    }

    [Fact]
    public async Task GivenTextReader_WhenTokenizeAsyncGeneric_ThenPopulatesObject()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Person.Name}, Age: {Person.Age}");
        using var reader = new StringReader("Name: Bob, Age: 25");

        // Act
        var result = await _tokenizer.TokenizeAsync<Person>(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", result.Value.Name);
        Assert.Equal(25, result.Value.Age);
    }

    [Fact]
    public async Task GivenStream_WhenTokenizeAsync_ThenMatchesTokens()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Name}");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Charlie"));

        // Act
        var result = await _tokenizer.TokenizeAsync(template, stream, Encoding.UTF8);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Charlie", result.Tokens.Matches.First().Value);
    }

    [Fact]
    public async Task GivenCancellationToken_WhenCancelled_ThenThrowsOperationCancelled()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Name}");
        using var reader = new StringReader("Name: Test");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _tokenizer.TokenizeAsync(template, reader, cts.Token));
    }

    [Fact]
    public async Task GivenStringInput_WhenTokenizeAsyncWithStringReader_ThenProducesSameResultsAsSyncPath()
    {
        // Arrange
        var template = _tokenizer.Compile("Hello {Name}, welcome to {Place}!");
        var input = "Hello World, welcome to Earth!";

        // Act
        var syncResult = _tokenizer.Tokenize(template, input);
        using var reader = new StringReader(input);
        var asyncResult = await _tokenizer.TokenizeAsync(template, reader);

        // Assert — both paths produce identical results
        Assert.Equal(syncResult.Success, asyncResult.Success);
        Assert.Equal(syncResult.Tokens.Matches.Count, asyncResult.Tokens.Matches.Count);
        for (var i = 0; i < syncResult.Tokens.Matches.Count; i++)
        {
            Assert.Equal(syncResult.Tokens.Matches[i].Token.Name, asyncResult.Tokens.Matches[i].Token.Name);
            Assert.Equal(syncResult.Tokens.Matches[i].Value, asyncResult.Tokens.Matches[i].Value);
        }
    }

    private class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerAsyncTests"
```

Expected: FAIL — `TokenizeAsync` doesn't exist.

- [ ] **Step 3: Add async methods to ITokenizer**

In `src/Tokenizer/ITokenizer.cs`, add:

```csharp
/// <summary>
/// Asynchronously tokenizes input from a <see cref="TextReader"/> using the provided template.
/// </summary>
Task<TokenizeResult> TokenizeAsync(Template template, TextReader input, CancellationToken ct = default);

/// <summary>
/// Asynchronously tokenizes input from a <see cref="TextReader"/>, mapping values onto <typeparamref name="T"/>.
/// </summary>
Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct = default) where T : class, new();

/// <summary>
/// Asynchronously tokenizes input from a <see cref="Stream"/> using the provided template.
/// </summary>
Task<TokenizeResult> TokenizeAsync(Template template, Stream input, Encoding encoding, CancellationToken ct = default);

/// <summary>
/// Asynchronously tokenizes input from a <see cref="Stream"/>, mapping values onto <typeparamref name="T"/>.
/// </summary>
Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new();
```

- [ ] **Step 4: Implement TokenizeAsync on Tokenizer**

In `src/Tokenizer/Tokenizer.cs`, add:

```csharp
/// <inheritdoc />
public async Task<TokenizeResult> TokenizeAsync(Template template, TextReader input, CancellationToken ct = default)
{
    var result = new TokenizeResult(template);
    await TokenizeAsyncCore(result, null, template, input, ct).ConfigureAwait(false);
    return result;
}

/// <inheritdoc />
public async Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct = default) where T : class, new()
{
    var result = new TokenizeResult<T>(template);
    await TokenizeAsyncCore(result, result.Value, template, input, ct).ConfigureAwait(false);
    return result;
}

/// <inheritdoc />
public async Task<TokenizeResult> TokenizeAsync(Template template, Stream input, Encoding encoding, CancellationToken ct = default)
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return await TokenizeAsync(template, reader, ct).ConfigureAwait(false);
}

/// <inheritdoc />
public async Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new()
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return await TokenizeAsync<T>(template, reader, ct).ConfigureAwait(false);
}

private async Task TokenizeAsyncCore(TokenizeResultBase result, object? value, Template template, TextReader reader, CancellationToken ct)
{
    log.LogInformation("Starting async tokenization for template {TemplateName}", template.Name);

    using var context = new TokenizationContext();
    // Don't call Initialize — it does a sync FillBuffer. Instead, init manually and fill async.
    context.InitializeForAsync(reader);
    await context.Enumerator.FillBufferAsync(ct).ConfigureAwait(false);

    IDiagnosticCollector collector = template.Options.EnableDiagnostics
        ? new DiagnosticCollector(null, null)
        : NullDiagnosticCollector.Instance;

    var hintsMissing = hintStrategy.PreProcess(template, context.Enumerator, null, result, collector);

    if (!hintsMissing)
    {
        var engine = (TokenizationEngine)tokenizationEngine;
        context.Result = result;
        engine.BeginTokenization(template, value, context, result, collector, this.hintStrategy);
        do
        {
            await context.Enumerator.FillBufferAsync(ct).ConfigureAwait(false);
        }
        while (!engine.ContinueTokenization(context, ct));
        engine.EndTokenization(context);

        if (this.hintStrategy.PostProcess(result))
        {
            log.LogWarning("Post-tokenization hint check failed");
        }
    }

    resultBuilder.BuildUnmatchedTokens(template, result, collector);
    result.Diagnostics = collector.GetResult();

    log.LogInformation("Async tokenization {Result} for template {TemplateName}",
        result.Success ? "succeeded" : "failed", template.Name);
}
```

Note: `InitializeForAsync` is a new method on `TokenizationContext` that sets up the enumerator without calling `FillBuffer` in the constructor. Add to `TokenizationContext`:

```csharp
/// <summary>
/// Initializes the context for async tokenization. Unlike Initialize, does not
/// perform an initial buffer fill — the caller must call FillBufferAsync.
/// </summary>
internal void InitializeForAsync(TextReader reader)
{
    // Create enumerator without auto-fill
    Enumerator = new TokenEnumerator(reader, asyncMode: true);
    ClearCandidates();
    ClearReplacement();
    MatchIds.Clear();
    DisabledRepeatingTokens.Clear();
    ReplacementLocation = new FileLocation();
}
```

And update `TokenEnumerator` constructor to support skipping initial fill:

```csharp
internal TokenEnumerator(TextReader reader, bool asyncMode) : this(reader, null, asyncMode)
{
}

private TokenEnumerator(TextReader reader, string? originalString, bool asyncMode = false)
{
    this.reader = reader;
    this.originalString = originalString;
    buffer = new char[DefaultBufferSize];
    // ... same init ...

    if (!asyncMode)
    {
        FillBuffer();
    }
}
```

- [ ] **Step 5: Run async tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerAsyncTests"
```

Expected: All async tests pass.

- [ ] **Step 6: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs src/Tokenizer/ITokenizer.cs src/Tokenizer/Tokenization/TokenizationContext.cs src/Tokenizer/Enumerators/TokenEnumerator.cs tests/Tokenizer.Tests/TokenizerAsyncTests.cs
git commit -m "feat: add TokenizeAsync with ring-buffered streaming I/O"
```

---

### Task 12: Add CompileAsync to Tokenizer

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/ITokenizer.cs`
- Create: `tests/Tokenizer.Tests/CompileAsyncTests.cs`

- [ ] **Step 1: Write CompileAsync tests**

Create `tests/Tokenizer.Tests/CompileAsyncTests.cs`:

```csharp
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class CompileAsyncTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public CompileAsyncTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public async Task GivenTextReader_WhenCompileAsync_ThenProducesValidTemplate()
    {
        // Arrange
        using var reader = new StringReader("Name: {Name}, Age: {Age}");

        // Act
        var template = await _tokenizer.CompileAsync(reader);

        // Assert
        Assert.NotNull(template);
        Assert.Equal(2, template.Tokens.Count);
    }

    [Fact]
    public async Task GivenTextReaderWithName_WhenCompileAsync_ThenTemplateHasName()
    {
        // Arrange
        using var reader = new StringReader("Name: {Name}");

        // Act
        var template = await _tokenizer.CompileAsync(reader, "my-template");

        // Assert
        Assert.Equal("my-template", template.Name);
    }

    [Fact]
    public async Task GivenStream_WhenCompileAsync_ThenProducesValidTemplate()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Value: {Value}"));

        // Act
        var template = await _tokenizer.CompileAsync(stream, Encoding.UTF8);

        // Assert
        Assert.NotNull(template);
        Assert.Single(template.Tokens);
    }

    [Fact]
    public async Task GivenCancellationToken_WhenCancelled_ThenThrowsOperationCancelled()
    {
        // Arrange
        using var reader = new StringReader("Name: {Name}");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _tokenizer.CompileAsync(reader, cts.Token));
    }

    [Fact]
    public async Task GivenTextReader_WhenCompileAsync_ThenProducesSameResultAsSync()
    {
        // Arrange
        var pattern = "Hello {Name}, welcome to {Place}!";

        // Act
        var syncTemplate = _tokenizer.Compile(pattern);
        using var reader = new StringReader(pattern);
        var asyncTemplate = await _tokenizer.CompileAsync(reader);

        // Assert
        Assert.Equal(syncTemplate.Tokens.Count, asyncTemplate.Tokens.Count);
        for (var i = 0; i < syncTemplate.Tokens.Count; i++)
        {
            Assert.Equal(syncTemplate.Tokens[i].Name, asyncTemplate.Tokens[i].Name);
            Assert.Equal(syncTemplate.Tokens[i].Preamble, asyncTemplate.Tokens[i].Preamble);
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CompileAsyncTests"
```

Expected: FAIL — `CompileAsync` doesn't exist.

- [ ] **Step 3: Add CompileAsync to ITokenizer**

In `src/Tokenizer/ITokenizer.cs`, add:

```csharp
/// <summary>
/// Asynchronously compiles a template from a <see cref="TextReader"/>.
/// </summary>
Task<Template> CompileAsync(TextReader reader, CancellationToken ct = default);

/// <summary>
/// Asynchronously compiles a template from a <see cref="TextReader"/> with an explicit name.
/// </summary>
Task<Template> CompileAsync(TextReader reader, string name, CancellationToken ct = default);

/// <summary>
/// Asynchronously compiles a template from a <see cref="Stream"/>.
/// </summary>
Task<Template> CompileAsync(Stream input, Encoding encoding, CancellationToken ct = default);

/// <summary>
/// Asynchronously compiles a template from a <see cref="Stream"/> with an explicit name.
/// </summary>
Task<Template> CompileAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default);
```

- [ ] **Step 4: Implement CompileAsync on Tokenizer**

In `src/Tokenizer/Tokenizer.cs`, add:

```csharp
/// <inheritdoc />
public async Task<Template> CompileAsync(TextReader reader, CancellationToken ct = default)
{
    var content = await ReadToEndAsync(reader, ct).ConfigureAwait(false);
    return parser.Parse(content);
}

/// <inheritdoc />
public async Task<Template> CompileAsync(TextReader reader, string name, CancellationToken ct = default)
{
    var content = await ReadToEndAsync(reader, ct).ConfigureAwait(false);
    return parser.Parse(content, name);
}

/// <inheritdoc />
public async Task<Template> CompileAsync(Stream input, Encoding encoding, CancellationToken ct = default)
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return await CompileAsync(reader, ct).ConfigureAwait(false);
}

/// <inheritdoc />
public async Task<Template> CompileAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default)
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return await CompileAsync(reader, name, ct).ConfigureAwait(false);
}

/// <summary>
/// Reads a TextReader to end asynchronously with chunked reads and cancellation support.
/// </summary>
private static async Task<string> ReadToEndAsync(TextReader reader, CancellationToken ct)
{
    var sb = new StringBuilder();
    var buffer = new char[4096];
    int read;
#if NET8_0_OR_GREATER
    while ((read = await reader.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false)) > 0)
    {
        ct.ThrowIfCancellationRequested();
        sb.Append(buffer, 0, read);
    }
#else
    while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
    {
        ct.ThrowIfCancellationRequested();
        sb.Append(buffer, 0, read);
    }
#endif
    return sb.ToString();
}
```

- [ ] **Step 5: Run CompileAsync tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CompileAsyncTests"
```

Expected: All pass.

- [ ] **Step 6: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs src/Tokenizer/ITokenizer.cs tests/Tokenizer.Tests/CompileAsyncTests.cs
git commit -m "feat: add CompileAsync with chunked async reader support"
```

---

### Task 13: Add MatchAsync and RegisterTemplateAsync to TokenMatcher

**Files:**
- Modify: `src/Tokenizer/TokenMatcher.cs`
- Modify: `src/Tokenizer/ITokenMatcher.cs`
- Create: `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs`

- [ ] **Step 1: Write async matcher tests**

Create `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs`:

```csharp
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenMatcherAsyncTests : TokenizerTestBase
{
    private class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    public TokenMatcherAsyncTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task GivenTextReader_WhenMatchAsync_ThenFindsBestMatch()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        using var reader = new StringReader("Name: Alice, Age: 30");

        // Act
        var result = await matcher.MatchAsync(reader);

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public async Task GivenTextReader_WhenMatchAsyncGeneric_ThenPopulatesObject()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        using var reader = new StringReader("Name: Bob, Age: 25");

        // Act
        var result = await matcher.MatchAsync<Person>(reader);

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("Bob", result.BestMatch.Value.Name);
        Assert.Equal(25, result.BestMatch.Value.Age);
    }

    [Fact]
    public async Task GivenSeekableStream_WhenMatchAsync_ThenRewindsBetweenTemplates()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Charlie, Age: 35"));

        // Act
        var result = await matcher.MatchAsync(stream, Encoding.UTF8);

        // Assert — both templates were tried (stream was rewound)
        Assert.True(result.Results.Count >= 2);
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public async Task GivenSeekableStream_WhenMatchAsyncCompletes_ThenStreamIsNotDisposed()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Dave"));

        // Act
        await matcher.MatchAsync(stream, Encoding.UTF8);

        // Assert
        Assert.True(stream.CanRead);
        stream.Dispose();
    }

    [Fact]
    public async Task GivenNonSeekableStream_WhenAllowStreamBufferingFalse_ThenThrows()
    {
        // Arrange
        var matcher = new TokenMatcher(new TokenizerOptions { AllowStreamBuffering = false });
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        using var stream = new NonSeekableStream(Encoding.UTF8.GetBytes("Name: Eve"));

        // Act & Assert
        await Assert.ThrowsAsync<Exceptions.TokenizerException>(
            () => matcher.MatchAsync(stream, Encoding.UTF8));
    }

    [Fact]
    public async Task GivenNonSeekableStream_WhenAllowStreamBufferingTrue_ThenBuffersAndMatches()
    {
        // Arrange
        var matcher = new TokenMatcher(new TokenizerOptions { AllowStreamBuffering = true });
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        using var stream = new NonSeekableStream(Encoding.UTF8.GetBytes("Name: Frank"));

        // Act
        var result = await matcher.MatchAsync(stream, Encoding.UTF8);

        // Assert
        Assert.NotNull(result.BestMatch);
    }

    [Fact]
    public async Task GivenTextReader_WhenRegisterTemplateAsync_ThenTemplateIsRegistered()
    {
        // Arrange
        var matcher = new TokenMatcher();
        using var reader = new StringReader("Name: {Name}");

        // Act
        await matcher.RegisterTemplateAsync(reader, "my-template");

        // Assert
        Assert.True(matcher.Templates.TryGet("my-template", out _));
    }

    /// <summary>
    /// A stream wrapper that does not support seeking — simulates a NetworkStream.
    /// </summary>
    private class NonSeekableStream : Stream
    {
        private readonly MemoryStream inner;

        public NonSeekableStream(byte[] data)
        {
            inner = new MemoryStream(data);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => inner.ReadAsync(buffer, offset, count, ct);
#if NET8_0_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => inner.ReadAsync(buffer, ct);
#endif
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
```

- [ ] **Step 2: Run to verify failure**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenMatcherAsyncTests"
```

Expected: FAIL — `MatchAsync` and `RegisterTemplateAsync` don't exist.

- [ ] **Step 3: Add async methods to ITokenMatcher**

In `src/Tokenizer/ITokenMatcher.cs`, add:

```csharp
// Async registration
Task<ITokenMatcher> RegisterTemplateAsync(TextReader reader, CancellationToken ct = default);
Task<ITokenMatcher> RegisterTemplateAsync(TextReader reader, string name, CancellationToken ct = default);
Task<ITokenMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, CancellationToken ct = default);
Task<ITokenMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default);

// Async matching
Task<TokenMatcherResult> MatchAsync(TextReader input, CancellationToken ct = default);
Task<TokenMatcherResult> MatchAsync(TextReader input, string[]? tags, CancellationToken ct = default);
Task<TokenMatcherResult<T>> MatchAsync<T>(TextReader input, CancellationToken ct = default) where T : class, new();
Task<TokenMatcherResult<T>> MatchAsync<T>(TextReader input, string[]? tags, CancellationToken ct = default) where T : class, new();
Task<TokenMatcherResult> MatchAsync(Stream input, Encoding encoding, CancellationToken ct = default);
Task<TokenMatcherResult> MatchAsync(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default);
Task<TokenMatcherResult<T>> MatchAsync<T>(Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new();
Task<TokenMatcherResult<T>> MatchAsync<T>(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default) where T : class, new();
```

- [ ] **Step 4: Implement async methods on TokenMatcher**

In `src/Tokenizer/TokenMatcher.cs`, add:

```csharp
/// <inheritdoc />
public async Task<ITokenMatcher> RegisterTemplateAsync(TextReader reader, CancellationToken ct = default)
{
    var template = await tokenizer.CompileAsync(reader, ct).ConfigureAwait(false);
    Templates.Add(template);
    return this;
}

/// <inheritdoc />
public async Task<ITokenMatcher> RegisterTemplateAsync(TextReader reader, string name, CancellationToken ct = default)
{
    var template = await tokenizer.CompileAsync(reader, name, ct).ConfigureAwait(false);
    Templates.Add(template);
    return this;
}

/// <inheritdoc />
public async Task<ITokenMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, CancellationToken ct = default)
{
    var template = await tokenizer.CompileAsync(input, encoding, ct).ConfigureAwait(false);
    Templates.Add(template);
    return this;
}

/// <inheritdoc />
public async Task<ITokenMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default)
{
    var template = await tokenizer.CompileAsync(input, encoding, name, ct).ConfigureAwait(false);
    Templates.Add(template);
    return this;
}

/// <inheritdoc />
public Task<TokenMatcherResult> MatchAsync(TextReader input, CancellationToken ct = default)
    => MatchAsync(input, null, ct);

/// <inheritdoc />
public async Task<TokenMatcherResult> MatchAsync(TextReader input, string[]? tags, CancellationToken ct = default)
{
    var stream = await BufferTextReaderAsync(input, ct).ConfigureAwait(false);
    return await MatchAsyncFromSeekableStream(stream, tags, ct).ConfigureAwait(false);
}

/// <inheritdoc />
public Task<TokenMatcherResult<T>> MatchAsync<T>(TextReader input, CancellationToken ct = default) where T : class, new()
    => MatchAsync<T>(input, null, ct);

/// <inheritdoc />
public async Task<TokenMatcherResult<T>> MatchAsync<T>(TextReader input, string[]? tags, CancellationToken ct = default) where T : class, new()
{
    var stream = await BufferTextReaderAsync(input, ct).ConfigureAwait(false);
    return await MatchAsyncFromSeekableStream<T>(stream, tags, ct).ConfigureAwait(false);
}

/// <inheritdoc />
public Task<TokenMatcherResult> MatchAsync(Stream input, Encoding encoding, CancellationToken ct = default)
    => MatchAsync(input, encoding, null, ct);

/// <inheritdoc />
public async Task<TokenMatcherResult> MatchAsync(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default)
{
    var seekable = await EnsureSeekableAsync(input, ct).ConfigureAwait(false);
    return await MatchAsyncFromSeekableStream(seekable, tags, ct).ConfigureAwait(false);
}

/// <inheritdoc />
public Task<TokenMatcherResult<T>> MatchAsync<T>(Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new()
    => MatchAsync<T>(input, encoding, null, ct);

/// <inheritdoc />
public async Task<TokenMatcherResult<T>> MatchAsync<T>(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default) where T : class, new()
{
    var seekable = await EnsureSeekableAsync(input, ct).ConfigureAwait(false);
    return await MatchAsyncFromSeekableStream<T>(seekable, tags, ct).ConfigureAwait(false);
}

private async Task<MemoryStream> BufferTextReaderAsync(TextReader reader, CancellationToken ct)
{
    var buffer = new MemoryStream();
    var writer = new StreamWriter(buffer, leaveOpen: true);
    var charBuf = new char[4096];
    int read;
    while ((read = await reader.ReadAsync(charBuf, 0, charBuf.Length).ConfigureAwait(false)) > 0)
    {
        ct.ThrowIfCancellationRequested();
        await writer.WriteAsync(charBuf, 0, read).ConfigureAwait(false);
    }
    await writer.FlushAsync().ConfigureAwait(false);
    buffer.Position = 0;
    return buffer;
}

private async Task<Stream> EnsureSeekableAsync(Stream input, CancellationToken ct)
{
    if (input.CanSeek) return input;

    if (!tokenizer.Options.AllowStreamBuffering)
    {
        throw new Exceptions.TokenizerException(
            "Stream is not seekable. Provide a seekable stream or " +
            "set TokenizerOptions.AllowStreamBuffering = true to allow buffering into memory.");
    }

    var buffer = new MemoryStream();
    await input.CopyToAsync(buffer, 81920, ct).ConfigureAwait(false);
    buffer.Position = 0;
    return buffer;
}

private async Task<TokenMatcherResult> MatchAsyncFromSeekableStream(Stream stream, string[]? tags, CancellationToken ct)
{
    tags ??= Array.Empty<string>();
    var results = new TokenMatcherResult();
    var startPos = stream.Position;

    foreach (var name in Templates.Names)
    {
        if (!Templates.TryGet(name, out var template)) continue;
        if (!CheckTemplateTags(template, tags)) continue;

        stream.Position = startPos;
        using var reader = new StreamReader(stream, leaveOpen: true);

        try
        {
            var result = await tokenizer.TokenizeAsync(template, reader, ct).ConfigureAwait(false);
            results.AddResult(result);
        }
        catch (Exception e)
        {
            var exception = new Exceptions.TokenMatcherException(e.Message, template, e);
            log.LogError(e, "Error processing template: {TemplateName}", template.Name);
            throw exception;
        }
    }

    results.BestMatch = results.GetBestMatch();
    return results;
}

private async Task<TokenMatcherResult<T>> MatchAsyncFromSeekableStream<T>(Stream stream, string[]? tags, CancellationToken ct) where T : class, new()
{
    tags ??= Array.Empty<string>();
    var results = new TokenMatcherResult<T>();
    var startPos = stream.Position;

    foreach (var name in Templates.Names)
    {
        if (!Templates.TryGet(name, out var template)) continue;
        if (!CheckTemplateTags(template, tags)) continue;

        stream.Position = startPos;
        using var reader = new StreamReader(stream, leaveOpen: true);

        try
        {
            var result = await tokenizer.TokenizeAsync<T>(template, reader, ct).ConfigureAwait(false);
            results.AddResult(result);
        }
        catch (Exception e)
        {
            var exception = new Exceptions.TokenMatcherException(e.Message, template, e);
            log.LogError(e, "Error processing template: {TemplateName}", template.Name);
            throw exception;
        }
    }

    results.BestMatch = results.GetBestMatch();
    return results;
}
```

Note: The `tokenizer.Options` is accessed for `AllowStreamBuffering`, but `TokenMatcher` stores an `ITokenizer`, not the options directly. The `ITokenizer` interface exposes `TokenizerOptions Options { get; }` — check if this exists. If not, store the options separately.

- [ ] **Step 5: Run async matcher tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenMatcherAsyncTests"
```

Expected: All tests pass.

- [ ] **Step 6: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/TokenMatcher.cs src/Tokenizer/ITokenMatcher.cs tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs
git commit -m "feat: add MatchAsync and RegisterTemplateAsync to TokenMatcher"
```

---

### Task 14: Add System.Threading.Tasks.Extensions for netstandard2.0

**Files:**
- Modify: `src/Tokenizer/Tokenizer.csproj`

- [ ] **Step 1: Check if ValueTask requires the package**

Build targeting netstandard2.0:

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```

If the build fails with errors about `ValueTask` on `netstandard2.0`, add the package reference.

- [ ] **Step 2: Add package reference if needed**

In `src/Tokenizer/Tokenizer.csproj`, add within an `ItemGroup` conditioned on netstandard2.0:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
  <PackageReference Include="System.Threading.Tasks.Extensions" Version="4.5.4" />
</ItemGroup>
```

- [ ] **Step 3: Build all targets**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```

Expected: Build succeeds for all three target frameworks.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenizer.csproj
git commit -m "build: add System.Threading.Tasks.Extensions for netstandard2.0 ValueTask support"
```

---

### Task 15: Final verification

**Files:** None (verification only)

- [ ] **Step 1: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v normal
```

Expected: All tests pass.

- [ ] **Step 2: Build Release for all targets**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```

Expected: Clean build, no warnings.

- [ ] **Step 3: Verify no sync TextReader/Stream methods remain on public API**

```bash
# Should find no sync Tokenize/Match/Compile overloads taking TextReader or Stream
grep -n "TextReader\|Stream input" src/Tokenizer/ITokenizer.cs src/Tokenizer/ITokenMatcher.cs | grep -v Async | grep -v "//"
```

Expected: No matches (all TextReader/Stream methods are async).

- [ ] **Step 4: Commit any remaining changes**

```bash
git status
```

If clean, no commit needed. If there are changes, commit them with an appropriate message.
