# Code Review Report

## Review Metadata

- **Branch:** diagnostics
- **Base:** main
- **Work Item:** N/A
- **Change set:** branch diff
- **Files changed:** 88
- **Lines:** +9613 / -748
- **Design docs:** `docs/superpowers/specs/2026-07-09-diagnostic-system-redesign.md`, `docs/superpowers/specs/2026-07-10-diagnostic-review-fixes-design.md`
- **Reviewed:** 2026-07-10

---

## Merge Recommendation

**Verdict:** APPROVE WITH CONDITIONS

**Rationale:** No critical blockers, but 9 High-priority issues across code quality, spec compliance, observability, and test coverage should be addressed before merge.

---

## Summary of Changes

This branch implements a comprehensive redesign of the diagnostic system across 7 phases. It replaces the flat `DiagnosticSummary`/`Events` model with a token-centric `TokenDiagnostic` narrative (tokens, attempts, outcomes, issues), separates compilation from runtime diagnostics, adds structured error codes (TK001-TK008), introduces 5 new hint generators, fixes misleading "preamble never found" messages, and adds ~60 characterisation tests. The `UPGRADING.md` migration guide documents the v2-to-v3 API changes.

---

## Strengths & Weaknesses

### Strengths

- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:70-183` — Single-pass event collection replaces multiple LINQ scans, improving both clarity and performance
- `src/Tokenizer/Diagnostics/TokenDiagnostic.cs` — Per-token diagnostic narrative with attempts and issues is the right abstraction for the domain
- `src/Tokenizer/Diagnostics/CompilationDiagnosticCollector.cs` / `RuntimeDiagnosticCollector.cs` — Clean separation of compilation vs runtime concerns eliminates the awkward dual-mode collector
- `src/Tokenizer/Diagnostics/IssueCodeMap.cs` — Stable error codes with exhaustive switch enable programmatic filtering and documentation linking
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:279-320` — Blocked token causality chain detection is a genuinely useful diagnostic insight
- `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs:14` — Singleton null-object pattern ensures zero overhead when diagnostics are disabled
- `tests/Tokenizer.Tests/Diagnostics/IssueCodeMapTests.cs:55-65` — Enum-driven uniqueness test automatically catches future code collisions

### Weaknesses

- `src/Tokenizer/Diagnostics/DiagnosticResult.cs:118-129` — Lazy init pattern without synchronisation risks torn reads on a public type
- `src/Tokenizer/Tokenizer.cs:357-367` — Diagnostic logging loop runs unconditionally even when debug logging is off
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:304-318` — Blocked reclassification silently discards original per-token issues
- `src/Tokenizer/Diagnostics/Hints/ValueMismatchHintGenerator.cs:16` — Hint text missing dynamic preamble/token details per spec

---

## Security Review

**Security Posture:** SECURE

No security vulnerabilities found. This is an in-process text tokenization library with no I/O boundaries, no user-controlled input reaching dangerous sinks, no reflection, and no deserialization. Internal constructors and `internal init` setters prevent external mutation. Ordinal string comparisons used throughout.

---

## Multi-Tenant Isolation Review

**Isolation Verdict:** N/A

N/A — system is not multi-tenant. No tenant IDs, middleware, RLS policies, or tenant-scoped state. Library is a NuGet package for text tokenization.

---

## Performance Impact

**Volume Assumptions:** Library processing up to thousands of templates/sec, 5-50 tokens per template, 20-200 events per tokenization. Diagnostics are opt-in. [UNCONFIRMED]

**Performance Impact:** LOW IMPACT

Hint generators (MultipleRejection, ChainedDecorator, RepeatingToken) each perform full scans of `RawEvents` per invocation, resulting in O(F*E) work. `PreambleNearMissHintGenerator` allocates full line arrays per missed token. `IssueFactory` creates throwaway `DiagnosticIssue` objects. At stated volumes (20-200 events), none are critical, but they compound under high-throughput diagnostic-enabled scenarios.

---

## Database Review

**Database Verdict:** N/A

**Target Database(s):** N/A

N/A — no database changes detected.

---

## Observability Review

**Observability Verdict:** PARTIALLY OBSERVABLE

Compilation exceptions correctly attach diagnostics via `ex.Data["CompilationDiagnostics"]`, but runtime exceptions do not. All diagnostic detail is logged at Debug level only. The catch-all in `TemplateCompiler` drops compilation diagnostics on unexpected exceptions.

---

## Hiring Recommendation

**Recommended Level:** Senior

**Justification:**

- `src/Tokenizer/Diagnostics/TokenDiagnostic.cs` — Token-centric model with `Attempts` and `Issues` demonstrates strong domain modelling
- `src/Tokenizer/Diagnostics/IssueFactory.cs` + `IHintGenerator` chain — Solid SOLID application with single-responsibility generators and open-closed extensibility
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:249-276` — ValueMismatch greedy-capture detection shows non-obvious diagnostic insight
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:279-320` — Causality chain detection for blocked tokens demonstrates domain understanding
- 60+ characterisation tests across 11 fixtures — Comprehensive test coverage at multiple levels

**Gaps preventing Staff:**
- `DiagnosticResult.cs:118-129` — Lazy init without `Lazy<T>` or atomic assignment risks torn reads
- `TokenDiagnosticBuilder.cs` — Static singleton `IssueFactory` prevents injection for testing/configuration
- `ChainedDecoratorHintGenerator.cs:32` — `ReferenceEquals` loop terminator is fragile against synthetic events
- Several characterisation tests remain at `Assert.NotNull` without structural assertions

---

## Delta to Staff-Level

**D1:** `src/Tokenizer/Diagnostics/DiagnosticResult.cs:118` — Lazy init sets 5 fields sequentially without synchronisation. Staff-level: bundle into a single `Lazy<BuiltResult>` record for atomic visibility. Effort: S

**D2:** `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:30` — Static `IssueFactory` singleton prevents injecting different hint generator configurations. Staff-level: make factory injectable through the builder for testability. Effort: S

**D3:** `src/Tokenizer/Diagnostics/Hints/ChainedDecoratorHintGenerator.cs:32` — `ReferenceEquals` for loop termination is fragile. Staff-level: use event index or structural comparison. Effort: S

**D4:** Various characterisation tests — Some tests log without asserting. Staff-level: commit stronger assertions even in characterisation tests. Effort: M

---

## Issues

| ID | Severity | Reviewer | File:Line | Issue | Fix |
|----|----------|----------|-----------|-------|-----|
| H1 | H | Code Quality | `AlignmentRenderer.cs:117` | "Failures" count includes all attempts (including Backtracked), not just failures; body renders Backtracked as "failed on" | Filter attempts to `ValidatorRejected`/`TransformerFailed` only in both count and body |
| H2 | H | Code Quality | `Tokenizer.cs:357-367` | Diagnostic logging loop runs unconditionally, forcing `EnsureBuilt()` even when debug logging is off | Move `foreach` inside `if (_log.IsEnabled(LogLevel.Debug))` guard |
| H3 | H | Code Quality | `TokenDiagnosticBuilder.cs:304-318` | `ApplyBlockedAnnotations` replaces original issues list with single Blocked issue, discarding near-miss hints | Merge original issues with new Blocked issue instead of replacing |
| H4 | H | Spec Compliance | `DiagnosticEventType.cs` | Compilation event types still in shared enum; spec requires "runtime enum shrinks" | Move compilation event types to separate enum or mark with attribute |
| H5 | H | Spec Compliance | `TokenDiagnosticBuilder.cs:149-151` | Preamble texts collected from `TokenMissed` events, not `PreambleMatched` as spec requires | Collect from `PreambleMatched` events to cover matched tokens too |
| H6 | H | Observability | `Tokenizer.cs:361` | All diagnostic issues logged exclusively at Debug level; invisible in production | Log issues at Warning with error codes: `_log.LogWarning("Token '{TokenName}' [{IssueCode}]: {Description}", ...)` |
| H7 | H | Observability | `TemplateCompiler.cs:73-78` | Catch-all exception wrapper doesn't attach `CompilationDiagnostics` despite comment saying it does | Add `wrappedException.Data["CompilationDiagnostics"] = collector.GetCompilationResult()` |
| H8 | H | Observability | `Tokenizer.cs:316-327` | Runtime exceptions don't attach diagnostic results (compilation path does) | Add `ex.Data["Diagnostics"] = collector.GetResult()` to both catch blocks |
| H9 | H | Test Coverage | `TokenDiagnosticBuilder.cs:131-140` | `BacktrackStarted` event path producing `AttemptOutcome.Backtracked` has no test | Add unit test recording `BacktrackStarted` and asserting outcome |
| M1 | M | Code Quality | `TokenDiagnosticBuilder.cs:335` | `issue.TokenName!` null-forgiving operator hides latent null dereference | Add guard: `if (issue.TokenName == null) return;` or throw `ArgumentException` |
| M2 | M | Code Quality | `DiagnosticResult.cs:118-129` | `EnsureBuilt()` check-then-set pattern without synchronisation; fields written sequentially | Use `Lazy<T>` or single atomic record assignment |
| M3 | M | Code Quality | `IssueFactory.cs:80-87` | `GenerateHint` creates throwaway `DiagnosticIssue` just to pass to generators | Refactor `IHintGenerator.TryGenerateHint` to accept fields directly |
| M4 | M | Code Quality | `ChainedDecoratorHintGenerator.cs:32` | `ReferenceEquals(evt, sourceEvent)` fragile against synthetic events | Use event index or document why type guard makes this safe |
| M5 | M | Code Quality | `MultipleRejectionHintGenerator.cs:35-37` | "Only fire on last rejection" uses string equality, not reference identity | Use `ReferenceEquals(last, sourceEvent)` to check event identity |
| M6 | M | Spec Compliance | `ValueMismatchHintGenerator.cs:16` | Hint text missing dynamic preamble/token name prefix per spec | Add `$"Value contains '{preamble}' which is the preamble of token '{otherToken}'. "` prefix |
| M7 | M | Spec Compliance | N/A | "First transformer in chain fails" test (H7 in spec) not implemented | Add test in `TransformerFailureTests`: `ToDateTime, ToUpper` on `"bad"` |
| M8 | M | Observability | `IssueCodeMap.cs:18` | `ArgumentOutOfRangeException` on unknown enum values crashes `issue.Code` property | Return fallback `$"TK???({(int)type})"` instead of throwing |
| M9 | M | Performance | `MultipleRejectionHintGenerator.cs:58-75` | `CollectRejections` scans full `RawEvents` per rejection event: O(N*E) | Pre-index rejection events by token name during collection |
| M10 | M | Performance | `ChainedDecoratorHintGenerator.cs:29-44` | Full `RawEvents` scan per decorator failure: O(F*E) | Pre-index decorator success events by token name |
| M11 | M | Test Coverage | `ChainedDecoratorHintGeneratorTests.cs:10-39` | Tests never exercise `ReferenceEquals` early-exit; passes by scanning all events | Use actual trace event as `sourceEvent`, or add test with post-failure events |
| M12 | M | Test Coverage | `IssueFactory.cs:42-55` | `CreateValueMismatch` has no unit test | Add unit test similar to `CreateBlocked` test |
| M13 | M | Test Coverage | `AlignmentRenderer.cs:95-109` | Blocked Tokens rendering section has no test | Add test with blocked tokens asserting `⊘` marker and `BlockedBy` text |
| M14 | M | Test Coverage | `NullDiagnosticCollector.cs:22` | `IsEnabled` property never tested on any collector | Add `Assert.False/True` for each collector's `IsEnabled` |
| M15 | M | Test Coverage | `TokenDiagnosticBuilder.cs:171-173` | Per-token `HintMissing` path only covered by characterisation tests | Add unit test with named `HintMissing` event asserting per-token attachment |
| L1 | L | Code Quality | `IssueCodeMap.cs:15-17` | TK006 gap undocumented; codes jump TK005 to TK007 | Add comment: `// TK006: reserved (was UnmatchedInputSection, removed in v3)` |
| L2 | L | Code Quality | `IDiagnosticCollector.cs:22` | Doc comment references deleted "DiagnosticCollector" class name | Update to `RuntimeDiagnosticCollector` |
| L3 | L | Code Quality | `IssueCodeMap.cs:9` | `GetCode` is `public` on `internal static` class; misleading visibility | Change to `internal static string GetCode(...)` |
| L4 | L | Code Quality | `OptionalTokenHintGenerator.cs:21` | Accesses `internal` property `trace.OptionalTokenNames` creating coupling | Accept as intentional (both internal) but add comment documenting coupling |
| L5 | L | Spec Compliance | `EdgeCaseTests.cs:149-168` | `NewlineTerminated` test not graduated per H5; logs without asserting | Add assertion on `NewlineTerminatedTokenProcessed` event count |
| L6 | L | Spec Compliance | `ValidatorRejectionTests.cs` | "Multiple validators both failing" test documents short-circuit, not "both reject" per spec | Rename test or add commentary explaining engine short-circuit behavior |
| L7 | L | Performance | `RepeatingTokenHintGenerator.cs:18-32` | LINQ `LastOrDefault` allocates enumerator per call | Replace with reverse `for` loop |
| L8 | L | Performance | `PreambleNearMissHintGenerator.cs:41` | `String.Split('\n')` allocates full line array per missed token | Cache split lines on `DiagnosticResult` |
| L9 | L | Performance | `PreambleNearMissHintGenerator.cs:63-66` | `Regex.Replace` called per line per missed token: O(M*L) | Cache normalized lines alongside split lines |
| L10 | L | Performance | `TokenDiagnosticBuilder.cs:249-276` | `ApplyValueMismatchIssues` is O(A*M*V) string search | Acceptable at stated volumes; short-circuit on value length |
| L11 | L | Performance | `AlignmentRenderer.cs:117` | LINQ `Sum` allocates delegate+enumerator, inconsistent with anti-LINQ comments | Replace with `foreach` loop |
| L12 | L | Test Coverage | `DiagnosticResult.cs:104` | `RenderAlignment` caching not tested (but `RenderProcessingOrder` caching is) | Add `Assert.Same(first, second)` test |
| L13 | L | Test Coverage | `NullDiagnosticCollector.cs:37` | `GetCompilationResult()` not tested (other collectors are) | Add `Assert.Null(NullDiagnosticCollector.Instance.GetCompilationResult())` |
| D1 | D | Hiring | `DiagnosticResult.cs:118` | Lazy init sets 5 fields sequentially. Staff: `Lazy<BuiltResult>` record | Effort: S |
| D2 | D | Hiring | `TokenDiagnosticBuilder.cs:30` | Static `IssueFactory` singleton not injectable. Staff: make configurable | Effort: S |
| D3 | D | Hiring | `ChainedDecoratorHintGenerator.cs:32` | `ReferenceEquals` fragile. Staff: structural comparison or index | Effort: S |
| D4 | D | Hiring | Various characterisation tests | Some tests log without asserting. Staff: structural assertions always | Effort: M |

---

## Recommended Fixes

- H1 - Filter AlignmentRenderer "Failures" to actual failure outcomes only
- H2 - Move diagnostic logging loop inside `IsEnabled(LogLevel.Debug)` guard
- H3 - Merge original issues with Blocked issue instead of replacing
- H4 - Split compilation event types from runtime `DiagnosticEventType` enum
- H5 - Collect preamble texts from `PreambleMatched` events per spec
- H6 - Log diagnostic issues at Warning level with error codes
- H7 - Attach `CompilationDiagnostics` in catch-all exception wrapper
- H8 - Attach `DiagnosticResult` to runtime exceptions
- H9 - Add unit test for `BacktrackStarted` event path
- M1 - Guard against null `TokenName` in `AddIssue`
- M5 - Fix `MultipleRejectionHintGenerator` deduplication to use reference equality
- M6 - Add dynamic preamble/token details to `ValueMismatchHintGenerator` hint text
- M7 - Add "first transformer in chain fails" test

---

## Reviewer Competition

| Reviewer | Stars |
|----------|-------|
| Code Quality | 12 |
| Spec Compliance | 6 |
| Test Coverage | 8 |
| Security | 0 |
| Multi-Tenant Isolation | 0 |
| Performance | 7 |
| Database | 0 |
| Observability | 4 |
| Hiring Recommendation | 2 |

**Winner: Code Quality** with 12 stars, leading by 4 over second place (Test Coverage, 8 stars).
