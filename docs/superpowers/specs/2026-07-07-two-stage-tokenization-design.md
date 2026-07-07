# Two-Stage Tokenization: Separate Matching from Reflection

**Date:** 2026-07-07
**Branch:** v3

## Problem

The tokenization pipeline currently mixes two concerns:

1. **Matching** — scanning input text, matching preambles, running the decorator pipeline (transformers + validators) to produce transformed values
2. **Reflection** — constructing a target object and assigning matched values to its properties via `SetValue`

These concerns are interleaved at the lowest level: `TokenAssigner.Assign()` runs the decorator pipeline *and* reflects values onto the target object in a single method. The target object is threaded through `TokenizationEngine`, `TokenizationSession`, `CandidateProcessor`, `CandidateTokenList`, and `FrontMatterProcessor` — even though the non-generic `Tokenize()` already passes `null` and skips reflection.

## Goals

- Separate matching (Stage 1) from object construction/reflection (Stage 2)
- Enable a workflow where callers get raw matches, inspect them, and optionally project onto a typed object
- Simplify the matching pipeline by removing the target object parameter
- Rename `TokenAssigner` to reflect its actual responsibility after the split

## Design

### Stage 1: Matching Pipeline

The matching pipeline produces a `TokenizeResult` containing `TokenMatch` records with already-transformed values. No target object exists during this stage.

#### `TokenAssigner` → `DecoratorPipeline` (rename)

The class is renamed to `DecoratorPipeline`. Its responsibility narrows to: prepare the raw matched text, run transformers, run validators, return the transformed value.

**Before:**
```csharp
internal bool Assign(Token token, object? target, string value, FileLocation location, out object? assignedValue)
```

**After:**
```csharp
internal bool Evaluate(Token token, string value, FileLocation location, out object? evaluatedValue)
```

The method retains:
- `PrepareValue()` — trim, newline handling, null token checks
- `RunDecoratorPipeline()` — transformers then validators with diagnostics

The method loses:
- `SetDictionaryValue()` — moves to `Assign<T>()`
- `target.SetValue()` reflection — moves to `Assign<T>()`
- Concatenation-to-object logic — moves to `Assign<T>()`
- `MissingMemberException` / `TypeConversionException` catch blocks — moves to `Assign<T>()`

`CanAssign()` becomes `CanEvaluate()` with the same signature change (already had no target parameter).

#### Target object removal

The `target` / `targetObject` parameter is removed from:

| Class | Change |
|-------|--------|
| `DecoratorPipeline` (née `TokenAssigner`) | `Evaluate()` loses `target` param |
| `CandidateTokenList` | `TryAssign()` loses `target` param |
| `CandidateProcessor` | Loses `_targetObject` field and constructor param |
| `TokenizationSession` | Loses `_targetObject` field and constructor param |
| `TokenizationEngine.CreateSession()` | Loses `targetObject` param |
| `FrontMatterProcessor.Process()` | Loses `targetObject` param |
| `InputValidator.ValidateTargetObject()` | Removed |

#### Naming updates through the chain

With `TokenAssigner` → `DecoratorPipeline` and `Assign` → `Evaluate`:
- `CandidateTokenList.TryAssign()` → `TryEvaluate()` — iterates candidates, first to pass `Evaluate()` wins
- `CandidateTokenList.CanAnyAssign()` → `CanAnyEvaluate()`
- `CandidateProcessor.TryAssign()` → keeps its name. It assigns a match to the *result* (adds to `TokenResult.Matches`), which is still accurate. Not to be confused with `CandidateTokenList.TryEvaluate()` which evaluates candidates through the decorator pipeline.
- Internal variable names: `assigner` → `pipeline` or `decoratorPipeline`

### Stage 2: Object Reflection via `Assign<T>()`

New method on `TokenizeResult`:

```csharp
public TokenizeResult<T> Assign<T>() where T : class, new()
```

#### Behavior

1. Creates a new `TokenizeResult<T>` projecting forward: `Template`, `Tokens`, `Hints`, `Diagnostics` from the source result. Stage 1 exceptions are **not** copied forward — they remain on the original `TokenizeResult`.
2. Creates `new T()`.
3. Iterates `Tokens.Matches` in order. For each `TokenMatch`:
   - If `T` is `IDictionary<string, object>`: uses dictionary assignment logic (handles repeating tokens as `List<object>`)
   - If `token.CanConcatenate`: gets current property value, concatenates via `ValueConcatenation.Concatenate()`, sets result
   - Otherwise: calls `target.SetValue(match.Token.Name, match.Value)`
   - Catches `TypeConversionException`: adds to result's Exceptions, continues
   - Catches `MissingMemberException`: if `IgnoreMissingProperties` → skip silently, otherwise adds to Exceptions, continues
   - Catches `TokenAssignmentException` (concatenation failure): adds to Exceptions, continues
4. Returns the `TokenizeResult<T>` with populated `Value` and any assignment exceptions.

#### Immutability

The original `TokenizeResult` is not modified. Multiple calls to `Assign<T>()` and `Assign<U>()` on the same result are safe — each produces an independent `TokenizeResult<T>`.

### Result Type Changes

#### `TokenizeResult<T>` construction

New internal constructor for projection from an existing result:

```csharp
internal TokenizeResult(Template template, TokenResult tokens, HintResult hints, DiagnosticResult? diagnostics)
```

The existing public constructor `TokenizeResult(Template template)` remains for backward compatibility but is no longer used by the `Tokenize<T>()` code path.

#### `Success` semantics

`Success` becomes `virtual` on `TokenizeResultBase`.

`TokenizeResult` (untyped) — no change:
```csharp
// Matching succeeded if we have matches, no required tokens missing, hints satisfied
public override bool Success => Tokens.HasMatches && !Tokens.HasMissingRequiredTokens && ...
```

`TokenizeResult<T>` — adds assignment check:
```csharp
// Object is usable only if matching succeeded AND reflection had no errors
public override bool Success => base.Success && Exceptions.Count == 0;
```

Stage 1 exceptions (from `CandidateProcessor` catch blocks) live only on the `TokenizeResult` and do not affect `TokenizeResult<T>.Success`. Only assignment exceptions (from `Assign<T>()`) are recorded on the typed result.

### ITokenizer API Changes

#### Sync

```csharp
public TokenizeResult<T> Tokenize<T>(Template template, string input) where T : class, new()
{
    return Tokenize(template, input).Assign<T>();
}
```

#### Async

```csharp
public async Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct)
    where T : class, new()
{
    var result = await TokenizeAsync(template, input, ct).ConfigureAwait(false);
    return result.Assign<T>();
}
```

Stream overloads delegate to TextReader overloads as today — no changes needed.

### Error Handling

All errors during `Assign<T>()` are non-fatal to the assignment loop. The object is populated best-effort, but `Success` is `false` if any exception was recorded.

| Exception | Stage 2 Behavior |
|-----------|-----------------|
| `TypeConversionException` | Added to Exceptions, skip property, continue. Indicates template decorator pipeline didn't produce a compatible type — template authoring error. |
| `MissingMemberException` | If `IgnoreMissingProperties` → skip silently. Otherwise → added to Exceptions, continue. |
| `TokenAssignmentException` | Added to Exceptions, continue. Concatenation type mismatch. |

### Usage Examples

**Current (single stage):**
```csharp
var result = tokenizer.Tokenize<MyObject>(template, input);
// result.Value is populated, result.Success reflects matching
```

**New — same convenience (unchanged API):**
```csharp
var result = tokenizer.Tokenize<MyObject>(template, input);
// Identical behavior, just internally does Tokenize() then Assign<T>()
```

**New — inspect then assign:**
```csharp
var matches = tokenizer.Tokenize(template, input);
if (matches.Success && matches.Contains("Name"))
{
    var typed = matches.Assign<MyObject>();
    // typed.Value is populated
    // typed.Success also checks assignment errors
}
```

**New — assign to multiple types:**
```csharp
var matches = tokenizer.Tokenize(template, input);
var person = matches.Assign<Person>();
var summary = matches.Assign<PersonSummary>();
```

## Files Changed

| File | Change |
|------|--------|
| `Tokenization/TokenAssigner.cs` | Rename to `DecoratorPipeline.cs`. Remove target/reflection logic. `Assign` → `Evaluate`, `CanAssign` → `CanEvaluate`. |
| `CandidateTokenList.cs` | Remove `target` param. `TryAssign` → `TryEvaluate`, `CanAnyAssign` → `CanAnyEvaluate`. |
| `Tokenization/CandidateProcessor.cs` | Remove `_targetObject`. Update calls to use new names. |
| `Tokenization/TokenizationSession.cs` | Remove `_targetObject`. Update constructor and `FrontMatterProcessor` call. |
| `Tokenization/TokenizationEngine.cs` | Remove `targetObject` from `CreateSession()`. |
| `Tokenization/FrontMatterProcessor.cs` | Remove `targetObject` param. |
| `TokenizeResultBase.cs` | Make `Success` virtual. |
| `TokenizeResult.cs` | Add `Assign<T>()` method. Override `Success`. |
| `TokenizeResult{T}.cs` (or same file) | Add projection constructor. Override `Success` to include exception check. |
| `Tokenizer.cs` | `Tokenize<T>()` → calls `Tokenize().Assign<T>()`. Same for async overloads. Remove `value` param from `TokenizeCore`/`TokenizeAsyncCore`. |
| `ITokenizer.cs` | No signature changes (generic overloads still exist). |
| `ITokenizationEngine.cs` | Remove `targetObject` from `CreateSession()`. |
| Test files | Update `TokenAssigner` tests → `DecoratorPipeline` tests. Add `Assign<T>()` tests. |

## Out of Scope

- Renaming `CandidateProcessor.TryAssign()` — it assigns to the *result*, which is still accurate.
- Changes to `TokenMatch`, `TokenResult`, or `Token` — these are unchanged.
- Any changes to the compilation pipeline.
- Performance optimizations to the matching loop (separate effort).
