# Tokenizer.cs Unification Refactoring

**Date:** 2026-07-08
**Status:** Approved
**Scope:** `Tokenizer.cs`, `IHintStrategy`, hint strategy implementations, `TokenizationSession`, `TokenEnumerator`

## Problem

`Tokenizer.cs` contains two nearly identical private methods — `TokenizeCore` (sync) and `TokenizeAsyncCore` (async). They share the same control flow (setup, hint pre-processing, session execution, hint post-processing, finalization) but diverge at three points:

1. Hint strategy choice (`ContainsHintStrategy` vs `IntegratedHintStrategy`)
2. Session execution (`session.Run()` vs `await session.RunAsync()`)
3. `rawInput` availability (full string vs null)

The async hint strategy (`IntegratedHintStrategy`) also has a correctness gap: it checks matched preambles for hint text but never inspects the actual buffer contents flowing through during tokenization, missing hints that appear in token values or other non-preamble text.

Additionally, `Tokenizer.cs` contains `ReadToEndAsync` — a bounded buffered read helper that violates SRP by mixing IO concerns into the tokenizer class.

## Design

### Approach: Unified async core with sync shim

A single `RunCoreAsync` method replaces both `TokenizeCore` and `TokenizeAsyncCore`. The sync path produces an already-completed `Task` (one state machine allocation per tokenization, accepted trade-off). A single `if` branch selects `session.Run()` vs `await session.RunAsync()`.

### 1. Hint Strategy Redesign

#### Interface

`IHintStrategy` changes to:

```csharp
internal interface IHintStrategy
{
    bool PreProcess(Template template, TokenEnumerator enumerator,
                    string? rawInput, TokenizeResult result, IDiagnosticCollector collector);

    void OnBufferFilled(char[] buffer, int count);

    bool PostProcess(TokenizeResult result);
}
```

- `OnTokenMatched(Token)` is **removed** — replaced by `OnBufferFilled(char[], int)`
- `OnBufferFilled` is called by `TokenizationSession` after each buffer refill, passing the staging buffer contents

#### `UpfrontHintStrategy` (renamed from `ContainsHintStrategy`)

- `PreProcess`: Scans full `rawInput` with `string.Contains()` — unchanged behavior
- `OnBufferFilled`: No-op
- `PostProcess`: No-op (returns false) — unchanged behavior

#### `StreamingHintStrategy` (renamed from `IntegratedHintStrategy`)

- `PreProcess`: Stores template reference, clears state, returns false — no-op for hint detection
- `OnBufferFilled`: Scans `char[] buffer` (length `count`) for hint strings. Maintains a reusable overlap `char[]` of `maxHintLength - 1` characters from the previous chunk to catch hints spanning chunk boundaries
- `PostProcess`: Evaluates which hints were found across all buffer fills, populates result

#### Performance constraints for `OnBufferFilled`

- Work directly on `char[]` + `int count`; use `Span<char>` on net8.0+ with netstandard2.0 fallback
- Overlap window is a reusable `char[]` buffer, not `StringBuilder`
- No LINQ in the hot path
- No string allocations per chunk

### 2. `TokenizationSession` Changes

- Stores `IHintStrategy?` directly as a field (in addition to passing it to `TokenMatchRouter`)
- After `FillBuffer()`/`FillBufferAsync()`, calls `_hintStrategy?.OnBufferFilled(enumerator.StagingBuffer, enumerator.LastReadCount)`
- `TokenMatchRouter` drops `OnTokenMatched` forwarding (method removed from interface)

#### `TokenEnumerator` additions

Two new read-only properties:

- `char[] StagingBuffer` — the staging buffer used during fills
- `int LastReadCount` — how many chars were read in the most recent fill

These expose existing internal state (`_stagingBuffer` and a captured `read` count from `FillBuffer`/`FillBufferAsync`).

### 3. Unified `RunCoreAsync` in `Tokenizer.cs`

```csharp
private async Task RunCoreAsync(
    TokenizeResult result, Template template, TextReader reader,
    string? rawInput, CancellationToken ct)
```

Control flow:

1. Select hint strategy: `rawInput != null` -> `UpfrontHintStrategy`, else -> `StreamingHintStrategy`
2. Build logging scope properties (`Operation` = `"Tokenize"` or `"TokenizeAsync"`)
3. Fast-fail max input length check when `rawInput` is available
4. Create `TokenizationContext`, initialize with reader
5. Create `DiagnosticCollector` with `rawInput` (null for streaming)
6. `hintStrategy.PreProcess(...)`
7. If hints not missing:
   - Create session
   - `if (rawInput != null) session.Run(context); else await session.RunAsync(context, ct).ConfigureAwait(false);`
   - `hintStrategy.PostProcess(result)`
8. `FinalizeTokenization(result, template, collector, rawInput)`

Error handling:

- `OperationCanceledException` catch — harmless on sync path (CancellationToken.None), correct for async
- `TokenizerException` catch — same logging for both paths

Sync callers: The non-async `Tokenize` overloads call `RunCoreAsync(...)`. Inside `RunCoreAsync`, the sync branch calls `session.Run(context)` — no `await` is hit, so the compiler-generated state machine completes synchronously and returns an already-completed `Task`. The caller doesn't need `.GetAwaiter().GetResult()` or `.Wait()` — it simply calls the method as a fire-and-complete invocation.

### 4. `TextReaderExtensions` — Extracted IO Logic

`ReadToEndAsync` moves to `Extensions/TextReaderExtensions.cs`:

```csharp
internal static class TextReaderExtensions
{
    public static async Task<string> ReadToEndBoundedAsync(
        this TextReader reader, int maxLength, CancellationToken ct)
    {
        // Existing StringBuilder + char[4096] buffered read with max-length check
    }
}
```

`CompileAsync` callers change to `reader.ReadToEndBoundedAsync(Options.MaxTemplateLength, ct)`.

### 5. Class Member Ordering

`Tokenizer.cs` body reorders to:

1. Fields (`_parser`, `_log`, `_tokenizationEngine`, `_resultBuilder`)
2. Properties (`Options`)
3. Constructors
4. `Compile()` methods
5. `CompileAsync()` methods and overloads
6. `Tokenize()` methods and overloads
7. `TokenizeAsync()` methods and overloads
8. Private methods (`RunCoreAsync`, `FinalizeTokenization`)

## Testing & Performance Strategy

### Benchmark baseline

Run the full benchmark suite before any code changes. Run again after implementation. No regressions acceptable.

### Test plan

1. **Existing tests pass unchanged** — behavior-preserving refactoring
2. **`StreamingHintStrategy` tests** — buffer scanning, cross-chunk boundary hints (overlap window)
3. **`TextReaderExtensions.ReadToEndBoundedAsync` tests** — isolated coverage for extracted method
4. **`UpfrontHintStrategy` tests** — existing tests renamed to match new class name, behavior unchanged
5. **No-op verification** — `UpfrontHintStrategy.OnBufferFilled` is a no-op; `StreamingHintStrategy.PreProcess` returns false

### Performance considerations

- `StreamingHintStrategy.OnBufferFilled`: no string allocations, no LINQ, work on `char[]`/`Span<char>`
- Overlap window: reusable `char[]` buffer
- The `async Task` state machine for sync callers is the one accepted allocation
- Be mindful of string allocations and CPU-expensive operations in the hot path throughout

## Files Changed

| File | Change |
|------|--------|
| `src/Tokenizer/Tokenizer.cs` | Unify `TokenizeCore`/`TokenizeAsyncCore` into `RunCoreAsync`, reorder members, remove `ReadToEndAsync` |
| `src/Tokenizer/Tokenization/IHintStrategy.cs` | Replace `OnTokenMatched` with `OnBufferFilled` |
| `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs` | Rename to `UpfrontHintStrategy`, add no-op `OnBufferFilled` |
| `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs` | Rename to `StreamingHintStrategy`, replace preamble tracking with buffer scanning |
| `src/Tokenizer/Tokenization/TokenizationSession.cs` | Store `IHintStrategy?`, call `OnBufferFilled` after buffer refills |
| `src/Tokenizer/Enumerators/TokenEnumerator.cs` (src/) | Expose `StagingBuffer`, `LastReadCount` properties |
| `src/Tokenizer/Tokenization/TokenMatchRouter.cs` | Remove `OnTokenMatched` forwarding |
| `src/Tokenizer/Extensions/TextReaderExtensions.cs` | New file — extracted `ReadToEndBoundedAsync` |
| Test files | New strategy tests, renamed existing, benchmark validation |
