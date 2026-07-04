# Engine Cleanup and Async Support — Design Spec

## Overview

Four phases of work on the tokenization engine: narrow the internal interface, strip duplicate logging, refactor TokenMatcher, and add real async support with a ring-buffered streaming architecture.

All phases target the same codebase area and are designed together for coherence.

## Phase 1: Narrow ITokenizationEngine Interface (D4)

### Problem

The internal interface `ITokenizationEngine` exposes 5 methods. Only `ProcessTokenization` is the orchestration entry point. The other four (`TryAssignCandidateTokens`, `ProcessFrontMatterTokens`, `ProcessRepeatedTokens`, `ProcessNewlineTerminatedTokens`) are implementation details called only by the engine itself or by tests that reach into internals.

### Changes

**`ITokenizationEngine`** — reduce to single method:

```csharp
internal interface ITokenizationEngine
{
    void ProcessTokenization(
        Template template,
        object? targetObject,
        ITokenizationContext context,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);
}
```

Note: `inputLength` parameter removed. The engine derives its safety limit from `TokenEnumerator.CharactersConsumed` (see Phase 4).

**`TokenizationEngine`** — make `TryAssignCandidateTokens`, `ProcessFrontMatterTokens`, `ProcessRepeatedTokens`, `ProcessNewlineTerminatedTokens` private.

**`TokenEnumerator`** — add `long CharactersConsumed` property, incremented in `Next()`. The iteration safety limit becomes:

```csharp
if (template.Options.MaxIterations > 0 && iterationCount > template.Options.MaxIterations)
    throw ...;

if (iterationCount > context.Enumerator.CharactersConsumed * 2 + 100)
    throw ...;
```

The `+ 100` provides headroom for templates with many tokens before any characters are consumed.

**`TokenizationEngineInternalTests`** — rewrite all tests to exercise the same behaviors through `ProcessTokenization` or the full `Tokenizer.Tokenize` pipeline. The behaviors under test (repeated tokens, newline-terminated tokens, front matter assignment, candidate assignment) are all observable through `TokenizeResult`.

**`Tokenizer.TokenizeCore`** — remove `inputLength` / `rawInput` parameter threading. The `rawInput` string was used for: hint pre-filtering (passes the full string to `HintStrategy.PreProcess`), diagnostic alignment rendering (passes to `DiagnosticCollector`), and the now-removed iteration cap. Hint pre-filtering and diagnostic alignment still need the raw string on the string input path — pass `rawInput` to the collector and hint strategy directly, but not to the engine.

### Files Changed

- `src/Tokenizer/Tokenization/ITokenizationEngine.cs`
- `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- `src/Tokenizer/Enumerators/TokenEnumerator.cs`
- `src/Tokenizer/Tokenizer.cs`
- `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs`

## Phase 2: Strip Duplicate Verbose Logging (D2)

### Problem

The engine has ~30 guarded `LogTrace`/`LogDebug` calls running alongside ~10 `collector.Record` calls. Many log the exact same event. Two parallel tracing systems is redundant — the diagnostics system was designed as the replacement.

### Rule

- **Keep:** `LogWarning`, `LogError` (error conditions like infinite loop detection), `LogDebug` at phase boundaries (setup, teardown summaries), target object validation logging.
- **Remove:** `LogTrace` that duplicates a `collector.Record` event (e.g., "Token match found" duplicates `PreambleMatched`). `LogTrace` for per-character enumerator movement. `LogDebug` that restates what the collector records (e.g., "Token matched: '{TokenName}'" duplicates `TokenAssigned`). Per-candidate iteration logging ("Skipping {TokenName}").
- **Preserve:** `IsEnabled` guards on all remaining log calls. The diagnostics system remains opt-in via `TokenizerOptions.EnableDiagnostics`.

### Outcome

The diagnostic collector is the single source for detailed trace. Consumers who want per-token visibility enable diagnostics and inspect `DiagnosticResult`. `ILogger` handles operation-level summaries only.

This also significantly shrinks the engine code before the Phase 4 split.

### Files Changed

- `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- `src/Tokenizer/TokenMatcher.cs` (same pattern: strip LogTrace that duplicates behavior observable through results)

## Phase 3: TokenMatcher Refactor

### Problem

1. The match loop is copy-pasted between `Match(string, tags)` and `Match<T>(string, tags)` — ~40 identical lines differing only in the `Tokenize` call and result type.
2. `Match(TextReader)` eagerly materializes via `ReadToEnd()` — no streaming.
3. No support for seekable stream rewinding between template matches.
4. No guard against unbounded buffering of non-seekable streams.

### Changes

**Single logic path** — extract a `MatchCore` method:

```csharp
private TResult MatchCore<TResult, TTokenizeResult>(
    string input,
    string[]? tags,
    TResult results,
    Func<Template, TTokenizeResult> tokenize)
```

All overloads adapt their input and delegate to `MatchCore`. Tag checking, try/catch, logging, and best-match selection exist in one place.

**Stream handling for MatchAsync:**

- `Stream` + `CanSeek`: record start position, rewind between templates. Create a fresh `StreamReader` per template match (avoids `StreamReader` internal buffer issues).
- `Stream` + `!CanSeek`: copy to `MemoryStream` if `TokenizerOptions.AllowStreamBuffering` is true. Throw `TokenizerException` if false ("Stream is not seekable. Provide a seekable stream or set TokenizerOptions.AllowStreamBuffering = true").
- `TextReader`: buffer into `MemoryStream` (async, chunked). Buffering is inherent to multi-template matching — no config guard needed. Then treat as seekable stream.

Each individual template match in the async path uses the real streaming async tokenization from Phase 4 (ring buffer, `ContinueTokenization`).

**New option:**

```csharp
public record TokenizerOptions
{
    // ... existing ...
    public bool AllowStreamBuffering { get; init; } = false;
}
```

**API changes to ITokenMatcher (Phase 3 — sync only):**

Sync (string input — unchanged):
- `Match(string)`, `Match(string, tags)`
- `Match<T>(string)`, `Match<T>(string, tags)`

Registration — sync:
- `RegisterTemplate(string)`, `RegisterTemplate(string, name)`, `RegisterTemplate(Template)`

Removed (sync stream/reader — breaking, replaced by async in Phase 4):
- `Match(TextReader)` and variants
- `Match(Stream, Encoding)` and variants
- `RegisterTemplate(TextReader)` and variants

The async methods (`MatchAsync`, `RegisterTemplateAsync`) are added in Phase 4 when the async engine is available.

### Files Changed

- `src/Tokenizer/TokenMatcher.cs`
- `src/Tokenizer/ITokenMatcher.cs`
- `src/Tokenizer/TokenizerOptions.cs`
- `tests/Tokenizer.Tests/TokenMatcherTests.cs`
- `tests/Tokenizer.Tests/TokenMatcherStreamTests.cs`

## Phase 4: Async Support

### Architecture

The async design uses a ring buffer on `TokenEnumerator` for chunked async I/O, and splits the engine into three cooperative phases. The sync path is preserved unchanged. No sync-over-async. No fake-async wrappers.

### TokenEnumerator — Ring Buffer

The existing pushback queue and direct `reader.Read()` calls are replaced by a single ring buffer:

```
TokenEnumerator (refactored internals):
  char[] ringBuffer (1024 default)
  int readPos, writePos, bufferedCount
  long charactersConsumed

  ValueTask FillBufferAsync(CancellationToken ct)
    → await reader.ReadAsync(tempBuf, ct)
    → copy into ring buffer

  void FillBuffer()
    → reader.Read(tempBuf)
    → copy into ring buffer

  bool NeedsRefill
    → bufferedCount < watermark (e.g., 256)

  // Public API — unchanged signatures, now read from ring buffer:
  char Next()
  char Peek()
  bool TryMatch(string value)
  bool TryMatch(IEnumerable<Token>, bool, IList<Token>)
  void Advance(int count)
```

The pushback queue merges into the ring buffer. `TryMatch` lookahead reads from the buffer instead of filling a separate queue. CRLF normalization happens during buffer fill, not per-character.

### TokenizationEngine — Begin/Continue/End Split

```csharp
internal class TokenizationEngine : ITokenizationEngine
{
    // Setup: validation, diagnostics init, hint processing
    void BeginTokenization(
        Template template,
        object? targetObject,
        ITokenizationContext context,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);

    // Main loop: runs until buffer needs refill or input exhausted
    // Returns true when input is fully consumed, false when buffer needs refill
    bool ContinueTokenization(
        ITokenizationContext context,
        CancellationToken ct);

    // Teardown: remaining candidates, front matter tokens, diagnostics summary
    void EndTokenization(
        ITokenizationContext context,
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector);

    // Existing sync entry point — calls all three internally:
    void ProcessTokenization(
        Template template,
        object? targetObject,
        ITokenizationContext context,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null)
    {
        BeginTokenization(template, targetObject, context, result, collector, hintStrategy);
        do {
            context.Enumerator.FillBuffer();
        } while (!ContinueTokenization(context, CancellationToken.None));
        EndTokenization(context, template, targetObject, result, collector);
    }
}
```

`ContinueTokenization` contains the same main loop body as today. Two additional checks per iteration:

1. `ct.IsCancellationRequested` — sync volatile bool read, near-zero cost
2. `enumerator.NeedsRefill` — if true, return false to yield back to caller

### ITokenizationEngine Interface

The interface exposes only `ProcessTokenization` (sync entry point). The Begin/Continue/End methods are on the concrete class — they're internal implementation used by `Tokenizer.TokenizeAsync`, not a polymorphic contract.

### Tokenizer — Async Orchestration

```csharp
public async Task<TokenizeResult> TokenizeAsync(
    Template template, TextReader input, CancellationToken ct = default)
{
    var result = new TokenizeResult(template);
    using var context = new TokenizationContext();
    context.Initialize(input);

    IDiagnosticCollector collector = template.Options.EnableDiagnostics
        ? new DiagnosticCollector(null, null)
        : NullDiagnosticCollector.Instance;

    var hintsMissing = hintStrategy.PreProcess(template, context.Enumerator, null, result, collector);
    if (!hintsMissing)
    {
        engine.BeginTokenization(template, null, context, result, collector, hintStrategy);
        do {
            await context.Enumerator.FillBufferAsync(ct);
        } while (!engine.ContinueTokenization(context, ct));
        engine.EndTokenization(context, template, null, result, collector);
    }

    resultBuilder.BuildUnmatchedTokens(template, result, collector);
    result.Diagnostics = collector.GetResult();
    return result;
}
```

### CompileAsync

Templates are typically small (tens of lines). `CompileAsync` buffers the template source async into a `MemoryStream`, then compiles synchronously. The `TemplateLexer` and its `LookaheadReader` remain sync — the async value is "don't block a thread reading the template from disk/network," not "stream the compilation."

```csharp
public async Task<Template> CompileAsync(TextReader reader, CancellationToken ct = default)
{
    var buffer = new MemoryStream();
    // Chunked async copy with cancellation
    var charBuffer = new char[4096];
    int read;
    while ((read = await reader.ReadAsync(charBuffer, 0, charBuffer.Length)) > 0)
    {
        ct.ThrowIfCancellationRequested();
        // encode to buffer
    }
    buffer.Position = 0;
    using var bufferReader = new StreamReader(buffer);
    return parser.Parse(bufferReader);
}
```

### Tokenizer Public API

Sync (string input):
- `TokenizeResult Tokenize(Template, string)`
- `TokenizeResult<T> Tokenize<T>(Template, string)`
- `TokenizeResult Tokenize(string pattern, string input)` — convenience
- `TokenizeResult<T> Tokenize<T>(string pattern, string input)` — convenience

Async (stream/reader input):
- `Task<TokenizeResult> TokenizeAsync(Template, TextReader, CancellationToken)`
- `Task<TokenizeResult<T>> TokenizeAsync<T>(Template, TextReader, CancellationToken)`
- `Task<TokenizeResult> TokenizeAsync(Template, Stream, Encoding, CancellationToken)`
- `Task<TokenizeResult<T>> TokenizeAsync<T>(Template, Stream, Encoding, CancellationToken)`

Compilation:
- `Template Compile(string)`, `Compile(string, string)` — sync
- `Task<Template> CompileAsync(TextReader, CancellationToken)`, with name variant — async
- `Task<Template> CompileAsync(Stream, Encoding, CancellationToken)`, with name variant — async

Removed (breaking):
- `Tokenize(Template, TextReader)` — sync TextReader, replaced by async
- `Tokenize(Template, Stream, Encoding)` — sync Stream, replaced by async
- `Compile(TextReader)` — sync TextReader, replaced by async

### CancellationToken Handling

Two cancellation checkpoints:

1. **Buffer refill** — `FillBufferAsync` passes `CancellationToken` to `reader.ReadAsync`. If cancelled during I/O wait, `OperationCanceledException` propagates.
2. **Engine iteration** — `ContinueTokenization` checks `ct.IsCancellationRequested` at the top of each main loop iteration. This is a sync volatile bool read (near-zero cost) that prevents a long-running template match from grinding through buffered data without checking cancellation.

### Conditional Compilation

The ring buffer implementation may use `#if NET8_0_OR_GREATER` for `Memory<char>`/`ReadAsync(Memory<char>)` overloads vs `char[]`/`ReadAsync(char[], int, int)` on netstandard2.0. Same pattern already used in `LookaheadReader`.

`ValueTask` for `FillBufferAsync` requires `System.Threading.Tasks.Extensions` on netstandard2.0 (already a dependency if present, otherwise add it).

### Files Changed

- `src/Tokenizer/Enumerators/TokenEnumerator.cs` — ring buffer, FillBuffer/FillBufferAsync, CharactersConsumed
- `src/Tokenizer/Tokenization/TokenizationEngine.cs` — Begin/Continue/End split
- `src/Tokenizer/Tokenization/ITokenizationEngine.cs` — updated signature (inputLength removed)
- `src/Tokenizer/Tokenization/TokenizationContext.cs` — may need async initialization support
- `src/Tokenizer/Tokenizer.cs` — TokenizeAsync, CompileAsync, remove sync stream/reader overloads
- `src/Tokenizer/ITokenizer.cs` — async methods added, sync stream/reader removed
- `src/Tokenizer/TokenMatcher.cs` — MatchAsync, RegisterTemplateAsync
- `src/Tokenizer/ITokenMatcher.cs` — async methods added, sync stream/reader removed
- `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs` — no changes (CompileAsync buffers externally)
- `src/Tokenizer/Tokenizer.csproj` — System.Threading.Tasks.Extensions if needed
- `tests/Tokenizer.Tests/` — async test coverage

## Breaking Changes Summary

All breaking changes are expected for v3:

1. `Tokenize(Template, TextReader)` removed — use `TokenizeAsync(Template, TextReader, CancellationToken)`
2. `Tokenize(Template, Stream, Encoding)` removed — use `TokenizeAsync(Template, Stream, Encoding, CancellationToken)`
3. `Compile(TextReader)` removed — use `CompileAsync(TextReader, CancellationToken)`
4. `TokenMatcher.Match(TextReader)` and variants removed — use `MatchAsync`
5. `TokenMatcher.Match(Stream, Encoding)` and variants removed — use `MatchAsync`
6. `TokenMatcher.RegisterTemplate(TextReader)` and variants removed — use `RegisterTemplateAsync`
7. `ITokenizationEngine.ProcessTokenization` signature changed (inputLength removed) — internal only

## What This Design Does NOT Do

- No async-only path — sync remains first-class with zero overhead
- No sync-over-async — no `.GetAwaiter().GetResult()` anywhere
- No fake-async wrappers — all async methods perform real async I/O
- No `IAsyncEnumerable` on the lexer — CompileAsync buffers externally, compilation stays sync
- No changes to the tokenization algorithm — the engine logic is identical, just split into cooperative phases
