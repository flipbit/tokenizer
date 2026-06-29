# Tokenization Safety Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the empty-preamble infinite loop bug and add DoS protection limits to the tokenization engine.

**Architecture:** Add `replacement.Length > 0` guard before `HandleTokenSwitch` in the engine's main loop to prevent zero-progress iterations. Add four safety limit properties to `TokenizerOptions` with checks at the appropriate pipeline stages. All limit violations throw exceptions extending `TokenizerException`.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 6.0, xUnit, TokenParser for template creation in tests

**Spec:** `docs/superpowers/specs/2026-06-29-tokenization-safety-design.md`

---

### File Map

| File | Action | Responsibility |
|------|--------|---------------|
| `src/Tokenizer/TokenizerOptions.cs` | Modify | Add 4 safety limit properties + update Clone() |
| `src/Tokenizer/Tokenization/TokenizationEngine.cs` | Modify | Add replacement guard + iteration counter |
| `src/Tokenizer/Compilation/TokenParser.cs` | Modify | Add template length + token count checks |
| `src/Tokenizer/Tokenizer.cs` | Modify | Add input length check |
| `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEmptyPreambleTests.cs` | Create | Empty-preamble bug tests |
| `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs` | Create | Safety limit tests |

---

### Task 1: Red — Empty-preamble core behavior tests

**Files:**
- Create: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEmptyPreambleTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using Tokens.Compilation;
using Tokens.Tokenization;
using Tokens.Builders;
using Xunit;

namespace Tokens.Tests.Tokenization.Engine;

public class TokenizationEngineEmptyPreambleTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenConsecutiveTokensWithNoPreambles_WhenTokenizing_ThenAssignsOneCharEach()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}{b}{c}");
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, "abc", null, context, result);

        // Assert
        Assert.Equal(3, result.Tokens.Matches.Count);
        Assert.Equal("a", result.Tokens.Matches[0].Value);
        Assert.Equal("b", result.Tokens.Matches[1].Value);
        Assert.Equal("c", result.Tokens.Matches[2].Value);
    }

    [Fact]
    public void GivenConsecutiveTokensWithNoPreambles_WhenInputLongerThanTokens_ThenLastTokenGetsRemainder()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}{b}{c}");
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, "abcdef", null, context, result);

        // Assert
        Assert.Equal(3, result.Tokens.Matches.Count);
        Assert.Equal("a", result.Tokens.Matches[0].Value);
        Assert.Equal("b", result.Tokens.Matches[1].Value);
        Assert.Equal("cdef", result.Tokens.Matches[2].Value);
    }

    [Fact]
    public void GivenConsecutiveTokensWithNoPreambles_WhenInputShorterThanTokens_ThenUnmatchedTokensAreMisses()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}{b}{c}");
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, "ab", null, context, result);

        // Assert
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal("a", result.Tokens.Matches[0].Value);
        Assert.Equal("b", result.Tokens.Matches[1].Value);
    }

    [Fact]
    public void GivenSingleTokenWithNoPreamble_WhenTokenizing_ThenGetsEntireInput()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}");
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, "hello", null, context, result);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("hello", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenMixedPreambleAndNoPreambleTokens_WhenTokenizing_ThenMatchesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("X{a}{b}Y{c}");
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, "XabYc", null, context, result);

        // Assert
        Assert.Equal(3, result.Tokens.Matches.Count);
        Assert.Equal("a", result.Tokens.Matches[0].Value);
        Assert.Equal("b", result.Tokens.Matches[1].Value);
        Assert.Equal("c", result.Tokens.Matches[2].Value);
    }

    [Fact]
    public void GivenTwoConsecutiveTokens_WhenSingleCharInput_ThenFirstTokenMatchesSecondMisses()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{a}{b}");
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        // Act
        _engine.ProcessTokenization(template, "x", null, context, result);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("x", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenManyConsecutiveTokensWithNoPreambles_WhenTokenizing_ThenCompletes()
    {
        // Arrange
        var templateBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            templateBuilder.Append($"{{t{i}}}");
        }

        var parser = new TokenParser();
        var template = parser.Parse(templateBuilder.ToString());
        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();

        var input = new string('x', 100);

        // Act
        _engine.ProcessTokenization(template, input, null, context, result);

        // Assert — the key thing is that this completes (does not hang)
        Assert.Equal(100, result.Tokens.Matches.Count);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail (hang or wrong results)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenizationEngineEmptyPreambleTests" --timeout 10000 -v q`

Expected: Tests hang or fail. The `GivenManyConsecutiveTokensWithNoPreambles` test will hang (infinite loop). Others may hang too.

- [ ] **Step 3: Commit the red tests**

```bash
git add tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEmptyPreambleTests.cs
git commit -m "Add failing tests for empty-preamble tokenization bug"
```

---

### Task 2: Green — Fix the empty-preamble infinite loop

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs:125-156`

- [ ] **Step 1: Add the replacement.Length guard**

In `TokenizationEngine.cs`, find the main tokenization loop (the `if` block starting around line 125 that checks `context.Enumerator.Match(...)`). Change the token switch logic from:

```csharp
                    // We have candidates and found a new token -> always switch
                    HandleTokenSwitch(context, template, targetObject, result, matches, lineTracker);
```

To:

```csharp
                    // Only switch if we've accumulated a value — otherwise consume a character first
                    if (context.Replacement.Length > 0)
                    {
                        HandleTokenSwitch(context, template, targetObject, result, matches, lineTracker);
                    }
                    else
                    {
                        HandleNoTokenMatch(context, next);
                    }
```

This is at approximately line 150-151 in the current file. The full `if` block should now read:

```csharp
                    if (context.Enumerator.Match(template.TokensExcluding(context.MatchIds, context.Candidates, context.DisabledRepeatingTokens), template.Options.OutOfOrderTokens, out var matches))
                    {
                        log.LogTrace
                        (
                            "Token match found at Line {Line}, Column {Column}. Matched {MatchCount} token(s): {TokenNames}",
                            context.Enumerator.Location.Line, 
                            context.Enumerator.Location.Column,
                            matches.Count, 
                            string.Join(", ", matches.Select(m => m.Name))
                        );

                        // Special case: first token found, just prepare to read token value
                        if (context.Candidates.Any == false)
                        {
                            HandleFirstTokenMatch(context, matches);
                            continue;
                        }
                        
                        // Check candidates hasn't changed
                        {
                            
                        }
                        
                        

                        // Only switch if we've accumulated a value — otherwise consume a character first
                        if (context.Replacement.Length > 0)
                        {
                            HandleTokenSwitch(context, template, targetObject, result, matches, lineTracker);
                        }
                        else
                        {
                            HandleNoTokenMatch(context, next);
                        }
                    }
                    else
                    {
                        HandleNoTokenMatch(context, next);
                    }
```

- [ ] **Step 2: Run the empty-preamble tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenizationEngineEmptyPreambleTests" -v q`

Expected: All 7 tests PASS.

- [ ] **Step 3: Run the full test suite to check for regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v q 2>&1 | tail -3`

Expected: No new failures. The existing 37 null-target fixes + skipped TestAmazonCoJp should be the only non-passing tests remaining (if any).

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationEngine.cs
git commit -m "Fix empty-preamble infinite loop in tokenization engine

Add replacement.Length > 0 guard before HandleTokenSwitch to ensure
the enumerator always makes progress. Without this guard, consecutive
tokens with empty preambles cause the engine to cycle through all
tokens without consuming any input characters."
```

---

### Task 3: Red — Safety limit option tests

**Files:**
- Create: `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`

- [ ] **Step 1: Write the failing tests for MaxInputLength**

```csharp
using System;
using Tokens;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Tests.Safety;

public class TokenizerSafetyLimitTests
{
    [Fact]
    public void GivenInputExceedingMaxLength_WhenTokenizing_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxInputLength = 100;
        var tokenizer = Tokenizer.Create(options);
        var input = new string('x', 101);

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() =>
            tokenizer.Tokenize("Name: {Name}", input));
        Assert.Contains("101", ex.Message);
        Assert.Contains("100", ex.Message);
        Assert.Contains("MaxInputLength", ex.Message);
    }

    [Fact]
    public void GivenInputAtMaxLength_WhenTokenizing_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxInputLength = 100;
        var tokenizer = Tokenizer.Create(options);
        var input = "Name: " + new string('x', 94);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", input);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenMaxInputLengthDisabled_WhenTokenizingLargeInput_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxInputLength = 0;
        var tokenizer = Tokenizer.Create(options);
        var input = "Name: " + new string('x', 200_000);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", input);

        // Assert
        Assert.NotNull(result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenizerSafetyLimitTests" -v q`

Expected: FAIL — `MaxInputLength` property does not exist yet.

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs
git commit -m "Add failing tests for MaxInputLength safety limit"
```

---

### Task 4: Green — Add MaxInputLength to options and enforce in Tokenizer

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Modify: `src/Tokenizer/Tokenizer.cs:115-132`

- [ ] **Step 1: Add MaxInputLength property to TokenizerOptions**

In `TokenizerOptions.cs`, add the property after the existing properties (before the `Clone()` method):

```csharp
    /// <summary>
    /// Maximum allowed length for input text. Default: 1,048,576 (1MB).
    /// Set to 0 to disable.
    /// </summary>
    public int MaxInputLength { get; set; } = 1_048_576;
```

Update the `Clone()` method to include the new property. Add this line inside the object initializer:

```csharp
                MaxInputLength = MaxInputLength,
```

- [ ] **Step 2: Enforce MaxInputLength in Tokenizer.Tokenize()**

In `Tokenizer.cs`, at the start of the private `Tokenize` method (line 115), add the check before the `using` block:

```csharp
        private void Tokenize(TokenizeResultBase result, object value, Template template, string input)
        {
            // Safety limit: maximum input length
            if (template.Options.MaxInputLength > 0 && input.Length > template.Options.MaxInputLength)
            {
                throw new TokenizerException(
                    $"Input length {input.Length:N0} exceeds maximum allowed length of {template.Options.MaxInputLength:N0}. " +
                    "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
            }

            using (log.BeginScope(new Dictionary<string, object>
```

- [ ] **Step 3: Run the MaxInputLength tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenizerSafetyLimitTests" -v q`

Expected: All 3 tests PASS.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/TokenizerOptions.cs src/Tokenizer/Tokenizer.cs
git commit -m "Add MaxInputLength safety limit

Throws TokenizerException when input exceeds configurable limit.
Default 1MB. Set to 0 to disable."
```

---

### Task 5: Red — Template length and token count limit tests

**Files:**
- Modify: `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`

- [ ] **Step 1: Add failing tests for MaxTemplateLength and MaxTokenCount**

Append these tests to the existing `TokenizerSafetyLimitTests` class:

```csharp
    [Fact]
    public void GivenTemplateExceedingMaxLength_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxTemplateLength = 50;
        var tokenizer = Tokenizer.Create(options);
        var longTemplate = "Name: {Name}" + new string(' ', 50);

        // Act & Assert
        var ex = Assert.Throws<ParsingException>(() =>
            tokenizer.Tokenize(longTemplate, "Name: John"));
        Assert.Contains("MaxTemplateLength", ex.Message);
    }

    [Fact]
    public void GivenTemplateAtMaxLength_WhenParsing_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxTemplateLength = 100;
        var tokenizer = Tokenizer.Create(options);
        var template = "Name: {Name}";

        // Act
        var result = tokenizer.Tokenize(template, "Name: John");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenMaxTemplateLengthDisabled_WhenParsingLargeTemplate_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxTemplateLength = 0;
        var tokenizer = Tokenizer.Create(options);
        var template = "Name: {Name}" + new string(' ', 100_000);

        // Act
        var result = tokenizer.Tokenize(template, "Name: John");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenTemplateExceedingMaxTokenCount_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxTokenCount = 5;
        var tokenizer = Tokenizer.Create(options);

        var templateBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < 6; i++)
        {
            templateBuilder.Append($"T{i}: {{Token{i}}}\n");
        }

        // Act & Assert
        var ex = Assert.Throws<ParsingException>(() =>
            tokenizer.Tokenize(templateBuilder.ToString(), "T0: Value"));
        Assert.Contains("6", ex.Message);
        Assert.Contains("5", ex.Message);
        Assert.Contains("MaxTokenCount", ex.Message);
    }

    [Fact]
    public void GivenTemplateAtMaxTokenCount_WhenParsing_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxTokenCount = 5;
        var tokenizer = Tokenizer.Create(options);

        var templateBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < 5; i++)
        {
            templateBuilder.Append($"T{i}: {{Token{i}}}\n");
        }

        // Act
        var result = tokenizer.Tokenize(templateBuilder.ToString(), "T0: Value0\nT1: Value1");

        // Assert
        Assert.NotNull(result);
    }
```

- [ ] **Step 2: Add the missing using for ParsingException**

Add to the top of `TokenizerSafetyLimitTests.cs`:

```csharp
using Tokens.Exceptions;
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenizerSafetyLimitTests" -v q`

Expected: New tests FAIL — `MaxTemplateLength` and `MaxTokenCount` properties don't exist yet.

- [ ] **Step 4: Commit**

```bash
git add tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs
git commit -m "Add failing tests for MaxTemplateLength and MaxTokenCount limits"
```

---

### Task 6: Green — Add MaxTemplateLength and MaxTokenCount

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Modify: `src/Tokenizer/Compilation/TokenParser.cs:96-104`

- [ ] **Step 1: Add properties to TokenizerOptions**

Add after `MaxInputLength`:

```csharp
    /// <summary>
    /// Maximum allowed length for template pattern text. Default: 65,536 (64KB).
    /// Set to 0 to disable.
    /// </summary>
    public int MaxTemplateLength { get; set; } = 65_536;

    /// <summary>
    /// Maximum number of tokens allowed in a template. Default: 500.
    /// Set to 0 to disable.
    /// </summary>
    public int MaxTokenCount { get; set; } = 500;
```

Add to `Clone()`:

```csharp
                MaxTemplateLength = MaxTemplateLength,
                MaxTokenCount = MaxTokenCount,
```

- [ ] **Step 2: Enforce limits in TokenParser.Parse()**

In `TokenParser.cs`, in the `Parse(string content, string name)` method, add the template length check at the very start of the method (after the stopwatch setup, before `new Template(...)` on line 115):

```csharp
            if (Options.MaxTemplateLength > 0 && content.Length > Options.MaxTemplateLength)
            {
                throw new ParsingException(
                    $"Template length {content.Length:N0} exceeds maximum allowed length of {Options.MaxTemplateLength:N0}. " +
                    "Increase TokenizerOptions.MaxTemplateLength to allow larger templates.");
            }
```

Add the token count check after the foreach loop that adds tokens to the template (just before the `log.LogTrace("Parsed '{TemplateName}'..."` line around line 235):

```csharp
            if (Options.MaxTokenCount > 0 && template.Tokens.Count > Options.MaxTokenCount)
            {
                throw new ParsingException(
                    $"Template contains {template.Tokens.Count} tokens, exceeding maximum of {Options.MaxTokenCount:N0}. " +
                    "Increase TokenizerOptions.MaxTokenCount to allow more tokens.");
            }
```

- [ ] **Step 3: Run the template limit tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenizerSafetyLimitTests" -v q`

Expected: All 8 tests PASS.

- [ ] **Step 4: Run full test suite for regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v q 2>&1 | tail -3`

Expected: No new failures. Note: the `GivenManySmallTokens` performance test creates 500 tokens which now exactly equals `MaxTokenCount`. It should still pass since the check is `>`, not `>=`.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/TokenizerOptions.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add MaxTemplateLength and MaxTokenCount safety limits

Throws ParsingException when template exceeds size or token count.
Defaults: 64KB template length, 500 tokens. Set to 0 to disable."
```

---

### Task 7: Red — MaxIterations limit tests

**Files:**
- Modify: `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`

- [ ] **Step 1: Add failing tests for MaxIterations**

Append these tests to `TokenizerSafetyLimitTests`:

```csharp
    [Fact]
    public void GivenMaxIterationsExceeded_WhenTokenizing_ThenThrowsTokenizerException()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxIterations = 5;
        var tokenizer = Tokenizer.Create(options);

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() =>
            tokenizer.Tokenize("Name: {Name}", "Name: John Doe"));
        Assert.Contains("MaxIterations", ex.Message);
    }

    [Fact]
    public void GivenAutoMaxIterations_WhenTokenizingNormalInput_ThenProcessesSuccessfully()
    {
        // Arrange — default MaxIterations=0 means auto (input.Length * 2)
        var options = TokenizerOptions.Defaults;
        var tokenizer = Tokenizer.Create(options);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", "Name: John");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenCustomMaxIterations_WhenWithinLimit_ThenProcessesSuccessfully()
    {
        // Arrange
        var options = TokenizerOptions.Defaults;
        options.MaxIterations = 10000;
        var tokenizer = Tokenizer.Create(options);

        // Act
        var result = tokenizer.Tokenize("Name: {Name}", "Name: John");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenDefaultOptions_WhenTokenizingNormalInput_ThenProcessesSuccessfully()
    {
        // Arrange — verify defaults don't interfere with normal usage
        var tokenizer = Tokenizer.Create();

        // Act
        var result = tokenizer.Tokenize("Name: {Name}\nAge: {Age}", "Name: John\nAge: 30");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
    }
```

- [ ] **Step 2: Run to verify failures**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenizerSafetyLimitTests.GivenMaxIterations OR FullyQualifiedName~TokenizerSafetyLimitTests.GivenCustomMaxIterations OR FullyQualifiedName~TokenizerSafetyLimitTests.GivenDefaultOptions" -v q`

Expected: Tests referencing `MaxIterations` FAIL (property doesn't exist). `GivenDefaultOptions` may pass since it doesn't use new properties.

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs
git commit -m "Add failing tests for MaxIterations safety limit"
```

---

### Task 8: Green — Add MaxIterations to options and enforce in engine

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs:97-101`

- [ ] **Step 1: Add MaxIterations property to TokenizerOptions**

Add after `MaxTokenCount`:

```csharp
    /// <summary>
    /// Maximum number of iterations in the tokenization loop.
    /// Default: 0 (auto-calculated as input.Length * 2).
    /// Set to a positive value to override.
    /// </summary>
    public int MaxIterations { get; set; } = 0;
```

Add to `Clone()`:

```csharp
                MaxIterations = MaxIterations,
```

- [ ] **Step 2: Enforce MaxIterations in the engine's main loop**

In `TokenizationEngine.cs`, add an iteration counter before the `while` loop and a check inside it.

Before the `while` loop (around line 100, after the `log.LogDebug("Phase: Initialization completed...")`):

```csharp
            var iterationCount = 0;
            var maxIterations = template.Options.MaxIterations > 0
                ? template.Options.MaxIterations
                : input.Length * 2;
```

At the very start of the `while` loop body (line 102, right after `while (context.Enumerator.IsEmpty == false)`):

```csharp
                    iterationCount++;
                    if (iterationCount > maxIterations)
                    {
                        throw new TokenizerException(
                            $"Tokenization exceeded maximum iteration count of {maxIterations:N0}. " +
                            "This may indicate a problematic template pattern. " +
                            "Increase TokenizerOptions.MaxIterations to allow more iterations.");
                    }
```

Add `using Tokens.Exceptions;` to the top of the file if not already present.

- [ ] **Step 3: Run the MaxIterations tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenizerSafetyLimitTests" -v q`

Expected: All 12 tests PASS.

- [ ] **Step 4: Run the full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v q 2>&1 | tail -3`

Expected: All tests pass (0 failures, 1 skipped for TestAmazonCoJp).

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/TokenizerOptions.cs src/Tokenizer/Tokenization/TokenizationEngine.cs
git commit -m "Add MaxIterations safety limit

Auto-calculated as input.Length * 2 by default. Throws
TokenizerException if exceeded. Set a positive value to override,
or rely on the default to catch infinite loops and pathological
template patterns."
```

---

### Task 9: Final verification and cleanup

- [ ] **Step 1: Run the complete test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v q 2>&1 | tail -5`

Expected: `Passed: XXX, Failed: 0, Skipped: 1` (TestAmazonCoJp skipped).

- [ ] **Step 2: Verify the GivenManySmallTokens performance test no longer hangs**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~GivenManySmallTokens" -v q`

Expected: PASS (was previously hanging indefinitely).

- [ ] **Step 3: Review all changes**

Run: `git diff HEAD~8 --stat` to see all files changed across the implementation.

- [ ] **Step 4: Commit any remaining cleanup**

If any test output or formatting needs attention, fix and commit.
