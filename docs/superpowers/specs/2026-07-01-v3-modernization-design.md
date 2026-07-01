# Tokenizer v3 Modernization Design

## Overview

Modernize the Tokenizer library across all 20 roadmap items, shipping as v3.0.0 with breaking API changes, internal performance improvements, and infrastructure/polish. Approach: risk-first — breaking changes first, then internals, then polish.

## Decisions Made

- **Target frameworks**: Drop `net6.0`, add `net8.0` and `net10.0`. Final targets: `netstandard2.0;net8.0;net10.0`
- **Token property immutability**: Use `internal set` (no `init` polyfill needed)
- **All mutable public collections** locked down, not just the ones the roadmap listed
- **Seal all public classes** except interfaces (`ITokenTransformer`, `ITokenValidator`)
- **Simplify `TokenizationContext` dispose**: Remove finalizer entirely, plain `Dispose()` only
- **CI matrix**: Test against `net8.0` and `net10.0`

---

## Phase 1: Breaking API Changes

All breaking changes ship together as the v3.0.0 surface.

### 1a. Target framework change (roadmap items 6, 20)

- Change `Tokenizer.csproj` targets from `netstandard2.0;net6.0` to `netstandard2.0;net8.0;net10.0`
- Bump version to `3.0.0`
- Update `#if NET6_0_OR_GREATER` guards to `#if NET8_0_OR_GREATER`
- Review conditional compilation in `StringExtensions` and elsewhere

### 1b. Exception hierarchy fix (roadmap item 1)

- Change `ValidationException` to inherit from `TokenizerException` instead of `Exception`
- Preserve existing constructor signatures

### 1c. Immutable public collections (roadmap item 4)

All mutable `IList<T>` properties on public types become `IReadOnlyList<T>`, backed internally by `List<T>` exposed via `.AsReadOnly()` or cast.

Affected properties:
- `TokenResult.Matches` (`IList<Match>` → `IReadOnlyList<Match>`)
- `TokenResult.Misses` (`IList<Token>` → `IReadOnlyList<Token>`)
- `Template.Hints` (`IList<Hint>` → `IReadOnlyList<Hint>`)
- `Template.Tags` (`IList<string>` → `IReadOnlyList<string>`)
- `Token.Decorators` (`IList<TokenDecoratorContext>` → `IReadOnlyList<TokenDecoratorContext>`)
- `TokenDecoratorContext.Parameters` (`IList<string>` → `IReadOnlyList<string>`)
- `HintResult.Matches` (`IList<HintMatch>` → `IReadOnlyList<HintMatch>`)
- `HintResult.Misses` (`IList<Hint>` → `IReadOnlyList<Hint>`)
- `TokenizeResultBase.Exceptions` (`IList<Exception>` → `IReadOnlyList<Exception>`)

Internal code that mutates these collections uses the backing `List<T>` field directly or through internal methods.

### 1d. Token property immutability (roadmap item 8)

All public setters on `Token` become `internal set`:
- `Preamble`, `Name`, `Optional`, `Repeating`, `TerminateOnNewLine`, `Required`
- `IsFrontMatterToken`, `IsNull`, `Location`, `Concatenate`, `ConcatenationString`, `ConsiderOnce`

### 1e. Seal public classes (roadmap item 7)

Add `sealed` to all public classes that are not designed as extension points:
- Core: `Token`, `Template`, `Hint`, `Match`, `TokenizeResult`, `TokenizerOptions`, `Tokenizer`
- All built-in transformers (e.g., `ToUpperTransformer`, `ToLowerTransformer`, `ToDateTimeTransformer`, etc.)
- All built-in validators (e.g., `IsNumericValidator`, `IsEmailValidator`, etc.)

Extension points remain the interfaces: `ITokenTransformer`, `ITokenValidator`.

### 1f. Exception property immutability (roadmap item 16)

- `LexerException.Line` and `LexerException.Column` → `internal set`
- `ParsingException.Line` and `ParsingException.Column` → `internal set`

---

## Phase 2: Internal Improvements

Non-breaking changes improving performance, correctness, and GC behavior.

### 2a. Cache `Activator.CreateInstance` in `TokenDecoratorContext` (roadmap item 2)

- Add `static ConcurrentDictionary<Type, ITokenDecorator>` caching one instance per decorator type
- `CreateDecorator()` checks cache first, creates and stores on miss
- Safe because decorators are stateless

### 2b. Cache `GetType().GetProperties()` in `ObjectExtensions` (roadmap item 3)

- Add `static ConcurrentDictionary<Type, PropertyInfo[]>` in `ObjectExtensions`
- `SetInnerValue` reads from cache instead of calling `GetProperties()` each time

### 2c. Simplify `TokenizationContext` dispose (roadmap item 9)

- Remove `~TokenizationContext()` finalizer
- Remove `Dispose(bool disposing)` method
- Simplify to plain `Dispose()` that disposes the enumerator and sets `_disposed`
- Remove `GC.SuppressFinalize(this)` call

### 2d. Culture-invariant transformers (roadmap item 11)

- `ToLowerTransformer`: `.ToLower()` → `.ToLowerInvariant()`
- `ToUpperTransformer`: `.ToUpper()` → `.ToUpperInvariant()`

### 2e. Replace `string.Compare` with `string.Equals` (roadmap item 14)

- `Template.cs`: `string.Compare(candidate, tag, StringComparison.InvariantCultureIgnoreCase)` → `string.Equals(...)`
- `TokenParser.cs` (2 locations): same pattern

---

## Phase 3: Infrastructure & Polish

### 3a. GitHub Actions CI workflow (roadmap item 5)

- New `.github/workflows/build-and-test.yml`
- Matrix: `net8.0` and `net10.0` on `ubuntu-latest`
- Steps: checkout, setup-dotnet (multiple versions), restore, build, test
- Delete stale `appveyor.yml`

### 3b. `Directory.Build.props` and `.editorconfig` (roadmap item 10)

- New `Directory.Build.props` at repo root centralizing: `TreatWarningsAsErrors`, `Nullable`, `LangVersion`, `ImplicitUsings`
- Remove duplicated properties from `Tokenizer.csproj`
- New `.editorconfig` enforcing Allman brace style and existing naming conventions

### 3c. Replace `new string[0]` with `Array.Empty<string>()` (roadmap item 13)

- `TokenMatcher.cs` and `StringExtensions.cs` — replace all instances
- Grep for any additional occurrences

### 3d. Add `PackageReadmeFile` to NuGet package (roadmap item 12)

- Add `<PackageReadmeFile>README.md</PackageReadmeFile>` to csproj
- Include README.md in the package via `<None Include="..." Pack="true" PackagePath="\" />`

### 3e. Add `EmbedUntrackedSources` (roadmap item 15)

- Add `<EmbedUntrackedSources>true</EmbedUntrackedSources>` to csproj or `Directory.Build.props`

### 3f. Replace `ArgumentValidation.ThrowIfNull` with BCL method (roadmap item 19)

- Behind `#if NET8_0_OR_GREATER` guard, use `ArgumentNullException.ThrowIfNull`
- Keep existing implementation for `netstandard2.0` path

### 3g. XML doc coverage (roadmap item 17)

Add XML documentation to:
- `HintMatch`, `TokenResult`, `ParsingException`, `TokenAssignmentException`, `TokenMatcherException`
- `TokenizerOptions` properties: `IgnoreMissingProperties`, `TrimLeadingWhitespaceInTokenPreamble`, `TrimPreambleBeforeNewLine`, `OutOfOrderTokens`

### 3h. File-scoped namespaces (roadmap item 18)

- Convert all files from block-scoped to file-scoped namespaces
- Done last to minimize merge noise with other changes

---

## Roadmap Item Coverage

| # | Item | Phase | Section |
|---|------|-------|---------|
| 1 | Fix `ValidationException` inheritance | 1 | 1b |
| 2 | Cache `Activator.CreateInstance` | 2 | 2a |
| 3 | Cache `GetType().GetProperties()` | 2 | 2b |
| 4 | Replace mutable `IList<T>` with `IReadOnlyList<T>` | 1 | 1c |
| 5 | Add CI workflow | 3 | 3a |
| 6 | Bump version to 3.0.0 | 1 | 1a |
| 7 | Seal public classes | 1 | 1e |
| 8 | Make `Token` properties immutable | 1 | 1d |
| 9 | Remove finalizer on `TokenizationContext` | 2 | 2c |
| 10 | Add `Directory.Build.props` and `.editorconfig` | 3 | 3b |
| 11 | Culture-invariant transformers | 2 | 2d |
| 12 | Add `PackageReadmeFile` | 3 | 3d |
| 13 | Replace `new string[0]` with `Array.Empty` | 3 | 3c |
| 14 | Replace `string.Compare` with `string.Equals` | 2 | 2e |
| 15 | Add `EmbedUntrackedSources` | 3 | 3e |
| 16 | Exception location properties `internal set` | 1 | 1f |
| 17 | XML doc coverage | 3 | 3g |
| 18 | File-scoped namespaces | 3 | 3h |
| 19 | Use `ArgumentNullException.ThrowIfNull` | 3 | 3f |
| 20 | Target supported .NET version | 1 | 1a |
