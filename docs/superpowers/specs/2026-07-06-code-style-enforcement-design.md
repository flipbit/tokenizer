# Code Style Enforcement Design

## Goal

Add automated code style enforcement to the Tokenizer project using `.editorconfig` rules and Roslyn analyzers, so that violations break the build in CI and locally. Enable per-rule execution for AI-agent-driven fixes.

## Approach

`.editorconfig` + built-in .NET SDK analyzers + `Meziantou.Analyzer`. No ReSharper CLI, no StyleCop, no Roslynator.

This matches the approach used by `dotnet/runtime`, `dotnet/aspnetcore`, and `dotnet/efcore`.

## Configuration Changes

### Directory.Build.props

Add to existing `<PropertyGroup>`:

```xml
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<AnalysisLevel>latest-Recommended</AnalysisLevel>
```

Add shared analyzer package:

```xml
<ItemGroup>
  <PackageReference Include="Meziantou.Analyzer" Version="3.0.121">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### Tokenizer.Tests.csproj

Add `xunit.analyzers` (explicit, even if transitively available):

```xml
<PackageReference Include="xunit.analyzers" Version="1.27.0" />
```

### .editorconfig

Expand the existing file with the following rules.

#### Style Rules (IDE)

```ini
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

# File-scoped namespace declarations
dotnet_diagnostic.IDE0161.severity = warning
csharp_style_namespace_declarations = file_scoped:warning
```

#### Quality Rules (CA)

```ini
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
```

#### Naming Conventions

Replace the existing private field naming rules with underscore-prefix convention:

```ini
# Private fields: _camelCase
dotnet_naming_style.underscore_camel_case.required_prefix = _
dotnet_naming_style.underscore_camel_case.capitalization = camel_case

dotnet_naming_rule.private_fields.symbols = private_field_symbols
dotnet_naming_rule.private_fields.style = underscore_camel_case
dotnet_naming_rule.private_fields.severity = warning
dotnet_naming_symbols.private_field_symbols.applicable_kinds = field
dotnet_naming_symbols.private_field_symbols.applicable_accessibilities = private, protected, private_protected

# Constants: PascalCase
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
```

Note: naming rule precedence matters — constants and static readonly rules must be listed before the general private fields rule so they take priority.

## Build Enforcement

No CI changes required. The existing pipeline already runs `dotnet build` with `TreatWarningsAsErrors=true`. Adding `EnforceCodeStyleInBuild=true` makes IDE rules run during build. Any violation → warning → error → build failure.

## Per-Rule Execution

For AI agents fixing violations one rule at a time:

```bash
# Check a single rule (dry run)
dotnet format analyzers ./Tokenizer.sln --verify-no-changes --diagnostics IDE0005

# Fix a single rule (auto-fix where available)
dotnet format analyzers ./Tokenizer.sln --diagnostics IDE0005

# For style rules
dotnet format style ./Tokenizer.sln --diagnostics IDE1006

# For rules without auto-fixers, read build output
dotnet build ./Tokenizer.sln 2>&1 | grep "CA1507"
```

## Implementation Plan

### Phase 1: Configuration

1. Update `.editorconfig` with all rules and naming conventions listed above
2. Update `Directory.Build.props` with `EnforceCodeStyleInBuild`, `AnalysisLevel`, and `Meziantou.Analyzer`
3. Add `xunit.analyzers` to `Tokenizer.Tests.csproj`
4. Run `dotnet restore`
5. Commit: `style: add code style enforcement via .editorconfig and analyzers`

### Phase 2: Fix Violations Rule-by-Rule

For each rule below:
1. Run `dotnet build` or `dotnet format` filtered to that rule to identify violations
2. Fix all violations (auto-fix where possible, manual where not)
3. Run `dotnet test` — all tests must pass
4. Commit with the rule ID in the message

Rules to fix in order:

| Order | Rule | Description | Commit message |
|-------|------|-------------|----------------|
| 1 | IDE0005 | Remove unused usings | `style: fix IDE0005 — remove unused using statements` |
| 2 | IDE1006 | Private field naming `_camelCase` | `style: fix IDE1006 — standardize private field naming to _camelCase` |
| 3 | IDE0040 | Explicit accessibility modifiers | `style: fix IDE0040 — add explicit accessibility modifiers` |
| 4 | IDE0060 | Unused parameters | `style: fix IDE0060 — remove unused parameters` |
| 5 | IDE0055 | Formatting | `style: fix IDE0055 — fix formatting violations` |
| 6 | CA1507 | Use nameof | `style: fix CA1507 — use nameof instead of string literals` |
| 7 | CA2200 | Rethrow preservation | `fix: fix CA2200 — rethrow to preserve stack details` |
| 8 | CA2016 | Forward CancellationToken | `fix: fix CA2016 — forward CancellationToken to methods` |
| 9 | CA1825 | Zero-length array allocations | `style: fix CA1825 — use Array.Empty instead of zero-length allocations` |
| 10 | CA1508 | Dead conditional code | `fix: fix CA1508 — remove dead conditional code` |
| 11 | CA2213 | Disposable fields | `fix: fix CA2213 — dispose disposable fields` |
| 12 | Meziantou | Review and fix Meziantou violations | `style: fix Meziantou analyzer violations` (may be split into multiple commits if many distinct rules fire) |

Note: some rules may have zero violations. Skip the commit in that case.

### Phase 3: Verification

```bash
dotnet build ./Tokenizer.sln -c Release   # zero warnings
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj   # all pass
dotnet format ./Tokenizer.sln --verify-no-changes   # no drift
```

No commit — this is a verification step only.

### Phase 4: Documentation

Update `CLAUDE.md` with:
- List of enforced rules and their IDs
- How to check/fix a single rule
- Naming conventions summary
- Reference to `.editorconfig` as source of truth

Commit: `docs: update CLAUDE.md with code style rules and enforcement`

## Rules NOT Included (and Why)

| Rule/Tool | Reason excluded |
|-----------|-----------------|
| StyleCop.Analyzers | Overlaps with built-in analyzers, noisy, requires disabling many defaults |
| Roslynator | Massive surface area, overkill for this project |
| ReSharper CLI | Adds CI complexity, slow for large projects, marginal benefit over SDK tooling |
| CA1062 (validate public args) | Redundant with nullable reference types |
| CA2007 (ConfigureAwait) | Library-level concern, adds noise without clear benefit here |
| CA1031 (don't catch Exception) | Too many legitimate catch-all patterns |
| SerilogAnalyzer | Serilog only used for test output capture, not structured logging |
| IDE0046 (conditional return) | Not all cases are clearer as ternary; left as suggestion only |
| SA1402 (one type per file) | Only one instance in codebase (nested private class, which is acceptable) |
