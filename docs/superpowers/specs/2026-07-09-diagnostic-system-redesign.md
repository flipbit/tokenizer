# Diagnostic System Redesign

## Problem Statement

The current diagnostic system has two disconnected views (Events and Summary.Issues), produces misleading messages (reports "preamble never found" when a validator rejected the value), provides no per-token narrative, and has structural issues that make it hard to extend.

## Design Goals

1. **Token-centric**: The primary diagnostic view tells the story of each token — every consideration, every rejection, and the final outcome.
2. **Single source of truth**: No more correlating between Events and Summary.Issues.
3. **Accurate messaging**: Failure messages reflect what actually happened.
4. **Separated concerns**: Compilation diagnostics and runtime diagnostics live in separate models.
5. **Extensible**: Clean SOLID structure makes adding new issue types and hints straightforward.
6. **Exhaustive test coverage**: End-to-end characterisation suite covering all failure modes.

## Non-Goals

- Changing the tokenization engine itself.
- Changing how diagnostics are collected (the `IDiagnosticCollector` pattern is fine).
- Performance optimisation of diagnostics (opt-in feature, correctness trumps speed).

## Public API Design

### DiagnosticResult (redesigned)

```csharp
public sealed class DiagnosticResult
{
    /// Primary API — per-token diagnostic story.
    public IReadOnlyList<TokenDiagnostic> Tokens { get; }

    /// High-level verdict ("Matched 3 of 5 tokens (2 missed)").
    public string Verdict { get; }

    /// Raw event trace for power users and engine debugging.
    public IReadOnlyList<DiagnosticEvent> RawEvents { get; }

    /// Rendered alignment view (token-centric, grouped by token).
    public string RenderAlignment();

    /// Rendered processing-order view (chronological engine walk-through).
    public string RenderProcessingOrder();

    /// The input text that was tokenized.
    internal string? InputContent { get; }
}
```

**Removed from current API:**
- `Summary` (replaced by `Verdict` + `Tokens[].Issues`)
- `Failures` (replaced by `Tokens.Where(t => t.Outcome != Matched)`)
- `ForToken(name)` (replaced by `Tokens.First(t => t.TokenName == name)`)
- `FirstFailure` (replaced by `Tokens.FirstOrDefault(t => t.Outcome != Matched)`)
- `Events` (renamed to `RawEvents`)

### TokenDiagnostic (new)

```csharp
public sealed class TokenDiagnostic
{
    /// Token name from the template.
    public string TokenName { get; }

    /// Unique token ID within the template.
    public int TokenId { get; }

    /// Final outcome of this token.
    public TokenOutcome Outcome { get; }

    /// Every time this token was considered during tokenization.
    public IReadOnlyList<TokenAttempt> Attempts { get; }

    /// The final assigned value, if Outcome is Matched.
    public string? AssignedValue { get; }

    /// Where in the input the token was matched, if Outcome is Matched.
    public FileLocation? AssignedLocation { get; }

    /// Issues identified for this token (with adaptive hints).
    public IReadOnlyList<DiagnosticIssue> Issues { get; }
}
```

### TokenOutcome (new)

```csharp
public enum TokenOutcome
{
    /// Token was successfully matched and assigned a value.
    Matched,

    /// Token's preamble was found but all values were rejected
    /// by validators or transformers.
    Rejected,

    /// Token's preamble was never found in the input.
    NeverFound,

    /// Token was not searched for because a prior required token
    /// failed to match. Defined in Phase 4 but not populated until Phase 6.
    Blocked,
}
```

### TokenAttempt (new)

```csharp
public sealed class TokenAttempt
{
    /// Position in the input where this attempt occurred.
    public FileLocation Location { get; }

    /// The value that was considered.
    public string? Value { get; }

    /// What happened with this attempt.
    public AttemptOutcome Outcome { get; }

    /// The decorator that rejected/failed, if applicable.
    public string? DecoratorName { get; }

    /// Human-readable explanation of why this attempt failed.
    public string? Reason { get; }
}
```

### AttemptOutcome (new)

```csharp
public enum AttemptOutcome
{
    /// Value was accepted and assigned to the token.
    Assigned,

    /// A validator rejected the value.
    ValidatorRejected,

    /// A transformer failed to convert the value.
    TransformerFailed,

    /// The engine backtracked past this match.
    Backtracked,
}
```

### CompilationDiagnostics (new)

```csharp
public sealed class CompilationDiagnostics
{
    /// Raw compilation events (TokenCreated, DecoratorApplied, etc.).
    public IReadOnlyList<DiagnosticEvent> Events { get; }
}
```

Exposed on the compiled template, not on `TokenizeResult`.

### DiagnosticIssue (unchanged shape, new context)

```csharp
public sealed class DiagnosticIssue
{
    public DiagnosticIssueType Type { get; }
    public string? TokenName { get; }
    public string Description { get; }
    public FileLocation? Location { get; }
    public string? Hint { get; }
}
```

Now lives inside `TokenDiagnostic.Issues` instead of `DiagnosticSummary.Issues`.

### DiagnosticSummary (removed)

Replaced by `DiagnosticResult.Verdict` (string) and `TokenDiagnostic.Issues` (per-token).

## Implementation Phases

### Phase 0: Characterisation Test Suite

Exhaustive end-to-end test fixtures that tokenize real templates against real input and assert on diagnostic output. Documents current behaviour. Tests are updated as each phase changes expected output.

**61 test cases split across 10 fixture files in `tests/Tokenizer.Tests/Diagnostics/Characterisation/`:**

| File | Tests | Category |
|------|-------|----------|
| `PreambleMatchingTests.cs` | 12 | Preamble found/not found/near-miss scenarios |
| `ValidatorRejectionTests.cs` | 10 | Validator accepts/rejects, misleading message cases |
| `TransformerFailureTests.cs` | 6 | Transformer pass/fail, chained decorators |
| `RepeatingTokenTests.cs` | 5 | Repeating token match/cut-short/disabled |
| `HintTests.cs` | 3 | Required hint present/missing/case-mismatch |
| `FrontMatterTests.cs` | 2 | Front matter token matched/failed |
| `MultiTokenInteractionTests.cs` | 5 | Cascading failures, backtracking, ordering |
| `EdgeCaseTests.cs` | 9 | Empty/whitespace input, unicode, long values, optional tokens |
| `AttemptCountingTests.cs` | 3 | Token consideration/rejection history |
| `DiagnosticOutputFormatTests.cs` | 6 | RenderAlignment output, verdict strings |

See individual test case listings in `docs/superpowers/specs/diagnostic-redesign/scenarios/`.

### Phase 1: Separate Compilation from Runtime Diagnostics

- New `CompilationDiagnostics` class with `Events` list.
- New `CompilationDiagnosticCollector` (or split existing collector by event type).
- Compilation event types (`TokenCreated`, `DecoratorApplied`, `OptionApplied`, `ConcatenationApplied`, `TagAdded`, `CompilationCompleted`, `RepeatingTokenLinked`) recorded to compilation collector only.
- Template exposes `CompilationDiagnostics` property.
- `DiagnosticResult` no longer contains compilation events.
- Runtime `DiagnosticEventType` enum shrinks (compilation members removed or moved).

### Phase 2: Fix Misleading "Preamble Never Found"

- `AlignmentRenderer` applies `tokensWithFailures` logic from `DiagnosticSummaryBuilder`.
- Tokens whose preamble was found but whose value was rejected by validators/transformers render as "validator rejected value" or "transformer failed", not "preamble never found".
- Update Phase 0 tests: cases #18, #26, #58 change expected values.

### Phase 3: SOLID Refactor of DiagnosticSummaryBuilder

- Extract issue classification into per-type builders implementing a common interface.
- Eliminate the "create partial issue then clone with hint" anti-pattern — build each issue once.
- `AlignmentRenderer` consumes `DiagnosticSummary.Issues` instead of re-classifying events.
- No public API change. All Phase 0 tests continue to pass with identical output.

### Phase 4: Token-Centric Diagnostic Model

- New types: `TokenDiagnostic`, `TokenAttempt`, `TokenOutcome`, `AttemptOutcome`.
- `DiagnosticResult` redesigned per the API section above.
- New builder constructs `TokenDiagnostic` list from raw events.
- Issues are attached per-token.
- `DiagnosticSummary` removed. `Verdict` moves to `DiagnosticResult`.
- Old API members (`Summary`, `Failures`, `ForToken()`, `FirstFailure`, `Events`) removed.
- `RawEvents` retains the flat trace.
- Phase 0 tests rewritten to assert against new API shape.

### Phase 5: Update Renderers

- `AlignmentRenderer` rewritten to consume `TokenDiagnostic` list.
- Output grouped by token with attempt history.
- New `ProcessingOrderRenderer` added — chronological walk-through of engine decisions using `RawEvents`.
- `DiagnosticResult.RenderAlignment()` and `DiagnosticResult.RenderProcessingOrder()` delegate to respective renderers.
- Phase 0 rendering tests (#56-61) updated for new output format.

### Phase 6: Downstream Impact / Causality Chains

- During tokenization, track which tokens were never searched for because a prior required token was not yet matched.
- `TokenOutcome.Blocked` added with `BlockedBy` property on `TokenDiagnostic`.
- Root-cause tokens highlighted in rendered output.
- Blocked tokens' issues include a hint: "This token was not searched for because '{BlockingToken}' was not matched. Fix '{BlockingToken}' first."
- New Phase 0 tests added for blocked-token scenarios.

### Phase 7: Structured Error Codes

- Each `DiagnosticIssueType` gets a stable code: `TK001` (PreambleNeverFound), `TK002` (ValidatorRejection), `TK003` (TransformerFailure), `TK004` (ValueMismatch), `TK005` (RepeatingTokenCutShort), `TK006` (UnmatchedInputSection), `TK007` (HintMissing), `TK008` (Blocked).
- `DiagnosticIssue` gains `Code` property.
- Codes appear in rendered output.
- Phase 0 tests updated to assert codes.

## File Impact Summary

**New files:**
- `tests/Tokenizer.Tests/Diagnostics/DiagnosticCharacterisationTests.cs`
- `src/Tokenizer/Diagnostics/CompilationDiagnostics.cs`
- `src/Tokenizer/Diagnostics/TokenDiagnostic.cs`
- `src/Tokenizer/Diagnostics/TokenAttempt.cs`
- `src/Tokenizer/Diagnostics/TokenOutcome.cs`
- `src/Tokenizer/Diagnostics/AttemptOutcome.cs`
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`
- `src/Tokenizer/Diagnostics/ProcessingOrderRenderer.cs`
- Per-type issue builders (Phase 3, exact files TBD)

**Modified files:**
- `src/Tokenizer/Diagnostics/DiagnosticResult.cs` (redesigned)
- `src/Tokenizer/Diagnostics/DiagnosticCollector.cs` (split compilation/runtime)
- `src/Tokenizer/Diagnostics/AlignmentRenderer.cs` (rewritten)
- `src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs` (refactored, eventually replaced)
- `src/Tokenizer/Diagnostics/DiagnosticEventType.cs` (compilation events separated)
- `src/Tokenizer/Diagnostics/DiagnosticIssue.cs` (gains Code property in Phase 7)
- `src/Tokenizer/TokenizeResult.cs` (no change to Diagnostics property)
- Template class (gains CompilationDiagnostics property)
- Existing diagnostic test files (updated to match new API)

**Removed files:**
- `src/Tokenizer/Diagnostics/DiagnosticSummary.cs` (replaced by Verdict + per-token Issues)
