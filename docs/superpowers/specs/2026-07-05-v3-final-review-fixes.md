# V3 Final Review Fixes Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to
> implement this plan task-by-task.

**Goal:** Address code review findings from the v3 final branch review

**Source Review:** `docs/superpowers/specs/2026-07-04-v3-final-review.md`
**Design Doc:** Multiple specs in `docs/superpowers/specs/`
**Implementation Plan:** N/A

---

## Dismissed Issues

| ID | Rationale | Action |
|----|-----------|--------|
| H1 | Sync/async paths have legitimate structural differences (rawInput, Begin/Continue/End protocol, cancellation). Extracting helpers would create tangled abstractions. | Add inline comment at `Tokenizer.cs:367` explaining intentional divergence |
| D1 | Same as H1 | None (covered by H1 comment) |
| H7 | Lines 285 and 501: `ToString()` is only used for diagnostics — H6 fix (IsEnabled guard) will gate these. Line 402: `CanAnyAssign` legitimately needs the string. | None (addressed by H6) |
| H8 | Interface is internal with one implementor. Moving methods off gains nothing. | Add inline comment on `ITokenizationEngine.cs` interface documenting intentional spec divergence |
| M3 | YAGNI — only one hint strategy implementation exists. Make injectable when a second strategy is needed. | None |
| M4/D4 | Transformer/validator loops differ in: suffix, IsNot throwing behavior, IsNotValidator assignment. Unification would produce an awkward multi-parameter method. | None |
| M9 | Cache is bounded by maxSize (typically small). O(n) eviction is acceptable. Proper LRU rewrite not worth complexity. | None |
| M10 | MaxInputLength safety limit already bounds buffer growth. | Add inline comment in `TokenEnumerator.GrowBuffer()` noting MaxInputLength provides the bound |
| M11 | Library exceptions should include diagnostic data for developer debugging. Consumer controls logging/serialization. | None |
| M12 | Same rationale as M11. Public `Value` property on `TypeConversionException` is intentional for consumer diagnostics. | None |
| M13 | `rawInput` parameter enables fast string-based hint pre-filtering for sync paths. Intentional improvement over spec. | Add inline comment on `IHintStrategy.PreProcess` documenting rationale |
| M14 | `TokenizationContinuation` handle is a design improvement that enforces correct call ordering at compile time. Already documented in XML docs. | None |
| M15 | Async path can't do early hint rejection — inherent streaming limitation. Compensated by OnTokenMatched callbacks. | Add inline comment in `TokenizeAsyncCore` documenting asymmetry |
| M18 | LRU eviction test already exists at `TemplateCacheTests.cs:70` — reviewer missed it. | None |
| M19 | `TokenizationContinuation` is a data class. Properties tested via integration tests. | None |
| L1 | Entry-point validation runs once per Tokenize call. Already commented as "not in inner loop". | None |
| L3 | `propertyPath.Split('.')` runs during token assignment (bounded count per call), not in hot path. | None |
| L4 | After H6 fix, diagnostic `Record` calls only run when diagnostics enabled (debugging feature). | None |
| L5 | Adding `Enum.IsDefined` after Parse would break combined flags values. .NET convention. | None |
| L6 | Changing from 0 to -1 for "disabled" would be a breaking API change. | None |
| L7 | Already fixed in prior commit (a85ee94). | None |
| L10 | Cache owns its key strategy. Adding CacheKey to Template couples it to caching implementation. | None |

---

## Fix Tasks

### Task 1: Narrow `#if` in ReadToEndAsync (H2)
**Addresses:** H2
**Chosen approach:** Wrap only the `ReadAsync` call in `#if`, share the loop body
**Files:** Modify `src/Tokenizer/Tokenizer.cs`
**Steps:**
1. Write test that verifies `ReadToEndAsync` works correctly with a large input (test already exists at `TokenizerSafetyLimitTests.cs:209`)
2. Verify existing tests pass
3. Refactor `ReadToEndAsync` (lines 302-332) to share loop body, wrapping only `reader.ReadAsync` call in `#if NET8_0_OR_GREATER`
4. Verify tests pass
5. Commit: `refactor: narrow conditional compilation in ReadToEndAsync (H2)`

### Task 2: Add inline comments for dismissed issues
**Addresses:** H1/D1, H8, M10, M13, M15
**Chosen approach:** Add concise inline comments documenting intentional design decisions
**Files:** Modify `src/Tokenizer/Tokenizer.cs`, `src/Tokenizer/Tokenization/ITokenizationEngine.cs`, `src/Tokenizer/Enumerators/TokenEnumerator.cs`, `src/Tokenizer/Tokenization/IHintStrategy.cs`
**Steps:**
1. Add comment at `Tokenizer.cs:367` (before `TokenizeAsyncCore`) explaining sync/async structural divergence
2. Add comment at `ITokenizationEngine.cs:33` explaining Begin/Continue/End on interface is intentional
3. Add comment in `TokenEnumerator.GrowBuffer()` noting MaxInputLength provides the buffer bound
4. Add comment at `IHintStrategy.cs:21` explaining rawInput enables sync-path optimization
5. Add comment at `Tokenizer.cs:394` (async PreProcess call) documenting hint rejection asymmetry
6. Verify build succeeds
7. Commit: `docs: add inline comments for intentional spec divergences`

### Task 3: Remove unsafe downcast, accept concrete TokenizationContext (H3)
**Addresses:** H3
**Chosen approach:** Change `ITokenizationEngine.ProcessTokenization` to accept `TokenizationContext` instead of `ITokenizationContext`
**Files:** Modify `src/Tokenizer/Tokenization/ITokenizationEngine.cs`, `src/Tokenizer/Tokenization/TokenizationEngine.cs`
**Steps:**
1. Write test that exercises `ProcessTokenization` through the interface (verify existing integration tests cover this)
2. Verify tests pass
3. Change `ProcessTokenization` parameter from `ITokenizationContext` to `TokenizationContext` in `ITokenizationEngine.cs`
4. Remove downcast at `TokenizationEngine.cs:65`
5. Verify tests pass
6. Commit: `refactor: accept concrete TokenizationContext in ProcessTokenization (H3)`

### Task 4: Refactor engine private methods to pass context objects (H4/D2)
**Addresses:** H4, D2
**Chosen approach:** Pass `TokenizationContinuation` + `TokenizationContext` instead of destructured parameters
**Files:** Modify `src/Tokenizer/Tokenization/TokenizationEngine.cs`
**Steps:**
1. Verify all tests pass before refactoring
2. Refactor `TryAssignCandidateTokens` to accept `TokenizationContinuation` + `TokenizationContext` instead of 8 separate parameters
3. Verify tests pass
4. Refactor `ProcessRepeatedTokens` similarly
5. Verify tests pass
6. Refactor `ProcessNewlineTerminatedTokens` similarly
7. Verify tests pass
8. Refactor `ProcessFrontMatterTokens` similarly
9. Verify tests pass
10. Update all call sites in handler methods (`HandleTokenSwitch`, `HandleNewlineTerminatedToken`, `HandleRepeatedTokenMatching`, `EndTokenization`)
11. Verify tests pass
12. Commit: `refactor: pass context objects to engine private methods (H4)`

### Task 5: Inline HandleRepeatedTokenMatching wrapper (M5)
**Addresses:** M5
**Chosen approach:** Inline the method — it's a pointless passthrough
**Files:** Modify `src/Tokenizer/Tokenization/TokenizationEngine.cs`
**Steps:**
1. Verify tests pass
2. Inline `HandleRepeatedTokenMatching` at its call site in `ContinueTokenization` (line 173)
3. Delete the `HandleRepeatedTokenMatching` method (lines 569-577)
4. Verify tests pass
5. Commit: `refactor: inline HandleRepeatedTokenMatching wrapper (M5)`

### Task 6: Replace SHA256 cache key with non-crypto hash (H5)
**Addresses:** H5
**Chosen approach:** Use `XxHash64` on .NET 8+, fallback hash on netstandard2.0. No string retention, fast.
**Files:** Modify `src/Tokenizer/Compilation/TemplateCache.cs`
**Steps:**
1. Write test: cache with maxSize=2, add two patterns, verify second lookup returns cached template (existing test covers this)
2. Verify existing cache tests pass
3. Replace `ComputeHash` method: use `System.IO.Hashing.XxHash64` on NET8_0_OR_GREATER, use `FNV-1a` or similar on netstandard2.0
4. Check if `System.IO.Hashing` package is needed for .NET 8 (it's inbox in .NET 8+)
5. Verify all cache tests pass
6. Commit: `perf: replace SHA256 cache key with non-crypto hash (H5)`

### Task 7: Add IsEnabled guard to IDiagnosticCollector (H6)
**Addresses:** H6
**Chosen approach:** Add `IsEnabled` property to `IDiagnosticCollector`, guard LINQ allocations at call sites in hot paths
**Files:** Modify `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`, `src/Tokenizer/Diagnostics/DiagnosticCollector.cs`, `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs`, `src/Tokenizer/Tokenization/TokenizationEngine.cs`
**Steps:**
1. Write test: tokenize with diagnostics disabled, verify no allocation (or verify existing behavior test)
2. Add `bool IsEnabled { get; }` to `IDiagnosticCollector` interface
3. Implement: return `true` in `DiagnosticCollector`, return `false` in `NullDiagnosticCollector`
4. Guard `Record` calls in `TokenizationEngine` hot-path methods with `if (collector.IsEnabled)` — specifically lines 188-191, 287-290, 311-314, 407-410, 437-440, 503-507
5. Verify all tests pass
6. Commit: `perf: add IsEnabled guard to diagnostic collector (H6)`

### Task 8: Delete trivial ServiceConstructorTests (H9)
**Addresses:** H9
**Chosen approach:** Keep context init test, delete the rest
**Files:** Modify `tests/Tokenizer.Tests/Tokenization/Integration/ServiceConstructorTests.cs`
**Steps:**
1. Verify all tests pass
2. Delete all tests except `GivenTokenizationContext_WhenCreated_ThenInitializesCorrectly`
3. Verify tests pass (count should decrease by 14)
4. Commit: `test: remove trivial CLR-verifying ServiceConstructorTests (H9)`

### Task 9: Strengthen safety limit boundary test assertions (H10)
**Addresses:** H10
**Chosen approach:** Add `result.Success` and `Matches.Count` assertions to boundary tests
**Files:** Modify `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`
**Steps:**
1. Read existing test assertions to understand expected match counts
2. Add `Assert.True(result.Success)` and `Assert.True(result.Tokens.Matches.Count >= 1)` to:
   - `GivenInputAtMaxLength_WhenTokenizing_ThenProcessesSuccessfully` (line 40)
   - `GivenMaxInputLengthDisabled_WhenTokenizingLargeInput_ThenProcessesSuccessfully` (line 56)
   - `GivenTemplateAtMaxLength_WhenParsing_ThenProcessesSuccessfully` (line 87)
   - `GivenMaxTemplateLengthDisabled_WhenParsingLargeTemplate_ThenProcessesSuccessfully` (line 103)
   - `GivenTemplateAtMaxTokenCount_WhenParsing_ThenProcessesSuccessfully` (line 146)
3. Verify all tests pass
4. Commit: `test: strengthen safety limit boundary test assertions (H10)`

### Task 10: Extract LogTokenProduced helper in TemplateLexer (M1/D3)
**Addresses:** M1, D3
**Chosen approach:** Extract helper method to eliminate 9x logging duplication
**Files:** Modify `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs`
**Steps:**
1. Verify all tests pass
2. Create `LogTokenProduced(LexerToken token, int absolutePosition, FileLocation location)` private method
3. Replace all 9 occurrences of the logging block (+ fallback variant) with the helper call
4. Verify all tests pass
5. Commit: `refactor: extract LogTokenProduced helper in TemplateLexer (M1)`

### Task 11: Make TokenParser.Options get-only (M2)
**Addresses:** M2
**Chosen approach:** Change `{ get; private set; }` to `{ get; }`
**Files:** Modify `src/Tokenizer/Compilation/TokenParser.cs`
**Steps:**
1. Change line 26 from `public TokenizerOptions Options { get; private set; }` to `public TokenizerOptions Options { get; }`
2. Verify tests pass
3. Commit: `refactor: make TokenParser.Options init-only (M2)`

### Task 12: Fix LINQ indexing in TokenizationEngine (M6/M7)
**Addresses:** M6, M7
**Chosen approach:** Use index access with local variables
**Files:** Modify `src/Tokenizer/Tokenization/TokenizationEngine.cs`
**Steps:**
1. Verify all tests pass
2. In `ProcessNewlineTerminatedTokens`: replace 6x `candidates.Tokens.First()` with `var firstToken = candidates.Tokens[0];` local variable
3. In `WasLastMatchedToken`: replace `result.Tokens.Matches.LastOrDefault()` with index-based access `result.Tokens.Matches[result.Tokens.Matches.Count - 1]` with count check
4. Verify all tests pass
5. Commit: `perf: replace LINQ First()/LastOrDefault() with index access (M6/M7)`

### Task 13: Fix LINQ Any() in TokenResult (M8)
**Addresses:** M8
**Chosen approach:** Use `Count > 0` and `List.Exists()`
**Files:** Modify `src/Tokenizer/TokenResult.cs`
**Steps:**
1. Verify all tests pass
2. Change `HasMatches` from `Matches.Any()` to `_matches.Count > 0`
3. Change `HasMissingRequiredTokens` from `Misses.Any(m => m.IsRequired)` to `_misses.Exists(m => m.IsRequired)`
4. Verify all tests pass
5. Commit: `perf: replace LINQ Any() with Count/Exists in TokenResult (M8)`

### Task 14: Add missing async test coverage (M16/M17)
**Addresses:** M16, M17
**Chosen approach:** Add missing test methods
**Files:** Modify `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs`, `tests/Tokenizer.Tests/CompileAsyncTests.cs`
**Steps:**
1. Read existing test files to understand patterns and available overloads
2. Write test for `MatchAsync(Stream, Encoding, tags)` — tag filtering for streams
3. Write test for `CompileAsync(Stream, Encoding, name)` — verifying template name
4. Verify all tests pass
5. Commit: `test: add missing async stream test coverage (M16/M17)`

### Task 15: Remove default Enumerator allocation (L2)
**Addresses:** L2
**Chosen approach:** Use `null!` initializer to avoid wasted allocation
**Files:** Modify `src/Tokenizer/Tokenization/TokenizationContext.cs`
**Steps:**
1. Change line 24 from `public TokenEnumerator Enumerator { get; private set; } = new TokenEnumerator(string.Empty);` to `public TokenEnumerator Enumerator { get; private set; } = null!;`
2. Verify all tests pass
3. Commit: `refactor: remove wasted default Enumerator allocation (L2)`

### Task 16: Rename DependencyInjectionTests to Gherkin convention (L8)
**Addresses:** L8
**Chosen approach:** Rename test methods to `GivenX_WhenY_ThenZ`
**Files:** Modify `tests/Tokenizer.Tests/Integration/DependencyInjectionTests.cs`
**Steps:**
1. Rename all test methods to follow Gherkin naming convention
2. Verify all tests pass
3. Commit: `test: rename DependencyInjectionTests to Gherkin convention (L8)`

### Task 17: Add A/A/A comments to CompileAsyncTests (L9)
**Addresses:** L9
**Chosen approach:** Add Arrange/Act/Assert section comments
**Files:** Modify `tests/Tokenizer.Tests/CompileAsyncTests.cs`
**Steps:**
1. Add `// Arrange`, `// Act`, `// Assert` comments to all test methods
2. Verify all tests pass
3. Commit: `test: add Arrange/Act/Assert comments to CompileAsyncTests (L9)`
