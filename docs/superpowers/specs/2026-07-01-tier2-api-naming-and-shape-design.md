# Tier 2: API Naming and Shape — Design Spec

Rename and reshape the public API to follow .NET Framework Design Guidelines before v3 ships.

Since v3 has not shipped, all changes are free to break the current API without backward-compatibility concerns.

---

## Change 1: Rename `CanTransform` to `TryTransform`

**What:** Rename `CanTransform` to `TryTransform` on `ITokenTransformer` and `TokenDecoratorContext`.

**Why:** The current name implies a pure check, but the method performs the transformation. The signature already follows the .NET `bool TryX(... out T)` pattern — it just has the wrong name.

**Scope:**
- `ITokenTransformer.CanTransform` → `TryTransform` (interface definition)
- `TokenDecoratorContext.CanTransform` → `TryTransform` (wrapper method)
- All 16+ transformer implementations
- All call sites in `Token.cs`
- All test files referencing `CanTransform`

**Signature unchanged:**
```csharp
bool TryTransform(object value, string[] args, out object transformed);
```

---

## Change 2: Rename `Match` to `TokenMatch` and convert to record

**What:** Rename the `Match` class to `TokenMatch`, convert from a sealed class to a sealed positional record.

**Why:** `Match` collides with `System.Text.RegularExpressions.Match`. The class is a small data carrier with no behavior — a record gives us init-only properties, value equality, and `with` expressions for free.

**Before:**
```csharp
public sealed class Match
{
    public Match(Token token, object value, FileLocation location) { ... }
    public Token Token { get; set; }
    public object Value { get; set; }
    public FileLocation Location { get; set; }
}
```

**After:**
```csharp
public sealed record TokenMatch(Token Token, object Value, FileLocation Location);
```

**Required refactor:** In `TokenResult.TryConcatMatch()`, the in-place mutation `match.Value = concatenated` must be replaced. Replace the match in the list using `match with { Value = concatenated }`.

**File rename:** `Match.cs` → `TokenMatch.cs`

---

## Change 3: Convert `Hint` to record

**What:** Convert `Hint` from a sealed class with mutable properties to a sealed positional record. Remove the manual `Clone()` method.

**Why:** `Hint` is a small data carrier. A record gives init semantics, value equality, and `with`-expression cloning — making `Clone()` redundant.

**Before:**
```csharp
public sealed class Hint
{
    public string Text { get; set; } = string.Empty;
    public bool Optional { get; set; }
    public Hint Clone() { ... }
}
```

**After:**
```csharp
public sealed record Hint(string Text = "", bool Optional = false);
```

**Callers using `Clone()`** switch to `hint with { }`.

**Callers using object initializer syntax** (`new Hint { Text = "..." }`) switch to constructor syntax (`new Hint(Text: "...")`).

---

## Change 4: Rename `CandidateTokenList.Any` to `HasCandidates`

**What:** Rename the `Any` property to `HasCandidates`.

**Why:** `Any` reads as a LINQ method, not a property. `HasCandidates` clearly communicates intent.

**Scope:** ~10 references across `TokenizationEngine` and test files.

**Implementation unchanged:** `public bool HasCandidates => Count > 0;`

---

## Change 5: Rename `TokenEnumerator.Match()` to `TryMatch()`

**What:** Rename both overloads of `Match()` to `TryMatch()`.

**Why:** `Match()` is ambiguous — `TryMatch()` signals that the method attempts a match and returns success/failure.

**Scope:** ~5 call sites in `TokenizationEngine` and `HintProcessor`.

**Signatures unchanged:**
```csharp
public bool TryMatch(string value);
public bool TryMatch(IEnumerable<Token> tokens, bool outOfOrderTokens, IList<Token> matches);
```

---

## Change 6: Make `TokenizeResult<T>.Value` init-only

**What:** Change `Value` from `{ get; set; }` to `{ get; init; }`.

**Why:** Result objects should not allow consumer reassignment of the extracted value after tokenization completes.

**Before:** `public T Value { get; set; }`
**After:** `public T Value { get; init; }`

**Impact:** Constructor assignment (`Value = new T()`) and object initializers in builders still work. No external consumer should be reassigning `Value` after receiving a result.

---

## Change 7: Make tokenization infrastructure interfaces and implementations internal

**What:** Change the following 8 types from `public` to `internal`:

| Interface | Implementation |
|-----------|----------------|
| `ITokenizationEngine` | `TokenizationEngine` |
| `IHintProcessor` | `HintProcessor` |
| `IResultBuilder` | `ResultBuilder` |
| `ITokenizationContext` | `TokenizationContext` |

**Why:** These types were extracted from the monolithic `Tokenizer` class during a v3 refactoring. They are implementation details — no documentation, no examples, no external use case. The library's true extension points are `ITokenTransformer` and `ITokenValidator`. Keeping these public expands the API surface with contracts that have no stability guarantees.

**DI impact:** `TokenizerServiceCollectionExtensions` registers these as singletons. Registration code lives inside the library assembly, so `internal` types work fine.

**Test impact:** `InternalsVisibleTo("Tokenizer.Tests")` is already configured in `AssemblyInfo.cs`. All 19 test files that reference these types will continue to compile.

---

## Execution Order

Each change is an independent commit, executed in this order:

1. `CanTransform` → `TryTransform` (most files touched, pure rename)
2. `Match` → `TokenMatch` + record conversion (rename + structural)
3. `Hint` → record conversion (structural, drop `Clone()`)
4. `CandidateTokenList.Any` → `HasCandidates` (small rename)
5. `TokenEnumerator.Match()` → `TryMatch()` (small rename)
6. `TokenizeResult<T>.Value` → `init` setter (one-line change)
7. Internalize infrastructure types (visibility change, do last since it touches DI)

Order rationale: renames before structural changes before visibility changes. The internalization goes last because it's the broadest visibility change and benefits from all other renames being settled first.
