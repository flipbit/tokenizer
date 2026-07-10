# Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Address all 41 issues from the diagnostics branch code review, organized into 8 themed tasks.

**Architecture:** Each task targets a cohesive set of changes within one "theme" from the design spec. Tasks are ordered by dependency: Theme 1 (enum split) goes first since it changes the foundational `DiagnosticEventType`. Themes 2-6 are independent of each other. Themes 7-8 are cleanup/tests that go last.

**Tech Stack:** C# / .NET 10.0 / xUnit / NSubstitute

## Global Constraints

- Target frameworks: .NET Standard 2.0, .NET 8.0, .NET 10.0
- Root namespace: `Tokens` (not `Tokenizer`)
- Braces: Allman style
- Private fields: `_camelCase`
- Test naming: `GivenScenario_WhenAction_ThenResult()`
- All tests use Arrange / Act / Assert comments
- Build: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
- Test: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
- No `#region` blocks
- Conditional compilation required for .NET 8.0+ features with .NET Standard 2.0 fallback

---

### Task 1: Split DiagnosticEventType Enum + Compilation Diagnostics (H4, H7)

**Files:**
- Create: `src/Tokenizer/Diagnostics/CompilationEventType.cs`
- Create: `src/Tokenizer/Diagnostics/CompilationEvent.cs`
- Modify: `src/Tokenizer/Diagnostics/DiagnosticEventType.cs` — remove 8 compilation members
- Modify: `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` — add `RecordCompilation` method
- Modify: `src/Tokenizer/Diagnostics/CompilationDiagnosticCollector.cs` — implement `RecordCompilation`, no-op `Record`
- Modify: `src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs` — add no-op `RecordCompilation`
- Modify: `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs` — add no-op `RecordCompilation`
- Modify: `src/Tokenizer/Diagnostics/CompilationDiagnostics.cs` — change `Events` type to `IReadOnlyList<CompilationEvent>`
- Modify: `src/Tokenizer/Compilation/TemplateCompiler.cs` — attach diagnostics in catch-all
- Modify: `src/Tokenizer/Compilation/Binders/HintBinder.cs` — switch to `RecordCompilation`
- Modify: `src/Tokenizer/Compilation/Binders/TagBinder.cs` — switch to `RecordCompilation`
- Modify: `src/Tokenizer/Compilation/Binders/TokenFactory.cs` — switch to `RecordCompilation`
- Modify: `src/Tokenizer/Compilation/Binders/OptionApplier.cs` — switch to `RecordCompilation`
- Modify: `src/Tokenizer/Compilation/Binders/RepeatingTokenLinker.cs` — switch to `RecordCompilation`
- Modify: `src/Tokenizer/Compilation/Binders/DecoratorBinder.cs` — switch to `RecordCompilation`
- Test: `tests/Tokenizer.Tests/Diagnostics/CompilationDiagnosticsTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs`
- Test: `tests/Tokenizer.Tests/Compilation/Binders/HintBinderTests.cs`
- Test: `tests/Tokenizer.Tests/Compilation/Binders/TagBinderTests.cs`
- Test: `tests/Tokenizer.Tests/Compilation/Binders/OptionApplierTests.cs`
- Test: `tests/Tokenizer.Tests/Compilation/Binders/RepeatingTokenLinkerTests.cs`
- Test: `tests/Tokenizer.Tests/Compilation/CompilationResultTests.cs`

**Interfaces:**
- Produces: `CompilationEventType` enum, `CompilationEvent` class, `IDiagnosticCollector.RecordCompilation()` method

- [ ] **Step 1: Create `CompilationEventType` enum**

Create `src/Tokenizer/Diagnostics/CompilationEventType.cs`:

```csharp
namespace Tokens.Diagnostics;

/// <summary>
/// Identifies the type of event recorded during template compilation.
/// </summary>
public enum CompilationEventType
{
    /// <summary>
    /// A hint was added to the template during compilation.
    /// </summary>
    HintAdded,

    /// <summary>
    /// A tag was added to the template during compilation.
    /// </summary>
    TagAdded,

    /// <summary>
    /// A token was created from a token definition during compilation.
    /// </summary>
    TokenCreated,

    /// <summary>
    /// A template-level option was applied to a token during compilation.
    /// </summary>
    OptionApplied,

    /// <summary>
    /// A decorator (transformer or validator) was applied to a token during compilation.
    /// </summary>
    DecoratorApplied,

    /// <summary>
    /// A concatenation decorator was applied to a token during compilation.
    /// </summary>
    ConcatenationApplied,

    /// <summary>
    /// A repeating token was linked to its non-repeating counterpart during compilation.
    /// </summary>
    RepeatingTokenLinked,

    /// <summary>
    /// Template compilation has completed.
    /// </summary>
    CompilationCompleted,
}
```

- [ ] **Step 2: Create `CompilationEvent` class**

Create `src/Tokenizer/Diagnostics/CompilationEvent.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// A single event recorded during template compilation.
/// </summary>
public sealed class CompilationEvent
{
    /// <summary>
    /// The type of compilation event.
    /// </summary>
    public CompilationEventType Type { get; init; }

    /// <summary>
    /// The name of the token this event relates to, or null for
    /// events not specific to a single token.
    /// </summary>
    public string? TokenName { get; init; }

    /// <summary>
    /// The unique ID of the token within its template, or null
    /// for events not specific to a single token.
    /// </summary>
    public int? TokenId { get; init; }

    /// <summary>
    /// The position in the source text where this event occurred.
    /// </summary>
    public FileLocation? Location { get; init; }

    /// <summary>
    /// The value associated with this event.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Human-readable explanation providing additional context.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// The name of the decorator involved, or null for non-decorator events.
    /// </summary>
    public string? DecoratorName { get; init; }

    /// <summary>
    /// The parameters passed to the decorator, or null.
    /// </summary>
    public string[]? DecoratorArgs { get; init; }
}
```

- [ ] **Step 3: Remove compilation members from `DiagnosticEventType`**

In `src/Tokenizer/Diagnostics/DiagnosticEventType.cs`, remove these 8 members and their doc comments: `HintAdded`, `TagAdded`, `TokenCreated`, `OptionApplied`, `DecoratorApplied`, `ConcatenationApplied`, `RepeatingTokenLinked`, `CompilationCompleted`.

- [ ] **Step 4: Add `RecordCompilation` to `IDiagnosticCollector`**

In `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`, add after the existing `Record` method:

```csharp
    /// <summary>
    /// Records a compilation diagnostic event. Implementations may discard the event
    /// (NullDiagnosticCollector, RuntimeDiagnosticCollector) or store it (CompilationDiagnosticCollector).
    /// </summary>
    public void RecordCompilation(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);
```

Also fix the stale doc comment on `Record` (L2): change `"store it (DiagnosticCollector)"` to `"store it (RuntimeDiagnosticCollector)"`.

- [ ] **Step 5: Update `CompilationDiagnostics` to use `CompilationEvent`**

In `src/Tokenizer/Diagnostics/CompilationDiagnostics.cs`, change from `List<DiagnosticEvent>` to `List<CompilationEvent>`:

```csharp
namespace Tokens.Diagnostics;

public sealed class CompilationDiagnostics
{
    private readonly List<CompilationEvent> _events;

    internal CompilationDiagnostics()
    {
        _events = new List<CompilationEvent>();
    }

    public IReadOnlyList<CompilationEvent> Events => _events;

    internal void AddEvent(CompilationEvent evt) => _events.Add(evt);
}
```

- [ ] **Step 6: Update `CompilationDiagnosticCollector`**

Replace the `Record` method body with a no-op (empty body). Rename the current `Record` implementation to `RecordCompilation`, changing `DiagnosticEvent`/`DiagnosticEventType` to `CompilationEvent`/`CompilationEventType`:

```csharp
    public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
    }

    public void RecordCompilation(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
        _compilationDiagnostics.AddEvent(new CompilationEvent
        {
            Type = type,
            TokenName = tokenName,
            TokenId = tokenId,
            Location = location?.Clone(),
            Value = value,
            Detail = detail,
            DecoratorName = decoratorName,
            DecoratorArgs = decoratorArgs,
        });
    }
```

- [ ] **Step 7: Add no-op `RecordCompilation` to `RuntimeDiagnosticCollector` and `NullDiagnosticCollector`**

Add to both files after the existing `Record` method:

```csharp
    public void RecordCompilation(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
    }
```

- [ ] **Step 8: Update all binder call sites**

In each binder file, change `collector.Record(DiagnosticEventType.X, ...)` to `collector.RecordCompilation(CompilationEventType.X, ...)`:

- `HintBinder.cs:23` — `DiagnosticEventType.HintAdded` → `CompilationEventType.HintAdded`
- `TagBinder.cs:23` — `DiagnosticEventType.TagAdded` → `CompilationEventType.TagAdded`
- `TokenFactory.cs:29` — `DiagnosticEventType.TokenCreated` → `CompilationEventType.TokenCreated`
- `OptionApplier.cs:18,30` — `DiagnosticEventType.OptionApplied` → `CompilationEventType.OptionApplied`
- `RepeatingTokenLinker.cs:24` — `DiagnosticEventType.RepeatingTokenLinked` → `CompilationEventType.RepeatingTokenLinked`
- `DecoratorBinder.cs:26,70,103,137` — `DiagnosticEventType.DecoratorApplied` / `ConcatenationApplied` → `CompilationEventType.DecoratorApplied` / `ConcatenationApplied`
- `TemplateCompiler.cs:54` — `DiagnosticEventType.CompilationCompleted` → `CompilationEventType.CompilationCompleted` (also change `collector.Record` to `collector.RecordCompilation`)

- [ ] **Step 9: Attach compilation diagnostics in catch-all (H7)**

In `src/Tokenizer/Compilation/TemplateCompiler.cs`, change the catch-all block (lines 73-78) from:

```csharp
        catch (Exception ex)
        {
            _log.LogError(ex, "Unexpected error during template compilation: {Message}", ex.Message);
            throw new TokenizerException($"Unexpected error during template compilation: {ex.Message}", ex);
        }
```

To:

```csharp
        catch (Exception ex)
        {
            _log.LogError(ex, "Unexpected error during template compilation: {Message}", ex.Message);
            var wrapped = new TokenizerException($"Unexpected error during template compilation: {ex.Message}", ex);
            wrapped.Data["CompilationDiagnostics"] = collector.GetCompilationResult();
            throw wrapped;
        }
```

- [ ] **Step 10: Update tests**

Update test assertions that reference the old `DiagnosticEventType` compilation members:

- `CompilationDiagnosticsTests.cs` — change `e.Type == DiagnosticEventType.CompilationCompleted` to `e.Type == CompilationEventType.CompilationCompleted`, and `DiagnosticEvent` casts to `CompilationEvent`
- `DiagnosticIntegrationTests.cs:139-146` — remove `Assert.DoesNotContain` lines for compilation event types (they no longer exist in the enum)
- `HintBinderTests.cs:61` — change `DiagnosticEventType.HintAdded` to `CompilationEventType.HintAdded`
- `TagBinderTests.cs:61` — change `DiagnosticEventType.TagAdded` to `CompilationEventType.TagAdded`
- `RepeatingTokenLinkerTests.cs:99` — change `DiagnosticEventType.RepeatingTokenLinked` to `CompilationEventType.RepeatingTokenLinked`
- `OptionApplierTests.cs:77` — change `DiagnosticEventType.OptionApplied` to `CompilationEventType.OptionApplied`
- `CompilationResultTests.cs:51` — change `DiagnosticEventType.CompilationCompleted` to `CompilationEventType.CompilationCompleted`

- [ ] **Step 11: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds with 0 warnings, all tests pass.

- [ ] **Step 12: Commit**

```bash
git add src/Tokenizer/Diagnostics/CompilationEventType.cs src/Tokenizer/Diagnostics/CompilationEvent.cs src/Tokenizer/Diagnostics/DiagnosticEventType.cs src/Tokenizer/Diagnostics/IDiagnosticCollector.cs src/Tokenizer/Diagnostics/CompilationDiagnosticCollector.cs src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs src/Tokenizer/Diagnostics/CompilationDiagnostics.cs src/Tokenizer/Compilation/TemplateCompiler.cs src/Tokenizer/Compilation/Binders/ tests/
git commit -m "Split compilation events from DiagnosticEventType, attach diagnostics in catch-all"
```

---

### Task 2: TokenDiagnosticBuilder Fixes (H3, H5, M1, D2, H9, M15)

**Files:**
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Characterisation/CausalityChainTests.cs`

**Interfaces:**
- Consumes: `DiagnosticEventType.PreambleMatched`, `IssueFactory.CreateBlocked()`
- Produces: `TokenDiagnosticBuilder.Build(DiagnosticResult, IssueFactory?)` — optional `IssueFactory` parameter

- [ ] **Step 1: Write failing test for BacktrackStarted (H9)**

Add to `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`:

```csharp
    [Fact]
    public void GivenBacktrackEvent_WhenBuilding_ThenAttemptHasBacktrackedOutcome()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("Name: bad\nName: John");
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Name");
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Name", value: "bad");
        collector.Record(DiagnosticEventType.BacktrackStarted, tokenName: "Name", value: "bad");
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Name");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name", value: "John");
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        var token = Assert.Single(tokens);
        Assert.Equal(TokenOutcome.Matched, token.Outcome);
        Assert.Equal(2, token.Attempts.Count);
        Assert.Equal(AttemptOutcome.Backtracked, token.Attempts[0].Outcome);
        Assert.Equal(AttemptOutcome.Assigned, token.Attempts[1].Outcome);
    }
```

- [ ] **Step 2: Write failing test for per-token HintMissing (M15)**

Add to `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`:

```csharp
    [Fact]
    public void GivenHintMissingWithTokenName_WhenBuilding_ThenIssueAttachedToToken()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.HintMissing, tokenName: "Name", value: "Expected hint");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Name");
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        var nameToken = tokens.First(t => string.Equals(t.TokenName, "Name", StringComparison.Ordinal));
        Assert.Contains(nameToken.Issues, i => i.Type == DiagnosticIssueType.HintMissing);
    }
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenDiagnosticBuilderTests"`

Expected: Both new tests pass (BacktrackStarted already works; HintMissing with token name already routes correctly). If they already pass, good — these are coverage gap fills, not behavior changes.

- [ ] **Step 4: Make IssueFactory injectable (D2)**

In `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`, rename the static field and update `Build`:

Change:
```csharp
    private static readonly IssueFactory IssueFactory = new IssueFactory(new IHintGenerator[]
```
To:
```csharp
    private static readonly IssueFactory DefaultIssueFactory = new IssueFactory(new IHintGenerator[]
```

Change `Build` signature:
```csharp
    public static (IReadOnlyList<TokenDiagnostic> tokens, string verdict, int matchedCount, int missedCount, int totalCount) Build(DiagnosticResult diagnostics, IssueFactory? issueFactory = null)
    {
        issueFactory ??= DefaultIssueFactory;
        var collected = CollectEvents(diagnostics, issueFactory);
```

Pass `issueFactory` as a parameter to `CollectEvents`, `ClassifyOutcomes`, and `ApplyBlockedAnnotations`. Update each method signature to accept `IssueFactory issueFactory` and replace all references to the old `IssueFactory` static field.

- [ ] **Step 5: Guard null TokenName in AddIssue (M1)**

In `TokenDiagnosticBuilder.cs`, change `AddIssue` method:

From:
```csharp
    private static void AddIssue(Dictionary<string, List<DiagnosticIssue>> issues, DiagnosticIssue issue)
    {
        var tokenName = issue.TokenName!;
```
To:
```csharp
    private static void AddIssue(Dictionary<string, List<DiagnosticIssue>> issues, DiagnosticIssue issue)
    {
        if (issue.TokenName == null)
            return;

        var tokenName = issue.TokenName;
```

- [ ] **Step 6: Collect preambles from PreambleMatched events (H5)**

In the `CollectEvents` switch statement, add a new case before the existing `TokenMissed` case:

```csharp
                case DiagnosticEventType.PreambleMatched:
                    if (evt.TokenName != null && !string.IsNullOrEmpty(evt.Detail)
                        && !data.PreambleTexts.ContainsKey(evt.TokenName))
                    {
                        data.PreambleTexts[evt.TokenName] = evt.Detail!;
                    }
                    break;
```

Keep the existing preamble collection in the `TokenMissed` case as a fallback.

- [ ] **Step 7: Preserve original issues on blocked tokens (H3)**

In `ApplyBlockedAnnotations`, change the `TokenDiagnostic` replacement from:

```csharp
                    Issues = new List<DiagnosticIssue>
                    {
                        IssueFactory.CreateBlocked(token.TokenName, blockerName, diagnostics),
                    },
```
To:
```csharp
                    Issues = new List<DiagnosticIssue>(token.Issues)
                    {
                        issueFactory.CreateBlocked(token.TokenName, blockerName, diagnostics),
                    },
```

- [ ] **Step 8: Update causality chain characterisation tests**

In `CausalityChainTests.cs`, blocked tokens now have both original issues AND the `Blocked` issue. Update assertions that check `Issues` on blocked tokens to expect the merged list. For example, in `GivenOrderedTokens_WhenNonOptionalTokenMissing_ThenSubsequentTokensAreBlocked`, add:

```csharp
        Assert.Contains(tokenC.Issues, i => i.Type == DiagnosticIssueType.Blocked);
        Assert.Contains(tokenC.Issues, i => i.Type == DiagnosticIssueType.PreambleNeverFound);
```

- [ ] **Step 9: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 10: Commit**

```bash
git add src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs tests/Tokenizer.Tests/Diagnostics/Characterisation/CausalityChainTests.cs
git commit -m "Fix blocked token issue merging, preamble collection, null guard, injectable IssueFactory"
```

---

### Task 3: Hint Generator Fixes (M3, M4, M5, M6, M9, M10, L7, L8, L9, D3, M11)

**Files:**
- Modify: `src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/BlockedTokenHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/ChainedDecoratorHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/DateFormatHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/MultipleRejectionHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/OptionalTokenHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/RepeatingTokenHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/ValidatorValueHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/ValueMismatchHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/IssueFactory.cs`
- Modify: `src/Tokenizer/Diagnostics/DiagnosticResult.cs`
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/BlockedTokenHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/ChainedDecoratorHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/DateFormatHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/MultipleRejectionHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/OptionalTokenHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/PreambleNearMissHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/RepeatingTokenHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/ValidatorValueHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/ValueMismatchHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/IssueFactoryTests.cs`

**Interfaces:**
- Consumes: `DiagnosticResult` (adds internal properties)
- Produces: `IHintGenerator.TryGenerateHint(DiagnosticIssueType, string?, DiagnosticEvent, DiagnosticResult)` new signature

- [ ] **Step 1: Change `IHintGenerator` signature (M3)**

In `src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs`, change:

```csharp
    public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                            DiagnosticResult trace);
```
To:
```csharp
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                            DiagnosticEvent sourceEvent, DiagnosticResult trace);
```

- [ ] **Step 2: Add indexed properties and cached lines to `DiagnosticResult` (M9, M10, L8)**

In `src/Tokenizer/Diagnostics/DiagnosticResult.cs`, add internal properties (before the constructor):

```csharp
    internal Dictionary<string, List<DiagnosticEvent>>? RejectionsPerToken { get; set; }
    internal Dictionary<string, List<DiagnosticEvent>>? DecoratorSuccessesPerToken { get; set; }
    internal string[]? CachedInputLines { get; set; }
```

- [ ] **Step 3: Populate indexes in `TokenDiagnosticBuilder.CollectEvents`**

In `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`, initialize the indexes at the start of `CollectEvents`:

```csharp
        diagnostics.RejectionsPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal);
        diagnostics.DecoratorSuccessesPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal);
```

Add a helper method:

```csharp
    private static void AddToIndex(Dictionary<string, List<DiagnosticEvent>>? index, string tokenName, DiagnosticEvent evt)
    {
        if (index == null)
            return;

        if (!index.TryGetValue(tokenName, out var list))
        {
            list = new List<DiagnosticEvent>();
            index[tokenName] = list;
        }
        list.Add(evt);
    }
```

Add a new case in the switch for decorator successes:

```csharp
                case DiagnosticEventType.ValidatorPassed:
                case DiagnosticEventType.TransformerSucceeded:
                    if (evt.TokenName != null)
                        AddToIndex(diagnostics.DecoratorSuccessesPerToken, evt.TokenName, evt);
                    break;
```

In the `ValidatorFailed` case, add index population *before* the existing `IssueFactory.Create` call:

```csharp
                case DiagnosticEventType.ValidatorFailed:
                    if (evt.TokenName != null)
                        AddToIndex(diagnostics.RejectionsPerToken, evt.TokenName, evt);
                    var validatorDescription = BuildValidatorDescription(evt);
                    // ... rest unchanged ...
```

Same for `TransformerFailed`:

```csharp
                case DiagnosticEventType.TransformerFailed:
                    if (evt.TokenName != null)
                        AddToIndex(diagnostics.RejectionsPerToken, evt.TokenName, evt);
                    var transformerDescription = BuildTransformerDescription(evt);
                    // ... rest unchanged ...
```

- [ ] **Step 4: Update `IssueFactory.GenerateHint` to drop throwaway allocation (M3)**

In `src/Tokenizer/Diagnostics/IssueFactory.cs`, change `GenerateHint`:

```csharp
    private string? GenerateHint(DiagnosticIssueType type, DiagnosticEvent sourceEvent,
                                  string description, DiagnosticResult diagnostics)
    {
        foreach (var generator in _hintGenerators)
        {
            var hint = generator.TryGenerateHint(type, sourceEvent.TokenName, sourceEvent, diagnostics);
            if (hint != null)
                return hint;
        }
        return null;
    }
```

Also update `CreateValueMismatch` to set `sourceEvent.Detail = missedTokenName` (M6):

```csharp
    internal DiagnosticIssue CreateValueMismatch(string tokenName, string missedTokenName, DiagnosticResult diagnostics)
    {
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenAssigned,
            TokenName = tokenName,
            Detail = missedTokenName,
        };

        return Create(
            DiagnosticIssueType.ValueMismatch,
            sourceEvent,
            $"Token '{tokenName}' captured value containing preamble of token '{missedTokenName}'.",
            diagnostics);
    }
```

- [ ] **Step 5: Update all 9 hint generators for new signature**

For each generator, change `TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent, DiagnosticResult trace)` to `TryGenerateHint(DiagnosticIssueType type, string? tokenName, DiagnosticEvent sourceEvent, DiagnosticResult trace)`.

Replace `issue.Type` with `type` and `issue.TokenName` with `tokenName`.

**BlockedTokenHintGenerator:**
```csharp
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   DiagnosticEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.Blocked)
            return null;
        // ... rest unchanged, already reads from sourceEvent ...
```

**ChainedDecoratorHintGenerator (M4, D3, M9, M10):**

Rewrite to use `trace.DecoratorSuccessesPerToken` instead of scanning `RawEvents`:

```csharp
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   DiagnosticEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.ValidatorRejection &&
            type != DiagnosticIssueType.TransformerFailure)
        {
            return null;
        }

        var failingDecorator = sourceEvent.DecoratorName;
        var value = sourceEvent.Value ?? string.Empty;

        if (tokenName == null || failingDecorator == null)
            return null;

        if (trace.DecoratorSuccessesPerToken == null ||
            !trace.DecoratorSuccessesPerToken.TryGetValue(tokenName, out var successes) ||
            successes.Count == 0)
        {
            return null;
        }

        var priorDecorator = successes[successes.Count - 1].DecoratorName;
        if (priorDecorator == null)
            return null;

        var action = type == DiagnosticIssueType.ValidatorRejection ? "rejected" : "failed on";
        return $"Decorator chain: '{priorDecorator}' succeeded \u2192 '{failingDecorator}' {action} value '{value}'.";
    }
```

**MultipleRejectionHintGenerator (M5, M9):**

Rewrite to use `trace.RejectionsPerToken` and `ReferenceEquals`:

```csharp
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   DiagnosticEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.ValidatorRejection &&
            type != DiagnosticIssueType.TransformerFailure)
        {
            return null;
        }

        if (tokenName == null)
            return null;

        if (trace.RejectionsPerToken == null ||
            !trace.RejectionsPerToken.TryGetValue(tokenName, out var rejections) ||
            rejections.Count < 2)
        {
            return null;
        }

        if (!ReferenceEquals(rejections[rejections.Count - 1], sourceEvent))
            return null;

        var sb = new StringBuilder();
        sb.Append("Token was rejected ").Append(rejections.Count.ToInvariant()).Append(" times. Values tried: ");

        for (var i = 0; i < rejections.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");

            var evt = rejections[i];
            sb.Append('\'').Append(evt.Value ?? string.Empty).Append('\'');

            if (evt.Location != null)
                sb.Append(" (line ").Append(evt.Location.Line.ToInvariant()).Append(')');
        }

        sb.Append('.');
        return sb.ToString();
    }
```

Remove the `CollectRejections` private method.

**RepeatingTokenHintGenerator (L7):**

Rewrite to use `trace.RejectionsPerToken`:

```csharp
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   DiagnosticEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.RepeatingTokenCutShort)
            return null;

        if (tokenName != null && trace.RejectionsPerToken != null &&
            trace.RejectionsPerToken.TryGetValue(tokenName, out var rejections) &&
            rejections.Count > 0)
        {
            var last = rejections[rejections.Count - 1];

            if (last.Type == DiagnosticEventType.ValidatorFailed)
            {
                var validator = last.DecoratorName ?? "unknown validator";
                var value = last.Value ?? "unknown value";
                return $"Repeating token '{tokenName}' was disabled. " +
                       $"The value '{value}' failed {validator} validation.";
            }

            if (last.Type == DiagnosticEventType.TransformerFailed)
            {
                var transformer = last.DecoratorName ?? "unknown transformer";
                var value = last.Value ?? "unknown value";
                return $"Repeating token '{tokenName}' was disabled. " +
                       $"The value '{value}' failed {transformer} transformation.";
            }
        }

        if (!string.IsNullOrEmpty(sourceEvent.Detail))
        {
            return $"Repeating token '{tokenName}' was disabled: {sourceEvent.Detail}";
        }

        return null;
    }
```

**ValueMismatchHintGenerator (M6):**

```csharp
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   DiagnosticEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.ValueMismatch)
            return null;

        var missedToken = sourceEvent.Detail;
        if (string.IsNullOrEmpty(missedToken))
            return "Consider adding an end delimiter (e.g. newline-terminated with '$') to prevent greedy capture.";

        return $"Matched value may have captured the preamble of token '{missedToken}'. " +
               "Consider adding an end delimiter (e.g. newline-terminated with '$') to prevent greedy capture.";
    }
```

**PreambleNearMissHintGenerator (L8, L9):**

Update signature and use cached lines:

```csharp
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   DiagnosticEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.PreambleNeverFound)
            return null;

        // ... rest of preamble/inputContent checks unchanged ...

        if (trace.CachedInputLines == null)
        {
            trace.CachedInputLines = inputContent!.Split('\n');
        }
        var lines = trace.CachedInputLines;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            // ... rest unchanged ...
```

**DateFormatHintGenerator, ValidatorValueHintGenerator, OptionalTokenHintGenerator:**

Update signatures. Replace `issue.Type` with `type`, `issue.TokenName` with `tokenName`. Logic unchanged.

- [ ] **Step 6: Update `IssueFactoryTests.ConstantHintGenerator`**

In `tests/Tokenizer.Tests/Diagnostics/IssueFactoryTests.cs`, update the test double:

```csharp
    private sealed class ConstantHintGenerator : IHintGenerator
    {
        private readonly string _hint;

        internal ConstantHintGenerator(string hint) => _hint = hint;

        public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                       DiagnosticEvent sourceEvent, DiagnosticResult trace)
            => _hint;
    }
```

- [ ] **Step 7: Update all hint generator tests for new signature**

In every test file under `tests/Tokenizer.Tests/Diagnostics/Hints/`, change all `_generator.TryGenerateHint(issue, sourceEvent, trace)` calls to `_generator.TryGenerateHint(issue.Type, issue.TokenName, sourceEvent, trace)`.

The `issue` local variable can remain (it's used to set up `Type` and `TokenName`), or you can pass the values directly. The simplest mechanical change: keep the `issue` variable for readability and destructure at the call site.

- [ ] **Step 8: Fix ChainedDecoratorHintGenerator tests (M11)**

In `ChainedDecoratorHintGeneratorTests.cs`, the `sourceEvent` should be the actual event from the trace for `ReferenceEquals` to work with the new indexed approach. Change tests to retrieve the event from the collector:

```csharp
    [Fact]
    public void GivenValidatorRejectionWithPriorSuccess_WhenGeneratingHint_ThenDescribesChain()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");
        collector.Record(DiagnosticEventType.ValidatorPassed,
            tokenName: "Email", decoratorName: "IsEmailValidator", value: "bad value");
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsDomainNameValidator", value: "bad value");
        var trace = collector.GetResult()!;

        // Pre-populate indexes (normally done by TokenDiagnosticBuilder)
        trace.DecoratorSuccessesPerToken = new Dictionary<string, List<DiagnosticEvent>>(StringComparer.Ordinal)
        {
            ["Email"] = new List<DiagnosticEvent> { trace.RawEvents[0] },
        };

        var sourceEvent = trace.RawEvents[1]; // the actual ValidatorFailed event

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValidatorRejection, "Email", sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("IsEmailValidator", hint, StringComparison.Ordinal);
        Assert.Contains("IsDomainNameValidator", hint, StringComparison.Ordinal);
    }
```

Apply the same pattern to all `ChainedDecoratorHintGeneratorTests`. For the "no prior success" test, set an empty `DecoratorSuccessesPerToken` dictionary.

Similarly, update `MultipleRejectionHintGeneratorTests` to populate `RejectionsPerToken` and use actual trace events as `sourceEvent`.

- [ ] **Step 9: Update ValueMismatchHintGeneratorTests for dynamic hint (M6)**

```csharp
    [Fact]
    public void GivenValueMismatchIssueWithMissedToken_WhenGeneratingHint_ThenIncludesMissedTokenName()
    {
        // Arrange
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenAssigned,
            TokenName = "Description",
            Value = "some greedy value",
            Detail = "Price",
        };
        var trace = new RuntimeDiagnosticCollector("input").GetResult()!;

        // Act
        var hint = _generator.TryGenerateHint(DiagnosticIssueType.ValueMismatch, "Description", sourceEvent, trace);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("Price", hint, StringComparison.Ordinal);
        Assert.Contains("end delimiter", hint, StringComparison.Ordinal);
    }
```

- [ ] **Step 10: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 11: Commit**

```bash
git add src/Tokenizer/Diagnostics/ tests/Tokenizer.Tests/Diagnostics/
git commit -m "Refactor hint generators: new signature, pre-indexed events, cached lines, dynamic hints"
```

---

### Task 4: Logging/Observability (H2, H6, H8, M8)

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/Diagnostics/IssueCodeMap.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/IssueCodeMapTests.cs`

**Interfaces:**
- Consumes: `DiagnosticResult.Tokens`, `DiagnosticResult.MissedCount`, `TokenOutcome`, `DiagnosticIssue.Code`

- [ ] **Step 1: Guard diagnostic logging loop (H2) and add Warning logging (H6)**

In `src/Tokenizer/Tokenizer.cs`, replace the entire diagnostic logging block in `FinalizeTokenization` (lines 351-372) with:

```csharp
        if (result.Diagnostics != null)
        {
            if (result.Diagnostics.MissedCount > 0)
            {
                foreach (var token in result.Diagnostics.Tokens)
                {
                    if (token.Outcome == TokenOutcome.Matched)
                        continue;

                    foreach (var issue in token.Issues)
                    {
                        _log.LogWarning("[{IssueCode}] Token '{TokenName}': {Description}",
                            issue.Code, issue.TokenName, issue.Description);
                    }
                }
            }

            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("{Verdict}", result.Diagnostics.Verdict);
                foreach (var token in result.Diagnostics.Tokens)
                {
                    foreach (var issue in token.Issues)
                    {
                        _log.LogDebug("Token '{TokenName}': {Description}", issue.TokenName, issue.Description);
                        if (issue.Hint != null)
                        {
                            _log.LogDebug("  \u2192 Hint: {Hint}", issue.Hint);
                        }
                    }
                }
                if (rawInput != null)
                {
                    _log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
                }
            }
        }
```

- [ ] **Step 2: Attach diagnostics to runtime exceptions (H8)**

In `src/Tokenizer/Tokenizer.cs`, update both catch blocks (lines 316-327):

```csharp
            catch (TokenizerException ex)
            {
                _log.LogError(ex, "Tokenization failed for template {TemplateName}: {Message}",
                    template.Name, ex.Message);
                ex.Data["Diagnostics"] = collector.GetResult();
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error during tokenization for template {TemplateName}: {Message}",
                    template.Name, ex.Message);
                ex.Data["Diagnostics"] = collector.GetResult();
                throw;
            }
```

- [ ] **Step 3: IssueCodeMap fallback (M8)**

In `src/Tokenizer/Diagnostics/IssueCodeMap.cs`, change line 18:

From:
```csharp
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown DiagnosticIssueType"),
```
To:
```csharp
        _ => $"TK???({(int)type})",
```

- [ ] **Step 4: Update IssueCodeMap test for fallback**

In `tests/Tokenizer.Tests/Diagnostics/IssueCodeMapTests.cs`, change `GivenUnknownIssueType_WhenGettingCode_ThenThrowsArgumentOutOfRange`:

```csharp
    [Fact]
    public void GivenUnknownIssueType_WhenGettingCode_ThenReturnsFallbackCode()
    {
        // Arrange
        var unknownType = (DiagnosticIssueType)999;

        // Act
        var code = IssueCodeMap.GetCode(unknownType);

        // Assert
        Assert.Equal("TK???(999)", code);
    }
```

- [ ] **Step 5: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs src/Tokenizer/Diagnostics/IssueCodeMap.cs tests/Tokenizer.Tests/Diagnostics/IssueCodeMapTests.cs
git commit -m "Guard diagnostic logging, add Warning-level issue logging, attach diagnostics to exceptions"
```

---

### Task 5: AlignmentRenderer (H1, L11, M13)

**Files:**
- Modify: `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs`

**Interfaces:**
- Consumes: `TokenDiagnostic.Attempts`, `AttemptOutcome`

- [ ] **Step 1: Filter failures to actual failure outcomes (H1, L11)**

In `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`, change the Failures section body (lines 61-74):

```csharp
        if (rejectedTokens.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Failures ──");
            foreach (var token in rejectedTokens)
            {
                foreach (var attempt in token.Attempts)
                {
                    if (attempt.Outcome != AttemptOutcome.ValidatorRejected &&
                        attempt.Outcome != AttemptOutcome.TransformerFailed)
                        continue;

                    var decoratorDesc = !string.IsNullOrEmpty(attempt.DecoratorName) ? attempt.DecoratorName : "decorator";
                    sb.Append("  ✗ ").Append(token.TokenName).Append(": ").Append(attempt.Outcome).Append(" — ").Append(decoratorDesc).Append(" failed on '").Append(attempt.Value).AppendLine("'");
                }

                foreach (var issue in token.Issues)
                {
                    if (issue.Hint != null)
                        sb.Append("      Hint: ").AppendLine(issue.Hint);
                }
            }
        }
```

Replace the LINQ `Sum` on line 117 with a helper:

```csharp
            .Append(" | Failures: ").Append(CountFailures(rejectedTokens));
```

Add the helper at the end of the class:

```csharp
    private static int CountFailures(List<TokenDiagnostic> tokens)
    {
        var count = 0;
        foreach (var token in tokens)
            foreach (var attempt in token.Attempts)
                if (attempt.Outcome == AttemptOutcome.ValidatorRejected ||
                    attempt.Outcome == AttemptOutcome.TransformerFailed)
                    count++;
        return count;
    }
```

- [ ] **Step 2: Write test for blocked tokens rendering (M13)**

Add to `tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs`:

```csharp
    [Fact]
    public void GivenBlockedTokens_WhenRendered_ThenShowsBlockedSectionWithMarkerAndBlocker()
    {
        // Arrange — ordered template, B missing causes C to be blocked
        var template = "A: { A }\nB: { B }\nC: { C }";
        var input = "A: one";
        var result = TokenizeWithDiagnostics(template, input);

        // Act
        var alignment = result.Diagnostics!.RenderAlignment();
        Output.WriteLine(alignment);

        // Assert
        Assert.Contains("\u2298", alignment, StringComparison.Ordinal); // ⊘ marker
        Assert.Contains("blocked by", alignment, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Blocked:", alignment, StringComparison.Ordinal);
    }
```

- [ ] **Step 3: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Diagnostics/AlignmentRenderer.cs tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs
git commit -m "Filter AlignmentRenderer failures to actual failure outcomes, add blocked section test"
```

---

### Task 6: Lazy Init / EnsureBuilt (M2, D1, L12)

**Files:**
- Modify: `src/Tokenizer/Diagnostics/DiagnosticResult.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs`

**Interfaces:**
- Consumes: `TokenDiagnosticBuilder.Build()`

- [ ] **Step 1: Replace separate fields with `BuiltResult` record (M2, D1)**

In `src/Tokenizer/Diagnostics/DiagnosticResult.cs`, replace the 5 cached fields and `EnsureBuilt`:

Remove:
```csharp
    private IReadOnlyList<TokenDiagnostic>? _tokens;
    private string? _verdict;
    private int _matchedCount;
    private int _missedCount;
    private int _totalCount;
```

Add:
```csharp
    private sealed record BuiltResult(
        IReadOnlyList<TokenDiagnostic> Tokens,
        string Verdict,
        int MatchedCount,
        int MissedCount,
        int TotalCount);

    private BuiltResult? _built;
```

Replace `EnsureBuilt()` with:
```csharp
    private BuiltResult GetBuilt()
    {
        if (_built != null)
            return _built;

        var (tokens, verdict, matched, missed, total) = TokenDiagnosticBuilder.Build(this);
        _built = new BuiltResult(tokens, verdict, matched, missed, total);
        return _built;
    }
```

Update all property accessors:
```csharp
    public IReadOnlyList<TokenDiagnostic> Tokens => GetBuilt().Tokens;

    public string Verdict => GetBuilt().Verdict;

    public int MatchedCount => GetBuilt().MatchedCount;

    public int MissedCount => GetBuilt().MissedCount;

    public int TotalCount => GetBuilt().TotalCount;
```

- [ ] **Step 2: Write RenderAlignment caching test (L12)**

Add to `tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs`:

```csharp
    [Fact]
    public void GivenDiagnostics_WhenRenderAlignmentCalledTwice_ThenReturnsSameInstance()
    {
        // Arrange
        var result = TokenizeWithDiagnostics("Name: { Name }", "Name: Alice");
        var diagnostics = result.Diagnostics!;

        // Act
        var first = diagnostics.RenderAlignment();
        var second = diagnostics.RenderAlignment();

        // Assert
        Assert.Same(first, second);
    }
```

- [ ] **Step 3: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Diagnostics/DiagnosticResult.cs tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs
git commit -m "Replace EnsureBuilt with atomic BuiltResult record, add RenderAlignment caching test"
```

---

### Task 7: Doc/Style Cleanup (L1, L3, L6)

**Files:**
- Modify: `src/Tokenizer/Diagnostics/IssueCodeMap.cs`
- Modify: `tests/Tokenizer.Tests/Diagnostics/Characterisation/ValidatorRejectionTests.cs`

Note: L2 (stale doc comment) was addressed in Task 1, Step 4. L4 requires no change.

**Interfaces:** None — standalone cleanup.

- [ ] **Step 1: Add TK006 reserved comment (L1) and fix visibility (L3)**

In `src/Tokenizer/Diagnostics/IssueCodeMap.cs`:

Change `public static string GetCode` to `internal static string GetCode`.

Add comment between TK005 and TK007:

```csharp
        DiagnosticIssueType.RepeatingTokenCutShort => "TK005",
        // TK006: reserved
        DiagnosticIssueType.HintMissing => "TK007",
```

- [ ] **Step 2: Rename short-circuit test (L6)**

In `tests/Tokenizer.Tests/Diagnostics/Characterisation/ValidatorRejectionTests.cs`, rename:

`GivenMultipleValidators_WhenFirstFails_ThenFirstRejectionRecorded`

to:

`GivenMultipleValidators_WhenFirstFails_ThenEngineShortCircuits_OnlyFirstRejectionRecorded`

- [ ] **Step 3: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Diagnostics/IssueCodeMap.cs tests/Tokenizer.Tests/Diagnostics/Characterisation/ValidatorRejectionTests.cs
git commit -m "Fix IssueCodeMap visibility, add TK006 reserved comment, rename short-circuit test"
```

---

### Task 8: Test Gaps (M7, M12, M14, L5, L13, D4)

**Files:**
- Test: `tests/Tokenizer.Tests/Diagnostics/Characterisation/TransformerFailureTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Characterisation/EdgeCaseTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/IssueFactoryTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs`

**Interfaces:** None — standalone test additions.

- [ ] **Step 1: Add first-transformer-in-chain-fails test (M7)**

Add to `tests/Tokenizer.Tests/Diagnostics/Characterisation/TransformerFailureTests.cs`:

```csharp
    [Fact]
    public void GivenChainedTransformers_WhenFirstFails_ThenSecondNeverReached()
    {
        // Arrange — ToDateTime fails on "bad", ToUpper never runs
        var template = "Date: { Date : ToDateTime('yyyy-MM-dd'), ToUpper }";
        var input = "Date: bad";

        // Act
        var result = TokenizeWithDiagnostics(template, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        var issues = diagnostics.Tokens
            .Where(t => string.Equals(t.TokenName, "Date", StringComparison.Ordinal))
            .SelectMany(t => t.Issues)
            .ToList();

        Assert.Contains(issues, i => i.Type == DiagnosticIssueType.TransformerFailure
            && i.Description.Contains("ToDateTime", StringComparison.Ordinal));
        Assert.DoesNotContain(issues, i => i.Description.Contains("ToUpper", StringComparison.Ordinal));
    }
```

- [ ] **Step 2: Add CreateValueMismatch unit test (M12)**

Add to `tests/Tokenizer.Tests/Diagnostics/IssueFactoryTests.cs`:

```csharp
    [Fact]
    public void GivenValueMismatch_WhenCreatingIssue_ThenTypeIsValueMismatchAndDescriptionContainsMissedToken()
    {
        // Arrange
        var factory = new IssueFactory(new IHintGenerator[] { new ValueMismatchHintGenerator() });
        var diagnostics = new RuntimeDiagnosticCollector("input").GetResult()!;

        // Act
        var issue = factory.CreateValueMismatch("Description", "Price", diagnostics);

        // Assert
        Assert.Equal(DiagnosticIssueType.ValueMismatch, issue.Type);
        Assert.Equal("Description", issue.TokenName);
        Assert.Contains("Price", issue.Description, StringComparison.Ordinal);
        Assert.NotNull(issue.Hint);
        Assert.Contains("Price", issue.Hint, StringComparison.Ordinal);
    }
```

Add the required using at the top of the file:

```csharp
using Tokens.Diagnostics.Hints;
```

(Note: this `using` likely already exists for `ConstantHintGenerator`.)

- [ ] **Step 3: Add IsEnabled assertions (M14)**

Add to `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs`:

```csharp
    [Fact]
    public void GivenNullCollector_WhenCheckingIsEnabled_ThenReturnsFalse()
    {
        // Assert
        Assert.False(NullDiagnosticCollector.Instance.IsEnabled);
    }

    [Fact]
    public void GivenRuntimeCollector_WhenCheckingIsEnabled_ThenReturnsTrue()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("x");

        // Assert
        Assert.True(collector.IsEnabled);
    }

    [Fact]
    public void GivenCompilationCollector_WhenCheckingIsEnabled_ThenReturnsTrue()
    {
        // Arrange
        var collector = new CompilationDiagnosticCollector();

        // Assert
        Assert.True(collector.IsEnabled);
    }
```

- [ ] **Step 4: Graduate NewlineTerminated test (L5)**

In `tests/Tokenizer.Tests/Diagnostics/Characterisation/EdgeCaseTests.cs`, in `GivenNewlineTerminatedToken_WhenValueEndsAtNewline_ThenNewlineTerminatedEventRecorded`, replace the `Output.WriteLine` with an assertion:

```csharp
        var newlineEvents = diagnostics.RawEvents
            .Where(e => e.Type == DiagnosticEventType.NewlineTerminatedTokenProcessed)
            .ToList();
        Assert.NotEmpty(newlineEvents);
```

- [ ] **Step 5: Add NullDiagnosticCollector.GetCompilationResult test (L13)**

Add to `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs`:

```csharp
    [Fact]
    public void GivenNullCollector_WhenGetCompilationResult_ThenReturnsNull()
    {
        // Act
        var result = NullDiagnosticCollector.Instance.GetCompilationResult();

        // Assert
        Assert.Null(result);
    }
```

- [ ] **Step 6: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: Build succeeds, all tests pass.

- [ ] **Step 7: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/
git commit -m "Add missing test coverage: transformer chain, ValueMismatch, IsEnabled, NewlineTerminated, GetCompilationResult"
```
