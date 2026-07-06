# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Tokenizer is a C# library that extracts structured information from blocks of text using pattern matching and reflects them onto .NET objects. Published as a NuGet package.

- **Targets**: .NET Standard 2.0, .NET 8.0, and .NET 10.0
- **Root namespace**: `Tokens` (not `Tokenizer`)
- **Language**: C# with `LangVersion=latest`, nullable reference types enabled

## Build Commands

```bash
# Build
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release

# Run all tests
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj

# Run a single test by full name
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName"

# Run tests matching a pattern
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ClassName"
```

## Architecture

### Compilation Pipeline

Template patterns (strings) are compiled through a multi-stage pipeline:

1. **TemplateLexer** (`Compilation/Lexer/`) - Character-by-character scanning produces `LexerToken`s with `FileLocation` tracking
2. **TemplateParser** (`Compilation/Parsing/`) - Converts lexer tokens into an AST (`TemplateDocument` with `TemplateNode`s)
3. **AstTemplateDefinitionParser** (`Compilation/Definitions/`) - Transforms AST into `Template` definition objects
4. **FrontMatterBinder** (`Compilation/Binders/`) - Extracts YAML front matter configuration from between `---` markers
5. **TemplateCompiler** (`Compilation/TemplateCompiler.cs`) - Orchestrates the full compilation pipeline
6. **DecoratorRegistry** (`Compilation/DecoratorRegistry.cs`) - Discovers built-in transformers/validators via assembly reflection, merges custom registrations from `TokenizerOptions`

### Tokenization Engine

Once compiled, templates are used to extract data from input text:

- **TokenizationEngine** (`Tokenization/TokenizationEngine.cs`) - Core processing: matches input against template tokens
- **HintProcessor** (`Tokenization/HintProcessor.cs`) - Pre-filters templates by checking if hint strings exist in the input
- **ResultBuilder** (`Tokenization/ResultBuilder.cs`) - Aggregates matched/unmatched tokens into `TokenizeResult`
- **TokenizationContext** (`Tokenization/TokenizationContext.cs`) - Maintains state during tokenization

### Extension Points

- **Transformers** (`Transformers/`) - Implement `ITokenTransformer` to transform extracted values (e.g., `ToDateTimeTransformer`, `ToUpperTransformer`). Method: `bool CanTransform(object value, string[] args, out object transformed)`
- **Validators** (`Validators/`) - Implement `ITokenValidator` to validate extracted values (e.g., `IsNumericValidator`). Method: `bool IsValid(object value, params string[] args)`
- Register custom implementations via `Tokenizer.RegisterTransformer<T>()` / `RegisterValidator<T>()`

### Entry Points

- `Tokenizer.Create()` - Factory with default options
- `Tokenizer.Create(TokenizerOptions)` - Factory with custom options
- `Tokenizer.Create(TokenizerOptions, ILoggerFactory)` - Factory with logging

## Code Conventions

- **Braces**: Allman style
- **Naming**: Transformers as `[Action]Transformer`, Validators as `[Action]Validator`, Exceptions as `[Action]Exception`
- **Private fields**: `_camelCase` (underscore prefix)
- **Constants and static readonly**: `PascalCase`
- **Interfaces**: `IPascalCase`
- **Conditional compilation**: Required when using .NET 6.0+ features (Span<T>, pattern matching) — must provide .NET Standard 2.0 fallback
- **No regions**: Never use `#region` in source or tests
- **Async**: The core compilation and tokenization logic is synchronous. `Tokenizer` and `TokenMatcher` expose async overloads (`CompileAsync`, `TokenizeAsync`, `MatchAsync`) for stream/reader-based I/O. The async path uses cooperative buffer refills via `TokenEnumerator.FillBufferAsync`
- **Logging**: Uses `Microsoft.Extensions.Logging`

## Code Style Enforcement

Style and quality rules are enforced via `.editorconfig` and Roslyn analyzers. `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` means violations break the build locally and in CI.

**Analyzer packages:**
- Built-in .NET SDK analyzers (`AnalysisLevel=latest-None` — only explicitly enabled rules fire)
- `Meziantou.Analyzer` (shared via `Directory.Build.props`, all rules silent by default)

**Enforced rules:**
- `IDE0005` — No unused usings
- `IDE0040` — Explicit accessibility modifiers required
- `IDE0055` — Formatting
- `IDE0060` — No unused parameters
- `IDE0161` — File-scoped namespace declarations
- `IDE1006` — Naming conventions enforced
- `CA1507` — Use `nameof` over string literals
- `CA1508` — Avoid dead conditional code
- `CA1825` — Use `Array.Empty<T>()` over zero-length allocations
- `CA2016` — Forward CancellationToken
- `CA2200` — Rethrow to preserve stack traces
- `CA2213` — Disposable fields should be disposed

**Per-rule commands (useful for targeted fixes):**

```bash
# Check one rule (dry run)
dotnet format style ./Tokenizer.sln --verify-no-changes --diagnostics IDE0005

# Auto-fix one rule
dotnet format style ./Tokenizer.sln --diagnostics IDE0005

# See violations for one rule from build output
dotnet build ./Tokenizer.sln 2>&1 | grep "CA1507"
```

**Source of truth:** `.editorconfig` — all rules and severities are defined there.

## Testing Conventions

- **Framework**: xUnit 2.9.3 with NSubstitute for mocks
- **Naming**: Gherkin style — `GivenScenario_WhenAction_ThenResult()`
- **Structure**: Arrange / Act / Assert comments within tests
- **Builders**: Fluent test data builders in `tests/Tokenizer.Tests/Builders/` (e.g., `TokenBuilder`, `TemplateBuilder`)
- **Helpers**: Use `Expect[Object][State]` pattern for mock setup methods, placed at end of test class
- **Logging in tests**: Serilog with `Serilog.Sinks.XUnit` for test output
