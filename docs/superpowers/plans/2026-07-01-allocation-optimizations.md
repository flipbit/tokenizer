# Allocation Optimizations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce heap allocations on the tokenization hot path through three targeted optimizations: char-based TokenEnumerator with Span matching, index-based ObjectExtensions path navigation, and log-level guards on diagnostic string operations.

**Architecture:** Each optimization is independent and can be implemented/tested in isolation. The TokenEnumerator change has the largest blast radius (4 files) but all callers are internal. ObjectExtensions is self-contained. Log guards are mechanical wrapping.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 8.0 + .NET 10.0, xUnit, BenchmarkDotNet

---

## Task 1: TokenEnumerator — char-based Next/Peek

**Files:**
- Modify: `src/Tokenizer/Enumerators/TokenEnumerator.cs`
- Modify: `src/Tokenizer/Enumerators/FileLocation.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`

- [ ] **Step 1: Write tests for char-based Next/Peek behavior**

```csharp
// tests/Tokenizer.Tests/Enumerators/TokenEnumeratorCharTests.cs
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Enumerators;

public class TokenEnumeratorCharTests
{
    [Fact]
    public void GivenNonEmptyInput_WhenPeek_ThenReturnsCharNotString()
    {
        // Arrange
        var enumerator = new TokenEnumerator("hello");

        // Act
        char result = enumerator.Peek();

        // Assert
        Assert.Equal('h', result);
    }

    [Fact]
    public void GivenNonEmptyInput_WhenNext_ThenReturnsCharAndAdvances()
    {
        // Arrange
        var enumerator = new TokenEnumerator("hi");

        // Act
        char first = enumerator.Next();
        char second = enumerator.Next();

        // Assert
        Assert.Equal('h', first);
        Assert.Equal('i', second);
    }

    [Fact]
    public void GivenEmptyInput_WhenPeek_ThenReturnsNullChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("");

        // Act
        char result = enumerator.Peek();

        // Assert
        Assert.Equal('\0', result);
    }

    [Fact]
    public void GivenEmptyInput_WhenNext_ThenReturnsNullChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("");

        // Act
        char result = enumerator.Next();

        // Assert
        Assert.Equal('\0', result);
    }

    [Fact]
    public void GivenExhaustedInput_WhenPeek_ThenReturnsNullChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("a");
        enumerator.Next();

        // Act
        char result = enumerator.Peek();

        // Assert
        Assert.Equal('\0', result);
    }

    [Fact]
    public void GivenInput_WhenPeekWithOffset_ThenReturnsCorrectChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("abc");

        // Act & Assert
        Assert.Equal('a', enumerator.Peek(0));
        Assert.Equal('b', enumerator.Peek(1));
        Assert.Equal('c', enumerator.Peek(2));
        Assert.Equal('\0', enumerator.Peek(3));
    }

    [Fact]
    public void GivenNewlineInInput_WhenNext_ThenLocationTracksCorrectly()
    {
        // Arrange
        var enumerator = new TokenEnumerator("a\nb");

        // Act
        enumerator.Next(); // 'a'
        enumerator.Next(); // '\n'
        enumerator.Next(); // 'b'

        // Assert
        Assert.Equal(2, enumerator.Location.Line);
    }

    [Fact]
    public void GivenInput_WhenMatch_ThenMatchesPreambleCorrectly()
    {
        // Arrange
        var enumerator = new TokenEnumerator("Name: Alice");

        // Act & Assert
        Assert.True(enumerator.Match("Name: "));
        Assert.False(enumerator.Match("Age: "));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `~/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenEnumeratorCharTests"`
Expected: FAIL — `Next()` and `Peek()` currently return `string`, not `char`

- [ ] **Step 3: Change `FileLocation.Increment` to accept `char`**

In `src/Tokenizer/Enumerators/FileLocation.cs`, find the `Increment` method and change:

```csharp
// Before:
public void Increment(string value)
{
    if (value == "\r") return;
    if (value == "\n") return;
    // ...
}

// After:
public void Increment(char value)
{
    if (value == '\r') return;
    if (value == '\n') return;
    Column++;
}
```

- [ ] **Step 4: Change `TokenEnumerator` to return `char`**

In `src/Tokenizer/Enumerators/TokenEnumerator.cs`:

```csharp
public char Next()
{
    if (IsEmpty) return '\0';

    var next = pattern[currentLocation];
    currentLocation++;

    if (resetNextLine)
    {
        Location.NewLine();
        resetNextLine = false;
    }
    else
    {
        Location.Increment(next);
    }

    if (next == '\n')
    {
        resetNextLine = true;
    }

    return next;
}

public char Peek()
{
    if (IsEmpty) return '\0';

    return pattern[currentLocation];
}

public char Peek(int offset)
{
    if (IsEmpty) return '\0';

    var location = currentLocation + offset;

    if (location >= patternLength) return '\0';

    return pattern[currentLocation + offset];
}
```

- [ ] **Step 5: Update `TokenizationEngine.cs` callers**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`, update the 4 call sites:

Line ~125 (`Peek` return used as `next`):
```csharp
// The variable `next` is now char. Update comparisons:
var next = context.Enumerator.Peek();
```

Line ~583 in `HandleWindowsNewlines`:
```csharp
// Before:
private string HandleWindowsNewlines(TokenEnumerator enumerator, string next)
{
    if (next == "\r" && enumerator.Peek(1) == "\n")
    {
        // ...
        return "\n";
    }
    return next;
}

// After:
private char HandleWindowsNewlines(TokenEnumerator enumerator, char next)
{
    if (next == '\r' && enumerator.Peek(1) == '\n')
    {
        log.LogTrace("Normalizing Windows line ending (CRLF) to Unix (LF) at position Line {Line}, Column {Column}",
            enumerator.Location.Line, enumerator.Location.Column);
        enumerator.Next();
        return '\n';
    }
    return next;
}
```

Line ~625 in `ShouldProcessNewlineTerminatedToken`:
```csharp
// Before:
private bool ShouldProcessNewlineTerminatedToken(ITokenizationContext context, string next)
{
    return context.Candidates.Any && context.Candidates.TerminateOnNewLine && next == "\n";
}

// After:
private bool ShouldProcessNewlineTerminatedToken(ITokenizationContext context, char next)
{
    return context.Candidates.Any && context.Candidates.TerminateOnNewLine && next == '\n';
}
```

Line ~705 in `HandleNoTokenMatch`:
```csharp
// Before:
private void HandleNoTokenMatch(ITokenizationContext context, string next)

// After:
private void HandleNoTokenMatch(ITokenizationContext context, char next)
```

`context.Replacement.Append(next)` — `StringBuilder.Append(char)` already exists, no change needed to the call itself.

- [ ] **Step 6: Run all tests**

Run: `~/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Enumerators/TokenEnumerator.cs src/Tokenizer/Enumerators/FileLocation.cs src/Tokenizer/Tokenization/TokenizationEngine.cs tests/Tokenizer.Tests/Enumerators/TokenEnumeratorCharTests.cs
git commit -m "Refactor TokenEnumerator to return char instead of string"
```

---

## Task 2: TokenEnumerator — Span-based Match()

**Files:**
- Modify: `src/Tokenizer/Enumerators/TokenEnumerator.cs`

- [ ] **Step 1: Write a test verifying Match still works correctly**

```csharp
// tests/Tokenizer.Tests/Enumerators/TokenEnumeratorMatchTests.cs
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Enumerators;

public class TokenEnumeratorMatchTests
{
    [Fact]
    public void GivenMatchingPreamble_WhenMatch_ThenReturnsTrue()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        Assert.True(enumerator.Match("Name: "));
    }

    [Fact]
    public void GivenNonMatchingPreamble_WhenMatch_ThenReturnsFalse()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        Assert.False(enumerator.Match("Age: "));
    }

    [Fact]
    public void GivenEmptyValue_WhenMatch_ThenReturnsTrue()
    {
        var enumerator = new TokenEnumerator("anything");
        Assert.True(enumerator.Match(""));
        Assert.True(enumerator.Match(null));
    }

    [Fact]
    public void GivenValueLongerThanRemaining_WhenMatch_ThenReturnsFalse()
    {
        var enumerator = new TokenEnumerator("hi");
        Assert.False(enumerator.Match("hello"));
    }

    [Fact]
    public void GivenAdvancedPosition_WhenMatch_ThenMatchesFromCurrentPosition()
    {
        // Arrange
        var enumerator = new TokenEnumerator("Name: Alice");
        enumerator.Advance(6); // skip past "Name: "

        // Act & Assert
        Assert.True(enumerator.Match("Alice"));
        Assert.False(enumerator.Match("Name"));
    }

    [Fact]
    public void GivenCaseSensitiveInput_WhenMatch_ThenIsCaseSensitive()
    {
        var enumerator = new TokenEnumerator("Name: Alice");
        Assert.True(enumerator.Match("Name"));
        Assert.False(enumerator.Match("name"));
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (baseline)**

Run: `~/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenEnumeratorMatchTests"`
Expected: PASS (these verify existing behavior before we change the implementation)

- [ ] **Step 3: Replace Match implementation with zero-allocation version**

In `src/Tokenizer/Enumerators/TokenEnumerator.cs`, replace the `Match(string value)` method:

```csharp
public bool Match(string value)
{
    if (string.IsNullOrEmpty(value)) return true;
    if (currentLocation + value.Length > patternLength) return false;

#if NET8_0_OR_GREATER
    return pattern.AsSpan(currentLocation, value.Length).SequenceEqual(value.AsSpan());
#else
    return string.CompareOrdinal(pattern, currentLocation, value, 0, value.Length) == 0;
#endif
}
```

- [ ] **Step 4: Run all tests**

Run: `~/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Enumerators/TokenEnumerator.cs tests/Tokenizer.Tests/Enumerators/TokenEnumeratorMatchTests.cs
git commit -m "Use Span-based matching in TokenEnumerator.Match()"
```

---

## Task 3: ObjectExtensions — index-based path navigation

**Files:**
- Modify: `src/Tokenizer/Extensions/ObjectExtensions.cs`

- [ ] **Step 1: Write tests verifying path navigation behavior is preserved**

```csharp
// tests/Tokenizer.Tests/Extensions/ObjectExtensionsPathTests.cs
using Tokens.Extensions;
using Xunit;

namespace Tokens.Extensions;

public class ObjectExtensionsPathTests
{
    [Fact]
    public void GivenFlatProperty_WhenSetValue_ThenSetsCorrectly()
    {
        var target = new TestTarget();
        target.SetValue("Name", "Alice");
        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void GivenTypePrefixedPath_WhenSetValue_ThenStripsTypeAndSets()
    {
        var target = new TestTarget();
        target.SetValue("TestTarget.Name", "Bob");
        Assert.Equal("Bob", target.Name);
    }

    [Fact]
    public void GivenNestedPath_WhenSetValue_ThenCreatesIntermediateAndSets()
    {
        var target = new TestTarget();
        target.SetValue("Inner.Value", "deep");
        Assert.NotNull(target.Inner);
        Assert.Equal("deep", target.Inner!.Value);
    }

    [Fact]
    public void GivenDeeplyNestedPath_WhenSetValue_ThenCreatesAllIntermediates()
    {
        var target = new TestTarget();
        target.SetValue("Inner.Nested.Name", "three-deep");
        Assert.NotNull(target.Inner);
        Assert.NotNull(target.Inner!.Nested);
        Assert.Equal("three-deep", target.Inner.Nested!.Name);
    }

    [Fact]
    public void GivenFlatProperty_WhenGetValue_ThenReturnsCorrectly()
    {
        var target = new TestTarget { Name = "Alice" };
        var result = target.GetValue<string>("Name");
        Assert.Equal("Alice", result);
    }

    [Fact]
    public void GivenTypePrefixedPath_WhenGetValue_ThenStripsTypeAndGets()
    {
        var target = new TestTarget { Name = "Bob" };
        var result = target.GetValue<string>("TestTarget.Name");
        Assert.Equal("Bob", result);
    }

    [Fact]
    public void GivenNestedPath_WhenGetValue_ThenTraversesAndGets()
    {
        var target = new TestTarget { Inner = new TestInner { Value = "deep" } };
        var result = target.GetValue<string>("Inner.Value");
        Assert.Equal("deep", result);
    }

    public class TestTarget
    {
        public string? Name { get; set; }
        public TestInner? Inner { get; set; }
    }

    public class TestInner
    {
        public string? Value { get; set; }
        public TestNested? Nested { get; set; }
    }

    public class TestNested
    {
        public string? Name { get; set; }
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (baseline)**

Run: `~/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~ObjectExtensionsPathTests"`
Expected: PASS

- [ ] **Step 3: Refactor SetValue/SetInnerValue to use index-based navigation**

In `src/Tokenizer/Extensions/ObjectExtensions.cs`:

Replace `SetValue<T>`:
```csharp
public static T SetValue<T>(this T @object, string propertyPath, object value, StringComparison stringComparison) where T : class
{
    if (string.IsNullOrEmpty(propertyPath))
    {
        throw new ArgumentNullException(nameof(propertyPath));
    }

    var segments = propertyPath.Split('.');
    var objectType = @object.GetType().Name;

    var depth = string.Equals(objectType, segments[0], stringComparison) ? 1 : 0;

    @object = (T)SetInnerValue(@object, segments, depth, value, stringComparison);

    return @object;
}
```

Replace `SetInnerValue` signature and body — change `IReadOnlyList<string> path` to `string[] segments, int depth`. Replace all `path[0]` with `segments[depth]`, `path.Count == 1` with `depth == segments.Length - 1`, and the recursive call from `path.Skip(1).ToArray()` to `segments, depth + 1`:

```csharp
private static object SetInnerValue(object @object, string[] segments, int depth, object value, StringComparison stringComparison)
{
    var set = false;
    var propertyInfos = GetCachedProperties(@object.GetType());

    foreach (var propertyInfo in propertyInfos)
    {
        if (!string.Equals(propertyInfo.Name, segments[depth], stringComparison)) continue;

        set = true;

        System.Diagnostics.Debug.WriteLine(
            $"[SetValue] Attempting to set property '{propertyInfo.Name}' on type '{@object.GetType().Name}'. " +
            $"CanWrite: {propertyInfo.CanWrite}, HasSetter: {propertyInfo.GetSetMethod() != null}, " +
            $"PropertyType: {propertyInfo.PropertyType.Name}, ValueType: {value?.GetType().Name ?? "null"}");

        if (depth == segments.Length - 1)
        {
            // ... (entire leaf-assignment block stays the same, just replace path[0] references)
            // This is the existing code for handling IList<>, Nullable<>, enums, etc.
            // No changes to the logic — only the condition and variable names change.
```

The leaf-assignment block (lines 69-151 in the original) stays identical except the condition. The recursive call at line 177 changes:

```csharp
// Before:
SetInnerValue(currentValue, path.Skip(1).ToArray(), value, stringComparison);

// After:
SetInnerValue(currentValue, segments, depth + 1, value, stringComparison);
```

- [ ] **Step 4: Refactor GetValue/GetInnerValue the same way**

Replace `GetValue<T>`:
```csharp
public static T? GetValue<T>(this object target, string propertyPath, StringComparison stringComparison)
{
    if (string.IsNullOrEmpty(propertyPath))
    {
        throw new ArgumentNullException(nameof(propertyPath));
    }

    var segments = propertyPath.Split('.');
    var objectType = target.GetType().Name;

    var depth = string.Equals(objectType, segments[0], stringComparison) ? 1 : 0;

    return GetInnerValue<T>(target, segments, depth, stringComparison);
}
```

Replace `GetInnerValue<T>`:
```csharp
private static T? GetInnerValue<T>(object @object, string[] segments, int depth, StringComparison stringComparison)
{
    var propertyInfos = GetCachedProperties(@object.GetType());

    foreach (var propertyInfo in propertyInfos)
    {
        if (!string.Equals(propertyInfo.Name, segments[depth], stringComparison)) continue;

        if (depth == segments.Length - 1)
        {
            var value = propertyInfo.GetValue(@object);

            return value == null ? default : (T)value;
        }

        var currentValue = propertyInfo.GetValue(@object);

        if (currentValue == null)
        {
            return default;
        }

        return GetInnerValue<T>(currentValue, segments, depth + 1, stringComparison);
    }

    throw new MissingMemberException($@"Could find property '{segments[depth]}' on {@object.GetType().Name}");
}
```

- [ ] **Step 5: Run all tests**

Run: `~/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Extensions/ObjectExtensions.cs tests/Tokenizer.Tests/Extensions/ObjectExtensionsPathTests.cs
git commit -m "Use index-based path navigation in ObjectExtensions"
```

---

## Task 4: TokenizationEngine — log-level guards

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`

- [ ] **Step 1: Run all tests to confirm baseline**

Run: `~/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 2: Add log-level guards around expensive string-building in ProcessTokenization**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`, find the token match logging at line ~149:

```csharp
// Before:
log.LogTrace
(
    "Token match found at Line {Line}, Column {Column}. Matched {MatchCount} token(s): {TokenNames}",
    context.Enumerator.Location.Line,
    context.Enumerator.Location.Column,
    matches.Count,
    string.Join(", ", matches.Select(m => m.Name))
);

// After:
if (log.IsEnabled(LogLevel.Trace))
{
    log.LogTrace
    (
        "Token match found at Line {Line}, Column {Column}. Matched {MatchCount} token(s): {TokenNames}",
        context.Enumerator.Location.Line,
        context.Enumerator.Location.Column,
        matches.Count,
        string.Join(", ", matches.Select(m => m.Name))
    );
}
```

- [ ] **Step 3: Add log-level guards in TryAssignCandidateTokens**

Find line ~256 (assignment attempt logging):
```csharp
// Before:
log.LogTrace("Attempting to assign {CandidateCount} candidate token(s) with value '{ReplacementValue}' at Line {Line}, Column {Column}",
    candidates.Tokens.Count, replacement.ToString(), location.Line, location.Column);

// After:
if (log.IsEnabled(LogLevel.Trace))
{
    log.LogTrace("Attempting to assign {CandidateCount} candidate token(s) with value '{ReplacementValue}' at Line {Line}, Column {Column}",
        candidates.Tokens.Count, replacement.ToString(), location.Line, location.Column);
}
```

Find line ~259 (DiagnosticCollector with string.Join):
```csharp
// Before:
collector.Record(DiagnosticEventType.TokenAssignmentAttempted,
    tokenName: string.Join(", ", candidates.Tokens.Select(t => t.Name)),
    location: location,
    value: replacement.ToString());

// After — guard the string.Join but keep the collector call:
collector.Record(DiagnosticEventType.TokenAssignmentAttempted,
    tokenName: string.Join(", ", candidates.Tokens.Select(t => t.Name)),
    location: location,
    value: replacement.ToString());
```
Note: DiagnosticCollector calls are only active when `EnableDiagnostics` is true. The collector itself guards internally. Leave these as-is.

Find line ~291 (assignment failure logging with string.Join):
```csharp
// Before:
collector.Record(DiagnosticEventType.TokenAssignmentFailed,
    tokenName: string.Join(", ", candidates.Tokens.Select(t => t.Name)),
    ...);

foreach (var token in candidates.Tokens)
{
    log.LogTrace("Ln: {Line} Col: {Column} : Skipping {TokenName} ({TokenId}), '{Replacement}' is not a match.",
        location.Line, location.Column, token.Name, token.Id, replacement.ToString());
}

// After — guard the per-token logging:
// (leave collector as-is)

if (log.IsEnabled(LogLevel.Trace))
{
    foreach (var token in candidates.Tokens)
    {
        log.LogTrace("Ln: {Line} Col: {Column} : Skipping {TokenName} ({TokenId}), '{Replacement}' is not a match.",
            location.Line, location.Column, token.Name, token.Id, replacement.ToString());
    }
}
```

- [ ] **Step 4: Add log-level guards in ProcessRepeatedTokens**

Find line ~387 (replacement.ToString() in logging):
```csharp
// Before:
log.LogTrace("Checking if any of {CandidateCount} candidate(s) can assign replacement value '{ReplacementValue}'",
    candidates.Tokens.Count, replacement.ToString());

// After:
if (log.IsEnabled(LogLevel.Trace))
{
    log.LogTrace("Checking if any of {CandidateCount} candidate(s) can assign replacement value '{ReplacementValue}'",
        candidates.Tokens.Count, replacement.ToString());
}
```

Find line ~397 (backtrack string.Join):
```csharp
// Before:
collector.Record(DiagnosticEventType.BacktrackStarted,
    tokenName: string.Join(", ", candidates.Tokens.Select(t => t.Name)),
    location: enumerator.Location,
    value: replacement.ToString());

// After — leave collector as-is (guarded internally)
```

Find the per-token loop at lines ~426-454 with multiple `replacement.ToString()` calls:
```csharp
// Wrap the trace logs inside the loop:
if (log.IsEnabled(LogLevel.Trace))
{
    log.LogTrace("Ln: {Line} Col: {Column} : Skipping {TokenName} ({TokenId}), '{Replacement}' is not a match.",
        enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
}
```
Apply this pattern to each `log.LogTrace` inside the loop that calls `replacement.ToString()`.

- [ ] **Step 5: Drop unnecessary .ToList() in ProcessFrontMatterTokens**

Find line ~331:
```csharp
// Before:
var frontMatterTokens = template.Tokens.Where(t => t.IsFrontMatterToken).ToList();

// After:
var frontMatterTokens = template.Tokens.Where(t => t.IsFrontMatterToken);
```

Update the trace log on the next line to not call `.Count` on the enumerable (it would enumerate it):
```csharp
// Before:
log.LogTrace("Processing {FrontMatterCount} front matter tokens", frontMatterTokens.Count);

// After:
if (log.IsEnabled(LogLevel.Trace))
{
    log.LogTrace("Processing {FrontMatterCount} front matter tokens",
        template.Tokens.Count(t => t.IsFrontMatterToken));
}
```

- [ ] **Step 6: Run all tests**

Run: `~/.dotnet/dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationEngine.cs
git commit -m "Add log-level guards to prevent allocations when logging disabled"
```

---

## Task 5: Run benchmarks and verify improvements

- [ ] **Step 1: Run all benchmarks**

```bash
export DOTNET_ROOT=~/.dotnet && export PATH="$DOTNET_ROOT:$PATH"
dotnet run -c Release --project ./benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -- --filter '*CompilationBenchmarks*'
dotnet run -c Release --project ./benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -- --filter '*TokenizationBenchmarks*'
dotnet run -c Release --project ./benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -- --filter '*MatcherBenchmarks*'
```

- [ ] **Step 2: Compare results against baselines**

Compare the `Allocated` column from the new results against the post-v3 baselines:
- Compilation: ~49.55 KB / 231.36 KB / 782.31 KB
- Tokenization: ~41.13 KB / 215.64 KB / 929.66 KB
- Matcher (50 templates): ~3685 KB

Expect the Tokenization and Matcher allocations to decrease. Compilation allocations should be unchanged (compilation doesn't hit the optimized paths).

- [ ] **Step 3: Commit benchmark results if significant**

```bash
# Copy results to baselines directory if improvements are confirmed
mkdir -p benchmarks/baselines/2026-07-01-post-optimization
cp BenchmarkDotNet.Artifacts/results/*.md benchmarks/baselines/2026-07-01-post-optimization/
git add benchmarks/baselines/2026-07-01-post-optimization/
git commit -m "Add post-optimization benchmark results"
```
