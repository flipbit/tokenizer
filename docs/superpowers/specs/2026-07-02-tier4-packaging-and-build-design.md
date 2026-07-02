# Tier 4: Packaging and Build — Design Spec

## Goal

Bring the NuGet package and CI/CD infrastructure to professional .NET library standards before publishing v3.0.0. Ship XML IntelliSense docs, fix metadata, add a changelog, expand CI coverage, and add an automated release pipeline.

## Scope

1. Enable `GenerateDocumentationFile` and add missing XML doc comments
2. Fix NuGet metadata (HTTPS URL, copyright year, package icon)
3. Add `CHANGELOG.md` covering full release history
4. Expand CI matrix (Ubuntu + Windows) with code coverage reporting
5. Add tag-triggered release workflow (NuGet.org + GitHub Packages + GitHub Release)

## Out of Scope

- Trimming/AOT annotations (`IsTrimmable`, `IsAotCompatible`) — the library relies on reflection for core binding functionality; claiming compatibility without verification would be dishonest
- macOS CI — pure .NET library with no native dependencies; Ubuntu + Windows is sufficient
- `ContinuousIntegrationBuild` as a csproj property — passed as an MSBuild argument in the release workflow instead

---

## 1. XML Documentation

### Current State

- `GenerateDocumentationFile` is not set
- `TreatWarningsAsErrors` is enabled in `Directory.Build.props`
- 1,062 public members across 50 files lack XML doc comments

### Changes

- Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to `Tokenizer.csproj`
- Add XML doc comments to all undocumented public members

### Scope Breakdown

| Category | Approx. Warnings | Key Files |
|----------|------------------|-----------|
| AST/Compilation internals | ~400 | TokenAst.cs, TokenDefinition.cs, SetTokenDirective.cs |
| Core engine | ~210 | TokenMatcher.cs, CandidateTokenList.cs, TokenEnumerator.cs |
| Public API surface | ~150 | Tokenizer.cs, TokenizeResult.cs, TokenizerOptions.cs, Token.cs, Template.cs |
| Transformers/Validators | ~150 | 14 transformer classes, exception types |
| Supporting types | ~150 | Extensions, results, hints, front matter nodes |

### Conventions

- Document **what** and **why**, not implementation details
- Match the voice and style of existing XML docs in the codebase
- Properties: describe what the value represents
- Methods: describe what the method does and what it returns
- Parameters: describe what each parameter controls
- Exceptions: document thrown exceptions with `<exception cref="...">` where applicable

---

## 2. NuGet Metadata Fixes

### Current State

```xml
<PackageProjectUrl>http://github.com/flipbit/tokenizer</PackageProjectUrl>
<Copyright>Chris Wood 2021</Copyright>
<!-- No PackageIcon -->
```

### Changes

- Change `PackageProjectUrl` to `https://github.com/flipbit/tokenizer`
- Update `Copyright` to `Chris Wood 2026`
- Add package icon (see Section 5 below)

---

## 3. CHANGELOG.md

### Format

Follow [Keep a Changelog](https://keepachangelog.com/) convention. Categories: Added, Changed, Fixed, Removed.

### Release History

#### v3.0.0 (Unreleased)

**Added:**
- AST-based template compilation pipeline (TemplateLexer, TemplateParser, AstTemplateDefinitionParser)
- Tokenization diagnostics system with hint generators (PreambleNearMiss, ValidatorValue, UnmatchedInput, RepeatingToken, DateFormat)
- DiagnosticSummaryBuilder and AlignmentRenderer for visual template-input diffs
- Safety limits: MaxInputLength, MaxTemplateLength, MaxTokenCount, MaxIterations
- BenchmarkDotNet benchmark suite (compilation, tokenization, matching, concurrency)
- IOptions<TokenizerOptions> support for DI registration
- IReadOnlyCollection<Template> on TemplateCollection
- Microsoft.Extensions.Logging integration
- SourceLink and symbol package (.snupkg) support
- GitHub Actions CI (replacing AppVeyor)
- CodeQL security analysis
- .NET 10.0 target

**Changed:**
- Renamed `CanTransform` to `TryTransform` on ITokenTransformer
- Renamed `Match` class to `TokenMatch` (now a record)
- Renamed `CandidateTokenList.Any` to `HasCandidates`
- Renamed `TokenEnumerator.Match()` to `TryMatch()`
- Renamed boolean properties on Token: `IsOptional`, `IsRepeating`, `IsRequired`, `ShouldConcatenate`, `ShouldConsiderOnce`
- Standardized `TerminateOnNewLine` casing
- Unified decorator argument exceptions to ArgumentException
- Converted TokenizerOptions to record class with init-only semantics
- Converted Hint to positional record
- Made TokenizeResult<T>.Value init-only
- Made Hint properties init-only
- Narrowed ITokenizationEngine interface (removed implementation details)
- Made Token and exception properties internally settable
- Made public API collections IReadOnlyList<T>
- Sealed all non-extension-point classes
- Converted to file-scoped namespaces
- Replaced Tokenizer.Create() static factories with public constructors
- Target frameworks: netstandard2.0 + net8.0 + net10.0 (was netstandard2.0 + net6.0)

**Fixed:**
- Empty-preamble infinite loop in tokenization engine
- ComputePreamble ignoring template-level TrimLeadingWhitespace override
- Getter-only collection properties throwing on SetValue
- Context-aware quote handling and repeating token ordering
- Unknown escape sequences in quoted strings treated as literals
- Frontmatter-only matches excluded from Success when template has real tokens
- Internal whitespace preserved in hint text during front matter parsing
- All nullable reference type warnings resolved
- XML doc copy-paste errors in validators and transformers

**Removed:**
- `TokenizerOptions.Defaults` static property (use `new TokenizerOptions()`)
- `TokenizerOptions.Clone()` method (use `with` expressions)
- `EnableLogging` flag, `EnableLineByLineLogging`, and `LineTracker`
- Duplicate test files
- Stale `#if DOTNET35` and `NET6_0_OR_GREATER` preprocessor guards
- AppVeyor CI configuration

#### v2.2.1

**Fixed:**
- Reduced excessive log levels

#### v2.2.0

**Added:**
- Split() transformer
- Thread-safe TemplateCollection for concurrent access
- Documentation tests

**Fixed:**
- Made multiple template matching deterministic

#### v2.1.10

**Fixed:**
- IsPhoneNumber validator

#### v2.1.9

**Added:**
- Not validators (IsNotNull, IsNotEmpty, etc.)
- SubstringBeforeLast and SubstringAfterLast transformers

#### v2.1.8

**Added:**
- Multiple token consideration during Tokenize() operation
- TerminateOnNewLine front matter option
- Replace() transformer

**Fixed:**
- Infinite loop in initial token with no preamble
- Token transformer return value indicating transformation success

#### v2.1.2

**Added:**
- Multiline token support
- Long-form token modifiers
- Set transformer for setting token values
- Template tags for restricting matches
- Shorthand set token value assignment in front matter
- Enum and Boolean value assignment
- IsNotEmpty validator
- Token preamble trimming before last newline

**Fixed:**
- Repeating token matching
- Token.CanAssign() method
- Front matter token matching when no content tokens found

#### v2.1.0

**Added:**
- Result object returned from Tokenize operation
- Required fields support
- Token validators
- Template hints for selecting best match
- Front matter template naming
- Escaped `{` and `}` characters in templates
- DateTime parsing up to newline

**Fixed:**
- Invalid type conversion handling
- Token assignment failures no longer throw

#### v2.0.6

**Added:**
- UTC DateTime token transformer

#### v2.0.5

**Fixed:**
- Various bug fixes
- Added logging support

#### v2.0.0

**Added:**
- State machine-based token parser
- .NET Standard 2.0 and .NET Framework 4.5.2 support
- Transformers (value transformation pipeline)
- Newline handling in pattern parsing

**Changed:**
- Complete rewrite of parsing engine from regex to state machine
- Changed repeating token flag to `*`

---

## 4. CI Matrix Expansion

### Current State (`build-and-test.yml`)

- Trigger: push to master/v3, PRs to master
- Single OS: `ubuntu-latest`
- .NET SDKs: 8.0.x, 10.0.x

### Changes

**OS Matrix:**
```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest]
```

**Code Coverage:**
- Add `coverlet.collector` to test project (if not already present) or use Coverlet MSBuild
- Collect coverage during `dotnet test` with Cobertura output format
- Use a coverage report action to parse Cobertura XML and write a summary table to the GitHub Actions job summary
- No external service (Codecov, etc.) — self-contained in GitHub

### Resulting Workflow Shape

```
Trigger: push to master/v3, PRs to master
Matrix: [ubuntu-latest, windows-latest] × [8.0.x, 10.0.x]
Steps:
  1. Checkout
  2. Setup .NET (8.0.x + 10.0.x)
  3. Restore
  4. Build
  5. Test with coverage collection
  6. Generate coverage summary (ubuntu-latest only, to avoid duplicate reports)
```

---

## 5. Package Icon

### Approach

Use the **BracketsCurly** icon from the Phosphor icon pack (bold weight for visibility at small sizes). Place it on a solid colored background (rounded square with slight corner radius) for consistent rendering across light and dark themes.

### Format

- 128×128 PNG
- Located at `src/Tokenizer/icon.png`
- Packed into NuGet package root via:
  ```xml
  <PackageIcon>icon.png</PackageIcon>
  ```
  ```xml
  <None Include="icon.png" Pack="true" PackagePath="\" />
  ```

### Color Options to Explore

| Option | Background | Icon | Vibe |
|--------|-----------|------|------|
| A | Deep indigo (#3730A3) | White | Professional, stands out in NuGet gallery |
| B | Emerald (#059669) | White | Fresh, distinctive among .NET packages |
| C | Slate (#334155) | Amber (#F59E0B) | Subtle, pairs well with code/tooling aesthetic |
| D | White (#FFFFFF) | Indigo (#4F46E5) | Clean, but may blend on light backgrounds |

Final color selection during implementation.

---

## 6. Release Workflow

### New file: `.github/workflows/release.yml`

Mirrors the proven pattern from the whois repository.

### Trigger

```yaml
on:
  push:
    tags: ['v*']
```

### Jobs

**1. Validate Version**
- Extract version from git tag (strip `v` prefix)
- Read `<Version>` from `Tokenizer.csproj`
- Fail if they don't match

**2. Build and Test**
- Full matrix: [ubuntu-latest, windows-latest] × [8.0.x, 10.0.x]
- Same steps as CI build

**3. Pack and Publish**
- Depends on validate + build-and-test
- `dotnet build -c Release /p:ContinuousIntegrationBuild=true`
- `dotnet pack -c Release --no-build -o ./artifacts`
- Push `.nupkg` to NuGet.org via `secrets.NUGET_API_KEY`
- Push `.nupkg` to GitHub Packages via `secrets.GITHUB_TOKEN`
- Create GitHub Release with auto-generated notes and attached packages

### Permissions

```yaml
permissions:
  contents: write
  packages: write
```

### Prerequisites

- `NUGET_API_KEY` secret must be configured in the repository settings before first use

---

## Commit Strategy

All work lands as direct commits on the `v3` branch:

1. **csproj + metadata fixes** — `GenerateDocumentationFile`, HTTPS URL, copyright, icon config
2. **XML doc comments** — all 1,062 members (may be split across multiple commits by area)
3. **Package icon** — icon.png file
4. **CHANGELOG.md** — full release history
5. **CI matrix expansion** — build-and-test.yml updates with coverage
6. **Release workflow** — new release.yml

---

## Risks

| Risk | Mitigation |
|------|-----------|
| Enabling `GenerateDocumentationFile` with `TreatWarningsAsErrors` breaks the build until all docs are added | Add doc comments before or in the same commit as enabling the flag |
| Release workflow requires `NUGET_API_KEY` secret | Document as prerequisite; workflow will fail gracefully with clear error if missing |
| Code coverage tooling may behave differently on Windows vs Linux | Generate coverage report on Ubuntu only to avoid duplicate/conflicting reports |
| Package icon may not render well at very small sizes (16×16) | Use bold weight icon, test at multiple scales during implementation |
