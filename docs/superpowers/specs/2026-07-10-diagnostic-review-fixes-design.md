# Diagnostic Review Fixes & Hint Generator Enrichment

## Problem Statement

The diagnostics branch code review identified 31 issues across code quality, test coverage, performance, and spec compliance. Additionally, the hint generator system has significant coverage gaps and missing generators that would make diagnostic output more actionable. Two issue types (`ValueMismatch`, `UnmatchedInputSection`) exist as placeholders with no producers.

This spec addresses all review findings and enriches the hint generator system.

## Design Goals

1. Fix all Critical/High/Medium/Low issues from the review
2. Implement `ValueMismatch` detection (greedy capture detection)
3. Add 5 new hint generators for richer diagnostic output
4. Remove `UnmatchedInputSection` (requires engine changes outside scope)
5. Upgrade test assertions from string comparison to structured properties
6. Achieve integration-level hint coverage for all generators

## Non-Goals

- Implementing `UnmatchedInputSection` (needs engine-level input range tracking)
- Changing the tokenization engine itself
- Performance optimization beyond the identified issues

## Structural Changes

### 1. TokenDiagnosticBuilder Decomposition

Split the 200-line `Build` method into 4 clearly-scoped private methods:

- **`CollectEvents(DiagnosticResult)`** — single pass over `RawEvents`. Populates dictionaries for attempts, token order, token IDs, assigned tokens, preamble texts. Collects `matchedCount`, `missedCount`, `tokensWithFailures`, `missedTokenNames` sets. Stores description strings per event to avoid duplicate `StringBuilder` allocations. Returns a `CollectedEventData` record containing all aggregated state.
- **`ClassifyOutcomes(CollectedEventData, DiagnosticResult)`** — iterates token order, determines `TokenOutcome` per token, builds the `TokenDiagnostic` list. Runs `ValueMismatch` detection (see Section 4). Returns `List<TokenDiagnostic>`.
- **`ApplyBlockedAnnotations(List<TokenDiagnostic>, HashSet<string>)`** — unchanged logic, uses `IssueFactory.CreateBlockedIssue` instead of inline construction.
- **`BuildVerdict(int matched, int total, int missed)`** — unchanged, takes counts from `CollectedEventData`.

`CollectedEventData` is an internal record/class holding the dictionaries and counts. Not public API.

### 2. IssueFactory Extraction

New internal class `IssueFactory` in `Tokens.Diagnostics` namespace:

```csharp
internal sealed class IssueFactory
{
    private readonly IHintGenerator[] _generators;

    public IssueFactory(IHintGenerator[] generators);

    public DiagnosticIssue Create(
        DiagnosticIssueType type,
        DiagnosticEvent sourceEvent,
        string description,
        DiagnosticResult diagnostics);

    public DiagnosticIssue CreateBlocked(
        string tokenName,
        string blockerName,
        DiagnosticResult diagnostics);
}
```

**Key change from current code:** `Create` generates the hint first via the generator chain, then constructs a single `DiagnosticIssue` instance. Eliminates the create-then-clone anti-pattern (C1).

`CreateBlocked` constructs a `Blocked` issue with `DiagnosticIssueType.Blocked` and runs the hint generator chain (which will hit `BlockedTokenHintGenerator`). Eliminates the inline construction fragility (M7).

The `IHintGenerator[]` array moves from `TokenDiagnosticBuilder` to `IssueFactory`.

### 3. DiagnosticCollector Split

Replace the dual-mode `DiagnosticCollector` with two classes:

**`RuntimeDiagnosticCollector`:**
```csharp
internal sealed class RuntimeDiagnosticCollector : IDiagnosticCollector
{
    public RuntimeDiagnosticCollector(string? inputContent);
    public bool IsEnabled => true;
    public void Record(...);
    public DiagnosticResult? GetResult();
    public CompilationDiagnostics? GetCompilationResult() => null;
}
```

**`CompilationDiagnosticCollector`:**
```csharp
internal sealed class CompilationDiagnosticCollector : IDiagnosticCollector
{
    public CompilationDiagnosticCollector();
    public bool IsEnabled => true;
    public void Record(...);
    public DiagnosticResult? GetResult() => null;
    public CompilationDiagnostics? GetCompilationResult();
}
```

Callers already know which mode — `TemplateCompiler` creates compilation, `Tokenizer` creates runtime.

### 4. DiagnosticResult Temporal Coupling Fix

`OutOfOrderTokens` (bool) and `OptionalTokenNames` (HashSet) become constructor parameters stored as internal readonly properties:

```csharp
internal DiagnosticResult(string? inputContent, bool outOfOrderTokens, HashSet<string> optionalTokenNames)
```

`Tokenizer.cs` passes these at construction time instead of setting them after. `EnsureBuilt()` passes them to `TokenDiagnosticBuilder.Build`. Hint generators access `OptionalTokenNames` via `diagnostics.OptionalTokenNames` (needed by `OptionalTokenHintGenerator`).

## DiagnosticIssueType & IssueCodeMap Changes

### Add `Blocked` (C3)

```csharp
// DiagnosticIssueType
Blocked,  // "Token was not searched for because a prior required token failed to match."

// IssueCodeMap
DiagnosticIssueType.Blocked => "TK008",
```

### Remove `UnmatchedInputSection` (M2)

- Remove from `DiagnosticIssueType` enum
- Remove from `IssueCodeMap` (TK006 retired, not reassigned)
- Delete `UnmatchedInputHintGenerator.cs` and its test file

### Implement `ValueMismatch` Detection

In `ClassifyOutcomes`, after building matched tokens: for each matched token, check if its `AssignedValue` contains the preamble text of any *missed or rejected* token in the template. Only flag when a missed/rejected token's preamble appears inside a matched token's value — that's the actual greedy capture signal. If so, add a `ValueMismatch` issue to the matched token via `IssueFactory`.

Preamble texts are collected during `CollectEvents` from `PreambleMatched` events into a `Dictionary<string, string>` mapping token name to preamble.

### IssueCodeMap Default (L2)

Change `_ => "TK000"` to `_ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown DiagnosticIssueType")`.

## Hint Generators

### Updated Generator Array (priority order)

1. `BlockedTokenHintGenerator` (new)
2. `ChainedDecoratorHintGenerator` (new)
3. `DateFormatHintGenerator` (existing)
4. `MultipleRejectionHintGenerator` (new)
5. `ValueMismatchHintGenerator` (new)
6. `PreambleNearMissHintGenerator` (existing)
7. `ValidatorValueHintGenerator` (existing)
8. `OptionalTokenHintGenerator` (new)
9. `RepeatingTokenHintGenerator` (existing)

### New Generators

**`BlockedTokenHintGenerator`:**
- Fires for: `DiagnosticIssueType.Blocked`
- Source: blocker name from `sourceEvent.Detail`
- Hint: `"Fix '{blockerName}' first — this token may match once '{blockerName}' is resolved."`
- Replaces the hardcoded hint in `ApplyBlockedAnnotations`

**`ChainedDecoratorHintGenerator`:**
- Fires for: `ValidatorRejection` and `TransformerFailure` when the token has 2+ decorator events in the trace
- Detection: searches `diagnostics.RawEvents` for other decorator events (TransformerSucceeded, ValidatorPassed) on the same token that appear before the failing event
- Hint: `"Decorator chain: '{successfulDecorator}' succeeded → '{failingDecorator}' {rejected/failed on} value '{value}'."`
- Falls through to type-specific generators for single-decorator cases

**`OptionalTokenHintGenerator`:**
- Fires for: `PreambleNeverFound` when `diagnostics.OptionalTokenNames` contains the token name
- Hint: `"Token '{name}' is optional — no action needed unless you expected it to match."`

**`MultipleRejectionHintGenerator`:**
- Fires for: `ValidatorRejection` and `TransformerFailure` when the same token has 2+ rejection attempts in the trace
- Only fires on the last rejection event for that token (avoids repeating the hint)
- Detection: counts `ValidatorFailed`/`TransformerFailed` events for this token name in the trace
- Hint: `"Token was rejected {n} times. Values tried: '{val1}' (line {l1}), '{val2}' (line {l2})."`

**`ValueMismatchHintGenerator`:**
- Fires for: `DiagnosticIssueType.ValueMismatch`
- Hint: `"Value contains '{preamble}' which is the preamble of token '{otherToken}'. Consider adding an end delimiter (e.g. newline-terminated with '$') to prevent greedy capture."`
- Requires the issue's `Description` to contain the other token's name (embedded by the detection logic in `ClassifyOutcomes`)

### Existing Generator Fixes

**`PreambleNearMissHintGenerator` (M12):**
Fix `string.Split` to split on `"\n"` and trim trailing `\r` from each line, avoiding ghost empty entries from `\r\n`.

## Public API Changes

### DiagnosticResult — New Properties

```csharp
public int MatchedCount { get; }
public int MissedCount { get; }
public int TotalCount { get; }
```

Computed by `TokenDiagnosticBuilder.Build`, stored as readonly fields. Enables structured test assertions.

### Init Setter Visibility (M3)

Change `{ get; init; }` to `{ get; internal init; }` on:
- `TokenDiagnostic` — all properties
- `TokenAttempt` — all properties
- `DiagnosticIssue` — all properties

### Doc Comment Fix (D1)

`TokenOutcome.Blocked` comment changes from "Defined but not populated until Phase 6" to "Token was not searched for because a prior required token failed to match."

## Performance Fixes

### Single-Pass Event Collection (M5)

`CollectEvents` builds all dictionaries and counts in one `foreach`. Eliminates:
- Per-token `events.Any()` scan (was O(tokens × events))
- Two `events.Count()` full scans at the end

### Deduplicate Description Building (M6)

`CollectEvents` stores the description string in a local variable, passes it to both `TokenAttempt.Reason` and `IssueFactory.Create`. One `StringBuilder` allocation per event instead of two.

### Single-Pass AlignmentRenderer (M8)

Replace 4x `.Where().ToList()` with single `foreach` sorting tokens into 4 lists by `Outcome`.

### Thread Safety Documentation (L1)

Add XML doc to `DiagnosticResult`:
```
/// Thread safety: this type is not thread-safe. Designed for single-threaded
/// access after tokenization completes.
```

## Test Changes

### Infrastructure

**Extract `TokenizeWithDiagnostics` to `TokenizerTestBase` (M1):**
```csharp
protected TokenizeResult TokenizeWithDiagnostics(string template, string input)
{
    var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
    var compiled = tokenizer.Compile(template).Template;
    var result = tokenizer.Tokenize(compiled, input);
    Output.WriteLine(result.Diagnostics!.RenderAlignment());
    return result;
}
```

All 11 characterisation files drop their private copies.

### Verdict Assertion Migration (M4)

All ~20 tests switch from:
```csharp
Assert.Equal("Matched 2 of 2 tokens.", diagnostics.Verdict);
```
To:
```csharp
Assert.Equal(2, diagnostics.MatchedCount);
Assert.Equal(0, diagnostics.MissedCount);
```

### Error Code Assertions (C2)

Each characterisation test that asserts on an issue type also asserts the code:
```csharp
Assert.Equal(DiagnosticIssueType.PreambleNeverFound, issue.Type);
Assert.Equal("TK001", issue.Code);
```

### Hint Content Assertions (H4)

Upgrade `Assert.NotNull(issue.Hint)` to content assertions:
- `DateFormatHintGenerator`: hint contains the suggested format string
- `PreambleNearMissHintGenerator`: hint contains "case difference" or similar
- `ValidatorValueHintGenerator`: hint contains the rejected value
- `RepeatingTokenHintGenerator`: hint describes the cut-short reason

### Missing Failure Mode Tests (H6, H7, M9, M10)

| Test | File | Scenario |
|------|------|----------|
| Multiple validators both failing | `ValidatorRejectionTests` | `IsNumeric, IsEmail` on `"hello"` — both reject |
| First transformer in chain fails | `TransformerFailureTests` | `ToDateTime('yyyy-MM-dd'), ToUpper` on `"bad"` — first fails, second never reached |
| Blocked + optional interleaved | `CausalityChainTests` | `A, B?, C, D` where C missing — D blocked by C, B skipped |
| Repeating token + transformer | `RepeatingTokenTests` | `Repeating, ToDateTime('yyyy-MM-dd')` with mixed valid/invalid dates |

### Graduate Output.WriteLine Tests (H5)

Tests that log without asserting get real assertions:
- `EdgeCaseTests.NewlineTerminated`: assert on `NewlineTerminatedTokenProcessed` event count
- `TransformerFailureTests.WhenFormatDiffers`: assert hint contains suggested format
- `RepeatingTokenTests.WhenOneMatchThenFailure`: assert verdict via `MatchedCount`/`MissedCount`

### Other Test Fixes

- Unicode test asserts `"José"` value (L3)
- `DiagnosticOutputFormatTests` assert structural properties where possible, keep renderer string tests as focused unit tests (L4)

### New Generator Tests

Each new generator gets:
- **Unit test** in `tests/Tokenizer.Tests/Diagnostics/Hints/{Generator}Tests.cs` — generator in isolation with crafted inputs
- **Integration test** in the appropriate characterisation file — real tokenization, assert hint content

## File Impact

### New Files
- `src/Tokenizer/Diagnostics/IssueFactory.cs`
- `src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/CompilationDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/Hints/BlockedTokenHintGenerator.cs`
- `src/Tokenizer/Diagnostics/Hints/ChainedDecoratorHintGenerator.cs`
- `src/Tokenizer/Diagnostics/Hints/OptionalTokenHintGenerator.cs`
- `src/Tokenizer/Diagnostics/Hints/MultipleRejectionHintGenerator.cs`
- `src/Tokenizer/Diagnostics/Hints/ValueMismatchHintGenerator.cs`
- `tests/Tokenizer.Tests/Diagnostics/Hints/BlockedTokenHintGeneratorTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/Hints/ChainedDecoratorHintGeneratorTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/Hints/OptionalTokenHintGeneratorTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/Hints/MultipleRejectionHintGeneratorTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/Hints/ValueMismatchHintGeneratorTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/IssueFactoryTests.cs`

### Modified Files
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` (decomposed)
- `src/Tokenizer/Diagnostics/DiagnosticResult.cs` (constructor change, new properties)
- `src/Tokenizer/Diagnostics/DiagnosticIssue.cs` (internal init)
- `src/Tokenizer/Diagnostics/TokenDiagnostic.cs` (internal init)
- `src/Tokenizer/Diagnostics/TokenAttempt.cs` (internal init)
- `src/Tokenizer/Diagnostics/TokenOutcome.cs` (doc fix)
- `src/Tokenizer/Diagnostics/DiagnosticIssueType.cs` (add Blocked, remove UnmatchedInputSection)
- `src/Tokenizer/Diagnostics/IssueCodeMap.cs` (add TK008, remove TK006, throw on unknown)
- `src/Tokenizer/Diagnostics/AlignmentRenderer.cs` (single-pass)
- `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` (no change, both new collectors implement it)
- `src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs` (line split fix)
- `src/Tokenizer/Tokenizer.cs` (pass optionalTokenNames + outOfOrderTokens at DiagnosticResult construction)
- `src/Tokenizer/Compilation/TemplateCompiler.cs` (use CompilationDiagnosticCollector)
- `tests/Tokenizer.Tests/TokenizerTestBase.cs` (add TokenizeWithDiagnostics)
- All 11 characterisation test files (assertion upgrades)
- `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs` (updated for split)
- `tests/Tokenizer.Tests/Diagnostics/IssueCodeMapTests.cs` (add TK008, remove TK006)
- `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs` (updated for decomposition)

### Deleted Files
- `src/Tokenizer/Diagnostics/DiagnosticCollector.cs` (replaced by two collectors)
- `src/Tokenizer/Diagnostics/Hints/UnmatchedInputHintGenerator.cs`
- `tests/Tokenizer.Tests/Diagnostics/Hints/UnmatchedInputHintGeneratorTests.cs`
