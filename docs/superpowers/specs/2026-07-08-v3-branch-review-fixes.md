# V3 Branch Review Fixes Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to
> implement this plan task-by-task.

**Goal:** Address validated code review findings from the 2026-07-07 v3 branch review

**Source Review:** docs/superpowers/specs/2026-07-07-v3-branch-review.md
**Design Doc:** N/A
**Implementation Plan:** N/A

---

## Dismissed Issues

| ID | Rationale | Action |
|----|-----------|--------|
| C1 | Deferred — users can cache compiled templates themselves | None |
| C2 | Intentional — CompilationResult bundles diagnostics, better API than bare Template | None |
| H1 | Already decided — init is consistent with record design | None |
| H2 | Moot — requires mutable setters which conflict with H1 decision | None |
| H3 | Create() and AddTokenizer() cover the use cases | None |
| H4 | Deferred — front matter naming works for now | None |
| H5 | Explicit compile-then-tokenize is better design, avoids hidden recompilation | None |
| H6 | Already accepted — immutable With* API is consistent with record design | None |
| H9 | Already fixed — Token is now pure data, no double wrapping | None |
| D1 | Already fixed — Token refactored to pure data class | None |
| D2 | Already fixed — single catch block now | None |
| D3 | Already fixed — replaced by decomposed PropertyPathSetter | None |
| D4 | Caller owns TextReader, TokenEnumerator borrows — correct ownership | None |
| D5 | Already fixed — unified into RunCoreAsync | None |
| M1 | Intentional reuse pattern — buffers are caller-owned for performance | None |
| M4 | Already fixed — exclusion documented in XML doc | None |
| M5 | Already fixed — ObjectExtensions replaced entirely | None |
| M7 | Intentional — FileLocation is a mutable lexer cursor, not a value object | None |
| M8 | Already fixed — replaced by Upfront/StreamingHintStrategy | None |
| M10 | Negligible allocation after buffer refactor | None |
| M13 | Failures already throw exceptions — logging adds noise | None |
| M14 | Already fixed — InnerException preserves original type | None |
| L1 | Already fixed — method removed, CanConcatenate property now | None |
| L2 | Correct comparison for tag matching use case | None |
| L3 | Consistent with engine's ordinal preamble matching | None |
| L4 | Security hardening deferred as separate exercise | None |
| L5 | Security hardening deferred as separate exercise | None |
| L6 | Security hardening deferred as separate exercise | None |
| L9 | Compilation is one-time cost — YAGNI | None |
| L10 | Compilation isn't hot path — guard overhead not justified | None |
| L12 | Multi-line format is better for developer-facing compilation errors | None |
| L13 | Already fixed — TokenizeResultBase removed | None |
| L14 | Cosmetic — current format is functional | None |
| L15 | Already fixed — session-level tests added | None |
| H11 | Trivial rethrow — indirectly covered by existing tests | None |
| H12 | Already fixed — tested at multiple levels | None |
| H13 | Already fixed — throws on advanceLength == 0 | None |
| H15 | Already fixed — tested in PropertyPathSetter tests | None |
| H16 | Already fixed — both paths tested | None |

---

## Fix Tasks

### Task 1: Rename _parser to _compiler in Tokenizer.cs
**Addresses:** H7
**Chosen approach:** Rename field and all usages
**Files:** Modify `src/Tokenizer/Tokenizer.cs`
**Steps:**
1. Rename `_parser` field to `_compiler`
2. Update all references within the file
3. Build to verify
4. Commit

### Task 2: Remove dead code from ResultBuilder
**Addresses:** H10
**Chosen approach:** Remove unused methods from IResultBuilder and ResultBuilder, remove their dedicated test files
**Files:** Modify `src/Tokenizer/Tokenization/IResultBuilder.cs`, Modify `src/Tokenizer/Tokenization/ResultBuilder.cs`, Remove test files
**Steps:**
1. Identify which methods are called from production code (only BuildUnmatchedTokens)
2. Remove CreateTokenizeResult, AddTokenMatch, AddTokenMiss, AddException from IResultBuilder
3. Remove corresponding implementations from ResultBuilder
4. Remove or update test files that only test removed methods
5. Build and run tests
6. Commit

### Task 3: Add general exception catch with logging in RunCoreAsync
**Addresses:** H17
**Chosen approach:** Add catch (Exception) with error-level log and rethrow
**Files:** Modify `src/Tokenizer/Tokenizer.cs`
**Steps:**
1. Write a test that forces an unexpected exception through RunCoreAsync and verifies it is logged at Error level and rethrown
2. Verify test fails (no general catch exists)
3. Add `catch (Exception ex)` block after existing catches with `_log.LogError(ex, ...)` and `throw;`
4. Verify test passes
5. Run all tests
6. Commit

### Task 4: Fix out-of-order token matching break bug
**Addresses:** M6
**Chosen approach:** Guard break with `!outOfOrderTokens`
**Files:** Modify `src/Tokenizer/Enumerators/TokenEnumerator.cs`, Add test
**Steps:**
1. Write a test: template with out-of-order mode, multiple non-optional tokens, verify all tokens are evaluated
2. Verify test fails (break skips tokens after first non-optional)
3. Change line 285 from `if (!token.IsOptional) break;` to `if (!token.IsOptional && !outOfOrderTokens) break;`
4. Verify test passes
5. Run all tests to check for regressions
6. Commit

### Task 5: Cache reflected decorator types in DecoratorRegistry
**Addresses:** M9
**Chosen approach:** Static Lazy<> field caching discovered built-in types
**Files:** Modify `src/Tokenizer/Compilation/DecoratorRegistry.cs`
**Steps:**
1. Write a test verifying that two DecoratorRegistry instances share the same discovered types (or benchmark test)
2. Extract GetTypes() calls into a `static Lazy<(IReadOnlyList<Type> transformers, IReadOnlyList<Type> validators)>` field
3. Constructor merges cached built-ins with custom registrations from options
4. Build and run tests
5. Commit

### Task 6: Fix double scan in StringExtensions
**Addresses:** M11
**Chosen approach:** Replace Contains + IndexOf with single IndexOf call
**Files:** Modify `src/Tokenizer/Extensions/StringExtensions.cs`
**Steps:**
1. Write tests for SubstringAfterString and related methods if not already tested
2. Replace `Contains()` + `IndexOf()` with single `IndexOf()` + `!= -1` check in all 4 methods
3. Run tests
4. Commit

### Task 7: Add static sentinel TokenEnumerator for hint matching
**Addresses:** M2
**Chosen approach:** Static readonly empty TokenEnumerator reused instead of allocating per hint
**Files:** Modify `src/Tokenizer/Enumerators/TokenEnumerator.cs`, Modify `src/Tokenizer/Tokenization/Strategies/StreamingHintStrategy.cs`
**Steps:**
1. Add `internal static readonly TokenEnumerator Empty = new(string.Empty)` to TokenEnumerator
2. Replace `new TokenEnumerator(string.Empty)` in StreamingHintStrategy.PostProcess with `TokenEnumerator.Empty`
3. Search for other `new TokenEnumerator(string.Empty)` usages and replace
4. Run tests
5. Commit

### Task 8: Make UpfrontHintStrategy a static singleton
**Addresses:** H8
**Chosen approach:** Static Instance property, reference from RunCoreAsync
**Files:** Modify `src/Tokenizer/Tokenization/Strategies/UpfrontHintStrategy.cs`, Modify `src/Tokenizer/Tokenizer.cs`
**Steps:**
1. Add `internal static readonly UpfrontHintStrategy Instance = new();` to UpfrontHintStrategy
2. Replace `new UpfrontHintStrategy()` in RunCoreAsync with `UpfrontHintStrategy.Instance`
3. Run tests
4. Commit

### Task 9: Cache AsReadOnly wrapper on Template.Tokens
**Addresses:** L7
**Chosen approach:** Lazy-initialize cached wrapper, invalidate on AddToken
**Files:** Modify `src/Tokenizer/Template.cs`
**Steps:**
1. Add `private ReadOnlyCollection<Token>? _readOnlyTokens;` field
2. Change `Tokens` property to return `_readOnlyTokens ??= _tokens.AsReadOnly()`
3. Set `_readOnlyTokens = null` in AddToken to invalidate
4. Run tests
5. Commit

### Task 10: Replace LINQ in HasOnlyFrontMatterTokens
**Addresses:** L8
**Chosen approach:** foreach loop
**Files:** Modify `src/Tokenizer/Template.cs`
**Steps:**
1. Replace `_tokens.Where(...).All(...)` with foreach loop that returns false on first non-front-matter named token
2. Run tests
3. Commit

### Task 11: Seal TemplateLexer
**Addresses:** M3
**Chosen approach:** Add sealed modifier
**Files:** Modify `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs`
**Steps:**
1. Change `public class TemplateLexer` to `public sealed class TemplateLexer`
2. Build and run tests
3. Commit

### Task 12: Extract character set constants in TemplateLexer
**Addresses:** D6
**Chosen approach:** Static readonly char arrays referenced by TryRead* methods
**Files:** Modify `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs`
**Steps:**
1. Identify shared character sets across TryReadText, TryReadStructural, TryReadModifier
2. Extract to private static readonly fields (e.g., `StructuralChars`, `ModifierChars`)
3. Update TryRead* methods to reference the constants
4. Build and run tests
5. Commit

### Task 13: Add hint names to warning log messages
**Addresses:** M12
**Chosen approach:** Include missing hint names as structured log properties
**Files:** Modify `src/Tokenizer/Tokenizer.cs`
**Steps:**
1. In RunCoreAsync, where "Required hints are missing" is logged, collect and include the missing hint names
2. In the post-tokenization hint check warning, include which hints failed
3. Run tests (verify no test output changes break)
4. Commit

### Task 14: Add PII remark to DiagnosticCollector
**Addresses:** L11
**Chosen approach:** XML doc remarks on EnableDiagnostics and DiagnosticCollector
**Files:** Modify `src/Tokenizer/Diagnostics/DiagnosticCollector.cs` or `src/Tokenizer/TokenizerOptions.cs`
**Steps:**
1. Add `<remarks>` to EnableDiagnostics property noting diagnostic output may contain input text
2. Build
3. Commit

### Task 15: Add test for exception wrapping in TemplateCompiler
**Addresses:** H14
**Chosen approach:** Test that forces a non-TokenizerException and verifies wrapping
**Files:** Add/modify test in `tests/Tokenizer.Tests/`
**Steps:**
1. Write test that triggers a non-TokenizerException during compilation (e.g., via a custom decorator that throws ArgumentException)
2. Verify the exception is wrapped in TokenizerException with original as InnerException
3. Run all tests
4. Commit
