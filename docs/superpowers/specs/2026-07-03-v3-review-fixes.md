# V3 Code Review Fixes Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to
> implement this plan task-by-task.

**Goal:** Address code review findings from the v3 branch review

**Source Review:** docs/superpowers/specs/2026-07-03-v3-review.md
**Design Doc:** N/A (v3 rewrite spans multiple design docs)
**Implementation Plan:** N/A

---

## Dismissed Issues

| ID | Rationale | Action |
|----|-----------|--------|
| M7+L6 | `NET6_0_OR_GREATER` guard is correct for the APIs used (`SHA256.HashData`, `Convert.ToHexString`). Other files use `NET8_0_OR_GREATER` for different APIs. Not a bug, just different guards for different APIs. | Add inline comment at `TemplateCache.cs:81` |
| M10 | `GetProperties()` is cold-path entry validation (once per Tokenize call), not inner-loop. Performance impact negligible. | Add inline comment at `TokenizationEngine.cs:75` |
| L9 | `StringBuilder.ToString()` materialization is necessary — the string is used for logging and `CanAnyAssign`. | None |
| H5 | `ITokenValidator`/`ITokenTransformer` interfaces are inherently stateless (`IsValid`/`TryTransform` take input via params, return output). Caching is an intentional optimization. | Add inline comment at `TokenDecoratorContext.cs:13` |
| H10 | `UnmatchedInputHintGenerator` is a deliberate stub — the `UnmatchedInputSection` issue type is never raised either. Full feature (gap analysis + hint generation) is future work. | None |
| H12 | `IHintStrategy` is internal with two internal implementations. Not a user-facing extensibility point. | None |
| M3 | `TemplateBinder` re-reads 2 boolean front matter keys from the AST. Minimal duplication, code works correctly. | None |
| L5 | Property paths come from template token names (developer-authored), not user input. By design. | None |
| M2 | Already fixed — newlines preserved in quoted strings | None |
| H13 | Already fixed — `TokensExcluding` accepts caller-supplied buffers | None |
| D5 | Already fixed — single `ParseBoolean` helper | None |

## Deferred Issues

| ID | Rationale |
|----|-----------|
| D2 | Verbose logging duplicates diagnostics events — Effort M, architectural refactor, defer to separate task |
| D4 | `ITokenizationEngine` interface too wide — defer alongside D2 as part of engine internal cleanup |

## Future Work

| ID | Description |
|----|-------------|
| H10 | Implement `UnmatchedInputHintGenerator` gap analysis: detect large unmatched input sections, check for section headers the template doesn't account for, suggest null tokens |

---

## Fix Tasks

### Task 1: Extract shared Tokenize method
**Addresses:** D1, M13, H7, M12, L11
**Chosen approach:** Extract `TokenizeCore(result, value, template, TextReader reader, string? rawInput)` — the `rawInput` parameter (null for TextReader) drives behavioral differences
**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`
- Test: existing tests must continue to pass
**Steps:**
1. Write a test that tokenizes via TextReader with a template that has `MaxInputLength` set, and input exceeding the limit — verify it currently does NOT throw (confirming H7)
2. Run test, verify it passes (confirming the bug exists)
3. Extract shared `TokenizeCore` method from the two private `Tokenize` overloads, parameterizing differences via `string? rawInput`
4. For the string path: read input to determine length, enforce `MaxInputLength`, pass `rawInput` for diagnostics/hints/alignment
5. For the TextReader path: pass `null` as `rawInput`, skip length-dependent features
6. Add `IsEnabled(LogLevel.Information)` guard around `result.Diagnostics.Summary.Verdict` log call (M12)
7. Pass template pattern content to `DiagnosticCollector` instead of null (L11)
8. Run test from step 1 — verify it now throws for the string-based path's MaxInputLength check. For TextReader, decide: enforce by reading to string first, or document as unsupported
9. Run full test suite, verify all pass
10. Commit

### Task 2: Add lock around TemplateCache eviction
**Addresses:** H3, L4, D3
**Chosen approach:** Add a `lock` to `EvictIfOverCapacity` so only one thread evicts at a time. Keep O(N) scan — acceptable for small caches.
**Files:**
- Modify: `src/Tokenizer/Compilation/TemplateCache.cs`
- Test: `tests/Tokenizer.Tests/Compilation/TemplateCacheTests.cs` (if exists, otherwise create)
**Steps:**
1. Write a test that adds entries beyond `maxSize` from multiple threads concurrently and verifies the cache never exceeds `maxSize + 1` (allowing for race tolerance)
2. Run test, verify it may fail or show race behavior
3. Add a private `object _evictionLock = new()` field
4. Wrap the `while` loop body in `EvictIfOverCapacity` with `lock (_evictionLock)`
5. Run test, verify it passes
6. Run full test suite, verify all pass
7. Commit

### Task 3: Add inline comment for NET6_0_OR_GREATER guard
**Addresses:** M7, L6 (dismissed)
**Files:**
- Modify: `src/Tokenizer/Compilation/TemplateCache.cs`
**Steps:**
1. Add inline comment above the `#if NET6_0_OR_GREATER` at line 81 explaining: SHA256.HashData and Convert.ToHexString require .NET 6+; other files use NET8_0_OR_GREATER for different APIs (e.g. SearchValues)
2. Commit

### Task 4: Add regex timeout to MatchesRegexValidator and RegexReplaceTransformer
**Addresses:** H1, H2
**Files:**
- Modify: `src/Tokenizer/Validators/MatchesRegexValidator.cs`
- Modify: `src/Tokenizer/Transformers/RegexReplaceTransformer.cs`
- Test: `tests/Tokenizer.Tests/Validators/MatchesRegexValidatorTests.cs`
- Test: `tests/Tokenizer.Tests/Transformers/RegexReplaceTransformerTests.cs`
**Steps:**
1. Write a test for `MatchesRegexValidator` that passes a catastrophic backtracking pattern (e.g., `(a+)+$` against `"aaaaaaaaaaaaaaaaaaaaaaaaaaaaab"`) and verifies it throws `RegexMatchTimeoutException` (or completes within a reasonable time)
2. Run test, verify it hangs or takes excessively long (confirming the bug)
3. Change `Regex.IsMatch(valueString, args[0])` to use `Regex.IsMatch(valueString, args[0], RegexOptions.None, TimeSpan.FromSeconds(1))`
4. Run test, verify it now throws `RegexMatchTimeoutException`
5. Write equivalent test for `RegexReplaceTransformer`
6. Run test, verify it hangs (confirming the bug)
7. Change `Regex.Replace(valueString, args[0], args[1])` to use `Regex.Replace(valueString, args[0], args[1], RegexOptions.None, TimeSpan.FromSeconds(1))`
8. Run test, verify it throws `RegexMatchTimeoutException`
9. Run full test suite, verify all pass
10. Commit

### Task 5: Delete dead logging from Token.cs
**Addresses:** H6, L10
**Files:**
- Modify: `src/Tokenizer/Token.cs`
**Steps:**
1. Remove the `NullLogger` field (line 16) and the `using` directives for `Microsoft.Extensions.Logging` and `Microsoft.Extensions.Logging.Abstractions`
2. Delete all `Log.LogTrace(...)` calls throughout the file (approximately 10 occurrences)
3. Verify no other references to `Log` remain in the file
4. Run full test suite, verify all pass
5. Commit

### Task 6: Delete empty stub block in TokenizationEngine
**Addresses:** C1
**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
**Steps:**
1. Delete the empty block and comment at lines 186-189 (`// Check candidates hasn't changed { }`)
2. Delete any surrounding stale blank lines
3. Run full test suite, verify all pass
4. Commit

### Task 7: Add inline comment for cold-path GetProperties
**Addresses:** M10 (dismissed)
**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
**Steps:**
1. Add inline comment above the `GetProperties()` call at line 75 explaining: this is entry-point validation (once per Tokenize call), not inner-loop; caching not warranted
2. Commit

### Task 8: Change exception log level from Trace to Warning
**Addresses:** M11
**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
**Steps:**
1. Change `log.LogTrace(e, "Error Assigning Value: {Message}", e.Message)` at line 364 to `log.LogWarning(e, "Error Assigning Value: {Message}", e.Message)`
2. Update the `IsEnabled` guard from `LogLevel.Trace` to `LogLevel.Warning`
3. Run full test suite, verify all pass
4. Commit

### Task 9: Add leaveOpen to TemplateLexer StreamReader
**Addresses:** H4
**Files:**
- Modify: `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs`
- Test: `tests/Tokenizer.Tests/Compilation/Lexer/TemplateLexerTests.cs`
**Steps:**
1. Write a test that passes a `MemoryStream` to `Tokenize(Stream)`, then verifies the stream is still readable after tokenization completes (i.e., not disposed)
2. Run test, verify it fails (stream disposed)
3. Change `new StreamReader(input)` in `Tokenize(Stream)` (line 215) to `new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true)`
4. Run test, verify it passes
5. Run full test suite, verify all pass
6. Commit

### Task 10: Delete TokenizeAsync methods
**Addresses:** L3
**Files:**
- Modify: `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs`
- Modify: `tests/Tokenizer.Tests/Compilation/Lexer/TemplateLexerTests.cs`
**Steps:**
1. Delete all three `TokenizeAsync` methods from `TemplateLexer.cs` (string, Stream, TextReader overloads)
2. Remove any unused `using` directives that result from the deletion (e.g., `System.Runtime.CompilerServices` for `EnumeratorCancellation`)
3. Delete the test `GivenAsyncEnumeration_WhenCanceled_ThenThrowsOperationCanceled`
4. Run full test suite, verify all pass
5. Commit

### Task 11: Remove stale blank lines in TokenParser.GenerateTemplateName
**Addresses:** M4
**Files:**
- Modify: `src/Tokenizer/Compilation/TokenParser.cs`
**Steps:**
1. Remove the consecutive blank lines at lines 574-575 in `GenerateTemplateName`
2. Run full test suite, verify all pass
3. Commit

### Task 12: Truncate template content in error logs
**Addresses:** M6
**Files:**
- Modify: `src/Tokenizer/Compilation/TokenParser.cs`
**Steps:**
1. In the two `log.LogError` calls (lines 327-328 and 333-334), truncate `content` to 200 characters, e.g., `content.Length > 200 ? content.Substring(0, 200) + "..." : content`
2. Run full test suite, verify all pass
3. Commit

### Task 13: Clean up ObjectExtensions.cs
**Addresses:** M5, L2
**Files:**
- Modify: `src/Tokenizer/Extensions/ObjectExtensions.cs`
**Steps:**
1. Delete the `System.Diagnostics.Debug.WriteLine` block at lines 65-68 (M5)
2. Fix both "Could find property" messages to "Could not find property" at lines 187 and 289 (L2)
3. Run full test suite, verify all pass
4. Commit

### Task 14: Add inline comment for decorator cache thread-safety
**Addresses:** H5 (dismissed)
**Files:**
- Modify: `src/Tokenizer/TokenDecoratorContext.cs`
**Steps:**
1. Add inline comment above the `DecoratorCache` field (line 13) explaining: decorators are cached by type because ITokenTransformer/ITokenValidator interfaces are stateless (input via params, output via return). User-registered decorators must be stateless and thread-safe.
2. Commit

### Task 15: Fix Assert.True(true) and weak assertions in engine tests
**Addresses:** H8
**Files:**
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs`
**Steps:**
1. In `GivenNewlineTerminatedTokens_WhenProcessingNewlineTerminatedTokens_ThenHandlesCorrectly` (line 72): replace `Assert.True(true)` with assertions on `result` state (e.g., check `result.Tokens.Matches.Count` or `result.Success`)
2. In `GivenFrontMatterTokens_WhenProcessingFrontMatterTokens_ThenProcessesCorrectly` (line 95): replace `Assert.NotNull(result)` with meaningful assertions on result state
3. In `GivenTemplateWithOnlyFrontMatterTokens_WhenProcessingFrontMatterTokens_ThenProcessesCorrectly` (line 174): same — replace with meaningful assertions
4. Run full test suite, verify all pass
5. Commit

### Task 16: Comprehensive CandidateTokenList tests and fix Clear()
**Addresses:** H9, M1
**Files:**
- Modify: `src/Tokenizer/CandidateTokenList.cs`
- Modify: `tests/Tokenizer.Tests/CandidateTokenListTests.cs`
**Steps:**
1. Write comprehensive tests for CandidateTokenList covering:
   - `Add`: sets Preamble, TerminateOnNewLine, IsNullToken from first token; subsequent tokens don't override
   - `AddRange`: adds multiple tokens correctly
   - `Clear`: resets Preamble, clears tokens, and verify TerminateOnNewLine and IsNullToken are reset (this will initially fail due to M1)
   - `TryAssign`: returns true when a candidate token can accept the value; assigns correct token and value
   - `TryAssign`: returns false when no candidates match
   - `CanAnyAssign`: returns true when at least one token can accept the value
   - `CanAnyAssign`: returns false when no tokens can accept the value
   - `HasCandidates`: returns true when tokens exist, false when empty
   - `Remove`: removes specified token from candidates
   - `Count`: returns correct count after Add/Remove/Clear
   - `IsNullToken`: true when first token has blank name, false otherwise
   - `TerminateOnNewLine`: reflects first token's setting
   - Edge cases: empty list behavior for TryAssign and CanAnyAssign
2. Run tests, verify Clear test fails (M1 bug confirmed)
3. Fix `Clear()` in `CandidateTokenList.cs`: add `TerminateOnNewLine = false;` and `IsNullToken = false;`
4. Run tests, verify all pass
5. Run full test suite, verify all pass
6. Commit

### Task 17: Delete BCL tests from DecoratorDefinitionTests
**Addresses:** H11
**Files:**
- Modify: `tests/Tokenizer.Tests/Compilation/Definitions/DecoratorDefinitionTests.cs`
**Steps:**
1. Delete the 13 tests that test `List<string>` BCL behavior (everything from `GivenDecoratorDefinition_WhenAddingArgs_ThenArgsAreAdded` onward, lines 46-267)
2. Keep the 3 app-specific tests: default values, AppendName, IsNotDecorator
3. Run full test suite, verify all pass
4. Commit

### Task 18: Add first-char fast path to TokenEnumerator.TryMatch
**Addresses:** H14
**Files:**
- Modify: `src/Tokenizer/Enumerators/TokenEnumerator.cs`
- Test: `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorTests.cs`
**Steps:**
1. Write a test that verifies `TryMatch` returns false quickly when the first character doesn't match (can verify via correctness — existing tests should cover this)
2. In `TryMatch(string value)`, before `EnsurePushback(value.Length)`, add: `if (Peek() != value[0]) return false;`
3. Run full test suite, verify all pass
4. Commit

### Task 19: Change GetTokenIdsUpTo to accept HashSet parameter
**Addresses:** M9
**Files:**
- Modify: `src/Tokenizer/Template.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- Modify: `src/Tokenizer/Tokenization/ResultBuilder.cs`
**Steps:**
1. Change `GetTokenIdsUpTo` signature from `IEnumerable<int> GetTokenIdsUpTo(Token token)` to `void GetTokenIdsUpTo(Token token, HashSet<int> matchIds)` — add directly to the set instead of returning a list
2. Update caller in `TokenizationEngine.cs` (`AddMatchedTokenIds`, line 652): remove the foreach loop, just call `template.GetTokenIdsUpTo(matchedToken, matchIds)`
3. Update caller in `ResultBuilder.cs` (line 195): same pattern
4. Run full test suite, verify all pass
5. Commit

### Task 20: Rename snake_case test classes to PascalCase
**Addresses:** M14
**Files:**
- Rename: `HintProcessor_Basic_Tests` → `HintProcessorBasicTests`
- Rename: `HintProcessor_Error_Tests` → `HintProcessorErrorTests`
- Rename: `HintProcessor_EdgeCase_Tests` → `HintProcessorEdgeCaseTests`
- Rename the files accordingly
**Steps:**
1. Rename the class names and file names for all three test classes
2. Run full test suite, verify all pass
3. Commit

### Task 21: Delete dead helper methods in TokenizationEngineBasicTests
**Addresses:** M15
**Files:**
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineBasicTests.cs`
**Steps:**
1. Delete `CreateTemplate`, `CreateContext`, and `CreateResult` private helper methods
2. Remove any unused `using` directives
3. Run full test suite, verify all pass
4. Commit

### Task 22: Build HashSet in ResultBuilder.BuildUnmatchedTokens
**Addresses:** L7
**Files:**
- Modify: `src/Tokenizer/Tokenization/ResultBuilder.cs`
**Steps:**
1. Before the foreach loop at line 145, build a `HashSet<int>` from `result.Tokens.Matches.Select(m => m.Token.Id)`
2. Replace the `result.Tokens.Matches.Any(m => m.Token.Id == token.Id)` check with `matchedIds.Contains(token.Id)`
3. Run full test suite, verify all pass
4. Commit

### Task 23: Delete unused ToMd5 method
**Addresses:** L1, L8
**Files:**
- Modify: `src/Tokenizer/Extensions/StringExtensions.cs`
**Steps:**
1. Delete the `ToMd5` extension method entirely
2. Remove any unused `using` directives (e.g., `System.Security.Cryptography` if no longer needed)
3. Run full test suite, verify all pass (no callers exist)
4. Commit

### Task 24: Delete unused TokenBuilder.WithLocation(int, int)
**Addresses:** L12
**Files:**
- Modify: `tests/Tokenizer.Tests/Builders/TokenBuilder.cs`
**Steps:**
1. Delete the `WithLocation(int line, int column)` overload (lines 42-46)
2. Run full test suite, verify all pass
3. Commit

### Task 25: Delete no-op TemplateBuilder methods
**Addresses:** L13
**Files:**
- Modify: `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs`
**Steps:**
1. Delete `WithGlobalTransformers` and `WithGlobalValidators` methods
2. Run full test suite, verify all pass
3. Commit
