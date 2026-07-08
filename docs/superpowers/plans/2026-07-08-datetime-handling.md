# DateTime Handling Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the existing DateTime transformers with a two-stage pipeline that produces `DateTimeOffset` (lossless), supports culture-aware parsing, auto-detects unambiguous date formats via regex recognizers, and projects to target types at Assign time.

**Architecture:** A shared `TemporalParser` class orchestrates timezone normalization → regex-based format recognition → `DateTimeOffset.TryParseExact`. Transformers and validators call into this shared core. A new `IOptionsAwareTransformer`/`IOptionsAwareValidator` interface pair allows the pipeline to pass `TokenizerOptions` (which carries culture, timezone, and offset settings) to decorators that need it, without changing the existing `ITokenTransformer`/`ITokenValidator` interfaces.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 8.0 + .NET 10.0, xUnit, `System.Globalization`, `System.Text.RegularExpressions`

**Spec:** `docs/superpowers/specs/2026-07-08-datetime-handling-design.md`

## Global Constraints

- Target frameworks: `netstandard2.0;net8.0;net10.0`
- `DateOnly`/`TimeOnly` require `#if NET6_0_OR_GREATER` conditional compilation (available on net8.0 and net10.0, not netstandard2.0)
- Root namespace: `Tokens`
- Braces: Allman style
- Private fields: `_camelCase`
- No `#region` blocks
- `TreatWarningsAsErrors` is enabled — code must compile clean
- All tests use xUnit with `GivenX_WhenY_ThenZ` naming and Arrange/Act/Assert structure
- Run `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release` to verify compilation
- Run `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj` to verify tests
- TDD: write failing test first, then implement, then verify pass

---

### Task 1: TokenizerOptions — Culture, DefaultOffset, DefaultTimezone, Timezone Abbreviations

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Test: `tests/Tokenizer.Tests/TokenizerOptionsTests.cs`

**Interfaces:**
- Consumes: nothing
- Produces: `TokenizerOptions.Culture` (`CultureInfo?`), `TokenizerOptions.DefaultOffset` (`TimeSpan?`), `TokenizerOptions.DefaultTimezone` (`string?`), `TokenizerOptions.TimezoneAbbreviations` (`IReadOnlyDictionary<string, TimeSpan>`), `TokenizerOptions.WithTimezoneAbbreviation(string abbreviation, TimeSpan offset)` returning `TokenizerOptions`

- [ ] **Step 1: Write failing tests for new properties**

```csharp
// In tests/Tokenizer.Tests/TokenizerOptionsTests.cs — add to existing test class

[Fact]
public void GivenNewOptions_WhenAccessingCulture_ThenDefaultsToNull()
{
    // Arrange / Act
    var options = new TokenizerOptions();

    // Assert
    Assert.Null(options.Culture);
}

[Fact]
public void GivenOptions_WhenSettingCulture_ThenCultureIsPreserved()
{
    // Arrange / Act
    var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("pt-BR") };

    // Assert
    Assert.Equal("pt-BR", options.Culture!.Name);
}

[Fact]
public void GivenNewOptions_WhenAccessingDefaultOffset_ThenDefaultsToNull()
{
    // Arrange / Act
    var options = new TokenizerOptions();

    // Assert
    Assert.Null(options.DefaultOffset);
}

[Fact]
public void GivenOptions_WhenSettingDefaultOffset_ThenOffsetIsPreserved()
{
    // Arrange / Act
    var options = new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) };

    // Assert
    Assert.Equal(TimeSpan.FromHours(2), options.DefaultOffset);
}

[Fact]
public void GivenNewOptions_WhenAccessingDefaultTimezone_ThenDefaultsToNull()
{
    // Arrange / Act
    var options = new TokenizerOptions();

    // Assert
    Assert.Null(options.DefaultTimezone);
}

[Fact]
public void GivenOptions_WhenSettingDefaultTimezone_ThenTimezoneIsPreserved()
{
    // Arrange / Act
    var options = new TokenizerOptions { DefaultTimezone = "Europe/Berlin" };

    // Assert
    Assert.Equal("Europe/Berlin", options.DefaultTimezone);
}

[Fact]
public void GivenNewOptions_WhenAccessingTimezoneAbbreviations_ThenReturnsEmptyDictionary()
{
    // Arrange / Act
    var options = new TokenizerOptions();

    // Assert
    Assert.Empty(options.TimezoneAbbreviations);
}

[Fact]
public void GivenOptions_WhenAddingTimezoneAbbreviation_ThenAbbreviationIsStored()
{
    // Arrange / Act
    var options = new TokenizerOptions()
        .WithTimezoneAbbreviation("PST", TimeSpan.FromHours(-8));

    // Assert
    Assert.Single(options.TimezoneAbbreviations);
    Assert.Equal(TimeSpan.FromHours(-8), options.TimezoneAbbreviations["PST"]);
}

[Fact]
public void GivenOptions_WhenCopiedWithWith_ThenNewPropertiesAreDeepCopied()
{
    // Arrange
    var original = new TokenizerOptions
    {
        Culture = CultureInfo.GetCultureInfo("fr-FR"),
        DefaultOffset = TimeSpan.FromHours(1),
        DefaultTimezone = "Europe/Paris",
    };
    original = original.WithTimezoneAbbreviation("CET", TimeSpan.FromHours(1));

    // Act
    var copy = original with { DefaultOffset = TimeSpan.FromHours(2) };

    // Assert
    Assert.Equal(TimeSpan.FromHours(2), copy.DefaultOffset);
    Assert.Equal("fr-FR", copy.Culture!.Name);
    Assert.Single(copy.TimezoneAbbreviations);
    // Verify independence
    copy = copy.WithTimezoneAbbreviation("CEST", TimeSpan.FromHours(2));
    Assert.Single(original.TimezoneAbbreviations);
    Assert.Equal(2, copy.TimezoneAbbreviations.Count);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsTests"`
Expected: Compilation errors — properties/methods don't exist yet

- [ ] **Step 3: Implement new properties on TokenizerOptions**

Add to `src/Tokenizer/TokenizerOptions.cs`:

```csharp
// New field alongside existing _transformers/_validators:
private readonly Dictionary<string, TimeSpan> _timezoneAbbreviations = new(StringComparer.Ordinal);

// In copy constructor, add after existing copies:
Culture = original.Culture;
DefaultOffset = original.DefaultOffset;
DefaultTimezone = original.DefaultTimezone;
_timezoneAbbreviations = new Dictionary<string, TimeSpan>(original._timezoneAbbreviations, StringComparer.Ordinal);

// New properties (add after AllowStreamBuffering):

/// <summary>
/// The culture to use for parsing date/time values (month names, day names).
/// When null, <see cref="System.Globalization.CultureInfo.InvariantCulture"/> is used.
/// </summary>
public CultureInfo? Culture { get; init; }

/// <summary>
/// A static UTC offset applied to date/time values that have no offset information.
/// Takes precedence over <see cref="DefaultTimezone"/> when both are set.
/// Ignored when the input value already contains an offset.
/// </summary>
public TimeSpan? DefaultOffset { get; init; }

/// <summary>
/// An IANA or Windows timezone ID (e.g. "Europe/Berlin") applied to date/time values
/// that have no offset information. Uses <see cref="TimeZoneInfo"/> for DST-aware resolution.
/// Ignored when <see cref="DefaultOffset"/> is set or when the input already contains an offset.
/// </summary>
public string? DefaultTimezone { get; init; }

/// <summary>
/// Custom timezone abbreviation-to-offset mappings registered on this options instance.
/// These are merged with (and can override) the built-in defaults during timezone normalization.
/// </summary>
public IReadOnlyDictionary<string, TimeSpan> TimezoneAbbreviations =>
    new System.Collections.ObjectModel.ReadOnlyDictionary<string, TimeSpan>(_timezoneAbbreviations);

/// <summary>
/// Returns a new <see cref="TokenizerOptions"/> instance with the given timezone abbreviation added.
/// </summary>
public TokenizerOptions WithTimezoneAbbreviation(string abbreviation, TimeSpan offset)
{
    var copy = this with { };
    copy._timezoneAbbreviations[abbreviation] = offset;
    return copy;
}
```

Update `Equals` to include new properties. Update `GetHashCode` to include new properties. Follow the existing pattern exactly.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsTests"`
Expected: All PASS

- [ ] **Step 5: Run full test suite to verify nothing is broken**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/TokenizerOptions.cs tests/Tokenizer.Tests/TokenizerOptionsTests.cs
git commit -m "feat: add Culture, DefaultOffset, DefaultTimezone, TimezoneAbbreviations to TokenizerOptions"
```

---

### Task 2: FrontMatterBinder — Bind New Options

**Files:**
- Modify: `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs`
- Test: `tests/Tokenizer.Tests/Compilation/Binders/FrontMatterBinderTests.cs`

**Interfaces:**
- Consumes: `TokenizerOptions.Culture`, `TokenizerOptions.DefaultOffset`, `TokenizerOptions.DefaultTimezone` (from Task 1)
- Produces: Front matter keys `culture`, `defaultoffset`, `defaulttimezone` bound to template options

- [ ] **Step 1: Write failing tests for new front matter options**

```csharp
// Add to existing FrontMatterBinderTests.cs

[Fact]
public void GivenTemplateWithCultureFrontMatter_WhenCompiled_ThenOptionsCultureIsSet()
{
    // Arrange
    var pattern = """
                  ---
                  culture: pt-BR
                  ---
                  Name: { Name }
                  """;

    // Act
    var tokenizer = new Tokenizer();
    var result = tokenizer.Compile(pattern);

    // Assert
    Assert.Equal("pt-BR", result.Template.Options.Culture!.Name);
}

[Fact]
public void GivenTemplateWithInvalidCulture_WhenCompiled_ThenThrowsParsingException()
{
    // Arrange
    var pattern = """
                  ---
                  culture: not-a-real-culture
                  ---
                  Name: { Name }
                  """;

    // Act / Assert
    var tokenizer = new Tokenizer();
    Assert.Throws<Tokens.Exceptions.ParsingException>(() => tokenizer.Compile(pattern));
}

[Fact]
public void GivenTemplateWithDefaultOffsetFrontMatter_WhenCompiled_ThenOptionsDefaultOffsetIsSet()
{
    // Arrange
    var pattern = """
                  ---
                  defaultOffset: +02:00
                  ---
                  Name: { Name }
                  """;

    // Act
    var tokenizer = new Tokenizer();
    var result = tokenizer.Compile(pattern);

    // Assert
    Assert.Equal(TimeSpan.FromHours(2), result.Template.Options.DefaultOffset);
}

[Fact]
public void GivenTemplateWithNegativeDefaultOffset_WhenCompiled_ThenOptionsDefaultOffsetIsSet()
{
    // Arrange
    var pattern = """
                  ---
                  defaultOffset: -05:00
                  ---
                  Name: { Name }
                  """;

    // Act
    var tokenizer = new Tokenizer();
    var result = tokenizer.Compile(pattern);

    // Assert
    Assert.Equal(TimeSpan.FromHours(-5), result.Template.Options.DefaultOffset);
}

[Fact]
public void GivenTemplateWithDefaultTimezoneFrontMatter_WhenCompiled_ThenOptionsDefaultTimezoneIsSet()
{
    // Arrange
    var pattern = """
                  ---
                  defaultTimezone: Europe/Berlin
                  ---
                  Name: { Name }
                  """;

    // Act
    var tokenizer = new Tokenizer();
    var result = tokenizer.Compile(pattern);

    // Assert
    Assert.Equal("Europe/Berlin", result.Template.Options.DefaultTimezone);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FrontMatterBinderTests"`
Expected: FAIL — unknown front matter options throw `ParsingException`

- [ ] **Step 3: Add new cases to FrontMatterBinder.ApplyOption**

In `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs`, add cases to the `switch (key)` block before the `default` case:

```csharp
case "culture":
    try
    {
        template.Options = template.Options with
        {
            Culture = CultureInfo.GetCultureInfo(value.Trim()),
        };
    }
    catch (CultureNotFoundException ex)
    {
        throw new ParsingException(
            $"Invalid culture name: {value.Trim()}", entry.Location);
    }
    break;
case "defaultoffset":
    if (!TimeSpan.TryParseExact(value.Trim(), ["hh\\:mm", "\\+hh\\:mm", "\\-hh\\:mm"], CultureInfo.InvariantCulture, out var offset))
    {
        // Fall back to general TimeSpan.TryParse for formats like "+02:00"
        var offsetStr = value.Trim();
        if (!TimeSpan.TryParse(offsetStr, CultureInfo.InvariantCulture, out offset))
        {
            throw new ParsingException(
                $"Invalid offset format: {value.Trim()}. Expected format: +HH:mm or -HH:mm", entry.Location);
        }
    }
    template.Options = template.Options with { DefaultOffset = offset };
    break;
case "defaulttimezone":
    template.Options = template.Options with
    {
        DefaultTimezone = value.Trim(),
    };
    break;
```

Add `using System.Globalization;` to the file's usings.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FrontMatterBinderTests"`
Expected: All PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs tests/Tokenizer.Tests/Compilation/Binders/FrontMatterBinderTests.cs
git commit -m "feat: bind culture, defaultOffset, defaultTimezone in front matter"
```

---

### Task 3: IOptionsAwareTransformer/Validator + Pipeline Threading

**Files:**
- Create: `src/Tokenizer/Transformers/IOptionsAwareTransformer.cs`
- Create: `src/Tokenizer/Validators/IOptionsAwareValidator.cs`
- Modify: `src/Tokenizer/TokenDecoratorContext.cs`
- Modify: `src/Tokenizer/Tokenization/DecoratorPipeline.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/DecoratorPipelineTests.cs`

**Interfaces:**
- Consumes: `TokenizerOptions` (from Task 1)
- Produces: `IOptionsAwareTransformer` interface, `IOptionsAwareValidator` interface, `TokenDecoratorContext.TryTransform(object, TokenizerOptions, out object)`, `TokenDecoratorContext.Validate(object, TokenizerOptions)`

These are new interfaces that extend `ITokenTransformer`/`ITokenValidator`. The `DecoratorPipeline` checks at runtime whether a decorator implements the options-aware interface and passes `TokenizerOptions` if so. Existing transformers continue to work unchanged via the base interface.

- [ ] **Step 1: Create IOptionsAwareTransformer interface**

Create `src/Tokenizer/Transformers/IOptionsAwareTransformer.cs`:

```csharp
namespace Tokens.Transformers;

/// <summary>
/// A transformer that receives <see cref="TokenizerOptions"/> for context-dependent
/// operations such as culture-aware date/time parsing.
/// </summary>
public interface IOptionsAwareTransformer : ITokenTransformer
{
    /// <summary>
    /// Attempts to transform the given input using the specified options for context.
    /// </summary>
    bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed);
}
```

- [ ] **Step 2: Create IOptionsAwareValidator interface**

Create `src/Tokenizer/Validators/IOptionsAwareValidator.cs`:

```csharp
namespace Tokens.Validators;

/// <summary>
/// A validator that receives <see cref="TokenizerOptions"/> for context-dependent
/// operations such as culture-aware date/time validation.
/// </summary>
public interface IOptionsAwareValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token value is valid using the specified options for context.
    /// </summary>
    bool IsValid(object value, string[] args, TokenizerOptions options);
}
```

- [ ] **Step 3: Write a failing test for options-aware pipeline threading**

```csharp
// In tests/Tokenizer.Tests/Tokenization/DecoratorPipelineTests.cs — add test

[Fact]
public void GivenOptionsAwareTransformer_WhenPipelineEvaluates_ThenOptionsArePassed()
{
    // Arrange — compile a template using a registered options-aware transformer
    // that records whether it received options. We'll use the ToDateTime transformer
    // with a culture option as the real verification, but for this unit test we verify
    // the pipeline calls the options-aware overload.
    var options = new TokenizerOptions
    {
        Culture = CultureInfo.GetCultureInfo("fr-FR"),
    };

    var pattern = "Date: { Date : ToDateTime('dd MMMM yyyy') }";
    var input = "Date: 15 mars 2024";  // "mars" = March in French

    var tokenizer = new Tokenizer();
    var template = tokenizer.Compile(pattern, options).Template;
    var result = tokenizer.Tokenize(template, input);

    // Assert — this will fail until ToDateTime implements IOptionsAwareTransformer
    // and the pipeline threads options through. For now, just verify the pipeline
    // compiles and runs without error.
    // The actual culture-aware parsing test belongs in Task 6.
    Assert.NotNull(result);
}
```

- [ ] **Step 4: Update TokenDecoratorContext to support options-aware overloads**

In `src/Tokenizer/TokenDecoratorContext.cs`, add overloads:

```csharp
/// <summary>
/// Transforms the token value, passing options to options-aware transformers.
/// </summary>
public bool TryTransform(object value, TokenizerOptions options, out object transformed)
{
    var instance = (ITokenTransformer)CreateDecorator();

    if (instance is IOptionsAwareTransformer optionsAware)
    {
        return optionsAware.TryTransform(value, GetParameterArray(), options, out transformed);
    }

    return instance.TryTransform(value, GetParameterArray(), out transformed);
}

/// <summary>
/// Validates the token value, passing options to options-aware validators.
/// </summary>
public bool Validate(object value, TokenizerOptions options)
{
    var instance = (ITokenValidator)CreateDecorator();

    bool result;

    if (instance is IOptionsAwareValidator optionsAware)
    {
        result = optionsAware.IsValid(value, GetParameterArray(), options);
    }
    else
    {
        result = instance.IsValid(value, GetParameterArray());
    }

    return IsNotValidator ? !result : result;
}
```

- [ ] **Step 5: Update DecoratorPipeline to pass options**

In `src/Tokenizer/Tokenization/DecoratorPipeline.cs`, update `RunDecoratorPipeline` to pass `_options` through the new overloads:

Change line 87 from:
```csharp
if (!decorator.TryTransform(evaluatedValue!, out var output))
```
to:
```csharp
if (!decorator.TryTransform(evaluatedValue!, _options, out var output))
```

Change line 112 from:
```csharp
if (decorator.Validate(evaluatedValue!))
```
to:
```csharp
if (decorator.Validate(evaluatedValue!, _options))
```

- [ ] **Step 6: Run full test suite to verify nothing is broken**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All PASS — existing transformers/validators still work via the base interface path in the `TokenDecoratorContext` overloads

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/IOptionsAwareTransformer.cs src/Tokenizer/Validators/IOptionsAwareValidator.cs src/Tokenizer/TokenDecoratorContext.cs src/Tokenizer/Tokenization/DecoratorPipeline.cs tests/Tokenizer.Tests/Tokenization/DecoratorPipelineTests.cs
git commit -m "feat: add IOptionsAwareTransformer/Validator interfaces, thread options through pipeline"
```

---

### Task 4: TimezoneNormalizer

**Files:**
- Create: `src/Tokenizer/Temporal/TimezoneNormalizer.cs`
- Create: `tests/Tokenizer.Tests/Temporal/TimezoneNormalizerTests.cs`

**Interfaces:**
- Consumes: `TokenizerOptions.TimezoneAbbreviations` (from Task 1)
- Produces: `TimezoneNormalizer.Normalize(string value, IReadOnlyDictionary<string, TimeSpan> customAbbreviations)` returning `string`

- [ ] **Step 1: Write failing tests**

```csharp
using System.Globalization;
using Xunit;

namespace Tokens.Temporal;

public class TimezoneNormalizerTests
{
    private static readonly IReadOnlyDictionary<string, TimeSpan> NoCustom =
        new Dictionary<string, TimeSpan>();

    [Theory]
    [InlineData("2024-01-15 14:30:00 UTC", "2024-01-15 14:30:00 +00:00")]
    [InlineData("2024-01-15 14:30:00 GMT", "2024-01-15 14:30:00 +00:00")]
    [InlineData("2024-01-15 14:30:00 CEST", "2024-01-15 14:30:00 +02:00")]
    [InlineData("2024-01-15 14:30:00 CET", "2024-01-15 14:30:00 +01:00")]
    [InlineData("2024-01-15 14:30:00 JST", "2024-01-15 14:30:00 +09:00")]
    [InlineData("2024-01-15 14:30:00 MSK", "2024-01-15 14:30:00 +03:00")]
    public void GivenValueWithBuiltInAbbreviation_WhenNormalizing_ThenReplacesWithOffset(
        string input, string expected)
    {
        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenValueWithParenthesizedUtc_WhenNormalizing_ThenReplacesWithOffset()
    {
        // Act
        var result = TimezoneNormalizer.Normalize("2024-01-15 14:30:00 (UTC)", NoCustom);

        // Assert
        Assert.Equal("2024-01-15 14:30:00 +00:00", result);
    }

    [Fact]
    public void GivenValueWithNumericOffset_WhenNormalizing_ThenReturnsUnchanged()
    {
        // Arrange
        var input = "2024-01-15 14:30:00 +05:00";

        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void GivenValueWithNoTimezone_WhenNormalizing_ThenReturnsUnchanged()
    {
        // Arrange
        var input = "2024-01-15 14:30:00";

        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void GivenValueWithUnknownAbbreviation_WhenNormalizing_ThenReturnsUnchanged()
    {
        // Arrange
        var input = "2024-01-15 14:30:00 XYZ";

        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void GivenCustomAbbreviation_WhenNormalizing_ThenUsesCustomMapping()
    {
        // Arrange
        var custom = new Dictionary<string, TimeSpan>
        {
            ["PST"] = TimeSpan.FromHours(-8),
        };

        // Act
        var result = TimezoneNormalizer.Normalize("2024-01-15 14:30:00 PST", custom);

        // Assert
        Assert.Equal("2024-01-15 14:30:00 -08:00", result);
    }

    [Fact]
    public void GivenCustomAbbreviationOverridingBuiltIn_WhenNormalizing_ThenCustomWins()
    {
        // Arrange
        var custom = new Dictionary<string, TimeSpan>
        {
            ["UTC"] = TimeSpan.FromHours(5), // absurd, but proves custom overrides built-in
        };

        // Act
        var result = TimezoneNormalizer.Normalize("2024-01-15 14:30:00 UTC", custom);

        // Assert
        Assert.Equal("2024-01-15 14:30:00 +05:00", result);
    }

    [Fact]
    public void GivenAbbreviationIsCaseSensitive_WhenNormalizingLowercase_ThenReturnsUnchanged()
    {
        // Arrange — timezone abbreviations are uppercase by convention
        var input = "2024-01-15 14:30:00 utc";

        // Act
        var result = TimezoneNormalizer.Normalize(input, NoCustom);

        // Assert
        Assert.Equal(input, result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TimezoneNormalizerTests"`
Expected: Compilation error — `TimezoneNormalizer` doesn't exist

- [ ] **Step 3: Implement TimezoneNormalizer**

Create `src/Tokenizer/Temporal/TimezoneNormalizer.cs`:

```csharp
using System.Text.RegularExpressions;

namespace Tokens.Temporal;

/// <summary>
/// Normalizes timezone abbreviations in date/time strings by replacing them with
/// numeric UTC offsets. Runs as a pre-parse step before format recognition.
/// </summary>
internal static class TimezoneNormalizer
{
    private static readonly Dictionary<string, TimeSpan> BuiltInAbbreviations = new(StringComparer.Ordinal)
    {
        ["UTC"] = TimeSpan.Zero,
        ["GMT"] = TimeSpan.Zero,
        ["WET"] = TimeSpan.Zero,
        ["CET"] = TimeSpan.FromHours(1),
        ["CEST"] = TimeSpan.FromHours(2),
        ["EET"] = TimeSpan.FromHours(2),
        ["EEST"] = TimeSpan.FromHours(3),
        ["MSK"] = TimeSpan.FromHours(3),
        ["JST"] = TimeSpan.FromHours(9),
        ["KST"] = TimeSpan.FromHours(9),
        ["NZST"] = TimeSpan.FromHours(12),
        ["NZDT"] = TimeSpan.FromHours(13),
    };

#if NET8_0_OR_GREATER
    [System.Text.RegularExpressions.GeneratedRegex(@"\s*\(?([A-Z]{2,5})\)?\s*$")]
    private static partial Regex TrailingAbbreviationRegex();
#else
    private static readonly Regex TrailingAbbreviationRegexInstance =
        new(@"\s*\(?([A-Z]{2,5})\)?\s*$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static Regex TrailingAbbreviationRegex() => TrailingAbbreviationRegexInstance;
#endif

#if NET8_0_OR_GREATER
    [System.Text.RegularExpressions.GeneratedRegex(@"[+-]\d{2}:\d{2}\s*$")]
    private static partial Regex TrailingNumericOffsetRegex();
#else
    private static readonly Regex TrailingNumericOffsetRegexInstance =
        new(@"[+-]\d{2}:\d{2}\s*$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static Regex TrailingNumericOffsetRegex() => TrailingNumericOffsetRegexInstance;
#endif

    /// <summary>
    /// Replaces a trailing timezone abbreviation with its numeric UTC offset.
    /// Returns the input unchanged if no known abbreviation is found or if a
    /// numeric offset is already present.
    /// </summary>
    public static string Normalize(string value, IReadOnlyDictionary<string, TimeSpan> customAbbreviations)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        // Skip if already has a numeric offset
        if (TrailingNumericOffsetRegex().IsMatch(value)) return value;

        var match = TrailingAbbreviationRegex().Match(value);
        if (!match.Success) return value;

        var abbreviation = match.Groups[1].Value;

        // Custom abbreviations override built-in
        if (!customAbbreviations.TryGetValue(abbreviation, out var offset) &&
            !BuiltInAbbreviations.TryGetValue(abbreviation, out offset))
        {
            return value;
        }

        var prefix = value.Substring(0, match.Index).TrimEnd();
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var abs = offset < TimeSpan.Zero ? offset.Negate() : offset;

        return $"{prefix} {sign}{abs.Hours:D2}:{abs.Minutes:D2}";
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TimezoneNormalizerTests"`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Temporal/TimezoneNormalizer.cs tests/Tokenizer.Tests/Temporal/TimezoneNormalizerTests.cs
git commit -m "feat: add TimezoneNormalizer for timezone abbreviation-to-offset substitution"
```

---

### Task 5: DatePatternRecognizer

**Files:**
- Create: `src/Tokenizer/Temporal/DatePatternRecognizer.cs`
- Create: `tests/Tokenizer.Tests/Temporal/DatePatternRecognizerTests.cs`

**Interfaces:**
- Consumes: nothing
- Produces: `DatePatternRecognizer.TryRecognize(string value, CultureInfo culture, out string[] formats)` returning `bool`, `DatePatternRecognizer.Recognizers` static list

This is the regex-based format identification engine. Each recognizer is a regex + format string pair. The registry is ordered most-specific first.

- [ ] **Step 1: Write failing tests for representative recognizers**

Write tests for the key format families. Each test verifies that a recognizer correctly identifies the format for a given input. See spec section "Date Pattern Recognizers" for all 32 recognizers — test at least one from each major family:

```csharp
using System.Globalization;
using Xunit;

namespace Tokens.Temporal;

public class DatePatternRecognizerTests
{
    [Theory]
    [InlineData("2024-01-15T14:30:00+05:00")]
    [InlineData("2024-01-15T14:30:00.123+05:00")]
    [InlineData("2024-01-15T14:30:00.123456+05:00")]
    public void GivenIso8601WithOffset_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        Assert.True(DateTimeOffset.TryParseExact(input, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _));
    }

    [Theory]
    [InlineData("2024-01-15T14:30:00Z")]
    [InlineData("2024-01-15T14:30:00.1Z")]
    [InlineData("2024-01-15T14:30:00.123Z")]
    [InlineData("2024-01-15T14:30:00.1234567Z")]
    public void GivenIso8601WithZ_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        Assert.True(DateTimeOffset.TryParseExact(input, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out _));
    }

    [Theory]
    [InlineData("2024-01-15")]
    public void GivenYearMonthDay_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        Assert.Contains("yyyy-MM-dd", formats);
    }

    [Theory]
    [InlineData("15-Mar-2024")]
    [InlineData("15-Jan-2024")]
    public void GivenDayMonthNameYear_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        Assert.Contains("dd-MMM-yyyy", formats);
    }

    [Theory]
    [InlineData("20240115")]
    public void GivenCompactDate_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        Assert.Contains("yyyyMMdd", formats);
    }

    [Theory]
    [InlineData("2024. 01. 15.")]
    public void GivenKoreanStyle_WhenRecognizing_ThenMatchesAndParses(string input)
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize(input, CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        Assert.Contains("yyyy. MM. dd.", formats);
    }

    [Fact]
    public void GivenNonDateString_WhenRecognizing_ThenReturnsFalse()
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize("hello world", CultureInfo.InvariantCulture, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenAmbiguousNumericDate_WhenRecognizingWithInvariantCulture_ThenDefaultsToDdMm()
    {
        // Arrange — "15/01/2024" is unambiguous (day > 12), but "01/02/2024" is ambiguous
        // With invariant culture, dd/MM takes priority over MM/dd

        // Act
        var result = DatePatternRecognizer.TryRecognize("01/02/2024", CultureInfo.InvariantCulture, out var formats);

        // Assert
        Assert.True(result);
        Assert.Contains("dd/MM/yyyy", formats);
    }

    [Fact]
    public void GivenAmbiguousNumericDate_WhenRecognizingWithUsCulture_ThenDefaultsToMmDd()
    {
        // Act
        var result = DatePatternRecognizer.TryRecognize("01/02/2024", CultureInfo.GetCultureInfo("en-US"), out var formats);

        // Assert
        Assert.True(result);
        Assert.Contains("MM/dd/yyyy", formats);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DatePatternRecognizerTests"`
Expected: Compilation error

- [ ] **Step 3: Implement DatePatternRecognizer**

Create `src/Tokenizer/Temporal/DatePatternRecognizer.cs`. This is a large file — implement all 32 recognizers from the spec table. Each recognizer is a `record` with a compiled regex and associated format string(s). The `TryRecognize` method iterates the list in order and returns the first match.

Key implementation details:
- ISO 8601 recognizers generate format arrays with fractional second variants (`.f` through `.fffffff`)
- Culture-dependent recognizers (month/day names) use `[A-Za-z]+` regex patterns
- Ambiguous numeric date/month ordering checks `culture.DateTimeFormat.ShortDatePattern` to determine `dd/MM` vs `MM/dd` ordering
- Regexes use `^...$` anchors for exact matching
- Use `#if NET8_0_OR_GREATER` with `[GeneratedRegex]` where feasible, falling back to `new Regex(..., RegexOptions.Compiled)` on netstandard2.0

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DatePatternRecognizerTests"`
Expected: All PASS

- [ ] **Step 5: Add comprehensive test coverage for all 32 recognizers**

Add `[Theory]` tests with `[InlineData]` for every recognizer in the spec table, including edge cases:
- ISO 8601 with 1-7 fractional second digits
- Month name recognizers with multi-culture values
- Compact formats with boundary values
- Regional formats (Korean, Turkish)

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Temporal/DatePatternRecognizer.cs tests/Tokenizer.Tests/Temporal/DatePatternRecognizerTests.cs
git commit -m "feat: add DatePatternRecognizer with 32 regex-based date format recognizers"
```

---

### Task 6: TemporalParser — Shared Parsing Core

**Files:**
- Create: `src/Tokenizer/Temporal/TemporalParser.cs`
- Create: `tests/Tokenizer.Tests/Temporal/TemporalParserTests.cs`

**Interfaces:**
- Consumes: `TimezoneNormalizer.Normalize()` (Task 4), `DatePatternRecognizer.TryRecognize()` (Task 5), `TokenizerOptions` (Task 1)
- Produces: `TemporalParser.TryParse(string value, string[]? formats, TokenizerOptions options, out DateTimeOffset result)` returning `bool`

This is the central parsing method that all transformers and validators call.

- [ ] **Step 1: Write failing tests**

```csharp
using System.Globalization;
using Xunit;

namespace Tokens.Temporal;

public class TemporalParserTests
{
    [Fact]
    public void GivenIso8601Value_WhenParsingWithFormat_ThenReturnsDateTimeOffset()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15T14:30:00Z", ["yyyy-MM-ddTHH:mm:ssZ"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero), dto);
    }

    [Fact]
    public void GivenIso8601WithFractionalSeconds_WhenParsingBaseFormat_ThenToleratesFractionalSeconds()
    {
        // Arrange — format says "Z" but value has ".123Z", ISO 8601 tolerance should handle it
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15T14:30:00.123Z", ["yyyy-MM-ddTHH:mm:ssZ"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(123, dto.Millisecond);
    }

    [Fact]
    public void GivenNoFormat_WhenParsingIso8601_ThenAutoDetectsViaRecognizer()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15T14:30:00Z", null, options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(2024, dto.Year);
        Assert.Equal(1, dto.Month);
        Assert.Equal(15, dto.Day);
    }

    [Fact]
    public void GivenTimezoneAbbreviation_WhenParsing_ThenNormalizesBeforeParsing()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15 14:30:00 CEST", null, options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(2), dto.Offset);
    }

    [Fact]
    public void GivenCulture_WhenParsingFrenchMonthName_ThenParsesCorrectly()
    {
        // Arrange
        var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("fr-FR") };

        // Act
        var result = TemporalParser.TryParse("15-mars-2024", ["dd-MMM-yyyy"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(3, dto.Month);
        Assert.Equal(15, dto.Day);
    }

    [Fact]
    public void GivenDefaultOffset_WhenParsingValueWithoutOffset_ThenAppliesDefaultOffset()
    {
        // Arrange
        var options = new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) };

        // Act
        var result = TemporalParser.TryParse("2024-01-15 14:30:00", ["yyyy-MM-dd HH:mm:ss"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(2), dto.Offset);
    }

    [Fact]
    public void GivenDefaultOffset_WhenParsingValueWithExplicitOffset_ThenIgnoresDefault()
    {
        // Arrange
        var options = new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) };

        // Act
        var result = TemporalParser.TryParse("2024-01-15T14:30:00+05:00", ["yyyy-MM-ddTHH:mm:sszzz"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(5), dto.Offset); // explicit offset wins
    }

    [Fact]
    public void GivenDefaultTimezone_WhenParsingValueWithoutOffset_ThenAppliesDstAwareOffset()
    {
        // Arrange
        var options = new TokenizerOptions { DefaultTimezone = "Europe/Berlin" };

        // Act — January = CET (+01:00)
        var result = TemporalParser.TryParse("2024-01-15 14:30:00", ["yyyy-MM-dd HH:mm:ss"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(1), dto.Offset);
    }

    [Fact]
    public void GivenBothDefaultOffsetAndTimezone_WhenParsing_ThenOffsetTakesPrecedence()
    {
        // Arrange
        var options = new TokenizerOptions
        {
            DefaultOffset = TimeSpan.FromHours(5),
            DefaultTimezone = "Europe/Berlin",
        };

        // Act
        var result = TemporalParser.TryParse("2024-01-15 14:30:00", ["yyyy-MM-dd HH:mm:ss"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(5), dto.Offset); // DefaultOffset wins
    }

    [Fact]
    public void GivenCustomTimezoneAbbreviation_WhenParsing_ThenUsesCustomMapping()
    {
        // Arrange
        var options = new TokenizerOptions()
            .WithTimezoneAbbreviation("PST", TimeSpan.FromHours(-8));

        // Act
        var result = TemporalParser.TryParse("2024-01-15 14:30:00 PST", null, options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(TimeSpan.FromHours(-8), dto.Offset);
    }

    [Fact]
    public void GivenOrdinalSuffix_WhenParsing_ThenStripsAndParses()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("1st August 2001", ["dd MMMM yyyy"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(1, dto.Day);
        Assert.Equal(8, dto.Month);
    }

    [Fact]
    public void GivenUnparseableValue_WhenParsing_ThenReturnsFalse()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("not a date", ["yyyy-MM-dd"], options, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValueWithNewline_WhenParsing_ThenParsesBeforeNewline()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = TemporalParser.TryParse("2024-01-15\nsome text", ["yyyy-MM-dd"], options, out var dto);

        // Assert
        Assert.True(result);
        Assert.Equal(15, dto.Day);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemporalParserTests"`
Expected: Compilation error

- [ ] **Step 3: Implement TemporalParser**

Create `src/Tokenizer/Temporal/TemporalParser.cs`:

```csharp
using System.Globalization;
using System.Text.RegularExpressions;
using Tokens.Extensions;

namespace Tokens.Temporal;

/// <summary>
/// Central date/time parsing engine. Orchestrates timezone normalization,
/// format recognition, and DateTimeOffset parsing.
/// </summary>
internal static class TemporalParser
{
    // Reuse the ordinal suffix regex from the old ToDateTimeTransformer
#if NET8_0_OR_GREATER
    [GeneratedRegex(@"\b(?<digits>\d+)(?:st|nd|rd|th)\b", RegexOptions.ExplicitCapture)]
    private static partial Regex OrdinalSuffixRegex();
#else
    private static readonly Regex OrdinalSuffixRegexInstance =
        new(@"\b(?<digits>\d+)(?:st|nd|rd|th)\b", RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(-1));
    private static Regex OrdinalSuffixRegex() => OrdinalSuffixRegexInstance;
#endif

    /// <summary>
    /// Attempts to parse a string value into a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="value">The raw string value to parse.</param>
    /// <param name="formats">Explicit format strings, or null/empty for auto-detection.</param>
    /// <param name="options">Options providing culture, default offset/timezone, and timezone abbreviations.</param>
    /// <param name="result">The parsed result if successful.</param>
    /// <returns>true if parsing succeeded.</returns>
    public static bool TryParse(string? value, string[]? formats, TokenizerOptions options, out DateTimeOffset result)
    {
        result = default;

        if (value?.ToString() is not { Length: > 0 } rawString)
            return false;

        var valueString = rawString.SubstringBeforeNewLine();
        if (string.IsNullOrWhiteSpace(valueString))
            return false;

        // Normalize timezone abbreviations
        valueString = TimezoneNormalizer.Normalize(valueString, options.TimezoneAbbreviations);

        var culture = options.Culture ?? CultureInfo.InvariantCulture;

        if (formats is { Length: > 0 } && !string.IsNullOrWhiteSpace(formats[0]))
        {
            return TryParseWithFormats(valueString, formats, culture, options, out result);
        }

        return TryParseWithRecognizers(valueString, culture, options, out result);
    }

    private static bool TryParseWithFormats(
        string value, string[] formats, CultureInfo culture,
        TokenizerOptions options, out DateTimeOffset result)
    {
        foreach (var format in formats)
        {
            if (string.IsNullOrWhiteSpace(format)) continue;

            var valueToParse = value;

            // Strip ordinal suffixes when format uses day specifiers
            if (format.Contains(" d ", StringComparison.Ordinal) ||
                format.Contains(" dd ", StringComparison.Ordinal) ||
                format.StartsWith("d ", StringComparison.Ordinal) ||
                format.StartsWith("dd ", StringComparison.Ordinal))
            {
                valueToParse = OrdinalSuffixRegex().Replace(valueToParse, "${digits}");
            }

            // Try exact format
            if (DateTimeOffset.TryParseExact(valueToParse, format, culture,
                    DateTimeStyles.None, out result))
            {
                result = ApplyDefaultOffset(result, valueToParse, options);
                return true;
            }

            // ISO 8601 fractional second tolerance
            if (IsIso8601Format(format))
            {
                var expandedFormats = ExpandIso8601Formats(format);
                if (DateTimeOffset.TryParseExact(valueToParse, expandedFormats, culture,
                        DateTimeStyles.None, out result))
                {
                    result = ApplyDefaultOffset(result, valueToParse, options);
                    return true;
                }
            }
        }

        result = default;
        return false;
    }

    private static bool TryParseWithRecognizers(
        string value, CultureInfo culture,
        TokenizerOptions options, out DateTimeOffset result)
    {
        if (DatePatternRecognizer.TryRecognize(value, culture, out var formats))
        {
            if (DateTimeOffset.TryParseExact(value, formats, culture,
                    DateTimeStyles.None, out result))
            {
                result = ApplyDefaultOffset(result, value, options);
                return true;
            }
        }

        result = default;
        return false;
    }

    private static bool IsIso8601Format(string format)
    {
        return format.Contains("yyyy-MM-dd", StringComparison.Ordinal) &&
               format.Contains("T", StringComparison.Ordinal);
    }

    internal static string[] ExpandIso8601Formats(string baseFormat)
    {
        // Generate variants with fractional seconds
        var result = new List<string> { baseFormat };

        // Find the position of 'ss' to insert fractional parts
        var ssIndex = baseFormat.IndexOf("ss", StringComparison.Ordinal);
        if (ssIndex < 0) return result.ToArray();

        var afterSs = ssIndex + 2;
        var before = baseFormat.Substring(0, afterSs);
        var after = afterSs < baseFormat.Length ? baseFormat.Substring(afterSs) : string.Empty;

        for (var i = 1; i <= 7; i++)
        {
            result.Add($"{before}.{new string('f', i)}{after}");
        }

        return result.ToArray();
    }

    private static DateTimeOffset ApplyDefaultOffset(DateTimeOffset parsed, string originalValue, TokenizerOptions options)
    {
        // If the original value had an explicit offset, don't override
        if (HasExplicitOffset(originalValue))
            return parsed;

        // DefaultOffset takes precedence over DefaultTimezone
        if (options.DefaultOffset.HasValue)
        {
            return new DateTimeOffset(parsed.DateTime, options.DefaultOffset.Value);
        }

        if (!string.IsNullOrEmpty(options.DefaultTimezone))
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(options.DefaultTimezone);
                var offset = tz.GetUtcOffset(parsed.DateTime);
                return new DateTimeOffset(parsed.DateTime, offset);
            }
            catch (TimeZoneNotFoundException)
            {
                // Unknown timezone — fall through to return as-is
            }
        }

        return parsed;
    }

    private static bool HasExplicitOffset(string value)
    {
        // Check for trailing Z, +HH:mm, -HH:mm patterns
        var trimmed = value.TrimEnd();
        if (trimmed.EndsWith("Z", StringComparison.Ordinal)) return true;

        // Check for +/-HH:mm at end
        if (trimmed.Length >= 6)
        {
            var lastSix = trimmed.Substring(trimmed.Length - 6);
            if ((lastSix[0] == '+' || lastSix[0] == '-') &&
                char.IsDigit(lastSix[1]) && char.IsDigit(lastSix[2]) &&
                lastSix[3] == ':' &&
                char.IsDigit(lastSix[4]) && char.IsDigit(lastSix[5]))
            {
                return true;
            }
        }

        return false;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemporalParserTests"`
Expected: All PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Temporal/TemporalParser.cs tests/Tokenizer.Tests/Temporal/TemporalParserTests.cs
git commit -m "feat: add TemporalParser shared parsing core with timezone normalization and auto-detection"
```

---

### Task 7: ToDateTimeTransformer Rewrite + ToDateTimeUtcTransformer Deprecation

**Files:**
- Modify: `src/Tokenizer/Transformers/ToDateTimeTransformer.cs`
- Modify: `src/Tokenizer/Transformers/ToDateTimeUtcTransformer.cs`
- Modify: `tests/Tokenizer.Tests/Transformers/ToDateTimeTransformerTests.cs`
- Modify: `tests/Tokenizer.Tests/Transformers/ToDateTimeUtcTransformerTests.cs`

**Interfaces:**
- Consumes: `IOptionsAwareTransformer` (Task 3), `TemporalParser.TryParse()` (Task 6)
- Produces: `ToDateTimeTransformer` implementing `IOptionsAwareTransformer`, producing `DateTimeOffset`. `ToDateTimeUtcTransformer` marked `[Obsolete]`, delegating to `TemporalParser`.

- [ ] **Step 1: Update existing tests to expect DateTimeOffset output**

In `tests/Tokenizer.Tests/Transformers/ToDateTimeTransformerTests.cs`, update all tests to assert `DateTimeOffset` instead of `DateTime`. The test for `GivenValidDateStringWithFormat_WhenTransforming_ThenReturnsCorrectDateTime` becomes:

```csharp
[Fact]
public void GivenValidDateStringWithFormat_WhenTransforming_ThenReturnsCorrectDateTimeOffset()
{
    // Arrange
    var input = "2014-01-01";
    var format = "yyyy-MM-dd";
    var options = new TokenizerOptions();

    // Act
    var result = _transformer.TryTransform(input, [format], options, out var t);
    var dto = (DateTimeOffset)t;

    // Assert
    Assert.True(result);
    Assert.Equal(2014, dto.Year);
    Assert.Equal(1, dto.Month);
    Assert.Equal(1, dto.Day);
}
```

Update ALL existing tests similarly — change `(DateTime)t` casts to `(DateTimeOffset)t`, update assertions. Do not delete any tests. Add new tests for culture-aware parsing:

```csharp
[Fact]
public void GivenFrenchMonthName_WhenTransformingWithCulture_ThenParsesCorrectly()
{
    // Arrange
    var input = "15-mars-2024";
    var format = "dd-MMM-yyyy";
    var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("fr-FR") };

    // Act
    var result = _transformer.TryTransform(input, [format], options, out var t);
    var dto = (DateTimeOffset)t;

    // Assert
    Assert.True(result);
    Assert.Equal(3, dto.Month);
}

[Fact]
public void GivenSpanishMonthName_WhenTransformingWithCulture_ThenParsesCorrectly()
{
    // Arrange — replaces the old hardcoded Spanish hack
    var input = "16-abr-1997";
    var format = "dd-MMM-yyyy";
    var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("es-ES") };

    // Act
    var result = _transformer.TryTransform(input, [format], options, out var t);
    var dto = (DateTimeOffset)t;

    // Assert
    Assert.True(result);
    Assert.Equal(4, dto.Month);
}

[Fact]
public void GivenNoFormat_WhenTransforming_ThenAutoDetectsViaRecognizer()
{
    // Arrange
    var input = "2024-01-15T14:30:00Z";
    var options = new TokenizerOptions();

    // Act
    var result = _transformer.TryTransform(input, Array.Empty<string>(), options, out var t);
    var dto = (DateTimeOffset)t;

    // Assert
    Assert.True(result);
    Assert.Equal(2024, dto.Year);
    Assert.Equal(TimeSpan.Zero, dto.Offset);
}
```

Also update `ToDateTimeUtcTransformerTests.cs` — casts change from `(DateTime)` to `(DateTimeOffset)`, UTC assertions use `.Offset == TimeSpan.Zero`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToDateTime"`
Expected: FAIL — transformer still returns `DateTime`

- [ ] **Step 3: Rewrite ToDateTimeTransformer**

Replace the contents of `src/Tokenizer/Transformers/ToDateTimeTransformer.cs`:

```csharp
using Tokens.Temporal;
using Tokens.Validators;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="DateTimeOffset"/>.
/// </summary>
public sealed class ToDateTimeTransformer : IOptionsAwareTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        // Fallback for non-options-aware callers — use default options
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        if (TemporalParser.TryParse(value?.ToString(), args, options, out var result))
        {
            transformed = result;
            return true;
        }

        transformed = value!;
        return false;
    }
}
```

- [ ] **Step 4: Update ToDateTimeUtcTransformer as deprecated wrapper**

Replace `src/Tokenizer/Transformers/ToDateTimeUtcTransformer.cs`:

```csharp
using System.Globalization;
using Tokens.Temporal;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="DateTimeOffset"/> in UTC.
/// </summary>
[Obsolete("Use ToDateTime instead. ToDateTimeUtc will be removed in a future major version.")]
public sealed class ToDateTimeUtcTransformer : IOptionsAwareTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        // Strip UTC markers (same as before)
        if (value is string valueString && !string.IsNullOrWhiteSpace(valueString))
        {
            if (valueString.Contains("(UTC)", StringComparison.Ordinal))
            {
                valueString = valueString.Substring(0, valueString.IndexOf("(UTC)", StringComparison.Ordinal)).Trim();
            }
            else if (valueString.Contains("UTC", StringComparison.Ordinal))
            {
                valueString = valueString.Substring(0, valueString.IndexOf("UTC", StringComparison.Ordinal)).Trim();
            }

            value = valueString;
        }

        // Parse with AssumeUniversal — delegate to TemporalParser but force UTC
        var utcOptions = options with { DefaultOffset = TimeSpan.Zero };

        if (TemporalParser.TryParse(value?.ToString(), args, utcOptions, out var result))
        {
            // Ensure UTC
            transformed = result.ToOffset(TimeSpan.Zero);
            return true;
        }

        transformed = value!;
        return false;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToDateTime"`
Expected: All PASS

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All PASS — verify no other tests break from the `DateTime` → `DateTimeOffset` output change

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/ToDateTimeTransformer.cs src/Tokenizer/Transformers/ToDateTimeUtcTransformer.cs tests/Tokenizer.Tests/Transformers/ToDateTimeTransformerTests.cs tests/Tokenizer.Tests/Transformers/ToDateTimeUtcTransformerTests.cs
git commit -m "feat: rewrite ToDateTime to produce DateTimeOffset, deprecate ToDateTimeUtc"
```

---

### Task 8: ToDateTransformer + ToTimeTransformer

**Files:**
- Create: `src/Tokenizer/Transformers/ToDateTransformer.cs`
- Create: `src/Tokenizer/Transformers/ToTimeTransformer.cs`
- Create: `tests/Tokenizer.Tests/Transformers/ToDateTransformerTests.cs`
- Create: `tests/Tokenizer.Tests/Transformers/ToTimeTransformerTests.cs`

**Interfaces:**
- Consumes: `IOptionsAwareTransformer` (Task 3), `TemporalParser.TryParse()` (Task 6)
- Produces: `ToDateTransformer` producing `DateOnly`, `ToTimeTransformer` producing `TimeOnly` (both NET6+ only)

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Tokenizer.Tests/Transformers/ToDateTransformerTests.cs
#if NET6_0_OR_GREATER
using Xunit;

namespace Tokens.Transformers;

public class ToDateTransformerTests
{
    private readonly ToDateTransformer _transformer = new();

    [Fact]
    public void GivenDateString_WhenTransforming_ThenReturnsDateOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("2024-01-15", ["yyyy-MM-dd"], options, out var t);

        // Assert
        Assert.True(result);
        Assert.IsType<DateOnly>(t);
        Assert.Equal(new DateOnly(2024, 1, 15), t);
    }

    [Fact]
    public void GivenDateTimeString_WhenTransforming_ThenDropsTimeAndReturnsDateOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("2024-01-15 14:30:00", ["yyyy-MM-dd HH:mm:ss"], options, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal(new DateOnly(2024, 1, 15), t);
    }

    [Fact]
    public void GivenNoFormat_WhenTransforming_ThenAutoDetectsAndReturnsDateOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("2024-01-15", Array.Empty<string>(), options, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal(new DateOnly(2024, 1, 15), t);
    }

    [Fact]
    public void GivenInvalidString_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("not a date", ["yyyy-MM-dd"], options, out _);

        // Assert
        Assert.False(result);
    }
}
#endif
```

```csharp
// tests/Tokenizer.Tests/Transformers/ToTimeTransformerTests.cs
#if NET6_0_OR_GREATER
using Xunit;

namespace Tokens.Transformers;

public class ToTimeTransformerTests
{
    private readonly ToTimeTransformer _transformer = new();

    [Fact]
    public void GivenTimeString_WhenTransforming_ThenReturnsTimeOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("14:30:00", ["HH:mm:ss"], options, out var t);

        // Assert
        Assert.True(result);
        Assert.IsType<TimeOnly>(t);
        Assert.Equal(new TimeOnly(14, 30, 0), t);
    }

    [Fact]
    public void GivenTimeWithoutSeconds_WhenTransforming_ThenReturnsTimeOnly()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("14:30", ["HH:mm"], options, out var t);

        // Assert
        Assert.True(result);
        Assert.Equal(new TimeOnly(14, 30, 0), t);
    }

    [Fact]
    public void GivenInvalidString_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = _transformer.TryTransform("not a time", ["HH:mm:ss"], options, out _);

        // Assert
        Assert.False(result);
    }
}
#endif
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToDateTransformerTests|ToTimeTransformerTests"`
Expected: Compilation error

- [ ] **Step 3: Implement ToDateTransformer and ToTimeTransformer**

Create `src/Tokenizer/Transformers/ToDateTransformer.cs`:

```csharp
#if NET6_0_OR_GREATER
using Tokens.Temporal;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="DateOnly"/>.
/// Silently drops any time component present in the value.
/// </summary>
public sealed class ToDateTransformer : IOptionsAwareTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        if (TemporalParser.TryParse(value?.ToString(), args, options, out var dto))
        {
            transformed = DateOnly.FromDateTime(dto.Date);
            return true;
        }

        transformed = value!;
        return false;
    }
}
#endif
```

Create `src/Tokenizer/Transformers/ToTimeTransformer.cs`:

```csharp
#if NET6_0_OR_GREATER
using System.Globalization;
using Tokens.Temporal;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="TimeOnly"/>.
/// Silently drops any date component present in the value.
/// </summary>
public sealed class ToTimeTransformer : IOptionsAwareTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        var culture = options.Culture ?? CultureInfo.InvariantCulture;

        // Try TimeOnly-specific parsing first
        if (value?.ToString() is { Length: > 0 } str)
        {
            if (args is { Length: > 0 } && !string.IsNullOrWhiteSpace(args[0]))
            {
                foreach (var format in args)
                {
                    if (TimeOnly.TryParseExact(str, format, culture, DateTimeStyles.None, out var time))
                    {
                        transformed = time;
                        return true;
                    }
                }
            }

            // Fall back to TemporalParser for full datetime strings, extract time
            if (TemporalParser.TryParse(str, args, options, out var dto))
            {
                transformed = TimeOnly.FromTimeSpan(dto.TimeOfDay);
                return true;
            }
        }

        transformed = value!;
        return false;
    }
}
#endif
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToDateTransformerTests|ToTimeTransformerTests"`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Transformers/ToDateTransformer.cs src/Tokenizer/Transformers/ToTimeTransformer.cs tests/Tokenizer.Tests/Transformers/ToDateTransformerTests.cs tests/Tokenizer.Tests/Transformers/ToTimeTransformerTests.cs
git commit -m "feat: add ToDate and ToTime transformers producing DateOnly/TimeOnly (NET6+)"
```

---

### Task 9: Validators — IsDateTime Rewrite, IsDate, IsTime

**Files:**
- Modify: `src/Tokenizer/Validators/IsDateTimeValidator.cs`
- Create: `src/Tokenizer/Validators/IsDateValidator.cs`
- Create: `src/Tokenizer/Validators/IsTimeValidator.cs`
- Modify: `tests/Tokenizer.Tests/Validators/IsDateTimeValidatorTests.cs`
- Create: `tests/Tokenizer.Tests/Validators/IsDateValidatorTests.cs`
- Create: `tests/Tokenizer.Tests/Validators/IsTimeValidatorTests.cs`

**Interfaces:**
- Consumes: `IOptionsAwareValidator` (Task 3), `TemporalParser.TryParse()` (Task 6)
- Produces: `IsDateTimeValidator` implementing `IOptionsAwareValidator`, `IsDateValidator` (NET6+), `IsTimeValidator` (NET6+)

- [ ] **Step 1: Update existing IsDateTime tests and write new validator tests**

Update `IsDateTimeValidatorTests.cs` — add options-aware overload tests:

```csharp
[Fact]
public void GivenFrenchDate_WhenValidatingWithFrenchCulture_ThenReturnsTrue()
{
    // Arrange
    var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("fr-FR") };

    // Act
    var result = ((IOptionsAwareValidator)_validator).IsValid("15 mars 2024", ["dd MMM yyyy"], options);

    // Assert
    Assert.True(result);
}
```

Write new test files for IsDate and IsTime validators:

```csharp
// tests/Tokenizer.Tests/Validators/IsDateValidatorTests.cs
#if NET6_0_OR_GREATER
using Xunit;

namespace Tokens.Validators;

public class IsDateValidatorTests
{
    private readonly IsDateValidator _validator = new();

    [Fact]
    public void GivenDateOnlyString_WhenValidating_ThenReturnsTrue()
    {
        Assert.True(_validator.IsValid("2024-01-15"));
    }

    [Fact]
    public void GivenDateTimeString_WhenValidating_ThenReturnsFalse()
    {
        // IsDate rejects values with time components
        Assert.False(_validator.IsValid("2024-01-15 14:30:00"));
    }

    [Fact]
    public void GivenInvalidString_WhenValidating_ThenReturnsFalse()
    {
        Assert.False(_validator.IsValid("hello"));
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        Assert.False(_validator.IsValid(null!));
    }
}
#endif
```

```csharp
// tests/Tokenizer.Tests/Validators/IsTimeValidatorTests.cs
#if NET6_0_OR_GREATER
using Xunit;

namespace Tokens.Validators;

public class IsTimeValidatorTests
{
    private readonly IsTimeValidator _validator = new();

    [Fact]
    public void GivenTimeOnlyString_WhenValidating_ThenReturnsTrue()
    {
        Assert.True(_validator.IsValid("14:30:00"));
    }

    [Fact]
    public void GivenDateTimeString_WhenValidating_ThenReturnsFalse()
    {
        // IsTime rejects values with date components
        Assert.False(_validator.IsValid("2024-01-15 14:30:00"));
    }

    [Fact]
    public void GivenInvalidString_WhenValidating_ThenReturnsFalse()
    {
        Assert.False(_validator.IsValid("hello"));
    }
}
#endif
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsDateTimeValidatorTests|IsDateValidatorTests|IsTimeValidatorTests"`
Expected: FAIL

- [ ] **Step 3: Rewrite IsDateTimeValidator and implement IsDate/IsTime**

Update `src/Tokenizer/Validators/IsDateTimeValidator.cs`:

```csharp
using Tokens.Temporal;

namespace Tokens.Validators;

/// <summary>
/// Validates that the token value is a parseable date/time string.
/// Time is optional (defaults to midnight).
/// </summary>
public sealed class IsDateTimeValidator : IOptionsAwareValidator
{
    /// <inheritdoc />
    public bool IsValid(object value, params string[] args)
    {
        return IsValid(value, args, new TokenizerOptions());
    }

    /// <inheritdoc />
    public bool IsValid(object value, string[] args, TokenizerOptions options)
    {
        if (value == null) return false;

        var valueString = value.ToString();
        if (string.IsNullOrEmpty(valueString)) return false;

        return TemporalParser.TryParse(valueString, args, options, out _);
    }
}
```

Create `src/Tokenizer/Validators/IsDateValidator.cs`:

```csharp
#if NET6_0_OR_GREATER
using System.Globalization;
using Tokens.Temporal;

namespace Tokens.Validators;

/// <summary>
/// Validates that the token value is a date-only string.
/// Fails if a time component is present.
/// </summary>
public sealed class IsDateValidator : IOptionsAwareValidator
{
    /// <inheritdoc />
    public bool IsValid(object value, params string[] args)
    {
        return IsValid(value, args, new TokenizerOptions());
    }

    /// <inheritdoc />
    public bool IsValid(object value, string[] args, TokenizerOptions options)
    {
        if (value == null) return false;

        var valueString = value.ToString();
        if (string.IsNullOrEmpty(valueString)) return false;

        if (!TemporalParser.TryParse(valueString, args, options, out var dto))
            return false;

        // Reject if time component is non-midnight
        return dto.TimeOfDay == TimeSpan.Zero;
    }
}
#endif
```

Create `src/Tokenizer/Validators/IsTimeValidator.cs`:

```csharp
#if NET6_0_OR_GREATER
using System.Globalization;

namespace Tokens.Validators;

/// <summary>
/// Validates that the token value is a time-only string.
/// Fails if a date component is present.
/// </summary>
public sealed class IsTimeValidator : IOptionsAwareValidator
{
    /// <inheritdoc />
    public bool IsValid(object value, params string[] args)
    {
        return IsValid(value, args, new TokenizerOptions());
    }

    /// <inheritdoc />
    public bool IsValid(object value, string[] args, TokenizerOptions options)
    {
        if (value == null) return false;

        var valueString = value.ToString();
        if (string.IsNullOrEmpty(valueString)) return false;

        var culture = options.Culture ?? CultureInfo.InvariantCulture;

        if (args is { Length: > 0 } && !string.IsNullOrWhiteSpace(args[0]))
        {
            foreach (var format in args)
            {
                if (TimeOnly.TryParseExact(valueString, format, culture, DateTimeStyles.None, out _))
                    return true;
            }
            return false;
        }

        return TimeOnly.TryParse(valueString, culture, DateTimeStyles.None, out _);
    }
}
#endif
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsDateTimeValidatorTests|IsDateValidatorTests|IsTimeValidatorTests"`
Expected: All PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Validators/IsDateTimeValidator.cs src/Tokenizer/Validators/IsDateValidator.cs src/Tokenizer/Validators/IsTimeValidator.cs tests/Tokenizer.Tests/Validators/IsDateTimeValidatorTests.cs tests/Tokenizer.Tests/Validators/IsDateValidatorTests.cs tests/Tokenizer.Tests/Validators/IsTimeValidatorTests.cs
git commit -m "feat: rewrite IsDateTime validator, add IsDate and IsTime validators"
```

---

### Task 10: DateTimeProjection + PropertyPathSetter Auto-Conversion

**Files:**
- Create: `src/Tokenizer/Temporal/DateTimeProjection.cs`
- Modify: `src/Tokenizer/Reflection/PropertyPathSetter.cs`
- Create: `tests/Tokenizer.Tests/Temporal/DateTimeProjectionTests.cs`
- Modify: `tests/Tokenizer.Tests/Reflection/PropertyPathSetterTests.cs` (if exists, else create)

**Interfaces:**
- Consumes: `TemporalParser.TryParse()` (Task 6)
- Produces: `DateTimeProjection.Project(DateTimeOffset source, Type targetType, out object result)` returning `bool`, updated `PropertyPathSetter.ConvertValue` for auto-conversion

- [ ] **Step 1: Write failing tests for DateTimeProjection**

```csharp
using Xunit;

namespace Tokens.Temporal;

public class DateTimeProjectionTests
{
    [Fact]
    public void GivenDateTimeOffset_WhenProjectingToDateTimeOffset_ThenReturnsDirectly()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(2));

        // Act
        var result = DateTimeProjection.Project(source, typeof(DateTimeOffset));

        // Assert
        Assert.Equal(source, result);
    }

    [Fact]
    public void GivenUtcDateTimeOffset_WhenProjectingToDateTime_ThenReturnsUtcKind()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero);

        // Act
        var dt = (DateTime)DateTimeProjection.Project(source, typeof(DateTime));

        // Assert
        Assert.Equal(DateTimeKind.Utc, dt.Kind);
        Assert.Equal(14, dt.Hour);
    }

    [Fact]
    public void GivenNonUtcDateTimeOffset_WhenProjectingToDateTime_ThenReturnsUnspecifiedKind()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(2));

        // Act
        var dt = (DateTime)DateTimeProjection.Project(source, typeof(DateTime));

        // Assert
        Assert.Equal(DateTimeKind.Unspecified, dt.Kind);
        Assert.Equal(14, dt.Hour);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void GivenDateTimeOffset_WhenProjectingToDateOnly_ThenExtractsDate()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(2));

        // Act
        var date = (DateOnly)DateTimeProjection.Project(source, typeof(DateOnly));

        // Assert
        Assert.Equal(new DateOnly(2024, 1, 15), date);
    }

    [Fact]
    public void GivenDateTimeOffset_WhenProjectingToTimeOnly_ThenExtractsTime()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(2));

        // Act
        var time = (TimeOnly)DateTimeProjection.Project(source, typeof(TimeOnly));

        // Assert
        Assert.Equal(new TimeOnly(14, 30, 45), time);
    }
#endif
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DateTimeProjectionTests"`
Expected: Compilation error

- [ ] **Step 3: Implement DateTimeProjection**

Create `src/Tokenizer/Temporal/DateTimeProjection.cs`:

```csharp
namespace Tokens.Temporal;

/// <summary>
/// Projects a <see cref="DateTimeOffset"/> value to a target temporal type.
/// </summary>
internal static class DateTimeProjection
{
    /// <summary>
    /// Projects a <see cref="DateTimeOffset"/> to the specified target type.
    /// </summary>
    public static object Project(DateTimeOffset source, Type targetType)
    {
        if (targetType == typeof(DateTimeOffset)) return source;

        if (targetType == typeof(DateTime))
        {
            return source.Offset == TimeSpan.Zero
                ? source.UtcDateTime
                : source.DateTime;
        }

#if NET6_0_OR_GREATER
        if (targetType == typeof(DateOnly))
        {
            return DateOnly.FromDateTime(source.Date);
        }

        if (targetType == typeof(TimeOnly))
        {
            return TimeOnly.FromTimeSpan(source.TimeOfDay);
        }
#endif

        throw new InvalidOperationException(
            $"Cannot project DateTimeOffset to {targetType.Name}.");
    }

    /// <summary>
    /// Returns true if the target type is a temporal type that can be projected from DateTimeOffset.
    /// </summary>
    public static bool IsTemporalType(Type type)
    {
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return true;
#if NET6_0_OR_GREATER
        if (type == typeof(DateOnly) || type == typeof(TimeOnly)) return true;
#endif
        return false;
    }
}
```

- [ ] **Step 4: Update PropertyPathSetter for auto-conversion**

In `src/Tokenizer/Reflection/PropertyPathSetter.cs`, update `TryConvertNonIConvertible` to use `DateTimeProjection` for temporal types. Replace the existing DateTimeOffset/DateOnly/TimeOnly cases:

```csharp
private static object? TryConvertNonIConvertible(object value, Type targetType)
{
    var valueString = value.ToString();
    if (valueString == null) return null;

    try
    {
        if (targetType == typeof(Guid)) return Guid.Parse(valueString);
        if (targetType == typeof(TimeSpan)) return TimeSpan.Parse(valueString, CultureInfo.InvariantCulture);

        // DateTimeOffset projection — if value is already DateTimeOffset, project to target
        if (value is DateTimeOffset dto && DateTimeProjection.IsTemporalType(targetType))
        {
            return DateTimeProjection.Project(dto, targetType);
        }

        // Auto-conversion from string to temporal types
        if (DateTimeProjection.IsTemporalType(targetType))
        {
            var options = new TokenizerOptions();
            if (TemporalParser.TryParse(valueString, null, options, out var parsed))
            {
                return DateTimeProjection.Project(parsed, targetType);
            }
        }

        return null;
    }
    catch (FormatException ex)
    {
        throw new TypeConversionException(
            $"Unable to convert '{value}' to type {targetType.Name}", value, targetType, ex);
    }
}
```

Add `using Tokens.Temporal;` to the file's usings.

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DateTimeProjectionTests"`
Expected: All PASS

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Temporal/DateTimeProjection.cs src/Tokenizer/Reflection/PropertyPathSetter.cs tests/Tokenizer.Tests/Temporal/DateTimeProjectionTests.cs
git commit -m "feat: add DateTimeProjection and auto-conversion in PropertyPathSetter"
```

---

### Task 11: Diagnostics + Integration Tests

**Files:**
- Modify: `src/Tokenizer/Diagnostics/Hints/DateFormatHintGenerator.cs`
- Create: `tests/Tokenizer.Tests/Temporal/DateTimeIntegrationTests.cs`

**Interfaces:**
- Consumes: All previous tasks
- Produces: Updated `DateFormatHintGenerator`, end-to-end integration test suite

- [ ] **Step 1: Write integration tests covering the full pipeline**

```csharp
using System.Globalization;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Temporal;

public class DateTimeIntegrationTests : TokenizerTestBase
{
    public DateTimeIntegrationTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void GivenIso8601Template_WhenTokenizingAndAssigning_ThenProducesCorrectDateTime()
    {
        // Arrange
        var pattern = "Created: { Created : ToDateTime('yyyy-MM-ddTHH:mm:ssZ') }";
        var input = "Created: 2024-01-15T14:30:00Z";

        // Act
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert — tokenize produces DateTimeOffset
        var match = result.Matches.First(m => m.Token.Name == "Created");
        var dto = Assert.IsType<DateTimeOffset>(match.Value);
        Assert.Equal(TimeSpan.Zero, dto.Offset);
    }

    [Fact]
    public void GivenIso8601WithFractionalSeconds_WhenSingleFormatSpecified_ThenHandlesAllVariants()
    {
        // Arrange — the template author specifies one format, fractional seconds auto-tolerated
        var pattern = "Date: { Date : ToDateTime('yyyy-MM-ddTHH:mm:ssZ') }";

        var inputs = new[]
        {
            "Date: 2024-01-15T14:30:00Z",
            "Date: 2024-01-15T14:30:00.1Z",
            "Date: 2024-01-15T14:30:00.123Z",
            "Date: 2024-01-15T14:30:00.1234567Z",
        };

        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile(pattern).Template;

        foreach (var input in inputs)
        {
            // Act
            var result = tokenizer.Tokenize(template, input);

            // Assert
            Assert.True(result.Success, $"Failed for input: {input}");
        }
    }

    [Fact]
    public void GivenCultureInFrontMatter_WhenTokenizingPortugueseDate_ThenParsesCorrectly()
    {
        // Arrange
        var pattern = """
                      ---
                      culture: pt-BR
                      terminateOnNewLine: true
                      ---
                      Data: { Date : ToDateTime('dd-MMM-yyyy') }
                      """;
        var input = "Data: 15-mar-2024";

        // Act
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        var dto = (DateTimeOffset)result.Matches.First(m => m.Token.Name == "Date").Value;
        Assert.Equal(3, dto.Month);
        Assert.Equal(15, dto.Day);
    }

    [Fact]
    public void GivenNoFormatString_WhenTokenizingUnambiguousDate_ThenAutoDetects()
    {
        // Arrange
        var pattern = "Date: { Date : ToDateTime }";
        var input = "Date: 2024-01-15";

        // Act
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.True(result.Success);
        var dto = (DateTimeOffset)result.Matches.First(m => m.Token.Name == "Date").Value;
        Assert.Equal(2024, dto.Year);
    }

    [Fact]
    public void GivenTimezoneAbbreviation_WhenTokenizing_ThenPreservesOffset()
    {
        // Arrange
        var pattern = "Date: { Date : ToDateTime }";
        var input = "Date: 2024-01-15 14:30:00 CEST";

        // Act
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.True(result.Success);
        var dto = (DateTimeOffset)result.Matches.First(m => m.Token.Name == "Date").Value;
        Assert.Equal(TimeSpan.FromHours(2), dto.Offset);
    }

    [Fact]
    public void GivenDefaultOffset_WhenTokenizingDateWithoutOffset_ThenAppliesDefault()
    {
        // Arrange
        var pattern = """
                      ---
                      defaultOffset: +02:00
                      ---
                      Date: { Date : ToDateTime('yyyy-MM-dd') }
                      """;
        var input = "Date: 2024-01-15";

        // Act
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        var dto = (DateTimeOffset)result.Matches.First(m => m.Token.Name == "Date").Value;
        Assert.Equal(TimeSpan.FromHours(2), dto.Offset);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void GivenToDateTransformer_WhenAssigning_ThenProducesDateOnly()
    {
        // Arrange
        var pattern = "Birthday: { Birthday : ToDate('yyyy-MM-dd') }";
        var input = "Birthday: 1990-06-15";

        // Act
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        var match = result.Matches.First(m => m.Token.Name == "Birthday");
        Assert.IsType<DateOnly>(match.Value);
        Assert.Equal(new DateOnly(1990, 6, 15), match.Value);
    }
#endif

    // --- Whois-derived real-world format tests ---

    [Theory]
    [InlineData("2024-01-15", "yyyy-MM-dd")]
    [InlineData("15-Jan-2024", "dd-MMM-yyyy")]
    [InlineData("20240115", "yyyyMMdd")]
    [InlineData("2024.01.15 14:30:00", "yyyy.MM.dd HH:mm:ss")]
    [InlineData("2024/01/15", "yyyy/MM/dd")]
    [InlineData("15.01.2024", "dd.MM.yyyy")]
    [InlineData("15/01/2024", "dd/MM/yyyy")]
    public void GivenWhoisRealWorldFormat_WhenTokenizing_ThenParsesCorrectly(string dateValue, string format)
    {
        // Arrange
        var pattern = $"Date: {{ Date : ToDateTime('{format}') }}";
        var input = $"Date: {dateValue}";

        // Act
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.True(result.Success, $"Failed for format {format} with value {dateValue}");
        Assert.IsType<DateTimeOffset>(result.Matches.First(m => m.Token.Name == "Date").Value);
    }
}
```

- [ ] **Step 2: Update DateFormatHintGenerator**

In `src/Tokenizer/Diagnostics/Hints/DateFormatHintGenerator.cs`, update `CommonFormats` to use the `DatePatternRecognizer` registry if possible, or expand the hardcoded list to include the formats from the recognizer table. Update the `TryGenerateHint` method to try `DateTimeOffset.TryParseExact` instead of `DateTime.TryParseExact`:

```csharp
// Change DateTime.TryParseExact to DateTimeOffset.TryParseExact
if (DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture,
                                  DateTimeStyles.None, out _))
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All PASS

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Diagnostics/Hints/DateFormatHintGenerator.cs tests/Tokenizer.Tests/Temporal/DateTimeIntegrationTests.cs
git commit -m "feat: update DateFormatHintGenerator, add end-to-end DateTime integration tests"
```
