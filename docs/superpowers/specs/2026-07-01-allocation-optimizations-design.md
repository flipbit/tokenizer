# Allocation Optimizations Design

## Overview

Three targeted optimizations to reduce heap allocations on the tokenization hot path. No behavioral changes — pure performance work.

## Decisions Made

- **TokenEnumerator**: Full char-based refactor (Next/Peek return `char`) plus Span-based `Match()` behind `#if NET8_0_OR_GREATER`, with `string.CompareOrdinal` fallback on netstandard2.0
- **ObjectExtensions**: Index-based path navigation eliminates `Skip(1).ToArray()` array allocations on recursion
- **TokenizationEngine**: Log-level guards prevent `string.Join` and LINQ allocations when logging is disabled

---

## Optimization 1: TokenEnumerator char-based + Span matching

### Files
- `src/Tokenizer/Enumerators/TokenEnumerator.cs`
- `src/Tokenizer/Enumerators/FileLocation.cs`
- `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- `src/Tokenizer/Tokenization/HintProcessor.cs` (minor — return value already discarded)

### Changes

**TokenEnumerator.cs:**
- `Next()` returns `char` instead of `string`. Returns `'\0'` when empty. Uses `pattern[currentLocation]` instead of `pattern.Substring(currentLocation, 1)`.
- `Peek()` returns `char` instead of `string`. Same pattern.
- `Peek(int offset)` returns `char` instead of `string`. Same pattern.
- `Match(string value)` — on NET8_0_OR_GREATER, uses `pattern.AsSpan(currentLocation, value.Length).SequenceEqual(value.AsSpan())`. On netstandard2.0, uses `string.CompareOrdinal(pattern, currentLocation, value, 0, value.Length) == 0`. Both are zero-allocation.

**FileLocation.cs:**
- `Increment(string value)` → `Increment(char value)`. String comparisons `== "\r"` → `== '\r'`, `== "\n"` → `== '\n'`.

**TokenizationEngine.cs (4 call sites):**
- `next == "\n"` → `next == '\n'`
- `next == "\r"` → `next == '\r'`
- `Replacement.Append(next)` — already works with `char`, more efficient.

### Allocations eliminated
- Per-character `Substring(pos, 1)` in `Next()`, `Peek()`, `Peek(int)` — called thousands of times per tokenization
- Per-preamble-check `Substring(pos, length)` in `Match()` — called for every token at every position

---

## Optimization 2: ObjectExtensions index-based path navigation

### Files
- `src/Tokenizer/Extensions/ObjectExtensions.cs`

### Changes

Replace recursive `IReadOnlyList<string> path` parameter with `(string[] segments, int depth)`.

**SetValue:**
- `Split('.')` once, determine starting depth (0 or 1 depending on type name match)
- Pass `(segments, depth)` to `SetInnerValue`

**SetInnerValue signature change:**
- `(object, IReadOnlyList<string> path, object value, StringComparison)` → `(object, string[] segments, int depth, object value, StringComparison)`
- `path[0]` → `segments[depth]`
- `path.Count == 1` → `depth == segments.Length - 1`
- Recursive call: `SetInnerValue(obj, path.Skip(1).ToArray(), ...)` → `SetInnerValue(obj, segments, depth + 1, ...)`

**Same pattern for GetValue/GetInnerValue.**

### Allocations eliminated
- 1 array allocation per recursion level per property assignment
- For 3-deep path: 2 arrays saved per assignment

---

## Optimization 3: Log-level guards on diagnostic string operations

### Files
- `src/Tokenizer/Tokenization/TokenizationEngine.cs`

### Changes

Wrap expensive inline string-building expressions in `log.IsEnabled(LogLevel.Trace)` guards:

**Guarded call sites in TokenizationEngine.cs:**
- Line ~155: `string.Join(", ", matches.Select(m => m.Name))` in token match logging
- Line ~260: `string.Join(", ", candidates.Tokens.Select(t => t.Name))` in assignment attempt logging
- Line ~292: Same pattern in assignment failure logging
- Line ~397: Same pattern in backtrack logging
- Line ~406: Same pattern in infinite loop error logging (keep this one — it's an error path, not hot)

**Additional cleanup:**
- Line ~331: `template.Tokens.Where(t => t.IsFrontMatterToken).ToList()` → drop `.ToList()`, use lazy enumeration since only iterated once in `foreach`

### Allocations eliminated
- `string.Join` + LINQ iterator allocations on every match/backtrack when logging is disabled (the common production case)
- One `List<Token>` allocation from unnecessary `.ToList()`
