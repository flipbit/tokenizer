# Template Compiler Restructure

**Date:** 2026-07-06
**Status:** Draft
**Branch:** v3

## Problem

`TemplateCompiler.Compile()` is a monolithic method that parses, validates, binds, and links tokens in a single 200-line flow. It mixes orchestration with implementation, scatters logging throughout, and is difficult to test in isolation. The class violates the Single Responsibility Principle and makes it hard to understand how templates are assembled.

## Goals

1. **Readable orchestration** — `TemplateCompiler.Compile()` reads like a table of contents
2. **Focused, testable components** — each compilation step is a small class with dedicated tests
3. **Unified diagnostics** — replace scattered `ILogger` calls with the existing `IDiagnosticCollector` pattern from the tokenization pipeline
4. **Open/Closed** — adding a compilation step means adding a class and one line in `Compile()`
5. **Reduced allocations** — no new context objects or abstractions beyond what's needed

## Non-Goals

- Refactoring `TokenDefinition` StringBuilder usage (separate follow-up)
- Changing the compilation pipeline stages (lexer, parser, AST, binder)
- Adding pluggable/reorderable pipeline steps — the sequence is fixed

## Approach: Focused Static Binder Classes

Each responsibility in the current `Compile()` method becomes a small, focused class in `Tokens.Compilation.Binders`. No shared interface, no context object. Each class takes explicit inputs and produces explicit outputs.

## TemplateCompiler — The Orchestrator

After restructuring, `Compile()` becomes:

```csharp
public Template Compile(string content)
{
    TemplateLengthValidator.Validate(content, Options);

    var definition = new AstTemplateDefinitionParser().Parse(content, Options);
    var id = content.ComputeHash();
    var template = TemplateFactory.Create(id, definition);

    HintBinder.Bind(definition, template, collector);
    TagBinder.Bind(definition, template, collector);
    TokenBinder.Bind(definition, template, registry, _decoratorCache, collector);
    TokenCountValidator.Validate(template, Options);

    return template;
}
```

The compiler retains ownership of:

- `DecoratorRegistry` — constructed once, reused across compilations
- `_decoratorCache` — shared `ConcurrentDictionary<Type, ITokenDecorator>` for perf
- `IDiagnosticCollector` creation — instantiated at the top of `Compile()` using the same pattern as the tokenization path: `Options.EnableDiagnostics ? new DiagnosticCollector(...) : NullDiagnosticCollector.Instance`
- Error handling — `catch TokenizerException` / `catch Exception` stays here

Compilation timing moves into the diagnostic collector (`CompilationCompleted` event records duration). The `ILogger` dependency is removed from `TemplateCompiler` — all observability flows through the collector.

## Template Constructor Changes

`Template` is simplified to a single constructor:

```csharp
internal Template(ulong id, TokenizerOptions options)
```

- Remove `Template()` — dead code, no callers
- Remove `Template(string name)` — only used by tests, migrate to `TemplateBuilder`
- Remove `Template(string name, TokenizerOptions options)` — only used by tests, migrate to `TemplateBuilder`
- Remove `Template(string pattern, string name, TokenizerOptions options)` — replaced by `(ulong id, TokenizerOptions options)`
- `Name` remains a settable property, set after construction by `TemplateFactory`

The content-based `Id` is computed by the caller (`content.ComputeHash()`) and passed in as a `ulong`, avoiding passing raw content strings through the pipeline.

## Binder Components

### TemplateFactory

Creates a `Template` from a `TemplateDefinition`. Owns auto-naming and the `templateCounter`:

```csharp
internal static class TemplateFactory
{
    private static int templateCounter;

    public static Template Create(ulong id, TemplateDefinition definition)
    {
        var template = new Template(id, definition.Options);

        template.Name = string.IsNullOrWhiteSpace(definition.Name)
            ? $"Template_{Interlocked.Increment(ref templateCounter)}"
            : definition.Name;

        return template;
    }
}
```

### HintBinder

Assigns hints from the definition to the template, skipping duplicates:

```csharp
internal static class HintBinder
{
    public static void Bind(TemplateDefinition definition, Template template,
        IDiagnosticCollector collector)
    {
        foreach (var hint in definition.Hints)
        {
            if (template.Hints.Any(h => h == hint))
                continue;

            template.AddHint(hint);
            collector.Record(DiagnosticEventType.HintAdded, detail: hint.ToString());
        }
    }
}
```

### TagBinder

Same shape as `HintBinder` for tags.

### TokenBinder

Orchestrates per-token compilation by delegating to sub-components:

```csharp
internal static class TokenBinder
{
    public static void Bind(TemplateDefinition definition, Template template,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        IDiagnosticCollector collector)
    {
        foreach (var tokenDef in definition.Tokens)
        {
            var token = TokenFactory.Create(tokenDef, template.Options, collector);
            OptionApplier.Apply(token, template.Options, collector);
            DecoratorBinder.Bind(tokenDef, token, registry, decoratorCache, collector);
            template.AddToken(token);
            RepeatingTokenLinker.Link(token, template, collector);
        }
    }
}
```

### TokenFactory

Creates a `Token` from a `TokenDefinition`. Owns preamble computation as a private method:

- Maps all boolean flags (`IsOptional`, `IsRepeating`, `TerminateOnNewLine`, `IsRequired`, `DependsOnId`, `IsFrontMatterToken`, `IsNull`, `IsSingleUse`)
- Defaults `Name` to `string.Empty` when null
- Defaults `Location` to `new FileLocation()` when null
- `ComputePreamble` applies `TrimLeadingWhitespaceInTokenPreamble` and `TrimPreambleBeforeNewLine` options

### OptionApplier

Applies template-level option overrides to individual tokens:

- `OutOfOrderTokens` — marks token as optional
- `TerminateOnNewLine` — applies global newline termination when token doesn't set it

### DecoratorBinder

Resolves decorator definitions against the `DecoratorRegistry` and creates `TokenDecoratorContext` instances:

- Adds `SetTransformer` when `TokenDefinition.Value` is set
- Handles concatenation decorator as a special case (`TryApplyConcatenation`)
- Resolves transformers by name matching (`TryApplyTransformer`)
- Resolves validators by name matching (`TryApplyValidator`)
- Validates front matter tokens have a `SetTransformer`
- Throws `TokenizerException` for unknown decorator names

The nested `foreach` + `break` pattern is replaced with `TryApply*` methods that return `bool`.

### RepeatingTokenLinker

Links repeating tokens to their non-repeating counterpart with the same name:

- Only applies when `IsRepeating` is true, `DependsOnId` is `-1`, and at least 2 tokens exist
- Finds the previous token with the same name that is non-repeating
- Sets `DependsOnId` to create the dependency

### TemplateLengthValidator

Runs before parsing. Throws `ParsingException` if content exceeds `MaxTemplateLength`.

### TokenCountValidator

Runs after token binding. Throws `ParsingException` if token count exceeds `MaxTokenCount`.

## Diagnostics Integration

The existing `IDiagnosticCollector` interface is extended with compilation-specific event types in the `DiagnosticEventType` enum:

```
HintAdded
TagAdded
TokenCreated
OptionApplied
DecoratorApplied
ConcatenationApplied
RepeatingTokenLinked
CompilationCompleted
```

Each binder calls `collector.Record(...)` with the appropriate event type. The `IsEnabled` guard on the collector avoids overhead when diagnostics are disabled — same pattern as the tokenization side.

Error-level logging in `catch` blocks stays in `TemplateCompiler` as orchestration-level error reporting.

If the diagnostic event types start diverging significantly from the tokenization events, split into a separate `ICompilationDiagnosticCollector`.

## Variable Naming

The `pre~` prefix convention (`preToken`, `preTemplate`) is replaced:

- `preTemplate` becomes `definition` (type `TemplateDefinition`)
- `preToken` becomes `tokenDef` or `definition` (type `TokenDefinition`) — no collision since `TokenBinder` sub-components receive either the definition or the token, not both in the same scope (except `DecoratorBinder.Bind` which takes both explicitly)

## File Layout

All new classes in `src/Tokenizer/Compilation/Binders/`:

```
Compilation/
  Binders/
    FrontMatterBinder.cs          (existing, unchanged)
    HintBinder.cs
    TagBinder.cs
    TokenBinder.cs
    TokenFactory.cs
    OptionApplier.cs
    DecoratorBinder.cs
    RepeatingTokenLinker.cs
    TemplateLengthValidator.cs
    TokenCountValidator.cs
    TemplateFactory.cs
  TemplateCompiler.cs             (slimmed to orchestration only)
```

Namespace: `Tokens.Compilation.Binders`

## Testing Strategy

Each binder class gets its own test class:

- `TemplateFactoryTests` — auto-naming, id assignment
- `HintBinderTests` — hint assignment, duplicate skipping
- `TagBinderTests` — tag assignment, duplicate skipping
- `TokenFactoryTests` — property mapping, preamble computation (both trim options)
- `OptionApplierTests` — OutOfOrderTokens, TerminateOnNewLine
- `DecoratorBinderTests` — transformer resolution, validator resolution, concatenation, front matter validation, unknown decorator error
- `RepeatingTokenLinkerTests` — linking logic, edge cases (no prior token, non-matching names)
- `TemplateLengthValidatorTests` — over limit, at limit, disabled (0)
- `TokenCountValidatorTests` — over limit, at limit, disabled (0)
- `TemplateCompilerTests` — integration: end-to-end compilation still works (existing tests migrated)

Existing `TemplateCompilerTests` are preserved as integration tests. Tests that directly construct `new Template(string.Empty)` are migrated to use `TemplateBuilder`.

## Migration Notes

- `TemplateBuilder` in tests updated to use `Template(ulong id, TokenizerOptions options)` constructor
- Tests using `new Template(string.Empty)` or `new Template("name")` migrated to `TemplateBuilder`
- No public API changes — `TemplateCompiler` is `internal`
- `Template` public constructors removed (were only used by tests)
