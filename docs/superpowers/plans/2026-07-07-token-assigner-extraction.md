# Token Assignment Extraction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract assignment logic from `Token` into a session-scoped `TokenAssigner`, making `Token` a pure data model.

**Architecture:** Create `TokenAssigner` (session-scoped, owns assignment + decorator pipeline) and `ValueConcatenation` (static utility). Strip `Token` to pure data. Consolidate `CandidateProcessor` catch blocks. Wire through `TokenizationSession`.

**Tech Stack:** C# / .NET, xUnit, NSubstitute

**Spec:** `docs/superpowers/specs/2026-07-07-token-assigner-extraction.md`

---

### Task 1: Create `ValueConcatenation` static utility with tests

**Files:**
- Create: `src/Tokenizer/Extensions/ValueConcatenation.cs`
- Create: `tests/Tokenizer.Tests/Extensions/ValueConcatenationTests.cs`

This is a leaf dependency — no other changes needed first.

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Extensions/ValueConcatenationTests.cs`:

```csharp
using Tokens.Extensions;
using Xunit;

namespace Tokens.Extensions;

public class ValueConcatenationTests
{
    [Fact]
    public void GivenTwoStrings_WhenCanConcatenate_ThenReturnsTrue()
    {
        // Arrange / Act
        var result = ValueConcatenation.CanConcatenate("hello", "world");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNullExistingValue_WhenCanConcatenate_ThenReturnsFalse()
    {
        // Arrange / Act
        var result = ValueConcatenation.CanConcatenate(null, "world");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNonStringValues_WhenCanConcatenate_ThenReturnsFalse()
    {
        // Arrange / Act
        var result = ValueConcatenation.CanConcatenate(42, "world");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTwoStrings_WhenConcatenate_ThenReturnsCombinedString()
    {
        // Arrange / Act
        var result = ValueConcatenation.Concatenate("hello", "world", null);

        // Assert
        Assert.Equal("helloworld", result);
    }

    [Fact]
    public void GivenTwoStringsWithSeparator_WhenConcatenate_ThenReturnsSeparatedString()
    {
        // Arrange / Act
        var result = ValueConcatenation.Concatenate("hello", "world", ", ");

        // Assert
        Assert.Equal("hello, world", result);
    }

    [Fact]
    public void GivenTwoStringsWithCrSeparator_WhenConcatenate_ThenReplacesWithNewLine()
    {
        // Arrange / Act
        var result = ValueConcatenation.Concatenate("hello", "world", "<CR>");

        // Assert
        Assert.Equal($"hello{Environment.NewLine}world", result);
    }

    [Fact]
    public void GivenNonStringExistingValue_WhenConcatenate_ThenReturnsExistingValue()
    {
        // Arrange / Act
        var result = ValueConcatenation.Concatenate(42, "world", null);

        // Assert
        Assert.Equal(42, result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ValueConcatenationTests"`
Expected: Build failure — `ValueConcatenation` does not exist yet.

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Extensions/ValueConcatenation.cs`:

```csharp
namespace Tokens.Extensions;

/// <summary>
/// Utility methods for concatenating token values.
/// </summary>
internal static class ValueConcatenation
{
    /// <summary>
    /// Returns <see langword="true"/> if the existing and new values can be concatenated.
    /// Currently only string-to-string concatenation is supported.
    /// </summary>
    internal static bool CanConcatenate(object? existingValue, object newValue)
    {
        if (existingValue is string && newValue is string)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Concatenates two values using the specified joining string.
    /// The literal <c>&lt;CR&gt;</c> in the joining string is replaced with <see cref="Environment.NewLine"/>.
    /// Returns the existing value unchanged if the values are not both strings.
    /// </summary>
    internal static object? Concatenate(object? existingValue, object newValue, string? concatenationString)
    {
        if (existingValue is string && newValue is string)
        {
            var concatStringValue = (concatenationString ?? string.Empty).Replace("<CR>", Environment.NewLine, StringComparison.Ordinal);

            return $"{existingValue}{concatStringValue}{newValue}";
        }

        return existingValue;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ValueConcatenationTests"`
Expected: All 7 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Extensions/ValueConcatenation.cs tests/Tokenizer.Tests/Extensions/ValueConcatenationTests.cs
git commit -m "feat: add ValueConcatenation static utility with tests"
```

---

### Task 2: Create `TokenAssigner` with tests

**Files:**
- Create: `src/Tokenizer/Tokenization/TokenAssigner.cs`
- Create: `tests/Tokenizer.Tests/Tokenization/TokenAssignerTests.cs`

Depends on: Task 1 (uses `ValueConcatenation`).

The tests here mirror the existing `TokenTests.cs` tests but target `TokenAssigner` directly. The existing `TokenTests.cs` will be removed in Task 5.

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Tokenization/TokenAssignerTests.cs`:

```csharp
using System.Collections.Concurrent;
using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Validators;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tokenization;

public class TokenAssignerTests : TokenizerTestBase
{
    private readonly TokenAssigner _assigner;

    public TokenAssignerTests(ITestOutputHelper output) : base(output)
    {
        _assigner = new TokenAssigner(new TokenizerOptions(), NullDiagnosticCollector.Instance);
    }

    public class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    [Fact]
    public void GivenTokenWithValidValue_WhenAssigning_ThenSetsPropertyValue()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.Assign(token, person, "Sue", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("Sue", person.Name);
        Assert.Equal("Sue", value);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenAssigningValidNumber_ThenSetsPropertyValue()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Age").Build();
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var result = _assigner.Assign(token, person, "20", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(20, person.Age);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenAssigningInvalidNumber_ThenReturnsFalse()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Age").Build();
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var result = _assigner.Assign(token, person, "Twenty", new FileLocation(), out _);

        // Assert
        Assert.False(result);
        Assert.Equal(0, person.Age);
    }

    [Fact]
    public void GivenTokenWithTerminateOnNewLine_WhenValueContainsNewLine_ThenTruncatesAtNewLine()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder()
            .WithName("Name")
            .WithTerminateOnNewLine(true)
            .Build();

        // Act
        _assigner.Assign(token, person, "Alice\nBob", new FileLocation(), out _);

        // Assert
        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void GivenNullTarget_WhenAssigning_ThenReturnsTrueWithoutSideEffects()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.Assign(token, null, "Sue", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenEmptyValue_WhenAssigning_ThenReturnsFalse()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.Assign(token, person, string.Empty, new FileLocation(), out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenDictionaryTarget_WhenAssigning_ThenSetsKeyValue()
    {
        // Arrange
        var dict = new Dictionary<string, object>();
        var token = new TokenBuilder().WithName("Key").Build();

        // Act
        var result = _assigner.Assign(token, dict, "Value", new FileLocation(), out _);

        // Assert
        Assert.True(result);
        Assert.Equal("Value", dict["Key"]);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreEnabled_WhenAssigning_ThenReturnsTrueWithoutThrowing()
    {
        // Arrange
        var person = new Person();
        var options = new TokenizerOptions { IgnoreMissingProperties = true };
        var assigner = new TokenAssigner(options, NullDiagnosticCollector.Instance);
        var token = new TokenBuilder().WithName("NonExistent").Build();

        // Act
        var result = assigner.Assign(token, person, "value", new FileLocation(), out _);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreDisabled_WhenAssigning_ThenThrowsMissingMemberException()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("NonExistent").Build();

        // Act & Assert
        Assert.Throws<MissingMemberException>(() =>
            _assigner.Assign(token, person, "value", new FileLocation(), out _));
    }

    [Fact]
    public void GivenTokenWithTrimTrailingWhitespace_WhenAssigning_ThenTrimsValue()
    {
        // Arrange
        var person = new Person();
        var options = new TokenizerOptions { TrimTrailingWhiteSpace = true };
        var assigner = new TokenAssigner(options, NullDiagnosticCollector.Instance);
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        assigner.Assign(token, person, "Sue   ", new FileLocation(), out _);

        // Assert
        Assert.Equal("Sue", person.Name);
    }

    [Fact]
    public void GivenTokenWithValidValue_WhenCanAssign_ThenReturnsTrue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.CanAssign(token, "Sue");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenEmptyValue_WhenCanAssign_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.CanAssign(token, string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenConcatenatableToken_WhenAssigningTwice_ThenConcatenatesValues()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Name").Build();
        token.CanConcatenate = true;
        token.ConcatenationString = ", ";

        // Act
        _assigner.Assign(token, person, "Alice", new FileLocation(), out _);
        _assigner.Assign(token, person, "Bob", new FileLocation(), out _);

        // Assert
        Assert.Equal("Alice, Bob", person.Name);
    }

    [Fact]
    public void GivenRepeatingTokenWithDictionaryTarget_WhenAssigningMultipleTimes_ThenBuildsListValue()
    {
        // Arrange
        var dict = new Dictionary<string, object>();
        var token = new TokenBuilder().WithName("Items").WithRepeating(true).Build();

        // Act
        _assigner.Assign(token, dict, "one", new FileLocation(), out _);
        _assigner.Assign(token, dict, "two", new FileLocation(), out _);

        // Assert
        var list = Assert.IsType<List<object>>(dict["Items"]);
        Assert.Equal(2, list.Count);
        Assert.Equal("one", list[0]);
        Assert.Equal("two", list[1]);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenAssignerTests"`
Expected: Build failure — `TokenAssigner` does not exist yet.

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Tokenization/TokenAssigner.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Tokenization;

/// <summary>
/// Handles assignment of matched values to target objects via the token's decorator pipeline.
/// Session-scoped: constructed once per tokenization session with shared options and diagnostics.
/// </summary>
internal sealed class TokenAssigner
{
    private readonly TokenizerOptions _options;
    private readonly IDiagnosticCollector _collector;

    internal TokenAssigner(TokenizerOptions options, IDiagnosticCollector collector)
    {
        _options = options;
        _collector = collector;
    }

    /// <summary>
    /// Prepares the value, runs the decorator pipeline, and assigns the result to the target object.
    /// </summary>
    internal bool Assign(Token token, object? target, string value, FileLocation location, out object? assignedValue)
    {
        assignedValue = null;

        var prepared = PrepareValue(token, value);
        if (prepared == null) return false;

        if (_options.TrimTrailingWhiteSpace)
        {
            prepared = prepared.TrimEnd();
        }

        if (!RunDecoratorPipeline(token, prepared, location, out assignedValue)) return false;

        if (target is IDictionary<string, object> dictionary)
        {
            return SetDictionaryValue(token, dictionary, assignedValue!);
        }

        // Target can be null if not reflecting onto an object
        if (target is null)
        {
            return true;
        }

        try
        {
            if (token.CanConcatenate)
            {
                if (assignedValue == null) return true;

                var current = target.GetValue(token.Name);

                if (ValueConcatenation.CanConcatenate(current, assignedValue))
                {
                    var concatenated = ValueConcatenation.Concatenate(current, assignedValue, token.ConcatenationString);
                    if (concatenated != null) target.SetValue(token.Name, concatenated, StringComparison.Ordinal);
                }
                else
                {
                    throw new TokenAssignmentException(token, $"Unable to concatenate type {assignedValue.GetType().Name} to {token.Name}");
                }
            }
            else
            {
                target.SetValue(token.Name, assignedValue!, StringComparison.Ordinal);
            }
        }
        catch (MissingMemberException)
        {
            if (!_options.IgnoreMissingProperties)
            {
                throw;
            }

            if (_collector.IsEnabled)
            {
                _collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                    tokenName: token.Name, tokenId: token.Id,
                    value: value,
                    detail: $"Property '{token.Name}' not found on target type; ignored via IgnoreMissingProperties");
            }
        }
        catch (TypeConversionException ex)
        {
            _collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                tokenName: token.Name, tokenId: token.Id,
                value: value,
                detail: $"Type conversion failed: {ex.Message}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Dry-run: checks whether the value can pass through preparation and the decorator pipeline
    /// without performing any assignment.
    /// </summary>
    internal bool CanAssign(Token token, string value)
    {
        var prepared = PrepareValue(token, value);
        if (prepared == null) return false;

        return RunDecoratorPipeline(token, prepared, location: null, out _);
    }

    private static string? PrepareValue(Token token, string value)
    {
        if (string.IsNullOrEmpty(value) && !token.IsFrontMatterToken) return null;
        if (token.IsNull) return null;
        if (string.IsNullOrWhiteSpace(token.Name)) return null;

        value = value.TrimTrailingNewLine();

        if (!string.IsNullOrEmpty(value) && token.TerminateOnNewLine)
        {
#pragma warning disable MA0001 // IndexOf(char) is inherently ordinal; no StringComparison overload exists
            var index = value.IndexOf('\n');
            if (index >= 0)
            {
                value = value.Substring(0, index);
            }
#pragma warning restore MA0001
        }

        return value;
    }

    private bool RunDecoratorPipeline(Token token, object input, FileLocation? location, out object? assignedValue)
    {
        assignedValue = input;

        foreach (var decorator in token.Decorators)
        {
            if (decorator.IsTransformer)
            {
                if (!decorator.TryTransform(assignedValue!, out var output))
                {
                    _collector?.Record(DiagnosticEventType.TransformerFailed,
                        tokenName: token.Name, tokenId: token.Id,
                        location: location,
                        value: assignedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name,
                        decoratorArgs: decorator.Parameters.ToArray());

                    return false;
                }

                _collector?.Record(DiagnosticEventType.TransformerSucceeded,
                    tokenName: token.Name, tokenId: token.Id,
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
                    _collector?.Record(DiagnosticEventType.ValidatorPassed,
                        tokenName: token.Name, tokenId: token.Id,
                        value: assignedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);
                }
                else
                {
                    _collector?.Record(DiagnosticEventType.ValidatorFailed,
                        tokenName: token.Name, tokenId: token.Id,
                        value: input?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);

                    return false;
                }
            }
        }

        return true;
    }

    private static bool SetDictionaryValue(Token token, IDictionary<string, object> dictionary, object input)
    {
        if (token.IsRepeating)
        {
            List<object> list;
            if (dictionary.ContainsKey(token.Name))
            {
                list = dictionary[token.Name] as List<object> ?? new List<object> { dictionary[token.Name] };
            }
            else
            {
                list = new List<object>();
            }
            list.Add(input);
            input = list;
        }

        if (dictionary.ContainsKey(token.Name))
        {
            dictionary[token.Name] = input;
        }
        else
        {
            dictionary.Add(token.Name, input);
        }

        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenAssignerTests"`
Expected: All tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenAssigner.cs tests/Tokenizer.Tests/Tokenization/TokenAssignerTests.cs
git commit -m "feat: add TokenAssigner with session-scoped assignment logic and tests"
```

---

### Task 3: Wire `TokenAssigner` into `CandidateTokenList`, `CandidateProcessor`, and `TokenizationSession`

**Files:**
- Modify: `src/Tokenizer/CandidateTokenList.cs:69-106` — change `TryAssign` and `CanAnyAssign` signatures
- Modify: `src/Tokenizer/Tokenization/CandidateProcessor.cs:20-32,38-115,121-125` — add `_assigner` field, consolidate catch blocks, pass assigner to `CandidateTokenList`
- Modify: `src/Tokenizer/Tokenization/TokenizationSession.cs:18-41` — create and wire `TokenAssigner`

Depends on: Task 2.

- [ ] **Step 1: Update `CandidateTokenList.TryAssign` signature**

In `src/Tokenizer/CandidateTokenList.cs`, replace the `TryAssign` method (lines 69-87) with:

```csharp
    public bool TryAssign(object? target, StringBuilder value, TokenAssigner assigner, FileLocation location, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Token? assigned, out object? assignedValue)
    {
        assigned = null;
        assignedValue = null;

        var valueString = value.ToString();

        foreach (var token in _tokens)
        {
            if (assigner.Assign(token, target, valueString, location, out assignedValue))
            {
                assigned = token;

                return true;
            }
        }

        return false;
    }
```

Remove the `using Tokens.Diagnostics;` import if it becomes unused after this change.

- [ ] **Step 2: Update `CandidateTokenList.CanAnyAssign` signature**

In `src/Tokenizer/CandidateTokenList.cs`, replace the `CanAnyAssign` method (lines 95-106) with:

```csharp
    public bool CanAnyAssign(string value, TokenAssigner assigner)
    {
        foreach (var token in _tokens)
        {
            if (assigner.CanAssign(token, value))
            {
                return true;
            }
        }

        return false;
    }
```

Add `using Tokens.Tokenization;` to the imports.

- [ ] **Step 3: Update `CandidateProcessor` — add `_assigner` field and consolidate catch blocks**

In `src/Tokenizer/Tokenization/CandidateProcessor.cs`:

Add field:
```csharp
    private readonly TokenAssigner _assigner;
```

Update constructor to accept and store `TokenAssigner`:
```csharp
    public CandidateProcessor(
        object? targetObject,
        TokenizeResultBase result,
        Template template,
        TokenAssigner assigner,
        IDiagnosticCollector collector,
        ILogger logger)
    {
        _targetObject = targetObject;
        _result = result;
        _template = template;
        _assigner = assigner;
        _collector = collector;
        _logger = logger;
    }
```

Replace the `TryAssign` method's try-catch body (lines 48-115). Update the `TryAssign` call to use the new signature and consolidate four catch blocks to one:

```csharp
        try
        {
            if (context.Candidates.TryAssign(_targetObject, context.Replacement, _assigner, location, out var assigned, out var assignedValue))
            {
                // ... (diagnostic recording and result tracking unchanged)
                return true;
            }

            // ... (diagnostic recording for failure unchanged)
            return false;
        }
        catch (Exception e)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(e, "Error Assigning Value: {Message}", e.Message);
            }
            _result.AddException(e);
            return false;
        }
```

Update `HandleRepeat` to pass `_assigner`:
```csharp
        if (!context.Candidates.CanAnyAssign(replacementValue, _assigner))
```

Remove unused `using Tokens.Exceptions;` import.

- [ ] **Step 4: Update `TokenizationSession` to create and wire `TokenAssigner`**

In `src/Tokenizer/Tokenization/TokenizationSession.cs`:

Add field:
```csharp
    private readonly TokenAssigner _assigner;
```

In the constructor, create the assigner and pass it to `CandidateProcessor`:
```csharp
        _assigner = new TokenAssigner(_template.Options, collector);
        _candidateProcessor = new CandidateProcessor(
            targetObject, result, template, _assigner, collector, logger);
```

- [ ] **Step 5: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Then: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: Build succeeds. Some `CandidateTokenListTests` may fail due to the changed `TryAssign`/`CanAnyAssign` signatures — that's expected and fixed in the next step.

- [ ] **Step 6: Update `CandidateTokenListTests` for new signatures**

In `tests/Tokenizer.Tests/CandidateTokenListTests.cs`:

Add these imports and fields at the top of the class:
```csharp
using Tokens.Tokenization;
```

Add a static field:
```csharp
    private static readonly TokenAssigner DefaultAssigner = new TokenAssigner(DefaultOptions, Diagnostics.NullDiagnosticCollector.Instance);
```

Update `TryAssign` test calls — replace `DefaultOptions` and `NullDiagnosticCollector.Instance` with `DefaultAssigner`. For example:

```csharp
        // Old:
        var result = list.TryAssign(target: null, value, DefaultOptions, NoLocation, out var assigned, out var assignedValue, NullDiagnosticCollector.Instance);
        // New:
        var result = list.TryAssign(target: null, value, DefaultAssigner, NoLocation, out var assigned, out var assignedValue);
```

Update `CanAnyAssign` test calls — add `DefaultAssigner` parameter:

```csharp
        // Old:
        var result = list.CanAnyAssign("some value");
        // New:
        var result = list.CanAnyAssign("some value", DefaultAssigner);
```

- [ ] **Step 7: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/Tokenizer/CandidateTokenList.cs src/Tokenizer/Tokenization/CandidateProcessor.cs src/Tokenizer/Tokenization/TokenizationSession.cs tests/Tokenizer.Tests/CandidateTokenListTests.cs
git commit -m "refactor: wire TokenAssigner through CandidateProcessor and CandidateTokenList"
```

---

### Task 4: Wire `TokenAssigner` into `FrontMatterProcessor`

**Files:**
- Modify: `src/Tokenizer/Tokenization/FrontMatterProcessor.cs` — change signature
- Modify: `src/Tokenizer/Tokenization/TokenizationSession.cs:140` — pass `_assigner` to `FrontMatterProcessor`

Depends on: Task 3.

- [ ] **Step 1: Update `FrontMatterProcessor.Process` signature**

In `src/Tokenizer/Tokenization/FrontMatterProcessor.cs`, update the method signature and body:

```csharp
    public static void Process(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        TokenAssigner assigner,
        FileLocation location)
    {
        foreach (var token in template.Tokens)
        {
            if (!token.IsFrontMatterToken) continue;

            if (assigner.Assign(token, targetObject, string.Empty, location, out var assignedValue))
            {
                if (assigner.Collector.IsEnabled)
                {
                    assigner.Collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                        tokenName: token.Name, tokenId: token.Id,
                        value: assignedValue?.ToString());
                }
                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(token, assignedValue, token.Location);
                }
            }
            else
            {
                if (assigner.Collector.IsEnabled)
                {
                    assigner.Collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                        tokenName: token.Name, tokenId: token.Id);
                }
            }
        }
    }
```

Wait — the spec says diagnostic recording stays in `FrontMatterProcessor`, but currently it uses `collector` directly. We need `FrontMatterProcessor` to access the collector. Two options: pass `collector` as a separate param, or expose it on `TokenAssigner`. Since the spec says to remove `IDiagnosticCollector` from the signature, we should expose `Collector` on `TokenAssigner`.

Add to `src/Tokenizer/Tokenization/TokenAssigner.cs`:
```csharp
    internal IDiagnosticCollector Collector => _collector;
```

- [ ] **Step 2: Update `TokenizationSession.Finalize` to pass `_assigner`**

In `src/Tokenizer/Tokenization/TokenizationSession.cs`, update the `Finalize` method:

```csharp
        FrontMatterProcessor.Process(_template, _targetObject, _result, _assigner, context.Enumerator.Location);
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenization/FrontMatterProcessor.cs src/Tokenizer/Tokenization/TokenizationSession.cs src/Tokenizer/Tokenization/TokenAssigner.cs
git commit -m "refactor: wire TokenAssigner into FrontMatterProcessor"
```

---

### Task 5: Strip `Token` to pure data model

**Files:**
- Modify: `src/Tokenizer/TokenResult.cs:30-44` — use `ValueConcatenation` before removing `Token` statics
- Modify: `src/Tokenizer/Token.cs` — remove all assignment logic
- Modify: `src/Tokenizer/Compilation/Binders/TokenFactory.cs:16` — drop `content` param
- Modify: `tests/Tokenizer.Tests/Builders/TokenBuilder.cs:12-18,102-104` — drop `_content` field and constructor param
- Delete: `tests/Tokenizer.Tests/TokenTests.cs` — replaced by `TokenAssignerTests`
- Modify: `tests/Tokenizer.Tests/Compilation/Binders/TokenFactoryTests.cs:32` — remove `ToString()` assertion

Depends on: Tasks 1, 3, and 4 (all callers of `Token.Assign`, `Token.CanAssign`, and `Token` static methods must be migrated first).

- [ ] **Step 1: Update `TokenResult` to use `ValueConcatenation`**

In `src/Tokenizer/TokenResult.cs`, add `using Tokens.Extensions;` and update `TryConcatMatch`:

```csharp
    private bool TryConcatMatch(Token token, object value)
    {
        if (!token.CanConcatenate) return false;

        var index = _matches.FindIndex(m => string.Equals(m.Token.Name, token.Name, StringComparison.Ordinal));
        if (index < 0) return false;

        var match = _matches[index];

        if (!ValueConcatenation.CanConcatenate(match.Value, value)) return false;

        var concatenated = ValueConcatenation.Concatenate(match.Value, value, token.ConcatenationString);
        if (concatenated != null) _matches[index] = match with { Value = concatenated };

        return true;
    }
```

This must be done before stripping the `Token` statics in the next step.

- [ ] **Step 2: Strip `Token.cs`**

Replace the entire `Token.cs` with the pure data model. Remove:
- `_content` field
- `content` constructor parameter  
- `ToString()` override
- `Assign()` method
- `CanAssign()` method
- `PrepareValue()` method
- `RunDecoratorPipeline()` method
- `SetDictionaryValue()` method
- `CanConcatenateValues()` static method
- `ConcatenateValues()` static method

Add `[DebuggerDisplay]` attribute. Remove unused `using` statements (`Tokens.Diagnostics`, `Tokens.Exceptions`, `Tokens.Extensions`).

The resulting file should be:

```csharp
using System.Diagnostics;
using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Represents a single token in a string.
/// Properties use <c>internal set</c> because they are populated by the compilation
/// pipeline (TokenBinder and OptionApplier) after construction.
/// </summary>
[DebuggerDisplay("{Name} (Id={Id}, Optional={IsOptional})")]
public sealed class Token
{
    private readonly List<TokenDecoratorContext> _decorators;

    /// <summary>
    /// Creates a new <see cref="Token"/> with the specified name, preamble, and source location.
    /// </summary>
    /// <param name="name">The token name used to map the extracted value to a target property.</param>
    /// <param name="preamble">The static text that must precede this token in the input.</param>
    /// <param name="location">The location of this token within the template pattern.</param>
    public Token(string name, string preamble, FileLocation location)
    {
        Name = name;
        Preamble = preamble;
        Location = location;
        _decorators = new List<TokenDecoratorContext>();
    }

    /// <summary>
    /// Gets or sets the preamble string that must appear before the token.
    /// </summary>
    public string Preamble { get; internal set; }

    /// <summary>
    /// Gets or sets the value of the token.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// Gets the decorators on this Token
    /// </summary>
    public IReadOnlyList<TokenDecoratorContext> Decorators => _decorators;

    internal void AddDecorator(TokenDecoratorContext decorator)
    {
        _decorators.Add(decorator);
    }

    /// <summary>
    /// If <see langword="true"/> then this <see cref="Token"/> is optional and can be skipped
    /// during processing.
    /// </summary>
    public bool IsOptional { get; internal set; }

    /// <summary>
    /// If <see langword="true"/> then this <see cref="Token"/> can map multiple instances onto
    /// an <see cref="IList{T}"/>.
    /// </summary>
    public bool IsRepeating { get; internal set; }

    /// <summary>
    /// If <see langword="true"/> then this <see cref="Token"/> will map a value up to the next
    /// newline.
    /// </summary>
    public bool TerminateOnNewLine { get; internal set; }

    /// <summary>
    /// If <see langword="true"/> then this <see cref="Token"/> must be present in the input for
    /// the processing to be successful.
    /// </summary>
    public bool IsRequired { get; internal set; }

    /// <summary>
    /// The unique id of this token in the <see cref="Template"/>.
    /// </summary>
    public int Id { get; internal set; }

    /// <summary>
    /// Defines a token that must have been matched in the input before this token
    /// can be considered.  Used with repeating tokens that would otherwise be
    /// to aggressive in their matching.
    /// </summary>
    public int DependsOnId { get; internal set; } = -1;

    /// <summary>
    /// Determines if this <see cref="Token"/> was defined in the template front matter section.
    /// </summary>
    public bool IsFrontMatterToken { get; internal set; }

    /// <summary>
    /// Determines if this token is a null placeholder
    /// </summary>
    public bool IsNull { get; internal set; }

    /// <summary>
    /// The location of this token in the template.
    /// </summary>
    public FileLocation Location { get; internal set; }

    /// <summary>
    /// If true, multiple instances of this token will be concatenated together
    /// on the target.
    /// </summary>
    public bool CanConcatenate { get; internal set; }

    /// <summary>
    /// Defines a joining string to use when concatenating two token values.
    /// </summary>
    public string? ConcatenationString { get; internal set; }

    /// <summary>
    /// If true, this token will only be attempted to be matched once.
    /// </summary>
    public bool IsSingleUse { get; internal set; }
}
```

- [ ] **Step 3: Update `TokenFactory.Create` — drop `content` param**

In `src/Tokenizer/Compilation/Binders/TokenFactory.cs`, line 16, change:

```csharp
        // Old:
        var token = new Token(definition.Content, definition.Name ?? string.Empty, preamble, location);
        // New:
        var token = new Token(definition.Name ?? string.Empty, preamble, location);
```

- [ ] **Step 4: Update `TokenBuilder` — drop `_content`**

In `tests/Tokenizer.Tests/Builders/TokenBuilder.cs`:

Remove the `_content` field and `WithContent` method. Update `Build()`:

```csharp
    public Token Build()
    {
        var token = new Token(_name, _preamble, _location);
        foreach (var config in _configurations) config(token);
        return token;
    }
```

- [ ] **Step 5: Delete `TokenTests.cs`**

Delete `tests/Tokenizer.Tests/TokenTests.cs` — all its test cases are now covered by `TokenAssignerTests.cs`.

- [ ] **Step 6: Update `TokenFactoryTests.cs` — remove `ToString()` assertion**

In `tests/Tokenizer.Tests/Compilation/Binders/TokenFactoryTests.cs`, line 32, remove:

```csharp
        Assert.Equal("{Name}", token.ToString());
```

- [ ] **Step 7: Update all `new Token(` calls in test files**

Every `new Token("content", ...)` call across test files needs the first `"content"` argument removed. The affected files and their call patterns:

**`CandidateTokenListTests.cs`** — all `new Token("content", ...)` and `new Token("foo", ...)` and `new Token("c1", ...)` etc. become `new Token(...)` with 3 args. For example:
```csharp
// Old:
new Token("content", "Name", "preamble", NoLocation)
// New:
new Token("Name", "preamble", NoLocation)

// Old:
new Token("foo", string.Empty, "bar", NoLocation)
// New:
new Token(string.Empty, "bar", NoLocation)

// Old:
new Token("c1", "Name1", "pre1", NoLocation)
// New:
new Token("Name1", "pre1", NoLocation)
```

**`TokenAssignmentExceptionTests.cs`** — all `new Token("content", "MyToken", "preamble", new FileLocation())` become `new Token("MyToken", "preamble", new FileLocation())`.

**`TokenResultTests.cs`** — `new Token("content", "name", "Name:", new FileLocation())` becomes `new Token("name", "Name:", new FileLocation())`.

**`TokenMatchTests.cs`** — `new Token("content", "firstName", "Name:", new FileLocation())` becomes `new Token("firstName", "Name:", new FileLocation())`.

**`TokenizationContextTests.cs`** — `new Token("test", string.Empty, string.Empty, ...)` becomes `new Token(string.Empty, string.Empty, ...)`.

**`TokenCountValidatorTests.cs`** — e.g. `new Token("a", "A", "", new FileLocation())` becomes `new Token("A", "", new FileLocation())`.

**`DecoratorBinderTests.cs`** — e.g. `new Token("{Foo}", "Foo", "", new FileLocation())` becomes `new Token("Foo", "", new FileLocation())`.

**`OptionApplierTests.cs`** — e.g. `new Token("{Name}", "Name", "Preamble", new FileLocation())` becomes `new Token("Name", "Preamble", new FileLocation())`.

**`RepeatingTokenLinkerTests.cs`** — e.g. `new Token("{Name}", "Name", "Preamble\n", new FileLocation())` becomes `new Token("Name", "Preamble\n", new FileLocation())`.

- [ ] **Step 8: Build and run all tests**

Run: `dotnet build ./Tokenizer.sln -c Release`
Then: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: Build succeeds. All tests PASS.

- [ ] **Step 9: Commit**

```bash
git add -A  # After verifying with git status
git commit -m "refactor: strip Token to pure data model, remove assignment logic"
```

---

### Task 6: Fix `ObjectExtensions.SetValue` default comparison (M5)

**Files:**
- Modify: `src/Tokenizer/Extensions/ObjectExtensions.cs:25-28`

Independent of other tasks.

- [ ] **Step 1: Update the default**

In `src/Tokenizer/Extensions/ObjectExtensions.cs`, change line 27:

```csharp
    // Old:
    public static T SetValue<T>(this T @object, string propertyPath, object value) where T : class
    {
        return SetValue(@object, propertyPath, value, StringComparison.InvariantCulture);
    }

    // New:
    public static T SetValue<T>(this T @object, string propertyPath, object value) where T : class
    {
        return SetValue(@object, propertyPath, value, StringComparison.Ordinal);
    }
```

- [ ] **Step 2: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS.

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Extensions/ObjectExtensions.cs
git commit -m "fix: change ObjectExtensions.SetValue default comparison to Ordinal (M5)"
```

---

### Task 7: Final verification

**Files:** None — verification only.

- [ ] **Step 1: Full build**

Run: `dotnet build ./Tokenizer.sln -c Release`
Expected: Build succeeds with no warnings.

- [ ] **Step 2: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS.

- [ ] **Step 3: Run code style checks**

Run: `dotnet format style ./Tokenizer.sln --verify-no-changes`
Expected: No formatting violations.

- [ ] **Step 4: Verify Token is pure data**

Grep to confirm no assignment logic remains on Token:

```bash
grep -n "Assign\|PrepareValue\|RunDecorator\|ConcatenateValues\|_content" src/Tokenizer/Token.cs
```

Expected: No matches.

- [ ] **Step 5: Verify review issues resolved**

Check each issue:
- D1: `Token.cs` has no methods beyond `AddDecorator`
- D2: `CandidateProcessor.cs` has a single `catch (Exception)` block
- H9: No `TokenAssignmentException` wrapping in generic catch — only thrown explicitly for concat mismatch
- M5: `ObjectExtensions.cs` defaults to `StringComparison.Ordinal`
- L1: `ValueConcatenation.cs` uses `CanConcatenate`/`Concatenate` names
