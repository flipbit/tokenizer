# DateTime Handling Redesign

## Problem Statement

The tokenizer library's DateTime handling has several shortcomings:

1. **Template verbosity** - Templates require separate lines for each date format variant. The whois generic catch-all template uses 78 lines for 3 date fields because each label variant x format variant needs its own line. ISO 8601 fractional seconds alone cause 4x duplication (`Z`, `.fZ`, `.ffZ`, `.fffZ`).

2. **Data loss** - `ToDateTimeTransformer` produces `DateTime`, which loses timezone offset information. `ToDateTimeUtcTransformer` forces everything to UTC, discarding the original offset.

3. **Hardcoded culture support** - Spanish month abbreviations (`es-US`, `es-ES`) are hardcoded in `ToDateTimeTransformer.GetCultures()`. No mechanism for other cultures.

4. **Missing type support** - No support for `DateTimeOffset`, `DateOnly`, or `TimeOnly` target types.

5. **No auto-detection** - Template authors must always specify exact format strings, even for unambiguous formats like ISO 8601.

6. **Timezone handling** - Only `UTC`/`(UTC)` text markers are handled, and only by the separate `ToDateTimeUtcTransformer`. Other timezone abbreviations (CEST, GMT, etc.) require manual `Replace` workarounds.

## Design Overview

A two-stage pipeline that separates parsing from type projection:

- **Stage 1 (Tokenize time)** - Raw string to `DateTimeOffset` (lossless, offset preserved)
- **Stage 2 (Assign time)** - `DateTimeOffset` to target property type with diagnostic warnings on lossy conversions

Template authors retain control via explicit format strings. When no format is specified, the library auto-detects using an ordered set of regex-based pattern recognizers that are deterministic and unambiguous.

## Stage 1: Parse (Tokenize Time)

### When a `ToDateTime` decorator is attached

1. **Format strings provided** - `DateTimeOffset.TryParseExact` with each format, using the resolved culture. ISO 8601 formats get automatic fractional-second tolerance (`.f` through `.fffffff`) and offset variant tolerance (`Z`/`zzz`).
2. **No format strings** - Run the value through an ordered list of `DatePatternRecognizer` instances (see [Date Pattern Recognizers](#date-pattern-recognizers)). Most specific first, first match wins, parse with that format using the resolved culture.
3. **No match** - Transform fails. Optional token gets `null`, required token causes template mismatch.

### When no decorator is attached

Value stays as raw string in `TokenMatch`. No date parsing is attempted at Tokenize time.

### Timezone Abbreviation Normalization

Before the recognizer/parser runs, a pre-parse normalization step scans for trailing timezone abbreviations and replaces them with numeric offsets.

**Mechanics:**
- Runs after whitespace trimming, before recognizer/parser
- Matches a trailing word boundary + abbreviation (case-sensitive, timezone abbreviations are uppercase by convention)
- Strips parentheses if present: `(UTC)` is treated the same as `UTC`
- Replaces the abbreviation with the numeric offset: `"2024-01-15 14:30:00 CEST"` becomes `"2024-01-15 14:30:00 +02:00"`
- The recognizer then sees a standard offset format and matches accordingly
- If the input already has a numeric offset, normalization is skipped

**Built-in lookup table (unambiguous abbreviations only):**

| Abbreviation | Offset | Name |
|---|---|---|
| `UTC` | `+00:00` | Coordinated Universal Time |
| `GMT` | `+00:00` | Greenwich Mean Time |
| `WET` | `+00:00` | Western European Time |
| `CET` | `+01:00` | Central European Time |
| `CEST` | `+02:00` | Central European Summer Time |
| `EET` | `+02:00` | Eastern European Time |
| `EEST` | `+03:00` | Eastern European Summer Time |
| `MSK` | `+03:00` | Moscow Standard Time |
| `JST` | `+09:00` | Japan Standard Time |
| `KST` | `+09:00` | Korea Standard Time |
| `NZST` | `+12:00` | New Zealand Standard Time |
| `NZDT` | `+13:00` | New Zealand Daylight Time |

Deliberately excluded from defaults: `CST`, `IST`, `EST`, `PST`, `EDT`, `CDT`, `PDT`, `BST` - all are ambiguous across regions.

**Extensibility:**

```csharp
var options = new TokenizerOptions()
    .WithTimezoneAbbreviation("PST", TimeSpan.FromHours(-8))
    .WithTimezoneAbbreviation("PDT", TimeSpan.FromHours(-7));
```

Custom registrations merge with (and can override) the defaults. Stored as `IReadOnlyDictionary<string, TimeSpan>` on `TokenizerOptions`.

## Stage 2: Project (Assign Time)

When `Assign<T>()` assigns values to object properties, the reflection/assignment layer handles type projection.

### Projection from `DateTimeOffset`

| Source | Target | Behaviour | Diagnostic |
|---|---|---|---|
| `DateTimeOffset` | `DateTimeOffset` | Direct assignment | None |
| `DateTimeOffset` | `DateTime` | `.UtcDateTime` if offset is UTC (preserves `DateTimeKind.Utc`), `.DateTime` otherwise (`DateTimeKind.Unspecified`) | Warning: offset information lost |
| `DateTimeOffset` | `DateOnly` | `DateOnly.FromDateTime(dto.Date)` | Info: time and offset dropped |
| `DateTimeOffset` | `TimeOnly` | `TimeOnly.FromTimeSpan(dto.TimeOfDay)` | Info: date and offset dropped |

### Auto-conversion from raw string

When the value is a raw string (no transformer was attached) and the target property is a temporal type (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, or their nullable variants):

1. Run through the `DatePatternRecognizer` registry with the template's resolved culture
2. Parse succeeds: project to target type using the same projection logic above
3. Parse fails: throw (Assign throws on failure)

The projection logic is shared in a static `DateTimeProjection` class used by both the transformer output path and the Assign auto-conversion path.

### What Assign does NOT do

- No culture guessing - uses the culture resolved at compile time (stored on the template)
- No format string logic - that is the transformer's job; auto-conversion only uses the no-format recognizer path

## Date Pattern Recognizers

An ordered list of recognizers, each pairing a regex with format string(s). The registry is used by both the no-format transformer path and the auto-conversion path.

**Design:**
- Each recognizer: `Regex Pattern` + `string[] Formats` + `bool RequiresCulture`
- Ordered most-specific first - longer/more-constrained patterns take priority
- The recognizer only identifies the format; actual parsing is done by `DateTimeOffset.TryParseExact`
- If the regex matches but `TryParseExact` fails, continue to the next recognizer (guards against false-positive regex matches)
- Regexes compiled (or source-generated on .NET 8+)
- Culture-dependent recognizers (month/day names) only fire when the regex matches character patterns, then parsing is attempted with the resolved culture

**Built-in recognizers (derived from real-world whois template data):**

| Priority | Pattern Shape | Example Values | Format |
|---|---|---|---|
| 1 | ISO 8601 with offset | `2024-01-15T14:30:00+05:00` | `yyyy-MM-ddTHH:mm:sszzz` (+ fractional second tolerance) |
| 2 | ISO 8601 with Z | `2024-01-15T14:30:00Z` | `yyyy-MM-ddTHH:mm:ssZ` (+ fractional second tolerance) |
| 3 | ISO 8601 no offset | `2024-01-15T14:30:00` | `yyyy-MM-ddTHH:mm:ss` (+ fractional second tolerance) |
| 4 | RFC 2822 / asctime | `Tue Mar 5 14:30:00 GMT 2024` | `ddd MMM d HH:mm:ss \G\M\T yyyy` |
| 5 | Day-month-year with time + offset | `15/01/2024 14:30:00+05:00` | `dd/MM/yyyy HH:mm:sszzz` |
| 6 | Year-month-day with time + offset | `2024-01-15 14:30:00 +05:00` | `yyyy-MM-dd HH:mm:ss zzz` |
| 7 | Year.month.day with time | `2024.01.15 14:30:00` | `yyyy.MM.dd HH:mm:ss` |
| 8 | Year-month-day with time | `2024-01-15 14:30:00` | `yyyy-MM-dd HH:mm:ss` |
| 9 | Day-monthname-year with time | `15-Mar-2024 14:30:00` | `dd-MMM-yyyy HH:mm:ss` |
| 10 | Dayname day monthname year | `Tuesday 15 March 2024` | `dddd d MMMM yyyy` |
| 11 | Day monthname year with time+offset | `15 Mar 2024 14:30+05:00` | `dd MMM yyyy HH:mmzzz` |
| 12 | Monthname day, year | `March 15, 2024` | `MMMM d, yyyy` |
| 13 | Day-monthname-year | `15-Mar-2024` | `dd-MMM-yyyy` |
| 14 | Day monthname year | `15 Mar 2024` | `dd MMM yyyy` |
| 15 | Day-fullmonth-year | `15-March-2024` | `dd-MMMM-yyyy` |
| 16 | Day.month.year with time | `15.01.2024 14:30:00` | `dd.MM.yyyy HH:mm:ss` |
| 17 | Year-month-day with fractional seconds | `2024-01-15 14:30:00.50` | `yyyy-MM-dd HH:mm:ss.f` / `.ff` |
| 18 | Korean style | `2024. 01. 15.` | `yyyy. MM. dd.` |
| 19 | Turkish style | `2024-Mar-15.` | `yyyy-MMM-dd.` |
| 20 | Year/month/day with time | `2024/01/15 14:30:00` | `yyyy/MM/dd HH:mm:ss` |
| 21 | Day/month/year with time | `15/01/2024 14:30:00` | `dd/MM/yyyy HH:mm:ss` |
| 22 | Year-month-day | `2024-01-15` | `yyyy-MM-dd` |
| 23 | Year.month.day | `2024.01.15` | `yyyy.MM.dd` |
| 24 | Year/month/day | `2024/01/15` | `yyyy/MM/dd` |
| 25 | Day.month.year | `15.01.2024` | `dd.MM.yyyy` |
| 26 | Day/month/year | `15/01/2024` | `dd/MM/yyyy` |
| 27 | Day-month-year | `15-01-2024` | `dd-MM-yyyy` |
| 28 | Month/day/year | `01/15/2024` | `MM/dd/yyyy` |
| 29 | Compact with time | `20240115143000` | `yyyyMMddHHmmss` |
| 30 | Compact date-time | `20240115 14:30:00` | `yyyyMMdd HH:mm:ss` |
| 31 | Compact date | `20240115` | `yyyyMMdd` |
| 32 | Relaxed day.month.year | `5.1.2024` | `d.M.yyyy` |

**Ambiguity handling for numeric day/month formats (rows 26-28):**

Formats like `dd/MM/yyyy`, `dd-MM-yyyy`, and `MM/dd/yyyy` are inherently ambiguous when both day and month values are 12 or less. Resolution:

- Year-first formats are tried first (unambiguous)
- Month-name formats are tried first (unambiguous)
- For numeric `dd/MM` vs `MM/dd`: default to `dd/MM` (international convention), but respect the culture setting (`en-US` culture flips to `MM/dd`)
- Documented as a known behaviour so template authors can override with explicit format strings when needed

## Culture Cascade

A new `Culture` setting following the existing cascade pattern.

**Resolution order (most specific wins):**
1. Template front matter: `culture: pt-BR`
2. `TokenizerOptions.Culture` (instance-level)
3. Default: `CultureInfo.InvariantCulture`

**Front matter syntax:**
```yaml
---
culture: pt-BR
terminateOnNewLine: true
---
```

**Implementation:**
- Add `CultureInfo? Culture` property to `TokenizerOptions`
- Add `culture` case to `FrontMatterBinder.ApplyOption`
- Parse via `CultureInfo.GetCultureInfo(value)` - throw `ParsingException` on invalid culture name
- Resolved culture flows into Stage 1 parsing and validators
- `CultureInfo.InvariantCulture` handles English day/month names by default

**What gets removed:**
- Hardcoded `es-US`/`es-ES` Spanish detection in `ToDateTimeTransformer.GetCultures()`
- The `MonthAbbreviations` dictionary and `InitializeCulture` method
- The month-abbreviation sniffing logic

**Migration:** Templates relying on implicit Spanish detection need `culture: es-ES` added to front matter. This is a breaking change.

## Default Offset and Timezone

When the input value has no timezone information but the author knows what timezone the data is in.

### `defaultOffset`

A static UTC offset applied to values that have no offset information.

```yaml
---
defaultOffset: +02:00
---
```

`2026-01-01 10:00` becomes `2026-01-01T10:00:00+02:00`, which converts to `2026-01-01T08:00:00Z` when projected to UTC.

### `defaultTimezone`

A DST-aware timezone applied to values that have no offset information. Uses `TimeZoneInfo` to determine the correct offset for the parsed date.

```yaml
---
defaultTimezone: Europe/Berlin
---
```

`2026-07-01 10:00` gets `+02:00` (CEST), `2026-01-01 10:00` gets `+01:00` (CET).

### Cascade and precedence

Both settings cascade like other settings: `TokenizerOptions` > template front matter > most specific wins.

If both `defaultOffset` and `defaultTimezone` are resolved for the same template (whether set at the same cascade level or different levels), `defaultOffset` takes precedence because it is a more specific instruction. The cascade determines which value of each setting is active; the precedence rule determines which active setting is used when both are present.

If the input already has an offset (from the value itself or timezone abbreviation normalization), both settings are ignored - explicit offset in the data always wins.

## Transformers

### `ToDateTime` - Redesigned

Single transformer, always produces `DateTimeOffset`.

**With format strings:**
```
{ Registered : ToDateTime("yyyy-MM-dd") }
{ Registered : ToDateTime("dd-MMM-yyyy HH:mm:ss") }
```
- Uses `DateTimeOffset.TryParseExact` with resolved culture
- ISO 8601 formats get automatic fractional-second tolerance (`.f` through `.fffffff`) and offset variant tolerance (`Z`/`zzz`)
- Non-ISO formats are parsed exactly as specified

**Without format strings:**
```
{ Registered : ToDateTime }
```
- Runs through the `DatePatternRecognizer` registry
- First matching recognizer provides the format
- No match causes transform failure

### `ToDateTimeUtc` - Deprecated

Retained as a thin wrapper that delegates to the new `ToDateTime` with `DateTimeStyles.AssumeUniversal`. Marked `[Obsolete]`. Emits a compile-time diagnostic suggesting migration. The existing `DecoratorBinder` name-matching convention (`ToDateTimeUtc` resolves to `ToDateTimeUtcTransformer`) means no alias mechanism is needed.

### `ToDate` - New

```
{ Birthday : ToDate("yyyy-MM-dd") }
{ Birthday : ToDate }
```

Parses raw string and produces `DateOnly` directly. Supports explicit format strings and the no-format recognizer path. Silently drops any time component present in the value. Note: this is intentionally more lenient than the `IsDate` validator, which rejects values with time components. The transformer says "extract the date from this value"; the validator says "this value must be date-only."

### `ToTime` - New

```
{ StartTime : ToTime("HH:mm:ss") }
{ StartTime : ToTime }
```

Parses raw string and produces `TimeOnly` directly. Supports explicit format strings and the no-format recognizer path. Silently drops any date component present in the value.

## Validators

Three validators aligned with the temporal types. All share the same parsing core as the transformers and respect the culture cascade.

### `IsDateTime`

Validates that the string contains a parseable date (time optional, defaults to midnight).

```
{ Registered? : IsDateTime }
{ Registered? : IsDateTime("yyyy-MM-dd") }
```

- With format args: `DateTimeOffset.TryParseExact` with format and resolved culture
- Without format args: runs through the `DatePatternRecognizer` registry
- Replaces the existing `IsDateTimeValidator`

### `IsDate`

Validates that the string is a date-only value. Fails if a time component is present.

```
{ Birthday? : IsDate }
{ Birthday? : IsDate("yyyy-MM-dd") }
```

### `IsTime`

Validates that the string is a time-only value. Fails if a date component is present.

```
{ StartTime? : IsTime }
{ StartTime? : IsTime("HH:mm:ss") }
```

## Diagnostic Integration

### Compile-time Diagnostics

- **Deprecated transformer:** `ToDateTimeUtc` usage emits a warning suggesting migration to `ToDateTime`
- **Invalid culture:** `culture: xyz` in front matter throws `ParsingException` at compile time

### Tokenize-time Diagnostics

When `EnableDiagnostics` is true:

- **Recognizer match:** Records which recognizer matched, the regex pattern, and the resolved format string
- **Recognizer miss:** Records the raw value and that no recognizer matched
- **Timezone normalization:** Records the abbreviation substitution (e.g. `CEST` to `+02:00`)
- **Transform failure:** Records the raw value, attempted format(s), and culture. `DateFormatHintGenerator` is updated to work with the new recognizer registry

### Assign-time Diagnostics

- **Lossy projection:** Records what was lost during type projection:
  - `DateTimeOffset` to `DateTime`: "Offset +02:00 dropped"
  - `DateTimeOffset` to `DateOnly`: "Time component 14:30:00 and offset +02:00 dropped"
  - `DateTimeOffset` to `TimeOnly`: "Date component 2024-01-15 and offset +02:00 dropped"
- **Auto-conversion:** Records that no explicit transformer was used and which recognizer matched

### Severity Levels

- **Info:** Lossy projection, auto-conversion success
- **Warning:** Deprecated transformer usage, auto-conversion (no explicit decorator)
- **Error:** Not used at diagnostic level; actual errors throw or cause template mismatch

## Deprecation and Migration

### `ToDateTimeUtc`

- Retained as a functioning `[Obsolete]` wrapper delegating to `ToDateTime`
- Emits compile-time diagnostic suggesting migration
- Removal planned for a future major version

### `ToDateTime` output type change

- **Current:** Returns `DateTime`
- **New:** Returns `DateTimeOffset`
- Breaking change for code inspecting `TokenMatch` values directly
- Transparent for `Assign<T>()` usage via Stage 2 projection

### Spanish culture hack removal

- Templates relying on implicit Spanish detection need `culture: es-ES` in front matter
- Breaking change, documented in migration notes

### Existing tests

All existing transformer and validator tests are migrated, not deleted. Assertions change from `DateTime` to `DateTimeOffset` where appropriate.

## Test Strategy

### Test data

Real-world date values extracted from the whois project templates, encoded as standalone test cases in `Tokenizer.Tests`. No dependency on the whois project.

### Test categories

| Category | Example Values | Unique formats |
|---|---|---|
| ISO 8601 | `2024-01-15T14:30:00Z`, `2024-01-15T14:30:00.123Z`, `2024-01-15T14:30:00+05:00` | 6 |
| Year-first numeric | `2024-01-15`, `2024.01.15`, `2024/01/15`, `20240115` | 4 |
| Day-month-year numeric | `15.01.2024`, `15/01/2024`, `15-01-2024` | 3 |
| Month name short | `15-Mar-2024`, `15 Mar 2024`, `15 Mar 2024 14:30+05:00` | 5 |
| Month name full | `15-March-2024`, `March 15, 2024`, `Tuesday 15 March 2024` | 4 |
| With time | `2024-01-15 14:30:00`, `2024.01.15 14:30:00`, `15.01.2024 14:30:00` | 6 |
| With timezone abbreviation | `2024-01-15 14:30:00 CEST`, `2024-01-15 14:30:00 UTC` | 3 |
| RFC 2822 / asctime | `Tue Mar 5 14:30:00 GMT 2024` | 2 |
| Regional | `2024. 01. 15.` (Korean), `2024-Mar-15.` (Turkish) | 3 |
| Compact | `20240115`, `20240115143000`, `20240115 14:30:00` | 3 |
| Fractional seconds | `2024-01-15 14:30:00.5`, `2024-01-15 14:30:00.50` | 3 |

### Test structure

- **Recognizer tests:** Each `DatePatternRecognizer` gets its own test class verifying regex match + parse for its format family
- **Transformer tests:** `ToDateTimeTransformer` with explicit formats, without formats (recognizer path), with culture settings
- **Validator tests:** `IsDateTime`, `IsDate`, `IsTime` with valid inputs, invalid inputs, format args
- **Projection tests:** `DateTimeOffset` to each target type, verifying values and diagnostic output
- **Auto-conversion tests:** Raw string assigned to temporal property types during Assign
- **Culture tests:** Month/day names in various cultures (`pt-BR`, `es-ES`, `fr-FR`, `de-DE`), verifying the cascade
- **Timezone normalization tests:** Each built-in abbreviation, custom registrations, parenthesized markers, unknown abbreviations pass through unchanged
- **Default offset/timezone tests:** Values without offset info use `defaultOffset`/`defaultTimezone`, values with explicit offset ignore the defaults
- **Deprecation tests:** `ToDateTimeUtc` still works, emits diagnostic warning
- **Integration tests:** End-to-end template compile to tokenize to assign with real whois-shaped input data
- **Backwards compatibility tests:** Existing test cases migrated with `DateTimeOffset` assertions
