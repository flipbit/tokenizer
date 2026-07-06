# Code Style Enforcement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add automated code style enforcement via `.editorconfig` and Roslyn analyzers so violations break the build, then fix all existing violations rule-by-rule with one commit per rule.

**Architecture:** Expand existing `.editorconfig` with diagnostic severities, enable `EnforceCodeStyleInBuild` in `Directory.Build.props`, add `Meziantou.Analyzer`. Each rule violation is fixed and committed independently.

**Tech Stack:** .NET SDK built-in analyzers, Meziantou.Analyzer 3.0.121, `dotnet format` CLI

## Global Constraints

- Targets: netstandard2.0, net8.0, net10.0 (dual/triple-targeting)
- `TreatWarningsAsErrors=true` already in `Directory.Build.props` — any warning-severity rule breaks the build
- All tests must pass before each commit
- One commit per rule fix, with the rule ID in the commit message
- Private field naming convention: `_camelCase` (underscore prefix)
- Constants and static readonly fields: `PascalCase`
- xunit.analyzers already included transitively via the `xunit` package — do not add explicitly

---

### Task 1: Add Analyzer Configuration

**Files:**
- Modify: `.editorconfig` (replace entire `[*.cs]` section)
- Modify: `Directory.Build.props` (add properties and Meziantou.Analyzer package)

- [ ] **Step 1: Update `.editorconfig`**

Replace the entire `[*.cs]` section in `.editorconfig` with the expanded rules. The full file should be:

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
# Formatting
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true

# File-scoped namespace declarations
csharp_style_namespace_declarations = file_scoped:warning
dotnet_diagnostic.IDE0161.severity = warning

# Remove unnecessary usings
dotnet_diagnostic.IDE0005.severity = warning

# Do not require 'this.' qualification
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning

# Require explicit accessibility modifiers
dotnet_style_require_accessibility_modifiers = always:warning
dotnet_diagnostic.IDE0040.severity = warning

# Remove unused parameters
dotnet_diagnostic.IDE0060.severity = warning

# Formatting
dotnet_diagnostic.IDE0055.severity = warning

# Naming conventions
dotnet_diagnostic.IDE1006.severity = warning

# --- Naming rules (order matters: more specific rules first) ---

# Constants: PascalCase
dotnet_naming_style.pascal_case.capitalization = pascal_case

dotnet_naming_rule.constants.symbols = constant_symbols
dotnet_naming_rule.constants.style = pascal_case
dotnet_naming_rule.constants.severity = warning
dotnet_naming_symbols.constant_symbols.applicable_kinds = field
dotnet_naming_symbols.constant_symbols.required_modifiers = const

# Static readonly fields: PascalCase
dotnet_naming_rule.static_readonly.symbols = static_readonly_symbols
dotnet_naming_rule.static_readonly.style = pascal_case
dotnet_naming_rule.static_readonly.severity = warning
dotnet_naming_symbols.static_readonly_symbols.applicable_kinds = field
dotnet_naming_symbols.static_readonly_symbols.required_modifiers = static, readonly

# Interfaces: IPascalCase
dotnet_naming_style.interface_style.required_prefix = I
dotnet_naming_style.interface_style.capitalization = pascal_case

dotnet_naming_rule.interfaces.symbols = interface_symbols
dotnet_naming_rule.interfaces.style = interface_style
dotnet_naming_rule.interfaces.severity = warning
dotnet_naming_symbols.interface_symbols.applicable_kinds = interface
dotnet_naming_symbols.interface_symbols.applicable_accessibilities = *

# Public members: PascalCase
dotnet_naming_rule.public_members.symbols = public_symbols
dotnet_naming_rule.public_members.style = pascal_case
dotnet_naming_rule.public_members.severity = warning
dotnet_naming_symbols.public_symbols.applicable_kinds = property, method, event
dotnet_naming_symbols.public_symbols.applicable_accessibilities = public

# Private fields: _camelCase
dotnet_naming_style.underscore_camel_case.required_prefix = _
dotnet_naming_style.underscore_camel_case.capitalization = camel_case

dotnet_naming_rule.private_fields.symbols = private_field_symbols
dotnet_naming_rule.private_fields.style = underscore_camel_case
dotnet_naming_rule.private_fields.severity = warning
dotnet_naming_symbols.private_field_symbols.applicable_kinds = field
dotnet_naming_symbols.private_field_symbols.applicable_accessibilities = private, protected, private_protected

# --- Quality rules (CA) ---

# Use nameof instead of string literals
dotnet_diagnostic.CA1507.severity = warning

# Avoid dead conditional code
dotnet_diagnostic.CA1508.severity = warning

# Avoid zero-length array allocations
dotnet_diagnostic.CA1825.severity = warning

# Forward CancellationToken
dotnet_diagnostic.CA2016.severity = warning

# Rethrow to preserve stack details
dotnet_diagnostic.CA2200.severity = warning

# Disposable fields should be disposed
dotnet_diagnostic.CA2213.severity = warning

[*.{xml,csproj,props}]
indent_size = 2
```

- [ ] **Step 2: Update `Directory.Build.props`**

The file currently contains:

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

Replace with:

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-Recommended</AnalysisLevel>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Meziantou.Analyzer" Version="3.0.121">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Restore packages**

Run: `dotnet restore Tokenizer.sln`

Expected: Restore succeeds, Meziantou.Analyzer is downloaded.

- [ ] **Step 4: Verify configuration loads**

Run: `dotnet build Tokenizer.sln 2>&1 | head -50`

Expected: Build will FAIL with many warnings-as-errors. This is correct — it proves the rules are active. Skim the output to confirm you see IDE and CA diagnostic codes.

- [ ] **Step 5: Commit configuration**

```bash
git add .editorconfig Directory.Build.props
git commit -m "style: add code style enforcement via .editorconfig and analyzers"
```

---

### Task 2: Fix IDE0005 — Remove Unused Using Statements

**Files:**
- Modify: Any .cs files with unused usings (the audit found none, but the analyzers may catch some the manual scan missed)

- [ ] **Step 1: Auto-fix with dotnet format**

Run: `dotnet format style ./Tokenizer.sln --diagnostics IDE0005`

- [ ] **Step 2: Verify build**

Run: `dotnet build Tokenizer.sln 2>&1 | grep IDE0005`

Expected: No IDE0005 warnings. If any remain, fix manually.

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A  # safe here — only using removals
git commit -m "style: fix IDE0005 — remove unused using statements"
```

Skip if no changes.

---

### Task 3: Fix IDE1006 — Standardize Private Field Naming to _camelCase

This is the largest task. 101 private fields across ~50 files need renaming from `fieldName` to `_fieldName`. All references to each field must also be updated.

**Files:**
- Modify: All files listed below (src/ and tests/)

**src/ files (58 fields):**
- `src/Tokenizer/Template.cs:9-11` — `tokens`, `hints`, `tags`
- `src/Tokenizer/Token.cs:14` — `content`
- `src/Tokenizer/Tokenizer.cs:22-25` — `parser`, `log`, `tokenizationEngine`, `resultBuilder`
- `src/Tokenizer/TokenMatcher.cs:17-18` — `tokenizer`, `log`
- `src/Tokenizer/TokenizerOptions.cs:13-14` — `transformers`, `validators`
- `src/Tokenizer/TemplateCollection.cs:11` — `templates`
- `src/Tokenizer/CandidateTokenList.cs:12` — `tokens`
- `src/Tokenizer/Enumerators/FileLocation.cs:8` — `newLineCounter`
- `src/Tokenizer/Enumerators/TokenEnumerator.cs:17-27` — `reader`, `originalString`, `buffer`, `stagingBuffer`, `readPos`, `writePos`, `bufferedCount`, `readerExhausted`, `resetNextLine`
- `src/Tokenizer/Compilation/TemplateCompiler.cs:17` — `registry`
- `src/Tokenizer/Compilation/Definitions/TokenDefinition.cs:12-14` — `preamble`, `name`, `value`
- `src/Tokenizer/Compilation/Definitions/DecoratorDefinition.cs:10` — `name`
- `src/Tokenizer/Compilation/Parsing/TemplateParser.cs:12` — `lexer`
- `src/Tokenizer/Compilation/Parsing/TokenReader.cs:13-14` — `enumerator`, `buffer`
- `src/Tokenizer/Compilation/Parsing/TemplateDefinitionEnumerator.cs:8-12` — `pattern`, `patternLength`, `currentLocation`, `resetNextLine`
- `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs:37` — `log`
- `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs:53-59` — nested class fields: `inner`, `buffer`, `startIndex`, `length`, `buffer` (conditional)
- `src/Tokenizer/Tokenization/HintProcessor.cs:14` — `log`
- `src/Tokenizer/Tokenization/ResultBuilder.cs:14` — `log`
- `src/Tokenizer/Tokenization/TokenizationEngine.cs:13` — `log`
- `src/Tokenizer/Tokenization/TokenMatchRouter.cs:12-15` — `template`, `candidateProcessor`, `collector`, `hintStrategy`
- `src/Tokenizer/Tokenization/TokenizationSession.cs:16-23` — `template`, `targetObject`, `result`, `collector`, `router`, `candidateProcessor`, `hasExplicitLimit`, `iterationCount`
- `src/Tokenizer/Tokenization/CandidateProcessor.cs:14-18` — `targetObject`, `result`, `template`, `collector`, `logger`
- `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs:13-14` — `currentTemplate`, `matchedPreambles`
- `src/Tokenizer/Diagnostics/DiagnosticCollector.cs:11` — `diagnostics`
- `src/Tokenizer/Diagnostics/DiagnosticResult.cs:21-24` — `templateContent`, `inputContent`, `summary`, `alignment`

**tests/ files (43 fields):**
- `tests/Tokenizer.Tests/TokenTests.cs:16` — `token`
- `tests/Tokenizer.Tests/TokenMatcherTests.cs:9` — `matcher`
- `tests/Tokenizer.Tests/TokenizerTests.cs:11` — `tokenizer`
- `tests/Tokenizer.Tests/SampleTests.cs:8` — `tokenizer`
- `tests/Tokenizer.Tests/ConcatenationTests.cs:8` — `tokenizer`
- `tests/Tokenizer.Tests/AllocationOptimizationTests.cs:9` — `tokenizer`
- `tests/Tokenizer.Tests/SplitTests.cs:8` — `tokenizer`
- `tests/Tokenizer.Tests/ListTests.cs:8` — `tokenizer`
- `tests/Tokenizer.Tests/HintTests.cs:8` — `tokenizer`
- `tests/Tokenizer.Tests/MultilineTests.cs:8` — `tokenizer`
- `tests/Tokenizer.Tests/TemplateCollectionTests.cs:13` — `collection`
- `tests/Tokenizer.Tests/Types/BoolTests.cs:8` — `tokenizer`
- `tests/Tokenizer.Tests/Types/EnumTests.cs:8` — `tokenizer`
- `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorRingBufferTests.cs:13-15` — `data`, `position`, `chunkSize`
- `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs:236` — nested class `inner`
- `tests/Tokenizer.Tests/Compilation/TemplateCompilerTests.cs:9` — `parser`
- `tests/Tokenizer.Tests/Compilation/CompileApiTests.cs:8` — `tokenizer`
- `tests/Tokenizer.Tests/Compilation/Lexer/TemplateLexerTests.cs:511` — nested class `inner`
- `tests/Tokenizer.Tests/Compilation/Binders/TokenBinderTests.cs:11-12` — `registry`, `decoratorCache`
- `tests/Tokenizer.Tests/Compilation/Binders/DecoratorBinderTests.cs:14-15` — `registry`, `decoratorCache`
- `tests/Tokenizer.Tests/Transformers/*Tests.cs` — 23 test files each with a `transformer` field
- `tests/Tokenizer.Tests/Validators/*Tests.cs` — 20 test files each with a `validator` field

- [ ] **Step 1: Rename fields in src/ files**

For each file listed above, rename every non-compliant private field from `fieldName` to `_fieldName`. Also update ALL references to each field within the same file. Use find-and-replace scoped to each file to avoid missing references.

**Be careful with:**
- Constructor parameters that shadow field names — do NOT rename parameters, only fields
- `buffer` appears in multiple files/classes — scope replacements per-file
- `name` appears in `TokenDefinition.cs` and `DecoratorDefinition.cs` — scope replacements per-file
- Nested class fields in `TemplateLexer.cs` (the `LookaheadReader` class) — these are separate from the outer class fields

- [ ] **Step 2: Rename fields in tests/ files**

Same process for all test files. These are simpler — most are single-field classes with straightforward references.

- [ ] **Step 3: Build**

Run: `dotnet build Tokenizer.sln 2>&1 | grep IDE1006`

Expected: No IDE1006 warnings. If any remain, find and fix them.

- [ ] **Step 4: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass. Field renames should not change behavior.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "style: fix IDE1006 — standardize private field naming to _camelCase"
```

---

### Task 4: Fix IDE0040 — Add Explicit Accessibility Modifiers

**Files:**
- Modify: Any .cs files where types or members have implicit accessibility

The codebase audit found this was already consistent, but the analyzer may catch cases the manual audit missed.

- [ ] **Step 1: Auto-fix**

Run: `dotnet format style ./Tokenizer.sln --diagnostics IDE0040`

- [ ] **Step 2: Verify**

Run: `dotnet build Tokenizer.sln 2>&1 | grep IDE0040`

Expected: No IDE0040 warnings.

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A
git commit -m "style: fix IDE0040 — add explicit accessibility modifiers"
```

Skip if no changes.

---

### Task 5: Fix IDE0060 — Remove Unused Parameters

**Files:**
- Modify: Any .cs files with unused parameters

- [ ] **Step 1: Identify violations**

Run: `dotnet build Tokenizer.sln 2>&1 | grep IDE0060`

Review each violation carefully. Some "unused" parameters may be intentional (interface implementations, event handlers). For those, suppress with `[System.Diagnostics.CodeAnalysis.SuppressMessage]` or a pragma if the parameter is required by a contract.

- [ ] **Step 2: Fix violations**

Remove genuinely unused parameters. For interface-mandated parameters, add a targeted suppression.

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A
git commit -m "style: fix IDE0060 — remove unused parameters"
```

Skip if no changes.

---

### Task 6: Fix IDE0055 — Formatting Violations

**Files:**
- Modify: Any .cs files with formatting issues

- [ ] **Step 1: Auto-fix**

Run: `dotnet format whitespace ./Tokenizer.sln`

- [ ] **Step 2: Verify**

Run: `dotnet build Tokenizer.sln 2>&1 | grep IDE0055`

Expected: No IDE0055 warnings.

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A
git commit -m "style: fix IDE0055 — fix formatting violations"
```

Skip if no changes.

---

### Task 7: Fix CA1507 — Use nameof Instead of String Literals

**Files:**
- Modify: Any .cs files using string literals that should be `nameof()`

- [ ] **Step 1: Identify violations**

Run: `dotnet build Tokenizer.sln 2>&1 | grep CA1507`

- [ ] **Step 2: Fix violations**

Replace string literal arguments with `nameof()`. Example:
```csharp
// Before
throw new ArgumentNullException("value");
// After
throw new ArgumentNullException(nameof(value));
```

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A
git commit -m "style: fix CA1507 — use nameof instead of string literals"
```

Skip if no changes.

---

### Task 8: Fix CA2200 — Rethrow to Preserve Stack Details

**Files:**
- Modify: Any .cs files with `throw ex;` instead of `throw;`

- [ ] **Step 1: Identify violations**

Run: `dotnet build Tokenizer.sln 2>&1 | grep CA2200`

- [ ] **Step 2: Fix violations**

Replace `throw ex;` with `throw;` in catch blocks where the original exception should be rethrown with its stack trace preserved.

```csharp
// Before
catch (Exception ex)
{
    throw ex;  // destroys stack trace
}
// After
catch (Exception ex)
{
    throw;  // preserves stack trace
}
```

If the exception variable is still needed (e.g., for logging before rethrowing), keep it but change `throw ex;` to `throw;`.

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A
git commit -m "fix: fix CA2200 — rethrow to preserve stack details"
```

Skip if no changes.

---

### Task 9: Fix CA2016 — Forward CancellationToken

**Files:**
- Modify: Any .cs files calling async methods without forwarding available CancellationToken

- [ ] **Step 1: Identify violations**

Run: `dotnet build Tokenizer.sln 2>&1 | grep CA2016`

- [ ] **Step 2: Fix violations**

Add the CancellationToken parameter to calls that accept one, forwarding the token from the enclosing method.

```csharp
// Before
await stream.ReadAsync(buffer, 0, count);
// After
await stream.ReadAsync(buffer, 0, count, cancellationToken);
```

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A
git commit -m "fix: fix CA2016 — forward CancellationToken to methods"
```

Skip if no changes.

---

### Task 10: Fix CA1825 — Use Array.Empty Instead of Zero-Length Allocations

**Files:**
- Modify: Any .cs files with `new T[0]` or `new T[] { }`

- [ ] **Step 1: Identify violations**

Run: `dotnet build Tokenizer.sln 2>&1 | grep CA1825`

- [ ] **Step 2: Fix violations**

```csharp
// Before
return new string[0];
// After
return Array.Empty<string>();
```

Note: `Array.Empty<T>()` is available in .NET Standard 2.0, so no conditional compilation needed.

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A
git commit -m "style: fix CA1825 — use Array.Empty instead of zero-length allocations"
```

Skip if no changes.

---

### Task 11: Fix CA1508 — Remove Dead Conditional Code

**Files:**
- Modify: Any .cs files with always-true or always-false conditions

- [ ] **Step 1: Identify violations**

Run: `dotnet build Tokenizer.sln 2>&1 | grep CA1508`

- [ ] **Step 2: Fix violations**

Review each flagged condition carefully. The analyzer detects conditions that are provably always true or always false based on control flow. Remove dead branches or simplify the conditions.

**Be cautious:** CA1508 can sometimes flag conditions that look dead but have subtle reasons for existing (e.g., defensive programming against future changes). If a condition is intentionally defensive, suppress with a comment explaining why.

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A
git commit -m "fix: fix CA1508 — remove dead conditional code"
```

Skip if no changes.

---

### Task 12: Fix CA2213 — Dispose Disposable Fields

**Files:**
- Modify: Any .cs files with disposable fields that aren't properly disposed

- [ ] **Step 1: Identify violations**

Run: `dotnet build Tokenizer.sln 2>&1 | grep CA2213`

- [ ] **Step 2: Fix violations**

Implement `IDisposable` and dispose the fields, or if the class doesn't own the disposable resource (it was injected), suppress with a justification.

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 4: Commit (if changes were made)**

```bash
git add -A
git commit -m "fix: fix CA2213 — dispose disposable fields"
```

Skip if no changes.

---

### Task 13: Fix Meziantou Analyzer Violations

The Meziantou analyzer covers many rules. We don't know which will fire until the earlier tasks are complete and the build runs clean of the above rules.

- [ ] **Step 1: Identify all Meziantou violations**

Run: `dotnet build Tokenizer.sln 2>&1 | grep " MA" | sort | uniq -c | sort -rn`

This shows which Meziantou rules fire and how frequently.

- [ ] **Step 2: Triage violations**

For each distinct Meziantou rule that fires:
- **Fix** if the violation is clearly correct and the fix is mechanical
- **Suppress in `.editorconfig`** if the rule is too noisy or doesn't fit the project (add `dotnet_diagnostic.MAxxxx.severity = none`)
- Group fixes by Meziantou rule ID

- [ ] **Step 3: Fix violations grouped by rule**

Fix one Meziantou rule at a time. For each:
1. Fix all violations of that rule
2. Run tests: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
3. Commit: `style: fix MAxxxx — [description of what the rule enforces]`

- [ ] **Step 4: Suppress remaining noisy rules**

Add suppressions to `.editorconfig` for rules that don't fit. Commit:
```bash
git add .editorconfig
git commit -m "style: suppress noisy Meziantou analyzer rules"
```

---

### Task 14: Final Verification

No commit — verification only.

- [ ] **Step 1: Clean build**

Run: `dotnet build ./Tokenizer.sln -c Release`

Expected: Zero warnings, zero errors.

- [ ] **Step 2: All tests pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass.

- [ ] **Step 3: Format check**

Run: `dotnet format ./Tokenizer.sln --verify-no-changes`

Expected: No formatting drift (exit code 0).

---

### Task 15: Update CLAUDE.md with Code Style Rules

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Add code style section to CLAUDE.md**

Add the following section after the existing "Code Conventions" section:

```markdown
## Code Style Enforcement

Style and quality rules are enforced via `.editorconfig` and Roslyn analyzers. `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` means violations break the build locally and in CI.

**Analyzer packages:**
- Built-in .NET SDK analyzers (`AnalysisLevel=latest-Recommended`)
- `Meziantou.Analyzer` (shared via `Directory.Build.props`)

**Naming conventions:**
- Private fields: `_camelCase` (underscore prefix)
- Constants: `PascalCase`
- Static readonly fields: `PascalCase`
- Interfaces: `IPascalCase`
- Public members (properties, methods, events): `PascalCase`

**Key enforced rules:**
- `IDE0005` — No unused usings
- `IDE0040` — Explicit accessibility modifiers required
- `IDE0060` — No unused parameters
- `IDE1006` — Naming conventions enforced
- `CA1507` — Use `nameof` over string literals
- `CA2016` — Forward CancellationToken
- `CA2200` — Rethrow to preserve stack traces

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
```

- [ ] **Step 2: Run tests (sanity check)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Expected: All tests pass (CLAUDE.md change doesn't affect build, but verify nothing is broken).

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: update CLAUDE.md with code style rules and enforcement"
```
