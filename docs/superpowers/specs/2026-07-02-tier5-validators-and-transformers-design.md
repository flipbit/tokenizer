# Tier 5: Missing Validators and Transformers

**Date**: 2026-07-02
**Status**: Draft
**Roadmap Reference**: Tier 5 in `docs/ROADMAP.md`

---

## Overview

Add 14 new built-in validators and transformers to fill functional gaps in the decorator set. All items follow established patterns — sealed classes, parameterless constructors, registered in `TokenParser`, tested with the existing `Given_When_Then` convention.

---

## Validators (6)

### IsAlphanumericValidator

- **Args**: None
- **Behavior**: Returns `true` if every character is a letter or digit (`char.IsLetterOrDigit`). Null or empty returns `false`.
- **Usage**: `{ Token : IsAlphanumeric }`

### IsIntegerValidator

- **Args**: None
- **Behavior**: Returns `true` if the string parses as `long` via `long.TryParse` with `CultureInfo.InvariantCulture`. Distinct from `IsNumericValidator` which uses `float.TryParse` and accepts decimals.
- **Usage**: `{ Token : IsInteger }`

### IsGuidValidator

- **Args**: None
- **Behavior**: Returns `true` if the string parses as `Guid` via `Guid.TryParse`.
- **Usage**: `{ Token : IsGuid }`

### IsIpAddressValidator

- **Args**: None
- **Behavior**: Returns `true` if the string parses via `System.Net.IPAddress.TryParse`. Covers both IPv4 and IPv6.
- **Usage**: `{ Token : IsIpAddress }`

### IsInRangeValidator

- **Args**: Two required — `min`, `max`
- **Behavior**: Parses the value as `decimal` with `CultureInfo.InvariantCulture`. Returns `true` if `min <= value <= max` (inclusive). Returns `false` if the value cannot be parsed as a decimal. Throws `ArgumentException` if args are missing or not valid decimals.
- **Usage**: `{ Token : IsInRange(1, 100) }`

### MatchesRegexValidator

- **Args**: One required — the regex pattern
- **Behavior**: Returns `true` if `Regex.IsMatch(value, pattern)`. Inline flags (`(?i)`, `(?m)`, etc.) supported natively by .NET's regex engine. Throws `ArgumentException` if arg is missing.
- **Usage**: `{ Token : MatchesRegex(^\d{3}-\d{4}$) }`

---

## Transformers (8)

### ToIntTransformer

- **Args**: None
- **Behavior**: Parses value as `int` via `int.TryParse` with `CultureInfo.InvariantCulture`. Returns the `int` as the transformed value. Returns `false` from `TryTransform` on parse failure.
- **Usage**: `{ Token : ToInt }`

### ToDecimalTransformer

- **Args**: None
- **Behavior**: Parses value as `decimal` via `decimal.TryParse` with `CultureInfo.InvariantCulture`. Returns the `decimal` as the transformed value. Returns `false` from `TryTransform` on parse failure.
- **Usage**: `{ Token : ToDecimal }`

### ToBooleanTransformer

- **Args**: None
- **Behavior**: Case-insensitive matching: `true`/`yes`/`1` produce `true`; `false`/`no`/`0` produce `false`. Returns `false` from `TryTransform` for any other input. Null or empty returns `false` from `TryTransform`.
- **Usage**: `{ Token : ToBoolean }`

### ToGuidTransformer

- **Args**: None
- **Behavior**: Parses value as `Guid` via `Guid.TryParse`. Returns the `Guid` as the transformed value. Returns `false` from `TryTransform` on parse failure.
- **Usage**: `{ Token : ToGuid }`

### TruncateTransformer

- **Args**: One required — max length (int)
- **Behavior**: If the string length exceeds max length, returns the first N characters. If shorter or equal, returns unchanged. Null or empty returns empty string. Throws `ArgumentException` if arg is missing or not a valid int.
- **Usage**: `{ Token : Truncate(50) }`

### DefaultValueTransformer

- **Args**: One required — the fallback value
- **Behavior**: If the input value is null or `string.Empty`, returns the fallback arg. Otherwise returns the input unchanged. Always returns `true` from `TryTransform`. Whitespace-only strings are NOT coalesced (chain `Trim` first if needed).
- **Usage**: `{ Token : DefaultValue(N/A) }`

### RegexReplaceTransformer

- **Args**: Two required — pattern, replacement
- **Behavior**: Applies `Regex.Replace(value, pattern, replacement)`. Inline flags supported via the pattern. Null or empty returns empty string. Throws `ArgumentException` if args are missing.
- **Usage**: `{ Token : RegexReplace(\d+, #) }`

### TitleCaseTransformer

- **Args**: None
- **Behavior**: Converts value to title case using `CultureInfo.InvariantCulture.TextInfo.ToTitleCase` on the lowercased input. Null or empty returns empty string.
- **Usage**: `{ Token : TitleCase }`

---

## Registration

All 14 types are registered in the `TokenParser` constructor (`src/Tokenizer/Compilation/TokenParser.cs`) alongside the existing built-in validators and transformers, using `RegisterValidator<T>()` and `RegisterTransformer<T>()`.

---

## Testing

One test file per validator/transformer, following established conventions:

- **Location**: `tests/Tokenizer.Tests/Validators/` and `tests/Tokenizer.Tests/Transformers/`
- **Base class**: Validators inherit `TokenizerTestBase`; transformers are standalone (matching existing pattern)
- **Instantiation**: Direct — `private readonly IsGuidValidator validator = new();`
- **Naming**: `Given[Scenario]_When[Action]_Then[Result]()`
- **Structure**: Arrange / Act / Assert comments

### Required test coverage per item

- Valid input (happy path)
- Invalid input (returns `false` for validators, returns `false` from `TryTransform` for type-conversion transformers)
- Null input
- Empty string input
- Edge cases specific to the type (e.g., IPv6 for `IsIpAddressValidator`, negative numbers for `IsInRangeValidator`, boundary values for `TruncateTransformer`)
- Missing/invalid args throw `ArgumentException` (for items that take args)
- At least one integration test using `Tokenizer.Tokenize()` with a template string

---

## Design Decisions

1. **`MatchesRegexValidator` takes a single arg (pattern only)** — inline flags (`(?i)` etc.) handle options like case-insensitivity natively. No need for a second arg.
2. **Type-conversion transformers return `false` on parse failure** — consistent with `ToDateTimeTransformer` and the `TryTransform` naming convention.
3. **`ToIntTransformer` and `ToDecimalTransformer` use `InvariantCulture`** — predictable behavior; culture-specific formats are not supported.
4. **`ToBooleanTransformer` accepts `true`/`yes`/`1` and `false`/`no`/`0`** — covers common conventions without being surprising.
5. **`DefaultValueTransformer` coalesces null and empty only** — whitespace-only strings are not coalesced; chain `Trim` before `DefaultValue` if needed.
6. **`TruncateTransformer` takes max length only** — no suffix/ellipsis arg (YAGNI).
7. **`IsIntegerValidator` uses `long.TryParse`** — accepts the full range of 64-bit integers, not just `int`.
8. **`IsInRangeValidator` uses `decimal` parsing** — handles both integer and floating-point range checks with a single validator.
