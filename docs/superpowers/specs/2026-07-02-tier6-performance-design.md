# Tier 6: Performance — Design Spec

## Overview

Address remaining allocation and computation hotspots across the tokenizer library. These changes improve runtime performance, reduce GC pressure, and improve the debugging experience.

## Approach

Static fields + manual implementations (Approach A). No new abstractions, no conditional compilation. Follows existing codebase patterns.

---

## 1. Regex Caching

Promote three uncached `Regex.Split`/`Regex.Replace` calls to `private static readonly Regex` fields with `RegexOptions.Compiled`.

### StringExtensions.cs — `ToLines()`

```csharp
private static readonly Regex NewLineSplitRegex = new(@"\r\n|\r|\n", RegexOptions.Compiled);

// Replace Regex.Split(value, "\r\n|\r|\n") with:
return NewLineSplitRegex.Split(value);
```

### ToDateTimeTransformer.cs — ordinal suffix removal

```csharp
private static readonly Regex OrdinalSuffixRegex = new(@"\b(\d+)(?:st|nd|rd|th)\b", RegexOptions.Compiled);

// Replace Regex.Replace(valueToFormat, @"\b(\d+)(?:st|nd|rd|th)\b", "$1") with:
valueToFormat = OrdinalSuffixRegex.Replace(valueToFormat, "$1");
```

### PreambleNearMissHintGenerator.cs — whitespace normalization

```csharp
private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

// Replace Regex.Replace(value.Trim(), @"\s+", " ") with:
return WhitespaceRegex.Replace(value.Trim(), " ");
```

---

## 2. Reflection Caching

In `ObjectExtensions`, add a `MethodInfo` cache alongside the existing `PropertyCache`, following the same `ConcurrentDictionary` + `GetOrAdd` pattern.

```csharp
private static readonly ConcurrentDictionary<Type, MethodInfo> AddMethodCache = new();

// Replace:
//   var addMethod = list.GetType().GetMethod("Add") ?? throw ...
// With:
var addMethod = AddMethodCache.GetOrAdd(list.GetType(), t =>
    t.GetMethod("Add")
    ?? throw new InvalidOperationException($"Type {t.Name} does not have an Add method"));
```

---

## 3. Substring Allocation Elimination

### EndsWithNewLine (StringExtensions.cs)

Replace substring allocations with char indexing. The original checked for `"\n"` and `"\r\n"` separately, but both cases end with `'\n'`, so this simplifies to:

```csharp
public static bool EndsWithNewLine(this string value)
{
    return !string.IsNullOrEmpty(value) && value[value.Length - 1] == '\n';
}
```

### TrimLeadingSpaces (StringExtensions.cs)

Replace per-character substring allocation with char indexing. Remove the unused `StringBuilder`:

```csharp
public static string TrimLeadingSpaces(this string value)
{
    if (string.IsNullOrEmpty(value)) return value;

    for (var i = 0; i < value.Length; i++)
    {
        if (value[i] != ' ') return value.Substring(i);
    }

    return string.Empty;
}
```

---

## 4. Merge Triple Iteration in ProcessFrontMatterTokens

`TokenizationEngine.cs` — `ProcessFrontMatterTokens` iterates front matter tokens up to three times (lazy `Where`, `Count` for logging, `foreach`). Materialize once:

```csharp
var frontMatterTokens = template.Tokens.Where(t => t.IsFrontMatterToken).ToList();

if (log.IsEnabled(LogLevel.Trace))
{
    log.LogTrace("Processing {FrontMatterCount} front matter tokens", frontMatterTokens.Count);
}

foreach (var token in frontMatterTokens)
{
    // ...
}
```

---

## 5. ToString Overrides

Add compact, debugger-friendly `ToString()` overrides to all key types. Format examples in comments.

| Type | Format |
|------|--------|
| `FileLocation` | `"Ln 123, Col 10, Para 1"` (existing format, carried to record) |
| `TokenMatch` | `"TokenMatch('firstName' = 'John' @ Ln 5, Col 3, Para 1)"` |
| `Hint` | `"Hint('name')"` or `"Hint('name', Optional)"` |
| `HintMatch` | `"HintMatch('name' @ Ln 5, Col 3, Para 1)"` |
| `Template` | `"Template('name')"` or `"Template(3 tokens)"` |
| `TokenizeResult` | `"TokenizeResult(3 matched, 1 unmatched)"` |
| `TokenResult` | `"TokenResult('firstName' = 'John')"` |
| `HintResult` | `"HintResult(3 matches, 1 near-misses)"` |

Exact property names to be verified during implementation.

---

## 6. IEquatable and Record Conversions

### FileLocation — convert to record

Currently a mutable class with `private set` properties and a `Clone()` method. Convert to a sealed record:

```csharp
public sealed record FileLocation(int Line, int Column, int Paragraph)
{
    public override string ToString() => $"Ln {Line}, Col {Column}, Para {Paragraph}";
}
```

This provides `IEquatable<FileLocation>`, value equality, `GetHashCode`, and `with` expression support (replacing `Clone()`). Any code that mutates `FileLocation` after construction must be refactored to create new instances.

### HintMatch — add IEquatable manually

```csharp
public sealed class HintMatch : IEquatable<HintMatch>
{
    // existing init properties...

    public bool Equals(HintMatch? other) =>
        other is not null && Text == other.Text && Optional == other.Optional && Location == other.Location;

    public override bool Equals(object? obj) => Equals(obj as HintMatch);
    public override int GetHashCode() => HashCode.Combine(Text, Optional, Location);
}
```

Note: `HashCode.Combine` requires .NET Standard 2.1+ or .NET 6+. Need to verify availability on the netstandard2.0 target — may need a manual hash implementation or a polyfill.

### Hint, TokenMatch — no changes needed

Both are records and already have compiler-generated `IEquatable<T>`. With `FileLocation` becoming a record, `TokenMatch`'s value equality works correctly.

---

## Testing Strategy

- **Regex caching:** Existing tests should pass unchanged (behavioral equivalence). No new tests needed — the caching is an implementation detail.
- **Reflection caching:** Same — existing tests cover the behavior.
- **Substring elimination:** Existing `EndsWithNewLine` and `TrimLeadingSpaces` tests cover all edge cases. Verify they pass.
- **Triple iteration:** Existing front matter tests cover behavior. No new tests.
- **ToString:** Add tests for each type's `ToString()` output format.
- **IEquatable/records:** Add equality and `GetHashCode` tests for `FileLocation` (record) and `HintMatch`. Verify `TokenMatch` equality works with the new `FileLocation` record.
- **FileLocation conversion:** All existing tests that construct or mutate `FileLocation` will need updating. This is the highest-risk change — grep for all `FileLocation` usage and verify each site.

## Risk Assessment

- **Low risk:** Regex caching, reflection caching, substring elimination, iteration merge — pure implementation changes behind stable APIs.
- **Medium risk:** `FileLocation` record conversion — touches many call sites. Mutations become compile errors, which is good (compiler finds all sites) but may require non-trivial refactoring.
- **Low risk:** `ToString` overrides — additive, no existing behavior to break.
- **Low risk:** `HintMatch` IEquatable — additive.
