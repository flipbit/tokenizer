# Upgrading from Tokenizer v2 to v3

## Quick Checklist

- [ ] Rename `TokenMatcher` → `TemplateMatcher`
- [ ] Rename `Match` → `TokenMatch` (the individual match type)
- [ ] Replace `TokenizeResult<T>` with non-generic `TokenizeResult` + `Assign<T>()`
- [ ] Rename `ITokenTransformer.CanTransform` → `TryTransform`
- [ ] Replace `matcher.Match<T>()` calls with `matcher.Tokenize<T>()` or `matcher.Tokenize()`
- [ ] Move `RegisterTransformer<T>()` / `RegisterValidator<T>()` to `TokenizerOptions`
- [ ] Replace `TemplateCollection.Names` with `TemplateCollection` directly (it's `IReadOnlyCollection<Template>`)
- [ ] Replace `AssignedValue` / `AssignedLocation` with `AssignedValues` / `AssignedLocations` on `TokenDiagnostic`
- [ ] Handle `DateTimeOffset` instead of `DateTime` from date transformers
- [ ] Update any `(DateTime) match.Value` casts to `(DateTimeOffset)`

## Breaking Changes

### Renamed Types

| v2 | v3 |
|----|-----|
| `TokenMatcher` | `TemplateMatcher` |
| `Match` | `TokenMatch` |
| `MatchResult` / `TokenizeResult<T>` | `TemplateMatchResult` / `TokenizeResult` (non-generic) |

### Renamed Methods

| v2 | v3 |
|----|-----|
| `ITokenTransformer.CanTransform(object, string[], out object)` | `ITokenTransformer.TryTransform(object, string[], out object)` |
| `TokenMatcher.Match<T>(string)` | `TemplateMatcher.Tokenize<T>(string)` |
| `TokenMatcher.Match<T>(string, string[])` | `TemplateMatcher.Tokenize<T>(string, string[])` |

### Removed APIs

**`RegisterTransformer<T>()` / `RegisterValidator<T>()` on matcher**

These no longer exist on `TemplateMatcher`. Register custom transformers and validators
through `TokenizerOptions` at construction time instead. See [Migration Patterns](#registering-custom-transformers) below.

**`TemplateCollection.Names`**

`TemplateCollection` now implements `IReadOnlyCollection<Template>`. Use `.Count` for the
count, or enumerate directly to access templates by name via `TryGet()` or `Get()`.

**`TokenizeResult<T>` and `.Value`**

The generic `TokenizeResult<T>` has been removed. Use the non-generic `TokenizeResult` and
call `Assign<T>()` to project matches onto a typed object. See [Migration Patterns](#getting-typed-results) below.

### Changed Return Types

**Date transformers now produce `DateTimeOffset`**

`ToDateTime` and `ToDateTimeUtc` both return `DateTimeOffset` instead of `DateTime`.
This affects:

- Direct casts on `TokenMatch.Value` (e.g. `(DateTime) match.Value` → `(DateTimeOffset) match.Value`)
- The `Assign<T>()` method handles `DateTimeOffset` → `DateTime` conversion automatically
  via `DateTimeProjection`, so target objects with `DateTime` properties will still work

**`Tokenize<T>()` returns `T?` directly**

`TemplateMatcher.Tokenize<T>()` returns `T?` instead of a result wrapper. If you need access to the
underlying `TokenizeResult` (for match details, diagnostics, etc.), use the non-generic `Tokenize()` which
returns `TemplateMatchResult`.

### `AssignedValue` / `AssignedLocation` replaced with list-based properties

The singular `AssignedValue` and `AssignedLocation` properties on `TokenDiagnostic` have been replaced with `AssignedValues` (`IReadOnlyList<string>`) and `AssignedLocations` (`IReadOnlyList<FileLocation>`). These support repeating tokens which produce multiple values. For non-repeating tokens the lists contain a single element.

### `TokenMatch` is now a record

`TokenMatch` is a sealed record with init-only properties (`Token`, `Value`, `Location`). Code that
assigns to `match.Value` will no longer compile. If you need to transform a match value, do so
when reading it rather than mutating the match.

## New APIs

### TokenizerOptions

New properties for locale-aware date/time parsing:

```csharp
var options = new TokenizerOptions
{
    Culture = new CultureInfo("es"),          // for locale-specific month names, etc.
    DefaultOffset = TimeSpan.FromHours(2),    // UTC offset when none is in the data
    DefaultTimezone = "Europe/Berlin",        // IANA or Windows timezone ID
};

// Custom timezone abbreviation mappings
options = options.WithTimezoneAbbreviation("IST", TimeSpan.FromHours(5.5));
```

New fluent methods for registering custom transformers and validators:

```csharp
var options = new TokenizerOptions()
    .WithTransformer<MyCustomTransformer>()
    .WithValidator<MyCustomValidator>();
```

### Template Front Matter

New directives for per-template date/time configuration:

```yaml
---
name: my-template
culture: es
defaultOffset: +02:00
defaultTimezone: Europe/Berlin
---
```

### New Transformers and Validators (.NET 6+)

| Name | Produces | Description |
|------|----------|-------------|
| `ToDate` | `DateOnly` | Extracts date component |
| `ToTime` | `TimeOnly` | Extracts time component |
| `IsDate` | — | Validates date-only values |
| `IsTime` | — | Validates time-only values |

`IsDateTime` has been rewritten to use the new `TemporalParser`.

### Structured Diagnostics

Enable `TokenizerOptions.EnableDiagnostics = true` to get a token-centric diagnostic trace on `TokenizeResult.Diagnostics`. Each `TokenDiagnostic` includes the token's outcome, match attempts, assigned values with locations, and issues with adaptive hints and stable error codes (TK001–TK008). See [ARCHITECTURE.md](ARCHITECTURE.md#diagnostics-subsystem) for details.

### Options-Aware Decorators

New interfaces for transformers and validators that need access to `TokenizerOptions`
(e.g. for culture or timezone information):

- `IOptionsAwareTransformer` — extends `ITokenTransformer` with `TryTransform(object, string[], TokenizerOptions, out object)`
- `IOptionsAwareValidator` — extends `ITokenValidator` with `IsValid(object, string[], TokenizerOptions)`

Implement these instead of the base interfaces when your decorator needs culture, timezone,
or other options context.

### AssignmentFailedException.PartialResult

When `Assign<T>()` throws, the exception now carries the partially-populated target object:

```csharp
try
{
    var result = tokenizeResult.Assign<MyType>();
}
catch (AssignmentFailedException ex)
{
    // All assignable fields are populated; only failed conversions are skipped
    var partial = (MyType)ex.PartialResult!;
    var errorCount = ex.Errors.Count;
}
```

## Behavioural Changes

These won't cause compile errors but may change runtime results.

### Date/time values are `DateTimeOffset` throughout

All date transformers (`ToDateTime`, `ToDateTimeUtc`, `ToDate`, `ToTime`) now go through
`TemporalParser`, which produces `DateTimeOffset`. The `Assign<T>()` method automatically
converts `DateTimeOffset` to `DateTime`, `DateOnly`, or `TimeOnly` via `DateTimeProjection`
when the target property requires it. For UTC offsets, `DateTimeProjection` returns
`UtcDateTime`; for non-zero offsets, it returns `DateTime` (local representation).

If you access `TokenMatch.Value` directly and cast, update your casts from `DateTime` to
`DateTimeOffset`.

### `ToDateTimeUtc` is deprecated

`ToDateTimeUtc` still works but is marked `[Obsolete]`. Use `ToDateTime` instead, combined with
`defaultOffset: +00:00` in front matter or `DefaultOffset = TimeSpan.Zero` in options to
force UTC interpretation.

### `Assign<T>()` populates all fields before throwing

`Assign<T>()` processes every matched token, catches per-property errors, and only throws
`AssignmentFailedException` after the full iteration. The `PartialResult` on the exception
contains all successfully assigned values. This means a single bad field won't prevent
other fields from being set.

### Non-English date parsing requires `culture`

The v2 date parsing was more lenient with locale-specific month names. In v3,
`TemporalParser` uses `CultureInfo.InvariantCulture` by default, which only recognises
English month names. If your templates parse dates with non-English months
(e.g. `16-abr-1997` for Spanish), set the `culture` front matter directive or
`TokenizerOptions.Culture`.

## Migration Patterns

### Registering Custom Transformers

**Before (v2):**

```csharp
var matcher = new TokenMatcher();
matcher.RegisterTransformer<MyTransformer>();
matcher.RegisterValidator<MyValidator>();
```

**After (v3):**

```csharp
var options = new TokenizerOptions()
    .WithTransformer<MyTransformer>()
    .WithValidator<MyValidator>();

var matcher = new TemplateMatcher(options);
```

Note: transformers and validators must be registered before templates are compiled, since
decorators are resolved at compile time. The v2 pattern of registering after construction
is no longer supported.

### Getting Typed Results

**Before (v2):**

```csharp
var result = matcher.Match<MyType>(input, tags);

if (result.BestMatch != null)
{
    var value = result.BestMatch.Value;       // MyType
    var matches = result.BestMatch.Tokens;    // token details
    var errors = result.BestMatch.Exceptions; // errors
}
```

**After (v3) — simple case (just need the typed object):**

```csharp
var value = matcher.Tokenize<MyType>(input, tags);

if (value != null)
{
    // value is MyType directly
}
```

**After (v3) — need match details:**

```csharp
var result = matcher.Tokenize(input, tags);

if (result.BestMatch != null)
{
    var match = result.BestMatch;
    var value = match.Assign<MyType>();         // project onto typed object
    var matches = match.Tokens.Matches;         // IReadOnlyList<TokenMatch>
    var errors = match.Exceptions;              // IReadOnlyList<Exception>
}
```

### Working with Date Values

**Before (v2):**

```csharp
var dateTime = (DateTime)match.Value;
```

**After (v3):**

```csharp
// Option 1: Cast to DateTimeOffset
var dto = (DateTimeOffset)match.Value;

// Option 2: Convert to DateTime explicitly
var dateTime = ((DateTimeOffset)match.Value).UtcDateTime;

// Option 3: Let Assign<T>() handle it (automatic conversion to target property type)
var result = tokenizeResult.Assign<MyType>();  // DateTime properties converted automatically
```

### Implementing Custom Transformers

**Before (v2):**

```csharp
public class MyTransformer : ITokenTransformer
{
    public bool CanTransform(object value, string[] args, out object transformed)
    {
        // ...
    }
}
```

**After (v3):**

```csharp
public class MyTransformer : ITokenTransformer
{
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        // ...
    }
}
```

If your transformer needs access to culture or timezone options, implement
`IOptionsAwareTransformer` instead:

```csharp
public class MyTransformer : IOptionsAwareTransformer
{
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        var culture = options.Culture ?? CultureInfo.InvariantCulture;
        // ...
    }
}
```

### Handling Assignment Errors Gracefully

**Before (v2):**

```csharp
var result = matcher.Match<MyType>(input);
var value = result.BestMatch.Value;           // partially populated on errors
var errorCount = result.BestMatch.Exceptions.Count;
```

**After (v3):**

```csharp
var result = matcher.Tokenize(input);
var match = result.BestMatch;

MyType value;
var assignmentErrors = 0;

try
{
    value = match.Assign<MyType>();
}
catch (AssignmentFailedException ex)
{
    value = (MyType)ex.PartialResult!;
    assignmentErrors = ex.Errors.Count;
}

var totalErrors = match.Exceptions.Count + assignmentErrors;
```
