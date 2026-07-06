# Post-Refactoring Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 8 code quality issues identified during review of the TokenizationEngine decomposition.

**Architecture:** All changes are localized fixes — moving code to proper files, removing dead code, extracting shared logic, strengthening tests, and making patterns consistent. No new abstractions or public API changes.

**Tech Stack:** C# / .NET, xUnit

---

### Task 1: Move `ArgumentValidation` to its own file

**Files:**
- Create: `src/Tokenizer/Tokenization/ArgumentValidation.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs:1-20` (remove the class)

- [ ] **Step 1: Create `ArgumentValidation.cs`**

```csharp
namespace Tokens.Tokenization;

internal static class ArgumentValidation
{
#if NETSTANDARD2_0
    public static void ThrowIfNull(object argument, string paramName)
    {
        if (argument == null) throw new ArgumentNullException(paramName);
    }
#else
    public static void ThrowIfNull(object argument, string paramName)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }
#endif
}
```

- [ ] **Step 2: Remove `ArgumentValidation` from `TokenizationEngine.cs`**

Remove lines 1-20 (the `ArgumentValidation` class and its using directives that are only needed by it). The file should start with `using Microsoft.Extensions.Logging;`.

After removal, `TokenizationEngine.cs` should look like:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Thin orchestrator that validates inputs and creates tokenization sessions.
/// All tokenization logic lives in <see cref="TokenizationSession"/> and its sub-components.
/// </summary>
internal class TokenizationEngine : ITokenizationEngine
{
    // ... rest unchanged
}
```

- [ ] **Step 3: Build and run tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass. `ArgumentValidation` is resolved from the new file.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenization/ArgumentValidation.cs src/Tokenizer/Tokenization/TokenizationEngine.cs
git commit -m "refactor: move ArgumentValidation to its own file"
```

---

### Task 2: Remove `IDisposable` from `TokenizationContext`

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationContext.cs:12-14,133-141`
- Modify: `src/Tokenizer/Tokenizer.cs:168,338`

- [ ] **Step 1: Remove `IDisposable` from `TokenizationContext`**

In `TokenizationContext.cs`:
1. Remove `: IDisposable` from the class declaration (line 12)
2. Remove the `_disposed` field (line 14)
3. Remove the entire `Dispose()` method (lines 136-141)

The class declaration becomes:

```csharp
internal sealed class TokenizationContext
```

- [ ] **Step 2: Remove `using` statements from `Tokenizer.cs`**

In `TokenizeCore` (line 168), change:
```csharp
using (var context = new TokenizationContext())
{
```
to:
```csharp
var context = new TokenizationContext();
{
```

Note: keep the braces — they scope the `log.BeginScope` block that wraps this code. The opening `{` on the next line and its closing `}` at line 230 stay.

In `TokenizeAsyncCore` (line 338), change:
```csharp
using var context = new TokenizationContext();
```
to:
```csharp
var context = new TokenizationContext();
```

- [ ] **Step 3: Build and run tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationContext.cs src/Tokenizer/Tokenizer.cs
git commit -m "refactor: remove no-op IDisposable from TokenizationContext"
```

---

### Task 3: Merge consecutive debug-log guards

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs:150-165,329-335`

- [ ] **Step 1: Merge the two guards in `TokenizeCore`**

Replace lines 150-165:

```csharp
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Starting tokenization for template {TemplateName}", template.Name);
            }
            if (log.IsEnabled(LogLevel.Debug))
            {
                if (rawInput != null)
                {
                    log.LogDebug("Template has {TokenCount} tokens, input length is {InputLength}",
                        template.Tokens.Count, rawInput.Length);
                }
                else
                {
                    log.LogDebug("Template has {TokenCount} tokens", template.Tokens.Count);
                }
            }
```

with:

```csharp
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Starting tokenization for template {TemplateName}", template.Name);
                if (rawInput != null)
                {
                    log.LogDebug("Template has {TokenCount} tokens, input length is {InputLength}",
                        template.Tokens.Count, rawInput.Length);
                }
                else
                {
                    log.LogDebug("Template has {TokenCount} tokens", template.Tokens.Count);
                }
            }
```

- [ ] **Step 2: Merge the two guards in `TokenizeAsyncCore`**

Replace lines 329-335:

```csharp
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Starting async tokenization for template {TemplateName}", template.Name);
            }
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Template has {TokenCount} tokens", template.Tokens.Count);
            }
```

with:

```csharp
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Starting async tokenization for template {TemplateName}", template.Name);
                log.LogDebug("Template has {TokenCount} tokens", template.Tokens.Count);
            }
```

- [ ] **Step 3: Build and run tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs
git commit -m "style: merge consecutive debug-log guards in Tokenizer"
```

---

### Task 4: Guard diagnostic recording in `FrontMatterProcessor`

**Files:**
- Modify: `src/Tokenizer/Tokenization/FrontMatterProcessor.cs:27-29,37-38`

- [ ] **Step 1: Add `IsEnabled` guards**

Replace lines 25-39:

```csharp
            if (token.Assign(targetObject, string.Empty, template.Options, location, out var assignedValue, collector))
            {
                collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                    tokenName: token.Name, tokenId: token.Id,
                    value: assignedValue?.ToString());
                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(token, assignedValue, token.Location);
                }
            }
            else
            {
                collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                    tokenName: token.Name, tokenId: token.Id);
            }
```

with:

```csharp
            if (token.Assign(targetObject, string.Empty, template.Options, location, out var assignedValue, collector))
            {
                if (collector.IsEnabled)
                {
                    collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                        tokenName: token.Name, tokenId: token.Id,
                        value: assignedValue?.ToString());
                }
                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(token, assignedValue, token.Location);
                }
            }
            else
            {
                if (collector.IsEnabled)
                {
                    collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                        tokenName: token.Name, tokenId: token.Id);
                }
            }
```

- [ ] **Step 2: Build and run tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Tokenization/FrontMatterProcessor.cs
git commit -m "fix: guard diagnostic recording in FrontMatterProcessor with IsEnabled"
```

---

### Task 5: Remove dead `RouteNext` return value

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenMatchRouter.cs:34,44-46,53,85,97,101,108,110`

- [ ] **Step 1: Change `RouteNext` signature to `void`**

Change line 34 from:

```csharp
    public bool RouteNext(TokenizationContext context)
```

to:

```csharp
    public void RouteNext(TokenizationContext context)
```

- [ ] **Step 2: Update the doc comment**

Replace lines 29-33:

```csharp
    /// <summary>
    /// Examines the next character in the input and routes to the appropriate handler.
    /// Returns false if the repeated-token path cleared candidates (caller should continue the loop).
    /// Returns true for all other paths.
    /// </summary>
```

with:

```csharp
    /// <summary>
    /// Examines the next character in the input and routes to the appropriate handler.
    /// </summary>
```

- [ ] **Step 3: Replace all `return false` / `return true` with `return` / nothing**

Line 44-46 — the repeat path early-return:
```csharp
            if (!candidateProcessor.HandleRepeat(context))
            {
                return false;
            }
```
becomes:
```csharp
            if (!candidateProcessor.HandleRepeat(context))
            {
                return;
            }
```

Line 53 (`return true` after newline handling): remove entirely.

Line 85 (`return true` after first token found): remove entirely.

Line 110 (`return true` at end of method): remove entirely.

The final `else` block (lines 104-108) stays but loses its trailing `return true`:
```csharp
        else
        {
            context.Replacement.Append(next);
            context.Enumerator.Next();
        }
```

- [ ] **Step 4: Build and run tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass. The caller in `TokenizationSession.ProcessChunk` (line 124) already calls `router.RouteNext(context)` without using the return value.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenMatchRouter.cs
git commit -m "refactor: remove unused bool return value from RouteNext"
```

---

### Task 6: Extract `FinalizeTokenization` from `Tokenizer.cs`

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs:194-229,378-409`

- [ ] **Step 1: Add the `FinalizeTokenization` method**

Add the following private method to `Tokenizer.cs`, after `TokenizeCore`:

```csharp
    private void FinalizeTokenization(
        TokenizeResultBase result, Template template,
        IDiagnosticCollector collector, string? rawInput)
    {
        resultBuilder.BuildUnmatchedTokens(template, result, collector);

        var requiredMissingCount = result.Tokens.Misses.Count(t => t.IsRequired);
        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Tokenization complete: {MatchCount} matches, {MissCount} misses, {RequiredMissing} required missing",
                result.Tokens.Matches.Count, result.Tokens.Misses.Count, requiredMissingCount);
        }

        if (requiredMissingCount > 0)
        {
            log.LogWarning("{RequiredMissing} required tokens were missing", requiredMissingCount);
        }

        result.Diagnostics = collector.GetResult();

        if (result.Diagnostics != null)
        {
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("{Verdict}", result.Diagnostics.Summary.Verdict);
            }
            foreach (var issue in result.Diagnostics.Summary.Issues)
            {
                log.LogWarning("Token '{TokenName}': {Description}", issue.TokenName, issue.Description);
                if (issue.Hint != null)
                {
                    log.LogWarning("  → Hint: {Hint}", issue.Hint);
                }
            }
            if (rawInput != null && log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
            }
        }
    }
```

- [ ] **Step 2: Replace the sync post-processing block in `TokenizeCore`**

Replace lines 194-229 (from `// Build unmatched tokens collection` through the closing of the diagnostics block) with:

```csharp
                FinalizeTokenization(result, template, collector, rawInput);
```

- [ ] **Step 3: Replace the async post-processing block in `TokenizeAsyncCore`**

Replace lines 378-409 (from `// Build unmatched tokens collection` through the end of the diagnostics block) with:

```csharp
            FinalizeTokenization(result, template, collector, null);
```

Note: async path passes `null` for `rawInput` because the full input string is not available during streaming. This matches the current behavior where the async path's diagnostics block has no `rawInput != null` alignment rendering.

- [ ] **Step 4: Remove the stale comment in `TokenizeAsyncCore`**

Remove lines 313-316:
```csharp
    // TokenizeAsyncCore intentionally diverges from TokenizeCore: async uses session.RunAsync
    // with cooperative buffer refills, lacks rawInput for diagnostics alignment, and
    // adds cancellation-aware exception handling. These structural differences make shared
    // helper extraction awkward without introducing tangled abstractions.
```

This comment is no longer accurate — we just extracted the shared helper.

- [ ] **Step 5: Build and run tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass. Behavior is identical.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs
git commit -m "refactor: extract FinalizeTokenization to deduplicate sync/async post-processing"
```

---

### Task 7: Add sync-path `MaxInputLength` check in `TokenizationSession.Run`

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationSession.cs:48-59`
- Test: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationSessionTests.cs`

- [ ] **Step 1: Write the failing test**

Add the following test to `TokenizationSessionTests.cs`:

```csharp
    [Fact]
    public void GivenMaxInputLengthExceeded_WhenRunCalledWithReader_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxInputLength = 10 };
        var parser = new TemplateCompiler(options);
        var template = parser.Compile("Name: {Name}").Template;
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var engine = new TokenizationEngine();

        // Input exceeds MaxInputLength of 10
        var input = "Name: This is a very long input string that exceeds the limit";
        context.Initialize(new System.IO.StringReader(input));

        var session = engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() => session.Run(context));
        Assert.Contains("exceeds maximum allowed length", ex.Message);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenMaxInputLengthExceeded_WhenRunCalledWithReader_ThenThrowsTokenizerException"`

Expected: FAIL — the sync `Run` path does not check `MaxInputLength`.

- [ ] **Step 3: Add the `MaxInputLength` check to `Run`**

Replace the `Run` method (lines 48-59):

```csharp
    public void Run(TokenizationContext context)
    {
        Initialize(context);

        do
        {
            context.Enumerator.FillBuffer();
        }
        while (!ProcessChunk(context, CancellationToken.None));

        Finalize(context);
    }
```

with:

```csharp
    public void Run(TokenizationContext context)
    {
        Initialize(context);

        do
        {
            context.Enumerator.FillBuffer();

            if (template.Options.MaxInputLength > 0 &&
                context.Enumerator.TotalCharactersSeen > template.Options.MaxInputLength)
            {
                throw new TokenizerException(
                    $"Input length exceeds maximum allowed length of {template.Options.MaxInputLength:N0}. " +
                    "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
            }
        }
        while (!ProcessChunk(context, CancellationToken.None));

        Finalize(context);
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenMaxInputLengthExceeded_WhenRunCalledWithReader_ThenThrowsTokenizerException"`

Expected: PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationSession.cs tests/Tokenizer.Tests/Tokenization/Engine/TokenizationSessionTests.cs
git commit -m "fix: add MaxInputLength check to sync TokenizationSession.Run path"
```

---

### Task 8: Strengthen weak test assertions

**Files:**
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineStateTests.cs:131-150,169-182`
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineTokenMatchingTests.cs:332-351`
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs:79-90`

- [ ] **Step 1: Fix the backtracking test**

In `TokenizationEngineStateTests.cs`, replace the `GivenContext_WhenBacktracking_ThenRestoresPreviousState` test (lines 131-150):

```csharp
    [Fact]
    public void GivenContext_WhenBacktracking_ThenRestoresPreviousState()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Test{Name}").Template;

        var context = new TokenizationContext();
        var input = "Test Value";
        context.Initialize(new System.IO.StringReader(input));
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act - Exercise the engine through the public interface
        var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert — token should capture the value after the preamble "Test"
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Name", result.Tokens.Matches[0].Token.Name);
        Assert.Equal(" Value", result.Tokens.Matches[0].Value);
    }
```

- [ ] **Step 2: Fix the disabled repeating token test**

In `TokenizationEngineStateTests.cs`, replace the `GivenRepeatingToken_WhenDisabled_ThenNoLongerMatches` test (lines 169-182). The current test only exercises `HashSet.Add`/`Contains` — replace with an actual engine test:

```csharp
    [Fact]
    public void GivenRepeatingToken_WhenGapInInput_ThenStopsRepeating()
    {
        // Arrange — the # modifier means stop repeating on blank line gap
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Item: {Item*#}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Two items, then a blank line gap, then a third item
        var input = "Item: Apple\nItem: Banana\n\nItem: Cherry";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert — should match the first two items but stop at the gap
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal("Apple", result.Tokens.Matches[0].Value);
        Assert.Equal("Banana", result.Tokens.Matches[1].Value);
    }
```

- [ ] **Step 3: Fix the out-of-order disabled test**

In `TokenizationEngineTokenMatchingTests.cs`, replace the `GivenTokensInDifferentOrder_WhenOutOfOrderDisabled_ThenMatchesInOrder` test (lines 332-351):

```csharp
    [Fact]
    public void GivenTokensInDifferentOrder_WhenOutOfOrderDisabled_ThenMatchesInOrder()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions { OutOfOrderTokens = false });
        var template = parser.Compile("Age: {Age}\nName: {Name}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act - Provide input in reverse order (Name before Age)
        var input = "Name: John\nAge: 25";
        context.Initialize(new System.IO.StringReader(input));
        var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert - With out-of-order disabled, the engine expects Age first.
        // Since the input has Name first, fewer tokens should match than the
        // out-of-order-enabled test above.
        var enabledTest = GivenTokensInDifferentOrder_WhenOutOfOrderEnabled_MatchCount();
        Assert.True(result.Tokens.Matches.Count < enabledTest,
            $"Out-of-order disabled should match fewer tokens than enabled ({enabledTest}), " +
            $"but got {result.Tokens.Matches.Count}");
    }
```

Wait — this creates a coupling between tests. Better approach: just assert the concrete expected behavior directly.

```csharp
    [Fact]
    public void GivenTokensInDifferentOrder_WhenOutOfOrderDisabled_ThenMissesOutOfOrderTokens()
    {
        // Arrange — template expects Age then Name, in that order
        var parser = new TemplateCompiler(new TokenizerOptions { OutOfOrderTokens = false });
        var template = parser.Compile("Age: {Age}\nName: {Name}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act — input has Name first, then Age (reversed from template order)
        var input = "Name: John\nAge: 25";
        context.Initialize(new System.IO.StringReader(input));
        var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert — strict ordering means the engine can't find Age (it appears
        // after Name in input, but the template expects it before Name).
        // Only Name should match because its preamble appears in the input
        // after the engine has skipped past the Age preamble position.
        Assert.True(result.Tokens.Matches.Count < 2,
            $"With out-of-order disabled and reversed input, expected fewer than 2 matches " +
            $"but got {result.Tokens.Matches.Count}");
    }
```

- [ ] **Step 4: Delete the duplicate test in `TokenizationEngineInternalTests.cs`**

Remove the `GivenRepeatingToken_WhenValueIsAssignable_ThenTokenIsMatched` test (lines 79-90). It's identical to `GivenRepeatingToken_WhenInputDoesNotMatchRepeat_ThenBacktracks` (lines 20-32): same template `"test: {Name}"`, same input `"test: hello"`, same assertions.

- [ ] **Step 5: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass. If the new assertions in Steps 1-3 fail, the actual engine behavior differs from expectations — investigate and adjust assertions to match real behavior rather than weakening them back to `Assert.NotNull`.

- [ ] **Step 6: Commit**

```bash
git add tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineStateTests.cs tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineTokenMatchingTests.cs tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs
git commit -m "test: strengthen weak assertions and remove duplicate test"
```
