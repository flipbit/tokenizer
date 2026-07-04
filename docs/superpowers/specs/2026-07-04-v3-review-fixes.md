# V3 Review Fixes Plan (Post-Async)

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to
> implement this plan task-by-task.

**Goal:** Address code review findings from the 2026-07-04 v3 review (post-async)

**Source Review:** `docs/superpowers/specs/2026-07-04-v3-review.md`
**Design Doc:** `docs/superpowers/specs/2026-07-04-engine-cleanup-and-async-design.md`
**Implementation Plan:** `docs/superpowers/specs/2026-07-03-v3-review-fixes.md`

---

## Dismissed Issues

| ID | Rationale | Action |
|----|-----------|--------|
| H5 | TextReader has no seek concept — buffering is mandatory for multi-template matching. The AllowStreamBuffering option applies to Stream inputs only, where the caller can provide a seekable stream. The asymmetry with EnsureSeekableAsync is intentional. | Add inline comment in `BufferTextReaderAsync` explaining why AllowStreamBuffering is not checked |
| M5 | Race-to-add is a standard .NET concurrency pattern (MemoryCache uses the same approach). Duplicate compilation wastes microseconds but produces correct results. Lazy wrapper would add overhead to every cache hit for a rare edge case. | Add inline comment in `TemplateCache.GetOrAdd` explaining the intentional race-to-add pattern |
| L5 | GrowBuffer growth is bounded by max preamble length, not input length. MaxInputLength enforcement (M1) caps total memory. A hard GrowBuffer cap could break legitimate templates with long preambles. | None (addressed indirectly by M1 fix) |
| D3 | Duplicate of H6 — addressed there | None |
| D1 | Duplicate of M2+M3 — addressed there | None |
| D2 | Duplicate of M4 — addressed there | None |

---

## Fix Tasks

### Task 1: Remove Peek()-based exhaustion checks from TokenEnumerator
**Addresses:** H2, H3
**Chosen approach:** Remove `reader.Peek()` checks from both `FillBuffer` and `FillBufferAsync`. Only set `readerExhausted` when `Read`/`ReadAsync` returns 0.
**Files:** Modify `src/Tokenizer/Enumerators/TokenEnumerator.cs`, Test `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorRingBufferTests.cs`
**Steps:**
1. Write a test: construct `TokenEnumerator` with a custom `TextReader` that returns -1 from `Peek()` after a successful `Read` (simulating non-buffered reader). Verify all input is consumed without premature truncation
2. Verify test fails (sync path truncates due to Peek)
3. Remove lines 107-111 in `FillBuffer` (the `if (reader.Peek() == -1)` block)
4. Remove lines 140-144 in `FillBufferAsync` (same pattern)
5. Verify test passes
6. Run full test suite, verify no regressions
7. Commit

### Task 2: Fix thread-unsafe ContainsHintStrategy
**Addresses:** H1
**Chosen approach:** Create a new `ContainsHintStrategy` instance per tokenization call instead of sharing one across the `Tokenizer` lifetime.
**Files:** Modify `src/Tokenizer/Tokenizer.cs`, Test existing tests
**Steps:**
1. Write a test: create a `Tokenizer`, register templates with hints, call `Tokenize` (sync, string input) and `TokenizeAsync` (TextReader input with hints) concurrently. Verify both produce correct hint results without cross-contamination
2. Verify test fails or exhibits race (may need repeated runs / stress)
3. In `Tokenizer.cs`, remove the field `private readonly IHintStrategy hintStrategy = new ContainsHintStrategy()` (line 27)
4. In `TokenizeCore`, create `var hintStrategy = new ContainsHintStrategy()` at the start
5. In `TokenizeAsyncCore`, create `var hintStrategy = new ContainsHintStrategy()` at the start
6. Verify all tests pass
7. Commit

### Task 3: Forward encoding parameter in MatchAsync
**Addresses:** H4
**Chosen approach:** Add `Encoding encoding` parameter to `MatchAsyncFromSeekableStream` and use it in the `StreamReader` constructor.
**Files:** Modify `src/Tokenizer/TokenMatcher.cs`, Test `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs`
**Steps:**
1. Write a test: call `MatchAsync(Stream, Encoding.Unicode, ...)` with a UTF-16 encoded stream containing template-matchable content. Verify tokens are correctly extracted (currently fails because UTF-8 is hardcoded)
2. Verify test fails
3. Add `Encoding encoding` parameter to `MatchAsyncFromSeekableStream` method signature
4. Replace `Encoding.UTF8` on line 356 with the `encoding` parameter
5. Update all call sites of `MatchAsyncFromSeekableStream` to pass the encoding through
6. Verify test passes
7. Run full test suite
8. Commit

### Task 4: Add inline comment for BufferTextReaderAsync (dismissed H5)
**Addresses:** H5 (dismissed)
**Files:** Modify `src/Tokenizer/TokenMatcher.cs`
**Steps:**
1. Add inline comment above `BufferTextReaderAsync` explaining that AllowStreamBuffering is intentionally not checked because TextReader has no seek concept — buffering is the only way to support multi-template matching
2. Commit

### Task 5: Reuse staging buffer in TokenEnumerator
**Addresses:** H6, D3
**Chosen approach:** Add `char[] stagingBuffer` as instance field, allocate once in constructor, resize only in `GrowBuffer`.
**Files:** Modify `src/Tokenizer/Enumerators/TokenEnumerator.cs`, Test `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorRingBufferTests.cs`
**Steps:**
1. Write a test: create a `TokenEnumerator` from a `TextReader` with large input requiring multiple `FillBuffer` calls. Verify correct output (baseline — should already pass, this is a refactor)
2. Add `private char[] stagingBuffer` field, initialize to `new char[DefaultBufferSize]` in constructor
3. In `FillBuffer`, replace `var staging = new char[available]` with `var staging = stagingBuffer; if (available > staging.Length) staging = stagingBuffer = new char[available]`
4. In `FillBufferAsync`, apply the same change
5. In `GrowBuffer`, resize stagingBuffer if needed: `if (newSize > stagingBuffer.Length) stagingBuffer = new char[newSize]`
6. Verify all tests pass
7. Commit

### Task 6: Enforce MaxInputLength on async path
**Addresses:** M1
**Chosen approach:** Check `CharactersConsumed + bufferedCount` against a max input length in `TokenizeAsyncCore`'s fill loop.
**Files:** Modify `src/Tokenizer/Tokenizer.cs`, Test `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`
**Steps:**
1. Write a test: set `MaxInputLength = 100`, call `TokenizeAsync` with a `TextReader` producing 200+ chars. Verify `TokenizerException` is thrown
2. Verify test fails (no check exists)
3. In `TokenizeAsyncCore`, after `await context.Enumerator.FillBufferAsync(ct)`, add a check: if `MaxInputLength > 0` and `context.Enumerator.CharactersConsumed + bufferedCount > MaxInputLength`, throw `TokenizerException` with a message matching the sync path
4. Note: `bufferedCount` is private. Either add a `BufferedCount` property on `TokenEnumerator` or check `CharactersConsumed` alone (close enough since consumed tracks what's been dequeued)
5. Verify test passes
6. Run full test suite
7. Commit

### Task 7: Add observability parity to async path
**Addresses:** M2, M3, D1
**Chosen approach:** Inline the same observability code from `TokenizeCore` into `TokenizeAsyncCore` — BeginScope, try/catch, diagnostic summary, required-missing warnings.
**Files:** Modify `src/Tokenizer/Tokenizer.cs`, Test `tests/Tokenizer.Tests/` (observability tests)
**Steps:**
1. Write a test: call `TokenizeAsync` with EnableDiagnostics=true and a template with required tokens. Verify diagnostics result is populated (summary, alignment, issues)
2. Verify test behavior (may partially work already)
3. In `TokenizeAsyncCore`:
   a. Add `BeginScope` with structured properties (TemplateName, TokenCount, Operation="TokenizeAsync")
   b. Wrap the tokenization body in try/catch matching sync path's error handling
   c. After `resultBuilder.BuildUnmatchedTokens`, add required-missing count logging matching sync path
   d. After `result.Diagnostics = collector.GetResult()`, add diagnostic summary/alignment/issues logging matching sync path
4. Verify test passes
5. Run full test suite
6. Commit

### Task 8: Add Begin/Continue/End to ITokenizationEngine
**Addresses:** M4, D2
**Chosen approach:** Add `BeginTokenization`, `ContinueTokenization`, `EndTokenization` to the `ITokenizationEngine` interface. Remove the concrete cast.
**Files:** Modify `src/Tokenizer/Tokenization/ITokenizationEngine.cs`, `src/Tokenizer/Tokenizer.cs`, Test existing tests
**Steps:**
1. Add `BeginTokenization`, `ContinueTokenization`, `EndTokenization` method signatures to `ITokenizationEngine`
2. In `TokenizeAsyncCore`, replace `var engine = (TokenizationEngine)tokenizationEngine;` with direct `tokenizationEngine.BeginTokenization(...)` etc.
3. Verify all tests pass (no behavior change)
4. Commit

### Task 9: Eliminate frontMatterTokens ToList() allocation
**Addresses:** M6
**Chosen approach:** Replace `.Where().ToList()` with direct iteration + inline if check.
**Files:** Modify `src/Tokenizer/Tokenization/TokenizationEngine.cs`
**Steps:**
1. In `ProcessFrontMatterTokens` (line 354), replace `var frontMatterTokens = template.Tokens.Where(t => t.IsFrontMatterToken).ToList();` with `foreach (var token in template.Tokens)` and add `if (!token.IsFrontMatterToken) continue;` inside the loop
2. Remove the `frontMatterTokens` variable entirely
3. Verify all tests pass
4. Commit

### Task 10: Remove LogTrace narration calls from TokenizeCore
**Addresses:** M7
**Chosen approach:** Remove all 4 LogTrace "narration" calls that just describe control flow.
**Files:** Modify `src/Tokenizer/Tokenizer.cs`
**Steps:**
1. Remove the LogTrace block at lines 198-201 ("Tokenization context initialized")
2. Remove the LogTrace block at lines 208-211 ("Processing hints")
3. Remove the LogTrace block at lines 220-223 ("Hints validated successfully, proceeding with tokenization")
4. Remove the LogTrace block at lines 233-236 ("Building unmatched tokens collection")
5. Verify all tests pass
6. Commit

### Task 11: Add async tag-filtering tests for MatchAsync
**Addresses:** M8
**Files:** Modify `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs`
**Steps:**
1. Add test: register templates with different tags, call `MatchAsync(TextReader, tags)`, verify only matching templates are considered
2. Add test: register templates with tags, call `MatchAsync<T>(TextReader, tags)`, verify typed results filter correctly
3. Verify tests pass
4. Commit

### Task 12: Add RegisterTemplateAsync Stream tests
**Addresses:** M9
**Files:** Modify `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs`
**Steps:**
1. Add test: call `RegisterTemplateAsync(Stream, Encoding.UTF8)`, verify template is compiled and added to Templates collection
2. Add test: call `RegisterTemplateAsync(Stream, Encoding.UTF8, "name")`, verify template name is set
3. Verify tests pass
4. Commit

### Task 13: Add CompileAsync cancellation tests
**Addresses:** M10
**Files:** Modify `tests/Tokenizer.Tests/CompileAsyncTests.cs`
**Steps:**
1. Add test: call `CompileAsync` with a pre-cancelled `CancellationToken`, verify `OperationCanceledException` is thrown
2. Add test: call `CompileAsync` with a token that cancels mid-read (use a slow `TextReader` that cancels after first chunk), verify `OperationCanceledException`
3. Verify tests pass
4. Commit

### Task 14: Add GrowBuffer test coverage
**Addresses:** M11
**Files:** Modify `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorRingBufferTests.cs`
**Steps:**
1. Add test: create `TokenEnumerator` with input containing a string longer than 1024 chars. Call `TryMatch` with the full string, forcing `GrowBuffer`. Verify match succeeds
2. Add test: verify `TryMatch` with a 2048+ char string still works (double growth)
3. Verify tests pass
4. Commit

### Task 15: Replace BCL tests with real engine state tests
**Addresses:** M12
**Files:** Modify `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineStateTests.cs`
**Steps:**
1. Delete `GivenMatchIds_WhenTrackingMatches_ThenMaintainsUniqueSet` (line 69) — tests `HashSet<int>.Add`
2. Delete `GivenDisabledRepeatingTokens_WhenTrackingDisabled_ThenPreventsRematching` (line 86) — tests `HashSet<int>.Add`
3. Delete `GivenReplacementBuffer_WhenAccumulatingCharacters_ThenBuildsCorrectly` (line 100) — tests `StringBuilder.Append`
4. Delete `GivenReplacementBuffer_WhenClearing_ThenResetsState` (line 117) — tests `StringBuilder.Clear`
5. Add replacement test: tokenize input with repeating token, verify match IDs track correctly through the engine (not via BCL)
6. Add replacement test: tokenize input with a disabled repeating token, verify it's not re-matched
7. Verify all tests pass
8. Commit

### Task 16: Add dispose for MemoryStream and StreamReader in TokenMatcher
**Addresses:** L1, L6
**Chosen approach:** Wrap MemoryStream and StreamReader in `using` statements.
**Files:** Modify `src/Tokenizer/TokenMatcher.cs`
**Steps:**
1. In `MatchAsync(TextReader, ...)` overloads (lines 242, 258): wrap the `BufferTextReaderAsync` result in a `using` block or add `await using`/`using` before the `MatchAsyncFromSeekableStream` call
2. In `MatchAsyncFromSeekableStream` (line 356): wrap `new StreamReader(...)` in `using`
3. Verify all tests pass
4. Commit

### Task 17: Remove dead IDisposable check in TokenizationContext
**Addresses:** L2
**Files:** Modify `src/Tokenizer/Tokenization/TokenizationContext.cs`
**Steps:**
1. Remove lines 162-165 (the `if (Enumerator is IDisposable)` block) from the `Dispose` method
2. Verify all tests pass
3. Commit

### Task 18: Remove dead ResultBuilder methods and tests
**Addresses:** L3
**Files:** Modify `src/Tokenizer/Tokenization/IResultBuilder.cs`, `src/Tokenizer/Tokenization/ResultBuilder.cs`, `tests/Tokenizer.Tests/Tokenization/ResultBuilder_Basic_Tests.cs`, `tests/Tokenizer.Tests/Tokenization/ResultBuilder_Error_Tests.cs`
**Steps:**
1. Remove `AddMatchedTokenIds` and `WasLastMatchedToken` from `IResultBuilder` interface
2. Remove the implementations from `ResultBuilder`
3. Remove the 3 tests in `ResultBuilder_Basic_Tests.cs` (lines 100, 117, 133)
4. Remove the 5 argument validation tests in `ResultBuilder_Error_Tests.cs` (lines 73, 85, 97, 109, 120)
5. Verify all tests pass
6. Commit

### Task 19: Add MaxTemplateLength check in CompileAsync read loop
**Addresses:** L4
**Files:** Modify `src/Tokenizer/Tokenizer.cs`, Test `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`
**Steps:**
1. Write a test: set `MaxTemplateLength = 100`, call `CompileAsync` with a `TextReader` producing 200+ chars. Verify `TokenizerException` is thrown with "MaxTemplateLength" in the message
2. Verify test fails
3. Change `ReadToEndAsync` to accept `int maxLength` parameter
4. Inside the read loop, after `sb.Append`, check `if (maxLength > 0 && sb.Length > maxLength)` and throw `TokenizerException` matching the message format in `TokenParser.Parse`
5. Update `CompileAsync` callers to pass `Options.MaxTemplateLength`
6. Verify test passes
7. Run full test suite
8. Commit

### Task 20: Strengthen weak test assertions
**Addresses:** L9, L10
**Files:** Modify `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineTokenMatchingTests.cs`
**Steps:**
1. L10 (line 138): Change `Assert.True(result.Tokens.Matches.Count >= 1)` to `Assert.Equal(3, result.Tokens.Matches.Count)` — or run the test first to see actual count and assert accordingly
2. L9 (line 238): Replace `Assert.NotNull(result)` with meaningful assertions about the consecutive-tokens-without-separator behavior (e.g., assert on match count, verify which token captured what)
3. Verify tests pass
4. Commit

### Task 21: Add diagnostic event for TypeConversionException
**Addresses:** L7
**Files:** Modify `src/Tokenizer/Token.cs`, Test `tests/Tokenizer.Tests/`
**Steps:**
1. Write a test: define a token with a transformer that throws `TypeConversionException`, call `Assign`, verify a diagnostic event is recorded (via the collector)
2. Verify test fails (no event recorded currently)
3. In `Token.Assign`, in the `catch (TypeConversionException)` block (line 249), add `collector.Record(DiagnosticEventType.TokenAssignmentFailed, tokenName: Name, tokenId: Id, value: value)` before `return false`. Check if `TokenAssignmentFailed` exists as a `DiagnosticEventType`; if not, use the most appropriate existing event type or add one
4. Verify test passes
5. Commit

### Task 22: Fix FillBufferAsync test to actually test async
**Addresses:** L8
**Files:** Modify `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorRingBufferTests.cs`
**Steps:**
1. Rewrite `GivenTextReaderEnumerator_WhenFillBufferAsyncCalled_ThenBuffersCharacters` to: construct a `TokenEnumerator` with a short string (e.g., "ab"), consume both chars via `Next()`, verify `IsEmpty` is true. Then note: we can't easily add more data since the reader is exhausted. Alternative: use a `TextReader` wrapper that yields data in small chunks, verify `FillBufferAsync` actually fills the buffer
2. Alternatively: construct with a reader that has more data than DefaultBufferSize. Consume some chars. Call `FillBufferAsync` to refill. Verify new data is available
3. Verify the test actually awaits `FillBufferAsync`
4. Commit

### Task 23: Add inline comment for TemplateCache race-to-add (dismissed M5)
**Addresses:** M5 (dismissed)
**Files:** Modify `src/Tokenizer/Compilation/TemplateCache.cs`
**Steps:**
1. Add inline comment above the `GetOrAdd` method explaining the intentional race-to-add pattern: duplicate compilation is harmless and preferred over Lazy wrapper overhead on every cache hit
2. Commit

### Task 24: Implement continuation handle for Begin/Continue/End protocol
**Addresses:** D4
**Chosen approach:** `BeginTokenization` returns a typed continuation handle that `ContinueTokenization` and `EndTokenization` require as a parameter, enforcing correct call ordering at compile time.
**Files:** Modify `src/Tokenizer/Tokenization/TokenizationEngine.cs`, `src/Tokenizer/Tokenization/ITokenizationEngine.cs`, `src/Tokenizer/Tokenizer.cs`, new `src/Tokenizer/Tokenization/TokenizationContinuation.cs`
**Steps:**
1. Create `TokenizationContinuation` class (or readonly struct) that holds the state currently stored on `TokenizationContext` during the Begin/Continue/End protocol (Template, TargetObject, Result, Collector, HintStrategy, and any loop state)
2. Change `BeginTokenization` to return `TokenizationContinuation` instead of storing state on context
3. Change `ContinueTokenization` to accept `TokenizationContinuation` as parameter
4. Change `EndTokenization` to accept `TokenizationContinuation` as parameter
5. Update `ITokenizationEngine` interface (from Task 8) to reflect the new signatures
6. Update `TokenizeAsyncCore` to capture the continuation from Begin and pass to Continue/End
7. Remove the mutable phase state fields from `TokenizationContext` that are now on the continuation
8. Verify all tests pass
9. Commit

**Note:** Task 24 depends on Task 8 (which adds Begin/Continue/End to the interface).
