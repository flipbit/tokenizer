# CompilationResult Design

**Date:** 2026-07-06
**Status:** Draft
**Branch:** v3

## Problem

`TemplateCompiler.Compile()` creates and populates an `IDiagnosticCollector` with compilation events, but the collected diagnostics are discarded — `GetResult()` is never called. Users have no way to access compilation diagnostics to debug template compilation issues.

## Goals

1. Surface compilation diagnostics to library users via a `CompilationResult` type
2. Rename `TokenizationDiagnostics` to `DiagnosticResult` — the type is used for both compilation and tokenization diagnostics
3. Maintain consistency with the existing `TokenizeResult` pattern

## Non-Goals

- Adding `Success`, warnings, or compilation duration to `CompilationResult` (YAGNI)
- Separating compilation diagnostics from tokenization diagnostics into distinct types

## Design

### CompilationResult

A minimal result type returned from `Compile()`:

```csharp
public sealed class CompilationResult
{
    public Template Template { get; }
    public DiagnosticResult? Diagnostics { get; }

    internal CompilationResult(Template template, DiagnosticResult? diagnostics)
    {
        Template = template;
        Diagnostics = diagnostics;
    }
}
```

`Diagnostics` is null when `TokenizerOptions.EnableDiagnostics` is false — same convention as `TokenizeResult.Diagnostics`.

### Rename TokenizationDiagnostics → DiagnosticResult

The existing `TokenizationDiagnostics` class holds a list of `DiagnosticEvent`s and is used for both compilation and tokenization phases. Rename to `DiagnosticResult` to:

- Avoid the `Tokens.Diagnostics.Diagnostics` namespace collision
- Follow the `*Result` naming convention used elsewhere (`TokenizeResult`, `CompilationResult`)
- Accurately reflect that it serves both compilation and tokenization

### API Changes

`ITokenizer`:

```csharp
// Before
Template Compile(string pattern);
Task<Template> CompileAsync(TextReader reader, CancellationToken ct = default);

// After
CompilationResult Compile(string pattern);
Task<CompilationResult> CompileAsync(TextReader reader, CancellationToken ct = default);
```

`TemplateCompiler.Compile()` returns `CompilationResult` instead of `Template`:

```csharp
public CompilationResult Compile(string content)
{
    // ... existing binder orchestration ...
    var diagnostics = collector.GetResult();
    return new CompilationResult(template, diagnostics);
}
```

`TokenMatcher` internal callers access `result.Template` where they previously received a `Template` directly.

### Files Changed

- Create: `src/Tokenizer/CompilationResult.cs`
- Rename class: `TokenizationDiagnostics` → `DiagnosticResult` in `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs` (file also renamed to `DiagnosticResult.cs`)
- Modify: `ITokenizer.cs` — `Compile` and `CompileAsync` return types
- Modify: `Tokenizer.cs` — return types and async wrapper
- Modify: `TemplateCompiler.cs` — return `CompilationResult`
- Modify: `TokenMatcher.cs` — access `.Template` from result
- Modify: `IDiagnosticCollector.cs` — `GetResult()` return type
- Modify: `DiagnosticCollector.cs` — return type and field type
- Modify: `NullDiagnosticCollector.cs` — return type
- Modify: `TokenizeResultBase.cs` — `Diagnostics` property type
- Modify: `DiagnosticSummaryBuilder.cs` — parameter type
- Modify: `AlignmentRenderer.cs` — parameter type
- Modify: All test files referencing `TokenizationDiagnostics` or `Compile()` returning `Template`

### Testing

- Existing `TemplateCompilerTests` update to access `result.Template`
- New tests for `CompilationResult`:
  - `GivenDiagnosticsEnabled_WhenCompiling_ThenResultHasDiagnostics`
  - `GivenDiagnosticsDisabled_WhenCompiling_ThenDiagnosticsIsNull`
  - `GivenCompilationResult_WhenAccessed_ThenTemplateIsAvailable`
- All existing tests referencing `TokenizationDiagnostics` updated to use `DiagnosticResult`
