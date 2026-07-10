# Diagnostic Review Fixes & Hint Generator Enrichment — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Address all 31 code review issues and add 5 new hint generators to the diagnostics system.

**Architecture:** Extract `IssueFactory` from `TokenDiagnosticBuilder`, decompose the 200-line `Build` method into 4 phases, split `DiagnosticCollector` into runtime/compilation variants, add structured count properties to `DiagnosticResult`, implement `ValueMismatch` detection, and add 5 new hint generators. All changes are TDD — failing test first, then implementation.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 8.0 + .NET 10.0, xUnit, NSubstitute

## Global Constraints

- Target frameworks: .NET Standard 2.0, .NET 8.0, .NET 10.0 — use `#if` for framework-specific APIs
- Braces: Allman style
- Private fields: `_camelCase`
- Test naming: `GivenScenario_WhenAction_ThenResult()`
- No `#region` blocks
- All tests must pass: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
- Build must be clean: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
- Commit after each task

---

### Task 1: Enum and code map cleanup — add Blocked, remove UnmatchedInputSection

Small foundational changes that later tasks depend on.

**Files:**
- Modify: `src/Tokenizer/Diagnostics/DiagnosticIssueType.cs`
- Modify: `src/Tokenizer/Diagnostics/IssueCodeMap.cs`
- Modify: `src/Tokenizer/Diagnostics/TokenOutcome.cs`
- Modify: `tests/Tokenizer.Tests/Diagnostics/IssueCodeMapTests.cs`
- Delete: `src/Tokenizer/Diagnostics/Hints/UnmatchedInputHintGenerator.cs`
- Delete: `tests/Tokenizer.Tests/Diagnostics/Hints/UnmatchedInputHintGeneratorTests.cs`

**Interfaces:**
- Produces: `DiagnosticIssueType.Blocked` enum member, `IssueCodeMap.GetCode(DiagnosticIssueType.Blocked)` → `"TK008"`

- [ ] **Step 1: Update `IssueCodeMapTests` — add TK008 test, remove TK006 test, add unknown-type-throws test**

In `tests/Tokenizer.Tests/Diagnostics/IssueCodeMapTests.cs`:
- Add `[InlineData]` for `DiagnosticIssueType.Blocked, "TK008"`
- Remove the `[InlineData]` for `DiagnosticIssueType.UnmatchedInputSection, "TK006"`
- Add a new test:

```csharp
[Fact]
public void GivenUnknownIssueType_WhenGettingCode_ThenThrowsArgumentOutOfRange()
{
    // Arrange
    var unknownType = (DiagnosticIssueType)999;

    // Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => IssueCodeMap.GetCode(unknownType));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IssueCodeMapTests"`
Expected: Failures — `Blocked` doesn't exist yet, `UnmatchedInputSection` test data removed but enum still exists, unknown type returns `"TK000"` instead of throwing.

- [ ] **Step 3: Update `DiagnosticIssueType` — add `Blocked`, remove `UnmatchedInputSection`**

In `src/Tokenizer/Diagnostics/DiagnosticIssueType.cs`:
- Remove the `UnmatchedInputSection` member and its XML doc comment
- Add after `HintMissing`:

```csharp
/// <summary>
/// Token was not searched for because a prior required token failed to match.
/// </summary>
Blocked,
```

- [ ] **Step 4: Update `IssueCodeMap` — add TK008, remove TK006, throw on unknown**

In `src/Tokenizer/Diagnostics/IssueCodeMap.cs`:
- Remove the `DiagnosticIssueType.UnmatchedInputSection => "TK006",` line
- Add `DiagnosticIssueType.Blocked => "TK008",` before the default case
- Change `_ => "TK000",` to `_ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown DiagnosticIssueType"),`

- [ ] **Step 5: Fix `TokenOutcome.Blocked` doc comment**

In `src/Tokenizer/Diagnostics/TokenOutcome.cs`, replace the Blocked member's doc comment:

```csharp
/// <summary>
/// Token was not searched for because a prior required token
/// failed to match.
/// </summary>
Blocked,
```

- [ ] **Step 6: Delete `UnmatchedInputHintGenerator` and its test**

Delete:
- `src/Tokenizer/Diagnostics/Hints/UnmatchedInputHintGenerator.cs`
- `tests/Tokenizer.Tests/Diagnostics/Hints/UnmatchedInputHintGeneratorTests.cs`

- [ ] **Step 7: Remove `UnmatchedInputHintGenerator` from `TokenDiagnosticBuilder` generator array**

In `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`, remove the `new UnmatchedInputHintGenerator(),` line from the `HintGenerators` array.

- [ ] **Step 8: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: Build clean, all tests pass.

- [ ] **Step 9: Commit**

```bash
git add -A && git status
git commit -m "Add Blocked issue type (TK008), remove UnmatchedInputSection (TK006)"
```

---

### Task 2: Init setter visibility + public API properties

Tighten public API types and add structured count properties.

**Files:**
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnostic.cs`
- Modify: `src/Tokenizer/Diagnostics/TokenAttempt.cs`
- Modify: `src/Tokenizer/Diagnostics/DiagnosticIssue.cs`
- Modify: `src/Tokenizer/Diagnostics/DiagnosticResult.cs`
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`
- Modify: `tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs`

**Interfaces:**
- Produces: `DiagnosticResult.MatchedCount`, `DiagnosticResult.MissedCount`, `DiagnosticResult.TotalCount` (int properties)

- [ ] **Step 1: Write failing tests for structured count properties**

In `tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs`, add:

```csharp
[Fact]
public void GivenFullMatch_WhenCheckingCounts_ThenMatchedCountEqualsTotal()
{
    // Arrange
    var tokenizer = CreateDiagnosticTokenizer();
    var compiled = tokenizer.Compile("Name: { Name }").Template;

    // Act
    var result = tokenizer.Tokenize(compiled, "Name: Alice");

    // Assert
    var diagnostics = result.Diagnostics!;
    Assert.Equal(1, diagnostics.MatchedCount);
    Assert.Equal(0, diagnostics.MissedCount);
    Assert.Equal(1, diagnostics.TotalCount);
}

[Fact]
public void GivenPartialMatch_WhenCheckingCounts_ThenMissedCountReflectsMisses()
{
    // Arrange
    var tokenizer = CreateDiagnosticTokenizer();
    var compiled = tokenizer.Compile("A: { A }\nB: { B }").Template;

    // Act
    var result = tokenizer.Tokenize(compiled, "A: one");

    // Assert
    var diagnostics = result.Diagnostics!;
    Assert.Equal(1, diagnostics.MatchedCount);
    Assert.Equal(1, diagnostics.MissedCount);
    Assert.Equal(2, diagnostics.TotalCount);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticResultTests"`
Expected: Fails — `MatchedCount`, `MissedCount`, `TotalCount` don't exist.

- [ ] **Step 3: Add count properties to `DiagnosticResult` and update `TokenDiagnosticBuilder.Build` return type**

In `src/Tokenizer/Diagnostics/DiagnosticResult.cs`, add private fields:
```csharp
private int _matchedCount;
private int _missedCount;
private int _totalCount;
```

Add public properties with lazy init via `EnsureBuilt()`:
```csharp
/// <summary>
/// Number of tokens that were successfully matched.
/// </summary>
public int MatchedCount { get { EnsureBuilt(); return _matchedCount; } }

/// <summary>
/// Number of tokens that were missed (not matched).
/// </summary>
public int MissedCount { get { EnsureBuilt(); return _missedCount; } }

/// <summary>
/// Total number of tokens in the template.
/// </summary>
public int TotalCount { get { EnsureBuilt(); return _totalCount; } }
```

Update `EnsureBuilt()`:
```csharp
private void EnsureBuilt()
{
    if (_tokens != null)
        return;

    var (tokens, verdict, matchedCount, missedCount, totalCount) = TokenDiagnosticBuilder.Build(this);
    _tokens = tokens;
    _verdict = verdict;
    _matchedCount = matchedCount;
    _missedCount = missedCount;
    _totalCount = totalCount;
}
```

Add thread-safety doc to the class:
```xml
/// <remarks>
/// Thread safety: this type is not thread-safe. Designed for single-threaded
/// access after tokenization completes.
/// </remarks>
```

In `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`, change `Build` signature and return:
```csharp
public static (IReadOnlyList<TokenDiagnostic> tokens, string verdict, int matchedCount, int missedCount, int totalCount) Build(DiagnosticResult diagnostics)
```
Update the return at the end to: `return (result, verdict, matchedCount, missedCount, totalCount);`

- [ ] **Step 4: Change init setters to `internal init`**

In `TokenDiagnostic.cs`, `TokenAttempt.cs`, `DiagnosticIssue.cs`: change `{ get; init; }` to `{ get; internal init; }` on all properties. Keep `DiagnosticIssue.Code` as `get`-only (computed).

- [ ] **Step 5: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: Build clean, all tests pass.

- [ ] **Step 6: Commit**

```bash
git add -A && git status
git commit -m "Add MatchedCount/MissedCount/TotalCount to DiagnosticResult, tighten init setters"
```

---

### Task 3: Split DiagnosticCollector into Runtime and Compilation variants

**Files:**
- Create: `src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs`
- Create: `src/Tokenizer/Diagnostics/CompilationDiagnosticCollector.cs`
- Delete: `src/Tokenizer/Diagnostics/DiagnosticCollector.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/Compilation/TemplateCompiler.cs`
- Modify: `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs`

**Interfaces:**
- Consumes: `IDiagnosticCollector` interface (unchanged)
- Produces: `RuntimeDiagnosticCollector(string? inputContent)`, `CompilationDiagnosticCollector()`

- [ ] **Step 1: Update `DiagnosticCollectorTests` for split — replace constructor calls, add null-return tests**

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Create `RuntimeDiagnosticCollector` and `CompilationDiagnosticCollector`**

Each implements `IDiagnosticCollector`. `RuntimeDiagnosticCollector` takes `string? inputContent` and creates a `DiagnosticResult`. `CompilationDiagnosticCollector` creates a `CompilationDiagnostics`. Each returns null for the other's getter.

- [ ] **Step 4: Update callers and delete old `DiagnosticCollector`**

In `Tokenizer.cs:257`: `new RuntimeDiagnosticCollector(rawInput)`. In `TemplateCompiler.cs:34`: `new CompilationDiagnosticCollector()`. Delete `DiagnosticCollector.cs`.

- [ ] **Step 5: Build and run all tests**

- [ ] **Step 6: Commit**

```bash
git add -A && git status
git commit -m "Split DiagnosticCollector into RuntimeDiagnosticCollector and CompilationDiagnosticCollector"
```

---

### Task 4: Fix DiagnosticResult temporal coupling

**Files:**
- Modify: `src/Tokenizer/Diagnostics/DiagnosticResult.cs`
- Modify: `src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`

**Interfaces:**
- Consumes: `RuntimeDiagnosticCollector` from Task 3
- Produces: `DiagnosticResult` with readonly `OutOfOrderTokens` and `OptionalTokenNames`

- [ ] **Step 1: Update `DiagnosticResult` constructor to accept `outOfOrderTokens` and `optionalTokenNames`, make properties readonly**

- [ ] **Step 2: Update `RuntimeDiagnosticCollector` constructor to accept and forward params**

- [ ] **Step 3: Update `Tokenizer.cs` — pass params at construction, remove post-construction setters in `FinalizeTokenization`**

- [ ] **Step 4: Build and run all tests**

- [ ] **Step 5: Commit**

```bash
git add -A && git status
git commit -m "Fix DiagnosticResult temporal coupling: pass OutOfOrderTokens and OptionalTokenNames at construction"
```

---

### Task 5: Extract IssueFactory and decompose TokenDiagnosticBuilder

**Files:**
- Create: `src/Tokenizer/Diagnostics/IssueFactory.cs`
- Create: `src/Tokenizer/Diagnostics/Hints/BlockedTokenHintGenerator.cs`
- Create: `tests/Tokenizer.Tests/Diagnostics/IssueFactoryTests.cs`
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`

**Interfaces:**
- Consumes: `IHintGenerator`, `DiagnosticIssue`, `DiagnosticEvent`, `DiagnosticResult`
- Produces: `IssueFactory.Create(...)`, `IssueFactory.CreateBlocked(...)`

- [ ] **Step 1: Write `IssueFactory` unit tests** — test Create with no generators (null hint), Create with generator (hint populated), CreateBlocked (type=Blocked, hint mentions blocker)

- [ ] **Step 2: Run tests to verify they fail**

- [ ] **Step 3: Create `BlockedTokenHintGenerator`** — fires for `DiagnosticIssueType.Blocked`, reads blocker name from `sourceEvent.Detail`, produces "Fix '{blockerName}' first" hint

- [ ] **Step 4: Create `IssueFactory`** — `Create` generates hint first then constructs single issue. `CreateBlocked` creates synthetic event with blocker in Detail, delegates to hint chain. `GenerateHint` iterates generators, first non-null wins.

- [ ] **Step 5: Run `IssueFactoryTests` to verify they pass**

- [ ] **Step 6: Decompose `TokenDiagnosticBuilder.Build`**

Split into `CollectEvents` (single-pass, returns all dictionaries + counts), `ClassifyOutcomes` (builds TokenDiagnostic list), updated `ApplyBlockedAnnotations` (uses `IssueFactory.CreateBlocked`, type=`Blocked`), `BuildVerdict`. Replace `AddIssue`/`CreateIssue`/`GenerateHint` with `IssueFactory`. Store description strings in locals to avoid duplicate `BuildValidatorDescription`/`BuildTransformerDescription` calls. Fix performance: no per-token `events.Any()` scan, no `events.Count()` at end.

- [ ] **Step 7: Build and run all tests** — blocked token tests may need update from `PreambleNeverFound` to `Blocked` issue type

- [ ] **Step 8: Commit**

```bash
git add -A && git status
git commit -m "Extract IssueFactory, decompose TokenDiagnosticBuilder.Build into phases"
```

---

### Task 6: Performance fixes — AlignmentRenderer + PreambleNearMiss

**Files:**
- Modify: `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs`

- [ ] **Step 1: Run baseline tests**

- [ ] **Step 2: Replace 4x `.Where().ToList()` in `AlignmentRenderer` with single `foreach` + switch on `Outcome`**

- [ ] **Step 3: Fix `PreambleNearMissHintGenerator` — split on `'\n'`, trim `'\r'` from each line**

- [ ] **Step 4: Run all tests, verify no regressions**

- [ ] **Step 5: Commit**

```bash
git add -A && git status
git commit -m "Performance: single-pass AlignmentRenderer, fix PreambleNearMiss line splitting"
```

---

### Task 7: New hint generators — ChainedDecorator, Optional, MultipleRejection, ValueMismatch

**Files:**
- Create: `src/Tokenizer/Diagnostics/Hints/ChainedDecoratorHintGenerator.cs`
- Create: `src/Tokenizer/Diagnostics/Hints/OptionalTokenHintGenerator.cs`
- Create: `src/Tokenizer/Diagnostics/Hints/MultipleRejectionHintGenerator.cs`
- Create: `src/Tokenizer/Diagnostics/Hints/ValueMismatchHintGenerator.cs`
- Create: `tests/Tokenizer.Tests/Diagnostics/Hints/BlockedTokenHintGeneratorTests.cs`
- Create: `tests/Tokenizer.Tests/Diagnostics/Hints/ChainedDecoratorHintGeneratorTests.cs`
- Create: `tests/Tokenizer.Tests/Diagnostics/Hints/OptionalTokenHintGeneratorTests.cs`
- Create: `tests/Tokenizer.Tests/Diagnostics/Hints/MultipleRejectionHintGeneratorTests.cs`
- Create: `tests/Tokenizer.Tests/Diagnostics/Hints/ValueMismatchHintGeneratorTests.cs`
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` (update generator array in IssueFactory construction)

**Interfaces:**
- Consumes: `IHintGenerator`, `DiagnosticResult.OptionalTokenNames`, `DiagnosticResult.RawEvents`
- Produces: 4 new `IHintGenerator` implementations

- [ ] **Step 1-5: Write unit tests for all 5 generators** (BlockedToken already created in Task 5, add its test file here)

Each generator test: positive case (fires, returns hint with expected content) + negative case (wrong issue type, returns null). See detailed test code in the full plan above.

- [ ] **Step 6: Run tests to verify they fail**

- [ ] **Step 7-10: Implement each generator**

- `ChainedDecoratorHintGenerator`: fires for ValidatorRejection/TransformerFailure when prior decorator succeeded on same token. Scans RawEvents for prior success events.
- `OptionalTokenHintGenerator`: fires for PreambleNeverFound when token in `diagnostics.OptionalTokenNames`.
- `MultipleRejectionHintGenerator`: fires for ValidatorRejection/TransformerFailure when 2+ rejections for same token. Only on last event. Summarizes all values+lines.
- `ValueMismatchHintGenerator`: fires for ValueMismatch. Suggests adding end delimiter.

- [ ] **Step 11: Update `IssueFactory` generator array in `TokenDiagnosticBuilder`** — priority order: BlockedToken, ChainedDecorator, DateFormat, MultipleRejection, ValueMismatch, PreambleNearMiss, ValidatorValue, OptionalToken, RepeatingToken

- [ ] **Step 12: Build and run all tests**

- [ ] **Step 13: Commit**

```bash
git add -A && git status
git commit -m "Add ChainedDecorator, Optional, MultipleRejection, ValueMismatch hint generators"
```

---

### Task 8: Implement ValueMismatch detection

**Files:**
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`
- Modify: `tests/Tokenizer.Tests/Diagnostics/Characterisation/EdgeCaseTests.cs`

- [ ] **Step 1: Write failing integration test** — template `"Name: { Name }\nAge: { Age }"`, input `"Name: Age: 30\nAge: 25"`. Assert Name token has `ValueMismatch` issue with code `TK004` and hint containing "delimiter".

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Implement detection** — in `CollectEvents`, collect preamble texts from `PreambleMatched` events. In `ClassifyOutcomes`, for each matched token, check if `AssignedValue` contains preamble of any missed/rejected token. If so, add `ValueMismatch` issue via `IssueFactory`. Use `IndexOf(preamble, StringComparison.Ordinal) >= 0` for .NET Standard 2.0 compat.

- [ ] **Step 4: Run all tests**

- [ ] **Step 5: Commit**

```bash
git add -A && git status
git commit -m "Implement ValueMismatch detection for greedy token capture"
```

---

### Task 9: Test infrastructure + assertion upgrades

**Files:**
- Modify: `tests/Tokenizer.Tests/TokenizerTestBase.cs`
- Modify: All 11 characterisation test files

- [ ] **Step 1: Add `TokenizeWithDiagnostics` to `TokenizerTestBase`**

- [ ] **Step 2: Remove private `TokenizeWithDiagnostics` from all 11 characterisation files**

- [ ] **Step 3: Migrate ~20 verdict string assertions to `MatchedCount`/`MissedCount`**

- [ ] **Step 4: Add `Assert.Equal("TKxxx", issue.Code)` alongside every issue type assertion**

- [ ] **Step 5: Upgrade hint `NotNull` assertions to content checks** (contains format string, "case difference", rejected value, etc.)

- [ ] **Step 6: Graduate `Output.WriteLine`-only tests** — add real assertions for logged values. Fix unicode test to assert `"José"`.

- [ ] **Step 7: Add 4 missing failure mode tests** — multiple validators both failing, first transformer fails in chain, blocked+optional interleaved, repeating+transformer

- [ ] **Step 8: Build and run all tests**

- [ ] **Step 9: Commit**

```bash
git add -A && git status
git commit -m "Upgrade characterisation test assertions: structured counts, error codes, hint content, missing failure modes"
```

---

### Task 10: Integration tests for new hint generators

**Files:**
- Modify: `tests/Tokenizer.Tests/Diagnostics/Characterisation/TransformerFailureTests.cs`
- Modify: `tests/Tokenizer.Tests/Diagnostics/Characterisation/EdgeCaseTests.cs`
- Modify: `tests/Tokenizer.Tests/Diagnostics/Characterisation/ValidatorRejectionTests.cs`

- [ ] **Step 1: ChainedDecorator integration test** — `"Val: { Val : ToUpper, IsEmail }"` + `"Val: hello"`. Assert hint contains "ToUpperTransformer" and "IsEmailValidator".

- [ ] **Step 2: OptionalToken integration test** — `"Name: { Name }\nNickname: { Nickname? }"` + `"Name: Alice"`. Assert Nickname issue hint contains "optional".

- [ ] **Step 3: MultipleRejection integration test** — `"Email: { Email : IsEmail }"` + 3 bad values. Assert last rejection hint contains count "3".

- [ ] **Step 4: Verify existing BlockedToken integration test still passes**

- [ ] **Step 5: Build and run all tests**

- [ ] **Step 6: Commit**

```bash
git add -A && git status
git commit -m "Add integration tests for ChainedDecorator, Optional, MultipleRejection hint generators"
```
