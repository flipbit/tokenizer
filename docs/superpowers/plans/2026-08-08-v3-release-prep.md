# v3.0.0-beta.1 Release Preparation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prepare the v3 branch for squash-merge to main and publish 3.0.0-beta.1 to NuGet/GitHub.

**Architecture:** This is a housekeeping and release-prep effort. No new features. We stabilize uncommitted changes with tests, remove development artifacts, restructure documentation for humans and AI agents, update CI for the `master`-to-`main` rename, and set the beta version.

**Tech Stack:** C# / .NET 10.0, xUnit, BenchmarkDotNet, GitHub Actions, GitHub CLI (`gh`)

---

### Task 1: Review and test uncommitted changes

The working tree has 3 uncommitted files with valid but untested changes. Write tests first, then commit.

**Files:**
- Test: `tests/Tokenizer.Tests/Exceptions/AssignmentFailedExceptionTests.cs`
- Test: `tests/Tokenizer.Tests/Temporal/TemporalParserTests.cs`
- Test: `tests/Tokenizer.Tests/TokenizeResultAssignTests.cs`
- Already modified (unstaged): `src/Tokenizer/Exceptions/AssignmentFailedException.cs`
- Already modified (unstaged): `src/Tokenizer/Temporal/TemporalParser.cs`
- Already modified (unstaged): `src/Tokenizer/TokenizeResult.cs`

- [ ] **Step 1: Add PartialResult test to AssignmentFailedExceptionTests.cs**

Add this test to the existing `AssignmentFailedExceptionTests` class at `tests/Tokenizer.Tests/Exceptions/AssignmentFailedExceptionTests.cs`:

```csharp
[Fact]
public void GivenPartialResult_WhenSet_ThenCanBeRetrieved()
{
    // Arrange
    var exception = new AssignmentFailedException("test", new List<Exception>());
    var partial = new object();

    // Act
    exception.PartialResult = partial;

    // Assert
    Assert.Same(partial, exception.PartialResult);
}

[Fact]
public void GivenNewException_WhenCreated_ThenPartialResultIsNull()
{
    // Arrange & Act
    var exception = new AssignmentFailedException("test", new List<Exception>());

    // Assert
    Assert.Null(exception.PartialResult);
}
```

- [ ] **Step 2: Add PartialResult integration test to TokenizeResultAssignTests.cs**

Add this test to the existing `TokenizeResultAssignTests` class at `tests/Tokenizer.Tests/TokenizeResultAssignTests.cs`:

```csharp
[Fact]
public void GivenPartialAssignmentFailure_WhenAssign_ThenExceptionContainsPartialResult()
{
    // Arrange — Name will succeed, Score will fail (string -> int? conversion)
    var nameToken = new TokenBuilder().WithName("Name").Build();
    var scoreToken = new TokenBuilder().WithName("Score").Build();
    var template = new TemplateBuilder().WithName("Test")
        .WithTokens(nameToken, scoreToken).WithDefaultOptions().Build();
    var result = new TokenizeResultBuilder().WithTemplate(template)
        .WithMatches(
            new TokenMatch(nameToken, "Alice", new FileLocation()),
            new TokenMatch(scoreToken, "not-a-number", new FileLocation()))
        .Build();

    // Act
    var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign<Person>());

    // Assert
    Assert.NotNull(ex.PartialResult);
    var partial = Assert.IsType<Person>(ex.PartialResult);
    Assert.Equal("Alice", partial.Name);
}
```

- [ ] **Step 3: Add FormatContainsOffset tests to TemporalParserTests.cs**

Add these tests to the existing `TemporalParserTests` class at `tests/Tokenizer.Tests/Temporal/TemporalParserTests.cs`:

```csharp
[Theory]
[InlineData("yyyy-MM-ddTHH:mm:sszzz", true)]
[InlineData("yyyy-MM-ddTHH:mm:ssZ", false)]  // capital Z is literal, not a format specifier
[InlineData("yyyy-MM-dd HH:mm:ss zz", true)]
[InlineData("yyyy-MM-ddTHH:mm:ssK", true)]
[InlineData("yyyy-MM-dd HH:mm:ss", false)]
[InlineData("dd MMM yyyy", false)]
[InlineData("yyyy-MM-dd'T'HH:mm:ssz", true)]
[InlineData("'z'yyyy-MM-dd", false)]  // z inside quotes is literal
public void GivenFormat_WhenCheckingForOffset_ThenReturnsExpected(string format, bool expected)
{
    // Act — FormatContainsOffset is private, so we test via TryParse behavior
    // This test documents the expected behavior for format offset detection
    var options = new TokenizerOptions();

    // We test indirectly: if format has offset, DefaultOffset should NOT override parsed offset
    // If format lacks offset, DefaultOffset SHOULD apply
    // Use a value with explicit +05:00 and a DefaultOffset of +02:00
    if (expected)
    {
        // Format has offset specifier — parsed offset should be preserved
        // We verify by ensuring DefaultOffset doesn't override
        var result = TemporalParser.TryParse("2024-01-15T14:30:00+05:00",
            [format], new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) }, out var dto);

        // If parsing succeeds with this format, offset should be +05:00 not +02:00
        if (result)
        {
            Assert.Equal(TimeSpan.FromHours(5), dto.Offset);
        }
    }
    else
    {
        // Format lacks offset specifier — DefaultOffset should apply
        var result = TemporalParser.TryParse("2024-01-15 14:30:00",
            [format], new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) }, out var dto);

        if (result)
        {
            Assert.Equal(TimeSpan.FromHours(2), dto.Offset);
        }
    }
}
```

Wait — that test is indirect and fragile. Since `FormatContainsOffset` is private, let's write a focused integration test instead:

```csharp
[Fact]
public void GivenFormatWithOffsetSpecifier_WhenParsingWithDefaultOffset_ThenParsedOffsetIsPreserved()
{
    // Arrange — format contains "zzz", value has +05:00, default is +02:00
    var options = new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) };

    // Act
    var result = TemporalParser.TryParse("2024-01-15T14:30:00+05:00",
        ["yyyy-MM-ddTHH:mm:sszzz"], options, out var dto);

    // Assert — parsed offset wins over default
    Assert.True(result);
    Assert.Equal(TimeSpan.FromHours(5), dto.Offset);
}

[Fact]
public void GivenFormatWithKSpecifier_WhenParsingWithDefaultOffset_ThenParsedOffsetIsPreserved()
{
    // Arrange — format contains "K"
    var options = new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) };

    // Act
    var result = TemporalParser.TryParse("2024-01-15T14:30:00+05:00",
        ["yyyy-MM-ddTHH:mm:ssK"], options, out var dto);

    // Assert
    Assert.True(result);
    Assert.Equal(TimeSpan.FromHours(5), dto.Offset);
}
```

- [ ] **Step 4: Add whitespace tolerance test to TemporalParserTests.cs**

```csharp
[Fact]
public void GivenValueWithLeadingAndTrailingWhitespace_WhenParsing_ThenTrimsAndParses()
{
    // Arrange
    var options = new TokenizerOptions();

    // Act
    var result = TemporalParser.TryParse("  2024-01-15  ", ["yyyy-MM-dd"], options, out var dto);

    // Assert
    Assert.True(result);
    Assert.Equal(15, dto.Day);
}

[Fact]
public void GivenWhitespaceOnlyValue_WhenParsing_ThenReturnsFalse()
{
    // Arrange
    var options = new TokenizerOptions();

    // Act
    var result = TemporalParser.TryParse("   ", ["yyyy-MM-dd"], options, out _);

    // Assert
    Assert.False(result);
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "AssignmentFailedExceptionTests|TemporalParserTests|TokenizeResultAssignTests" -v normal`

Expected: All new tests PASS (the implementation already exists in the unstaged changes).

- [ ] **Step 6: Commit all changes**

```bash
git add src/Tokenizer/Exceptions/AssignmentFailedException.cs src/Tokenizer/Temporal/TemporalParser.cs src/Tokenizer/TokenizeResult.cs tests/Tokenizer.Tests/Exceptions/AssignmentFailedExceptionTests.cs tests/Tokenizer.Tests/Temporal/TemporalParserTests.cs tests/Tokenizer.Tests/TokenizeResultAssignTests.cs
git commit -m "feat: add PartialResult to AssignmentFailedException, fix TemporalParser offset handling"
```

---

### Task 2: Delete AI-generated development artifacts

Remove all spec/plan/review files that were generated during v3 development. These are tracked in git and need to be removed from the repository.

**Files:**
- Delete: `docs/` directory (entire tree — ROADMAP.md, superpowers/plans/*, superpowers/specs/*)
- Delete: `specs/` directory (entire tree — prds/*, tasks/*, review.md)
- Delete: `debug_token_app/` directory
- Delete: `benchmark-results/` directory (if tracked)
- Delete: `benchmarks/baselines/streaming-input/` directory
- Delete: `Tokenizer.sln.DotSettings.user`
- Delete: Root-level `Tokenizer/` and `Tokenizer.Tests/` directories (build artifacts)
- Modify: `Tokenizer.sln` — remove debug_token_app project and stale Appveyor reference
- Modify: `.gitignore` — add new exclusions

- [ ] **Step 1: Remove tracked artifact directories**

```bash
git rm -r docs/ specs/
git rm Tokenizer.sln.DotSettings.user
git rm -r benchmarks/baselines/streaming-input/
```

Note: Only run `git rm` on files that are actually tracked. Check with `git ls-files` first if unsure.

- [ ] **Step 2: Delete untracked artifact directories**

```bash
rm -rf debug_token_app/
rm -rf benchmark-results/
rm -rf Tokenizer/
rm -rf Tokenizer.Tests/
rm -rf BenchmarkDotNet.Artifacts/
```

- [ ] **Step 3: Remove debug_token_app from Tokenizer.sln**

The solution file no longer references debug_token_app (it was never added), but it does reference `Appveyor.yml` in the Solution Items section. Remove that stale reference.

In `Tokenizer.sln`, remove the entire Solution Items project block:

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Solution Items", "Solution Items", "{03008BA1-74FF-4DD9-BE0A-71AD03C4F969}"
	ProjectSection(SolutionItems) = preProject
		Appveyor.yml = Appveyor.yml
	EndProjectSection
EndProject
```

- [ ] **Step 4: Update .gitignore**

Add these entries to the `.gitignore` file:

```
# Development artifacts
benchmark-results/
debug_token_app/
*.DotSettings.user
```

- [ ] **Step 5: Verify build still works**

Run: `dotnet build ./Tokenizer.sln -c Release`

Expected: Build succeeds with no errors.

- [ ] **Step 6: Commit**

```bash
git status
git add -A  # safe here because we just did git status and know what's changing
git commit -m "chore: remove AI-generated specs, plans, and development artifacts"
```

---

### Task 3: Move LICENSE.txt and add global.json

**Files:**
- Move: `src/Tokenizer/LICENSE.txt` → `LICENSE.txt` (repo root)
- Modify: `src/Tokenizer/Tokenizer.csproj` — update license path
- Create: `global.json`

- [ ] **Step 1: Move LICENSE.txt to repo root**

```bash
git mv src/Tokenizer/LICENSE.txt LICENSE.txt
```

- [ ] **Step 2: Update csproj license path**

In `src/Tokenizer/Tokenizer.csproj`, change the license item group:

Old:
```xml
  <ItemGroup>
    <None Include="LICENSE.txt" Pack="true" PackagePath="$(PackageLicenseFile)" />
  </ItemGroup>
```

New:
```xml
  <ItemGroup>
    <None Include="../../LICENSE.txt" Pack="true" PackagePath="$(PackageLicenseFile)" />
  </ItemGroup>
```

- [ ] **Step 3: Create global.json**

Create `global.json` at the repo root:

```json
{
  "sdk": {
    "version": "10.0.301",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Step 4: Verify build**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`

Expected: Build succeeds, no license-related pack warnings.

- [ ] **Step 5: Commit**

```bash
git add LICENSE.txt src/Tokenizer/Tokenizer.csproj global.json
git commit -m "chore: move LICENSE.txt to repo root, add global.json"
```

---

### Task 4: Create ARCHITECTURE.md

Extract the architecture section from CLAUDE.md into a standalone file. This will be referenced by README.md, AGENTS.md, and CLAUDE.md.

**Files:**
- Create: `ARCHITECTURE.md`

- [ ] **Step 1: Create ARCHITECTURE.md**

Create `ARCHITECTURE.md` at the repo root with the architecture content extracted from CLAUDE.md. This should cover the compilation pipeline, tokenization engine, extension points, and entry points. Write it for a developer or AI agent who wants to understand how the library works internally.

```markdown
# Architecture

Tokenizer processes text in two phases: **compilation** (parsing a template pattern into an internal representation) and **tokenization** (matching input text against a compiled template to extract values).

## Compilation Pipeline

Template patterns are compiled through a multi-stage pipeline:

```
pattern string
    → TemplateLexer (character scanning → LexerTokens)
    → TemplateParser (LexerTokens → AST: TemplateDocument/TemplateNodes)
    → AstTemplateDefinitionParser (AST → Template definition)
    → FrontMatterBinder (extracts YAML config from --- markers)
    → TemplateCompiler (orchestrates the full pipeline)
```

| Stage | Location | Responsibility |
|-------|----------|---------------|
| TemplateLexer | `Compilation/Lexer/` | Character-by-character scanning, produces `LexerToken`s with `FileLocation` tracking |
| TemplateParser | `Compilation/Parsing/` | Converts lexer tokens into an AST (`TemplateDocument` with `TemplateNode`s) |
| AstTemplateDefinitionParser | `Compilation/Definitions/` | Transforms AST into `Template` definition objects |
| FrontMatterBinder | `Compilation/Binders/` | Extracts YAML front matter configuration from between `---` markers |
| TemplateCompiler | `Compilation/TemplateCompiler.cs` | Orchestrates the full compilation pipeline |
| DecoratorRegistry | `Compilation/DecoratorRegistry.cs` | Discovers built-in transformers/validators via assembly reflection, merges custom registrations from `TokenizerOptions` |

Compiled templates are cached internally by pattern string, so repeated calls to `Tokenize(pattern, input)` only compile once.

## Tokenization Engine

Once compiled, templates extract data from input text:

| Component | Location | Responsibility |
|-----------|----------|---------------|
| TokenizationEngine | `Tokenization/TokenizationEngine.cs` | Core processing: matches input against template tokens sequentially |
| HintProcessor | `Tokenization/HintProcessor.cs` | Pre-filters templates by checking if hint strings exist in the input before full tokenization |
| ResultBuilder | `Tokenization/ResultBuilder.cs` | Aggregates matched/unmatched tokens into `TokenizeResult` |
| TokenizationContext | `Tokenization/TokenizationContext.cs` | Maintains state (position, matches so far) during a tokenization pass |

The engine walks the input text looking for each token's **preamble** (the literal text preceding the token). When found, it extracts the value up to the next preamble or terminator, runs validators, applies transformers, and records the match.

## Extension Points

**Transformers** (`Transformers/`) modify extracted values before assignment. Implement `ITokenTransformer`:

```csharp
bool TryTransform(object value, string[] args, out object transformed);
```

**Validators** (`Validators/`) accept or reject extracted values. Implement `ITokenValidator`:

```csharp
bool IsValid(object value, params string[] args);
```

Register custom implementations via `TokenizerOptions`:

```csharp
var options = new TokenizerOptions()
    .WithTransformer<MyTransformer>()
    .WithValidator<MyValidator>();
```

## Async Path

The core compilation and tokenization logic is synchronous. `Tokenizer` and `TemplateMatcher` expose async overloads (`CompileAsync`, `TokenizeAsync`) for stream/reader-based I/O. The async path uses cooperative buffer refills via `TokenEnumerator.FillBufferAsync`, allowing tokenization of inputs larger than memory.

## Entry Points

| Class | Purpose |
|-------|---------|
| `Tokenizer` | Single-template tokenization. Compile a pattern, tokenize input against it. |
| `TemplateMatcher` | Multi-template matching. Register multiple templates, find the best match for an input. |

Both are available via DI using `services.AddTokenizer()`.
```

- [ ] **Step 2: Commit**

```bash
git add ARCHITECTURE.md
git commit -m "docs: add ARCHITECTURE.md"
```

---

### Task 5: Create AGENTS.md and restructure CLAUDE.md

Move the agent instructions from CLAUDE.md into AGENTS.md. Update CLAUDE.md to reference AGENTS.md. The AGENTS.md should contain everything an AI agent needs to work on this codebase.

**Files:**
- Create: `AGENTS.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: Create AGENTS.md**

Create `AGENTS.md` at the repo root. This contains the project overview, build commands, code conventions, code style enforcement, and testing conventions from the current CLAUDE.md. It references ARCHITECTURE.md for the architecture details. The entry points section should be updated to reflect the v3 API (public constructors, not `Create()` factories).

```markdown
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

- `new Tokenizer()` — default options
- `new Tokenizer(TokenizerOptions)` — custom options
- `new Tokenizer(TokenizerOptions, ILoggerFactory)` — with logging
- `new TemplateMatcher(ITokenizer)` — multi-template matching
- `services.AddTokenizer()` — DI registration

## Code Conventions

- **Braces**: Allman style
- **Naming**: Transformers as `[Action]Transformer`, Validators as `[Action]Validator`, Exceptions as `[Action]Exception`
- **Private fields**: `_camelCase` (underscore prefix)
- **Constants and static readonly**: `PascalCase`
- **Interfaces**: `IPascalCase`
- **Conditional compilation**: Required when using .NET 8.0+ features (Span<T>, pattern matching) — must provide .NET Standard 2.0 fallback
- **No regions**: Never use `#region` in source or tests
- **Async**: Core logic is synchronous. Async overloads exist for stream/reader-based I/O.
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
- **File naming**: Test file matches production class: `{ClassName}Tests.cs`. If a single test fixture is too crowded, split into `{ClassName}.{Scenario}.Tests.cs`
```

- [ ] **Step 2: Replace CLAUDE.md contents**

Replace the entire contents of `CLAUDE.md` with:

```markdown
@AGENTS.md
```

- [ ] **Step 3: Commit**

```bash
git add AGENTS.md CLAUDE.md
git commit -m "docs: create AGENTS.md, update CLAUDE.md to reference it"
```

---

### Task 6: Create nested AGENTS.md and CLAUDE.md for tests and benchmarks

**Files:**
- Create: `tests/AGENTS.md`
- Create: `tests/CLAUDE.md`
- Create: `benchmarks/AGENTS.md`
- Create: `benchmarks/CLAUDE.md`

- [ ] **Step 1: Create tests/AGENTS.md**

```markdown
# Test Suite

Instructions for AI agents working on tests in this project.

## Framework

- xUnit 2.9.3 with `Serilog.Sinks.XUnit` for test output
- NSubstitute for mocks (but never mock the thing you're testing)
- Tests run against .NET 10.0: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

## Naming

Gherkin style: `GivenScenario_WhenAction_ThenResult()`

Examples:
- `GivenEmptyInput_WhenTokenizing_ThenReturnsNoMatches()`
- `GivenOptionalToken_WhenMissing_ThenSkipsToken()`
- `GivenInvalidFormat_WhenParsing_ThenReturnsFalse()`

## File Naming

Test file matches production class: `{ClassName}Tests.cs`

Place test files in the same namespace hierarchy as the production code. For example:
- `src/Tokenizer/Temporal/TemporalParser.cs` → `tests/Tokenizer.Tests/Temporal/TemporalParserTests.cs`
- `src/Tokenizer/Tokenizer.cs` → `tests/Tokenizer.Tests/TokenizerTests.cs`

If a single test fixture grows too large, split by scenario: `{ClassName}.{Scenario}.Tests.cs`
- Example: `TokenizeResultAssignTests.cs` covers `TokenizeResult.Assign<T>()`

## Structure

Every test uses Arrange / Act / Assert comments:

```csharp
[Fact]
public void GivenValidInput_WhenTokenizing_ThenExtractsValue()
{
    // Arrange
    var tokenizer = new Tokenizer();
    var pattern = "Name: {Name}";

    // Act
    var result = tokenizer.Tokenize<Person>(pattern, "Name: Alice");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Alice", result.Name);
}
```

## Test Data Builders

Fluent builders live in `tests/Tokenizer.Tests/Builders/`:

- `TokenBuilder` — builds `Token` instances
- `TemplateBuilder` — builds `Template` instances with tokens and options
- `TokenizeResultBuilder` — builds `TokenizeResult` with matches, misses, exceptions
- `HintBuilder` — builds `Hint` instances

Use builders instead of constructing test objects directly. They handle required fields and default values.

## Mock Setup Helpers

Use `Expect[Object][State]` naming for mock setup methods. Place at the end of the test class:

```csharp
private void ExpectEngineReturnsNoMatches()
{
    _engine.Tokenize(Arg.Any<Template>(), Arg.Any<string>())
        .Returns(new TokenizeResult(template));
}
```

## Running Tests

```bash
# All tests
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj

# Single test class
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemporalParserTests"

# Single test method
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TemporalParserTests.GivenIso8601Value_WhenParsingWithFormat_ThenReturnsDateTimeOffset"
```
```

- [ ] **Step 2: Create tests/CLAUDE.md**

```markdown
@AGENTS.md
```

- [ ] **Step 3: Create benchmarks/AGENTS.md**

```markdown
# Benchmarks

Instructions for AI agents working on benchmarks in this project.

## Framework

BenchmarkDotNet 0.15.8 targeting .NET 10.0. Project: `benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj`

## Running Benchmarks

```bash
# Run all benchmarks (Release mode required)
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj

# Run a specific benchmark class
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -- --filter "*CompilationBenchmarks*"
```

## Benchmark Classes

| Class | What it measures |
|-------|-----------------|
| `CompilationBenchmarks` | Template pattern compilation throughput |
| `CompilationCacheBenchmarks` | Cache hit/miss performance |
| `TokenizationBenchmarks` | Core tokenization (string input) |
| `AsyncTokenizationBenchmarks` | Async tokenization (TextReader/Stream) |
| `MatcherBenchmarks` | Multi-template matching |
| `AsyncMatcherBenchmarks` | Async multi-template matching |
| `ConcurrencyBenchmarks` | Thread-safety and parallel throughput |
| `HintStrategyBenchmarks` | Hint pre-filtering strategies |
| `InputStreamBenchmarks` | Stream-based input processing |

## Baselines

Baselines are stored in `benchmarks/baselines/{yyyy-MM-dd}/` as GitHub-flavored Markdown reports.

When creating a new baseline:
1. Run the full benchmark suite
2. Copy the `*-report-github.md` files from `BenchmarkDotNet.Artifacts/results/` to `benchmarks/baselines/{today's date}/`
3. Commit the baseline

Compare against the most recent baseline to detect regressions.

## Configuration

Custom config in `Config/BenchmarkConfig.cs` adds:
- Memory diagnoser (allocations)
- Threading diagnoser
- P95 latency column
- Full JSON exporter
```

- [ ] **Step 4: Create benchmarks/CLAUDE.md**

```markdown
@AGENTS.md
```

- [ ] **Step 5: Commit**

```bash
git add tests/AGENTS.md tests/CLAUDE.md benchmarks/AGENTS.md benchmarks/CLAUDE.md
git commit -m "docs: add nested AGENTS.md and CLAUDE.md for tests and benchmarks"
```

---

### Task 7: Add CONTRIBUTING.md and GitHub templates

**Files:**
- Create: `CONTRIBUTING.md`
- Create: `.github/ISSUE_TEMPLATE/bug_report.md`
- Create: `.github/ISSUE_TEMPLATE/feature_request.md`
- Create: `.github/PULL_REQUEST_TEMPLATE.md`

- [ ] **Step 1: Create CONTRIBUTING.md**

```markdown
# Contributing

## Getting Started

1. Fork the repository
2. Clone your fork
3. Create a feature branch from `main`

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later (pinned in `global.json`)

## Building

```bash
dotnet build ./Tokenizer.sln
```

## Testing

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

All pull requests must pass the full test suite on both Ubuntu and Windows (enforced by CI).

## Code Style

Code style is enforced by `.editorconfig` and Roslyn analyzers. The build will fail on violations. To check formatting:

```bash
dotnet format style ./Tokenizer.sln --verify-no-changes
```

To auto-fix:

```bash
dotnet format style ./Tokenizer.sln
```

## Pull Requests

- Keep changes focused. One logical change per PR.
- Add tests for new functionality and bug fixes.
- Update `CHANGELOG.md` under the `[Unreleased]` section.
- Ensure all tests pass and the build is clean before submitting.

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for an overview of how the library is structured.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE.txt).
```

- [ ] **Step 2: Create .github/ISSUE_TEMPLATE/bug_report.md**

```markdown
---
name: Bug Report
about: Report a bug in Tokenizer
labels: bug
---

## Description

A clear description of the bug.

## Steps to Reproduce

1.
2.
3.

## Expected Behavior

What you expected to happen.

## Actual Behavior

What actually happened. Include error messages or stack traces if applicable.

## Environment

- Tokenizer version:
- .NET version:
- OS:
```

- [ ] **Step 3: Create .github/ISSUE_TEMPLATE/feature_request.md**

```markdown
---
name: Feature Request
about: Suggest a new feature or improvement
labels: enhancement
---

## Description

What would you like to see added or changed?

## Use Case

Describe the problem this would solve or the scenario where this is useful.

## Proposed Solution

If you have a specific approach in mind, describe it here.
```

- [ ] **Step 4: Create .github/PULL_REQUEST_TEMPLATE.md**

```markdown
## Summary

Brief description of what this PR does.

## Changes

-

## Test Plan

- [ ] New tests added
- [ ] All existing tests pass
- [ ] Build is clean (no warnings)

## Checklist

- [ ] CHANGELOG.md updated (if user-facing change)
- [ ] XML docs added for public API changes
```

- [ ] **Step 5: Commit**

```bash
git add CONTRIBUTING.md .github/ISSUE_TEMPLATE/ .github/PULL_REQUEST_TEMPLATE.md
git commit -m "docs: add CONTRIBUTING.md and GitHub issue/PR templates"
```

---

### Task 8: Update CI workflows for main branch

**Files:**
- Modify: `.github/workflows/build.yml`
- Modify: `.github/workflows/codeql.yml`
- Modify: `.github/workflows/release.yml`

- [ ] **Step 1: Update build.yml branch references**

In `.github/workflows/build.yml`, change:

```yaml
on:
  push:
    branches: [ master, v3 ]
  pull_request:
    branches: [ master ]
```

To:

```yaml
on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
```

- [ ] **Step 2: Update codeql.yml branch references**

In `.github/workflows/codeql.yml`, change:

```yaml
on:
  push:
    branches: [ "master" ]
  pull_request:
    branches: [ "master" ]
```

To:

```yaml
on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]
```

Also update the CodeQL action versions from v2/v3 to v4:

```yaml
      - name: Checkout
        uses: actions/checkout@v4

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: ${{ matrix.language }}
          queries: +security-and-quality

      - name: Autobuild
        uses: github/codeql-action/autobuild@v3

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v3
        with:
          category: "/language:${{ matrix.language }}"
```

- [ ] **Step 3: Add prerelease detection to release.yml**

In `.github/workflows/release.yml`, change the GitHub Release step from:

```yaml
      - name: Create GitHub Release
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          gh release create "${{ github.ref_name }}" \
            ./artifacts/*.nupkg \
            --title "${{ github.ref_name }}" \
            --generate-notes
```

To:

```yaml
      - name: Create GitHub Release
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          gh release create "${{ github.ref_name }}" \
            ./artifacts/*.nupkg \
            --title "${{ github.ref_name }}" \
            --generate-notes \
            ${{ contains(github.ref_name, '-') && '--prerelease' || '' }}
```

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/
git commit -m "ci: update branch references to main, add prerelease support"
```

---

### Task 9: Rewrite README.md

Full rewrite with v3 API examples and expanded feature coverage.

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Write new README.md**

Replace the entire contents of `README.md`. The README should cover:

1. **Header** — Project name, badges (build, NuGet version, NuGet downloads, license), one-line description
2. **Installation** — `dotnet add package Tokenizer --version 3.0.0-beta.1`
3. **Quick Start** — Basic pattern matching with `Tokenize<T>`, showing the `new Tokenizer()` constructor
4. **Features** — Each with a short code example:
   - In-order vs out-of-order processing (with front matter config)
   - Multiline tokens
   - Newline termination (`$`)
   - Repeating tokens (`*`)
   - Required (`!`) and optional (`?`) fields
   - Configuration (constructor options, front matter)
   - Data transformers (chaining example)
   - Data validators (with retry-on-failure behavior)
   - Template compilation and caching (`Compile()` + `Tokenize(template, input)`)
   - Async/streaming (`TokenizeAsync` with `TextReader`)
   - Multi-template matching (`TemplateMatcher`)
   - Dependency injection (`services.AddTokenizer()`)
5. **Built-in Transformers** — Table with name and description
6. **Built-in Validators** — Table with name and description
7. **Custom Transformers and Validators** — Brief example of implementing `ITokenTransformer`
8. **Configuration Reference** — Front matter options table
9. **Architecture** — Link to ARCHITECTURE.md
10. **Contributing** — Link to CONTRIBUTING.md
11. **License** — MIT, link to LICENSE.txt

Key points for the examples:
- Use `new Tokenizer()` not `Tokenizer.Create()`
- Use `tokenizer.Tokenize<T>(template, input)` with pre-compiled templates where appropriate
- Use `result.Assign<T>()` for the `TokenizeResult` path
- Use xUnit-style assertions in examples (or just plain asserts)
- Keep examples concise and runnable

- [ ] **Step 2: Verify README renders correctly**

Review the markdown for broken links, unclosed code fences, or formatting issues.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: rewrite README.md for v3 API"
```

---

### Task 10: Update CHANGELOG.md

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Update version header and add missing entries**

In `CHANGELOG.md`:

1. Change `## [3.0.0] - Unreleased` to `## [3.0.0-beta.1] - 2026-08-08`

2. Add these entries to the appropriate sections:

Under **Added**:
```
- `AssignmentFailedException.PartialResult` property for retrieving partially assigned objects
- `TokenizeResult.Assign<T>()` attaches partial result to exception on failure
- `TemporalParser.FormatContainsOffset` to detect offset specifiers in format strings
```

Under **Changed**:
```
- `TemporalParser` trims whitespace from input before parsing
- `TemporalParser` preserves parsed timezone offset when format contains offset specifier (z, zz, zzz, K)
```

Under **Removed**:
```
- Development-only debug console app
- AI-generated specs and plans (development artifacts)
```

3. Also update the entry points description under **Changed** — the README entry that says `Tokenizer.Create()` should say "public constructors" since that's how it works now (this is already correct in the changelog — verify).

- [ ] **Step 2: Commit**

```bash
git add CHANGELOG.md
git commit -m "docs: update CHANGELOG.md for 3.0.0-beta.1"
```

---

### Task 11: Set beta version in csproj

**Files:**
- Modify: `src/Tokenizer/Tokenizer.csproj`

- [ ] **Step 1: Update PackageVersion**

In `src/Tokenizer/Tokenizer.csproj`, change:

```xml
<PackageVersion>3.0.0</PackageVersion>
```

To:

```xml
<PackageVersion>3.0.0-beta.1</PackageVersion>
```

Leave `<Version>3.0.0.0</Version>` unchanged (assembly version doesn't support semver suffixes).

- [ ] **Step 2: Verify build and pack**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
dotnet pack ./src/Tokenizer/Tokenizer.csproj -c Release --no-build -o ./artifacts
ls ./artifacts/  # should show Tokenizer.3.0.0-beta.1.nupkg and .snupkg
rm -rf ./artifacts/
```

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Tokenizer.csproj
git commit -m "chore: set PackageVersion to 3.0.0-beta.1"
```

---

### Task 12: Generate benchmark baseline

Run the full benchmark suite and save results as the v3 baseline.

**Files:**
- Create: `benchmarks/baselines/2026-08-08/` (multiple report files)

- [ ] **Step 1: Run full benchmark suite**

```bash
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj
```

This will take several minutes. Results go to `BenchmarkDotNet.Artifacts/results/`.

- [ ] **Step 2: Copy reports to baseline directory**

```bash
mkdir -p benchmarks/baselines/2026-08-08
cp BenchmarkDotNet.Artifacts/results/*-report-github.md benchmarks/baselines/2026-08-08/
```

- [ ] **Step 3: Commit**

```bash
git add benchmarks/baselines/2026-08-08/
git commit -m "perf: add 2026-08-08 benchmark baseline for v3.0.0-beta.1"
```

---

### Task 13: Rename master to main

This must happen before we push and create the PR.

- [ ] **Step 1: Rename default branch on GitHub**

```bash
gh repo edit --default-branch main
```

This renames `master` to `main` on the remote.

- [ ] **Step 2: Update local tracking**

```bash
git branch -m master main
git fetch origin
git branch -u origin/main main
```

- [ ] **Step 3: Verify**

```bash
git branch -a
gh repo view --json defaultBranchRef -q '.defaultBranchRef.name'
```

Expected: Default branch is `main`, local `main` tracks `origin/main`.

---

### Task 14: Final validation

- [ ] **Step 1: Run full test suite**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v normal
```

Expected: All tests pass (should be 1590+).

- [ ] **Step 2: Run Release build**

```bash
dotnet build ./Tokenizer.sln -c Release
```

Expected: Clean build, no warnings.

- [ ] **Step 3: Verify pack**

```bash
dotnet pack ./src/Tokenizer/Tokenizer.csproj -c Release --no-build -o ./artifacts
ls ./artifacts/
rm -rf ./artifacts/
```

Expected: `Tokenizer.3.0.0-beta.1.nupkg` and `Tokenizer.3.0.0-beta.1.snupkg`

- [ ] **Step 4: Scan for dead code**

```bash
# Check for unused using directives
dotnet format style ./Tokenizer.sln --verify-no-changes --diagnostics IDE0005

# Check for unused parameters
dotnet format style ./Tokenizer.sln --verify-no-changes --diagnostics IDE0060

# Check for dead conditional code
dotnet build ./Tokenizer.sln -c Release 2>&1 | grep "CA1508"
```

Fix any issues found before proceeding.

---

### Task 15: Manual release walkthrough

This task is a guide for Christo, not automated steps.

- [ ] **Step 1: Push v3 branch**

```bash
git push origin v3
```

- [ ] **Step 2: Open PR**

```bash
gh pr create --base main --head v3 --title "v3.0.0-beta.1" --body "Squash merge of v3 branch. See CHANGELOG.md for details."
```

- [ ] **Step 3: Update NuGet API secret**

In the GitHub repo settings:
1. Go to Settings > Secrets and variables > Actions
2. Update (or create) `NUGET_API_KEY` with your current NuGet API key
3. You can generate a new key at https://www.nuget.org/account/apikeys

- [ ] **Step 4: Squash merge the PR**

Use the GitHub UI: select "Squash and merge" from the merge dropdown on the PR page.

- [ ] **Step 5: Pull main locally**

```bash
git checkout main
git pull origin main
```

- [ ] **Step 6: Tag and push**

```bash
git tag v3.0.0-beta.1
git push origin v3.0.0-beta.1
```

This triggers the release workflow which will:
- Validate the tag version matches `PackageVersion` in the csproj
- Build and test on Ubuntu and Windows
- Pack the NuGet package
- Push to NuGet.org
- Push to GitHub Packages
- Create a GitHub Release (marked as prerelease because the tag contains `-`)

- [ ] **Step 7: Verify release**

Check that all three published successfully:
- NuGet.org: https://www.nuget.org/packages/Tokenizer/3.0.0-beta.1
- GitHub Packages: Check the repo's Packages tab
- GitHub Releases: Check the repo's Releases tab

- [ ] **Step 8: Clean up**

```bash
# Delete v3 branch remotely
git push origin --delete v3

# Delete v3 branch locally
git branch -D v3
```
