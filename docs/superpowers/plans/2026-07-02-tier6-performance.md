# Tier 6: Performance — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate allocation hotspots, cache expensive operations, and add debugging aids (ToString/IEquatable) across the tokenizer library.

**Architecture:** Static field caching for regex and reflection. Char-indexing instead of substring allocations. Manual IEquatable on value-like classes. Compact ToString overrides on all key types.

**Tech Stack:** C# targeting netstandard2.0 / net8.0 / net10.0, xUnit for tests.

**Spec deviation:** The design spec proposed converting `FileLocation` to a record. This is not feasible — `FileLocation` is used as a mutable cursor with `internal` methods `Increment()`, `NewLine()`, and `Reset()` called from the lexer, parser, and enumerator. It stays as a class and gets `IEquatable<FileLocation>` added manually instead.

**HashCode note:** `HashCode.Combine` is not available on netstandard2.0. Use `#if` conditional compilation: `HashCode.Combine` on net8.0+, manual hash on netstandard2.0.

---

### Task 1: Cache regex in StringExtensions.ToLines()

**Files:**
- Modify: `src/Tokenizer/Extensions/StringExtensions.cs:187`

- [ ] **Step 1: Run existing tests to establish baseline**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "StringExtensionsTest" -v quiet`
Expected: All tests pass.

- [ ] **Step 2: Add static compiled regex field and update ToLines()**

In `src/Tokenizer/Extensions/StringExtensions.cs`, add a static field near the top of the class (after line 10) and update `ToLines()`:

```csharp
public static class StringExtensions
{
    private static readonly Regex NewLineSplitRegex = new(@"\r\n|\r|\n", RegexOptions.Compiled);
```

Then at line 187, replace:
```csharp
        return Regex.Split(value, "\r\n|\r|\n");
```
with:
```csharp
        return NewLineSplitRegex.Split(value);
```

- [ ] **Step 3: Run tests to verify no regression**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "StringExtensionsTest" -v quiet`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Extensions/StringExtensions.cs
git commit -m "Cache compiled regex in StringExtensions.ToLines()"
```

---

### Task 2: Cache regex in ToDateTimeTransformer

**Files:**
- Modify: `src/Tokenizer/Transformers/ToDateTimeTransformer.cs:97`

- [ ] **Step 1: Run existing tests to establish baseline**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToDateTimeTransformer" -v quiet`
Expected: All tests pass.

- [ ] **Step 2: Add static compiled regex field and update usage**

In `src/Tokenizer/Transformers/ToDateTimeTransformer.cs`, add after the `LockHandle` field (after line 13):

```csharp
    private static readonly Regex OrdinalSuffixRegex = new(@"\b(\d+)(?:st|nd|rd|th)\b", RegexOptions.Compiled);
```

Then at line 97, replace:
```csharp
                        valueToFormat = Regex.Replace(valueToFormat, @"\b(\d+)(?:st|nd|rd|th)\b", "$1");
```
with:
```csharp
                        valueToFormat = OrdinalSuffixRegex.Replace(valueToFormat, "$1");
```

- [ ] **Step 3: Run tests to verify no regression**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToDateTimeTransformer" -v quiet`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Transformers/ToDateTimeTransformer.cs
git commit -m "Cache compiled regex in ToDateTimeTransformer"
```

---

### Task 3: Cache regex in PreambleNearMissHintGenerator

**Files:**
- Modify: `src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs:56`

- [ ] **Step 1: Run existing tests to establish baseline**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PreambleNearMissHintGenerator" -v quiet`
Expected: All tests pass.

- [ ] **Step 2: Add static compiled regex field and update NormalizeWhitespace**

In `src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs`, add a static field inside the class (after line 11):

```csharp
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
```

Then at line 56, replace:
```csharp
        return Regex.Replace(value.Trim(), @"\s+", " ");
```
with:
```csharp
        return WhitespaceRegex.Replace(value.Trim(), " ");
```

- [ ] **Step 3: Run tests to verify no regression**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PreambleNearMissHintGenerator" -v quiet`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs
git commit -m "Cache compiled regex in PreambleNearMissHintGenerator"
```

---

### Task 4: Cache reflection GetMethod("Add") in ObjectExtensions

**Files:**
- Modify: `src/Tokenizer/Extensions/ObjectExtensions.cs:12,95`

- [ ] **Step 1: Run existing tests to establish baseline**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v quiet`
Expected: All tests pass.

- [ ] **Step 2: Add MethodInfo cache and update GetMethod call**

In `src/Tokenizer/Extensions/ObjectExtensions.cs`, add after the `PropertyCache` field (after line 12):

```csharp
    private static readonly ConcurrentDictionary<Type, MethodInfo> AddMethodCache = new();
```

Then at line 95-96, replace:
```csharp
                    var addMethod = list.GetType().GetMethod("Add")
                        ?? throw new InvalidOperationException($"Type {list.GetType().Name} does not have an Add method");
```
with:
```csharp
                    var addMethod = AddMethodCache.GetOrAdd(list.GetType(), t =>
                        t.GetMethod("Add")
                        ?? throw new InvalidOperationException($"Type {t.Name} does not have an Add method"));
```

- [ ] **Step 3: Run tests to verify no regression**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v quiet`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Extensions/ObjectExtensions.cs
git commit -m "Cache GetMethod('Add') reflection in ObjectExtensions"
```

---

### Task 5: Eliminate substring allocations in EndsWithNewLine

**Files:**
- Modify: `src/Tokenizer/Extensions/StringExtensions.cs:303-320`

- [ ] **Step 1: Run existing tests to establish baseline**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "EndsWithNewLine" -v quiet`
Expected: All 6 tests pass (Unix, Windows, False, Empty, Null, Short).

- [ ] **Step 2: Replace substring comparisons with char indexing**

In `src/Tokenizer/Extensions/StringExtensions.cs`, replace the entire `EndsWithNewLine` method body (lines 303-320):

```csharp
    public static bool EndsWithNewLine(this string value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        // Check Unix format
        if (value.Substring(value.Length - 1) == "\n")
        {
            return true;
        }

        // Check Windows format
        if (value.Length >= 2 && value.Substring(value.Length - 2) == "\r\n")
        {
            return true;
        }

        return false;
    }
```

with:

```csharp
    public static bool EndsWithNewLine(this string value)
    {
        return !string.IsNullOrEmpty(value) && value[value.Length - 1] == '\n';
    }
```

- [ ] **Step 3: Run tests to verify no regression**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "EndsWithNewLine" -v quiet`
Expected: All 6 tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Extensions/StringExtensions.cs
git commit -m "Eliminate substring allocations in EndsWithNewLine"
```

---

### Task 6: Eliminate substring allocations in TrimLeadingSpaces

**Files:**
- Modify: `src/Tokenizer/Extensions/StringExtensions.cs:218-235`

- [ ] **Step 1: Run existing tests to establish baseline**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "StringExtensionsTest" -v quiet`
Expected: All tests pass.

- [ ] **Step 2: Replace substring-per-char with char indexing and remove unused StringBuilder**

In `src/Tokenizer/Extensions/StringExtensions.cs`, replace the entire `TrimLeadingSpaces` method body (lines 218-235):

```csharp
    public static string TrimLeadingSpaces(this string value)
    {
        var sb = new StringBuilder();

        if (string.IsNullOrEmpty(value) == false)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var character = value.Substring(i, 1);

                if (character == " ") continue;

                return value.Substring(i);
            }
        }

        return sb.ToString();
    }
```

with:

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

- [ ] **Step 3: Run tests to verify no regression**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "StringExtensionsTest" -v quiet`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Extensions/StringExtensions.cs
git commit -m "Eliminate substring allocations in TrimLeadingSpaces"
```

---

### Task 7: Merge triple iteration in ProcessFrontMatterTokens

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs:342-349`

- [ ] **Step 1: Run existing tests to establish baseline**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FrontMatter" -v quiet`
Expected: All tests pass.

- [ ] **Step 2: Materialize front matter tokens to a list once**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`, replace lines 342-349:

```csharp
        var frontMatterTokens = template.Tokens.Where(t => t.IsFrontMatterToken);
        if (log.IsEnabled(LogLevel.Trace))
        {
            log.LogTrace("Processing {FrontMatterCount} front matter tokens",
                template.Tokens.Count(t => t.IsFrontMatterToken));
        }

        foreach (var token in frontMatterTokens)
```

with:

```csharp
        var frontMatterTokens = template.Tokens.Where(t => t.IsFrontMatterToken).ToList();
        if (log.IsEnabled(LogLevel.Trace))
        {
            log.LogTrace("Processing {FrontMatterCount} front matter tokens",
                frontMatterTokens.Count);
        }

        foreach (var token in frontMatterTokens)
```

- [ ] **Step 3: Run tests to verify no regression**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FrontMatter" -v quiet`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationEngine.cs
git commit -m "Materialize front matter tokens to eliminate triple iteration"
```

---

### Task 8: Add IEquatable to FileLocation

**Files:**
- Modify: `src/Tokenizer/Enumerators/FileLocation.cs`
- Create: `tests/Tokenizer.Tests/Enumerators/FileLocationTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Enumerators/FileLocationTests.cs`:

```csharp
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Enumerators;

public class FileLocationTests
{
    [Fact]
    public void GivenTwoLocationsWithSameValues_WhenCompared_ThenAreEqual()
    {
        // Arrange
        var a = new FileLocation();
        var b = new FileLocation();

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void GivenTwoLocationsWithSameValues_WhenComparedWithOperator_ThenAreEqual()
    {
        // Arrange
        var a = new FileLocation();
        var b = new FileLocation();

        // Act & Assert
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void GivenTwoLocationsWithDifferentValues_WhenCompared_ThenAreNotEqual()
    {
        // Arrange
        var a = new FileLocation();
        var b = new FileLocation();
        b.Increment('x'); // Column becomes 2

        // Act & Assert
        Assert.NotEqual(a, b);
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void GivenTwoEqualLocations_WhenHashed_ThenHashCodesMatch()
    {
        // Arrange
        var a = new FileLocation();
        var b = new FileLocation();

        // Act & Assert
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GivenLocation_WhenComparedToNull_ThenIsNotEqual()
    {
        // Arrange
        var location = new FileLocation();

        // Act & Assert
        Assert.False(location.Equals(null));
    }

    [Fact]
    public void GivenLocation_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var location = new FileLocation();

        // Act
        var result = location.ToString();

        // Assert
        Assert.Equal("Ln: 1 Col: 1 Para: 1", result);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FileLocationTests" -v quiet`
Expected: FAIL — `FileLocation` does not implement `IEquatable<FileLocation>`, `==` operator, or proper `GetHashCode`.

- [ ] **Step 3: Implement IEquatable on FileLocation**

In `src/Tokenizer/Enumerators/FileLocation.cs`, modify the class declaration and add equality members. Replace:

```csharp
public class FileLocation
```

with:

```csharp
public class FileLocation : IEquatable<FileLocation>
```

Then add the following members before the `ToString()` method (before line 97):

```csharp
    /// <summary>
    /// Determines whether the specified <see cref="FileLocation"/> is equal to this instance.
    /// </summary>
    public bool Equals(FileLocation? other)
    {
        return other is not null && Column == other.Column && Line == other.Line && Paragraph == other.Paragraph;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as FileLocation);

    /// <inheritdoc />
    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Column;
            hash = hash * 31 + Line;
            hash = hash * 31 + Paragraph;
            return hash;
        }
#else
        return HashCode.Combine(Column, Line, Paragraph);
#endif
    }

    /// <summary>
    /// Determines whether two <see cref="FileLocation"/> instances are equal.
    /// </summary>
    public static bool operator ==(FileLocation? left, FileLocation? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="FileLocation"/> instances are not equal.
    /// </summary>
    public static bool operator !=(FileLocation? left, FileLocation? right) => !Equals(left, right);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FileLocationTests" -v quiet`
Expected: All tests pass.

- [ ] **Step 5: Run full test suite to check for regressions**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v quiet`
Expected: All tests pass. Adding `==`/`!=` operators could affect existing code that used reference equality — verify no breakage.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Enumerators/FileLocation.cs tests/Tokenizer.Tests/Enumerators/FileLocationTests.cs
git commit -m "Add IEquatable<FileLocation> with equality operators and GetHashCode"
```

---

### Task 9: Add IEquatable to HintMatch

**Files:**
- Modify: `src/Tokenizer/HintMatch.cs`
- Create: `tests/Tokenizer.Tests/HintMatchTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/HintMatchTests.cs`:

```csharp
using Tokens.Enumerators;
using Xunit;

namespace Tokens;

public class HintMatchTests
{
    [Fact]
    public void GivenTwoHintMatchesWithSameValues_WhenCompared_ThenAreEqual()
    {
        // Arrange
        var location = new FileLocation();
        var a = new HintMatch("test", false, location);
        var b = new HintMatch("test", false, location);

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void GivenTwoHintMatchesWithDifferentText_WhenCompared_ThenAreNotEqual()
    {
        // Arrange
        var location = new FileLocation();
        var a = new HintMatch("test", false, location);
        var b = new HintMatch("other", false, location);

        // Act & Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GivenTwoHintMatchesWithDifferentOptional_WhenCompared_ThenAreNotEqual()
    {
        // Arrange
        var location = new FileLocation();
        var a = new HintMatch("test", false, location);
        var b = new HintMatch("test", true, location);

        // Act & Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GivenTwoEqualHintMatches_WhenHashed_ThenHashCodesMatch()
    {
        // Arrange
        var location = new FileLocation();
        var a = new HintMatch("test", false, location);
        var b = new HintMatch("test", false, location);

        // Act & Assert
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GivenHintMatch_WhenComparedToNull_ThenIsNotEqual()
    {
        // Arrange
        var match = new HintMatch("test", false, new FileLocation());

        // Act & Assert
        Assert.False(match.Equals(null));
    }

    [Fact]
    public void GivenHintMatch_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var match = new HintMatch("Domain Name", false, new FileLocation());

        // Act
        var result = match.ToString();

        // Assert
        Assert.Equal("HintMatch('Domain Name' @ Ln: 1 Col: 1 Para: 1)", result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HintMatchTests" -v quiet`
Expected: FAIL — `HintMatch` does not implement equality or custom `ToString`.

- [ ] **Step 3: Implement IEquatable and ToString on HintMatch**

Replace the entire content of `src/Tokenizer/HintMatch.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Represents a hint string that a template uses to pre-filter candidate inputs.
/// </summary>
public sealed class HintMatch : IEquatable<HintMatch>
{
    /// <summary>
    /// Creates a new <see cref="HintMatch"/> with the matched hint text, whether it is optional, and its location.
    /// </summary>
    /// <param name="text">The hint text that was found in the input.</param>
    /// <param name="optional">Whether the hint is optional.</param>
    /// <param name="location">The location in the input where the hint was matched.</param>
    public HintMatch(string text, bool optional, FileLocation location)
    {
        Text = text;
        Optional = optional;
        Location = location;
    }

    /// <summary>
    /// The hint string to search for in the input.
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// When true, the hint is optional and a missing match does not disqualify the template.
    /// </summary>
    public bool Optional { get; init; }

    /// <summary>
    /// The location in the template pattern where this hint was declared.
    /// </summary>
    public FileLocation Location { get; init; }

    /// <inheritdoc />
    public bool Equals(HintMatch? other)
    {
        return other is not null && Text == other.Text && Optional == other.Optional && Location == other.Location;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as HintMatch);

    /// <inheritdoc />
    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Text?.GetHashCode() ?? 0);
            hash = hash * 31 + Optional.GetHashCode();
            hash = hash * 31 + (Location?.GetHashCode() ?? 0);
            return hash;
        }
#else
        return HashCode.Combine(Text, Optional, Location);
#endif
    }

    /// <inheritdoc />
    public override string ToString() => $"HintMatch('{Text}' @ {Location})";
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HintMatchTests" -v quiet`
Expected: All tests pass.

- [ ] **Step 5: Run full test suite for regressions**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v quiet`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/HintMatch.cs tests/Tokenizer.Tests/HintMatchTests.cs
git commit -m "Add IEquatable<HintMatch> with ToString override"
```

---

### Task 10: Add ToString to TokenMatch

**Files:**
- Modify: `src/Tokenizer/TokenMatch.cs`
- Create: `tests/Tokenizer.Tests/TokenMatchTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/Tokenizer.Tests/TokenMatchTests.cs`:

```csharp
using Tokens.Enumerators;
using Xunit;

namespace Tokens;

public class TokenMatchTests
{
    [Fact]
    public void GivenTokenMatch_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var token = new Token("content", "firstName", "Name:", new FileLocation());
        var match = new TokenMatch(token, "John", new FileLocation());

        // Act
        var result = match.ToString();

        // Assert
        Assert.Equal("TokenMatch('firstName' = 'John' @ Ln: 1 Col: 1 Para: 1)", result);
    }

    [Fact]
    public void GivenTokenMatchWithNullValue_WhenToString_ThenHandlesGracefully()
    {
        // Arrange
        var token = new Token("content", "firstName", "Name:", new FileLocation());
        var match = new TokenMatch(token, null!, new FileLocation());

        // Act
        var result = match.ToString();

        // Assert
        Assert.Equal("TokenMatch('firstName' = '' @ Ln: 1 Col: 1 Para: 1)", result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenMatchTests" -v quiet`
Expected: FAIL — the default record `ToString` produces a different format.

- [ ] **Step 3: Add ToString override to TokenMatch**

In `src/Tokenizer/TokenMatch.cs`, replace:

```csharp
public sealed record TokenMatch(Token Token, object Value, FileLocation Location);
```

with:

```csharp
public sealed record TokenMatch(Token Token, object Value, FileLocation Location)
{
    /// <inheritdoc />
    public override string ToString() => $"TokenMatch('{Token.Name}' = '{Value}' @ {Location})";
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenMatchTests" -v quiet`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/TokenMatch.cs tests/Tokenizer.Tests/TokenMatchTests.cs
git commit -m "Add compact ToString override to TokenMatch record"
```

---

### Task 11: Add ToString to Hint

**Files:**
- Modify: `src/Tokenizer/Hint.cs`
- Modify: `tests/Tokenizer.Tests/HintTests.cs` (add ToString tests to existing file — it's an integration test file though, so create a unit test file)
- Create: `tests/Tokenizer.Tests/HintUnitTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/HintUnitTests.cs`:

```csharp
using Xunit;

namespace Tokens;

public class HintUnitTests
{
    [Fact]
    public void GivenRequiredHint_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var hint = new Hint("Domain Name");

        // Act
        var result = hint.ToString();

        // Assert
        Assert.Equal("Hint('Domain Name')", result);
    }

    [Fact]
    public void GivenOptionalHint_WhenToString_ThenIncludesOptionalFlag()
    {
        // Arrange
        var hint = new Hint("Domain Name", Optional: true);

        // Act
        var result = hint.ToString();

        // Assert
        Assert.Equal("Hint('Domain Name', Optional)", result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HintUnitTests" -v quiet`
Expected: FAIL — default record ToString produces `Hint { Text = Domain Name, Optional = False }`.

- [ ] **Step 3: Add ToString override to Hint**

In `src/Tokenizer/Hint.cs`, replace:

```csharp
public sealed record Hint(string Text = "", bool Optional = false);
```

with:

```csharp
public sealed record Hint(string Text = "", bool Optional = false)
{
    /// <inheritdoc />
    public override string ToString() => Optional ? $"Hint('{Text}', Optional)" : $"Hint('{Text}')";
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HintUnitTests" -v quiet`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Hint.cs tests/Tokenizer.Tests/HintUnitTests.cs
git commit -m "Add compact ToString override to Hint record"
```

---

### Task 12: Add ToString to Template

**Files:**
- Modify: `src/Tokenizer/Template.cs`
- Modify: `tests/Tokenizer.Tests/TemplateTests.cs`

- [ ] **Step 1: Write the failing tests**

Add to the end of `tests/Tokenizer.Tests/TemplateTests.cs` (before the closing `}`):

```csharp
    [Fact]
    public void GivenNamedTemplate_WhenToString_ThenReturnsName()
    {
        // Arrange
        var template = new Template("invoice", "Name: {Name}");

        // Act
        var result = template.ToString();

        // Assert
        Assert.Equal("Template('invoice')", result);
    }

    [Fact]
    public void GivenUnnamedTemplate_WhenToString_ThenReturnsTokenCount()
    {
        // Arrange
        var template = new Template(string.Empty);

        // Act
        var result = template.ToString();

        // Assert
        Assert.Equal("Template(0 tokens)", result);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateTests.GivenNamedTemplate_WhenToString" -v quiet`
Expected: FAIL — `Template` has no `ToString` override, returns default type name.

- [ ] **Step 3: Add ToString override to Template**

In `src/Tokenizer/Template.cs`, add before the `internal void AddHint` method (before line 87):

```csharp
    /// <inheritdoc />
    public override string ToString()
    {
        return !string.IsNullOrEmpty(name) ? $"Template('{name}')" : $"Template({Tokens.Count} tokens)";
    }
```

Note: uses the `name` field directly (not the `Name` property) to avoid triggering MD5 hash computation for unnamed templates.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateTests" -v quiet`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Template.cs tests/Tokenizer.Tests/TemplateTests.cs
git commit -m "Add compact ToString override to Template"
```

---

### Task 13: Add ToString to TokenResult

**Files:**
- Modify: `src/Tokenizer/TokenResult.cs`
- Create: `tests/Tokenizer.Tests/TokenResultTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/TokenResultTests.cs`:

```csharp
using Tokens.Enumerators;
using Xunit;

namespace Tokens;

public class TokenResultTests
{
    [Fact]
    public void GivenEmptyTokenResult_WhenToString_ThenReturnsZeroCounts()
    {
        // Arrange
        var result = new TokenResult();

        // Act
        var output = result.ToString();

        // Assert
        Assert.Equal("TokenResult(0 matched, 0 missed)", output);
    }

    [Fact]
    public void GivenTokenResultWithMatchesAndMisses_WhenToString_ThenReturnsCounts()
    {
        // Arrange
        var result = new TokenResult();
        var token = new Token("content", "name", "Name:", new FileLocation());
        result.AddMatch(token, "John", new FileLocation());
        result.AddMiss(token);

        // Act
        var output = result.ToString();

        // Assert
        Assert.Equal("TokenResult(1 matched, 1 missed)", output);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenResultTests" -v quiet`
Expected: FAIL — default `ToString` returns type name.

- [ ] **Step 3: Add ToString override to TokenResult**

In `src/Tokenizer/TokenResult.cs`, add before the closing `}` of the class:

```csharp
    /// <inheritdoc />
    public override string ToString() => $"TokenResult({Matches.Count} matched, {Misses.Count} missed)";
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenResultTests" -v quiet`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/TokenResult.cs tests/Tokenizer.Tests/TokenResultTests.cs
git commit -m "Add compact ToString override to TokenResult"
```

---

### Task 14: Add ToString to HintResult

**Files:**
- Modify: `src/Tokenizer/HintResult.cs`
- Create: `tests/Tokenizer.Tests/HintResultTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/HintResultTests.cs`:

```csharp
using Xunit;

namespace Tokens;

public class HintResultTests
{
    [Fact]
    public void GivenEmptyHintResult_WhenToString_ThenReturnsZeroCounts()
    {
        // Arrange
        var result = new HintResult();

        // Act
        var output = result.ToString();

        // Assert
        Assert.Equal("HintResult(0 matched, 0 missed)", output);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HintResultTests" -v quiet`
Expected: FAIL.

- [ ] **Step 3: Add ToString override to HintResult**

In `src/Tokenizer/HintResult.cs`, add before the closing `}` of the class:

```csharp
    /// <inheritdoc />
    public override string ToString() => $"HintResult({Matches.Count} matched, {Misses.Count} missed)";
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HintResultTests" -v quiet`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/HintResult.cs tests/Tokenizer.Tests/HintResultTests.cs
git commit -m "Add compact ToString override to HintResult"
```

---

### Task 15: Add ToString to TokenizeResult and TokenizeResult\<T\>

**Files:**
- Modify: `src/Tokenizer/TokenizeResult.cs`
- Create: `tests/Tokenizer.Tests/TokenizeResultTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/TokenizeResultTests.cs`:

```csharp
using Xunit;

namespace Tokens;

public class TokenizeResultTests
{
    [Fact]
    public void GivenTokenizeResult_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var template = new Template("test-template", "Name: {Name}");
        var result = new TokenizeResult(template);

        // Act
        var output = result.ToString();

        // Assert
        Assert.Equal("TokenizeResult('test-template': 0 matched, 0 missed)", output);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizeResultTests" -v quiet`
Expected: FAIL.

- [ ] **Step 3: Add ToString override to TokenizeResultBase**

The `ToString` should go on `TokenizeResultBase` so both `TokenizeResult` and `TokenizeResult<T>` inherit it. In `src/Tokenizer/TokenizeResultBase.cs`, add before the closing `}`:

```csharp
    /// <inheritdoc />
    public override string ToString() =>
        $"TokenizeResult('{Template.Name}': {Tokens.Matches.Count} matched, {Tokens.Misses.Count} missed)";
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizeResultTests" -v quiet`
Expected: All tests pass.

- [ ] **Step 5: Run full test suite**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v quiet`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/TokenizeResultBase.cs tests/Tokenizer.Tests/TokenizeResultTests.cs
git commit -m "Add compact ToString override to TokenizeResultBase"
```

---

### Task 16: Final verification and roadmap update

**Files:**
- Modify: `docs/ROADMAP.md`

- [ ] **Step 1: Run full test suite**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v quiet`
Expected: All tests pass.

- [ ] **Step 2: Build in Release mode**

Run: `dotnet build src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with no errors. Check for any new warnings.

- [ ] **Step 3: Update roadmap**

In `docs/ROADMAP.md`, mark Tier 6 items as complete:

Replace:
```markdown
- [ ] **Cache regex patterns with `RegexOptions.Compiled`** — `StringExtensions.cs:190`, `ToDateTimeTransformer.cs:84`, `PreambleNearMissHintGenerator.cs:57`
- [ ] **Cache `GetMethod("Add")` in `ObjectExtensions:86`** — uncached reflection on every list property assignment
- [ ] **Eliminate substring allocations in `StringExtensions`** — `EndsWithNewLine` (lines 308-316) and `TrimLeadingSpaces` (line 219) create substrings for single-char comparisons
- [ ] **Merge double iteration in `ProcessFrontMatterTokens`** — `TokenizationEngine.cs:342-344` iterates tokens twice (`.Where` + `.Count`)
- [ ] **Add `ToString()` overrides** on `Match`, `TokenizeResult`, `TokenResult`, `HintResult`, `Hint`, `Template` for debugging
- [ ] **Add `IEquatable<T>`** on value-like types: `Hint`, `HintMatch`, `Match`, `FileLocation`
```

with:

```markdown
- [x] **Cache regex patterns with `RegexOptions.Compiled`** — `StringExtensions.cs:190`, `ToDateTimeTransformer.cs:84`, `PreambleNearMissHintGenerator.cs:57`
- [x] **Cache `GetMethod("Add")` in `ObjectExtensions:86`** — uncached reflection on every list property assignment
- [x] **Eliminate substring allocations in `StringExtensions`** — `EndsWithNewLine` (lines 308-316) and `TrimLeadingSpaces` (line 219) create substrings for single-char comparisons
- [x] **Merge double iteration in `ProcessFrontMatterTokens`** — `TokenizationEngine.cs:342-344` iterates tokens twice (`.Where` + `.Count`)
- [x] **Add `ToString()` overrides** on `Match`, `TokenizeResult`, `TokenResult`, `HintResult`, `Hint`, `Template` for debugging
- [x] **Add `IEquatable<T>`** on value-like types: `Hint`, `HintMatch`, `Match`, `FileLocation`
```

- [ ] **Step 4: Commit**

```bash
git add docs/ROADMAP.md
git commit -m "Update ROADMAP.md: mark Tier 6 items complete"
```
