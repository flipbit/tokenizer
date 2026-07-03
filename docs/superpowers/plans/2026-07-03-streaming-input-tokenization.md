# Streaming Input Tokenization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `TokenEnumerator` to read from `TextReader` natively, add `TextReader`/`Stream` overloads to `ITokenizer` and `ITokenMatcher`, and benchmark four hint strategies to replace the current two-pass hint processor.

**Architecture:** `TokenEnumerator` switches from `string` indexing to `TextReader.Read()` with a pushback `Queue<char>` for non-advancing `TryMatch`. All line endings (`\r\n`, `\r`, `\n`) normalize to `\n` inline. String inputs wrap via `StringReader`; `Stream` inputs wrap via `StreamReader(stream, encoding, leaveOpen: true)`. Four `IHintStrategy` implementations are benchmarked; the winner replaces `IHintProcessor`.

**Tech Stack:** C# (.NET Standard 2.0 / .NET 8.0), xUnit, NSubstitute, BenchmarkDotNet

**Branch:** Create `feature/streaming-input` from `v3` before starting.

---

### Task 0: Create feature branch

**Files:**
- None (git only)

- [ ] **Step 1: Create the feature branch**

```bash
git checkout -b feature/streaming-input v3
```

- [ ] **Step 2: Verify branch**

Run: `git branch --show-current`
Expected: `feature/streaming-input`

---

### Task 1: Refactor TokenEnumerator to TextReader-native

Replace `string` indexing with `TextReader.Read()` and a pushback buffer. All CRLF normalization happens inline.

**Files:**
- Modify: `src/Tokenizer/Enumerators/TokenEnumerator.cs`
- Test: `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorCharTests.cs`
- Test: `tests/Tokenizer.Tests/Enumerators/TokenEnumeratorMatchTests.cs`

- [ ] **Step 1: Write failing tests for TextReader constructor and inline CRLF normalization**

Add these tests to `TokenEnumeratorCharTests.cs`:

```csharp
[Fact]
public void GivenTextReader_WhenNext_ThenReturnsCharsInOrder()
{
    // Arrange
    var enumerator = new TokenEnumerator(new StringReader("abc"));

    // Act / Assert
    Assert.Equal('a', enumerator.Next());
    Assert.Equal('b', enumerator.Next());
    Assert.Equal('c', enumerator.Next());
    Assert.Equal('\0', enumerator.Next());
}

[Fact]
public void GivenInputWithCRLF_WhenNext_ThenNormalizesToLF()
{
    // Arrange
    var enumerator = new TokenEnumerator(new StringReader("a\r\nb"));

    // Act / Assert
    Assert.Equal('a', enumerator.Next());
    Assert.Equal('\n', enumerator.Next());
    Assert.Equal('b', enumerator.Next());
}

[Fact]
public void GivenInputWithLoneCR_WhenNext_ThenNormalizesToLF()
{
    // Arrange
    var enumerator = new TokenEnumerator(new StringReader("a\rb"));

    // Act / Assert
    Assert.Equal('a', enumerator.Next());
    Assert.Equal('\n', enumerator.Next());
    Assert.Equal('b', enumerator.Next());
}

[Fact]
public void GivenInputWithLF_WhenNext_ThenReturnsLF()
{
    // Arrange
    var enumerator = new TokenEnumerator(new StringReader("a\nb"));

    // Act / Assert
    Assert.Equal('a', enumerator.Next());
    Assert.Equal('\n', enumerator.Next());
    Assert.Equal('b', enumerator.Next());
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenEnumeratorCharTests"`
Expected: FAIL — `TokenEnumerator` has no `TextReader` constructor

- [ ] **Step 3: Implement TextReader-native TokenEnumerator**

Replace the contents of `src/Tokenizer/Enumerators/TokenEnumerator.cs` with:

```csharp
using System.IO;

namespace Tokens.Enumerators;

/// <summary>
/// A forward-only, character-level enumerator over a <see cref="TextReader"/> that tracks the current
/// <see cref="FileLocation"/> (line and column) as it advances. All line endings
/// (<c>\r\n</c>, <c>\r</c>, <c>\n</c>) are normalised to <c>\n</c>.
/// </summary>
public class TokenEnumerator
{
    private readonly TextReader reader;
    private readonly Queue<char> pushback = new();

    private bool isEmpty;
    private bool resetNextLine;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenEnumerator"/> over the specified <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">The text reader to enumerate.</param>
    public TokenEnumerator(TextReader reader)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        isEmpty = reader.Peek() == -1;
        Location = new FileLocation();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenEnumerator"/> over the specified string.
    /// </summary>
    /// <param name="pattern">The string to enumerate.</param>
    public TokenEnumerator(string pattern)
        : this(new StringReader(pattern ?? string.Empty))
    {
    }

    /// <summary>
    /// Gets a value indicating whether all characters have been consumed.
    /// </summary>
    public bool IsEmpty => isEmpty && pushback.Count == 0;

    /// <summary>
    /// Gets the current position as a line/column <see cref="FileLocation"/>.
    /// </summary>
    public FileLocation Location { get; }

    /// <summary>
    /// Advances the enumerator by one character and returns it, updating <see cref="Location"/>.
    /// Returns <c>'\0'</c> if all characters have been consumed.
    /// </summary>
    public char Next()
    {
        var next = ReadChar();
        if (next == '\0') return '\0';

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

    /// <summary>
    /// Returns the next character without advancing the enumerator.
    /// Returns <c>'\0'</c> if all characters have been consumed.
    /// </summary>
    public char Peek()
    {
        if (pushback.Count > 0) return pushback.Peek();

        var raw = reader.Peek();
        if (raw == -1) return '\0';

        // If we see \r, we need to resolve CRLF — read through ReadChar
        // and push into pushback so we return the normalized character
        if (raw == '\r')
        {
            var normalized = ReadChar();
            if (normalized != '\0')
            {
                pushback.Enqueue(normalized);
            }
            return normalized;
        }

        return (char)raw;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the characters starting at the current position match <paramref name="value"/>
    /// exactly, without advancing the enumerator.
    /// </summary>
    /// <param name="value">The string to compare against the current position.</param>
    public bool TryMatch(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        if (IsEmpty) return false;

        // Ensure pushback has enough characters for comparison
        EnsurePushback(value.Length);

        if (pushback.Count < value.Length) return false;

        // Compare pushback contents against value
        var i = 0;
        foreach (var c in pushback)
        {
            if (i >= value.Length) break;
            if (c != value[i]) return false;
            i++;
        }

        return i == value.Length;
    }

    /// <summary>
    /// Checks which of the given tokens have a preamble that matches the text at the current position,
    /// populating <paramref name="matches"/> with every token whose preamble is found.
    /// When matching an out-of-order template, tokens without a name are skipped.
    /// </summary>
    /// <param name="tokens">The tokens whose preambles should be tested.</param>
    /// <param name="outOfOrderTokens">
    /// When <see langword="true"/>, tokens without a name are excluded from consideration.
    /// </param>
    /// <param name="matches">A list that is cleared and then populated with every token whose preamble matches.</param>
    /// <returns><see langword="true"/> if at least one token's preamble matched; otherwise <see langword="false"/>.</returns>
    public bool TryMatch(IEnumerable<Token> tokens, bool outOfOrderTokens, IList<Token> matches)
    {
        matches.Clear();

        foreach (var token in tokens)
        {
            // Special case: if matching out of order template,
            // don't match any tokens without a value
            if (outOfOrderTokens && string.IsNullOrWhiteSpace(token.Name))
            {
                continue;
            }

            if (TryMatch(token.Preamble))
            {
                matches.Add(token);
            }

            if (token.IsOptional == false) break;
        }

        return matches.Count > 0;
    }

    /// <summary>
    /// Advances the enumerator by the specified number of characters, consuming each one.
    /// </summary>
    /// <param name="count">The number of characters to advance.</param>
    public void Advance(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Next();
        }
    }

    /// <summary>
    /// Resets the enumerator to the beginning of the input and clears the tracked <see cref="Location"/>.
    /// Only supported for seekable readers (e.g. <see cref="StringReader"/>).
    /// </summary>
    /// <exception cref="NotSupportedException">The underlying reader does not support seeking.</exception>
    public void Reset()
    {
        throw new NotSupportedException(
            "Reset is not supported on TextReader-based enumerators. " +
            "Use a hint strategy that does not require enumerator reset.");
    }

    /// <summary>
    /// Reads the next character from the reader or pushback buffer, normalizing line endings.
    /// All <c>\r\n</c> and lone <c>\r</c> sequences are converted to <c>\n</c>.
    /// Returns <c>'\0'</c> at end of input.
    /// </summary>
    private char ReadChar()
    {
        int raw;

        if (pushback.Count > 0)
        {
            // Pushback chars are already normalized
            return pushback.Dequeue();
        }

        raw = reader.Read();

        if (raw == -1)
        {
            isEmpty = true;
            return '\0';
        }

        if (raw == '\r')
        {
            // Consume \n if it follows \r (CRLF), otherwise lone \r becomes \n
            var next = reader.Peek();
            if (next == '\n')
            {
                reader.Read(); // consume the \n
            }
            return '\n';
        }

        return (char)raw;
    }

    /// <summary>
    /// Ensures the pushback buffer has at least <paramref name="count"/> characters
    /// by reading ahead from the reader.
    /// </summary>
    private void EnsurePushback(int count)
    {
        while (pushback.Count < count)
        {
            var raw = reader.Read();
            if (raw == -1)
            {
                isEmpty = true;
                return;
            }

            if (raw == '\r')
            {
                var next = reader.Peek();
                if (next == '\n')
                {
                    reader.Read();
                }
                pushback.Enqueue('\n');
            }
            else
            {
                pushback.Enqueue((char)raw);
            }
        }
    }
}
```

- [ ] **Step 4: Run new tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenEnumeratorCharTests"`
Expected: PASS

- [ ] **Step 5: Update existing tests that use `Peek(int offset)`**

The existing test `GivenNonEmptyInput_WhenPeekWithOffset_ThenReturnsCorrectChar` and `GivenNonEmptyInput_WhenPeekBeyondEnd_ThenReturnsNullChar` in `TokenEnumeratorCharTests.cs` use `Peek(int offset)` which has been removed. Remove these two tests — the functionality is replaced by the pushback buffer internally and no public consumer needs offset peek.

- [ ] **Step 6: Run ALL tests to verify nothing is broken**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Enumerators/TokenEnumerator.cs tests/Tokenizer.Tests/Enumerators/TokenEnumeratorCharTests.cs
git commit -m "Refactor TokenEnumerator to read from TextReader with inline CRLF normalization"
```

---

### Task 2: Remove HandleWindowsNewlines from TokenizationEngine

The enumerator now normalizes CRLF inline, so the engine's `HandleWindowsNewlines` method and its call site are dead code.

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`

- [ ] **Step 1: Remove `HandleWindowsNewlines` method**

Delete the `HandleWindowsNewlines` method (lines 613-623 in `TokenizationEngine.cs`):

```csharp
// DELETE THIS METHOD:
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

- [ ] **Step 2: Remove the call to `HandleWindowsNewlines` in the main loop**

In `ProcessTokenization`, around line 127, change:

```csharp
var next = context.Enumerator.Peek();

// Handle Windows new lines (normalize to Unix)
next = HandleWindowsNewlines(context.Enumerator, next);
```

To:

```csharp
var next = context.Enumerator.Peek();
```

- [ ] **Step 3: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationEngine.cs
git commit -m "Remove HandleWindowsNewlines from TokenizationEngine — enumerator handles CRLF inline"
```

---

### Task 3: Update TokenizationContext to accept TextReader

Change `TokenizationContext.Initialize` to accept a `TextReader` instead of a `string`.

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationContext.cs`
- Modify: `src/Tokenizer/Tokenization/ITokenizationContext.cs` (if Initialize is on the interface)
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs` (call site)
- Modify: `src/Tokenizer/Tokenizer.cs` (call site)

- [ ] **Step 1: Check if `Initialize` is on `ITokenizationContext`**

Run: `grep -n "Initialize" src/Tokenizer/Tokenization/ITokenizationContext.cs`

If it is, update both the interface and implementation.

- [ ] **Step 2: Change `TokenizationContext.Initialize` to accept `TextReader`**

In `TokenizationContext.cs`, change:

```csharp
public void Initialize(string input)
{
    if (string.IsNullOrEmpty(input))
        throw new ArgumentException("Input cannot be null or empty", nameof(input));

    Enumerator = new TokenEnumerator(input);
    Reset();
}
```

To:

```csharp
public void Initialize(TextReader reader)
{
    ArgumentValidation.ThrowIfNull(reader, nameof(reader));

    Enumerator = new TokenEnumerator(reader);
    Reset();
}
```

Add `using System.IO;` to the top of the file.

- [ ] **Step 3: Update `TokenizationEngine.ProcessTokenization` to pass `TextReader`**

In `TokenizationEngine.cs`, the `ProcessTokenization` method calls `context.Initialize(input)`. The `input` parameter is a `string`. For now, wrap it:

Change the call at line 101:

```csharp
context.Initialize(input);
```

To:

```csharp
context.Initialize(new StringReader(input));
```

Add `using System.IO;` if not already present.

- [ ] **Step 4: Update `Tokenizer.Tokenize` to pass `TextReader`**

In `Tokenizer.cs`, the private `Tokenize` method calls `context.Initialize(input)` at line 164. Change it the same way:

```csharp
context.Initialize(new StringReader(input));
```

- [ ] **Step 5: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationContext.cs src/Tokenizer/Tokenization/TokenizationEngine.cs src/Tokenizer/Tokenizer.cs
git commit -m "Update TokenizationContext.Initialize to accept TextReader"
```

---

### Task 4: Refactor engine to remove string input parameter

`TokenizationEngine.ProcessTokenization` currently takes `string input` but only uses `input.Length` for logging and max-iterations. Replace with an `int inputLength` parameter, or move length checks to the caller. This decouples the engine from strings.

**Files:**
- Modify: `src/Tokenizer/Tokenization/ITokenizationEngine.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- Modify: `src/Tokenizer/Tokenizer.cs` (call site)

- [ ] **Step 1: Change `ITokenizationEngine.ProcessTokenization` signature**

In `ITokenizationEngine.cs`, change:

```csharp
void ProcessTokenization(Template template, string input, object? targetObject, ITokenizationContext context, TokenizeResultBase result, IDiagnosticCollector collector);
```

To:

```csharp
void ProcessTokenization(Template template, int inputLength, object? targetObject, ITokenizationContext context, TokenizeResultBase result, IDiagnosticCollector collector);
```

- [ ] **Step 2: Update `TokenizationEngine.ProcessTokenization` implementation**

Change the method signature to accept `int inputLength` instead of `string input`. Replace all uses of `input.Length` with `inputLength`. Replace `ArgumentValidation.ThrowIfNull(input, ...)` with a range check if needed (or remove — length of 0 is valid for empty reader).

The three usages to change:
- `template.Name, input.Length` → `template.Name, inputLength` (log line ~96)
- `Input length: {input.Length}` → `Input length: {inputLength}` (diagnostics ~99)
- `input.Length * 2` → `inputLength * 2` (max iterations ~110)

- [ ] **Step 3: Update `Tokenizer.Tokenize` call site**

In `Tokenizer.cs`, change:

```csharp
tokenizationEngine.ProcessTokenization(template, input, value, context, result, collector);
```

To:

```csharp
tokenizationEngine.ProcessTokenization(template, input.Length, value, context, result, collector);
```

- [ ] **Step 4: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenization/ITokenizationEngine.cs src/Tokenizer/Tokenization/TokenizationEngine.cs src/Tokenizer/Tokenizer.cs
git commit -m "Replace string input with int inputLength in TokenizationEngine"
```

---

### Task 5: Add IHintStrategy interface and EnumeratorScanHintStrategy (baseline)

Extract the current hint processing logic into the first strategy implementation.

**Files:**
- Create: `src/Tokenizer/Tokenization/IHintStrategy.cs`
- Create: `src/Tokenizer/Tokenization/Strategies/EnumeratorScanHintStrategy.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Strategies/EnumeratorScanHintStrategyTests.cs`

- [ ] **Step 1: Create `IHintStrategy` interface**

Create `src/Tokenizer/Tokenization/IHintStrategy.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Defines a strategy for processing template hints during tokenization.
/// Strategies differ in when and how they check for required hint strings in the input.
/// </summary>
internal interface IHintStrategy
{
    /// <summary>
    /// Pre-tokenization hint processing. Some strategies do their full check here.
    /// Returns <see langword="true"/> if required hints are missing and tokenization should be skipped.
    /// </summary>
    /// <param name="template">The template containing hint definitions.</param>
    /// <param name="enumerator">The token enumerator positioned at the start of input.</param>
    /// <param name="rawInput">The original input string, if available. Null for TextReader-only inputs.</param>
    /// <param name="result">The result object to populate with hint matches and misses.</param>
    /// <param name="collector">The diagnostic collector.</param>
    bool PreProcess(Template template, TokenEnumerator enumerator,
                    string? rawInput, TokenizeResultBase result, IDiagnosticCollector collector);

    /// <summary>
    /// Called by the engine when a token preamble is matched during tokenization.
    /// Single-pass strategies use this to track hint satisfaction.
    /// </summary>
    void OnTokenMatched(Token token);

    /// <summary>
    /// Post-tokenization hint evaluation. Single-pass strategies check results here.
    /// Returns <see langword="true"/> if required hints are missing.
    /// </summary>
    bool PostProcess(TokenizeResultBase result);
}
```

- [ ] **Step 2: Write failing test for EnumeratorScanHintStrategy**

Create `tests/Tokenizer.Tests/Tokenization/Strategies/EnumeratorScanHintStrategyTests.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Tokenization;
using Tokens.Tokenization.Strategies;
using Xunit;

namespace Tokens.Tests.Tokenization.Strategies;

public class EnumeratorScanHintStrategyTests
{
    [Fact]
    public void GivenNoHints_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new EnumeratorScanHintStrategy();
        var template = new TemplateBuilder().Build();
        var enumerator = new TokenEnumerator("some input");
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(missing);
    }

    [Fact]
    public void GivenRequiredHintPresent_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new EnumeratorScanHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var enumerator = new TokenEnumerator("Domain: example.com");
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(missing);
    }

    [Fact]
    public void GivenRequiredHintMissing_WhenPreProcess_ThenReturnsTrue()
    {
        // Arrange
        var strategy = new EnumeratorScanHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var enumerator = new TokenEnumerator("Name: example.com");
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(missing);
    }

    [Fact]
    public void GivenOnTokenMatched_WhenCalled_ThenIsNoOp()
    {
        // Arrange
        var strategy = new EnumeratorScanHintStrategy();
        var token = new TokenBuilder().WithPreamble("Domain:").Build();

        // Act — should not throw
        strategy.OnTokenMatched(token);
    }

    [Fact]
    public void GivenPostProcess_WhenCalled_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new EnumeratorScanHintStrategy();
        var template = new TemplateBuilder().Build();
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PostProcess(result);

        // Assert
        Assert.False(missing);
    }
}
```

Note: The test uses `TemplateBuilder` and `TokenBuilder` from the existing test builders in `tests/Tokenizer.Tests/Builders/`. Check that `TemplateBuilder` has a `WithHint` method — if not, add one:

```csharp
public TemplateBuilder WithHint(string text, bool optional = false)
{
    template.Hints.Add(new Hint(text, optional));
    return this;
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "EnumeratorScanHintStrategyTests"`
Expected: FAIL — class does not exist

- [ ] **Step 4: Implement EnumeratorScanHintStrategy**

Create `src/Tokenizer/Tokenization/Strategies/EnumeratorScanHintStrategy.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Baseline hint strategy that scans the input via the enumerator character-by-character,
/// checking each hint at each position. Two-pass: scans first, then resets enumerator
/// for tokenization. This is the original hint processing approach, kept for benchmark comparison.
/// </summary>
internal sealed class EnumeratorScanHintStrategy : IHintStrategy
{
    /// <inheritdoc />
    public bool PreProcess(
        Template template,
        TokenEnumerator enumerator,
        string? rawInput,
        TokenizeResultBase result,
        IDiagnosticCollector collector)
    {
        if (template.Hints.Count == 0) return false;

        while (enumerator.IsEmpty == false)
        {
            foreach (var hint in template.Hints)
            {
                if (string.IsNullOrEmpty(hint.Text)) continue;

                if (enumerator.TryMatch(hint.Text))
                {
                    result.Hints.AddMatch(hint, enumerator);

                    collector.Record(DiagnosticEventType.HintMatched,
                        value: hint.Text,
                        location: enumerator.Location);
                }
            }

            if (result.Hints.Matches.Count == template.Hints.Count) break;

            enumerator.Next();
        }

        foreach (var hint in template.Hints)
        {
            result.Hints.AddMiss(hint);
        }

        enumerator.Reset();

        return result.Hints.Misses.Any(h => h.Optional == false);
    }

    /// <inheritdoc />
    public void OnTokenMatched(Token token)
    {
        // No-op — this strategy does all work in PreProcess
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResultBase result)
    {
        // No-op — this strategy does all work in PreProcess
        return false;
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "EnumeratorScanHintStrategyTests"`
Expected: PASS

- [ ] **Step 6: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Tokenization/IHintStrategy.cs src/Tokenizer/Tokenization/Strategies/EnumeratorScanHintStrategy.cs tests/Tokenizer.Tests/Tokenization/Strategies/EnumeratorScanHintStrategyTests.cs
git commit -m "Add IHintStrategy interface and EnumeratorScanHintStrategy baseline"
```

---

### Task 6: Implement ContainsHintStrategy

Uses `string.Contains()` for each hint — fast pre-filter without touching the enumerator.

**Files:**
- Create: `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Strategies/ContainsHintStrategyTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/Tokenizer.Tests/Tokenization/Strategies/ContainsHintStrategyTests.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Tokenization;
using Tokens.Tokenization.Strategies;
using Xunit;

namespace Tokens.Tests.Tokenization.Strategies;

public class ContainsHintStrategyTests
{
    [Fact]
    public void GivenNoHints_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new ContainsHintStrategy();
        var template = new TemplateBuilder().Build();
        var input = "some input";
        var enumerator = new TokenEnumerator(input);
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PreProcess(template, enumerator, input, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(missing);
    }

    [Fact]
    public void GivenRequiredHintPresent_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new ContainsHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var input = "Domain: example.com";
        var enumerator = new TokenEnumerator(input);
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PreProcess(template, enumerator, input, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(missing);
    }

    [Fact]
    public void GivenRequiredHintMissing_WhenPreProcess_ThenReturnsTrue()
    {
        // Arrange
        var strategy = new ContainsHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var input = "Name: example.com";
        var enumerator = new TokenEnumerator(input);
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PreProcess(template, enumerator, input, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(missing);
    }

    [Fact]
    public void GivenOptionalHintMissing_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new ContainsHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain", optional: true)
            .Build();
        var input = "Name: example.com";
        var enumerator = new TokenEnumerator(input);
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PreProcess(template, enumerator, input, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(missing);
    }

    [Fact]
    public void GivenRawInput_WhenPreProcess_ThenEnumeratorIsNotConsumed()
    {
        // Arrange — enumerator should not be touched by ContainsHintStrategy
        var strategy = new ContainsHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var input = "Domain: example.com";
        var enumerator = new TokenEnumerator(input);
        var result = new TokenizeResult(template);

        // Act
        strategy.PreProcess(template, enumerator, input, result, NullDiagnosticCollector.Instance);

        // Assert — enumerator should still be at the start (not consumed)
        Assert.Equal('D', enumerator.Peek());
    }

    [Fact]
    public void GivenNullRawInput_WhenPreProcessWithHints_ThenThrows()
    {
        // Arrange
        var strategy = new ContainsHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var enumerator = new TokenEnumerator("Domain: example.com");
        var result = new TokenizeResult(template);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() =>
            strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ContainsHintStrategyTests"`
Expected: FAIL — class does not exist

- [ ] **Step 3: Implement ContainsHintStrategy**

Create `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Hint strategy that uses <see cref="string.Contains(string)"/> to check for each hint.
/// Uses the raw input string directly without touching the enumerator. No reset needed.
/// For TextReader-only inputs where no raw string is available, falls back to
/// reading the enumerator (consuming it, requiring a new enumerator for tokenization).
/// </summary>
internal sealed class ContainsHintStrategy : IHintStrategy
{
    /// <inheritdoc />
    public bool PreProcess(
        Template template,
        TokenEnumerator enumerator,
        string? rawInput,
        TokenizeResultBase result,
        IDiagnosticCollector collector)
    {
        if (template.Hints.Count == 0) return false;

        if (rawInput == null)
        {
            throw new InvalidOperationException(
                "ContainsHintStrategy requires raw input text. " +
                "Use IntegratedHintStrategy for TextReader-only inputs.");
        }

        foreach (var hint in template.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text)) continue;

            if (rawInput.Contains(hint.Text))
            {
                result.Hints.AddMatch(hint, enumerator);
                collector.Record(DiagnosticEventType.HintMatched, value: hint.Text);
            }
            else
            {
                result.Hints.AddMiss(hint);
            }
        }

        return result.Hints.Misses.Any(h => h.Optional == false);
    }

    /// <inheritdoc />
    public void OnTokenMatched(Token token)
    {
        // No-op
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResultBase result)
    {
        // No-op
        return false;
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ContainsHintStrategyTests"`
Expected: PASS

- [ ] **Step 5: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/IHintStrategy.cs src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs src/Tokenizer/Tokenization/Strategies/EnumeratorScanHintStrategy.cs tests/Tokenizer.Tests/Tokenization/Strategies/
git commit -m "Add ContainsHintStrategy and update IHintStrategy to pass rawInput"
```

---

### Task 7: Implement IntegratedHintStrategy (single-pass)

No separate hint phase — hints are tracked as tokens are matched during tokenization.

**Files:**
- Create: `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Strategies/IntegratedHintStrategyTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/Tokenizer.Tests/Tokenization/Strategies/IntegratedHintStrategyTests.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Tokenization;
using Tokens.Tokenization.Strategies;
using Xunit;

namespace Tokens.Tests.Tokenization.Strategies;

public class IntegratedHintStrategyTests
{
    [Fact]
    public void GivenNoHints_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new IntegratedHintStrategy();
        var template = new TemplateBuilder().Build();
        var enumerator = new TokenEnumerator("input");
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(missing);
    }

    [Fact]
    public void GivenRequiredHint_WhenTokenMatchedWithHintPreamble_ThenPostProcessReturnsFalse()
    {
        // Arrange
        var strategy = new IntegratedHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var enumerator = new TokenEnumerator("input");
        var result = new TokenizeResult(template);

        strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Simulate engine matching a token with "Domain" in its preamble
        var token = new TokenBuilder().WithPreamble("Domain: ").Build();
        strategy.OnTokenMatched(token);

        // Act
        var missing = strategy.PostProcess(result);

        // Assert
        Assert.False(missing);
    }

    [Fact]
    public void GivenRequiredHint_WhenNoMatchingTokenFound_ThenPostProcessReturnsTrue()
    {
        // Arrange
        var strategy = new IntegratedHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var enumerator = new TokenEnumerator("input");
        var result = new TokenizeResult(template);

        strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // No OnTokenMatched called — hint not found

        // Act
        var missing = strategy.PostProcess(result);

        // Assert
        Assert.True(missing);
    }

    [Fact]
    public void GivenOptionalHintOnly_WhenNoMatch_ThenPostProcessReturnsFalse()
    {
        // Arrange
        var strategy = new IntegratedHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain", optional: true)
            .Build();
        var enumerator = new TokenEnumerator("input");
        var result = new TokenizeResult(template);

        strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act
        var missing = strategy.PostProcess(result);

        // Assert
        Assert.False(missing);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IntegratedHintStrategyTests"`
Expected: FAIL

- [ ] **Step 3: Implement IntegratedHintStrategy**

Create `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Single-pass hint strategy. No separate hint phase — hints are tracked as token preambles
/// are matched during tokenization. Stream-native: no rewind needed.
/// Trade-off: performs full tokenization even for non-matching templates.
/// </summary>
internal sealed class IntegratedHintStrategy : IHintStrategy
{
    private readonly HashSet<string> matchedPreambles = new();
    private List<Hint> requiredHints = new();
    private Template? currentTemplate;

    /// <inheritdoc />
    public bool PreProcess(
        Template template,
        TokenEnumerator enumerator,
        string? rawInput,
        TokenizeResultBase result,
        IDiagnosticCollector collector)
    {
        // Store template hints for PostProcess evaluation
        currentTemplate = template;
        matchedPreambles.Clear();
        requiredHints.Clear();

        foreach (var hint in template.Hints)
        {
            if (hint.Optional == false)
            {
                requiredHints.Add(hint);
            }
        }

        // Never skip tokenization — hints are checked after
        return false;
    }

    /// <inheritdoc />
    public void OnTokenMatched(Token token)
    {
        if (!string.IsNullOrEmpty(token.Preamble))
        {
            matchedPreambles.Add(token.Preamble);
        }
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResultBase result)
    {
        if (currentTemplate == null || currentTemplate.Hints.Count == 0) return false;

        foreach (var hint in currentTemplate.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text)) continue;

            var found = matchedPreambles.Any(p => p.Contains(hint.Text));

            if (found)
            {
                result.Hints.AddMatch(hint, null!);
            }
            else
            {
                result.Hints.AddMiss(hint);
            }
        }

        return result.Hints.Misses.Any(h => h.Optional == false);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IntegratedHintStrategyTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs tests/Tokenizer.Tests/Tokenization/Strategies/IntegratedHintStrategyTests.cs
git commit -m "Add IntegratedHintStrategy — single-pass, stream-native hint processing"
```

---

### Task 8: Implement EarlyAbandonHintStrategy

Like IntegratedHintStrategy but can signal early termination when required hints cannot possibly be found.

**Files:**
- Create: `src/Tokenizer/Tokenization/Strategies/EarlyAbandonHintStrategy.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Strategies/EarlyAbandonHintStrategyTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/Tokenizer.Tests/Tokenization/Strategies/EarlyAbandonHintStrategyTests.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Tokenization;
using Tokens.Tokenization.Strategies;
using Xunit;

namespace Tokens.Tests.Tokenization.Strategies;

public class EarlyAbandonHintStrategyTests
{
    [Fact]
    public void GivenNoHints_WhenPreProcess_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new EarlyAbandonHintStrategy();
        var template = new TemplateBuilder().Build();
        var enumerator = new TokenEnumerator("input");
        var result = new TokenizeResult(template);

        // Act
        var missing = strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(missing);
    }

    [Fact]
    public void GivenAllHintsMatched_WhenPostProcess_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new EarlyAbandonHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var enumerator = new TokenEnumerator("input");
        var result = new TokenizeResult(template);

        strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);
        strategy.OnTokenMatched(new TokenBuilder().WithPreamble("Domain: ").Build());

        // Act
        var missing = strategy.PostProcess(result);

        // Assert
        Assert.False(missing);
    }

    [Fact]
    public void GivenRequiredHintMissing_WhenPostProcess_ThenReturnsTrue()
    {
        // Arrange
        var strategy = new EarlyAbandonHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var enumerator = new TokenEnumerator("input");
        var result = new TokenizeResult(template);

        strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);

        // Act
        var missing = strategy.PostProcess(result);

        // Assert
        Assert.True(missing);
    }

    [Fact]
    public void GivenAllRequiredHintsSatisfied_WhenShouldAbandon_ThenReturnsFalse()
    {
        // Arrange
        var strategy = new EarlyAbandonHintStrategy();
        var template = new TemplateBuilder()
            .WithHint("Domain")
            .Build();
        var enumerator = new TokenEnumerator("input");
        var result = new TokenizeResult(template);

        strategy.PreProcess(template, enumerator, null, result, NullDiagnosticCollector.Instance);
        strategy.OnTokenMatched(new TokenBuilder().WithPreamble("Domain: ").Build());

        // Act
        var shouldAbandon = strategy.ShouldAbandon;

        // Assert
        Assert.False(shouldAbandon);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "EarlyAbandonHintStrategyTests"`
Expected: FAIL

- [ ] **Step 3: Implement EarlyAbandonHintStrategy**

Create `src/Tokenizer/Tokenization/Strategies/EarlyAbandonHintStrategy.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Single-pass hint strategy with early termination support.
/// Tracks hint satisfaction during tokenization and exposes a <see cref="ShouldAbandon"/>
/// flag that the engine can check to stop processing early when required hints
/// cannot possibly be satisfied.
/// </summary>
internal sealed class EarlyAbandonHintStrategy : IHintStrategy
{
    private readonly HashSet<string> matchedPreambles = new();
    private readonly List<Hint> requiredHints = new();
    private Template? currentTemplate;

    /// <summary>
    /// Gets a value indicating whether tokenization should be abandoned.
    /// This is true when all tokens have been checked but required hints remain unsatisfied.
    /// The engine should check this property periodically during tokenization.
    /// </summary>
    public bool ShouldAbandon { get; private set; }

    /// <inheritdoc />
    public bool PreProcess(
        Template template,
        TokenEnumerator enumerator,
        string? rawInput,
        TokenizeResultBase result,
        IDiagnosticCollector collector)
    {
        currentTemplate = template;
        matchedPreambles.Clear();
        requiredHints.Clear();
        ShouldAbandon = false;

        foreach (var hint in template.Hints)
        {
            if (hint.Optional == false)
            {
                requiredHints.Add(hint);
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void OnTokenMatched(Token token)
    {
        if (!string.IsNullOrEmpty(token.Preamble))
        {
            matchedPreambles.Add(token.Preamble);

            // Check if all required hints are now satisfied
            if (requiredHints.Count > 0 && AllRequiredHintsSatisfied())
            {
                ShouldAbandon = false;
            }
        }
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResultBase result)
    {
        if (currentTemplate == null || currentTemplate.Hints.Count == 0) return false;

        foreach (var hint in currentTemplate.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text)) continue;

            var found = matchedPreambles.Any(p => p.Contains(hint.Text));

            if (found)
            {
                result.Hints.AddMatch(hint, null!);
            }
            else
            {
                result.Hints.AddMiss(hint);
            }
        }

        return result.Hints.Misses.Any(h => h.Optional == false);
    }

    private bool AllRequiredHintsSatisfied()
    {
        foreach (var hint in requiredHints)
        {
            if (string.IsNullOrEmpty(hint.Text)) continue;
            if (!matchedPreambles.Any(p => p.Contains(hint.Text))) return false;
        }
        return true;
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "EarlyAbandonHintStrategyTests"`
Expected: PASS

- [ ] **Step 5: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/Strategies/EarlyAbandonHintStrategy.cs tests/Tokenizer.Tests/Tokenization/Strategies/EarlyAbandonHintStrategyTests.cs
git commit -m "Add EarlyAbandonHintStrategy — single-pass with early termination"
```

---

### Task 9: Wire IHintStrategy into Tokenizer

Replace `IHintProcessor` usage in `Tokenizer` with `IHintStrategy`. Keep `IHintProcessor` for now (existing DI registrations) but route through the strategy.

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Modify: `src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs`

- [ ] **Step 1: Add `HintStrategy` to `TokenizerOptions`**

In `TokenizerOptions.cs`, add:

```csharp
using Tokens.Tokenization;
using Tokens.Tokenization.Strategies;
```

And add the property:

```csharp
/// <summary>
/// The hint processing strategy. Default: <see cref="ContainsHintStrategy"/>.
/// </summary>
internal IHintStrategy HintStrategy { get; init; } = new ContainsHintStrategy();
```

Update the copy constructor to include:

```csharp
HintStrategy = original.HintStrategy;
```

- [ ] **Step 2: Update `Tokenizer` to use `IHintStrategy` instead of `IHintProcessor`**

In `Tokenizer.cs`, in the private `Tokenize` method, replace the hint processing block:

```csharp
// Process hints first
log.LogTrace("Processing hints");
var hintsMissing = hintProcessor.FindAndValidateHints(template, context.Enumerator, result, collector);

if (hintsMissing)
{
    log.LogWarning("Required hints are missing, skipping tokenization");
}
else
{
    log.LogTrace("Hints validated successfully, proceeding with tokenization");
    tokenizationEngine.ProcessTokenization(template, input, value, context, result, collector);
}
```

With:

```csharp
// Process hints (pre-pass)
log.LogTrace("Processing hints with {Strategy}", Options.HintStrategy.GetType().Name);
var hintsMissing = Options.HintStrategy.PreProcess(template, context.Enumerator, input, result, collector);

if (hintsMissing)
{
    log.LogWarning("Required hints are missing, skipping tokenization");
}
else
{
    log.LogTrace("Hints validated successfully, proceeding with tokenization");
    tokenizationEngine.ProcessTokenization(template, input.Length, value, context, result, collector);

    // Post-process hints (single-pass strategies evaluate here)
    if (Options.HintStrategy.PostProcess(result))
    {
        log.LogWarning("Post-tokenization hint check failed — required hints not found");
    }
}
```

- [ ] **Step 3: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs src/Tokenizer/TokenizerOptions.cs
git commit -m "Wire IHintStrategy into Tokenizer, replacing IHintProcessor for hint processing"
```

---

### Task 10: Add TextReader and Stream overloads to ITokenizer

Add the new overloads to the interface and implementation.

**Files:**
- Modify: `src/Tokenizer/ITokenizer.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`
- Test: `tests/Tokenizer.Tests/TokenizerStreamTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/Tokenizer.Tests/TokenizerStreamTests.cs`:

```csharp
using System.IO;
using System.Text;
using Xunit;

namespace Tokens;

public class TokenizerStreamTests
{
    [Fact]
    public void GivenTextReaderInput_WhenTokenize_ThenExtractsValues()
    {
        // Arrange
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile("Name: {Name}");
        using var reader = new StringReader("Name: Alice");

        // Act
        var result = tokenizer.Tokenize(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Alice", result.First("Name").Value);
    }

    [Fact]
    public void GivenTextReaderInput_WhenTokenizeGeneric_ThenPopulatesObject()
    {
        // Arrange
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile("Name: {Name}");
        using var reader = new StringReader("Name: Alice");

        // Act
        var result = tokenizer.Tokenize<SimpleRecord>(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Alice", result.Value.Name);
    }

    [Fact]
    public void GivenStreamInput_WhenTokenize_ThenExtractsValues()
    {
        // Arrange
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile("Name: {Name}");
        var bytes = Encoding.UTF8.GetBytes("Name: Alice");
        using var stream = new MemoryStream(bytes);

        // Act
        var result = tokenizer.Tokenize(template, stream, Encoding.UTF8);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Alice", result.First("Name").Value);
    }

    [Fact]
    public void GivenStreamInput_WhenTokenizeGeneric_ThenPopulatesObject()
    {
        // Arrange
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile("Name: {Name}");
        var bytes = Encoding.UTF8.GetBytes("Name: Alice");
        using var stream = new MemoryStream(bytes);

        // Act
        var result = tokenizer.Tokenize<SimpleRecord>(template, stream, Encoding.UTF8);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Alice", result.Value.Name);
    }

    [Fact]
    public void GivenStreamInput_WhenTokenize_ThenStreamIsNotDisposed()
    {
        // Arrange
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile("Name: {Name}");
        var bytes = Encoding.UTF8.GetBytes("Name: Alice");
        var stream = new MemoryStream(bytes);

        // Act
        tokenizer.Tokenize(template, stream, Encoding.UTF8);

        // Assert — stream should still be usable (not disposed)
        Assert.True(stream.CanRead);
        stream.Dispose();
    }

    public class SimpleRecord
    {
        public string? Name { get; set; }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerStreamTests"`
Expected: FAIL — overloads don't exist

- [ ] **Step 3: Add overloads to ITokenizer**

In `ITokenizer.cs`, add after the existing tokenization methods:

```csharp
/// <summary>
/// Tokenizes the input from a <see cref="TextReader"/> using a pre-compiled template.
/// The caller retains ownership of the reader — it is not disposed.
/// </summary>
TokenizeResult Tokenize(Template template, TextReader input);

/// <summary>
/// Tokenizes the input from a <see cref="TextReader"/> using a pre-compiled template,
/// mapping values onto a new <typeparamref name="T"/>.
/// The caller retains ownership of the reader — it is not disposed.
/// </summary>
TokenizeResult<T> Tokenize<T>(Template template, TextReader input) where T : class, new();

/// <summary>
/// Tokenizes the input from a <see cref="Stream"/> using a pre-compiled template.
/// The stream is read using the specified encoding. The caller retains ownership of the stream.
/// </summary>
TokenizeResult Tokenize(Template template, Stream input, Encoding encoding);

/// <summary>
/// Tokenizes the input from a <see cref="Stream"/> using a pre-compiled template,
/// mapping values onto a new <typeparamref name="T"/>.
/// The stream is read using the specified encoding. The caller retains ownership of the stream.
/// </summary>
TokenizeResult<T> Tokenize<T>(Template template, Stream input, Encoding encoding) where T : class, new();
```

Add `using System.Text;` to the file.

- [ ] **Step 4: Implement overloads in Tokenizer**

In `Tokenizer.cs`, add a private `Tokenize` overload that accepts `TextReader`:

```csharp
private void Tokenize(TokenizeResultBase result, object? value, Template template, TextReader input)
{
    using (log.BeginScope(new Dictionary<string, object>
    {
        ["TemplateName"] = template.Name,
        ["Operation"] = "Tokenize"
    }))
    {
        log.LogInformation("Starting tokenization for template {TemplateName} from TextReader", template.Name);

        using (var context = new TokenizationContext())
        {
            context.Initialize(input);

            IDiagnosticCollector collector = template.Options.EnableDiagnostics
                ? new DiagnosticCollector(null, null)
                : NullDiagnosticCollector.Instance;

            // Pre-process hints (pass null rawInput — no string available)
            var hintsMissing = Options.HintStrategy.PreProcess(template, context.Enumerator, null, result, collector);

            if (hintsMissing)
            {
                log.LogWarning("Required hints are missing, skipping tokenization");
            }
            else
            {
                tokenizationEngine.ProcessTokenization(template, 0, value, context, result, collector);

                if (Options.HintStrategy.PostProcess(result))
                {
                    log.LogWarning("Post-tokenization hint check failed");
                }
            }

            resultBuilder.BuildUnmatchedTokens(template, result, collector);
            result.Diagnostics = collector.GetResult();
        }
    }
}
```

Then add the public overloads:

```csharp
/// <inheritdoc />
public TokenizeResult Tokenize(Template template, TextReader input)
{
    var result = new TokenizeResult(template);
    Tokenize(result, null, template, input);
    return result;
}

/// <inheritdoc />
public TokenizeResult<T> Tokenize<T>(Template template, TextReader input) where T : class, new()
{
    var result = new TokenizeResult<T>(template);
    Tokenize(result, result.Value, template, input);
    return result;
}

/// <inheritdoc />
public TokenizeResult Tokenize(Template template, Stream input, Encoding encoding)
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return Tokenize(template, reader);
}

/// <inheritdoc />
public TokenizeResult<T> Tokenize<T>(Template template, Stream input, Encoding encoding) where T : class, new()
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return Tokenize<T>(template, reader);
}
```

Update the existing string `Tokenize` to wrap with `StringReader`:

```csharp
private void Tokenize(TokenizeResultBase result, object? value, Template template, string input)
{
    if (template.Options.MaxInputLength > 0 && input.Length > template.Options.MaxInputLength)
    {
        throw new TokenizerException(
            $"Input length {input.Length:N0} exceeds maximum allowed length of {template.Options.MaxInputLength:N0}.");
    }

    using var reader = new StringReader(input);
    // For string inputs, we pass the raw input for hint strategies that need it
    using (log.BeginScope(new Dictionary<string, object>
    {
        ["TemplateName"] = template.Name,
        ["InputLength"] = input.Length,
        ["TokenCount"] = template.Tokens.Count,
        ["Operation"] = "Tokenize"
    }))
    {
        log.LogInformation("Starting tokenization for template {TemplateName}", template.Name);

        using (var context = new TokenizationContext())
        {
            context.Initialize(reader);

            IDiagnosticCollector collector = template.Options.EnableDiagnostics
                ? new DiagnosticCollector(null, input)
                : NullDiagnosticCollector.Instance;

            var hintsMissing = Options.HintStrategy.PreProcess(template, context.Enumerator, input, result, collector);

            if (hintsMissing)
            {
                log.LogWarning("Required hints are missing, skipping tokenization");
            }
            else
            {
                tokenizationEngine.ProcessTokenization(template, input.Length, value, context, result, collector);

                if (Options.HintStrategy.PostProcess(result))
                {
                    log.LogWarning("Post-tokenization hint check failed");
                }
            }

            resultBuilder.BuildUnmatchedTokens(template, result, collector);
            result.Diagnostics = collector.GetResult();
        }
    }
}
```

- [ ] **Step 5: Run new tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerStreamTests"`
Expected: PASS

- [ ] **Step 6: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/ITokenizer.cs src/Tokenizer/Tokenizer.cs tests/Tokenizer.Tests/TokenizerStreamTests.cs
git commit -m "Add TextReader and Stream tokenization overloads to ITokenizer"
```

---

### Task 11: Add TextReader and Stream overloads to ITokenMatcher

**Files:**
- Modify: `src/Tokenizer/ITokenMatcher.cs`
- Modify: `src/Tokenizer/TokenMatcher.cs`
- Test: `tests/Tokenizer.Tests/TokenMatcherStreamTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/Tokenizer.Tests/TokenMatcherStreamTests.cs`:

```csharp
using System.IO;
using System.Text;
using Xunit;

namespace Tokens;

public class TokenMatcherStreamTests
{
    [Fact]
    public void GivenTextReaderInput_WhenMatch_ThenFindsMatch()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Name}");
        using var reader = new StringReader("Name: Alice");

        // Act
        var result = matcher.Match(reader);

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.True(result.BestMatch.Success);
    }

    [Fact]
    public void GivenTextReaderInput_WhenMatchGeneric_ThenPopulatesObject()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Name}");
        using var reader = new StringReader("Name: Alice");

        // Act
        var result = matcher.Match<SimpleRecord>(reader);

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("Alice", result.BestMatch.Value.Name);
    }

    [Fact]
    public void GivenTextReaderInputWithTags_WhenMatch_ThenFiltersCorrectly()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("---\ntag: server1\n---\nName: {Name}");
        matcher.RegisterTemplate("---\ntag: server2\n---\nAge: {Age}");
        using var reader = new StringReader("Name: Alice");

        // Act
        var result = matcher.Match(reader, new[] { "server1" });

        // Assert
        Assert.NotNull(result.BestMatch);
    }

    [Fact]
    public void GivenStreamInput_WhenMatch_ThenFindsMatch()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Name}");
        var bytes = Encoding.UTF8.GetBytes("Name: Alice");
        using var stream = new MemoryStream(bytes);

        // Act
        var result = matcher.Match(stream, Encoding.UTF8);

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.True(result.BestMatch.Success);
    }

    [Fact]
    public void GivenStreamInput_WhenMatch_ThenStreamIsNotDisposed()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Name}");
        var bytes = Encoding.UTF8.GetBytes("Name: Alice");
        var stream = new MemoryStream(bytes);

        // Act
        matcher.Match(stream, Encoding.UTF8);

        // Assert
        Assert.True(stream.CanRead);
        stream.Dispose();
    }

    public class SimpleRecord
    {
        public string? Name { get; set; }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenMatcherStreamTests"`
Expected: FAIL

- [ ] **Step 3: Add overloads to ITokenMatcher**

In `ITokenMatcher.cs`, add:

```csharp
/// <summary>
/// Matches the input from a <see cref="TextReader"/> against all registered templates.
/// The reader is consumed fully. The caller retains ownership.
/// </summary>
TokenMatcherResult Match(TextReader input);

/// <summary>
/// Matches the input from a <see cref="TextReader"/> against registered templates filtered by tags.
/// </summary>
TokenMatcherResult Match(TextReader input, string[]? tags);

/// <summary>
/// Matches the input from a <see cref="TextReader"/> and populates a new <typeparamref name="T"/>.
/// </summary>
TokenMatcherResult<T> Match<T>(TextReader input) where T : class, new();

/// <summary>
/// Matches the input from a <see cref="TextReader"/> filtered by tags and populates a new <typeparamref name="T"/>.
/// </summary>
TokenMatcherResult<T> Match<T>(TextReader input, string[]? tags) where T : class, new();

/// <summary>
/// Matches the input from a <see cref="Stream"/> against all registered templates.
/// The caller retains ownership of the stream.
/// </summary>
TokenMatcherResult Match(Stream input, Encoding encoding);

/// <summary>
/// Matches the input from a <see cref="Stream"/> filtered by tags.
/// </summary>
TokenMatcherResult Match(Stream input, Encoding encoding, string[]? tags);

/// <summary>
/// Matches the input from a <see cref="Stream"/> and populates a new <typeparamref name="T"/>.
/// </summary>
TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding) where T : class, new();

/// <summary>
/// Matches the input from a <see cref="Stream"/> filtered by tags and populates a new <typeparamref name="T"/>.
/// </summary>
TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding, string[]? tags) where T : class, new();
```

Add `using System.Text;` to the file.

- [ ] **Step 4: Implement overloads in TokenMatcher**

In `TokenMatcher.cs`, add:

```csharp
/// <inheritdoc />
public TokenMatcherResult Match(TextReader input)
{
    var content = input.ReadToEnd();
    return Match(content);
}

/// <inheritdoc />
public TokenMatcherResult Match(TextReader input, string[]? tags)
{
    var content = input.ReadToEnd();
    return Match(content, tags);
}

/// <inheritdoc />
public TokenMatcherResult<T> Match<T>(TextReader input) where T : class, new()
{
    var content = input.ReadToEnd();
    return Match<T>(content);
}

/// <inheritdoc />
public TokenMatcherResult<T> Match<T>(TextReader input, string[]? tags) where T : class, new()
{
    var content = input.ReadToEnd();
    return Match<T>(content, tags);
}

/// <inheritdoc />
public TokenMatcherResult Match(Stream input, Encoding encoding)
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return Match(reader);
}

/// <inheritdoc />
public TokenMatcherResult Match(Stream input, Encoding encoding, string[]? tags)
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return Match(reader, tags);
}

/// <inheritdoc />
public TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding) where T : class, new()
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return Match<T>(reader);
}

/// <inheritdoc />
public TokenMatcherResult<T> Match<T>(Stream input, Encoding encoding, string[]? tags) where T : class, new()
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return Match<T>(reader, tags);
}
```

Add `using System.Text;` to the file.

- [ ] **Step 5: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenMatcherStreamTests"`
Expected: PASS

- [ ] **Step 6: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/ITokenMatcher.cs src/Tokenizer/TokenMatcher.cs tests/Tokenizer.Tests/TokenMatcherStreamTests.cs
git commit -m "Add TextReader and Stream matching overloads to ITokenMatcher"
```

---

### Task 12: Add InputStreamBenchmarks

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Benchmarks/InputStreamBenchmarks.cs`

- [ ] **Step 1: Create InputStreamBenchmarks**

Create `benchmarks/Tokenizer.Benchmarks/Benchmarks/InputStreamBenchmarks.cs`:

```csharp
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures tokenization cost across different input source types:
/// string (via StringReader), TextReader directly, and Stream with encoding.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class InputStreamBenchmarks
{
    private Tokenizer tokenizer = null!;
    private Template smallTemplate = null!;
    private Template mediumTemplate = null!;
    private Template largeTemplate = null!;
    private string smallInput = null!;
    private string mediumInput = null!;
    private string largeInput = null!;
    private byte[] smallBytes = null!;
    private byte[] mediumBytes = null!;
    private byte[] largeBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        tokenizer = new Tokenizer();
        var parser = new TokenParser();

        smallTemplate = parser.Parse(WorkloadGenerator.SmallTemplate(), "small");
        mediumTemplate = parser.Parse(WorkloadGenerator.MediumTemplate(), "medium");
        largeTemplate = parser.Parse(WorkloadGenerator.LargeTemplate(), "large");

        smallInput = WorkloadGenerator.SmallInput();
        mediumInput = WorkloadGenerator.MediumInput();
        largeInput = WorkloadGenerator.LargeInput();

        smallBytes = Encoding.UTF8.GetBytes(smallInput);
        mediumBytes = Encoding.UTF8.GetBytes(mediumInput);
        largeBytes = Encoding.UTF8.GetBytes(largeInput);
    }

    // String input (baseline)

    [Benchmark(Baseline = true, Description = "String small")]
    public TokenizeResult String_Small() => tokenizer.Tokenize(smallTemplate, smallInput);

    [Benchmark(Description = "String medium")]
    public TokenizeResult String_Medium() => tokenizer.Tokenize(mediumTemplate, mediumInput);

    [Benchmark(Description = "String large")]
    public TokenizeResult String_Large() => tokenizer.Tokenize(largeTemplate, largeInput);

    // TextReader input

    [Benchmark(Description = "TextReader small")]
    public TokenizeResult TextReader_Small()
    {
        using var reader = new StringReader(smallInput);
        return tokenizer.Tokenize(smallTemplate, reader);
    }

    [Benchmark(Description = "TextReader medium")]
    public TokenizeResult TextReader_Medium()
    {
        using var reader = new StringReader(mediumInput);
        return tokenizer.Tokenize(mediumTemplate, reader);
    }

    [Benchmark(Description = "TextReader large")]
    public TokenizeResult TextReader_Large()
    {
        using var reader = new StringReader(largeInput);
        return tokenizer.Tokenize(largeTemplate, reader);
    }

    // Stream input

    [Benchmark(Description = "Stream small")]
    public TokenizeResult Stream_Small()
    {
        using var stream = new MemoryStream(smallBytes);
        return tokenizer.Tokenize(smallTemplate, stream, Encoding.UTF8);
    }

    [Benchmark(Description = "Stream medium")]
    public TokenizeResult Stream_Medium()
    {
        using var stream = new MemoryStream(mediumBytes);
        return tokenizer.Tokenize(mediumTemplate, stream, Encoding.UTF8);
    }

    [Benchmark(Description = "Stream large")]
    public TokenizeResult Stream_Large()
    {
        using var stream = new MemoryStream(largeBytes);
        return tokenizer.Tokenize(largeTemplate, stream, Encoding.UTF8);
    }
}
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build ./benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Benchmarks/InputStreamBenchmarks.cs
git commit -m "Add InputStreamBenchmarks — string vs TextReader vs Stream performance"
```

---

### Task 13: Add HintStrategyBenchmarks

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Benchmarks/HintStrategyBenchmarks.cs`

- [ ] **Step 1: Create HintStrategyBenchmarks**

Create `benchmarks/Tokenizer.Benchmarks/Benchmarks/HintStrategyBenchmarks.cs`:

```csharp
using BenchmarkDotNet.Attributes;
using Tokens.Config;
using Tokens.Data;
using Tokens.Tokenization.Strategies;

namespace Tokens.Benchmarks;

/// <summary>
/// Compares hint strategy performance across scenarios:
/// single template (hints present/missing), multi-template matching.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class HintStrategyBenchmarks
{
    private string mediumInput = null!;
    private string nonMatchingInput = null!;
    private Template mediumTemplate = null!;

    private Tokenizer containsTokenizer = null!;
    private Tokenizer enumeratorTokenizer = null!;
    private Tokenizer integratedTokenizer = null!;
    private Tokenizer earlyAbandonTokenizer = null!;

    [GlobalSetup]
    public void Setup()
    {
        mediumInput = WorkloadGenerator.MediumInput();
        nonMatchingInput = "This input does not match any template patterns at all.";

        var containsOpts = new TokenizerOptions { HintStrategy = new ContainsHintStrategy() };
        var enumeratorOpts = new TokenizerOptions { HintStrategy = new EnumeratorScanHintStrategy() };
        var integratedOpts = new TokenizerOptions { HintStrategy = new IntegratedHintStrategy() };
        var earlyAbandonOpts = new TokenizerOptions { HintStrategy = new EarlyAbandonHintStrategy() };

        containsTokenizer = new Tokenizer(containsOpts);
        enumeratorTokenizer = new Tokenizer(enumeratorOpts);
        integratedTokenizer = new Tokenizer(integratedOpts);
        earlyAbandonTokenizer = new Tokenizer(earlyAbandonOpts);

        mediumTemplate = containsTokenizer.Compile(WorkloadGenerator.MediumTemplate(), "medium");
    }

    // Hints present (happy path)

    [Benchmark(Baseline = true, Description = "Contains — hints present")]
    public TokenizeResult Contains_HintsPresent()
        => containsTokenizer.Tokenize(mediumTemplate, mediumInput);

    [Benchmark(Description = "EnumeratorScan — hints present")]
    public TokenizeResult EnumeratorScan_HintsPresent()
        => enumeratorTokenizer.Tokenize(mediumTemplate, mediumInput);

    [Benchmark(Description = "Integrated — hints present")]
    public TokenizeResult Integrated_HintsPresent()
        => integratedTokenizer.Tokenize(mediumTemplate, mediumInput);

    [Benchmark(Description = "EarlyAbandon — hints present")]
    public TokenizeResult EarlyAbandon_HintsPresent()
        => earlyAbandonTokenizer.Tokenize(mediumTemplate, mediumInput);

    // Hints missing (rejection path)

    [Benchmark(Description = "Contains — hints missing")]
    public TokenizeResult Contains_HintsMissing()
        => containsTokenizer.Tokenize(mediumTemplate, nonMatchingInput);

    [Benchmark(Description = "EnumeratorScan — hints missing")]
    public TokenizeResult EnumeratorScan_HintsMissing()
        => enumeratorTokenizer.Tokenize(mediumTemplate, nonMatchingInput);

    [Benchmark(Description = "Integrated — hints missing")]
    public TokenizeResult Integrated_HintsMissing()
        => integratedTokenizer.Tokenize(mediumTemplate, nonMatchingInput);

    [Benchmark(Description = "EarlyAbandon — hints missing")]
    public TokenizeResult EarlyAbandon_HintsMissing()
        => earlyAbandonTokenizer.Tokenize(mediumTemplate, nonMatchingInput);
}
```

Note: `HintStrategy` needs to be `init` accessible for benchmarks. Since it's `internal`, the benchmark project needs `InternalsVisibleTo`. Check if this is already configured — if not, add `[assembly: InternalsVisibleTo("Tokenizer.Benchmarks")]` to the tokenizer project.

- [ ] **Step 2: Add InternalsVisibleTo if needed**

Check for existing `InternalsVisibleTo` in the tokenizer project. If the benchmark project isn't listed, add to `src/Tokenizer/Tokenizer.csproj`:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="Tokenizer.Benchmarks" />
</ItemGroup>
```

- [ ] **Step 3: Verify it compiles**

Run: `dotnet build ./benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Benchmarks/HintStrategyBenchmarks.cs src/Tokenizer/Tokenizer.csproj
git commit -m "Add HintStrategyBenchmarks — four strategies compared across scenarios"
```

---

### Task 14: Run benchmarks and capture results

**Files:**
- Output to: `benchmarks/baselines/streaming-input/`

- [ ] **Step 1: Run existing benchmarks as pre-refactor baseline**

Run: `cd /Users/work/Source/tokenizer && dotnet run --project benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release -- --filter "TokenizationBenchmarks" --artifacts benchmarks/baselines/streaming-input/pre`

- [ ] **Step 2: Run InputStreamBenchmarks**

Run: `cd /Users/work/Source/tokenizer && dotnet run --project benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release -- --filter "InputStreamBenchmarks" --artifacts benchmarks/baselines/streaming-input/input-stream`

- [ ] **Step 3: Run HintStrategyBenchmarks**

Run: `cd /Users/work/Source/tokenizer && dotnet run --project benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release -- --filter "HintStrategyBenchmarks" --artifacts benchmarks/baselines/streaming-input/hint-strategy`

- [ ] **Step 4: Run all tests one final time**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit benchmark results**

```bash
git add benchmarks/baselines/streaming-input/
git commit -m "Add streaming input benchmark results"
```

---

### Task 15: Update DI registration

Update `TokenizerServiceCollectionExtensions` to no longer depend on `IHintProcessor` if the strategy is now on options. Keep `IHintProcessor` registered for backward compatibility but mark as obsolete internally.

**Files:**
- Modify: `src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs`

- [ ] **Step 1: Review what changes are needed**

If `Tokenizer` no longer takes `IHintProcessor` in its DI constructor, remove that registration. If it still takes it (for backward compat), leave it.

Check the internal DI constructor in `Tokenizer.cs` — if `IHintProcessor` is still a parameter, leave the DI registration. If we've fully moved to `IHintStrategy` via options, remove the `IHintProcessor` registration.

- [ ] **Step 2: Make necessary changes**

If removing `IHintProcessor`:
- Remove the `services.TryAddSingleton<IHintProcessor>(...)` block
- Remove the `IHintProcessor` parameter from the internal `Tokenizer` constructor
- Remove the `hintProcessor` field from `Tokenizer`

- [ ] **Step 3: Run ALL tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs src/Tokenizer/Tokenizer.cs
git commit -m "Remove IHintProcessor DI registration — replaced by IHintStrategy on options"
```

---

### Task 16: Final cleanup and documentation

**Files:**
- Modify: `docs/ROADMAP.md`

- [ ] **Step 1: Run ALL tests one final time**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 2: Verify build in Release mode**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeded with no warnings

- [ ] **Step 3: Commit any remaining changes**

```bash
git status
# Add any remaining files
git commit -m "Final cleanup for streaming input tokenization"
```
