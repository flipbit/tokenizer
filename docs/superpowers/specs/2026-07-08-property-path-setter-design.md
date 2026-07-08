# PropertyPathSetter Design

## Overview

Replace `ObjectExtensions` with a dedicated `PropertyPathSetter` class that reflects values onto .NET objects via dot-separated property paths. Decompose the 136-line monolithic `SetInnerValue` method into focused, testable components. Add exhaustive type coverage and collection support. Addresses code review issues D3 (ObjectExtensions decomposition) and H2 (unbounded recursion depth).

## Motivation

`ObjectExtensions.SetInnerValue` mixes three responsibilities: property lookup, path traversal with intermediate auto-instantiation, and leaf value assignment across multiple type categories (scalars, nullables, enums, lists). The method is difficult to test in isolation and lacks support for common .NET types (`Guid`, `TimeSpan`, `DateTimeOffset`) and collection types beyond `List<T>`/`IList<T>`.

The recent two-stage pipeline refactoring (commit `cb976fd`) isolated reflection into `TokenizeResult.Assign<T>()`, making this the right time to replace the underlying reflection utility.

## Design

### 1. New class: PropertyPathSetter

**File:** `src/Tokenizer/Reflection/PropertyPathSetter.cs`
**Namespace:** `Tokens.Reflection`
**Visibility:** `internal sealed`

#### Public API

```csharp
internal sealed class PropertyPathSetter
{
    // Set a single scalar value at the given property path
    void SetScalar(object target, string path, object value, StringComparison comparison);

    // Set a collection of values at the given property path
    void SetCollection(object target, string path, IReadOnlyList<object> values, StringComparison comparison);

    // Resolve the leaf property at the given path and check whether it is a supported collection type
    bool IsCollectionProperty(Type targetType, string path, StringComparison comparison);
}
```

#### Internal decomposition

The class is internally decomposed into focused private methods:

- **`FindProperty(Type type, string segment, StringComparison)`** — cached `PropertyInfo` lookup by segment name. Returns `PropertyInfo` or throws `MissingMemberException`.
- **`TraverseToLeaf(object target, string[] segments, int startDepth, StringComparison)`** — walks path segments, auto-instantiates null intermediates, enforces max depth, returns `(object leafTarget, PropertyInfo leafProperty)`.
- **`AssignScalar(object target, PropertyInfo property, object value)`** — type conversion dispatch and property assignment for scalar values.
- **`AssignCollection(object target, PropertyInfo property, IReadOnlyList<object> values)`** — detects target collection type, batch converts values, assigns in a single operation.
- **`ConvertValue(object value, Type targetType)`** — type conversion with support for all categories (see Section 2).

#### Caches

All caches are owned by `PropertyPathSetter` as private static fields:

- `ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache` — cached properties per type
- `ConcurrentDictionary<string, string[]> PathSegmentCache` — cached path splits

#### Depth limit

Traversal enforces a max depth of 10 segments. Exceeding this throws `InvalidOperationException` with a descriptive message. This closes the H2 security gap (unbounded recursive property traversal).

#### Path prefix stripping

When the first path segment matches the target type name, it is skipped (existing behavior preserved).

### 2. Type conversion

`ConvertValue` handles these target types in priority order:

| Priority | Category | Types | Strategy |
|----------|----------|-------|----------|
| 1 | Already correct type | any | Return as-is |
| 2 | Enum | any enum | `Enum.Parse(type, string, ignoreCase: true)` |
| 3 | Nullable\<T\> | `int?`, `DateTime?`, etc. | Unwrap to `T`, recurse |
| 4 | Non-IConvertible structs | `Guid`, `TimeSpan`, `DateTimeOffset` | `Guid.Parse`, `TimeSpan.Parse`, `DateTimeOffset.Parse` with `CultureInfo.InvariantCulture` |
| 5 | Non-IConvertible structs (.NET 6+) | `DateOnly`, `TimeOnly` | Behind `#if NET6_0_OR_GREATER`, same parse approach |
| 6 | IConvertible primitives | `string`, `bool`, `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `DateTime` | `Convert.ChangeType(value, type, CultureInfo.InvariantCulture)` |

Failed conversions throw `TypeConversionException` with the value, target type, and inner exception.

### 3. Collection support

`AssignCollection` detects the target property type and performs a single batch assignment. Detection order matters — most specific first:

| Priority | Target Type | Strategy |
|----------|-------------|----------|
| 1 | `T[]` (IsArray) | Convert all values, call `ToArray()`, assign |
| 2 | `HashSet<T>` | Convert all values into `HashSet<T>`, verify `Count == values.Count` — if not, throw `InvalidOperationException` identifying the duplicate value |
| 3 | `ImmutableList<T>` | Detect by generic type definition name at runtime (no hard dependency on `System.Collections.Immutable`), build via `ImmutableList.CreateRange()` |
| 4 | `ImmutableArray<T>` | Same runtime detection, build via `ImmutableArray.CreateRange()` |
| 5 | `List<T>` / `IList<T>` / `ICollection<T>` | Create `List<T>`, convert and add all values, assign |

**Getter-only collection properties:** If the property has no setter but has an existing non-null collection instance, add to it directly (existing behavior for `IList<T>` preserved). Only applies to `List<T>`/`IList<T>`/`ICollection<T>` — getter-only arrays and immutables throw `InvalidOperationException`.

**Unsupported collection types** throw `InvalidOperationException` with a descriptive message suggesting `List<T>`:
- `Dictionary<K,V>` — no key/value semantics in the token model
- `IEnumerable<T>` (bare interface)
- Any other collection type not in the table above

**Immutable collections note:** Detected by checking `Type.GetGenericTypeDefinition().FullName` against known type names at runtime. This avoids a hard package dependency on `System.Collections.Immutable` for consumers who don't use them. If the immutable types aren't available at runtime, the property is treated as unsupported.

### 4. Unsupported scenarios with explicit exceptions

| Scenario | Exception | Message |
|----------|-----------|---------|
| Struct as intermediate path segment | `InvalidOperationException` | Value types cannot be used as intermediate path segments (mutations would not propagate) |
| Interface/abstract class as intermediate | `InvalidOperationException` | Cannot auto-instantiate interface or abstract type |
| Read-only scalar property | `InvalidOperationException` | Property is read-only |
| Depth limit exceeded | `InvalidOperationException` | Property path exceeds maximum depth of 10 |
| Missing property | `MissingMemberException` | Could not find property '{name}' on {type} |
| Type conversion failure | `TypeConversionException` | Unable to convert '{value}' to type {type} |
| HashSet duplicate | `InvalidOperationException` | Duplicate value '{value}' for HashSet property {name} |
| Unsupported collection type | `InvalidOperationException` | Collection type {type} is not supported; use List<T> |

### 5. Changes to Assign\<T\>()

**File:** `src/Tokenizer/TokenizeResult.cs`

`AssignToObject` is rewritten with grouping logic:

```
1. Create PropertyPathSetter instance
2. Group matches by Token.Name
3. For each group:
   a. Check setter.IsCollectionProperty(typeof(T), path, comparison)
   b. Collection property → setter.SetCollection(target, path, allValues, comparison)
   c. Scalar property → setter.SetScalar(target, path, singleValue, comparison)
4. Catch MissingMemberException:
   - IgnoreMissingProperties = true → silently skip
   - IgnoreMissingProperties = false → record on result
5. Catch TypeConversionException / InvalidOperationException → record on result
```

The `AssignToDictionary` method is unaffected by this change.

### 6. Deleted code

- `src/Tokenizer/Extensions/ObjectExtensions.cs` — entire file removed
- All three existing test files for ObjectExtensions removed (replaced by new test files)
- `GetValue` / `GetValue<T>` / `GetInnerValue<T>` — removed entirely (zero production callers)

### 7. File layout

| Action | Path |
|--------|------|
| **New** | `src/Tokenizer/Reflection/PropertyPathSetter.cs` |
| **Delete** | `src/Tokenizer/Extensions/ObjectExtensions.cs` |
| **Modify** | `src/Tokenizer/TokenizeResult.cs` |
| **Modify** | `CLAUDE.md` |
| **Delete** | `tests/Tokenizer.Tests/Extensions/ObjectExtensionsTests.cs` |
| **Delete** | `tests/Tokenizer.Tests/Extensions/ObjectExtensionsPathTests.cs` |
| **Delete** | `tests/Tokenizer.Tests/Extensions/ObjectExtensionsPropertyCacheTests.cs` |
| **New** | `tests/Tokenizer.Tests/Reflection/PropertyPathSetterTests.cs` |
| **New** | `tests/Tokenizer.Tests/Reflection/PropertyPathSetter.ScalarTypes.Tests.cs` |
| **New** | `tests/Tokenizer.Tests/Reflection/PropertyPathSetter.Collections.Tests.cs` |
| **New** | `tests/Tokenizer.Tests/Reflection/PropertyPathSetter.Pipeline.Tests.cs` |

## Testing

### Tier 1: PropertyPathSetterTests.cs — Core path traversal

- Flat property (`"Name"`)
- Type-prefixed path (`"TestTarget.Name"`)
- Nested path with auto-instantiation (`"Inner.Value"`)
- Deeply nested (3+ levels)
- Case-insensitive matching
- Missing property → `MissingMemberException`
- Max depth (10) exceeded → `InvalidOperationException`
- Struct intermediate → `InvalidOperationException`
- Interface/abstract intermediate → `InvalidOperationException`
- Read-only scalar property → `InvalidOperationException`
- Null/empty path → `ArgumentNullException`

### Tier 2: PropertyPathSetter.ScalarTypes.Tests.cs — Exhaustive type conversion

For each type, test setting from a string value and verify the property value matches:

- **IConvertible primitives:** `string`, `bool`, `char`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `DateTime`
- **Nullable\<T\>:** nullable wrapper for each primitive above — set value + set null
- **Enum:** by name, case-insensitive match, invalid value → exception
- **Non-IConvertible:** `Guid`, `TimeSpan`, `DateTimeOffset`
- **Non-IConvertible (.NET 6+):** `DateOnly`, `TimeOnly` (conditional compilation)
- **Pass-through:** value already the correct type (no conversion needed)
- **Invalid conversion:** e.g. `"abc"` → `int` → `TypeConversionException`

### Tier 3: PropertyPathSetter.Collections.Tests.cs — Collection assignment

- `List<T>` — multiple values assigned correctly
- `IList<T>` — same
- `ICollection<T>` — same
- `T[]` — values converted to array, correct element type
- `HashSet<T>` — unique values succeed
- `HashSet<T>` — duplicate values throw with duplicate value in message
- `ImmutableList<T>` — values assigned correctly
- `ImmutableArray<T>` — values assigned correctly
- Element type conversion within collections (e.g. `List<int>` from string values)
- Unsupported collection type → `InvalidOperationException`
- Empty value list → empty collection assigned
- Getter-only list property with existing instance → adds to existing
- Getter-only array/immutable property → `InvalidOperationException`
- Single value to collection property → single-element collection
- `IsCollectionProperty` returns true/false for each supported/unsupported type

### Tier 4: PropertyPathSetter.Pipeline.Tests.cs — End-to-end through tokenizer

- Target class with one property of each scalar type → template with tokens for each → `Assign<T>()` → all properties set correctly (1:1 match with Tier 2)
- Target class with collection properties → template with repeated tokens → `Assign<T>()` → collections populated correctly
- Multi-value grouping: two tokens with same name → single collection property
- Mixed scalar + collection properties on same target
- `IgnoreMissingProperties = true` → token with no matching property → value silently dropped, other properties still assigned
- `IgnoreMissingProperties = false` → token with no matching property → `AssignmentFailedException`
- Type conversion failure during pipeline → recorded as assignment error

## CLAUDE.md Update

Add to the Testing Conventions section:

```
- **File naming**: Test file matches production class: `{ClassName}Tests.cs`. If a single test fixture is too crowded, split into `{ClassName}.{Scenario}.Tests.cs`
```
