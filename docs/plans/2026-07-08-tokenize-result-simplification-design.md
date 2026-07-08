# TokenizeResult Simplification Design

## Status

Proposed — 2026-07-08

## Goal

Simplify the tokenization result API by completing the two-stage pipeline introduced in
the previous commit series. Stage 1 (matching) returns `TokenizeResult`. Stage 2
(assignment) is an explicit `Assign<T>()` that returns `T` directly. All generic result
wrapper types are eliminated.

## Changes

### 1. Collapse `TokenizeResultBase` into `TokenizeResult`

`TokenizeResultBase` exists only because `TokenizeResult<T>` subclassed it. With
`TokenizeResult<T>` going away, the hierarchy is unnecessary.

- Move all members from `TokenizeResultBase` into `TokenizeResult`
- Delete `TokenizeResultBase.cs`
- `Success` becomes non-virtual (no overrides remain)
- Remove the projection constructor (was only used by `TokenizeResult<T>`)

### 2. Remove query methods from `TokenizeResult`

Remove `First()`, `First<T>()`, `FirstOrDefault()`, `FirstOrDefault<T>()`, `All()`, and
`Contains()`. The `Matches` collection (`IReadOnlyList<TokenMatch>`) is already public —
callers use LINQ directly.

### 3. Remove dictionary assignment path

The `IDictionary<string, object>` branch inside `Assign<T>()` is deleted. With
`TokenizeResult.Matches` exposed directly, callers who want a loose key-value bag can do:

```csharp
var dict = result.Matches.ToDictionary(m => m.Token.Name, m => m.Value);
```

This is more explicit and gives the caller control over duplicate-key handling.

### 4. `Assign<T>()` returns `T`

`Assign<T>()` on `TokenizeResult` creates a `new T()`, iterates `Matches`, and assigns
values to properties via reflection (`SetValue`).

- Returns `T` on success
- Throws `AssignmentFailedException` (with `IReadOnlyList<Exception> Errors`) if any
  assignment errors occur (type conversion failure, missing member when
  `IgnoreMissingProperties` is false, etc.)
- Errors are collected across all matches before throwing — the caller sees all failures,
  not just the first

### 5. Delete `TokenizeResult<T>`

No longer needed. `Assign<T>()` returns `T` directly.

### 6. New `AssignmentFailedException` for aggregate errors

The existing `TokenAssignmentException` is a single-token error (carries a `Token`
property, thrown when one property can't be set). Aggregate assignment failure needs a
separate type:

```csharp
public class AssignmentFailedException : TokenizerException
{
    public AssignmentFailedException(string message, IReadOnlyList<Exception> Errors)
        : base(message)
    {
        Errors = errors;
    }

    public IReadOnlyList<Exception> Errors { get; }
}
```

`Assign<T>()` collects individual errors (which may include `TokenAssignmentException`,
`MissingMemberException`, `TypeConversionException`, etc.) and wraps them in a single
`AssignmentFailedException`.

### 7. `Tokenizer` / `ITokenizer` — updated signatures

`Tokenize<T>()` returns `T?`:
- Returns `T` when matching succeeds and assignment succeeds
- Returns `null` when matching fails (`Success == false`)
- Throws `AssignmentFailedException` when matching succeeds but assignment fails

```csharp
public T? Tokenize<T>(Template template, string input) where T : class, new()
{
    var result = Tokenize(template, input);
    if (!result.Success) return null;
    return result.Assign<T>();
}
```

Async variants (`TokenizeAsync<T>()`) follow the same pattern, returning `Task<T?>`.

Untyped `Tokenize()` and `TokenizeAsync()` are unchanged.

### 8. `TokenMatcher` → `TemplateMatcher` rename

Rename all types:
- `TokenMatcher` → `TemplateMatcher`
- `ITokenMatcher` → `ITemplateMatcher`
- `TokenMatcherResult` → `TemplateMatchResult`
- `TokenMatcherResult<T>` → deleted (non-generic only)
- `TokenMatcherException` → `TemplateMatcherException`

### 9. `TemplateMatcher` — `Match`/`MatchAsync` → `Tokenize`/`TokenizeAsync`

Aligns with `ITokenizer` naming. Same two-stage concept:

**Untyped — diagnostic entry point:**
```csharp
public TemplateMatchResult Tokenize(string input, string[]? tags = null)
```
Runs Stage 1 on all templates, returns `TemplateMatchResult` with `BestMatch` and
`Results`. Caller can do `result.BestMatch.Assign<T>()` for the two-stage flow.

**Typed — convenience:**
```csharp
public T? Tokenize<T>(string input, string[]? tags = null) where T : class, new()
```
Internally calls untyped `Tokenize()`, picks `BestMatch`, calls `Assign<T>()` on it.
Returns `null` if no template matched. Throws `AssignmentFailedException` if assignment
fails.

**Async variants** mirror the sync ones.

### 10. `TemplateMatchResult` — non-generic only

```csharp
public sealed class TemplateMatchResult
{
    public IReadOnlyList<TokenizeResult> Results { get; }
    public TokenizeResult? BestMatch { get; }
    public bool Success => BestMatch != null;
}
```

`TemplateMatchResult<T>` is deleted.

### 11. Internal simplification

**`MatchCore` in `TemplateMatcher`:** Currently generic over both result type and
tokenize-result type. Simplifies to always use `TokenizeResult` — no generic variance
needed since there's only one result type.

**Assignment only on the winner:** The typed `Tokenize<T>()` path runs `Assign<T>()` on
the best match only, not on every template. This is both more correct and more efficient
than the pre-refactor behavior.

## Error contract summary

| Scenario | `Tokenizer.Tokenize<T>()` | `TemplateMatcher.Tokenize<T>()` |
|---|---|---|
| Match succeeds, assignment succeeds | Returns `T` | Returns `T` |
| Match fails | Returns `null` | Returns `null` (no template matched) |
| Match succeeds, assignment fails | Throws `AssignmentFailedException` | Throws `AssignmentFailedException` |

## Types deleted

- `TokenizeResultBase`
- `TokenizeResult<T>`
- `TokenMatcherResult<T>`
- `TokenMatcherResult` (renamed to `TemplateMatchResult`)
- `TokenMatcherException` (renamed to `TemplateMatcherException`)
- `ITokenMatcher` (renamed to `ITemplateMatcher`)
- `TokenMatcher` (renamed to `TemplateMatcher`)

## Files affected

### Source (modify)
- `TokenizeResult.cs` — collapse base class, remove query methods, remove dictionary path, `Assign<T>()` returns `T`
- `Tokenizer.cs` — `Tokenize<T>()` returns `T?`, async variants return `Task<T?>`
- `ITokenizer.cs` — updated signatures
- New `AssignmentFailedException.cs` — aggregate assignment error with `Errors` collection
- `TokenMatcherResult.cs` → `TemplateMatchResult.cs` — non-generic only, renamed
- `TokenMatcher.cs` → `TemplateMatcher.cs` — renamed, `Match` → `Tokenize`, simplified internals
- `ITokenMatcher.cs` → `ITemplateMatcher.cs` — renamed, updated signatures
- `TokenMatcherException.cs` → `TemplateMatcherException.cs` — renamed
- `TokenizerServiceCollectionExtensions.cs` — update DI registrations

### Source (delete)
- `TokenizeResultBase.cs`

### Tests (modify)
- `TokenizeResultAssignTests.cs` — update for `T` return type, remove dictionary tests
- `TokenMatcherTests.cs` → `TemplateMatcherTests.cs` — update for new API
- `TokenMatcherResultTests.cs` → `TemplateMatchResultTests.cs` — update for non-generic
- `TokenMatcherAsyncTests.cs` → `TemplateMatcherAsyncTests.cs` — update for new API
- `SampleTests.cs` — update matcher usage
- `TokenizerTests.cs` — update `Tokenize<T>()` call sites (no `.Value`)
- `ImmutableCollectionsTests.cs` — update type references
- `SealedClassTests.cs` — update type references
- All test files using `Tokenize<T>().Value` pattern — remove `.Value`

### Benchmarks (modify)
- `TokenizationBenchmarks.cs` — update call sites
- `AsyncTokenizationBenchmarks.cs` — update call sites
- `CompilationCacheBenchmarks.cs` — update call sites
