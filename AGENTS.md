# AGENTS.md

Instructions for AI agents working on this codebase.

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

See [ARCHITECTURE.md](ARCHITECTURE.md) for the compilation pipeline, tokenization engine, extension points, and async path.

### Entry Points

- `new Tokenizer()` -- default options
- `new Tokenizer(TokenizerOptions)` -- custom options
- `new Tokenizer(TokenizerOptions, ILoggerFactory)` -- with logging
- `new TemplateMatcher(ITokenizer)` -- multi-template matching
- `services.AddTokenizer()` -- DI registration

## Code Conventions

- **Braces**: Allman style
- **Naming**: Transformers as `[Action]Transformer`, Validators as `[Action]Validator`, Exceptions as `[Action]Exception`
- **Private fields**: `_camelCase` (underscore prefix)
- **Constants and static readonly**: `PascalCase`
- **Interfaces**: `IPascalCase`
- **Conditional compilation**: Required when using .NET 8.0+ features (Span<T>, pattern matching) -- must provide .NET Standard 2.0 fallback
- **No regions**: Never use `#region` in source or tests
- **Async**: Core logic is synchronous. Async overloads exist for stream/reader-based I/O.
- **Logging**: Uses `Microsoft.Extensions.Logging`

## Code Style Enforcement

Style and quality rules are enforced via `.editorconfig` and Roslyn analyzers. `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` means violations break the build locally and in CI.

**Analyzer packages:**
- Built-in .NET SDK analyzers (`AnalysisLevel=latest-None` -- only explicitly enabled rules fire)
- `Meziantou.Analyzer` (shared via `Directory.Build.props`, all rules silent by default)

**Enforced rules:**
- `IDE0004` -- Remove unnecessary cast
- `IDE0005` -- No unused usings
- `IDE0040` -- Explicit accessibility modifiers required
- `IDE0044` -- Make field readonly
- `IDE0055` -- Formatting
- `IDE0059` -- Remove unnecessary value assignment
- `IDE0060` -- No unused parameters
- `IDE0161` -- File-scoped namespace declarations
- `IDE1006` -- Naming conventions enforced
- `CA1031` -- Do not catch general exception types
- `CA1507` -- Use `nameof` over string literals
- `CA1508` -- Avoid dead conditional code
- `CA1825` -- Use `Array.Empty<T>()` over zero-length allocations
- `CA2000` -- Dispose objects before losing scope
- `CA2016` -- Forward CancellationToken
- `CA2200` -- Rethrow to preserve stack traces
- `CA2213` -- Disposable fields should be disposed

**Per-rule commands (useful for targeted fixes):**

```bash
# Check one rule (dry run)
dotnet format style ./Tokenizer.sln --verify-no-changes --diagnostics IDE0005

# Auto-fix one rule
dotnet format style ./Tokenizer.sln --diagnostics IDE0005

# See violations for one rule from build output
dotnet build ./Tokenizer.sln 2>&1 | grep "CA1507"
```

**Source of truth:** `.editorconfig` -- all rules and severities are defined there.

## Testing Conventions

- **Framework**: xUnit 2.9.3 with NSubstitute for mocks
- **Naming**: Gherkin style -- `GivenScenario_WhenAction_ThenResult()`
- **Structure**: Arrange / Act / Assert comments within tests
- **Builders**: Fluent test data builders in `tests/Tokenizer.Tests/Builders/` (e.g., `TokenBuilder`, `TemplateBuilder`)
- **Helpers**: Use `Expect[Object][State]` pattern for mock setup methods, placed at end of test class
- **Logging in tests**: Serilog with `Serilog.Sinks.XUnit` for test output
- **File naming**: Test file matches production class: `{ClassName}Tests.cs`. If a single test fixture is too crowded, split into `{ClassName}.{Scenario}.Tests.cs`
