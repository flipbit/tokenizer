# Tier 4: Packaging and Build — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the NuGet package and CI/CD infrastructure to professional standards before publishing v3.0.0 — ship XML IntelliSense docs, fix metadata, add a changelog, expand CI, and add an automated release pipeline.

**Architecture:** csproj property changes for documentation and metadata, a new `release.yml` GitHub Actions workflow mirroring the whois repo pattern, CI matrix expansion with Coverlet code coverage, and a Keep a Changelog format `CHANGELOG.md`.

**Tech Stack:** MSBuild, GitHub Actions, Coverlet (code coverage), Phosphor Icons (package icon)

---

### Task 1: Enable `GenerateDocumentationFile` and Fix NuGet Metadata

**Files:**
- Modify: `src/Tokenizer/Tokenizer.csproj`

All public members already have XML doc comments (zero CS1591 warnings verified). This task just enables the flag and fixes metadata.

- [ ] **Step 1: Verify the build passes with `GenerateDocumentationFile` before making changes**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release /p:GenerateDocumentationFile=true`
Expected: Build succeeds with 0 warnings, 0 errors.

- [ ] **Step 2: Add `GenerateDocumentationFile` to the csproj**

In `src/Tokenizer/Tokenizer.csproj`, add to the first `<PropertyGroup>` (after the `<EmbedUntrackedSources>` line):

```xml
   <GenerateDocumentationFile>true</GenerateDocumentationFile>
```

- [ ] **Step 3: Fix `PackageProjectUrl` to use HTTPS**

In `src/Tokenizer/Tokenizer.csproj`, change:

```xml
   <PackageProjectUrl>http://github.com/flipbit/tokenizer</PackageProjectUrl>
```

to:

```xml
   <PackageProjectUrl>https://github.com/flipbit/tokenizer</PackageProjectUrl>
```

- [ ] **Step 4: Update `Copyright` year**

In `src/Tokenizer/Tokenizer.csproj`, change:

```xml
   <Copyright>Chris Wood 2024</Copyright>
```

to:

```xml
   <Copyright>Chris Wood 2026</Copyright>
```

- [ ] **Step 5: Add `PackageIcon` property and pack item**

In `src/Tokenizer/Tokenizer.csproj`, add to the first `<PropertyGroup>`:

```xml
   <PackageIcon>icon.png</PackageIcon>
```

Add a new `<ItemGroup>` after the existing `<None Include="../../README.md" ...>` ItemGroup:

```xml
  <ItemGroup>
    <None Include="icon.png" Pack="true" PackagePath="\" />
  </ItemGroup>
```

Note: The actual `icon.png` file will be created in Task 3. The build will warn about the missing file until then, but won't fail since `PackageIcon` is metadata-only.

- [ ] **Step 6: Verify the build still passes**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds. There may be a warning about missing `icon.png` — that's expected until Task 3.

- [ ] **Step 7: Verify XML doc file is generated**

Check that these files exist after the build:
- `src/Tokenizer/bin/Release/net10.0/Tokenizer.xml`
- `src/Tokenizer/bin/Release/net8.0/Tokenizer.xml`
- `src/Tokenizer/bin/Release/netstandard2.0/Tokenizer.xml`

- [ ] **Step 8: Commit**

```bash
git add src/Tokenizer/Tokenizer.csproj
git commit -m "Enable GenerateDocumentationFile, fix NuGet metadata"
```

---

### Task 2: Add Coverlet to Test Project

**Files:**
- Modify: `tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Add `coverlet.collector` so CI can collect code coverage data.

- [ ] **Step 1: Add Coverlet package reference**

In `tests/Tokenizer.Tests/Tokenizer.Tests.csproj`, add to the `<ItemGroup>` containing other `<PackageReference>` elements:

```xml
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
```

- [ ] **Step 2: Verify tests still pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --verbosity normal`
Expected: All tests pass.

- [ ] **Step 3: Verify coverage collection works**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --collect:"XPlat Code Coverage" --results-directory ./TestResults`
Expected: Tests pass and a `coverage.cobertura.xml` file is created under `./TestResults/`.

- [ ] **Step 4: Clean up test results**

```bash
rm -rf ./TestResults
```

- [ ] **Step 5: Commit**

```bash
git add tests/Tokenizer.Tests/Tokenizer.Tests.csproj
git commit -m "Add coverlet.collector for code coverage reporting"
```

---

### Task 3: Create Package Icon

**Files:**
- Create: `src/Tokenizer/icon.png`

Create a 128×128 PNG using the Phosphor BracketsCurly bold icon on a solid colored background.

- [ ] **Step 1: Download the Phosphor BracketsCurly bold SVG**

Download from Phosphor's GitHub. The bold variant of BracketsCurly provides the best visibility at small sizes.

```bash
curl -sL "https://raw.githubusercontent.com/phosphor-icons/core/main/assets/bold/brackets-curly-bold.svg" -o /tmp/brackets-curly-bold.svg
```

- [ ] **Step 2: Create the icon**

Using an image tool (ImageMagick, Figma, or a script), compose a 128×128 PNG:
- Solid rounded-rectangle background with slight corner radius (~16px)
- BracketsCurly bold icon centered, sized to ~80px with padding
- Color options from spec — pick one:
  - **Option A:** Deep indigo (#3730A3) background, white icon — professional
  - **Option B:** Emerald (#059669) background, white icon — fresh, distinctive
  - **Option C:** Slate (#334155) background, amber (#F59E0B) icon — code/tooling aesthetic
  - **Option D:** White (#FFFFFF) background, indigo (#4F46E5) icon — clean (but may blend on light backgrounds)

Present all options to Christo for selection before finalizing.

- [ ] **Step 3: Save the icon**

Save the final icon to `src/Tokenizer/icon.png`.

- [ ] **Step 4: Verify the build succeeds with the icon**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with 0 warnings (the missing icon warning from Task 1 should be resolved).

- [ ] **Step 5: Verify the icon is packed into the NuGet package**

Run: `dotnet pack ./src/Tokenizer/Tokenizer.csproj -c Release --no-build -o ./artifacts`
Then inspect: `dotnet nuget locals global-packages -l` or unzip the `.nupkg` and verify `icon.png` is at the root.

```bash
unzip -l ./artifacts/Tokenizer.3.0.0.nupkg | grep icon
rm -rf ./artifacts
```

Expected: `icon.png` appears in the package listing.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/icon.png
git commit -m "Add package icon (Phosphor BracketsCurly)"
```

---

### Task 4: Add CHANGELOG.md

**Files:**
- Create: `CHANGELOG.md`

Full release history from v2.0.0 through v3.0.0 in Keep a Changelog format.

- [ ] **Step 1: Create the changelog**

Create `CHANGELOG.md` at the repository root with this content:

```markdown
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
```

- [ ] **Step 2: Verify the file looks correct**

Open `CHANGELOG.md` and verify formatting is correct — headings, lists, and links render properly.

- [ ] **Step 3: Commit**

```bash
git add CHANGELOG.md
git commit -m "Add CHANGELOG.md with full release history"
```

---

### Task 5: Expand CI Matrix and Add Code Coverage

**Files:**
- Modify: `.github/workflows/build-and-test.yml`

Add Windows to the OS matrix and add Coverlet code coverage reporting to the GitHub Actions job summary.

- [ ] **Step 1: Replace the current workflow**

Replace the full contents of `.github/workflows/build-and-test.yml` with:

```yaml
name: Build and Test

on:
  push:
    branches: [ master, v3 ]
  pull_request:
    branches: [ master ]

jobs:
  build:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest]
    runs-on: ${{ matrix.os }}

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: |
          8.0.x
          10.0.x

    - name: Restore
      run: dotnet restore Tokenizer.sln

    - name: Build
      run: dotnet build Tokenizer.sln --no-restore

    - name: Test
      run: dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./TestResults

    - name: Generate Coverage Report
      if: matrix.os == 'ubuntu-latest'
      uses: danielpalme/ReportGenerator-GitHub-Action@5
      with:
        reports: ./TestResults/**/coverage.cobertura.xml
        targetdir: ./CoverageReport
        reporttypes: MarkdownSummaryGithub

    - name: Write Coverage to Job Summary
      if: matrix.os == 'ubuntu-latest'
      run: cat ./CoverageReport/SummaryGithub.md >> $GITHUB_STEP_SUMMARY
```

- [ ] **Step 2: Verify the workflow YAML is valid**

```bash
cat .github/workflows/build-and-test.yml | python3 -c "import sys, yaml; yaml.safe_load(sys.stdin)" && echo "Valid YAML"
```

Expected: "Valid YAML"

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/build-and-test.yml
git commit -m "Expand CI matrix to Ubuntu + Windows, add code coverage reporting"
```

---

### Task 6: Add Release Workflow

**Files:**
- Create: `.github/workflows/release.yml`

Tag-triggered release pipeline: validate version, build/test on full matrix, pack and publish to NuGet.org + GitHub Packages + GitHub Release.

- [ ] **Step 1: Create the release workflow**

Create `.github/workflows/release.yml` with this content:

```yaml
name: Release

on:
  push:
    tags: ['v*']

permissions:
  contents: write
  packages: write

jobs:
  validate:
    name: Validate Version
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.version.outputs.version }}
    steps:
      - uses: actions/checkout@v4

      - name: Extract version from tag
        id: version
        run: |
          TAG_VERSION="${GITHUB_REF_NAME#v}"
          echo "version=$TAG_VERSION" >> "$GITHUB_OUTPUT"

      - name: Read version from csproj
        id: csproj
        run: |
          CSPROJ_VERSION=$(grep -oPm1 '(?<=<PackageVersion>)[^<]+' src/Tokenizer/Tokenizer.csproj)
          echo "version=$CSPROJ_VERSION" >> "$GITHUB_OUTPUT"

      - name: Validate versions match
        run: |
          echo "Tag version: ${{ steps.version.outputs.version }}"
          echo "PackageVersion: ${{ steps.csproj.outputs.version }}"
          if [ "${{ steps.version.outputs.version }}" != "${{ steps.csproj.outputs.version }}" ]; then
            echo "::error::Tag version (${{ steps.version.outputs.version }}) does not match PackageVersion (${{ steps.csproj.outputs.version }})"
            exit 1
          fi

  build-and-test:
    name: Build and Test (${{ matrix.os }})
    needs: validate
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            10.0.x

      - name: Restore
        run: dotnet restore Tokenizer.sln

      - name: Build
        run: dotnet build Tokenizer.sln --no-restore

      - name: Test
        run: dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --no-build --verbosity normal

  pack-and-publish:
    name: Pack and Publish
    needs: [validate, build-and-test]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            10.0.x

      - name: Restore
        run: dotnet restore src/Tokenizer/Tokenizer.csproj

      - name: Build Release
        run: dotnet build src/Tokenizer/Tokenizer.csproj -c Release --no-restore /p:ContinuousIntegrationBuild=true

      - name: Pack
        run: dotnet pack src/Tokenizer/Tokenizer.csproj -c Release --no-build -o ./artifacts

      - name: Push to NuGet.org
        run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate

      - name: Push to GitHub Packages
        run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.GITHUB_TOKEN }} --source https://nuget.pkg.github.com/flipbit/index.json --skip-duplicate

      - name: Create GitHub Release
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          gh release create "${{ github.ref_name }}" \
            ./artifacts/*.nupkg \
            --title "${{ github.ref_name }}" \
            --generate-notes
```

- [ ] **Step 2: Verify the workflow YAML is valid**

```bash
cat .github/workflows/release.yml | python3 -c "import sys, yaml; yaml.safe_load(sys.stdin)" && echo "Valid YAML"
```

Expected: "Valid YAML"

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/release.yml
git commit -m "Add tag-triggered release workflow for NuGet publishing"
```

---

### Task 7: Update ROADMAP.md

**Files:**
- Modify: `docs/ROADMAP.md`

Check off completed Tier 4 items (all except trimming/AOT which was descoped).

- [ ] **Step 1: Update the Tier 4 checklist**

In `docs/ROADMAP.md`, update the Tier 4 section to:

```markdown
## Tier 4: Packaging and Build

Ensure the NuGet package meets the bar for a professional .NET library.

- [x] **Enable `GenerateDocumentationFile`** — IntelliSense XML docs must ship in the package
- [x] **Add `ContinuousIntegrationBuild` property** — conditional on CI for deterministic/reproducible builds
- [x] **Fix NuGet metadata** — add `PackageIcon`, change `PackageProjectUrl` to HTTPS, update copyright year
- [ ] **Add trimming/AOT annotations** — `IsTrimmable` and `IsAotCompatible` on net8.0+ targets (descoped: library uses reflection for core binding)
- [x] **Expand CI matrix** — add Windows/macOS, add code coverage reporting
- [x] **Add `CHANGELOG.md`** — release notes convention for v3.0.0
```

- [ ] **Step 2: Commit**

```bash
git add docs/ROADMAP.md
git commit -m "Update ROADMAP.md: mark Tier 4 items complete"
```

---

### Task 8: Final Verification

**Files:** None (verification only)

- [ ] **Step 1: Run the full build**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```

Expected: Build succeeds, 0 warnings, 0 errors.

- [ ] **Step 2: Run all tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --verbosity normal
```

Expected: All tests pass.

- [ ] **Step 3: Verify NuGet package contents**

```bash
dotnet pack ./src/Tokenizer/Tokenizer.csproj -c Release --no-build -o ./artifacts
unzip -l ./artifacts/Tokenizer.3.0.0.nupkg | grep -E "icon\.png|Tokenizer\.xml|README\.md|LICENSE\.txt"
rm -rf ./artifacts
```

Expected: All four files appear in the package:
- `icon.png`
- `lib/net10.0/Tokenizer.xml` (and net8.0, netstandard2.0 variants)
- `README.md`
- `LICENSE.txt`

- [ ] **Step 4: Verify coverage collection works locally**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --collect:"XPlat Code Coverage" --results-directory ./TestResults
ls ./TestResults/*/coverage.cobertura.xml
rm -rf ./TestResults
```

Expected: Coverage file is generated.

- [ ] **Step 5: Verify YAML workflows are valid**

```bash
cat .github/workflows/build-and-test.yml | python3 -c "import sys, yaml; yaml.safe_load(sys.stdin)" && echo "build-and-test.yml: Valid"
cat .github/workflows/release.yml | python3 -c "import sys, yaml; yaml.safe_load(sys.stdin)" && echo "release.yml: Valid"
```

Expected: Both print "Valid".

- [ ] **Step 6: Review git log**

```bash
git log --oneline -10
```

Verify the commit history looks clean and matches the expected sequence from this plan.

---

## Prerequisites Checklist

Before the first release, the repository must have:

- [ ] `NUGET_API_KEY` secret configured in GitHub repository settings (Settings → Secrets and variables → Actions)
- [ ] Icon color option selected and icon.png created
