# Diagnostic Review Fixes

Addresses all 41 issues (H1-H9, M1-M15, L1-L13, D1-D4) from the diagnostics branch code review.

## Themes

Work is organized into 8 themes, each producing one or more commits:

1. Enum split + compilation diagnostics
2. TokenDiagnosticBuilder fixes
3. Hint generator fixes
4. Logging/observability
5. AlignmentRenderer
6. Lazy init / EnsureBuilt
7. Doc/style cleanup
8. Test gaps

## Theme 1: Enum Split + Compilation Diagnostics

**Issues:** H4, H7

### Split DiagnosticEventType (H4)

Create `CompilationEventType` enum with these members moved from `DiagnosticEventType`:

- `HintAdded`
- `TagAdded`
- `TokenCreated`
- `OptionApplied`
- `DecoratorApplied`
- `ConcatenationApplied`
- `RepeatingTokenLinked`
- `CompilationCompleted`

`DiagnosticEventType` retains all runtime event types only.

### New CompilationEvent Type

`CompilationDiagnostics.Events` changes from `IReadOnlyList<DiagnosticEvent>` to `IReadOnlyList<CompilationEvent>`, where `CompilationEvent` mirrors `DiagnosticEvent` but uses `CompilationEventType`.

### IDiagnosticCollector Changes

Add a second recording method:

```csharp
void RecordCompilation(CompilationEventType type, string? tokenName = null, int? tokenId = null,
    FileLocation? location = null, string? value = null, string? detail = null,
    string? decoratorName = null, string[]? decoratorArgs = null);
```

One interface, two methods:

- `CompilationDiagnosticCollector` — `RecordCompilation` stores events, `Record` is a no-op
- `RuntimeDiagnosticCollector` — `Record` stores events, `RecordCompilation` is a no-op
- `NullDiagnosticCollector` — both are no-ops

### Call Site Updates

Binders (`HintBinder`, `TagBinder`, `TokenBinder`, `OptionApplier`, `RepeatingTokenLinker`) change from `collector.Record(DiagnosticEventType.TokenCreated, ...)` to `collector.RecordCompilation(CompilationEventType.TokenCreated, ...)`.

### Attach Compilation Diagnostics in Catch-All (H7)

`TemplateCompiler.cs` catch-all (general `Exception` catch) wraps into `TokenizerException`. Attach diagnostics before throwing:

```csharp
var wrapped = new TokenizerException($"...: {ex.Message}", ex);
wrapped.Data["CompilationDiagnostics"] = collector.GetCompilationResult();
throw wrapped;
```

### File Impact

**New files:**
- `src/Tokenizer/Diagnostics/CompilationEventType.cs`
- `src/Tokenizer/Diagnostics/CompilationEvent.cs`

**Modified files:**
- `src/Tokenizer/Diagnostics/DiagnosticEventType.cs` — remove 8 compilation members
- `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` — add `RecordCompilation`
- `src/Tokenizer/Diagnostics/CompilationDiagnosticCollector.cs` — implement `RecordCompilation`, no-op `Record`
- `src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs` — no-op `RecordCompilation`
- `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs` — no-op `RecordCompilation`
- `src/Tokenizer/Diagnostics/CompilationDiagnostics.cs` — change `Events` type
- `src/Tokenizer/Compilation/TemplateCompiler.cs` — attach diagnostics in catch-all
- `src/Tokenizer/Compilation/Binders/*.cs` — switch to `RecordCompilation`
- `tests/Tokenizer.Tests/Compilation/Binders/*.cs` — update for new method
- `tests/Tokenizer.Tests/Diagnostics/CompilationDiagnosticsTests.cs` — update for `CompilationEvent`
- `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs` — update for split

## Theme 2: TokenDiagnosticBuilder Fixes

**Issues:** H3, H5, M1, D2; tests H9, M15

### Preserve Original Issues on Blocked Tokens (H3)

`ApplyBlockedAnnotations` currently replaces `token.Issues` with a single `Blocked` issue. Change to merge:

```csharp
var issues = new List<DiagnosticIssue>(token.Issues)
{
    IssueFactory.CreateBlocked(token.TokenName, blockerName, diagnostics),
};
```

Original issues (e.g. `PreambleNeverFound` with near-miss hint) are preserved.

### Collect Preambles from PreambleMatched Events (H5)

Add `PreambleMatched` case to `CollectEvents` switch:

```csharp
case DiagnosticEventType.PreambleMatched:
    if (evt.TokenName != null && !string.IsNullOrEmpty(evt.Detail)
        && !data.PreambleTexts.ContainsKey(evt.TokenName))
    {
        data.PreambleTexts[evt.TokenName] = evt.Detail!;
    }
    break;
```

Keep existing `TokenMissed` fallback for tokens whose preamble was never matched.

### Guard Null TokenName in AddIssue (M1)

Replace `var tokenName = issue.TokenName!;` with early return:

```csharp
if (issue.TokenName == null)
    return;
```

### Make IssueFactory Injectable (D2)

Change `Build` to accept an optional `IssueFactory`:

```csharp
private static readonly IssueFactory DefaultIssueFactory = new(...);

public static (...) Build(DiagnosticResult diagnostics, IssueFactory? issueFactory = null)
{
    issueFactory ??= DefaultIssueFactory;
    ...
}
```

Pass `issueFactory` through to `CollectEvents`, `ClassifyOutcomes`, and `ApplyBlockedAnnotations` instead of using the static field.

### Tests

- **H9:** Unit test in `TokenDiagnosticBuilderTests` — record `BacktrackStarted` event, assert `TokenAttempt` has `AttemptOutcome.Backtracked`
- **M15:** Unit test — record `HintMissing` event with a `tokenName`, assert issue is attached to that token's `TokenDiagnostic` (not global)
- Update characterisation tests: blocked tokens now have both `PreambleNeverFound` and `Blocked` issues

### File Impact

- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`
- `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/Characterisation/CausalityChainTests.cs`

## Theme 3: Hint Generator Fixes

**Issues:** M3, M4, M5, M6, M9, M10, L7, L8, L9, D3

### Eliminate Throwaway DiagnosticIssue (M3)

Change `IHintGenerator.TryGenerateHint` signature:

```csharp
string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                        DiagnosticEvent sourceEvent, DiagnosticResult trace);
```

All 9 generators update to read `type` and `tokenName` from parameters instead of `issue.Type` and `issue.TokenName`. `IssueFactory.GenerateHint` passes the fields directly — no intermediate `DiagnosticIssue` allocation.

### ChainedDecoratorHintGenerator ReferenceEquals (M4, D3)

Keep `ReferenceEquals` — the type guard excludes synthetic events, making it safe. No comment needed; the code is self-documenting.

### MultipleRejectionHintGenerator Deduplication (M5)

Change "only fire on last rejection" guard from string equality to reference identity:

```csharp
if (!ReferenceEquals(last, sourceEvent))
    return null;
```

### ValueMismatchHintGenerator Dynamic Hint Text (M6)

`IssueFactory.CreateValueMismatch` sets `sourceEvent.Detail = missedTokenName`. The generator reads it:

```csharp
var missedToken = sourceEvent.Detail;
if (string.IsNullOrEmpty(missedToken))
    return "Consider adding an end delimiter (e.g. newline-terminated with '$') to prevent greedy capture.";

return $"Matched value may have captured the preamble of token '{missedToken}'. "
     + "Consider adding an end delimiter (e.g. newline-terminated with '$') to prevent greedy capture.";
```

### Pre-Index Events for Hint Generators (M9, M10)

Add internal indexed properties to `DiagnosticResult`:

```csharp
internal Dictionary<string, List<DiagnosticEvent>>? RejectionsPerToken { get; set; }
internal Dictionary<string, List<DiagnosticEvent>>? DecoratorSuccessesPerToken { get; set; }
```

Populated progressively during `CollectEvents`. For each event, the index is updated *before* any `IssueFactory.Create` call, so by the time a hint generator runs, the index only contains events that precede the current failure in chronological order. This enables generators to look up prior events without scanning `RawEvents`.

Add cases to the `CollectEvents` switch to populate the indexes:

```csharp
case DiagnosticEventType.ValidatorPassed:
case DiagnosticEventType.TransformerSucceeded:
    if (evt.TokenName != null)
        AddToIndex(diagnostics.DecoratorSuccessesPerToken, evt.TokenName, evt);
    break;
```

Rejection events (`ValidatorFailed`, `TransformerFailed`) are already handled — add index population at the top of those cases, before the `IssueFactory.Create` call:

```csharp
case DiagnosticEventType.ValidatorFailed:
    AddToIndex(diagnostics.RejectionsPerToken, evt.TokenName!, evt);
    // ... existing issue creation code ...
    break;
```

**MultipleRejectionHintGenerator** reads `trace.RejectionsPerToken` instead of calling `CollectRejections`.

**ChainedDecoratorHintGenerator** reads `trace.DecoratorSuccessesPerToken` to find prior successes instead of scanning `RawEvents`. Since the indexes are populated progressively during `CollectEvents` (before issues are created), the list only contains successes that precede the current failure. The generator takes the last element — no `ReferenceEquals` loop needed.

**RepeatingTokenHintGenerator (L7)** reads the last element of `trace.RejectionsPerToken[tokenName]` instead of LINQ `LastOrDefault` over `RawEvents`.

### Cache Input Lines (L8, L9)

Add internal cached property to `DiagnosticResult`:

```csharp
internal string[]? CachedInputLines { get; set; }
```

`PreambleNearMissHintGenerator` checks this first, populates on first access. Lines split once, reused across all missed tokens. Eliminates O(M*L) `String.Split` allocations.

### Tests

- **M11:** Fix `ChainedDecoratorHintGeneratorTests` to use actual trace event as `sourceEvent`. Add test with events after the failing event to verify loop termination.
- Update all hint generator tests for new `TryGenerateHint` signature
- Update `ValueMismatchHintGeneratorTests` for dynamic hint text

### File Impact

- `src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs` — new signature
- `src/Tokenizer/Diagnostics/Hints/*.cs` — all 9 generators updated
- `src/Tokenizer/Diagnostics/IssueFactory.cs` — drop throwaway allocation
- `src/Tokenizer/Diagnostics/DiagnosticResult.cs` — add indexed properties and cached lines
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` — populate indexes during `CollectEvents`
- `tests/Tokenizer.Tests/Diagnostics/Hints/*.cs` — all generator tests updated

## Theme 4: Logging/Observability

**Issues:** H2, H6, H8, M8

### Guard Diagnostic Logging Loop (H2)

Move the `foreach` over `result.Diagnostics.Tokens` inside the existing `IsEnabled(LogLevel.Debug)` guard in `FinalizeTokenization`. Consolidate into a single guarded block:

```csharp
if (result.Diagnostics != null && _log.IsEnabled(LogLevel.Debug))
{
    _log.LogDebug("{Verdict}", result.Diagnostics.Verdict);
    foreach (var token in result.Diagnostics.Tokens)
    {
        foreach (var issue in token.Issues)
        {
            _log.LogDebug("Token '{TokenName}': {Description}", issue.TokenName, issue.Description);
            if (issue.Hint != null)
                _log.LogDebug("  → Hint: {Hint}", issue.Hint);
        }
    }
    if (rawInput != null)
        _log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
}
```

### Log Diagnostic Issues at Warning (H6)

Add Warning-level logging for actual failures (non-matched tokens with issues) when `MissedCount > 0`:

```csharp
if (result.Diagnostics != null && result.Diagnostics.MissedCount > 0)
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
```

Debug logging retains full detail (hints, alignment). Warning provides production visibility with error codes for filtering.

### Attach Diagnostics to Runtime Exceptions (H8)

Both catch blocks in `Tokenizer.cs` tokenization method attach collected diagnostics:

```csharp
catch (TokenizerException ex)
{
    _log.LogError(ex, "Tokenization failed ...");
    ex.Data["Diagnostics"] = collector.GetResult();
    throw;
}
catch (Exception ex)
{
    _log.LogError(ex, "Unexpected error ...");
    ex.Data["Diagnostics"] = collector.GetResult();
    throw;
}
```

### IssueCodeMap Fallback (M8)

Replace `throw new ArgumentOutOfRangeException(...)` with:

```csharp
_ => $"TK???({(int)type})",
```

### File Impact

- `src/Tokenizer/Tokenizer.cs`
- `src/Tokenizer/Diagnostics/IssueCodeMap.cs`
- `tests/Tokenizer.Tests/Diagnostics/IssueCodeMapTests.cs` — update unknown-type test

## Theme 5: AlignmentRenderer

**Issues:** H1, L11, M13

### Filter Failures to Actual Failure Outcomes (H1)

The "Failures" section body renders every attempt on rejected tokens. Filter to only `ValidatorRejected` and `TransformerFailed` outcomes:

```csharp
foreach (var attempt in token.Attempts)
{
    if (attempt.Outcome != AttemptOutcome.ValidatorRejected &&
        attempt.Outcome != AttemptOutcome.TransformerFailed)
        continue;
    ...
}
```

The summary line failure count uses a `foreach` helper instead of LINQ `Sum` (also fixes L11):

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

### Test Blocked Tokens Rendering (M13)

Add test in `AlignmentRendererTests.cs` using real tokenization with ordered template where a prior required token fails. Assert:
- `⊘` marker present
- `BlockedBy` name appears
- Hint text appears
- `Blocked:` count in summary line

### File Impact

- `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`
- `tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs`

## Theme 6: Lazy Init / EnsureBuilt

**Issues:** M2, D1, L12

### Atomic Lazy Init (M2, D1)

Replace 5 separate cached fields with a single record:

```csharp
private sealed record BuiltResult(
    IReadOnlyList<TokenDiagnostic> Tokens,
    string Verdict,
    int MatchedCount,
    int MissedCount,
    int TotalCount);

private BuiltResult? _built;

private BuiltResult GetBuilt()
{
    if (_built != null)
        return _built;

    var (tokens, verdict, matched, missed, total) = TokenDiagnosticBuilder.Build(this);
    _built = new BuiltResult(tokens, verdict, matched, missed, total);
    return _built;
}
```

Properties delegate to `GetBuilt()`:

```csharp
public IReadOnlyList<TokenDiagnostic> Tokens => GetBuilt().Tokens;
public string Verdict => GetBuilt().Verdict;
public int MatchedCount => GetBuilt().MatchedCount;
public int MissedCount => GetBuilt().MissedCount;
public int TotalCount => GetBuilt().TotalCount;
```

Single reference assignment is atomic on .NET — eliminates the partial-visibility window.

### Test RenderAlignment Caching (L12)

Add to `DiagnosticResultTests.cs`:

```csharp
var first = diagnostics.RenderAlignment();
var second = diagnostics.RenderAlignment();
Assert.Same(first, second);
```

### File Impact

- `src/Tokenizer/Diagnostics/DiagnosticResult.cs`
- `tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs`

## Theme 7: Doc/Style Cleanup

**Issues:** L1, L2, L3, L4, L6

### TK006 Gap Comment (L1)

Add to `IssueCodeMap.cs`:

```csharp
DiagnosticIssueType.RepeatingTokenCutShort => "TK005",
// TK006: reserved
DiagnosticIssueType.HintMissing => "TK007",
```

### Fix Stale Doc Comment (L2)

`IDiagnosticCollector.cs` line 22: change `"store it (DiagnosticCollector)"` to `"store it (RuntimeDiagnosticCollector)"`.

### Fix Visibility (L3)

`IssueCodeMap.cs`: change `public static string GetCode(...)` to `internal static string GetCode(...)`.

### Remove Unnecessary Coupling Comment (L4)

No change needed — code is self-documenting.

### Rename Short-Circuit Test (L6)

Rename `GivenMultipleValidators_WhenFirstFails_ThenFirstRejectionRecorded` to `GivenMultipleValidators_WhenFirstFails_ThenEngineShortCircuits_OnlyFirstRejectionRecorded`.

### File Impact

- `src/Tokenizer/Diagnostics/IssueCodeMap.cs`
- `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`
- `tests/Tokenizer.Tests/Diagnostics/Characterisation/ValidatorRejectionTests.cs`

## Theme 8: Test Gaps

**Issues:** M7, M12, M14, L5, L13, D4

Tests mentioned in earlier themes (H9, M11, M13, M15, L12) are implemented alongside those themes. This theme covers standalone test additions.

### First Transformer in Chain Fails (M7)

Add to `TransformerFailureTests.cs`:

```
GivenChainedTransformers_WhenFirstFails_ThenSecondNeverReached
```

Template with `ToDateTime('yyyy-MM-dd'), ToUpper` applied to input `"bad"`. Assert single `TransformerFailure` issue for `ToDateTime`, no issue for `ToUpper`.

### CreateValueMismatch Unit Test (M12)

Add to `IssueFactoryTests.cs`: call `CreateValueMismatch("tokenA", "tokenB", diagnostics)`, assert:
- Issue type is `ValueMismatch`
- Token name is `"tokenA"`
- Description contains `"tokenB"`
- Hint contains dynamic text (after M6 fix)

### IsEnabled Assertions (M14)

Add to `DiagnosticCollectorTests.cs`:

```csharp
Assert.False(NullDiagnosticCollector.Instance.IsEnabled);
Assert.True(new RuntimeDiagnosticCollector("x").IsEnabled);
Assert.True(new CompilationDiagnosticCollector().IsEnabled);
```

### Graduate NewlineTerminated Test (L5)

`EdgeCaseTests.NewlineTerminated`: add assertion on `NewlineTerminatedTokenProcessed` event count instead of only logging.

### NullDiagnosticCollector.GetCompilationResult (L13)

Add `Assert.Null(NullDiagnosticCollector.Instance.GetCompilationResult())` to existing null collector test.

### Strengthen Weak Characterisation Assertions (D4)

Characterisation tests touched by other themes get structural assertions added during those changes. Any remaining `Assert.NotNull`-only tests are strengthened during this theme.

### File Impact

- `tests/Tokenizer.Tests/Diagnostics/Characterisation/TransformerFailureTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/Characterisation/EdgeCaseTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/IssueFactoryTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs`

## Issue Cross-Reference

| Issue | Theme | Section |
|-------|-------|---------|
| H1 | 5 | Filter Failures |
| H2 | 4 | Guard Logging Loop |
| H3 | 2 | Preserve Original Issues |
| H4 | 1 | Split DiagnosticEventType |
| H5 | 2 | Collect Preambles |
| H6 | 4 | Log at Warning |
| H7 | 1 | Attach in Catch-All |
| H8 | 4 | Attach to Runtime Exceptions |
| H9 | 2 | BacktrackStarted Test |
| M1 | 2 | Guard Null TokenName |
| M2 | 6 | Atomic Lazy Init |
| M3 | 3 | Eliminate Throwaway Issue |
| M4 | 3 | ChainedDecorator ReferenceEquals |
| M5 | 3 | MultipleRejection Dedup |
| M6 | 3 | ValueMismatch Hint Text |
| M7 | 8 | First Transformer Fails Test |
| M8 | 4 | IssueCodeMap Fallback |
| M9 | 3 | Pre-Index Rejections |
| M10 | 3 | Pre-Index Decorator Successes |
| M11 | 3 | ChainedDecorator Test Fix |
| M12 | 8 | CreateValueMismatch Test |
| M13 | 5 | Blocked Tokens Rendering Test |
| M14 | 8 | IsEnabled Tests |
| M15 | 2 | Per-Token HintMissing Test |
| L1 | 7 | TK006 Comment |
| L2 | 7 | Stale Doc Comment |
| L3 | 7 | Visibility Fix |
| L4 | 7 | No Change |
| L5 | 8 | Graduate NewlineTerminated |
| L6 | 7 | Rename Test |
| L7 | 3 | RepeatingToken Via Index |
| L8 | 3 | Cache Input Lines |
| L9 | 3 | Cache Normalized Lines |
| L10 | — | Accepted (no change) |
| L11 | 5 | CountFailures Helper |
| L12 | 6 | RenderAlignment Caching Test |
| L13 | 8 | GetCompilationResult Test |
| D1 | 6 | Atomic Lazy Init |
| D2 | 2 | Injectable IssueFactory |
| D3 | 3 | ChainedDecorator ReferenceEquals |
| D4 | 8 | Strengthen Assertions |
