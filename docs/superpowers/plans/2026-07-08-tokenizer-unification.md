# Tokenizer.cs Unification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unify `TokenizeCore`/`TokenizeAsyncCore` into a single `RunCoreAsync` method, fix async hint processing to scan buffer contents, extract IO logic, and reorder `Tokenizer.cs` members.

**Architecture:** A single `async Task RunCoreAsync(...)` replaces both private tokenization methods. Hint strategies are redesigned: `UpfrontHintStrategy` (sync, `string.Contains` upfront) and `StreamingHintStrategy` (async, scans staging buffer via `OnBufferFilled` callback). `ReadToEndAsync` is extracted to `TextReaderExtensions`.

**Tech Stack:** C# (.NET Standard 2.0 / .NET 8.0 / .NET 10.0), xUnit, BenchmarkDotNet

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `src/Tokenizer/Tokenization/IHintStrategy.cs` | Modify | Replace `OnTokenMatched` with `OnBufferFilled` |
| `src/Tokenizer/Tokenization/Strategies/UpfrontHintStrategy.cs` | Create (rename) | Sync hint strategy — upfront `string.Contains`, no-op `OnBufferFilled` |
| `src/Tokenizer/Tokenization/Strategies/StreamingHintStrategy.cs` | Create (rename) | Async hint strategy — no-op `PreProcess`, buffer scanning `OnBufferFilled` |
| `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs` | Delete | Replaced by `UpfrontHintStrategy` |
| `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs` | Delete | Replaced by `StreamingHintStrategy` |
| `src/Tokenizer/Enumerators/TokenEnumerator.cs` (src/) | Modify | Expose `StagingBuffer`, `LastReadCount` properties |
| `src/Tokenizer/Tokenization/TokenizationSession.cs` | Modify | Store `IHintStrategy?`, call `OnBufferFilled` after buffer refills |
| `src/Tokenizer/Tokenization/TokenMatchRouter.cs` | Modify | Remove `OnTokenMatched` forwarding block |
| `src/Tokenizer/Extensions/TextReaderExtensions.cs` | Create | Extracted `ReadToEndBoundedAsync` extension method |
| `src/Tokenizer/Tokenizer.cs` | Modify | Unify to `RunCoreAsync`, reorder members, remove `ReadToEndAsync` |
| `tests/Tokenizer.Tests/Tokenization/Strategies/UpfrontHintStrategyTests.cs` | Create (rename) | Renamed from `ContainsHintStrategyTests` |
| `tests/Tokenizer.Tests/Tokenization/Strategies/StreamingHintStrategyTests.cs` | Create (rename) | New buffer-scanning tests |
| `tests/Tokenizer.Tests/Tokenization/Strategies/ContainsHintStrategyTests.cs` | Delete | Replaced |
| `tests/Tokenizer.Tests/Tokenization/Strategies/IntegratedHintStrategyTests.cs` | Delete | Replaced |
| `tests/Tokenizer.Tests/Extensions/TextReaderExtensionsTests.cs` | Create | Tests for extracted `ReadToEndBoundedAsync` |

---

### Task 0: Benchmark Baseline

**Files:**
- None modified

- [ ] **Step 1: Run full benchmark suite**

Run:
```bash
cd benchmarks/Tokenizer.Benchmarks && dotnet run -c Release -- --filter '*' --artifacts ../../../benchmark-results/baseline
```

- [ ] **Step 2: Save results for comparison**

The results are written to `benchmark-results/baseline/`. These will be compared against after implementation.

- [ ] **Step 3: Verify all existing tests pass**

Run:
```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

---

### Task 1: Update `IHintStrategy` Interface

**Files:**
- Modify: `src/Tokenizer/Tokenization/IHintStrategy.cs`

- [ ] **Step 1: Replace `OnTokenMatched` with `OnBufferFilled`**

Replace the full contents of `IHintStrategy.cs` with:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Defines a strategy for processing template hints during tokenization.
/// </summary>
internal interface IHintStrategy
{
    /// <summary>
    /// Pre-processes hints before tokenization begins.
    /// Returns true if required hints are missing and tokenization should be skipped.
    /// </summary>
    /// <param name="template">The template containing hint definitions.</param>
    /// <param name="enumerator">The token enumerator positioned at the start of input.</param>
    /// <param name="rawInput">The original string when available, null for TextReader-only inputs.</param>
    /// <param name="result">The result object to populate with hint matches and misses.</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    /// <returns>True if required hints are missing, false if all required hints are found.</returns>
    bool PreProcess(Template template, TokenEnumerator enumerator,
                    string? rawInput, TokenizeResult result, IDiagnosticCollector collector);

    /// <summary>
    /// Called by the tokenization session after each buffer refill, passing the staging
    /// buffer contents before they are copied into the ring buffer.
    /// </summary>
    /// <param name="buffer">The staging buffer containing newly-read characters.</param>
    /// <param name="count">The number of valid characters in <paramref name="buffer"/>.</param>
    void OnBufferFilled(char[] buffer, int count);

    /// <summary>
    /// Post-processes hints after tokenization completes.
    /// Returns true if required hints are missing.
    /// </summary>
    /// <param name="result">The result object containing tokenization results.</param>
    /// <returns>True if required hints are missing, false otherwise.</returns>
    bool PostProcess(TokenizeResult result);
}
```

- [ ] **Step 2: Verify the build fails**

Run:
```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release 2>&1 | head -40
```

Expected: Build errors in `ContainsHintStrategy`, `IntegratedHintStrategy`, `TokenMatchRouter` — they reference the old `OnTokenMatched` method.

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Tokenization/IHintStrategy.cs
git commit -m "refactor: replace OnTokenMatched with OnBufferFilled in IHintStrategy"
```

---

### Task 2: Create `UpfrontHintStrategy` (rename `ContainsHintStrategy`)

**Files:**
- Create: `src/Tokenizer/Tokenization/Strategies/UpfrontHintStrategy.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Strategies/UpfrontHintStrategyTests.cs`
- Delete: `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs`
- Delete: `tests/Tokenizer.Tests/Tokenization/Strategies/ContainsHintStrategyTests.cs`

- [ ] **Step 1: Create `UpfrontHintStrategyTests.cs`**

Copy `ContainsHintStrategyTests.cs` and update:
- Rename class to `UpfrontHintStrategyTests`
- Change `_strategy` type and instantiation to `UpfrontHintStrategy`
- Replace the `OnTokenMatched` no-op test with an `OnBufferFilled` no-op test:

```csharp
using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization.Strategies;

public class UpfrontHintStrategyTests
{
    private readonly UpfrontHintStrategy _strategy = new();

    [Fact]
    public void GivenTemplateWithNoHints_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithRequiredHintPresent_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Hello")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithRequiredHintMissing_WhenPreProcess_ThenReturnsTrue()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Goodbye")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithOptionalHintMissing_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Goodbye")
                .WithOptional()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenEnumerator_WhenPreProcess_ThenEnumeratorIsNotConsumed()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Hello")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _strategy.PreProcess(template, enumerator, "Hello World", result, NullDiagnosticCollector.Instance);

        // Assert - enumerator should still be at the beginning
        Assert.Equal('H', enumerator.Peek());
    }

    [Fact]
    public void GivenNullRawInputWithHints_WhenPreProcess_ThenThrowsArgumentNullException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Hello")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act & Assert — sync path always provides rawInput; null is a programming error
        Assert.Throws<ArgumentNullException>(() =>
            _strategy.PreProcess(template, enumerator, rawInput: null, result, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenBuffer_WhenOnBufferFilled_ThenDoesNotThrow()
    {
        // Arrange
        var buffer = "Hello World".ToCharArray();

        // Act & Assert — OnBufferFilled is a no-op for the upfront strategy
        var exception = Record.Exception(() => _strategy.OnBufferFilled(buffer, buffer.Length));
        Assert.Null(exception);
    }

    [Fact]
    public void GivenResult_WhenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }
}
```

- [ ] **Step 2: Create `UpfrontHintStrategy.cs`**

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Hint strategy for synchronous tokenization. Scans the full input string upfront
/// using <see cref="string.Contains(string, StringComparison)"/> to find hints.
/// Does not touch the enumerator, so no reset is needed.
/// </summary>
internal sealed class UpfrontHintStrategy : IHintStrategy
{
    /// <inheritdoc />
    public bool PreProcess(Template template, TokenEnumerator enumerator,
                           string? rawInput, TokenizeResult result, IDiagnosticCollector collector)
    {
        if (template.Hints.Count == 0)
        {
            return false;
        }

        if (rawInput == null)
        {
            throw new ArgumentNullException(nameof(rawInput), "UpfrontHintStrategy requires rawInput — use StreamingHintStrategy for streaming inputs");
        }

        foreach (var hint in template.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text))
            {
                continue;
            }

            if (rawInput.Contains(hint.Text, StringComparison.Ordinal))
            {
                result.Hints.TryAddMatch(hint, enumerator);

                collector.Record(DiagnosticEventType.HintMatched,
                    value: hint.Text,
                    location: enumerator.Location);
            }
        }

        foreach (var hint in template.Hints)
        {
            if (result.Hints.TryAddMiss(hint) && !hint.Optional)
            {
                collector.Record(DiagnosticEventType.HintMissing,
                    value: hint.Text);
            }
        }

        return result.Hints.Misses.Any(h => !h.Optional);
    }

    /// <inheritdoc />
    public void OnBufferFilled(char[] buffer, int count)
    {
        // Upfront strategy scans the full input in PreProcess — no per-chunk work needed
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResult result)
    {
        return false;
    }
}
```

- [ ] **Step 3: Delete old files**

```bash
git rm src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs
git rm tests/Tokenizer.Tests/Tokenization/Strategies/ContainsHintStrategyTests.cs
```

- [ ] **Step 4: Update references in `Tokenizer.cs`**

In `src/Tokenizer/Tokenizer.cs`, change `new ContainsHintStrategy()` to `new UpfrontHintStrategy()` (line 130).

- [ ] **Step 5: Run the new tests**

Run:
```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "UpfrontHintStrategyTests"
```

Expected: All 8 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/Strategies/UpfrontHintStrategy.cs tests/Tokenizer.Tests/Tokenization/Strategies/UpfrontHintStrategyTests.cs src/Tokenizer/Tokenizer.cs
git commit -m "refactor: rename ContainsHintStrategy to UpfrontHintStrategy"
```

---

### Task 3: Expose `StagingBuffer` and `LastReadCount` on `TokenEnumerator`

**Files:**
- Modify: `src/Tokenizer/Enumerators/TokenEnumerator.cs` (src/)

- [ ] **Step 1: Add `LastReadCount` field and property**

In `src/Tokenizer/Enumerators/TokenEnumerator.cs`, add a field after `_resetNextLine` (line 22):

```csharp
    private int _lastReadCount;
```

Add properties after the `NeedsRefill` property (after line 99):

```csharp
    /// <summary>
    /// Gets the staging buffer used during <see cref="FillBuffer"/> and <see cref="FillBufferAsync"/>.
    /// Callers should only read the first <see cref="LastReadCount"/> characters.
    /// </summary>
    internal char[] StagingBuffer => _stagingBuffer;

    /// <summary>
    /// Gets the number of characters read during the most recent
    /// <see cref="FillBuffer"/> or <see cref="FillBufferAsync"/> call.
    /// </summary>
    internal int LastReadCount => _lastReadCount;
```

- [ ] **Step 2: Capture read count in `FillBuffer`**

In the `FillBuffer()` method, after `var read = _reader.Read(staging, 0, available);` (line 113), store the count:

Change:
```csharp
        var read = _reader.Read(staging, 0, available);
        if (read == 0)
        {
            _readerExhausted = true;
            return;
        }

        CopyToRingBuffer(staging, read);
```

To:
```csharp
        var read = _reader.Read(staging, 0, available);
        if (read == 0)
        {
            _lastReadCount = 0;
            _readerExhausted = true;
            return;
        }

        _lastReadCount = read;
        CopyToRingBuffer(staging, read);
```

- [ ] **Step 3: Capture read count in `FillBufferAsync`**

In the `FillBufferAsync()` method, apply the same pattern:

Change:
```csharp
        if (read == 0)
        {
            _readerExhausted = true;
            return;
        }

        CopyToRingBuffer(staging, read);
```

To:
```csharp
        if (read == 0)
        {
            _lastReadCount = 0;
            _readerExhausted = true;
            return;
        }

        _lastReadCount = read;
        CopyToRingBuffer(staging, read);
```

- [ ] **Step 4: Verify build succeeds**

Run:
```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```

Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Enumerators/TokenEnumerator.cs
git commit -m "feat: expose StagingBuffer and LastReadCount on TokenEnumerator"
```

---

### Task 4: Create `StreamingHintStrategy`

**Files:**
- Create: `src/Tokenizer/Tokenization/Strategies/StreamingHintStrategy.cs`
- Create: `tests/Tokenizer.Tests/Tokenization/Strategies/StreamingHintStrategyTests.cs`
- Delete: `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs`
- Delete: `tests/Tokenizer.Tests/Tokenization/Strategies/IntegratedHintStrategyTests.cs`

- [ ] **Step 1: Write `StreamingHintStrategyTests.cs`**

```csharp
using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization.Strategies;

public class StreamingHintStrategyTests
{
    private readonly StreamingHintStrategy _strategy = new();

    [Fact]
    public void GivenTemplateWithNoHints_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenAnyTemplate_WhenPreProcess_ThenAlwaysReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Missing")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        var hintsMissing = _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Assert — PreProcess never skips tokenization for streaming strategies
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenTemplateWithNoHints_WhenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();
        var enumerator = new TokenEnumerator("Hello World");
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenRequiredHintInBuffer_WhenOnBufferFilledThenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Hello")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenRequiredHintNotInBuffer_WhenPostProcess_ThenReturnsTrue()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Goodbye")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenOptionalHintMissing_WhenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Goodbye")
                .WithOptional()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenHintSpanningTwoChunks_WhenOnBufferFilledTwice_ThenHintIsFound()
    {
        // Arrange — hint "Hello" spans across two buffer fills: "...Hel" and "lo..."
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("Hello")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act — feed two chunks where the hint spans the boundary
        var chunk1 = "Some text Hel".ToCharArray();
        _strategy.OnBufferFilled(chunk1, chunk1.Length);

        var chunk2 = "lo more text".ToCharArray();
        _strategy.OnBufferFilled(chunk2, chunk2.Length);

        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenHintInSecondChunk_WhenOnBufferFilledTwice_ThenHintIsFound()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("World")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act
        var chunk1 = "Hello ".ToCharArray();
        _strategy.OnBufferFilled(chunk1, chunk1.Length);

        var chunk2 = "World".ToCharArray();
        _strategy.OnBufferFilled(chunk2, chunk2.Length);

        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.False(hintsMissing);
    }

    [Fact]
    public void GivenMultipleHints_WhenSomeFoundSomeMissing_ThenRequiredMissingReturnsTrue()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(
                new HintBuilder().WithText("Hello").WithRequired().Build(),
                new HintBuilder().WithText("Missing").WithRequired().Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, buffer.Length);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert
        Assert.True(hintsMissing);
    }

    [Fact]
    public void GivenBufferLargerThanCount_WhenOnBufferFilled_ThenOnlyCountCharsScanned()
    {
        // Arrange — buffer is larger but count limits what's scanned
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithHints(new HintBuilder()
                .WithText("World")
                .WithRequired()
                .Build())
            .Build();
        var enumerator = new TokenEnumerator(string.Empty);
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        _strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act — "Hello" is in first 5 chars, "World" starts at index 6, but count=5 limits scan
        var buffer = "Hello World".ToCharArray();
        _strategy.OnBufferFilled(buffer, 5);
        var hintsMissing = _strategy.PostProcess(result);

        // Assert — "World" should not be found
        Assert.True(hintsMissing);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "StreamingHintStrategyTests" 2>&1 | tail -5
```

Expected: Build failure — `StreamingHintStrategy` does not exist.

- [ ] **Step 3: Create `StreamingHintStrategy.cs`**

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Hint strategy for asynchronous/streaming tokenization. Scans buffer contents
/// incrementally via <see cref="OnBufferFilled"/> callbacks during tokenization,
/// maintaining an overlap window to detect hints spanning chunk boundaries.
/// </summary>
internal sealed class StreamingHintStrategy : IHintStrategy
{
    private Template? _currentTemplate;
    private int _maxHintLength;
    private char[]? _overlapBuffer;
    private int _overlapCount;
    private readonly HashSet<string> _foundHints = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public bool PreProcess(Template template, TokenEnumerator enumerator,
                           string? rawInput, TokenizeResult result, IDiagnosticCollector collector)
    {
        _currentTemplate = template;
        _foundHints.Clear();
        _overlapCount = 0;

        if (template.Hints.Count == 0)
        {
            _maxHintLength = 0;
            return false;
        }

        _maxHintLength = 0;
        foreach (var hint in template.Hints)
        {
            if (!string.IsNullOrEmpty(hint.Text) && hint.Text.Length > _maxHintLength)
            {
                _maxHintLength = hint.Text.Length;
            }
        }

        // Overlap window: maxHintLength - 1 characters from the end of the previous chunk
        // to catch hints spanning chunk boundaries
        if (_maxHintLength > 1)
        {
            var overlapSize = _maxHintLength - 1;
            if (_overlapBuffer == null || _overlapBuffer.Length < overlapSize)
            {
                _overlapBuffer = new char[overlapSize];
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void OnBufferFilled(char[] buffer, int count)
    {
        if (_currentTemplate == null || _currentTemplate.Hints.Count == 0 || count == 0)
        {
            return;
        }

        // If all hints are already found, skip scanning
        if (_foundHints.Count >= _currentTemplate.Hints.Count)
        {
            return;
        }

        // Scan for hints in the overlap region (previous tail + current head) and current chunk
        ScanForHints(buffer, count);

        // Save trailing characters for overlap with next chunk
        if (_maxHintLength > 1)
        {
            var overlapSize = _maxHintLength - 1;
            var copyCount = Math.Min(overlapSize, count);
            var sourceOffset = count - copyCount;
            Array.Copy(buffer, sourceOffset, _overlapBuffer!, 0, copyCount);
            _overlapCount = copyCount;
        }
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResult result)
    {
        if (_currentTemplate == null || _currentTemplate.Hints.Count == 0)
        {
            return false;
        }

        foreach (var hint in _currentTemplate.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text))
            {
                continue;
            }

            if (_foundHints.Contains(hint.Text))
            {
                result.Hints.TryAddMatch(hint, new TokenEnumerator(string.Empty));
            }
        }

        foreach (var hint in _currentTemplate.Hints)
        {
            result.Hints.TryAddMiss(hint);
        }

        return result.Hints.Misses.Any(h => !h.Optional);
    }

    private void ScanForHints(char[] buffer, int count)
    {
        foreach (var hint in _currentTemplate!.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text) || _foundHints.Contains(hint.Text))
            {
                continue;
            }

            if (ScanChunk(buffer, count, hint.Text))
            {
                _foundHints.Add(hint.Text);
            }
        }
    }

    private bool ScanChunk(char[] buffer, int count, string hintText)
    {
        // First, check the overlap region: previous tail + current head
        if (_overlapCount > 0 && _maxHintLength > 1)
        {
            if (ScanOverlap(buffer, count, hintText))
            {
                return true;
            }
        }

        // Then scan the current chunk
#if NET8_0_OR_GREATER
        var span = buffer.AsSpan(0, count);
        return span.IndexOf(hintText.AsSpan(), StringComparison.Ordinal) >= 0;
#else
        return IndexOfInCharArray(buffer, count, hintText) >= 0;
#endif
    }

    private bool ScanOverlap(char[] buffer, int count, string hintText)
    {
        // Build a window from the overlap tail + the start of the new buffer
        // The window only needs to be large enough to contain a hint that straddles the boundary
        var windowFromBuffer = Math.Min(hintText.Length - 1, count);
        var windowLength = _overlapCount + windowFromBuffer;

        if (windowLength < hintText.Length)
        {
            return false;
        }

        // Scan by checking each position in the overlap region
        var maxStart = windowLength - hintText.Length;
        for (var start = 0; start <= maxStart; start++)
        {
            // Only check positions that actually straddle the boundary
            // (positions fully within overlap were scanned in the previous chunk,
            //  positions fully within buffer will be scanned in ScanChunk)
            if (start + hintText.Length <= _overlapCount || start >= _overlapCount)
            {
                continue;
            }

            var matched = true;
            for (var j = 0; j < hintText.Length; j++)
            {
                var pos = start + j;
                var c = pos < _overlapCount ? _overlapBuffer![pos] : buffer[pos - _overlapCount];
                if (c != hintText[j])
                {
                    matched = false;
                    break;
                }
            }

            if (matched) return true;
        }

        return false;
    }

#if !NET8_0_OR_GREATER
    private static int IndexOfInCharArray(char[] buffer, int count, string value)
    {
        var valueLength = value.Length;
        if (valueLength == 0) return 0;
        if (count < valueLength) return -1;

        var firstChar = value[0];
        var maxStart = count - valueLength;

        for (var i = 0; i <= maxStart; i++)
        {
            if (buffer[i] != firstChar) continue;

            var found = true;
            for (var j = 1; j < valueLength; j++)
            {
                if (buffer[i + j] != value[j])
                {
                    found = false;
                    break;
                }
            }

            if (found) return i;
        }

        return -1;
    }
#endif
}
```

- [ ] **Step 4: Delete old files**

```bash
git rm src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs
git rm tests/Tokenizer.Tests/Tokenization/Strategies/IntegratedHintStrategyTests.cs
```

- [ ] **Step 5: Update reference in `Tokenizer.cs`**

In `src/Tokenizer/Tokenizer.cs`, change `new IntegratedHintStrategy()` to `new StreamingHintStrategy()` (line 348).

- [ ] **Step 6: Run the new tests**

Run:
```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "StreamingHintStrategyTests"
```

Expected: All 10 tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Tokenization/Strategies/StreamingHintStrategy.cs tests/Tokenizer.Tests/Tokenization/Strategies/StreamingHintStrategyTests.cs src/Tokenizer/Tokenizer.cs
git commit -m "refactor: rename IntegratedHintStrategy to StreamingHintStrategy with buffer scanning"
```

---

### Task 5: Wire `OnBufferFilled` into `TokenizationSession` and Remove `OnTokenMatched` from `TokenMatchRouter`

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationSession.cs`
- Modify: `src/Tokenizer/Tokenization/TokenMatchRouter.cs`

- [ ] **Step 1: Store `IHintStrategy?` in `TokenizationSession`**

In `TokenizationSession.cs`, add a field after `_candidateProcessor` (line 20):

```csharp
    private readonly IHintStrategy? _hintStrategy;
```

In the constructor, store it (after line 33, before the `_pipeline` assignment):

```csharp
        _hintStrategy = hintStrategy;
```

- [ ] **Step 2: Call `OnBufferFilled` in `Run` after `FillBuffer`**

In the `Run` method, after `context.Enumerator.FillBuffer();` (line 52), add:

```csharp
            _hintStrategy?.OnBufferFilled(context.Enumerator.StagingBuffer, context.Enumerator.LastReadCount);
```

The `do` loop should now look like:

```csharp
        do
        {
            context.Enumerator.FillBuffer();
            _hintStrategy?.OnBufferFilled(context.Enumerator.StagingBuffer, context.Enumerator.LastReadCount);

            if (_template.Options.MaxInputLength > 0 &&
                context.Enumerator.TotalCharactersSeen > _template.Options.MaxInputLength)
            {
                throw new TokenizerException(
                    $"Input length exceeds maximum allowed length of {_template.Options.MaxInputLength.ToInvariant("N0")}. " +
                    "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
            }
        }
        while (!ProcessChunk(context, CancellationToken.None));
```

- [ ] **Step 3: Call `OnBufferFilled` in `RunAsync` after `FillBufferAsync`**

In the `RunAsync` method, after `await context.Enumerator.FillBufferAsync(ct).ConfigureAwait(false);` (line 76), add:

```csharp
            _hintStrategy?.OnBufferFilled(context.Enumerator.StagingBuffer, context.Enumerator.LastReadCount);
```

- [ ] **Step 4: Remove hint strategy from `TokenMatchRouter`**

In `TokenMatchRouter.cs`:

First, remove the `_hintStrategy` field (line 14):
```csharp
    private readonly IHintStrategy? _hintStrategy;
```

Remove the `hintStrategy` constructor parameter and its assignment. The constructor changes from:
```csharp
    public TokenMatchRouter(
        Template template,
        CandidateProcessor candidateProcessor,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy)
    {
        _template = template;
        _candidateProcessor = candidateProcessor;
        _collector = collector;
        _hintStrategy = hintStrategy;
    }
```

To:
```csharp
    public TokenMatchRouter(
        Template template,
        CandidateProcessor candidateProcessor,
        IDiagnosticCollector collector)
    {
        _template = template;
        _candidateProcessor = candidateProcessor;
        _collector = collector;
    }
```

Remove the `OnTokenMatched` forwarding block (lines 67-73):
```csharp
            // Notify hint strategy of matched tokens
            if (_hintStrategy != null)
            {
                foreach (var match in context.MatchBuffer)
                {
                    _hintStrategy.OnTokenMatched(match);
                }
            }
```

Then update `TokenizationSession.cs` where `TokenMatchRouter` is constructed (around line 39). Change:
```csharp
        _router = new TokenMatchRouter(
            template, _candidateProcessor, collector, hintStrategy);
```

To:
```csharp
        _router = new TokenMatchRouter(
            template, _candidateProcessor, collector);
```

Also update `TokenMatchRouterTests.cs` — all four occurrences of the constructor call pass `hintStrategy: null`. Remove that parameter from each call. For example, change:
```csharp
            NullDiagnosticCollector.Instance, hintStrategy: null);
```
To:
```csharp
            NullDiagnosticCollector.Instance);
```

- [ ] **Step 5: Build and run all tests**

Run:
```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: Build succeeds, all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationSession.cs src/Tokenizer/Tokenization/TokenMatchRouter.cs
git commit -m "refactor: wire OnBufferFilled into TokenizationSession, remove OnTokenMatched from router"
```

---

### Task 6: Extract `ReadToEndBoundedAsync` to `TextReaderExtensions`

**Files:**
- Create: `src/Tokenizer/Extensions/TextReaderExtensions.cs`
- Create: `tests/Tokenizer.Tests/Extensions/TextReaderExtensionsTests.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`

- [ ] **Step 1: Write `TextReaderExtensionsTests.cs`**

```csharp
using System.Text;
using Tokens.Exceptions;
using Tokens.Extensions;
using Xunit;

namespace Tokens.Tests.Extensions;

public class TextReaderExtensionsTests
{
    [Fact]
    public async Task GivenShortInput_WhenReadToEndBoundedAsync_ThenReturnsFullContent()
    {
        // Arrange
        using var reader = new StringReader("Hello World");

        // Act
        var result = await reader.ReadToEndBoundedAsync(maxLength: 100, CancellationToken.None);

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task GivenInputExceedingMaxLength_WhenReadToEndBoundedAsync_ThenThrowsTokenizerException()
    {
        // Arrange
        var longInput = new string('x', 200);
        using var reader = new StringReader(longInput);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TokenizerException>(
            () => reader.ReadToEndBoundedAsync(maxLength: 100, CancellationToken.None));
        Assert.Contains("exceeds maximum allowed length", ex.Message);
    }

    [Fact]
    public async Task GivenZeroMaxLength_WhenReadToEndBoundedAsync_ThenReadsWithoutLimit()
    {
        // Arrange
        var longInput = new string('x', 10_000);
        using var reader = new StringReader(longInput);

        // Act
        var result = await reader.ReadToEndBoundedAsync(maxLength: 0, CancellationToken.None);

        // Assert
        Assert.Equal(longInput, result);
    }

    [Fact]
    public async Task GivenEmptyReader_WhenReadToEndBoundedAsync_ThenReturnsEmptyString()
    {
        // Arrange
        using var reader = new StringReader(string.Empty);

        // Act
        var result = await reader.ReadToEndBoundedAsync(maxLength: 100, CancellationToken.None);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GivenCancelledToken_WhenReadToEndBoundedAsync_ThenThrowsOperationCancelled()
    {
        // Arrange
        var longInput = new string('x', 10_000);
        using var reader = new StringReader(longInput);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => reader.ReadToEndBoundedAsync(maxLength: 0, cts.Token));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TextReaderExtensionsTests" 2>&1 | tail -5
```

Expected: Build failure — `TextReaderExtensions` does not exist.

- [ ] **Step 3: Create `TextReaderExtensions.cs`**

```csharp
using System.Text;
using Tokens.Exceptions;

namespace Tokens.Extensions;

/// <summary>
/// Extension methods for <see cref="TextReader"/>.
/// </summary>
internal static class TextReaderExtensions
{
    /// <summary>
    /// Asynchronously reads all content from the <paramref name="reader"/>, enforcing
    /// a maximum character length if <paramref name="maxLength"/> is greater than zero.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="maxLength">Maximum allowed length. Zero or negative disables the limit.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>The full content of the reader as a string.</returns>
    /// <exception cref="TokenizerException">Thrown when the content exceeds <paramref name="maxLength"/>.</exception>
    public static async Task<string> ReadToEndBoundedAsync(this TextReader reader, int maxLength, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var buffer = new char[4096];
        int read;
#if NET8_0_OR_GREATER
        while ((read = await reader.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false)) > 0)
#else
        while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
#endif
        {
            ct.ThrowIfCancellationRequested();
            sb.Append(buffer, 0, read);
            if (maxLength > 0 && sb.Length > maxLength)
            {
                throw new TokenizerException(
                    $"Template length {sb.Length.ToInvariant("N0")} exceeds maximum allowed length of {maxLength.ToInvariant("N0")}. " +
                    "Increase TokenizerOptions.MaxTemplateLength to allow larger templates.");
            }
        }
        return sb.ToString();
    }
}
```

- [ ] **Step 4: Update `Tokenizer.cs` to use extension method**

In `src/Tokenizer/Tokenizer.cs`, replace the `CompileAsync(TextReader, ...)` method body:

Change:
```csharp
    public async Task<CompilationResult> CompileAsync(TextReader reader, CancellationToken ct = default)
    {
        var content = await ReadToEndAsync(reader, ct, Options.MaxTemplateLength).ConfigureAwait(false);
        return _parser.Compile(content);
    }
```

To:
```csharp
    public async Task<CompilationResult> CompileAsync(TextReader reader, CancellationToken ct = default)
    {
        var content = await reader.ReadToEndBoundedAsync(Options.MaxTemplateLength, ct).ConfigureAwait(false);
        return _parser.Compile(content);
    }
```

Then delete the entire `ReadToEndAsync` private method (lines 263-284).

- [ ] **Step 5: Run tests**

Run:
```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TextReaderExtensionsTests"
```

Expected: All 5 tests pass.

- [ ] **Step 6: Run all tests to verify no regressions**

Run:
```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Extensions/TextReaderExtensions.cs tests/Tokenizer.Tests/Extensions/TextReaderExtensionsTests.cs src/Tokenizer/Tokenizer.cs
git commit -m "refactor: extract ReadToEndBoundedAsync to TextReaderExtensions"
```

---

### Task 7: Unify `TokenizeCore`/`TokenizeAsyncCore` into `RunCoreAsync` and Reorder Class

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`

- [ ] **Step 1: Replace `Tokenizer.cs` with unified and reordered version**

Replace the full contents of `src/Tokenizer/Tokenizer.cs` with:

```csharp
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Tokenization;
using Tokens.Tokenization.Strategies;

namespace Tokens;

/// <summary>
/// Class that creates objects and populates their properties with values
/// from input strings
/// </summary>
public sealed class Tokenizer : ITokenizer
{
    private readonly TemplateCompiler _parser;
    private readonly ILogger<Tokenizer> _log;
    private readonly ITokenizationEngine _tokenizationEngine;
    private readonly IResultBuilder _resultBuilder;

    /// <summary>Gets the options.</summary>
    public TokenizerOptions Options { get; }

    /// <summary>
    /// Creates a new Tokenizer with default options.
    /// </summary>
    public Tokenizer() : this(new TokenizerOptions())
    {
    }

    /// <summary>
    /// Creates a new Tokenizer with the specified options.
    /// </summary>
    public Tokenizer(TokenizerOptions options) : this(options, loggerFactory: null)
    {
    }

    /// <summary>
    /// Creates a new Tokenizer with the specified options and logger factory.
    /// </summary>
    public Tokenizer(TokenizerOptions options, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        Options = options with { };
        _log = loggerFactory.CreateLogger<Tokenizer>();
        _parser = new TemplateCompiler(Options, loggerFactory);
        _tokenizationEngine = new TokenizationEngine(loggerFactory.CreateLogger<TokenizationEngine>());
        _resultBuilder = new ResultBuilder(loggerFactory.CreateLogger<ResultBuilder>());
    }

    /// <summary>
    /// Internal constructor for dependency injection.
    /// </summary>
    internal Tokenizer(
        IOptions<TokenizerOptions> options,
        ILogger<Tokenizer> logger,
        TemplateCompiler parser,
        ITokenizationEngine tokenizationEngine,
        IResultBuilder resultBuilder)
    {
        Options = options.Value with { };
        _log = logger;
        _parser = parser;
        _tokenizationEngine = tokenizationEngine;
        _resultBuilder = resultBuilder;
    }

    /// <inheritdoc />
    public CompilationResult Compile(string pattern) => _parser.Compile(pattern);

    /// <inheritdoc />
    public async Task<CompilationResult> CompileAsync(TextReader reader, CancellationToken ct = default)
    {
        var content = await reader.ReadToEndBoundedAsync(Options.MaxTemplateLength, ct).ConfigureAwait(false);
        return _parser.Compile(content);
    }

    /// <inheritdoc />
    public async Task<CompilationResult> CompileAsync(Stream input, Encoding encoding, CancellationToken ct = default)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await CompileAsync(reader, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Tokenizes the <paramref name="input"/> string using the provided compiled <paramref name="template"/>.
    /// </summary>
    /// <param name="template">The compiled template to match against.</param>
    /// <param name="input">The input text to extract values from.</param>
    /// <returns>A <see cref="TokenizeResult"/> containing the matched and unmatched tokens.</returns>
    public TokenizeResult Tokenize(Template template, string input)
    {
        var result = new TokenizeResult(template);

        // template.Options reflects merged instance + front matter overrides — intentionally
        // used instead of this.Options so per-template front matter settings take effect.
        if (template.Options.MaxInputLength > 0 && input.Length > template.Options.MaxInputLength)
        {
            throw new TokenizerException(
                $"Input length {input.Length.ToInvariant("N0")} exceeds maximum allowed length of {template.Options.MaxInputLength.ToInvariant("N0")}. " +
                "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
        }

        // Sync path: RunCoreAsync completes synchronously when rawInput is non-null
        // (no awaits are hit). GetAwaiter().GetResult() unwraps exceptions correctly.
        RunCoreAsync(result, template, new StringReader(input), input, CancellationToken.None)
            .GetAwaiter().GetResult();

        return result;
    }

    /// <summary>
    /// Tokenizes the <paramref name="input"/> string using the provided compiled <paramref name="template"/>,
    /// mapping extracted values onto a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to populate with extracted values.</typeparam>
    /// <param name="template">The compiled template to match against.</param>
    /// <param name="input">The input text to extract values from.</param>
    /// <returns>A new instance of <typeparamref name="T"/> with populated properties, or null if matching fails.</returns>
    public T? Tokenize<T>(Template template, string input) where T : class, new()
    {
        var result = Tokenize(template, input);
        if (!result.Success) return null;
        return result.Assign<T>();
    }

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/> using a pre-compiled template.
    /// </summary>
    /// <remarks>
    /// Hint matching in streaming mode scans buffer contents incrementally rather than
    /// searching the full input. Alignment rendering in diagnostics is unavailable.
    /// </remarks>
    public async Task<TokenizeResult> TokenizeAsync(Template template, TextReader input, CancellationToken ct = default)
    {
        var result = new TokenizeResult(template);
        await RunCoreAsync(result, template, input, rawInput: null, ct).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/>, mapping values onto a new <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Hint matching in streaming mode scans buffer contents incrementally rather than
    /// searching the full input. Alignment rendering in diagnostics is unavailable.
    /// </remarks>
    public async Task<T?> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct = default) where T : class, new()
    {
        var result = await TokenizeAsync(template, input, ct).ConfigureAwait(false);
        if (!result.Success) return null;
        return result.Assign<T>();
    }

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/> using a pre-compiled template.
    /// </summary>
    /// <remarks>
    /// Hint matching in streaming mode scans buffer contents incrementally rather than
    /// searching the full input. Alignment rendering in diagnostics is unavailable.
    /// </remarks>
    public async Task<TokenizeResult> TokenizeAsync(Template template, Stream input, Encoding encoding, CancellationToken ct = default)
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await TokenizeAsync(template, reader, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/>, mapping values onto a new <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Hint matching in streaming mode scans buffer contents incrementally rather than
    /// searching the full input. Alignment rendering in diagnostics is unavailable.
    /// </remarks>
    public async Task<T?> TokenizeAsync<T>(Template template, Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new()
    {
        using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        return await TokenizeAsync<T>(template, reader, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Unified tokenization core. Handles both sync and async paths.
    /// Sync callers pass <paramref name="rawInput"/> (non-null) and the method completes synchronously.
    /// Async callers pass <paramref name="rawInput"/> as null and await the result.
    /// </summary>
    private async Task RunCoreAsync(
        TokenizeResult result, Template template, TextReader reader,
        string? rawInput, CancellationToken ct)
    {
        var isSync = rawInput != null;
        IHintStrategy hintStrategy = isSync
            ? new UpfrontHintStrategy()
            : new StreamingHintStrategy();

        var scopeProperties = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["TemplateName"] = template.Name,
            ["TokenCount"] = template.Tokens.Count,
            ["Operation"] = isSync ? "Tokenize" : "TokenizeAsync",
        };

        if (rawInput != null)
        {
            scopeProperties["InputLength"] = rawInput.Length;
        }

        using (_log.BeginScope(scopeProperties))
        {
            try
            {
                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug("Starting tokenization for template {TemplateName}", template.Name);
                    if (rawInput != null)
                    {
                        _log.LogDebug("Template has {TokenCount} tokens, input length is {InputLength}",
                            template.Tokens.Count, rawInput.Length);
                    }
                    else
                    {
                        _log.LogDebug("Template has {TokenCount} tokens", template.Tokens.Count);
                    }
                }

                // Create and initialize the tokenization context
                var context = new TokenizationContext();
                context.Initialize(reader);

                IDiagnosticCollector collector = template.Options.EnableDiagnostics
                    ? new DiagnosticCollector(rawInput)
                    : NullDiagnosticCollector.Instance;

                // Process hints — upfront for sync (string.Contains), no-op for async
                var hintsMissing = hintStrategy.PreProcess(template, context.Enumerator, rawInput, result, collector);

                if (hintsMissing)
                {
                    _log.LogWarning("Required hints are missing, skipping tokenization");
                }
                else
                {
                    var session = _tokenizationEngine.CreateSession(template, result, collector, hintStrategy);

                    if (isSync)
                    {
                        session.Run(context);
                    }
                    else
                    {
                        await session.RunAsync(context, ct).ConfigureAwait(false);
                    }

                    if (hintStrategy.PostProcess(result))
                    {
                        _log.LogWarning("Post-tokenization hint check failed");
                    }
                }

                FinalizeTokenization(result, template, collector, rawInput);

                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug("Tokenization {Result} for template {TemplateName}",
                        result.Success ? "succeeded" : "failed", template.Name);
                }
            }
            catch (OperationCanceledException)
            {
                _log.LogWarning("Async tokenization cancelled for template {TemplateName}", template.Name);
                throw;
            }
            catch (TokenizerException ex)
            {
                _log.LogError(ex, "Tokenization failed for template {TemplateName}: {Message}",
                    template.Name, ex.Message);
                throw;
            }
        }
    }

    private void FinalizeTokenization(
        TokenizeResult result, Template template,
        IDiagnosticCollector collector, string? rawInput)
    {
        _resultBuilder.BuildUnmatchedTokens(template, result, collector);

        var requiredMissingCount = result.Tokens.Misses.Count(t => t.IsRequired);
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Tokenization complete: {MatchCount} matches, {MissCount} misses, {RequiredMissing} required missing",
                result.Tokens.Matches.Count, result.Tokens.Misses.Count, requiredMissingCount);
        }

        if (requiredMissingCount > 0)
        {
            _log.LogWarning("{RequiredMissing} required tokens were missing", requiredMissingCount);
        }

        result.Diagnostics = collector.GetResult();

        if (result.Diagnostics != null)
        {
            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("{Verdict}", result.Diagnostics.Summary.Verdict);
            }
            foreach (var issue in result.Diagnostics.Summary.Issues)
            {
                _log.LogWarning("Token '{TokenName}': {Description}", issue.TokenName, issue.Description);
                if (issue.Hint != null)
                {
                    _log.LogWarning("  → Hint: {Hint}", issue.Hint);
                }
            }
            if (rawInput != null && _log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
            }
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run:
```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```

Expected: Build succeeds.

- [ ] **Step 3: Run all tests**

Run:
```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs
git commit -m "refactor: unify TokenizeCore/TokenizeAsyncCore into RunCoreAsync, reorder class members"
```

---

### Task 8: Final Verification

**Files:**
- None modified

- [ ] **Step 1: Run all tests one final time**

Run:
```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 2: Run full benchmark suite**

Run:
```bash
cd benchmarks/Tokenizer.Benchmarks && dotnet run -c Release -- --filter '*' --artifacts ../../../benchmark-results/after-unification
```

- [ ] **Step 3: Compare benchmark results**

Compare `benchmark-results/baseline/` against `benchmark-results/after-unification/`. Look for:
- Any benchmark that regressed more than 5% in mean execution time
- Any benchmark that shows significantly more allocations

If regressions are found, investigate and fix before considering the refactoring complete.

- [ ] **Step 4: Verify no stale files remain**

Run:
```bash
git status
```

Ensure no leftover `ContainsHintStrategy.cs` or `IntegratedHintStrategy.cs` files remain.
