# v3.0.0-beta.1 Release Preparation

Design spec for preparing the v3 branch for merge to main and first beta release.

## Context

The v3 branch has 533 commits covering a major rewrite: AST-based compilation pipeline, new validators/transformers, streaming support, diagnostics, performance optimizations, and significant API changes. The branch also accumulated ~80 AI-generated spec/plan files, a debug app, and stale benchmark artifacts that need cleaning before merge.

The goal is to squash-merge v3 into a renamed `main` branch, tag `v3.0.0-beta.1`, and trigger the release pipeline to publish to NuGet and GitHub.

## Phase 1: Stabilize Working State

Three uncommitted changes need review and tests before committing:

1. **`AssignmentFailedException.PartialResult`** -- New `object? PartialResult` property, set during `Assign<T>()` when some assignments fail. Allows callers to retrieve successfully assigned values when handling the exception.

2. **`TemporalParser` offset fix** -- Two changes:
   - Trim whitespace from `SubstringBeforeNewLine()` result before parsing
   - Skip `ApplyDefaultOffset` when the format string contains an explicit offset specifier (`z`, `zz`, `zzz`, `K`), so parsed offsets from the data are not overridden by defaults

3. **`TokenizeResult.Assign<T>()`** -- Attaches partial result to `AssignmentFailedException`

Tests needed:
- `AssignmentFailedException`: verify `PartialResult` is populated when assignment partially fails
- `TemporalParser.FormatContainsOffset`: true for formats with `z`/`K`, false for formats without
- `TemporalParser.TryParse`: when format has offset specifier, parsed offset is preserved (not replaced by default)
- `TemporalParser.TryParse`: whitespace around date values is tolerated

## Phase 2: Clean Up Artifacts

Remove all AI-generated development artifacts from tracking:

- **Delete `docs/` directory** -- 80+ spec/plan files, ROADMAP.md (completed items already in CHANGELOG)
- **Delete `specs/` directory** -- PRDs and task breakdowns from early v3 development
- **Delete `debug_token_app/`** -- Dev-only console app, remove from `Tokenizer.sln`
- **Delete `benchmark-results/`** -- Stale results in wrong location
- **Delete `benchmarks/baselines/streaming-input/`** -- Non-standard baseline directory name
- **Remove `Tokenizer.sln.DotSettings.user`** from git tracking
- **Update `.gitignore`** -- Add `benchmark-results/`, `*.DotSettings.user`, `debug_token_app/`

Also remove the root-level `Tokenizer/` and `Tokenizer.Tests/` directories if they contain only build artifacts (bin/obj).

## Phase 3: Repo Structure

### ARCHITECTURE.md

Extract the architecture section from CLAUDE.md into a standalone `ARCHITECTURE.md` at the repo root. Covers:

- Compilation pipeline (lexer, parser, AST, front matter, compiler, decorator registry)
- Tokenization engine (engine, hint processor, result builder, context)
- Extension points (transformers, validators)
- Entry points

### AGENTS.md / CLAUDE.md restructure

- Create root `AGENTS.md` with the full agent instructions (current CLAUDE.md content, minus architecture which moves to ARCHITECTURE.md, plus a reference to it)
- Update root `CLAUDE.md` to just `@AGENTS.md`
- Create `tests/AGENTS.md` + `tests/CLAUDE.md` with test-specific conventions (framework, naming, builders, helpers, file naming)
- Create `benchmarks/AGENTS.md` + `benchmarks/CLAUDE.md` with benchmark conventions (baseline location `baselines/{yyyy-MM-dd}`, how to run, how to compare)

### LICENSE.txt

- Move from `src/Tokenizer/LICENSE.txt` to repo root
- Update csproj to reference `../../LICENSE.txt`

### New files

- **`global.json`** -- Pin .NET SDK version (10.0.x)
- **`CONTRIBUTING.md`** -- Build instructions, test conventions, PR process
- **`.github/ISSUE_TEMPLATE/bug_report.md`** -- Bug report template
- **`.github/ISSUE_TEMPLATE/feature_request.md`** -- Feature request template
- **`.github/PULL_REQUEST_TEMPLATE.md`** -- PR checklist template

## Phase 4: Update Documentation

### README.md

Full rewrite with v3 API. Sections:

1. Badges and one-line description
2. Installation (`dotnet add package Tokenizer --version 3.0.0-beta.1`)
3. Quick start (basic pattern matching with `Tokenize<T>`)
4. Features with examples:
   - In-order vs out-of-order processing
   - Line handling and multiline tokens
   - Newline termination (`$`)
   - Repeating tokens (`*`)
   - Required fields (`!`)
   - Optional fields (`?`)
   - Configuration (constructor, front matter)
   - Data transformers (with chaining)
   - Data validators
   - Async/streaming tokenization
   - Template compilation and caching
   - Diagnostics
   - DI registration
5. Built-in transformers and validators (table)
6. Custom transformers/validators (brief example)
7. Configuration reference (front matter options)
8. Contributing link
9. License

### CHANGELOG.md

- Add entries for the uncommitted changes (PartialResult, TemporalParser offset fix)
- Set version header to `[3.0.0-beta.1]`

## Phase 5: Branch Rename and CI

### Rename master to main

Via `gh` CLI:
```
gh repo edit --default-branch main
```

Update local:
```
git branch -m master main
git fetch origin
git branch -u origin/main main
```

### CI updates

- `.github/workflows/build.yml` -- Change `master` to `main` in branch triggers
- `.github/workflows/codeql.yml` -- Change `master` to `main`
- Add prerelease detection to `.github/workflows/release.yml`:
  ```yaml
  gh release create "${{ github.ref_name }}" \
    ./artifacts/*.nupkg \
    --title "${{ github.ref_name }}" \
    --generate-notes \
    ${{ contains(github.ref_name, '-') && '--prerelease' || '' }}
  ```

## Phase 6: Release Prep

- Set `PackageVersion` to `3.0.0-beta.1` in csproj
- Run full benchmark suite, save results to `benchmarks/baselines/2026-08-08/`
- Run `dotnet build -c Release` and `dotnet test` to validate
- Scan for dead code and obvious defects

## Phase 7: Manual Release Steps

After all automated work is done, walk Christo through:

1. Push v3 branch to origin
2. Open PR (v3 -> main) with squash merge
3. Update NuGet API secret in GitHub repo settings
4. Squash merge the PR
5. Pull main locally
6. Tag `v3.0.0-beta.1`
7. Push tag to trigger release workflow
8. Verify NuGet, GitHub Packages, and GitHub Release published correctly
9. Delete v3 branch (remote and local)

## Out of Scope

- Tier 8 roadmap items (architecture/extensibility)
- Trimming/AOT annotations (descoped due to reflection usage)
- New features or API changes beyond the 3 uncommitted fixes
