# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [3.0.0] - Unreleased

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
