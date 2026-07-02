# Tokenizer v3 Roadmap

Prioritized work remaining to bring the library to gold-standard .NET quality.
Each tier is an independent unit of work, executed in order.

---

## Tier 1: Correctness and Consistency

Fix bugs, typos, and inconsistencies that erode trust in a public API.

- [x] **Fix XML doc copy-paste errors**
    - `MinLengthValidator:8` says "maximum" instead of "minimum"
    - `SplitTransformer:8` says "Removes occurrences from end"
    - `SubstringAfterLastTransformer` / `SubstringBeforeLastTransformer` say "first" instead of "last", misspell "occurence"
    - `MinLengthValidator:18` / `MaxLengthValidator:18` / `SetTransformer:14` — "must specified" -> "must specify"
- [x] **Unify exception types across validators and transformers** — pick one strategy (e.g. `TokenizerException` for missing args everywhere)
- [x] **Fix `NewLine` vs `Newline` casing inconsistency** — `Token.TerminateOnNewLine` vs `TokenizerOptions.TerminateOnNewline`
- [x] **Fix inconsistent boolean naming on `Token`** — `Optional`, `Repeating`, `Required`, `Concatenate`, `ConsiderOnce` should be `IsOptional`, `IsRepeating`, `IsRequired`, `ShouldConcatenate`, `ShouldConsiderOnce`
- [x] **Remove duplicate test files** — `ContainsValidatorTest.cs` + `ContainsValidatorTests.cs` (and similar pairs for EndsWith, RemoveEnd, RemoveStart, Remove, Split, SubstringBefore)
- [x] **Clean up stale `#if` guards** — `NET6_0_OR_GREATER` -> `NET8_0_OR_GREATER` in `TemplateLexer.cs`, remove `DOTNET35` guard in `StringExtensions.cs:237`

## Tier 2: API Naming and Shape

Rename and reshape the public API to follow .NET Framework Design Guidelines before more users depend on v3 names.

- [x] **Rename `CanTransform` to `TryTransform`** on `ITokenTransformer` and `TokenDecoratorContext` — current name implies a pure check but it performs the transformation
- [x] **Rename `Match` class to `TokenMatch`** — avoids collision with `System.Text.RegularExpressions.Match`
- [x] **Rename `CandidateTokenList.Any` to `HasCandidates`** — `Any` reads as a method, not a property
- [x] **Rename `TokenEnumerator.Match()` to `TryMatch()` or `StartsWith()`** — clearer intent
- [x] **Make `TokenizeResult<T>.Value` have an `init` setter** — result objects shouldn't allow consumer reassignment
- [x] **Make `Hint` properties use `init` setters** — currently fully mutable with public setters
- [x] **Narrow `ITokenizationEngine` interface** — remove `ProcessRepeatedTokens`, `ProcessNewlineTerminatedTokens`, `TryAssignCandidateTokens` from the contract (these are implementation details)

## Tier 3: Immutability and Options

Lock down mutability to prevent misuse and align with modern .NET patterns.

- [x] **Freeze `TokenizerOptions` after construction** — either `init` setters, a builder, or a `Freeze()` pattern so mutations after `Tokenizer.Create()` are prevented
- [x] **Adopt `IOptions<TokenizerOptions>` in DI registration** — use `services.Configure<TokenizerOptions>()` instead of raw singleton registration
- [x] **Make `TokenizerOptions.Defaults` a `static readonly` field** — currently allocates a new instance on every access
- [x] **Implement `IEnumerable<Template>` on `TemplateCollection`** — users expect to foreach and LINQ over collections

## Tier 4: Packaging and Build

Ensure the NuGet package meets the bar for a professional .NET library.

- [x] **Enable `GenerateDocumentationFile`** — IntelliSense XML docs must ship in the package
- [x] **Add `ContinuousIntegrationBuild` property** — conditional on CI for deterministic/reproducible builds
- [x] **Fix NuGet metadata** — add `PackageIcon`, change `PackageProjectUrl` to HTTPS, update copyright year
- [ ] **Add trimming/AOT annotations** — `IsTrimmable` and `IsAotCompatible` on net8.0+ targets (descoped: library uses reflection for core binding)
- [x] **Expand CI matrix** — add Windows/macOS, add code coverage reporting
- [x] **Add `CHANGELOG.md`** — release notes convention for v3.0.0

## Tier 5: Missing Validators and Transformers

Fill functional gaps in the built-in decorator set.

- [ ] **Add `MatchesRegexValidator`** — the universal escape hatch; highest-value single addition
- [ ] **Add `IsGuidValidator`**
- [ ] **Add `IsIntegerValidator`** — distinct from float-based `IsNumeric`
- [ ] **Add `IsAlphanumericValidator`**
- [ ] **Add `IsInRangeValidator`** — numeric min/max
- [ ] **Add `IsIpAddressValidator`**
- [ ] **Add `ToIntTransformer`**
- [ ] **Add `ToDecimalTransformer`**
- [ ] **Add `ToBooleanTransformer`**
- [ ] **Add `ToGuidTransformer`**
- [ ] **Add `TruncateTransformer`** — cap extracted values to a max length
- [ ] **Add `DefaultValueTransformer`** — coalesce null/empty to a fallback
- [ ] **Add `RegexReplaceTransformer`**
- [ ] **Add `TitleCaseTransformer`**

## Tier 6: Performance

Address remaining allocation and computation hotspots.

- [ ] **Cache regex patterns with `RegexOptions.Compiled`** — `StringExtensions.cs:190`, `ToDateTimeTransformer.cs:84`, `PreambleNearMissHintGenerator.cs:57`
- [ ] **Cache `GetMethod("Add")` in `ObjectExtensions:86`** — uncached reflection on every list property assignment
- [ ] **Eliminate substring allocations in `StringExtensions`** — `EndsWithNewLine` (lines 308-316) and `TrimLeadingSpaces` (line 219) create substrings for single-char comparisons
- [ ] **Merge double iteration in `ProcessFrontMatterTokens`** — `TokenizationEngine.cs:342-344` iterates tokens twice (`.Where` + `.Count`)
- [ ] **Add `ToString()` overrides** on `Match`, `TokenizeResult`, `TokenResult`, `HintResult`, `Hint`, `Template` for debugging
- [ ] **Add `IEquatable<T>`** on value-like types: `Hint`, `HintMatch`, `Match`, `FileLocation`

## Tier 7: Template Compilation Caching

Prevent repeated parsing of the same template pattern.

- [ ] **Introduce internal compilation cache** — `ConcurrentDictionary<string, Template>` behind the string-overload `Tokenize()` methods
- [ ] **Expose `Compile()` API** — let users explicitly compile a template for reuse, making the `CompiledTemplate` concept first-class
- [ ] **Add compilation cache benchmarks** — measure the impact in the benchmark suite

## Tier 8: Architecture and Extensibility

Structural improvements for advanced users and long-term maintainability.

- [ ] **Extract `ITokenParser` interface from `TokenParser`** — separate registration (transformers/validators) from compilation
- [ ] **Make `IHintGenerator` public** — let users add custom hint generators to the diagnostic system
- [ ] **Add middleware/pipeline hooks to `TokenizationEngine`** — pre/post-processing delegates so users can plug in custom logic without subclassing
- [ ] **Add custom matching strategy support** — `ITokenMatcher` strategy pattern for regex-based or fuzzy matching
- [ ] **Add AST visitor pattern** — `Accept(ISyntaxVisitor)` on syntax nodes for template analysis and transformation
- [ ] **Improve parser error recovery** — collect multiple errors instead of failing on the first one
- [ ] **Remove `ITokenDecorator` marker interface** — replace with attributes or type checks per FDG
