# TokenizationEngine Refactoring Design

## Summary

Refactor `TokenizationEngine` from a monolithic 566-line class into a thin orchestrator
over focused, composable modules. Unify the sync and async code paths into a single
algorithm with two entry points.

## Goals

- **Reduced complexity**: decompose the engine into focused, single-responsibility components
- **Better developer UX**: readable orchestrator, traceable data flow, clear extension points
- **No perf regressions**: no virtual dispatch in hot loops, no extra allocations beyond one-time session construction
- **Open/Closed principle**: new token-processing behaviors = new processor classes
- **Enhanced testability**: each module independently unit-testable
- **Parameter reduction**: bind session-scoped dependencies at construction, not per-call

## Approach: Composition with Focused Instance Classes

The TemplateCompiler refactoring used static binders because compilation is a linear
pipeline. Tokenization is fundamentally different — it's a stateful loop where components
interact repeatedly. We use instance classes that hold session-scoped dependencies at
construction, eliminating parameter bloat while keeping direct method calls (no virtual
dispatch in hot paths).

## Component Architecture

```
TokenizationEngine (thin orchestrator)
  │
  ├── CreateSession(template, target, result, collector, hints?)
  │     → validates target, returns TokenizationSession
  │
  └── InputValidator (static)
        → targetObject settable-property check + logging
        → argument null checks

TokenizationSession (per-run coordinator)
  │
  ├── holds: template, target, result, collector, hintStrategy
  ├── holds: iteration guard state (count, hasExplicitLimit)
  │
  ├── Run(context) → void
  │     → sync entry point
  ├── RunAsync(context, ct) → Task
  │     → async entry point
  ├── ProcessChunk(context, ct) → bool
  │     → iteration guard check
  │     → delegates to TokenMatchRouter per character
  └── Finalize(context) → void
        → candidateProcessor.ProcessRemaining()
        → FrontMatterProcessor.Process()
        → diagnostics recording

TokenMatchRouter (per-character decision logic)
  │
  ├── holds: CandidateProcessor, collector, hintStrategy
  │
  └── RouteNext(context) → void
        → peek character
        → if repeated token preamble → candidateProcessor.HandleRepeat()
        → if newline-terminated → candidateProcessor.HandleNewline()
        → if next token match → HandleFirstMatch / HandleSwitch / Accumulate
        → else → Accumulate

CandidateProcessor (token assignment & lifecycle)
  │
  ├── holds: targetObject, result, options, template, collector
  │
  ├── TryAssign(context, location) → bool
  ├── HandleRepeat(context) → bool
  ├── HandleNewline(context) → void
  ├── ProcessRemaining(context) → void
  └── (private) AddMatchedTokenIds, WasLastMatchedToken

FrontMatterProcessor (static)
  │
  └── Process(template, target, result, collector) → void
```

## Async/Sync Unification

Currently there are two call paths with near-duplicate orchestration code in `Tokenizer.cs`.
The sync path calls `ProcessTokenization` (which internally does Begin/Continue/End).
The async path manually calls Begin, loops FillBufferAsync + Continue, then End.

### New design — single algorithm, two entry points:

```csharp
// TokenizationSession
public void Run(TokenizationContext context)
{
    context.MatchBuffer.Clear();
    collector.Record(DiagnosticEventType.TokenizationStarted, ...);

    do { context.Enumerator.FillBuffer(); }
    while (!ProcessChunk(context, CancellationToken.None));

    Finalize(context);
}

public async Task RunAsync(TokenizationContext context, CancellationToken ct)
{
    context.MatchBuffer.Clear();
    collector.Record(DiagnosticEventType.TokenizationStarted, ...);

    do { await context.Enumerator.FillBufferAsync(ct).ConfigureAwait(false); }
    while (!ProcessChunk(context, ct));

    Finalize(context);
}
```

`ProcessChunk` is identical for both paths — it's the pure synchronous algorithm that
processes whatever's in the buffer. The only difference is `FillBuffer()` vs
`await FillBufferAsync(ct)`. The sync enumerator's `FillBuffer` is effectively a no-op
(all data is already in the buffer), so the algorithm never actually yields.

The `MaxInputLength` check currently in `Tokenizer.TokenizeAsync` moves into
`ProcessChunk` or the iteration guard so both paths get it.

### Impact on callers (Tokenizer.cs):

```csharp
// Sync — was: engine.ProcessTokenization(template, value, context, result, collector, hintStrategy)
var session = engine.CreateSession(template, value, result, collector, hintStrategy);
session.Run(context);

// Async — was: 5 lines of Begin/FillBufferAsync/Continue/End
var session = engine.CreateSession(template, value, result, collector, hintStrategy);
await session.RunAsync(context, ct);
```

Callers no longer need to know about Begin/Continue/End phasing.

## Interface & API Changes

### Current `ITokenizationEngine` (4 methods, 6 params on the big ones):

```csharp
void ProcessTokenization(Template, object?, TokenizationContext, TokenizeResultBase, IDiagnosticCollector, IHintStrategy?)
TokenizationContinuation BeginTokenization(Template, object?, TokenizationContext, TokenizeResultBase, IDiagnosticCollector, IHintStrategy?)
bool ContinueTokenization(TokenizationContinuation, TokenizationContext, CancellationToken)
void EndTokenization(TokenizationContinuation, TokenizationContext)
```

### New `ITokenizationEngine` (1 method):

```csharp
internal interface ITokenizationEngine
{
    TokenizationSession CreateSession(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);
}
```

### Deleted types:

- `TokenizationContinuation` — subsumed by `TokenizationSession`

### New types:

| Type | Kind | Namespace |
|------|------|-----------|
| `TokenizationSession` | `internal sealed class` | `Tokens.Tokenization` |
| `TokenMatchRouter` | `internal sealed class` | `Tokens.Tokenization` |
| `CandidateProcessor` | `internal sealed class` | `Tokens.Tokenization` |
| `FrontMatterProcessor` | `internal static class` | `Tokens.Tokenization` |
| `InputValidator` | `internal static class` | `Tokens.Tokenization` |

### File changes:

| File | Action |
|------|--------|
| `ITokenizationEngine.cs` | Simplified to single `CreateSession` method |
| `TokenizationEngine.cs` | Thin orchestrator — `CreateSession` + delegates to `InputValidator` |
| `TokenizationContinuation.cs` | Deleted |
| `TokenizationSession.cs` | New — `Run`, `RunAsync`, `ProcessChunk`, `Finalize` |
| `TokenMatchRouter.cs` | New — per-character decision routing |
| `CandidateProcessor.cs` | New — assignment, backtracking, newline, remaining |
| `FrontMatterProcessor.cs` | New — static class, post-loop front matter |
| `InputValidator.cs` | New — static class, target object validation |
| `Tokenizer.cs` | Updated call sites (simpler) |

## Data Flow & Parameter Reduction

### Construction-time bindings (set once per session):

```csharp
internal sealed class TokenizationSession
{
    private readonly Template template;
    private readonly object? targetObject;
    private readonly TokenizeResultBase result;
    private readonly IDiagnosticCollector collector;
    private readonly IHintStrategy? hintStrategy;
    private readonly TokenMatchRouter router;
    private readonly CandidateProcessor candidateProcessor;
    private readonly bool hasExplicitLimit;
    private int iterationCount;
}
```

`CandidateProcessor` holds `targetObject`, `result`, `template`, `options`, `collector`
at construction. `TokenMatchRouter` holds `candidateProcessor`, `collector`, `hintStrategy`.

### Result: most hot-path methods take only `TokenizationContext`:

| Current signature | New signature |
|-------------------|---------------|
| `ProcessRepeatedTokens(continuation, context)` | `candidateProcessor.HandleRepeat(context)` |
| `TryAssignCandidateTokens(continuation, context, location)` | `candidateProcessor.TryAssign(context, location)` |
| `HandleTokenSwitch(continuation, context, matches)` | `router.HandleSwitch(context, matches)` |
| `ProcessFrontMatterTokens(continuation, location)` | `FrontMatterProcessor.Process(template, target, result, collector)` |

## Testing Strategy

### Existing tests: preserve and adapt

The 6 existing test files (~1496 lines) testing the engine end-to-end are updated to
call `engine.CreateSession(...).Run(context)` instead of `engine.ProcessTokenization(...)`.
They continue to validate the full algorithm works. Minimal changes needed beyond the
call pattern.

### New unit test files:

| Test class | Tests what | Key scenarios |
|------------|-----------|---------------|
| `CandidateProcessorTests` | Assignment, backtracking, newline | Successful assign, failed assign with diagnostics, backtracking with empty preamble detection, single-use token removal, repeating token disabling, newline gap detection |
| `TokenMatchRouterTests` | Per-character routing decisions | Routes to repeat handler, routes to newline handler, routes to first-match, routes to switch, falls through to accumulate |
| `FrontMatterProcessorTests` | Post-loop front matter | Assigns front matter tokens, records diagnostics, skips non-front-matter |
| `TokenizationSessionTests` | Session lifecycle, iteration guards | Explicit limit exceeded, derived limit exceeded, cancellation respected, Run/RunAsync equivalence, Finalize processes remaining |
| `InputValidatorTests` | Target object validation | Rejects anonymous types, rejects read-only, accepts writable, accepts null, accepts dictionaries |

### Testing principles:

- `CandidateProcessor` and `TokenMatchRouter` tested with real `TokenizationContext` — no mocking the context
- `TokenEnumerator` initialized with `StringReader` for predictable input
- Diagnostics verified via `DiagnosticCollector` (not mocked) — assert on collected events
- Router tests can use NSubstitute for `CandidateProcessor` to verify routing decisions independently of assignment logic

## Migration Notes

- `ArgumentValidation` helper class stays — it's used outside the engine
- `ITokenizationContext` interface: remove it. It's only used by the `ShouldProcess*`
  guard methods which move into their respective processors. The concrete
  `TokenizationContext` is sufficient.
- Logger dependency: `TokenizationEngine` keeps its `ILogger` for `InputValidator`
  debug logging (target object type info). `CandidateProcessor` takes an
  `ILogger` for the assignment error catch block (`log.LogWarning`), matching
  current behavior. All other observability goes through `IDiagnosticCollector`.
