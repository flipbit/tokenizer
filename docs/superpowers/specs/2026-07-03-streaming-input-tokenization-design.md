# Streaming Input Tokenization — Design Spec

## Overview

Refactor the tokenization input path to work natively on `TextReader` instead of `string`. The `TokenEnumerator` becomes `TextReader`-native with inline CRLF normalization. String and `Stream` overloads are convenience wrappers. Hint processing is rearchitected with multiple strategies benchmarked to determine the best default.

## Approach

`TokenEnumerator` reads from a `TextReader` internally. String inputs wrap via `StringReader`; `Stream` inputs wrap via `StreamReader(stream, encoding, leaveOpen: true)`. A pushback buffer supports non-advancing `TryMatch` lookahead. Four hint strategies are implemented behind an `IHintStrategy` interface and benchmarked to select the default.

---

## 1. TokenEnumerator Refactoring

### Current state

`TokenEnumerator(string pattern)` — stores the full string, indexes into it via `pattern[currentLocation]`. CRLF normalization is eager: `pattern.Replace("\r\n", "\n")` in the constructor.

### New state

`TokenEnumerator(TextReader reader)` — reads from any `TextReader`. No stored string, no eager normalization.

### Internal fields

```csharp
internal sealed class TokenEnumerator
{
    private readonly TextReader reader;
    private readonly Queue<char> pushback;  // chars read ahead by TryMatch
    private bool isEmpty;
    // FileLocation tracking unchanged
}
```

### Character reading with CRLF normalization

All line ending conventions (`\n`, `\r\n`, `\r`) normalize to `\n`:

```
ReadChar():
  source = pushback.Count > 0 ? pushback.Dequeue() : reader.Read()
  if source == -1 → isEmpty = true, return '\0'
  if source == '\r':
    next = pushback.Count > 0 ? pushback.Peek() : reader.Peek()
    if next == '\n' → consume it (Dequeue or Read)
    return '\n'    // lone \r also becomes \n
  return (char)source
```

One code path for both string and stream inputs.

### Method changes

- **`Next()`**: Calls `ReadChar()`, updates `FileLocation`. Returns the character.
- **`Peek()`**: If pushback is non-empty, return front (already CRLF-normalized). Otherwise call `reader.Peek()` — if it returns `\r`, we cannot resolve whether it's `\r\n` without consuming. Solution: call `ReadChar()` to resolve the CRLF, push the result into the pushback buffer, and return it. This ensures `Peek()` always returns the normalized character.
- **`Peek(int offset)`**: Removed. Only caller was `HandleWindowsNewlines` in `TokenizationEngine`, which is eliminated by inline CRLF normalization.
- **`IsEmpty`**: `isEmpty` flag set when `ReadChar()` returns end-of-stream and pushback is empty.
- **`Reset()`**: Kept for backward compat during hint strategy migration. Only meaningful for the `EnumeratorScanHintStrategy` (Strategy 1). For `StringReader`, reset works by re-creating from the original string. For non-seekable `TextReader` inputs, `Reset()` throws `NotSupportedException` — strategies 2-4 do not call it, so this only surfaces if someone explicitly uses Strategy 1 with a stream input.

### TryMatch with pushback buffer

`TryMatch(string value)` is non-advancing — it must compare without consuming characters.

```
TryMatch("Domain:"):
  1. Ensure pushback has >= value.Length chars (read from reader to fill)
  2. Compare pushback contents against value
  3. Leave pushback intact — chars are not consumed
  4. Return true/false
```

After a successful `TryMatch`, the caller typically calls `Advance(value.Length)`, which drains chars from the pushback buffer via `Next()`.

When multiple `TryMatch` calls happen at the same position (checking multiple token preambles), the pushback buffer fills on the first call and subsequent calls compare against it without touching the reader.

The buffer naturally stays small — sized to the longest preamble being matched (typically 10-30 characters).

### `TryMatch(IEnumerable<Token>, bool, IList<Token>)` overload

Unchanged in signature. Calls `TryMatch(string)` per token preamble as today. Benefits from pushback buffer reuse across calls.

### Advance(int count)

Unchanged — calls `Next()` N times, which drains from pushback before reading from reader.

### Removed

- `string pattern` field
- `int patternLength` field
- `pattern.Contains("\r\n")` / `pattern.Replace("\r\n", "\n")` in constructor
- `HandleWindowsNewlines()` in `TokenizationEngine`

---

## 2. Public API Changes

### ITokenizer additions

```csharp
public interface ITokenizer
{
    // Existing string overloads (unchanged)
    TokenizeResult Tokenize(Template template, string input);
    TokenizeResult<T> Tokenize<T>(Template template, string input) where T : class, new();
    TokenizeResult Tokenize(string pattern, string input);
    TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new();

    // New TextReader overloads
    TokenizeResult Tokenize(Template template, TextReader input);
    TokenizeResult<T> Tokenize<T>(Template template, TextReader input) where T : class, new();

    // New Stream overloads (convenience)
    TokenizeResult Tokenize(Template template, Stream input, Encoding encoding);
    TokenizeResult<T> Tokenize<T>(Template template, Stream input, Encoding encoding) where T : class, new();

    // ... existing Compile, Cache methods unchanged
}
```

### ITokenMatcher additions

```csharp
public interface ITokenMatcher
{
    // Existing string overloads (unchanged)
    TokenMatcherResult Match(string input);
    TokenMatcherResult Match(string input, string[]? tags);
    TokenMatcherResult<T> Match<T>(string input) where T : class, new();
    TokenMatcherResult<T> Match<T>(string input, string[]? tags) where T : class, new();

    // New TextReader overloads
    TokenMatcherResult Match(TextReader input);
    TokenMatcherResult Match(TextReader input, string[]? tags);
    TokenMatcherResult<T> Match<T>(TextReader input) where T : class, new();
    TokenMatcherResult<T> Match<T>(TextReader input, string[]? tags) where T : class, new();

    // New Stream overloads
    TokenMatcherResult Match(Stream input, Encoding encoding);
    TokenMatcherResult Match(Stream input, Encoding encoding, string[]? tags);
    TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding) where T : class, new();
    TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding, string[]? tags) where T : class, new();
}
```

### Overloads not included

`Tokenize(string pattern, TextReader input)` and `Tokenize(string pattern, Stream input, Encoding encoding)` are deliberately omitted to keep the API lean. Users can call `Compile(pattern)` then `Tokenize(template, reader)`.

---

## 3. Disposal Rules

- **`StringReader` created from string overloads**: created internally, disposed internally via `using`
- **`StreamReader` created from Stream overloads**: created internally with `leaveOpen: true`, disposed internally via `using`. The caller's `Stream` is not disposed.
- **`TextReader` passed by caller**: never disposed — caller owns it

The `TextReader` overload is the core implementation and never disposes. String and Stream overloads wrap in `using` before delegating.

---

## 4. Hint Strategy Benchmarking

### Interface

```csharp
internal interface IHintStrategy
{
    /// <summary>
    /// Pre-tokenization hint processing. Strategies 1 and 2 do their work here.
    /// Returns true if required hints are missing and tokenization should be skipped.
    /// </summary>
    bool PreProcess(Template template, TokenEnumerator enumerator,
                    TokenizeResultBase result, IDiagnosticCollector collector);

    /// <summary>
    /// Called by the engine when a token preamble is matched during tokenization.
    /// Strategies 3 and 4 use this to track hint satisfaction.
    /// </summary>
    void OnTokenMatched(Token token);

    /// <summary>
    /// Post-tokenization hint evaluation. Strategies 3 and 4 check results here.
    /// Returns true if required hints are missing.
    /// </summary>
    bool PostProcess(TokenizeResultBase result);
}
```

### Strategy 1: EnumeratorScanHintStrategy (current baseline)

Two-pass. Iterates enumerator character-by-character checking each hint via `TryMatch`. Resets enumerator after. Kept for benchmark comparison only.

### Strategy 2: ContainsHintStrategy

Two-phase. For string inputs, uses `string.Contains()` per hint on the original string — no enumerator involvement. For stream inputs, reads the full input to string first via `TextReader.ReadToEnd()`, then checks. Enumerator is untouched, no reset needed.

### Strategy 3: IntegratedHintStrategy

Single-pass. No separate hint phase. `PreProcess` is a no-op. During tokenization, the engine calls `OnTokenMatched()` for each matched preamble. `PostProcess` checks if all required hints were satisfied. Stream-native — no rewind.

Trade-off: performs full tokenization work even for non-matching templates.

### Strategy 4: EarlyAbandonHintStrategy

Like Strategy 3, but monitors progress during tokenization. If required hints haven't been found and remaining input is insufficient to contain them, signals early termination. Gets stream-friendly benefit of Strategy 3 with some rejection efficiency.

### Selection

```csharp
public record class TokenizerOptions
{
    // Default set after benchmarking — initially ContainsHintStrategy
    // as the most likely winner, updated based on benchmark results
    internal IHintStrategy HintStrategy { get; init; } = new ContainsHintStrategy();
}
```

Default is set after benchmarking. Multiple strategies may be retained if tradeoffs are scenario-dependent (e.g. single-template vs multi-template matching).

---

## 5. TokenMatcher Stream Handling

`TokenMatcher.Match()` tries input against multiple templates. A `TextReader` can only be read once, but the matcher needs to retry against each template.

For `TextReader` and `Stream` overloads, the matcher reads the input to string once via `ReadToEnd()`, then creates a `StringReader` per template attempt:

```csharp
public TokenMatcherResult Match(TextReader input)
{
    var content = input.ReadToEnd();
    return Match(content);
}
```

This is pragmatic:
- The matcher must re-read input per template — unavoidable
- Real-world inputs are small (1-7 KB in whois)
- Single-template `Tokenize(Template, TextReader)` remains truly streaming
- The caller's `TextReader` is consumed fully as expected

---

## 6. Benchmarks

### InputStreamBenchmarks

Measures the cost of the TextReader-native enumerator vs the current string-based baseline.

| Benchmark | Measures |
|---|---|
| `String_Small/Medium/Large` | String input via `StringReader` — overhead of new enumerator |
| `TextReader_Small/Medium/Large` | `TextReader` input directly |
| `Stream_Small/Medium/Large` | `Stream` + `Encoding` overload — `StreamReader` construction cost |

### HintStrategyBenchmarks

Each of the four strategies across scenarios that matter.

| Benchmark | Measures |
|---|---|
| `Strategy_SingleTemplate_HintsPresent` | Happy path — hints found, tokenization proceeds |
| `Strategy_SingleTemplate_HintsMissing` | Rejection path — how fast does each strategy bail |
| `Strategy_MultiTemplate_5/15/50` | `TokenMatcher` scenario — filtering across many templates |

Parameterized by strategy (EnumeratorScan, Contains, Integrated, EarlyAbandon) and workload size (Small/Medium/Large).

### Regression checks

Run existing `TokenizationBenchmarks` and `MatcherBenchmarks` before and after the refactor. Compare against baselines in `benchmarks/baselines/2026-07-01/`.

### Decision criteria

1. Pick the default hint strategy based on benchmark results
2. If `StringReader` overhead is negligible (<5%), keep single code path. If significant, revisit.
3. Document results alongside existing baseline files

---

## 7. Tier 8 Compatibility

| Tier 8 Item | Impact |
|---|---|
| Extract `ITokenParser` | No conflict — compilation-side |
| Public `IHintGenerator` | `IHintStrategy` replaces `IHintProcessor` — hint generators feed into strategies, compatible |
| Middleware/pipeline hooks | `IHintStrategy.PreProcess`/`OnTokenMatched`/`PostProcess` pattern aligns with a future pipeline model |
| Custom `ITokenMatcher` strategy | Already an interface — stream overloads are additive |
| AST visitor pattern | Compilation-side, no interaction |
| Parser error recovery | Compilation-side, no interaction |
| Remove `ITokenDecorator` marker | Unrelated to input handling |

No blockers. The `IHintStrategy` hook mechanism is a stepping stone toward Tier 8 middleware/pipeline hooks.

---

## 8. Breaking Changes

| Change | Impact |
|---|---|
| `TokenEnumerator` constructor takes `TextReader` instead of `string` | Internal class — no public API break |
| `Peek(int offset)` removed | Internal — no public API break |
| `HandleWindowsNewlines` removed from engine | Internal — no public API break |
| `IHintProcessor` replaced by `IHintStrategy` | Internal interface — no public API break |
| `TextReader` overloads on `ITokenizer` | Non-breaking (additive) |
| `Stream` overloads on `ITokenizer` | Non-breaking (additive) |
| `TextReader` overloads on `ITokenMatcher` | Non-breaking (additive) |
| `Stream` overloads on `ITokenMatcher` | Non-breaking (additive) |

All changes are either internal or additive. No public API breaks.
