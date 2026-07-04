# Engine Cleanup and Async Support

## Background

The v3 code review (2026-07-03) identified three areas of deferred work in the tokenization engine, plus an architectural gap: the library has no real async support.

This prompt covers all four concerns as a single coherent body of work, since they share the same codebase area and benefit from being designed together.

## Deferred Review Issues

### D2: Verbose logging duplicates diagnostics events

**File:** `src/Tokenizer/Tokenization/TokenizationEngine.cs`

The engine has two parallel tracing systems running simultaneously:
1. `log.LogTrace` / `log.LogDebug` calls — Microsoft.Extensions.Logging
2. `collector.Record(...)` calls — the DiagnosticCollector event system

Both record the same events (tokenization started, character consumed, token matched, backtrack, etc.). The diagnostics system was designed as the replacement for verbose logging, but the logging was never removed.

**Goal:** Use diagnostic events as the primary detailed trace. Reserve `ILogger` for operation-level summaries (start, complete, warnings, errors). Strip per-character and per-token `LogTrace`/`LogDebug` calls that duplicate `collector.Record` events.

**Constraints:**
- The `IsEnabled` guards on remaining log calls must be preserved
- Operation-level logging (LogInformation, LogWarning, LogError) stays
- The diagnostics system must remain the opt-in detailed trace (enabled via `TokenizerOptions.EnableDiagnostics`)

### D4: ITokenizationEngine interface too wide

**File:** `src/Tokenizer/Tokenization/ITokenizationEngine.cs`

The `internal interface ITokenizationEngine` exposes 5 methods:
- `ProcessTokenization` — the orchestration entry point (should be on the interface)
- `TryAssignCandidateTokens` — implementation detail
- `ProcessFrontMatterTokens` — implementation detail
- `ProcessRepeatedTokens` — implementation detail
- `ProcessNewlineTerminatedTokens` — implementation detail

The last four are only called by the engine itself or by internal tests that reach into implementation details.

**Goal:** Narrow the interface to `ProcessTokenization` only. Make the other methods private or protected on `TokenizationEngine`. Update internal tests to test through the public orchestration method instead of reaching into implementation details.

**Note:** The test file `TokenizationEngineInternalTests.cs` calls these methods directly. Those tests need to be rewritten to test the same behaviors through `ProcessTokenization` (or via the full `Tokenizer.Tokenize` pipeline).

## New Work: Real Async Support

### Current State

The library is fully synchronous. The v3 branch had fake-async `TokenizeAsync` wrappers on `TemplateLexer` (sync code wrapped in `IAsyncEnumerable` with `Task.Yield()`), but these were removed in the review fixes as they provided no real async benefit.

The core I/O path is:
1. `TemplateLexer` reads from `TextReader` via `LookaheadReader` (sync `Read()`/`Peek()`)
2. `TokenizationEngine` reads from `TokenEnumerator` which wraps `TextReader` (sync `Read()`)

Both paths block the calling thread on I/O. For callers feeding a `NetworkStream` or other I/O-bound source, this means a thread is blocked for the duration of tokenization.

### Goal

Add real async tokenization support so that:
1. `Tokenizer.TokenizeAsync(template, TextReader, CancellationToken)` is truly non-blocking
2. The async path uses `ReadAsync` on the underlying `TextReader`/`StreamReader`
3. `CancellationToken` is honored throughout
4. The sync path remains unchanged (no performance regression)

### Design Considerations

**Approach A: Dual implementation**
- Maintain separate sync and async code paths
- Pros: optimal performance for both paths
- Cons: significant code duplication (the exact problem we just fixed in D1)

**Approach B: Async-first with sync wrapper**
- Implement async as the primary path, sync calls `.GetAwaiter().GetResult()`
- Pros: single implementation
- Cons: sync-over-async is an anti-pattern, can deadlock in certain contexts

**Approach C: Template method with sync/async specialization**
- Abstract the I/O operations behind an interface/strategy
- The tokenization algorithm stays in one place
- I/O calls are dispatched to sync or async implementations
- Pros: single algorithm, no duplication, no sync-over-async
- Cons: more abstraction, potential allocation overhead from the dispatch layer

**Recommendation:** Approach C deserves serious exploration. The key I/O operations to abstract are:
- `TextReader.Read()` / `TextReader.ReadAsync()`
- `TextReader.Peek()` (no async equivalent — needs buffering)

The `LookaheadReader` and `TokenEnumerator` are natural places for this abstraction since they already buffer characters.

### Scope

The async work touches:
- `TokenEnumerator` — needs async character reading
- `LookaheadReader` — needs async buffering
- `TokenizationEngine` — needs async `ProcessTokenization`
- `Tokenizer` — needs public `TokenizeAsync` methods
- `TemplateLexer` — template compilation could also benefit from async (lower priority since templates are typically small)

### What NOT to do
- Don't add fake-async wrappers (we just removed those)
- Don't break the sync API or regress its performance
- Don't make async the only path — sync must remain first-class

## Suggested Approach

1. **Start with D4** (narrow the interface) — this simplifies the engine's public contract before adding async to it
2. **Then D2** (strip verbose logging) — reduces noise in the engine code, making the async work cleaner
3. **Then design the async abstraction** — propose the I/O dispatch layer, get alignment
4. **Then implement async** — bottom-up: TokenEnumerator → Engine → Tokenizer

## Entry Points

- Engine: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- Interface: `src/Tokenizer/Tokenization/ITokenizationEngine.cs`
- Enumerator: `src/Tokenizer/Enumerators/TokenEnumerator.cs`
- LookaheadReader: `src/Tokenizer/Enumerators/LookaheadReader.cs`
- Tokenizer: `src/Tokenizer/Tokenizer.cs`
- Internal tests: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs`
