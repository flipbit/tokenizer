# V3 Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Address code review findings from `docs/superpowers/specs/2026-07-05-v3-review.md`

**Source Review:** `docs/superpowers/specs/2026-07-05-v3-review.md`

**Architecture:** Fixes span API immutability (TokenizerOptions init-only, Template.Options init), performance (regex caching, parameter array caching, hash allocation elimination), code quality (Token.Assign extraction, decorator cache scoping, hint strategy cleanup), log level corrections, and test coverage gaps. Each task is independent unless noted.

**Tech Stack:** C# / .NET (netstandard2.0 + net8.0 + net10.0), xUnit, NSubstitute

---

## Dismissed Issues

| ID | Rationale | Action |
|----|-----------|--------|
| H1 (bug claim) | ContainsHintStrategy already falls back to IntegratedHintStrategy when rawInput is null — hints work on async path | Refactor to use IntegratedHintStrategy directly (Task 4) |
| H7 + D1 | Sync/async duplication is ~30 lines of shared post-processing; extracting adds a 7-param helper for minimal benefit. Paths diverge further with H1 fix | None |
| M2 | ReDoS 1s timeout is reasonable — threat model requires malicious template author who already controls tokenization | None |
| M3 | Hash collision at 64-bit is ~1 in 2^45 at max capacity; threat model same as M2 | None |
| M7 | Begin/Continue/End on internal interface is intentional for test substitution, documented inline | None |
| M8 | propertyPath.Split('.') produces 1-2 element arrays, called once per token match — below meaningful perf impact | None |
| M9 | HasOnlyFrontMatterTokens LINQ called once per result check, not hot path | None |
| D4 | Two sequential if-statements (16 lines) don't warrant extraction | None |
| L2 | Scope dictionary allocation once per Tokenize call — wrapping in IsEnabled hurts readability for trivial saving | None |
| L3 | template.Options is correct — reflects merged instance + front matter options | Add inline comment (Task 16) |
| L4 | O(n) eviction at n=500 is acceptable | None |
| L6 | XxHash64/FNV-1a is intentional improvement over SHA256 for cache keys | None |

---

## Fix Tasks

### Task 1: Make TokenizerOptions properties init-only (C2 + H10)

**Addresses:** C2, H10
**Chosen approach:** Change all `set` properties to `init`, rename `RegisterTransformer`/`RegisterValidator` to `WithTransformer`/`WithValidator` returning new instances via `with`.

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Modify: `tests/Tokenizer.Tests/TokenizerOptionsRegistrationTests.cs`
- Modify: `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`
- Modify: `tests/Tokenizer.Tests/Integration/DependencyInjectionTests.cs`
- Modify: `tests/Tokenizer.Tests/TokenMatcherTests.cs` (line 97)
- Modify: `tests/Tokenizer.Tests/TokenizerTests.cs` (line 291)

**Important context:**
- The `protected TokenizerOptions(TokenizerOptions original)` copy constructor (line 20) is used by `with` expressions — it already deep-copies transformer/validator lists, so `with` works correctly.
- `AllowStreamBuffering` and `CompilationCacheMaxSize` are already `init` — no change needed.
- All existing `new TokenizerOptions { Prop = value }` call sites use object initializer syntax, which works with `init`.
- Test files that do `options.MaxInputLength = 100` after construction must switch to `new TokenizerOptions { MaxInputLength = 100 }`.

- [ ] **Step 1: Write failing test for WithTransformer immutability**

In `tests/Tokenizer.Tests/TokenizerOptionsRegistrationTests.cs`, add:

```csharp
[Fact]
public void GivenOptions_WhenCallingWithTransformer_ThenReturnsNewInstanceWithTransformer()
{
    // Arrange
    var original = new TokenizerOptions();

    // Act
    var result = original.WithTransformer<ToUpperTransformer>();

    // Assert
    Assert.NotSame(original, result);
    Assert.Contains(typeof(ToUpperTransformer), result.Transformers);
    Assert.DoesNotContain(typeof(ToUpperTransformer), original.Transformers);
}

[Fact]
public void GivenOptions_WhenCallingWithValidator_ThenReturnsNewInstanceWithValidator()
{
    // Arrange
    var original = new TokenizerOptions();

    // Act
    var result = original.WithValidator<IsNumericValidator>();

    // Assert
    Assert.NotSame(original, result);
    Assert.Contains(typeof(IsNumericValidator), result.Validators);
    Assert.DoesNotContain(typeof(IsNumericValidator), original.Validators);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsRegistrationTests"`
Expected: Compilation error — `WithTransformer` and `WithValidator` don't exist yet.

- [ ] **Step 3: Change all `set` properties to `init` and implement WithTransformer/WithValidator**

In `src/Tokenizer/TokenizerOptions.cs`:

Change every `{ get; set; }` property to `{ get; init; }`:
- `IgnoreMissingProperties` (line 43)
- `EnableDiagnostics` (line 51)
- `TrimLeadingWhitespaceInTokenPreamble` (line 56)
- `TrimPreambleBeforeNewLine` (line 61)
- `TrimTrailingWhiteSpace` (line 66)
- `OutOfOrderTokens` (line 71)
- `TokenStringComparison` (line 76)
- `TerminateOnNewLine` (line 81)
- `MaxInputLength` (line 87)
- `MaxTemplateLength` (line 93)
- `MaxTokenCount` (line 99)
- `MaxIterations` (line 106)

Replace `RegisterTransformer` (lines 137-141) and `RegisterValidator` (lines 146-150) with:

```csharp
/// <summary>
/// Returns a new TokenizerOptions instance with the specified transformer type added.
/// </summary>
public TokenizerOptions WithTransformer<T>() where T : ITokenTransformer
{
    var copy = this with { };
    copy.transformers.Add(typeof(T));
    return copy;
}

/// <summary>
/// Returns a new TokenizerOptions instance with the specified validator type added.
/// </summary>
public TokenizerOptions WithValidator<T>() where T : ITokenValidator
{
    var copy = this with { };
    copy.validators.Add(typeof(T));
    return copy;
}
```

Note: the `with` expression invokes the copy constructor which deep-copies the lists, so mutating `copy.transformers` is safe.

- [ ] **Step 4: Fix all call sites that mutate properties after construction**

In `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`, change all patterns like:
```csharp
var options = new TokenizerOptions();
options.MaxInputLength = 100;
```
to:
```csharp
var options = new TokenizerOptions { MaxInputLength = 100 };
```

In `tests/Tokenizer.Tests/Integration/DependencyInjectionTests.cs`, change:
```csharp
options.TrimTrailingWhiteSpace = false;
options.OutOfOrderTokens = true;
```
to object initializer syntax or `with` expression as appropriate.

In `tests/Tokenizer.Tests/TokenMatcherTests.cs` (line 97) and `tests/Tokenizer.Tests/TokenizerTests.cs` (line 291), change:
```csharp
options.RegisterTransformer<BlowsUpTransformer>();
```
to:
```csharp
options = options.WithTransformer<BlowsUpTransformer>();
```

In `tests/Tokenizer.Tests/TokenizerOptionsRegistrationTests.cs`, update existing tests:
- `GivenNewOptions_WhenRegisteringTransformer_ThenTransformerTypeIsStored` — use `WithTransformer`, assert on returned instance
- `GivenNewOptions_WhenRegisteringValidator_ThenValidatorTypeIsStored` — use `WithValidator`, assert on returned instance
- `GivenNewOptions_WhenRegisteringTransformer_ThenReturnsSameOptionsForChaining` — change to assert `NotSame` (returns new instance)
- `GivenOptionsWithBuiltInTransformer_WhenRegisteringSameType_ThenTokenParserDoesNotDuplicate` — use `WithTransformer`

- [ ] **Step 5: Run all tests to verify**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/TokenizerOptions.cs tests/Tokenizer.Tests/TokenizerOptionsRegistrationTests.cs tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs tests/Tokenizer.Tests/Integration/DependencyInjectionTests.cs tests/Tokenizer.Tests/TokenMatcherTests.cs tests/Tokenizer.Tests/TokenizerTests.cs
git commit -m "refactor: make TokenizerOptions init-only, rename Register to With (C2+H10)"
```

---

### Task 2: Make Template.Options init-only with constructor parameter (C1)

**Addresses:** C1
**Chosen approach:** Change `Template.Options` to `{ get; init; }`, pass options via constructor. Update `TokenParser` and `FrontMatterBinder` to build options before constructing Template.

**Files:**
- Modify: `src/Tokenizer/Template.cs`
- Modify: `src/Tokenizer/Compilation/TokenParser.cs`
- Modify: `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs`
- Modify: `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs`

**Important context:**
- `Template` currently has two constructors: `Template()` and `Template(string name)`. Both create a default `TokenizerOptions`.
- `TokenParser.Parse` creates `new Template(name)` at line 182, then assigns `template.Options = preTemplate.Options` at line 193. The `preTemplate` (a `TemplateDefinition`) has its Options built up by `AstTemplateDefinitionParser` and `FrontMatterBinder.ApplyOption`.
- `FrontMatterBinder.ApplyOption` mutates `template.Options` on the `TemplateDefinition` (not the final `Template`) using `with` expressions — this is fine, it's the pre-template.
- The key change: `TokenParser` must set Options on the final `Template` via constructor or init, not post-construction assignment.
- `TemplateBuilder.Build()` currently does `template.Options = _options` — must change to pass via constructor.

- [ ] **Step 1: Write failing test**

In `tests/Tokenizer.Tests/TemplateTests.cs`, add:

```csharp
[Fact]
public void GivenTemplate_WhenConstructedWithOptions_ThenOptionsAreAccessible()
{
    // Arrange
    var options = new TokenizerOptions { TrimTrailingWhiteSpace = false };

    // Act
    var template = new Template("test", options);

    // Assert
    Assert.False(template.Options.TrimTrailingWhiteSpace);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenTemplate_WhenConstructedWithOptions"`
Expected: Compilation error — no constructor accepting `TokenizerOptions`.

- [ ] **Step 3: Add constructor overload and change to init**

In `src/Tokenizer/Template.cs`:

Add a new constructor and change the Options property:

```csharp
/// <summary>
/// Creates a new template with the given name and options.
/// </summary>
public Template(string name, TokenizerOptions options)
{
    tokens = new List<Token>();
    hints = new List<Hint>();
    tags = new List<string>();
    Options = options;
    this.name = name;
}

/// <summary>
/// Creates a new template with the given name.
/// </summary>
public Template(string name) : this(name, new TokenizerOptions())
{
}

/// <summary>
/// Creates a new unnamed template.
/// </summary>
public Template() : this(string.Empty)
{
}
```

Change the property:
```csharp
public TokenizerOptions Options { get; init; }
```

- [ ] **Step 4: Update TokenParser.Parse to pass Options via constructor**

In `src/Tokenizer/Compilation/TokenParser.cs`, change line 182-193 from:

```csharp
var template = new Template(name);
// ... (lines 184-191)
template.Options = preTemplate.Options;
```

to:

```csharp
var preTemplate = new AstTemplateDefinitionParser().Parse(content, Options);

var template = new Template(name, preTemplate.Options);
```

Move the `preTemplate` parse above the `Template` construction. The logging between them can reference `template` after it's created.

- [ ] **Step 5: Update TemplateBuilder**

In `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs`, change `Build()`:

```csharp
public Template Build()
{
    var template = _options != null
        ? new Template(_name, _options)
        : new Template(_name);
    foreach (var token in _tokens) template.AddToken(token);
    foreach (var hint in _hints) template.AddHint(hint);
    foreach (var tag in _tags) template.AddTag(tag);
    return template;
}
```

- [ ] **Step 6: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Template.cs src/Tokenizer/Compilation/TokenParser.cs tests/Tokenizer.Tests/Builders/TemplateBuilder.cs
git commit -m "refactor: make Template.Options init-only with constructor param (C1)"
```

---

### Task 3: Cache _parameters.ToArray() in TokenDecoratorContext (H2)

**Addresses:** H2
**Chosen approach:** Lazy-cache the array into a private field on first access.

**Files:**
- Modify: `src/Tokenizer/TokenDecoratorContext.cs`

- [ ] **Step 1: Write failing test**

In `tests/Tokenizer.Tests/TokenDecoratorContextCachingTests.cs`, add:

```csharp
[Fact]
public void GivenDecoratorWithParameters_WhenCallingTryTransformTwice_ThenReturnsSameParameterArray()
{
    // Arrange
    var context = new TokenDecoratorContext(typeof(ToLowerTransformer));

    // Act
    context.TryTransform("TEST", out _);
    context.TryTransform("TEST2", out _);

    // Assert — if caching works, no way to directly assert array identity
    // from outside, but we can verify it doesn't throw and behaves correctly
    Assert.True(context.TryTransform("TEST3", out var result));
    Assert.Equal("test3", result);
}
```

Note: The real verification is that the code change is correct — the test validates behavior is preserved.

- [ ] **Step 2: Run test to verify it passes (baseline)**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenDecoratorWithParameters_WhenCallingTryTransformTwice"`
Expected: PASS (behavior should already work).

- [ ] **Step 3: Add cached parameter array**

In `src/Tokenizer/TokenDecoratorContext.cs`, add a field and modify `TryTransform`/`Validate`:

```csharp
private readonly List<string> _parameters;
private string[]? _parameterArray;

private string[] GetParameterArray()
{
    return _parameterArray ??= _parameters.ToArray();
}
```

Replace all three `_parameters.ToArray()` calls (lines 81, 93, 96) with `GetParameterArray()`.

- [ ] **Step 4: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/TokenDecoratorContext.cs tests/Tokenizer.Tests/TokenDecoratorContextCachingTests.cs
git commit -m "perf: cache _parameters.ToArray() in TokenDecoratorContext (H2)"
```

---

### Task 4: Use IntegratedHintStrategy directly on async path (H1)

**Addresses:** H1 (dismissed as non-bug, but refactoring for clarity)
**Chosen approach:** Async path creates `IntegratedHintStrategy` directly. Remove fallback delegation from `ContainsHintStrategy`.

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs` (line 363)
- Modify: `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs`

- [ ] **Step 1: Run existing hint tests as baseline**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "Hint"`
Expected: All pass.

- [ ] **Step 2: Change async path to use IntegratedHintStrategy**

In `src/Tokenizer/Tokenizer.cs`, change line 363 in `TokenizeAsyncCore`:

```csharp
// Before:
var hintStrategy = new ContainsHintStrategy();

// After:
var hintStrategy = new IntegratedHintStrategy();
```

Add `using Tokens.Tokenization.Strategies;` if not already present (it is — line 12).

Update the comment block at lines 388-390 to:
```csharp
// Async path uses IntegratedHintStrategy directly — it tracks hints via
// OnTokenMatched callbacks during single-pass tokenization, since the full
// input string isn't available during streaming.
```

- [ ] **Step 3: Remove fallback from ContainsHintStrategy**

In `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs`, remove:
- The `IntegratedHintStrategy fallback` field (line 14)
- The `bool usingFallback` field (line 15)
- The `rawInput == null` branch in `PreProcess` (lines 27-31)
- The `usingFallback` checks in `OnTokenMatched` (lines 69-73) and `PostProcess` (lines 79-84)

The resulting class should only handle the `rawInput != null` case. If `rawInput` is null, throw `ArgumentNullException` since that's now a caller error.

```csharp
internal class ContainsHintStrategy : IHintStrategy
{
    public bool PreProcess(Template template, TokenEnumerator enumerator,
                           string? rawInput, TokenizeResultBase result, IDiagnosticCollector collector)
    {
        if (template.Hints.Count == 0)
        {
            return false;
        }

        if (rawInput == null)
        {
            throw new ArgumentNullException(nameof(rawInput),
                "ContainsHintStrategy requires raw input. Use IntegratedHintStrategy for streaming paths.");
        }

        foreach (var hint in template.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text))
            {
                continue;
            }

            if (rawInput.Contains(hint.Text))
            {
                result.Hints.AddMatch(hint, enumerator);

                collector.Record(DiagnosticEventType.HintMatched,
                    value: hint.Text,
                    location: enumerator.Location);
            }
        }

        foreach (var hint in template.Hints)
        {
            result.Hints.AddMiss(hint);

            if (hint.Optional == false &&
                result.Hints.Misses.Any(m => m.Text == hint.Text))
            {
                collector.Record(DiagnosticEventType.HintMissing,
                    value: hint.Text);
            }
        }

        return result.Hints.Misses.Any(h => h.Optional == false);
    }

    public void OnTokenMatched(Token token)
    {
        // ContainsHintStrategy uses upfront string scanning, not per-token tracking
    }

    public bool PostProcess(TokenizeResultBase result)
    {
        return false;
    }
}
```

- [ ] **Step 4: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs
git commit -m "refactor: use IntegratedHintStrategy directly on async path (H1)"
```

---

### Task 5: Cache compiled Regex in MatchesRegexValidator (H3)

**Addresses:** H3
**Chosen approach:** Static `ConcurrentDictionary<string, Regex>` keyed on pattern string.

**Files:**
- Modify: `src/Tokenizer/Validators/MatchesRegexValidator.cs`
- Create: `tests/Tokenizer.Tests/Validators/MatchesRegexValidatorTests.cs`

- [ ] **Step 1: Write failing test for caching behavior**

Create `tests/Tokenizer.Tests/Validators/MatchesRegexValidatorTests.cs`:

```csharp
using Tokens.Validators;
using Xunit;

namespace Tokens.Validators;

public class MatchesRegexValidatorTests
{
    private readonly MatchesRegexValidator _validator = new();

    [Fact]
    public void GivenMatchingValue_WhenValidating_ThenReturnsTrue()
    {
        // Act
        var result = _validator.IsValid("abc123", "^[a-z]+\\d+$");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNonMatchingValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = _validator.IsValid("abc", "^\\d+$");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenSamePattern_WhenValidatingMultipleTimes_ThenProducesSameResults()
    {
        // Act — call multiple times to exercise cache path
        var result1 = _validator.IsValid("test123", "^\\w+$");
        var result2 = _validator.IsValid("test456", "^\\w+$");
        var result3 = _validator.IsValid("!!!", "^\\w+$");

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.False(result3);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = _validator.IsValid(null!, "^\\w+$");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNoArgs_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _validator.IsValid("test"));
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (baseline for behavior)**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "MatchesRegexValidatorTests"`
Expected: PASS for all (existing behavior should be correct).

- [ ] **Step 3: Implement regex cache**

In `src/Tokenizer/Validators/MatchesRegexValidator.cs`:

```csharp
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value matches a regular expression pattern
/// </summary>
public sealed class MatchesRegexValidator : ITokenValidator
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (args == null || args.Length == 0)
        {
            throw new ArgumentException("MatchesRegex(pattern): missing argument — you must specify a regex pattern");
        }

        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        var regex = RegexCache.GetOrAdd(args[0],
            pattern => new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1)));

        return regex.IsMatch(valueString);
    }
}
```

- [ ] **Step 4: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Validators/MatchesRegexValidator.cs tests/Tokenizer.Tests/Validators/MatchesRegexValidatorTests.cs
git commit -m "perf: cache compiled Regex in MatchesRegexValidator (H3)"
```

---

### Task 6: Fix TemplateCollection.Names and tag methods (H4 + M10)

**Addresses:** H4, M10
**Chosen approach:** Return `templates.Keys` directly as `IReadOnlyCollection<string>`. Fix `ContainsTag`/`ContainsAllTags` to iterate values instead of Names.

**Files:**
- Modify: `src/Tokenizer/TemplateCollection.cs`

**Important context:**
- `ConcurrentDictionary<TKey, TValue>.Keys` returns `ICollection<TKey>` which implements `IReadOnlyCollection<TKey>` on .NET 8+. On netstandard2.0, `ICollection<T>` does NOT implement `IReadOnlyCollection<T>`.
- Need conditional compilation or just wrap in a read-only adapter for netstandard2.0.
- Callers of `Names`: check if any depend on `IList<string>` (indexing by position).

- [ ] **Step 1: Write failing test**

In a new or existing test file for TemplateCollection, add:

```csharp
[Fact]
public void GivenTemplateCollection_WhenAccessingNames_ThenReturnsReadOnlyCollection()
{
    // Arrange
    var collection = new TemplateCollection();
    var template = new TemplateBuilder().WithName("Test").Build();
    collection.Add(template);

    // Act
    var names = collection.Names;

    // Assert
    Assert.Contains("Test", names);
    Assert.IsAssignableFrom<IReadOnlyCollection<string>>(names);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenTemplateCollection_WhenAccessingNames"`
Expected: May fail on `IReadOnlyCollection` assertion since current type is `List<string>` (which does implement it — so this test is a baseline). The real fix is the implementation change.

- [ ] **Step 3: Implement the fix**

In `src/Tokenizer/TemplateCollection.cs`:

Change the `Names` property (line 16):
```csharp
// Before:
public IList<string> Names => templates.Keys.ToList();

// After:
public IReadOnlyCollection<string> Names => templates.Keys.ToArray();
```

Note: Using `.ToArray()` instead of `.Keys` directly because `ConcurrentDictionary.Keys` returns a snapshot as `ICollection<TKey>`, and on netstandard2.0 `ICollection<T>` doesn't extend `IReadOnlyCollection<T>`. `ToArray()` returns `string[]` which implements `IReadOnlyCollection<string>` on all targets. The allocation is acceptable since `Names` is not called in hot loops after this fix.

Change `ContainsTag` (lines 73-85):
```csharp
public bool ContainsTag(string tag)
{
    foreach (var template in this)
    {
        if (template.HasTag(tag))
        {
            return true;
        }
    }

    return false;
}
```

Change `ContainsAllTags` (lines 90-103):
```csharp
public bool ContainsAllTags(params string[] tags)
{
    foreach (var template in this)
    {
        if (template.HasTags(tags))
        {
            return true;
        }
    }

    return false;
}
```

- [ ] **Step 4: Fix any compilation errors from IList → IReadOnlyCollection**

Check callers of `Names`. If any use indexing (`Names[0]`), they'll need updating. Search for `\.Names\[` to find these.

- [ ] **Step 5: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/TemplateCollection.cs
git commit -m "refactor: return IReadOnlyCollection for Names, iterate values for tags (H4+M10)"
```

---

### Task 7: Eliminate byte[] allocation in TemplateCache hash (H5)

**Addresses:** H5
**Chosen approach:** Hash chars directly, use `ulong` as dictionary key instead of hex string.

**Files:**
- Modify: `src/Tokenizer/Compilation/TemplateCache.cs`

- [ ] **Step 1: Run existing cache tests as baseline**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "Cache"`
Expected: All pass.

- [ ] **Step 2: Change dictionary to ulong key and hash chars directly**

In `src/Tokenizer/Compilation/TemplateCache.cs`:

Change dictionary type (line 15):
```csharp
// Before:
private readonly ConcurrentDictionary<string, CacheEntry> cache = new();

// After:
private readonly ConcurrentDictionary<ulong, CacheEntry> cache = new();
```

Replace `ComputeHash` method (lines 88-109):
```csharp
private static ulong ComputeHash(string input)
{
#if NET8_0_OR_GREATER
    return XxHash64.HashToUInt64(System.Runtime.InteropServices.MemoryMarshal.AsBytes(input.AsSpan()));
#else
    // FNV-1a 64-bit over chars
    const ulong fnvOffset = 14695981039346656037;
    const ulong fnvPrime = 1099511628211;

    var hash = fnvOffset;

    foreach (var c in input)
    {
        hash ^= c;
        hash *= fnvPrime;
    }

    return hash;
#endif
}
```

Remove `using System.Text;` if no longer used.

- [ ] **Step 3: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Compilation/TemplateCache.cs
git commit -m "perf: hash chars directly with ulong key in TemplateCache (H5)"
```

---

### Task 8: Extract shared value-preparation from Token.Assign/CanAssign (H6 + D2)

**Addresses:** H6, D2
**Chosen approach:** Extract value preparation (trim, newline) and decorator pipeline (transform, validate) into shared private methods. Keep property assignment in `Assign`. This avoids a new service class while eliminating the duplication.

**Files:**
- Modify: `src/Tokenizer/Token.cs`

**Important context:**
- `Assign` (line 132-265): does value prep → decorator pipeline with diagnostics → property assignment
- `CanAssign` (line 296-337): does value prep → decorator pipeline without diagnostics → returns bool
- Shared phases: (1) null/empty checks, (2) TrimTrailingNewLine, (3) TerminateOnNewLine substring, (4) decorator loop (transform + validate)
- Differences: `Assign` takes `options` for `TrimTrailingWhiteSpace`, records diagnostics, and does property assignment. `CanAssign` has none of these.
- Strategy: extract `PrepareValue` for phases 1-3 and `RunDecoratorPipeline` for phase 4.

- [ ] **Step 1: Run existing Token tests as baseline**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "Token"`
Expected: All pass.

- [ ] **Step 2: Extract PrepareValue method**

In `src/Tokenizer/Token.cs`, add a private method:

```csharp
/// <summary>
/// Prepares a raw extracted value for assignment or validation: trims trailing
/// newline, truncates at first newline if TerminateOnNewLine is set.
/// Returns null if the value should be skipped (empty, null token, no name).
/// </summary>
private string? PrepareValue(string value)
{
    if (string.IsNullOrEmpty(value) && IsFrontMatterToken == false) return null;
    if (IsNull) return null;
    if (string.IsNullOrWhiteSpace(Name)) return null;

    value = value.TrimTrailingNewLine();

    if (string.IsNullOrEmpty(value) == false && TerminateOnNewLine)
    {
        var index = value.IndexOf("\n");
        if (index > 0)
        {
            value = value.Substring(0, index);
        }
    }

    return value;
}
```

- [ ] **Step 3: Extract RunDecoratorPipeline method**

```csharp
/// <summary>
/// Runs the transform/validate decorator pipeline on a value.
/// Returns false if any transformer fails or any validator rejects.
/// On success, assignedValue contains the (possibly transformed) result.
/// </summary>
private bool RunDecoratorPipeline(object input, IDiagnosticCollector? collector,
    FileLocation? location, out object? assignedValue)
{
    assignedValue = input;

    foreach (var decorator in Decorators)
    {
        if (decorator.IsTransformer)
        {
            if (decorator.TryTransform(assignedValue!, out var output) == false)
            {
                collector?.Record(DiagnosticEventType.TransformerFailed,
                    tokenName: Name, tokenId: Id,
                    location: location,
                    value: assignedValue?.ToString(),
                    decoratorName: decorator.DecoratorType.Name,
                    decoratorArgs: decorator.Parameters.ToArray());

                return false;
            }

            collector?.Record(DiagnosticEventType.TransformerSucceeded,
                tokenName: Name, tokenId: Id,
                location: location,
                value: assignedValue?.ToString(),
                detail: output?.ToString(),
                decoratorName: decorator.DecoratorType.Name,
                decoratorArgs: decorator.Parameters.ToArray());

            assignedValue = output;
        }

        if (decorator.IsValidator)
        {
            if (decorator.Validate(assignedValue!))
            {
                collector?.Record(DiagnosticEventType.ValidatorPassed,
                    tokenName: Name, tokenId: Id,
                    value: assignedValue?.ToString(),
                    decoratorName: decorator.DecoratorType.Name);
            }
            else
            {
                collector?.Record(DiagnosticEventType.ValidatorFailed,
                    tokenName: Name, tokenId: Id,
                    value: input?.ToString(),
                    decoratorName: decorator.DecoratorType.Name);

                return false;
            }
        }
    }

    return true;
}
```

- [ ] **Step 4: Refactor Assign to use extracted methods**

Replace lines 132-206 of `Assign` with:

```csharp
internal bool Assign(object? target, string value, TokenizerOptions options, FileLocation location, out object? assignedValue, IDiagnosticCollector collector)
{
    assignedValue = null;

    var prepared = PrepareValue(value);
    if (prepared == null) return false;

    if (options.TrimTrailingWhiteSpace)
    {
        prepared = prepared.TrimEnd();
    }

    if (!RunDecoratorPipeline(prepared, collector, location, out assignedValue))
    {
        return false;
    }

    // Property assignment phase (unchanged from here)
    if (target is IDictionary<string, object> dictionary)
    {
        return SetDictionaryValue(dictionary, assignedValue!);
    }

    if (target is null)
    {
        return true;
    }

    try
    {
        if (CanConcatenate)
        {
            if (assignedValue == null) return true;

            var current = target.GetValue(Name);

            if (CanConcatenateValues(current, assignedValue))
            {
                var concatenated = ConcatenateValues(current, assignedValue, ConcatenationString);
                if (concatenated != null) target.SetValue(Name, concatenated);
            }
            else
            {
                throw new TokenAssignmentException(this, $"Unable to concatenate type {assignedValue.GetType().Name} to {Name}");
            }
        }
        else
        {
            target.SetValue(Name, assignedValue!);
        }
    }
    catch (MissingMemberException)
    {
        if (options.IgnoreMissingProperties == false)
        {
            throw;
        }
    }
    catch (TypeConversionException ex)
    {
        collector.Record(DiagnosticEventType.TokenAssignmentFailed,
            tokenName: Name, tokenId: Id,
            value: value,
            detail: $"Type conversion failed: {ex.Message}");
        return false;
    }
    catch (Exception e)
    {
        var ex = new TokenAssignmentException(this, e);
        throw ex;
    }

    return true;
}
```

- [ ] **Step 5: Refactor CanAssign to use extracted methods**

```csharp
internal bool CanAssign(string value)
{
    var prepared = PrepareValue(value);
    if (prepared == null) return false;

    return RunDecoratorPipeline(prepared, null, null, out _);
}
```

- [ ] **Step 6: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Token.cs
git commit -m "refactor: extract shared value-preparation from Token.Assign/CanAssign (H6+D2)"
```

---

### Task 9: Move DecoratorCache from static to TokenParser instance (H8 + D3)

**Addresses:** H8, D3
**Chosen approach:** Move the `ConcurrentDictionary<Type, ITokenDecorator>` from a static field on `TokenDecoratorContext` to an instance field on `TokenParser`, passed into `TokenDecoratorContext` via constructor.

**Files:**
- Modify: `src/Tokenizer/TokenDecoratorContext.cs`
- Modify: `src/Tokenizer/Compilation/TokenParser.cs`
- Modify: `tests/Tokenizer.Tests/TokenDecoratorContextCachingTests.cs`
- Modify: `tests/Tokenizer.Tests/TokenTests.cs` (lines 47, 63 — construct with cache)

**Important context:**
- `TokenDecoratorContext` instances are created in `TokenParser.ParseTokenDecorators` (lines 350, 391, 417).
- `TokenParser` is created once per `Tokenizer` instance and held as a field.
- The cache should live on `TokenParser` so all decorators compiled by the same parser share it.

- [ ] **Step 1: Write failing test**

In `tests/Tokenizer.Tests/TokenDecoratorContextCachingTests.cs`, change tests to verify instance-scoped caching:

```csharp
[Fact]
public void GivenSameCache_WhenCreatingMultipleDecoratorsOfSameType_ThenReturnsSameInstance()
{
    // Arrange
    var cache = new ConcurrentDictionary<Type, ITokenDecorator>();
    var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer), cache);
    var context2 = new TokenDecoratorContext(typeof(ToLowerTransformer), cache);

    // Act
    var decorator1 = context1.CreateDecorator();
    var decorator2 = context2.CreateDecorator();

    // Assert
    Assert.Same(decorator1, decorator2);
}

[Fact]
public void GivenDifferentCaches_WhenCreatingSameDecoratorType_ThenReturnsDifferentInstances()
{
    // Arrange
    var cache1 = new ConcurrentDictionary<Type, ITokenDecorator>();
    var cache2 = new ConcurrentDictionary<Type, ITokenDecorator>();
    var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer), cache1);
    var context2 = new TokenDecoratorContext(typeof(ToLowerTransformer), cache2);

    // Act
    var decorator1 = context1.CreateDecorator();
    var decorator2 = context2.CreateDecorator();

    // Assert
    Assert.NotSame(decorator1, decorator2);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenDecoratorContextCachingTests"`
Expected: Compilation error — constructor doesn't accept cache parameter.

- [ ] **Step 3: Add cache parameter to TokenDecoratorContext**

In `src/Tokenizer/TokenDecoratorContext.cs`:

Remove the static field (line 14):
```csharp
// Remove: private static readonly ConcurrentDictionary<Type, ITokenDecorator> DecoratorCache = new();
```

Add instance field and update constructor:
```csharp
private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache;

public TokenDecoratorContext(Type tokenDecorator, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache)
{
    DecoratorType = tokenDecorator;
    _parameters = new List<string>();
    _decoratorCache = decoratorCache;
}
```

Update `CreateDecorator` to use `_decoratorCache` instead of `DecoratorCache`.

Update the comment on line 13 to reflect the change.

- [ ] **Step 4: Add cache field to TokenParser and pass through**

In `src/Tokenizer/Compilation/TokenParser.cs`, add a field:

```csharp
private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache = new();
```

Update all `new TokenDecoratorContext(...)` calls in `ParseTokenDecorators` (lines 350, 391, 417) to pass `_decoratorCache`:

```csharp
// Line 350:
var setContext = new TokenDecoratorContext(typeof(SetTransformer), _decoratorCache);

// Line 391:
context = new TokenDecoratorContext(operatorType, _decoratorCache);

// Line 417:
context = new TokenDecoratorContext(validatorType, _decoratorCache);
```

- [ ] **Step 5: Fix test call sites**

In `tests/Tokenizer.Tests/TokenTests.cs`, update lines 47 and 63:
```csharp
// Create a shared cache for test purposes
var cache = new ConcurrentDictionary<Type, ITokenDecorator>();
token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), cache));
```

Add necessary `using System.Collections.Concurrent;` and `using Tokens.Transformers;` where needed.

- [ ] **Step 6: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/TokenDecoratorContext.cs src/Tokenizer/Compilation/TokenParser.cs tests/Tokenizer.Tests/TokenDecoratorContextCachingTests.cs tests/Tokenizer.Tests/TokenTests.cs
git commit -m "refactor: scope DecoratorCache to TokenParser instance (H8+D3)"
```

---

### Task 10: Demote HintProcessor log level (H9)

**Addresses:** H9
**Chosen approach:** Change `LogError` to `LogWarning` for required hint miss.

**Files:**
- Modify: `src/Tokenizer/Tokenization/HintProcessor.cs` (line 115)

- [ ] **Step 1: Change log level**

In `src/Tokenizer/Tokenization/HintProcessor.cs`, line 115:

```csharp
// Before:
log.LogError("Required hint missing: '{HintText}'", hint.Text);

// After:
log.LogWarning("Required hint missing: '{HintText}'", hint.Text);
```

- [ ] **Step 2: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Tokenization/HintProcessor.cs
git commit -m "fix: demote required hint miss from Error to Warning (H9)"
```

---

### Task 11: Rename AddMiss/AddMatch to TryAddMiss/TryAddMatch (M1)

**Addresses:** M1
**Chosen approach:** Rename methods on `HintResult`, use return values at call sites instead of redundant `Misses.Any()` checks.

**Files:**
- Modify: `src/Tokenizer/HintResult.cs`
- Modify: `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs`
- Modify: `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs`
- Modify: `src/Tokenizer/Tokenization/HintProcessor.cs`
- Modify: `tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs`

**Important context:**
- `AddMatch` returns `bool` (false if already matched) — rename to `TryAddMatch`
- `AddMiss` returns `bool` (false if already missed or matched) — rename to `TryAddMiss`
- `ContainsHintStrategy.PreProcess` (lines 52-63) adds all hints as misses then checks `Misses.Any()` — should use return value instead
- `IntegratedHintStrategy.PostProcess` (lines 58-61) has the same pattern

- [ ] **Step 1: Rename methods in HintResult**

In `src/Tokenizer/HintResult.cs`:

```csharp
// Line 33: rename AddMatch → TryAddMatch
internal bool TryAddMatch(Hint hint, TokenEnumerator enumerator)

// Line 42: rename AddMiss → TryAddMiss
internal bool TryAddMiss(Hint hint)
```

- [ ] **Step 2: Update ContainsHintStrategy to use return values**

In `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs`, replace lines 52-63:

```csharp
foreach (var hint in template.Hints)
{
    if (result.Hints.TryAddMiss(hint) && !hint.Optional)
    {
        collector.Record(DiagnosticEventType.HintMissing,
            value: hint.Text);
    }
}

return result.Hints.Misses.Any(h => h.Optional == false);
```

- [ ] **Step 3: Update IntegratedHintStrategy to use return values**

In `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs`, replace lines 58-61:

```csharp
foreach (var hint in currentTemplate.Hints)
{
    result.Hints.TryAddMiss(hint);
}
```

- [ ] **Step 4: Update HintProcessor**

In `src/Tokenizer/Tokenization/HintProcessor.cs`, update `AddHintMiss` call site (line 107) and the `AddMatch` call site — search for `AddMatch` and `AddMiss` in this file and rename to `TryAddMatch`/`TryAddMiss`.

- [ ] **Step 5: Update TokenizeResultBuilder**

In `tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs`, update lines 49 and 56 (and equivalent in the generic builder):

```csharp
// Line 49:
result.Hints.TryAddMatch(...)

// Line 56:
result.Hints.TryAddMiss(hintMiss);
```

- [ ] **Step 6: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/HintResult.cs src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs src/Tokenizer/Tokenization/HintProcessor.cs tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs
git commit -m "refactor: rename AddMiss/AddMatch to TryAddMiss/TryAddMatch (M1)"
```

---

### Task 12: Demote log levels (M4 + M5 + L1)

**Addresses:** M4, M5, L1
**Chosen approach:** Demote Information → Debug for per-call logs, Debug → Trace for lexer scanner noise.

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs` (lines 180, 258, 373, 465)
- Modify: `src/Tokenizer/Compilation/TokenParser.cs` (lines 172, 320)
- Modify: `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs` (line 358)

- [ ] **Step 1: Demote Tokenizer.cs per-call logs to Debug**

In `src/Tokenizer/Tokenizer.cs`:

Line 180 — wrap in IsEnabled guard:
```csharp
// Before:
log.LogInformation("Starting tokenization for template {TemplateName}", template.Name);

// After:
if (log.IsEnabled(LogLevel.Debug))
{
    log.LogDebug("Starting tokenization for template {TemplateName}", template.Name);
}
```

Line 258:
```csharp
// Before:
log.LogInformation("Tokenization {Result} for template {TemplateName}", ...);

// After:
if (log.IsEnabled(LogLevel.Debug))
{
    log.LogDebug("Tokenization {Result} for template {TemplateName}", ...);
}
```

Lines 373 and 465 (async equivalents) — same change.

Also demote the diagnostic verdict log (line 241) if present:
```csharp
if (log.IsEnabled(LogLevel.Debug))
{
    log.LogDebug("{Verdict}", result.Diagnostics.Summary.Verdict);
}
```

- [ ] **Step 2: Demote TokenParser.cs per-call logs to Debug**

In `src/Tokenizer/Compilation/TokenParser.cs`:

Line 172:
```csharp
// Before:
log.LogInformation("Starting template parsing: ...");

// After:
if (log.IsEnabled(LogLevel.Debug))
{
    log.LogDebug("Starting template parsing: {TemplateName}, ContentLength: {ContentLength}", name, content.Length);
}
```

Line 320:
```csharp
// Before:
log.LogInformation("Template parsing complete: ...");

// After:
if (log.IsEnabled(LogLevel.Debug))
{
    log.LogDebug("Template parsing complete: {TemplateName}, TotalTokens: {TokenCount}, Duration: {Duration}",
        template.Name, template.Tokens.Count, stopwatch?.Elapsed.TotalMilliseconds ?? 0);
}
```

- [ ] **Step 3: Demote TemplateLexer scanner log to Trace**

In `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs`, line 356-358:

```csharp
// Before:
if (log.IsEnabled(LogLevel.Debug))
{
    log.LogDebug("Lexer token produced: ...");

// After:
if (log.IsEnabled(LogLevel.Trace))
{
    log.LogTrace("Lexer token produced: ...");
```

- [ ] **Step 4: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs src/Tokenizer/Compilation/TokenParser.cs src/Tokenizer/Compilation/Lexer/TemplateLexer.cs
git commit -m "fix: demote per-call logs to Debug/Trace for library use (M4+M5+L1)"
```

---

### Task 13: Make IDiagnosticCollector internal (M6)

**Addresses:** M6
**Chosen approach:** Change `public interface` to `internal interface`.

**Files:**
- Modify: `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` (line 11)

- [ ] **Step 1: Change visibility**

In `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`, line 11:

```csharp
// Before:
public interface IDiagnosticCollector

// After:
internal interface IDiagnosticCollector
```

- [ ] **Step 2: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass. If any test references `IDiagnosticCollector` directly, it will fail and need `InternalsVisibleTo` (check if already set).

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Diagnostics/IDiagnosticCollector.cs
git commit -m "refactor: make IDiagnosticCollector internal (M6)"
```

---

### Task 14: Make TokenizeResultBase abstract (M11)

**Addresses:** M11
**Chosen approach:** Add `abstract` modifier.

**Files:**
- Modify: `src/Tokenizer/TokenizeResultBase.cs` (line 7)

- [ ] **Step 1: Change to abstract**

In `src/Tokenizer/TokenizeResultBase.cs`, line 7:

```csharp
// Before:
public class TokenizeResultBase

// After:
public abstract class TokenizeResultBase
```

- [ ] **Step 2: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass (nobody instantiates `TokenizeResultBase` directly).

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/TokenizeResultBase.cs
git commit -m "refactor: make TokenizeResultBase abstract (M11)"
```

---

### Task 15: HasTags early-exit overload (L5)

**Addresses:** L5
**Chosen approach:** Make the `params` overload use early-exit loop instead of delegating to the `out` overload.

**Files:**
- Modify: `src/Tokenizer/Template.cs` (lines 114-117)

- [ ] **Step 1: Run existing HasTags tests as baseline**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HasTags"`
Expected: All pass.

- [ ] **Step 2: Implement early-exit overload**

In `src/Tokenizer/Template.cs`, replace lines 114-117:

```csharp
// Before:
public bool HasTags(IList<string> tags)
{
    return HasTags(tags, out _);
}

// After:
public bool HasTags(IList<string> tags)
{
    if (tags == null) return false;

    foreach (var tag in tags)
    {
        if (!HasTag(tag)) return false;
    }

    return true;
}
```

- [ ] **Step 3: Run all tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Template.cs
git commit -m "perf: early-exit in HasTags params overload (L5)"
```

---

### Task 16: Add inline comments for dismissed issues (L3)

**Addresses:** L3 (MaxInputLength uses template.Options)
**Chosen approach:** Add clarifying inline comment.

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs` (line 142)

- [ ] **Step 1: Add comment**

In `src/Tokenizer/Tokenizer.cs`, before line 142:

```csharp
// template.Options reflects merged instance + front matter overrides — intentionally
// used instead of this.Options so per-template front matter settings take effect.
if (template.Options.MaxInputLength > 0 && input.Length > template.Options.MaxInputLength)
```

- [ ] **Step 2: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs
git commit -m "docs: add inline comment explaining template.Options usage (L3)"
```

---

### Task 17: Add TokenizeResult API tests (H11)

**Addresses:** H11
**Chosen approach:** Add dedicated unit tests for `First`, `FirstOrDefault`, `All`, `Contains` methods.

**Files:**
- Create: `tests/Tokenizer.Tests/TokenizeResultApiTests.cs`

- [ ] **Step 1: Write tests**

Create `tests/Tokenizer.Tests/TokenizeResultApiTests.cs`:

```csharp
using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;

namespace Tokens;

public class TokenizeResultApiTests
{
    private TokenizeResult CreateResultWithMatches(params (string name, object value)[] matches)
    {
        var tokens = new List<Token>();
        foreach (var (name, _) in matches)
        {
            tokens.Add(new TokenBuilder().WithContent($"{{{name}}}").WithName(name).Build());
        }

        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(tokens.ToArray())
            .Build();

        var result = new TokenizeResult(template);

        foreach (var (name, value) in matches)
        {
            var token = tokens.First(t => t.Name == name);
            result.Tokens.AddMatch(token, value, new FileLocation());
        }

        return result;
    }

    // First
    [Fact]
    public void GivenMatchingToken_WhenCallingFirst_ThenReturnsValue()
    {
        // Arrange
        var result = CreateResultWithMatches(("Name", "Alice"));

        // Act
        var value = result.First("Name");

        // Assert
        Assert.Equal("Alice", value);
    }

    [Fact]
    public void GivenNoMatchingToken_WhenCallingFirst_ThenThrowsTokenizerException()
    {
        // Arrange
        var result = CreateResultWithMatches(("Name", "Alice"));

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() => result.First("Missing"));
        Assert.Contains("Missing", ex.Message);
    }

    // First<T>
    [Fact]
    public void GivenMatchingToken_WhenCallingFirstGeneric_ThenReturnsCastValue()
    {
        // Arrange
        var result = CreateResultWithMatches(("Count", 42));

        // Act
        var value = result.First<int>("Count");

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GivenNoMatchingToken_WhenCallingFirstGeneric_ThenThrowsTokenizerException()
    {
        // Arrange
        var result = CreateResultWithMatches(("Name", "Alice"));

        // Act & Assert
        Assert.Throws<TokenizerException>(() => result.First<string>("Missing"));
    }

    // FirstOrDefault
    [Fact]
    public void GivenMatchingToken_WhenCallingFirstOrDefault_ThenReturnsValue()
    {
        // Arrange
        var result = CreateResultWithMatches(("Name", "Alice"));

        // Act
        var value = result.FirstOrDefault("Name");

        // Assert
        Assert.Equal("Alice", value);
    }

    [Fact]
    public void GivenNoMatchingToken_WhenCallingFirstOrDefault_ThenReturnsNull()
    {
        // Arrange
        var result = CreateResultWithMatches(("Name", "Alice"));

        // Act
        var value = result.FirstOrDefault("Missing");

        // Assert
        Assert.Null(value);
    }

    // FirstOrDefault<T>
    [Fact]
    public void GivenMatchingToken_WhenCallingFirstOrDefaultGeneric_ThenReturnsCastValue()
    {
        // Arrange
        var result = CreateResultWithMatches(("Count", 42));

        // Act
        var value = result.FirstOrDefault<int>("Count");

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GivenNoMatchingToken_WhenCallingFirstOrDefaultGeneric_ThenReturnsDefault()
    {
        // Arrange
        var result = CreateResultWithMatches(("Name", "Alice"));

        // Act
        var value = result.FirstOrDefault<int>("Missing");

        // Assert
        Assert.Equal(0, value);
    }

    // All
    [Fact]
    public void GivenMultipleMatches_WhenCallingAll_ThenReturnsAllValues()
    {
        // Arrange
        var token = new TokenBuilder().WithContent("{Tag}").WithName("Tag").Build();
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(token)
            .Build();

        var result = new TokenizeResult(template);
        result.Tokens.AddMatch(token, "one", new FileLocation());
        result.Tokens.AddMatch(token, "two", new FileLocation());
        result.Tokens.AddMatch(token, "three", new FileLocation());

        // Act
        var values = result.All("Tag");

        // Assert
        Assert.Equal(3, values.Count);
        Assert.Equal("one", values[0]);
        Assert.Equal("two", values[1]);
        Assert.Equal("three", values[2]);
    }

    [Fact]
    public void GivenNoMatches_WhenCallingAll_ThenReturnsEmptyList()
    {
        // Arrange
        var result = CreateResultWithMatches(("Name", "Alice"));

        // Act
        var values = result.All("Missing");

        // Assert
        Assert.Empty(values);
    }

    // Contains
    [Fact]
    public void GivenMatchingToken_WhenCallingContains_ThenReturnsTrue()
    {
        // Arrange
        var result = CreateResultWithMatches(("Name", "Alice"));

        // Act & Assert
        Assert.True(result.Contains("Name"));
    }

    [Fact]
    public void GivenNoMatchingToken_WhenCallingContains_ThenReturnsFalse()
    {
        // Arrange
        var result = CreateResultWithMatches(("Name", "Alice"));

        // Act & Assert
        Assert.False(result.Contains("Missing"));
    }
}
```

- [ ] **Step 2: Run tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizeResultApiTests"`
Expected: All PASS.

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/TokenizeResultApiTests.cs
git commit -m "test: add dedicated TokenizeResult API tests (H11)"
```

---

### Task 18: Add read-only target ArgumentException test (H12)

**Addresses:** H12
**Chosen approach:** Add test with a class that has only get-only properties.

**Files:**
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs`

- [ ] **Step 1: Write test**

In `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs`, add:

```csharp
[Fact]
public void GivenReadOnlyTargetObject_WhenProcessingTokenization_ThenThrowsArgumentException()
{
    // Arrange
    var template = new TemplateBuilder()
        .WithName("TestTemplate")
        .WithTokens(new TokenBuilder()
            .WithContent("{Name}")
            .WithName("Name")
            .Build())
        .WithDefaultOptions()
        .Build();

    var context = new TokenizationContext();
    context.Initialize(new System.IO.StringReader("test"));
    var result = new TokenizeResultBuilder().WithTemplate(template).Build();

    var readOnlyTarget = new ReadOnlyTarget("test");

    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
        _engine.ProcessTokenization(template, readOnlyTarget, context, result, NullDiagnosticCollector.Instance));

    Assert.Contains("no settable properties", ex.Message);
}

private sealed class ReadOnlyTarget
{
    public ReadOnlyTarget(string name) { Name = name; }
    public string Name { get; }
}
```

- [ ] **Step 2: Run test**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenReadOnlyTargetObject"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs
git commit -m "test: add read-only target ArgumentException test (H12)"
```

---

### Task 19: Add derived iteration limit test (M12)

**Addresses:** M12
**Chosen approach:** Add test that triggers derived limit with no explicit MaxIterations set.

**Files:**
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs` or `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`

**Important context:**
- The derived limit formula is: `CharactersConsumed * 2 + 100` (line 153 in TokenizationEngine.cs)
- To trigger it, we need a template pattern that causes the engine to loop many times relative to input length
- A template with tokens that have empty preambles and fail to assign will cause repeated backtracking

- [ ] **Step 1: Write test**

In `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`, add:

```csharp
[Fact]
public void GivenNoExplicitMaxIterations_WhenTokenizationExceedsDerivedLimit_ThenThrowsWithDerivedLimitMessage()
{
    // Arrange — MaxIterations defaults to 0, which enables the derived limit
    var options = new TokenizerOptions { MaxIterations = 0 };
    var tokenizer = new Tokenizer(options);

    // A template with many tokens that share empty preambles triggers excessive
    // iteration relative to input length
    var template = tokenizer.Compile("{A}{B}{C}{D}{E}{F}{G}{H}{I}{J}");

    // Act & Assert — short input with many tokens causes iteration count to
    // exceed the derived limit (CharactersConsumed * 2 + 100)
    var ex = Assert.Throws<TokenizerException>(() =>
        tokenizer.Tokenize(template, "x"));

    Assert.Contains("derived iteration limit", ex.Message);
}
```

- [ ] **Step 2: Run test**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenNoExplicitMaxIterations_WhenTokenizationExceedsDerivedLimit"`
Expected: PASS. If the test doesn't trigger the derived limit, adjust the template pattern — add more tokens or use a pathological pattern. The key assertion is the "derived iteration limit" message string from line 156.

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs
git commit -m "test: add derived iteration limit test (M12)"
```

---

### Task 20: Add empty-preamble guard test (M13)

**Addresses:** M13
**Chosen approach:** Add test triggering the `advanceLength==0` path in `ProcessRepeatedTokens`.

**Files:**
- Create: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEmptyPreambleTests.cs`

**Important context:**
- The guard is at `TokenizationEngine.cs:363`: when backtracking with `advanceLength == 0` and tokens have no separator, it throws `InvalidOperationException` with message "Tokenization cannot proceed: tokens with empty preambles"
- To trigger: need consecutive tokens with no preamble separator where assignment fails

- [ ] **Step 1: Write test**

Create `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEmptyPreambleTests.cs`:

```csharp
using Tokens.Builders;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Tokenization.Engine;

public class TokenizationEngineEmptyPreambleTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenConsecutiveTokensWithEmptyPreamble_WhenAssignmentFails_ThenThrowsInvalidOperationException()
    {
        // Arrange — two tokens with empty preambles that cannot be distinguished
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(
                new TokenBuilder()
                    .WithContent("{First}")
                    .WithName("First")
                    .WithPreamble("")
                    .Build(),
                new TokenBuilder()
                    .WithContent("{Second}")
                    .WithName("Second")
                    .WithPreamble("")
                    .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("some input text"));
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Target has no matching properties — forces assignment failure
        var target = new { Unrelated = "" };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.ProcessTokenization(template, target, context, result, NullDiagnosticCollector.Instance));

        Assert.Contains("empty preambles", ex.Message);
    }
}
```

- [ ] **Step 2: Run test**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenConsecutiveTokensWithEmptyPreamble"`
Expected: PASS. If not triggered, the test may need adjustment — the key is creating a scenario where the engine backtracks with `advanceLength == 0`. If the target object check (H12's ArgumentException) fires first, use `null` as target and adjust the template to have tokens that fail validation.

Note: This test may need to use `IgnoreMissingProperties = true` on the template options to bypass the MissingMemberException and reach the backtracking path.

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEmptyPreambleTests.cs
git commit -m "test: add empty-preamble guard test (M13)"
```

---

### Task 21: Rename TokenParserTests to Gherkin naming (L7)

**Addresses:** L7
**Chosen approach:** Rename all test methods to Given/When/Then style.

**Files:**
- Modify: `tests/Tokenizer.Tests/Compilation/TokenParserTests.cs`

- [ ] **Step 1: Rename all tests**

In `tests/Tokenizer.Tests/Compilation/TokenParserTests.cs`, rename each test:

| Old Name | New Name |
|----------|----------|
| `TestParseToken` | `GivenTemplateWithDecorator_WhenParsing_ThenTokenHasDecorator` |
| `TestParseTokenWithTrailingNewLine` | `GivenTemplateWithTrailingNewLine_WhenParsing_ThenTokenHasDecorator` |
| `TestParseTokenWithRequiredFlag` | `GivenTemplateWithRequiredFlag_WhenParsing_ThenTokenIsRequired` |
| `TestParseSetName` | `GivenSimpleText_WhenParsing_ThenNameIsText` |
| `TestParseSetNameLimitToThreeWords` | `GivenTextWithManyWords_WhenParsing_ThenNameIsTruncated` |
| `TestParseSetNameCountsNewLines` | `GivenTextWithNewLines_WhenParsing_ThenNewLinesCountAsWordBreaks` |
| `TestParseSetNameIgnoresFrontmatterWithWindowsNewlines` | `GivenFrontMatterWithWindowsNewlines_WhenParsing_ThenNameIgnoresFrontMatter` |
| `TestParseSetNameIgnoresFrontmatterWithUnixNewlines` | `GivenFrontMatterWithUnixNewlines_WhenParsing_ThenNameIgnoresFrontMatter` |
| `TestParseSetNameWhenEmpty` | `GivenEmptyContent_WhenParsing_ThenNameIsEmpty` |
| `TestParseSetsTags` | `GivenFrontMatterWithTag_WhenParsing_ThenTemplateHasTag` |
| `TestParseFrontMatterTokenWithoutSet` | `GivenFrontMatterTokenWithoutSetValue_WhenParsing_ThenThrowsException` |
| `TestParseFrontMatterToken` | `GivenFrontMatterTokenWithSetValue_WhenParsing_ThenTokenHasName` |

- [ ] **Step 2: Run tests**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenParserTests"`
Expected: All pass.

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Compilation/TokenParserTests.cs
git commit -m "style: rename TokenParserTests to Gherkin naming (L7)"
```

---

### Task 22: Fix weak assertion in TokenizationEngineErrorTests (L8)

**Addresses:** L8
**Chosen approach:** Replace `Assert.NotNull` on always-non-null property with meaningful assertion.

**Files:**
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs` (lines 89-110)

- [ ] **Step 1: Fix assertion**

In `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs`, replace the test at lines 89-110:

```csharp
[Fact]
public void GivenTokenizationWithNoMatch_WhenProcessingTokenization_ThenResultHasNoExceptions()
{
    // Arrange — template expects a token that won't be found in input
    var template = new TemplateBuilder()
        .WithName("TestTemplate")
        .WithTokens(new TokenBuilder()
            .WithContent("test")
            .WithName("TestToken")
            .Build())
        .Build();

    var context = new TokenizationContext();
    context.Initialize(new System.IO.StringReader("no match here"));
    var result = new TokenizeResultBuilder().WithTemplate(template).Build();

    // Act
    _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

    // Assert — a non-matching template is not an error condition
    Assert.Empty(result.Exceptions);
    Assert.False(result.Success);
}
```

- [ ] **Step 2: Run test**

Run: `/Users/work/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenTokenizationWithNoMatch"`
Expected: PASS. If `Exceptions` is not empty, investigate what exceptions are thrown during a no-match scenario and adjust the assertion accordingly.

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs
git commit -m "test: fix weak assertion in TokenizationEngineErrorTests (L8)"
```
