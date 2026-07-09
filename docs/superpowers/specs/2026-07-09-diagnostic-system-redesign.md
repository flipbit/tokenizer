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

Write an exhaustive end-to-end test fixture (`DiagnosticCharacterisationTests.cs`) that tokenizes real templates against real input and asserts on diagnostic output. Documents current behaviour. Tests are updated as each phase changes expected output.

**61 test cases across these categories:**

**Preamble Matching (12 tests)**
1. Simple match — happy path
2. All tokens match
3. Preamble not found at all
4. Preamble case mismatch — expects near-miss hint
5. Preamble whitespace mismatch
6. Preamble partial match (e.g. "Username:" vs "User:")
7. Out-of-order tokens (test with OutOfOrder both on and off)
8. Multiple tokens sharing same preamble prefix
9. Preamble appears multiple times in input
10. Empty preamble (token at start of input)
11. Preamble with special characters
12. Preamble found but value is empty

**Validator Rejections (10 tests)**
13. IsEmail rejects invalid value
14. IsEmail accepts valid value
15. IsNumeric rejects text
16. IsPhoneNumber rejects gibberish
17. IsDomainName rejects invalid
18. Validator rejects but preamble was found — must NOT say "preamble never found"
19. Multiple validators on one token — first passes, second rejects
20. Same token (repeating) rejected at some occurrences, accepted at others
21. Validator rejects every occurrence — token ends up missed
22. Validator rejects with null/empty value

**Transformer Failures (6 tests)**
23. ToDateTime with wrong format
24. ToDateTime with correct format
25. ToDateTime — hint suggests matching format
26. Transformer fails but preamble was found — must NOT say preamble not found
27. Chained transformer + validator — transformer succeeds, validator fails
28. Chained transformers — first succeeds, second fails

**Repeating Tokens (5 tests)**
29. Repeating token — all match
30. Repeating token cut short by validator
31. Repeating token cut short by line gap
32. Repeating token — zero matches (preamble never found)
33. Repeating token — one match then disabled

**Hints (3 tests)**
34. Required hint present
35. Required hint missing
36. Hint case mismatch

**Front Matter (2 tests)**
37. Front matter token matched
38. Front matter token failed

**Multi-Token Interaction (5 tests)**
39. First token fails, second would match
40. First token's validator fails, second token matches
41. All tokens fail
42. Middle token fails, others match
43. Token matched after backtracking

**Edge Cases (9 tests)**
44. Empty input
45. Whitespace-only input
46. Single character input
47. Very long value
48. Value contains preamble text of another token
49. Unicode in preamble and value
50. Newline-terminated token
51. Single-use token fails and is removed
52. Optional token not present — no issue raised

**Attempt Counting (3 tests)**
53. Token considered 3 times, rejected twice, matched once
54. Token considered multiple times, never matched
55. Token with multiple candidates at same position

**Diagnostic Output Format (6 tests)**
56. RenderAlignment for clean match
57. RenderAlignment for mixed results
58. RenderAlignment for validator rejection — says "validator rejected"
59. Verdict string for full match
60. Verdict string for partial match
61. Verdict string for zero matches

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
