# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [3.0.0] - 2026-07-23

### Added

- `Assign<T>(T target)` overload for populating existing object instances (classes, structs, records, and types without parameterless constructors)
- Online documentation at pullpatchpush.com/tokenizer with interactive playground
- Configuration reference and Extensibility guide on docs site

### Changed

- Library icon updated to Phosphor brackets-curly design
- README trimmed to landing-page format with links to documentation site
- README links converted to absolute URLs for NuGet rendering

## [3.0.0-beta.2] - 2026-07-09

### Added

- Token-centric diagnostic model with per-token outcome tracking (Matched, Rejected, NeverFound, Blocked), match attempt history, and assigned value locations
- Diagnostic hint generators: PreambleNearMiss, ValidatorValue, DateFormat, ChainedDecorator, MultipleRejection, OptionalToken, RepeatingToken, ValueMismatch, BlockedToken
- Stable diagnostic issue codes (TK001–TK008) for programmatic filtering
- AlignmentRenderer and ProcessingOrderRenderer for human-readable diagnostic output
- Causality analysis for ordered-mode diagnostics (blocked token detection)
- `MaxRegexTimeout` option on `TokenizerOptions` (default: 1 second) to bound regex evaluation in user-supplied patterns
- `CancellationToken` overloads on synchronous `Tokenize` methods
- SECURITY.md with guidance for processing untrusted input
- 61 diagnostic characterisation tests

### Changed

- Diagnostic subsystem redesigned from flat event stream to token-centric model (`TokenDiagnostic`, `TokenAttempt`, `DiagnosticIssue`)
- `DiagnosticResult` replaced with `TokenizationDiagnostics` (lazy-built token view, raw event access kept)
- Singular `AssignedValue`/`AssignedLocation` on `TokenDiagnostic` replaced with list-based `AssignedValues`/`AssignedLocations` for repeating token support
- Compilation and tokenization diagnostic collectors separated (`ICompilationDiagnosticCollector`, `ITokenizationDiagnosticCollector`)
- `MatchesRegexValidator` hardened: catches `RegexMatchTimeoutException`, removed `RegexOptions.Compiled`, added bounded cache eviction
- `RegexReplaceTransformer` hardened: catches timeout, uses `MaxRegexTimeout`
- Removed unbounded static `PathSegmentCache` (replaced with instance-scoped caching)
- PII logging: guarded exception messages at Debug level, downgraded diagnostic log output

### Fixed

- All CodeQL code scanning alerts resolved (catch-of-all-exceptions, useless-assignment, local-not-disposed, dispose-not-called-on-throw, useless-upcast, missed-readonly, nested-if, missed-ternary, null-argument-to-equals, path-combine, useless-gethashcode, misleading-indentation)
- Infinite regex timeouts on netstandard2.0 fallback paths

## [3.0.0-beta.1] - 2026-06-20

### Added

- AST-based template compilation pipeline (TemplateLexer, TemplateParser, AstTemplateDefinitionParser)
- Tokenization diagnostics system with hint generators (PreambleNearMiss, ValidatorValue, UnmatchedInput, RepeatingToken, DateFormat)
- DiagnosticSummaryBuilder and AlignmentRenderer for visual template-input diffs
- Safety limits: MaxInputLength, MaxTemplateLength, MaxTokenCount, MaxIterations
- BenchmarkDotNet benchmark suite (compilation, tokenization, matching, concurrency)
- `IOptions<TokenizerOptions>` support for DI registration
- `IReadOnlyCollection<Template>` on TemplateCollection
- Microsoft.Extensions.Logging integration
- SourceLink and symbol package (.snupkg) support
- GitHub Actions CI (replacing AppVeyor)
- CodeQL security analysis
- .NET 10.0 target
- XML IntelliSense documentation in NuGet package
- NuGet package icon
- `AssignmentFailedException.PartialResult` property for retrieving partially assigned objects on failure
- `TemporalParser` format offset detection to preserve parsed timezone offsets

### Changed

- Renamed `CanTransform` to `TryTransform` on `ITokenTransformer`
- Renamed `Match` class to `TokenMatch` (now a record)
- Renamed `CandidateTokenList.Any` to `HasCandidates`
- Renamed `TokenEnumerator.Match()` to `TryMatch()`
- Renamed boolean properties on `Token`: `IsOptional`, `IsRepeating`, `IsRequired`, `ShouldConcatenate`, `ShouldConsiderOnce`
- Standardized `TerminateOnNewLine` casing
- Unified decorator argument exceptions to `ArgumentException`
- Converted `TokenizerOptions` to record class
- Converted `Hint` to positional record
- Made `TokenizeResult<T>.Value` init-only
- Made `Hint` properties init-only
- Narrowed `ITokenizationEngine` interface (removed implementation details)
- Made `Token` and exception properties internally settable
- Made public API collections `IReadOnlyList<T>`
- Sealed all non-extension-point classes
- Converted to file-scoped namespaces
- Replaced `Tokenizer.Create()` static factories with public constructors
- Target frameworks: netstandard2.0 + net8.0 + net10.0 (was netstandard2.0 + net6.0)
- `TemporalParser` trims whitespace from input values before parsing
- `TemporalParser` preserves parsed timezone offset when format contains offset specifier (z, zz, zzz, K)

### Fixed

- Empty-preamble infinite loop in tokenization engine
- `ComputePreamble` ignoring template-level `TrimLeadingWhitespace` override
- Getter-only collection properties throwing on `SetValue`
- Context-aware quote handling and repeating token ordering
- Unknown escape sequences in quoted strings treated as literals
- Frontmatter-only matches excluded from `Success` when template has real tokens
- Internal whitespace preserved in hint text during front matter parsing
- All nullable reference type warnings resolved
- XML doc copy-paste errors in validators and transformers

### Removed

- `TokenizerOptions.Defaults` static property (use `new TokenizerOptions()`)
- `TokenizerOptions.Clone()` method (use `with` expressions)
- `EnableLogging` flag, `EnableLineByLineLogging`, and `LineTracker`
- Duplicate test files
- Stale `#if DOTNET35` and `NET6_0_OR_GREATER` preprocessor guards
- AppVeyor CI configuration
- Development-only debug console app
- AI-generated specs and plans (development artifacts)

## [2.2.1] - 2019-10-01

### Fixed

- Reduced excessive log levels

## [2.2.0] - 2019-09-30

### Added

- `Split()` transformer
- Thread-safe `TemplateCollection` for concurrent access
- Documentation tests

### Fixed

- Made multiple template matching deterministic

## [2.1.10] - 2019-06-10

### Fixed

- `IsPhoneNumber` validator

## [2.1.9] - 2019-06-10

### Added

- Not validators (`IsNotNull`, `IsNotEmpty`, etc.)
- `SubstringBeforeLast` and `SubstringAfterLast` transformers

## [2.1.8] - 2019-05-20

### Added

- Multiple token consideration during `Tokenize()` operation
- `TerminateOnNewLine` front matter option
- `Replace()` transformer

### Fixed

- Infinite loop in initial token with no preamble
- Token transformer return value indicating transformation success

## [2.1.2] - 2019-01-15

### Added

- Multiline token support
- Long-form token modifiers
- `Set` transformer for setting token values
- Template tags for restricting matches
- Shorthand set token value assignment in front matter
- Enum and Boolean value assignment
- `IsNotEmpty` validator
- Token preamble trimming before last newline

### Fixed

- Repeating token matching
- `Token.CanAssign()` method
- Front matter token matching when no content tokens found

## [2.1.0] - 2018-11-01

### Added

- Result object returned from `Tokenize` operation
- Required fields support
- Token validators
- Template hints for selecting best match
- Front matter template naming
- Escaped `{` and `}` characters in templates
- DateTime parsing up to newline

### Fixed

- Invalid type conversion handling
- Token assignment failures no longer throw

## [2.0.6] - 2018-09-01

### Added

- UTC DateTime token transformer

## [2.0.5] - 2018-08-15

### Fixed

- Various bug fixes
- Added logging support

## [2.0.0] - 2018-07-01

### Added

- State machine-based token parser
- .NET Standard 2.0 and .NET Framework 4.5.2 support
- Transformers (value transformation pipeline)
- Newline handling in pattern parsing

### Changed

- Complete rewrite of parsing engine from regex to state machine
- Changed repeating token flag to `*`
