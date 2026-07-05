# Code Review Report

## Review Metadata

- **Branch:** v3
- **Base:** master
- **Work Item:** N/A
- **Change set:** branch diff
- **Files changed:** 554
- **Lines:** +75,034 / -11,928
- **Design docs:** `docs/superpowers/plans/` (16 plans), `docs/superpowers/specs/` (16 specs)
- **Reviewed:** 2026-07-05

---

## Merge Recommendation

**Verdict:** APPROVE WITH CONDITIONS

**Rationale:** 4 Critical code quality bugs (inverted logic, hash weakness, TerminateOnNewLine off-by-one, iteration counter overflow) and 2 Critical security issues (unbounded regex cache, ReDoS via RegexReplaceTransformer) should be fixed before merge. The overall architecture is sound, tests are comprehensive (1233 passing), and the modernization goals are substantially achieved.

---

## Summary of Changes

The v3 branch is a comprehensive modernization of the Tokenizer library spanning 200+ commits. Key changes include: a new multi-stage compilation pipeline (Lexer → Parser → AST → Binder), a diagnostics subsystem with alignment rendering and hint generators, async streaming tokenization with ring-buffered I/O, safety limits for DoS protection, record-based immutability for options and value types, 20+ new transformers/validators, performance optimizations (Span-based matching, reflection caching, allocation reduction), and dual-targeting netstandard2.0/net8.0/net10.0 with conditional compilation.

---

## Strengths & Weaknesses

### Strengths

- `src/Tokenizer/Tokenization/TokenizationEngine.cs:78-241` — Cooperative async protocol (Begin/Continue/End) enables streaming without duplicating the core algorithm
- `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs` — Clean compiler architecture with proper phase separation (lexer → parser → AST → binder → definitions)
- `src/Tokenizer/TokenizerOptions.cs:87-106` — Production hardening via configurable safety limits with sensible defaults and auto-derived fallbacks
- `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` — Zero-cost diagnostics pattern with IsEnabled guard + NullDiagnosticCollector
- `src/Tokenizer/Enumerators/TokenEnumerator.cs` — Multi-target conditional compilation with Span-based optimizations on modern runtimes and netstandard2.0 fallback

### Weaknesses

- `src/Tokenizer/Tokenizer.cs:139-268,368-481` — Significant duplication between sync and async paths (~80% identical code)
- `src/Tokenizer/Compilation/TemplateCache.cs:94-106` — Non-cryptographic hash without collision verification enables silent wrong results
- `src/Tokenizer/TokenizerOptions.cs:13-14` — Record class with mutable `List<Type>` fields creates unclear immutability contract
- `src/Tokenizer/Compilation/Definitions/DecoratorDefinition.cs:24` — StringBuilder.ToString() on every property access creates allocation pressure during compilation

---

## Security Review

**Security Posture:** MEDIUM RISK

Two Critical findings (unbounded regex cache enabling memory exhaustion DoS, and attacker-controlled regex in RegexReplaceTransformer enabling 1-second CPU burns). Three High findings around TemplateCache hash collision enabling cache poisoning, unbounded recursive property traversal via dot-paths, and unbounded memory buffering in BufferTextReaderAsync. The library has good safety limits (MaxInputLength, MaxIterations, MaxTemplateLength, MaxTokenCount) but gaps remain in regex handling and cache integrity.

---

## Multi-Tenant Isolation Review

**Isolation Verdict:** N/A

N/A — system is a standalone text-parsing library with no persistence layer, HTTP pipeline, or tenancy concept.

---

## Performance Impact

**Volume Assumptions:** Library-scale: templates with dozens of tokens, input text KB-MB, hot paths are character-by-character matching. [From code context and CLAUDE.md]

**Performance Impact:** MEDIUM IMPACT

Significant allocation-reduction work has been done (Span-based matching, reflection caching, LINQ elimination in many paths), but several hot-path allocations remain: StringBuilder.ToString() on every property access in DecoratorDefinition/TokenDefinition, LINQ in diagnostic string building, double-iteration in TokenizeResult.First/FirstOrDefault, and Template.Tokens creating a new ReadOnlyCollection wrapper per access.

---

## Database Review

**Database Verdict:** N/A

**Target Database(s):** N/A

N/A — no database changes detected.

---

## Observability Review

**Observability Verdict:** PARTIALLY OBSERVABLE

Good structured logging with IsEnabled guards throughout, and a comprehensive diagnostics subsystem. However: DiagnosticCollector always receives null for template content (rendering alignment incomplete), IntegratedHintStrategy doesn't record diagnostic events for hint misses (async path blind spot), PreambleNearMissHintGenerator is effectively dead code (always short-circuits), and several compilation pipeline stages (TemplateParser, FrontMatterBinder, TemplateBinder) have no logging at all.

---

## Hiring Recommendation

**Recommended Level:** Senior

**Justification:**

- `src/Tokenizer/Tokenization/TokenizationEngine.cs:78-241` — Cooperative async protocol demonstrates state machine sophistication beyond mid-level
- `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs` — Textbook compiler pipeline with proper phase separation
- `src/Tokenizer/TokenizerOptions.cs:87-106` — Production hardening with auto-derived iteration limits shows operational awareness
- `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` — Zero-cost diagnostics pattern with consistent IsEnabled guard usage
- `src/Tokenizer/Enumerators/TokenEnumerator.cs` — Multi-target conditional compilation done correctly with Span-based optimizations

**Gaps preventing Staff:** Sync/async code duplication (~80% identical), mutable state in record class, no formal decorator lifecycle management, compilation pipeline orchestrator (TokenParser) is 500+ lines with hard-coded registrations, Token.Assign violates SRP (70+ lines mixing preparation/validation/assignment), TemplateCache LRU eviction is O(N).

---

## Delta to Staff-Level

**D1:** `src/Tokenizer/Tokenizer.cs:139-268,368-481` — Sync/async paths share ~80% identical code. Staff-level: extract shared orchestration into template method, keeping only I/O strategy as variance point. Effort: M

**D2:** `src/Tokenizer/TokenizerOptions.cs:13-14` — Record class with mutable List<Type> fields and Equals that excludes them. Staff-level: genuine immutability via builder pattern or ImmutableList. Effort: M

**D3:** `src/Tokenizer/TokenDecoratorContext.cs:14` — Decorator cache scoped to TokenParser instance with implicit stateless/thread-safe contract. Staff-level: formalize with DecoratorRegistry abstraction with explicit lifecycle. Effort: M

**D4:** `src/Tokenizer/Compilation/TokenParser.cs` — 500+ line class registering 40+ types by name. Staff-level: dictionary-based lookup by normalized name, or assembly-scanning discovery. Effort: L

**D5:** `src/Tokenizer/Token.cs:132-203` — Assign() handles value prep, trimming, decorators, dictionary assignment, reflection, concatenation, and 3 exception types. Staff-level: decompose into preparation, validation, and assignment strategy. Effort: M

**D6:** `src/Tokenizer/Compilation/TemplateCache.cs:60-87` — O(N) LRU eviction via full dictionary scan. Staff-level: linked-list-based LRU for O(1) eviction. Effort: S

**D7:** `src/Tokenizer/Tokenization/IHintStrategy.cs:24-25` — PreProcess takes both TokenEnumerator and nullable rawInput, but strategies disagree on nullability (ContainsHintStrategy throws, IntegratedHintStrategy ignores). Staff-level: split interfaces or use discriminated input type. Effort: S

---

## Issues

| ID | Severity | Reviewer | File:Line | Issue | Fix |
|----|----------|----------|-----------|-------|-----|
| C1 | C | Code Quality | `Compilation/Binders/TemplateBinder.cs:558` | `GetRepeatingMultilinePreamble` logic inverted — returns newline+whitespace when intent is to return the tail serving as repeating preamble | Re-examine condition: should check `!IsNullOrWhiteSpace(post)` or invert the branch |
| C2 | C | Code Quality | `Compilation/TemplateCache.cs:94-106` | FNV-1a hash iterates over `char` values (16-bit) not bytes, producing weaker distribution and more collisions | Hash over `Encoding.UTF8.GetBytes(input)` or iterate each byte of each char |
| C3 | C | Code Quality | `TokenizationEngine.cs:153` | `IterationCount` is `int` but compared to `CharactersConsumed * 2 + 100` (long). If MaxInputLength is disabled (0), IterationCount can overflow | Make `IterationCount` a `long` |
| C4 | C | Code Quality | `Token.cs:195` | `PrepareValue` checks `index > 0` for TerminateOnNewLine truncation, but `index >= 0` is correct — value starting with `\n` is not truncated | Change `index > 0` to `index >= 0` |
| C5 | C | Security | `Validators/MatchesRegexValidator.cs:11` | Static `ConcurrentDictionary<string, Regex>` with no size limit — attacker-controlled template patterns grow cache unboundedly | Add LRU eviction or max-size check to the regex cache |
| C6 | C | Security | `Transformers/RegexReplaceTransformer.cs:24` | Attacker-controlled regex pattern with 1s timeout per call enables CPU-based DoS at scale | Cache compiled regex (like MatchesRegexValidator), consider shorter timeout |
| C7 | C | Performance | `Compilation/Definitions/DecoratorDefinition.cs:24` | `Name => name.ToString()` allocates new string on every property access; called 40+ times per decorator during lookup | Cache the string after building is complete |
| C8 | C | Performance | `Compilation/Definitions/TokenDefinition.cs:33-42` | `Preamble`/`Name`/`Value` properties allocate via `StringBuilder.ToString()` per access; accessed 4+ times each during compilation | Cache results after building is complete |
| C9 | C | Performance | `TokenizationEngine.cs:348` | `context.Replacement.ToString()` allocates a new string on every call in main tokenization loop | Cache the string value, invalidate on append |
| C10 | C | Observability | `Tokenizer.cs:139-268` | Sync `TokenizeCore` has no try/catch — unexpected exceptions propagate with no log entry or diagnostic context | Add exception handling matching async path pattern |
| C11 | C | Test Coverage | `Exceptions/TokenAssignmentException.cs` | Zero test coverage for `TokenAssignmentException` constructors | Add constructor and integration tests |
| C12 | C | Test Coverage | `Exceptions/TokenMatcherException.cs` | Zero test coverage for `TokenMatcherException` constructors | Add constructor tests |
| C13 | C | Test Coverage | `Exceptions/TypeConversionException.cs` | Zero test coverage for `TypeConversionException` constructors | Add constructor and integration tests |
| H1 | H | Security | `Compilation/TemplateCache.cs:36-52` | Non-crypto hash as sole cache key — collision means wrong compiled template returned silently | Store original pattern string in CacheEntry and verify on hit |
| H2 | H | Security | `Extensions/ObjectExtensions.cs:44-48` | Unbounded recursive property traversal via dot-path token names; each null intermediate triggers Activator.CreateInstance | Add max depth limit (e.g., 10 levels) |
| H3 | H | Security | `TokenMatcher.cs:302-316` | `BufferTextReaderAsync` reads entire TextReader into memory with no size limit | Add MaxInputLength check during buffering |
| H4 | H | Code Quality | `Compilation/Lexer/TemplateLexer.cs:1383` | `TryReadFrontMatter` detects `---` anywhere, not only at line start — mid-line `---` corrupts lexer state | Track line-start position and only match `---` at beginning of line |
| H5 | H | Code Quality | `TokenizationEngine.cs:9942` | `BeginTokenization` validates target has writable properties but doesn't account for `IgnoreMissingProperties` option | Skip validation when `IgnoreMissingProperties` is true |
| H6 | H | Code Quality | `Compilation/Parsing/AstTemplateDefinitionParser.cs:2386` | FrontMatterBinder and TemplateBinder produce separate TemplateDefinitions with potentially inconsistent options | Consolidate option application into single code path |
| H7 | H | Performance | `TokenizeResult.cs:32-37,50-55` | `First`/`FirstOrDefault` call `Any()` then `First()`, iterating the list twice | Use single-pass foreach or dictionary lookup |
| H8 | H | Performance | `Template.cs:79` | `Tokens => tokens.AsReadOnly()` creates new wrapper per access; called in hot paths | Cache the ReadOnlyCollection wrapper |
| H9 | H | Performance | `Compilation/TemplateCache.cs:60-87` | LRU eviction iterates entire ConcurrentDictionary (O(N) per eviction with interlocked reads) | Use linked-list-based LRU for O(1) eviction |
| H10 | H | Performance | `Extensions/ObjectExtensions.cs:44` | `propertyPath.Split('.')` allocates array per call on every token assignment | Cache split results or use Span on .NET 8+ |
| H11 | H | Performance | `TokenDecoratorContext.cs:69,75` | `IsAssignableFrom` reflection call per decorator per token match in hot path | Cache as boolean field at construction |
| H12 | H | Performance | `CandidateTokenList.cs:80` | `value.ToString()` on StringBuilder allocates unconditionally even when no candidate accepts | Defer allocation until candidate accepts |
| H13 | H | Observability | `DiagnosticCollector.cs:18-19` | Constructor always receives null for templateContent — alignment rendering incomplete | Pass actual template content from Tokenizer.cs call sites |
| H14 | H | Observability | `Tokenization/Strategies/IntegratedHintStrategy.cs:37-65` | `PostProcess` records no diagnostic events for hint misses on async path | Add `collector.Record(DiagnosticEventType.HintMissing, ...)` matching ContainsHintStrategy |
| H15 | H | Observability | `TokenizationEngine.cs:296-304` | Exception during token assignment logged at Warning with only `{Message}` — no token name, value, or position | Add token name, candidate list, and input position to log entry |
| H16 | H | Observability | `Token.cs:180-186` | `MissingMemberException` silently swallowed when `IgnoreMissingProperties` is true — no logging or diagnostic | Add Debug-level log or diagnostic event for dropped token values |
| H17 | H | Spec Compliance | `Token.cs:61-122` | Token properties renamed (`Optional`→`IsOptional`, etc.) without plan documentation | Update plan docs or add rationale in design docs |
| H18 | H | Spec Compliance | `TokenMatch.cs:8` | `Match` renamed to `TokenMatch` without plan documentation | Update plan docs |
| H19 | H | Spec Compliance | `Transformers/ITokenTransformer.cs:11` | `CanTransform` renamed to `TryTransform` without plan documentation | Update plan docs |
| H20 | H | Test Coverage | `Diagnostics/Hints/UnmatchedInputHintGenerator.cs` | Only hint generator without test coverage | Add test file verifying null return behavior |
| H21 | H | Test Coverage | `Template.cs:158` | `HasOnlyFrontMatterTokens` has no direct test | Add test creating template with only front-matter tokens |
| H22 | H | Test Coverage | `Exceptions/LexerException.cs` | Inner-exception constructors and Message override untested | Add constructor and Message formatting tests |
| H23 | H | Test Coverage | `Exceptions/ParsingException.cs:46-57` | Message override appending line/column untested | Add Message formatting test |
| H24 | H | Test Coverage | No integration test for `:Once` modifier | `IsSingleUse` runtime behavior in TokenizationEngine untested end-to-end | Add integration test: compile with `:Once`, verify single consumption |
| M1 | M | Code Quality | `Transformers/ToDateTimeTransformer.cs:11893` | `Dictionary<string, string[]>` with double-checked lock is not thread-safe for concurrent reads/writes | Use `ConcurrentDictionary<string, string[]>` |
| M2 | M | Code Quality | `Tokenization/TokenizationContext.cs:136` | `Dispose()` is a no-op — sets flag but doesn't dispose Enumerator or other resources | Remove IDisposable or properly dispose managed resources |
| M3 | M | Code Quality | `Compilation/TokenParser.cs:3876` | `ComputePreamble` is instance method with access to `this.Options` AND an `options` parameter — latent regression risk | Make method static to prevent accidental use of instance Options |
| M4 | M | Code Quality | `Compilation/Parsing/TokenReader.cs:3145` | `Peek(int lookahead)` iterates entire Queue — O(n) per call | Use `List<LexerToken>` for indexed access |
| M5 | M | Code Quality | `Transformers/RegexReplaceTransformer.cs:24` | Regex compiled on every invocation, unlike MatchesRegexValidator which caches | Cache compiled Regex like MatchesRegexValidator |
| M6 | M | Code Quality | `Validators/IsNumericValidator.cs:12804` | `float.TryParse` accepts "Infinity"/"NaN" and has 7-digit precision limit | Use `decimal.TryParse` or `double.TryParse` |
| M7 | M | Code Quality | `CandidateTokenList.cs:144` | `IList<Token> Tokens` exposes internal mutable state | Return `IReadOnlyList<Token>` |
| M8 | M | Security | `Tokenizer.cs:309-329` | MaxLength check after `sb.Append` allows exceeding limit by up to bufferSize-1 | Check before appending or use exact-length reads |
| M9 | M | Security | `Enumerators/TokenEnumerator.cs:342-372` | `GrowBuffer` doubles with no upper bound — long template preambles force excessive growth | Add configurable max buffer size |
| M10 | M | Security | `Extensions/ObjectExtensions.cs:153-168` | `Activator.CreateInstance` on intermediate null properties could trigger constructors with side effects | Document security contract or add type allowlist |
| M11 | M | Spec Compliance | Multiple files | 19 public classes remain unsealed — exception hierarchy is valid, but diagnostics classes, TemplateCollection, TokenEnumerator, FileLocation lack justification | Seal remaining non-extension classes |
| M12 | M | Spec Compliance | `HintResult.cs:55` | `HasMissingRequiredHints` still uses LINQ `Any()` instead of `Exists()` like fixed TokenResult | Change to `_misses.Exists(m => m.IsRequired)` |
| M13 | M | Spec Compliance | `Token.cs:278,289` | `RunDecoratorPipeline` calls `decorator.Parameters.ToArray()` bypassing the cached `GetParameterArray()` | Use cached parameter array |
| M14 | M | Spec Compliance | `TokenizeResultBase.cs:66` | `Success` property uses LINQ `Any()` re-evaluated on every access | Cache result after tokenization completes |
| M15 | M | Spec Compliance | `Template.cs:158` | `HasOnlyFrontMatterTokens` uses LINQ `Where().All()` evaluated on every access via Success | Cache as bool after template construction |
| M16 | M | Spec Compliance | `TokenDecoratorContext.cs:80` | `IsNotValidator` has public setter, inconsistent with v3 immutability pattern | Change to `internal set` |
| M17 | M | Spec Compliance | `Template.cs:59` | `Name` has public setter, inconsistent with init-only pattern applied elsewhere | Change to `init` or `internal set` |
| M18 | M | Performance | `Tokenization/Strategies/IntegratedHintStrategy.cs:50` | `matchedPreambles.Any(p => p.Contains(hint.Text))` is O(H*P*L) | Use more efficient lookup or break early |
| M19 | M | Performance | `Tokenization/ResultBuilder.cs:144` | Creates new HashSet from LINQ per tokenization call | Pass MatchIds from TokenizationContext |
| M20 | M | Performance | `Tokenizer.cs:167-173` | Dictionary allocated for log scope on every call even when logging disabled | Guard with `log.IsEnabled(LogLevel.Debug)` |
| M21 | M | Performance | `Compilation/TokenParser.cs:385-443` | O(T+V) linear scan per decorator (40+ string comparisons per decorator) | Use dictionary lookup by normalized name |
| M22 | M | Performance | `Tokenization/Strategies/ContainsHintStrategy.cs:34` | `rawInput.Contains(hint.Text)` without StringComparison — culture-dependent on netstandard2.0 | Specify `StringComparison.Ordinal` |
| M23 | M | Observability | `Tokenizer.cs:459-473` | Async path doesn't log alignment rendering unlike sync path | Add alignment rendering to async logging |
| M24 | M | Observability | `Compilation/Parsing/TemplateParser.cs` | Entire AST parsing phase has no logging — blind spot between lexer and binder | Add Trace-level logging for parsing decisions |
| M25 | M | Observability | `Compilation/Binders/FrontMatterBinder.cs` | Front matter option binding operates silently — no Debug/Trace for successful bindings | Add Debug-level logging for option binding |
| M26 | M | Observability | `Compilation/Binders/TemplateBinder.cs` | Binder transforms AST to definitions without logging | Add Trace-level logging for binding steps |
| M27 | M | Observability | `HintProcessor.cs:110-111` | Optional hint miss logged at Warning — should be Debug since optional misses are expected | Demote to Debug level |
| M28 | M | Observability | `Diagnostics/Hints/PreambleNearMissHintGenerator.cs:21` | Generator is effectively dead code — `Detail` and `Value` are always null for `TokenMissed` events, so it always short-circuits | Fix event recording to include preamble data, or remove generator |
| M29 | M | Test Coverage | `TokenParser.cs:866-870` | `Concat()` with >1 argument error path untested through full pipeline | Add compilation integration test |
| M30 | M | Test Coverage | `Enumerators/TokenEnumerator.cs:617-624` | `Reset()` on TextReader-backed enumerator `NotSupportedException` untested | Add unit test |
| M31 | M | Test Coverage | `Extensions/ObjectExtensions.cs:291-311` | Enum type conversion via `ChangeEnumType` untested | Add test with enum target property |
| M32 | M | Test Coverage | `Compilation/Binders/FrontMatterBinder.cs:194` | Unknown front matter node type default case untested | Add test with custom SyntaxNode |
| M33 | M | Test Coverage | `Tokenization/Strategies/ContainsHintStrategy.cs:24` | Null `rawInput` ArgumentNullException untested | Add null-input test |
| M34 | M | Test Coverage | `Enumerators/TokenEnumerator.cs:686-694` | Ring buffer wrap-around during grow untested | Add test filling, consuming, then forcing grow |
| L1 | L | Code Quality | `Validators/ContainsValidator.cs:12330` | XML summary says "ends with" — copy-paste error from EndsWithValidator | Fix XML doc |
| L2 | L | Code Quality | `TokenizerOptions.cs:159-178` | Equals/GetHashCode excludes transformer/validator lists — two instances differing only in registrations compare equal, risking TemplateCache collisions | Include registrations in equality or document the exclusion |
| L3 | L | Code Quality | `TokenMatcher.cs:8555-8563` | `CheckTemplateTags` has redundant if/return pattern; `missing` variable unused | Simplify to `return template.HasTags(tags, out _)` |
| L4 | L | Performance | `Extensions/StringExtensions.cs:199` | `IsOnlySpaces` calls `.ToCharArray()` unnecessarily — foreach over string iterates chars without allocation | Remove `.ToCharArray()` |
| L5 | L | Performance | `TemplateCollection.cs:16` | `Names` allocates new array via `Keys.ToArray()` on every access | Cache or iterate `.Values` directly |
| L6 | L | Performance | `TokenParser.cs:166-172,323-327` | Stopwatch only created under Trace guard but logged at Debug — reports 0 when Debug enabled without Trace | Create stopwatch under Debug guard |
| L7 | L | Observability | `Tokenizer.cs:181-196` | Two consecutive `log.IsEnabled(LogLevel.Debug)` guards could be merged | Combine into single guard block |
| L8 | L | Observability | `Tokenization/Strategies/ContainsHintStrategy.cs` | No log entries for hint matches/misses (only diagnostic events) | Add Debug-level logging |
| L9 | L | Observability | `TokenMatcher.cs:380-397` | No log output when templates filtered by tag mismatch | Add Debug-level tag filtering log |
| L10 | L | Observability | `Compilation/Binders/TemplateBinder.cs:575-583` | `IsFrontMatterOptionTrue` silently returns false for unrecognized values, inconsistent with FrontMatterBinder which throws | Log warning for unrecognized boolean values |
| L11 | L | Spec Compliance | `TemplateCollection.cs:9` | Not sealed per plan Task 6 requirement | Seal the class |
| L12 | L | Test Coverage | `Extensions/TokenizerServiceCollectionExtensions.cs:27-35` | IConfiguration overload for DI registration untested | Add test with ConfigurationBuilder |
| L13 | L | Test Coverage | `Compilation/Parsing/TemplateParser.cs:76-81` | Leading whitespace before front matter delimiter untested | Add test with whitespace before `---` |
| L14 | L | Test Coverage | `Diagnostics/AlignmentRenderer.cs:38` | Hint rendering sub-branch (`issue?.Hint != null`) partially untested | Add test with non-null hints |

---

## Recommended Fixes

### Must fix before merge (Critical)

- C1 - Fix inverted logic in `GetRepeatingMultilinePreamble`
- C2 - Fix FNV-1a hash to operate on bytes, not chars
- C3 - Change `IterationCount` from `int` to `long`
- C4 - Change `index > 0` to `index >= 0` in `PrepareValue` TerminateOnNewLine
- C5 - Add max-size eviction to static regex cache in MatchesRegexValidator
- C6 - Cache compiled regex in RegexReplaceTransformer (like MatchesRegexValidator)

### Should fix before merge (High — top priority)

- H1 - Store original pattern in TemplateCache CacheEntry for collision verification
- H2 - Add max depth limit to ObjectExtensions dot-path traversal
- H3 - Add MaxInputLength check to BufferTextReaderAsync
- H4 - Fix `---` detection in TemplateLexer to only match at line start
- C7/C8 - Cache StringBuilder.ToString() results in DecoratorDefinition/TokenDefinition after building
- C9 - Cache Replacement.ToString() in ProcessRepeatedTokens
- C10 - Add exception handling to sync TokenizeCore matching async path
- H7 - Fix double-iteration in TokenizeResult.First/FirstOrDefault
- H8 - Cache ReadOnlyCollection wrapper in Template.Tokens
- H13 - Pass actual template content to DiagnosticCollector
- H14 - Add diagnostic events for hint misses in IntegratedHintStrategy

---

## Reviewer Competition

| Reviewer | Stars |
|----------|-------|
| Code Quality | 28 |
| Spec Compliance | 15 |
| Test Coverage | 18 |
| Security | 14 |
| Multi-Tenant Isolation | 0 |
| Performance | 29 |
| Database | 0 |
| Observability | 21 |
| Hiring Recommendation | 7 |

**Winner: Performance** with 29 stars, leading by 1 over second place (Code Quality, 28 stars).

*Note: Stars counted before deduplication. In the unified table, duplicate findings are attributed to the first reviewer who identified them.*
