# TokenizationEngine Refactoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `TokenizationEngine` from a monolithic 566-line class into a thin orchestrator over focused modules (`InputValidator`, `FrontMatterProcessor`, `CandidateProcessor`, `TokenMatchRouter`, `TokenizationSession`), unifying the sync and async code paths.

**Architecture:** Composition-based decomposition. The engine becomes a factory that creates `TokenizationSession` instances. Each session holds sub-components at construction time, eliminating parameter passing in hot-path methods. Sync/async unification via `Run`/`RunAsync` entry points that share a single `ProcessChunk` algorithm.

**Tech Stack:** C# (.NET Standard 2.0 / .NET 6.0 dual-target), xUnit, NSubstitute

---

### Task 1: Extract `InputValidator`

Extract the target object validation logic from `BeginTokenization` into a static `InputValidator` class.

**Files:**
- Create: `src/Tokenizer/Tokenization/InputValidator.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Engine/InputValidatorTests.cs`

- [ ] **Step 1: Write failing tests for InputValidator**

```csharp
// tests/Tokenizer.Tests/Tokenization/Engine/InputValidatorTests.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tokens.Tokenization.Engine;

public class InputValidatorTests
{
    private readonly ILogger<TokenizationEngine> _logger = NullLogger<TokenizationEngine>.Instance;

    [Fact]
    public void GivenNullTarget_WhenValidating_ThenDoesNotThrow()
    {
        // Act & Assert
        InputValidator.ValidateTargetObject(null, _logger);
    }

    [Fact]
    public void GivenDictionaryTarget_WhenValidating_ThenDoesNotThrow()
    {
        // Arrange
        var target = new Dictionary<string, object>();

        // Act & Assert
        InputValidator.ValidateTargetObject(target, _logger);
    }

    [Fact]
    public void GivenWritableTarget_WhenValidating_ThenDoesNotThrow()
    {
        // Arrange
        var target = new WritableTarget();

        // Act & Assert
        InputValidator.ValidateTargetObject(target, _logger);
    }

    [Fact]
    public void GivenReadOnlyTarget_WhenValidating_ThenThrowsArgumentException()
    {
        // Arrange
        var target = new ReadOnlyTarget("test");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            InputValidator.ValidateTargetObject(target, _logger));
        Assert.Contains("no settable properties", ex.Message);
    }

    private class WritableTarget
    {
        public string Name { get; set; } = null!;
    }

    private sealed class ReadOnlyTarget
    {
        public ReadOnlyTarget(string name) { Name = name; }
        public string Name { get; }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "InputValidatorTests"`
Expected: FAIL — `InputValidator` does not exist

- [ ] **Step 3: Implement InputValidator**

```csharp
// src/Tokenizer/Tokenization/InputValidator.cs
using Microsoft.Extensions.Logging;

namespace Tokens.Tokenization;

/// <summary>
/// Validates target objects before tokenization begins.
/// </summary>
internal static class InputValidator
{
    /// <summary>
    /// Validates that the target object has settable properties if it is not null and not a dictionary.
    /// </summary>
    public static void ValidateTargetObject(object? targetObject, ILogger logger)
    {
        if (targetObject == null || targetObject is System.Collections.Generic.IDictionary<string, object>)
        {
            return;
        }

        var properties = targetObject.GetType().GetProperties();
        var hasSettableProperty = properties.Any(p => p.CanWrite && p.GetSetMethod() != null);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Target object type: {TypeName}, Properties: {PropertyCount}, Settable: {SettableCount}",
                targetObject.GetType().Name,
                properties.Length,
                properties.Count(p => p.CanWrite && p.GetSetMethod() != null));
        }

        if (!hasSettableProperty)
        {
            throw new ArgumentException(
                $"Target object of type '{targetObject.GetType().Name}' has no settable properties. " +
                "Anonymous types and objects with read-only properties cannot be used as tokenization targets. " +
                "Consider using a class with writable properties or passing null as the target.",
                nameof(targetObject));
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "InputValidatorTests"`
Expected: PASS — all 4 tests green

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenization/InputValidator.cs tests/Tokenizer.Tests/Tokenization/Engine/InputValidatorTests.cs
git commit -m "feat: extract InputValidator from TokenizationEngine"
```

---

### Task 2: Extract `FrontMatterProcessor`

Extract the front matter token processing logic from `EndTokenization` into a static class.

**Files:**
- Create: `src/Tokenizer/Tokenization/FrontMatterProcessor.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Engine/FrontMatterProcessorTests.cs`

- [ ] **Step 1: Write failing tests for FrontMatterProcessor**

```csharp
// tests/Tokenizer.Tests/Tokenization/Engine/FrontMatterProcessorTests.cs
using Tokens.Builders;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Tokenization.Engine;

public class FrontMatterProcessorTests
{
    [Fact]
    public void GivenFrontMatterToken_WhenProcessing_ThenAssignsAndRecordsMatch()
    {
        // Arrange
        var token = new TokenBuilder()
            .WithName("TemplateName")
            .WithFrontMatter()
            .Build();
        var template = new TemplateBuilder()
            .WithName("Test")
            .WithTokens(token)
            .WithDefaultOptions()
            .Build();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var collector = new DiagnosticCollector(null, null);
        var location = new FileLocation();

        // Act
        FrontMatterProcessor.Process(template, null, result, collector, location);

        // Assert
        Assert.Contains(collector.GetResult()!.Events,
            e => e.Type == DiagnosticEventType.FrontMatterTokenAssigned);
    }

    [Fact]
    public void GivenNonFrontMatterToken_WhenProcessing_ThenSkipsToken()
    {
        // Arrange
        var token = new TokenBuilder()
            .WithName("Name")
            .WithPreamble("Name: ")
            .Build();
        var template = new TemplateBuilder()
            .WithName("Test")
            .WithTokens(token)
            .WithDefaultOptions()
            .Build();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var collector = new DiagnosticCollector(null, null);
        var location = new FileLocation();

        // Act
        FrontMatterProcessor.Process(template, null, result, collector, location);

        // Assert — no front matter events recorded
        var diagnosticResult = collector.GetResult();
        Assert.DoesNotContain(diagnosticResult!.Events,
            e => e.Type == DiagnosticEventType.FrontMatterTokenAssigned);
        Assert.DoesNotContain(diagnosticResult.Events,
            e => e.Type == DiagnosticEventType.FrontMatterTokenFailed);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FrontMatterProcessorTests"`
Expected: FAIL — `FrontMatterProcessor` does not exist

- [ ] **Step 3: Implement FrontMatterProcessor**

```csharp
// src/Tokenizer/Tokenization/FrontMatterProcessor.cs
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Processes front matter tokens that don't require input text matching.
/// </summary>
internal static class FrontMatterProcessor
{
    /// <summary>
    /// Iterates template tokens and assigns values for any front matter tokens.
    /// </summary>
    public static void Process(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        FileLocation location)
    {
        foreach (var token in template.Tokens)
        {
            if (!token.IsFrontMatterToken) continue;

            if (token.Assign(targetObject, string.Empty, template.Options, location, out var assignedValue, collector))
            {
                collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                    tokenName: token.Name, tokenId: token.Id,
                    value: assignedValue?.ToString());
                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(token, assignedValue, token.Location);
                }
            }
            else
            {
                collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                    tokenName: token.Name, tokenId: token.Id);
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FrontMatterProcessorTests"`
Expected: PASS

- [ ] **Step 5: Run full test suite to verify no regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/FrontMatterProcessor.cs tests/Tokenizer.Tests/Tokenization/Engine/FrontMatterProcessorTests.cs
git commit -m "feat: extract FrontMatterProcessor from TokenizationEngine"
```

---

### Task 3: Extract `CandidateProcessor`

Extract token assignment, backtracking, and newline handling into a focused instance class.

**Files:**
- Create: `src/Tokenizer/Tokenization/CandidateProcessor.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Engine/CandidateProcessorTests.cs`

- [ ] **Step 1: Write failing tests for CandidateProcessor**

```csharp
// tests/Tokenizer.Tests/Tokenization/Engine/CandidateProcessorTests.cs
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Tokenization.Engine;

public class CandidateProcessorTests
{
    [Fact]
    public void GivenMatchingCandidate_WhenTryAssign_ThenReturnsTrue()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));
        context.Candidates.AddRange(template.Tokens);
        context.Replacement.Append("Alice");
        var location = new FileLocation();

        // Act
        var assigned = processor.TryAssign(context, location);

        // Assert
        Assert.True(assigned);
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenNonMatchingCandidate_WhenTryAssign_ThenReturnsFalse()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name:IsNumeric}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: NotANumber"));
        context.Candidates.AddRange(template.Tokens);
        context.Replacement.Append("NotANumber");
        var location = new FileLocation();

        // Act
        var assigned = processor.TryAssign(context, location);

        // Assert
        Assert.False(assigned);
    }

    [Fact]
    public void GivenRemainingCandidates_WhenProcessRemaining_ThenAssignsThem()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Bob"));
        context.Candidates.AddRange(template.Tokens);
        context.Replacement.Append("Bob");

        // Act
        processor.ProcessRemaining(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Bob", result.Tokens.Matches[0].Value);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CandidateProcessorTests"`
Expected: FAIL — `CandidateProcessor` does not exist

- [ ] **Step 3: Implement CandidateProcessor**

```csharp
// src/Tokenizer/Tokenization/CandidateProcessor.cs
using Microsoft.Extensions.Logging;
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Handles token candidate assignment, backtracking, and newline-terminated token processing.
/// Constructed once per tokenization session with session-scoped dependencies.
/// </summary>
internal sealed class CandidateProcessor
{
    private readonly object? targetObject;
    private readonly TokenizeResultBase result;
    private readonly Template template;
    private readonly IDiagnosticCollector collector;
    private readonly ILogger logger;

    public CandidateProcessor(
        object? targetObject,
        TokenizeResultBase result,
        Template template,
        IDiagnosticCollector collector,
        ILogger logger)
    {
        this.targetObject = targetObject;
        this.result = result;
        this.template = template;
        this.collector = collector;
        this.logger = logger;
    }

    /// <summary>
    /// Attempts to assign the accumulated replacement value to a candidate token.
    /// </summary>
    public bool TryAssign(TokenizationContext context, FileLocation location)
    {
        if (collector.IsEnabled)
        {
            collector.Record(DiagnosticEventType.TokenAssignmentAttempted,
                tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                location: location,
                value: context.Replacement.ToString());
        }

        try
        {
            if (context.Candidates.TryAssign(targetObject, context.Replacement, template.Options, location, out var assigned, out var assignedValue, collector))
            {
                if (collector.IsEnabled)
                {
                    collector.Record(DiagnosticEventType.TokenAssigned,
                        tokenName: assigned.Name, tokenId: assigned.Id,
                        location: location,
                        value: assignedValue?.ToString());
                }

                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(assigned, assignedValue, location);
                    AddMatchedTokenIds(assigned, context.MatchIds);
                }

                return true;
            }
            else
            {
                if (collector.IsEnabled)
                {
                    collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                        tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                        location: location,
                        value: context.Replacement.ToString());
                }

                return false;
            }
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(e, "Error Assigning Value: {Message}", e.Message);
            }
            result.AddException(e);
            return false;
        }
    }

    /// <summary>
    /// Handles repeated token backtracking when the accumulated value cannot be assigned.
    /// Returns true if the outer loop should continue processing, false if candidates were cleared.
    /// </summary>
    public bool HandleRepeat(TokenizationContext context)
    {
        var replacementValue = context.Replacement.ToString();

        if (context.Candidates.CanAnyAssign(replacementValue) == false)
        {
            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.BacktrackStarted,
                    tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                    location: context.Enumerator.Location,
                    value: replacementValue);
            }

            var advanceLength = context.Candidates.Preamble.Length;
            if (advanceLength == 0 && context.Candidates.Tokens.Count > 0)
            {
                var tokenNames = string.Join(", ", context.Candidates.Tokens.Select(t => t.Name));
                logger.LogError(
                    "Infinite loop detected: Cannot backtrack with empty preamble for tokens [{TokenNames}]. " +
                    "This occurs when consecutive tokens have no separator and assignment fails. " +
                    "Current position: Line {Line}, Column {Column}",
                    tokenNames, context.Enumerator.Location.Line, context.Enumerator.Location.Column);

                throw new InvalidOperationException(
                    $"Tokenization cannot proceed: tokens with empty preambles ({tokenNames}) cannot be " +
                    $"distinguished from each other. Add separators (preambles) between consecutive tokens, " +
                    $"or ensure the target object has writable properties.");
            }

            for (var i = 0; i < context.Candidates.Tokens.Count; i++)
            {
                var token = context.Candidates.Tokens[i];
                if (WasLastMatchedToken(token) && string.IsNullOrWhiteSpace(token.Preamble) && string.IsNullOrWhiteSpace(replacementValue))
                {
                    if (collector.IsEnabled)
                    {
                        collector.Record(DiagnosticEventType.RepeatingTokenDisabled,
                            tokenName: token.Name, tokenId: token.Id,
                            location: context.Enumerator.Location);
                    }
                    context.DisabledRepeatingTokens.Add(token.Id);
                    context.Candidates.Remove(token);
                    i--;
                }
                else if (token.IsSingleUse)
                {
                    if (collector.IsEnabled)
                    {
                        collector.Record(DiagnosticEventType.SingleUseTokenRemoved,
                            tokenName: token.Name, tokenId: token.Id,
                            location: context.Enumerator.Location);
                    }
                    context.Candidates.Remove(token);
                    result.Tokens.AddMiss(token);
                    context.MatchIds.Add(token.Id);
                }
            }

            context.Replacement.Clear();
            context.Enumerator.Advance(advanceLength);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles newline-terminated token processing: assigns the current value and
    /// optionally disables repeating tokens that span non-adjacent lines.
    /// </summary>
    public void HandleNewline(TokenizationContext context)
    {
        var location = context.Enumerator.Location;
        var firstToken = context.Candidates.Tokens[0];

        if (collector.IsEnabled)
        {
            collector.Record(DiagnosticEventType.NewlineTerminatedTokenProcessed,
                tokenName: firstToken.Name,
                tokenId: firstToken.Id,
                value: context.Replacement.ToString(),
                location: location);
        }

        if (firstToken.IsRepeating &&
            string.IsNullOrWhiteSpace(context.Candidates.Preamble) &&
            result.Tokens.HasMatches)
        {
            var matches = result.Tokens.Matches;
            var lastMatch = matches[matches.Count - 1];
            if (lastMatch.Token.Id == firstToken.Id)
            {
                if (context.Enumerator.Location.Line > lastMatch.Location.Line + 1)
                {
                    context.DisabledRepeatingTokens.Add(firstToken.Id);
                    context.Candidates.Remove(firstToken);
                }
            }
        }

        TryAssign(context, location);

        context.ClearCandidates();
        context.ClearReplacement();
        context.ReplacementLocation = context.Enumerator.Location;
    }

    /// <summary>
    /// Processes any remaining candidates after the main tokenization loop completes.
    /// </summary>
    public void ProcessRemaining(TokenizationContext context)
    {
        if (context.Candidates.HasCandidates && context.Replacement.Length > 0 && !context.Candidates.IsNullToken)
        {
            TryAssign(context, context.ReplacementLocation);
        }
    }

    private void AddMatchedTokenIds(Token matchedToken, HashSet<int> matchIds)
    {
        template.GetTokenIdsUpTo(matchedToken, matchIds);
    }

    private bool WasLastMatchedToken(Token token)
    {
        var matches = result.Tokens.Matches;
        if (matches.Count == 0)
        {
            return false;
        }

        return matches[matches.Count - 1].Token.Id == token.Id;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CandidateProcessorTests"`
Expected: PASS — all 3 tests green

- [ ] **Step 5: Run full test suite to verify no regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/CandidateProcessor.cs tests/Tokenizer.Tests/Tokenization/Engine/CandidateProcessorTests.cs
git commit -m "feat: extract CandidateProcessor from TokenizationEngine"
```

---

### Task 4: Extract `TokenMatchRouter`

Extract the per-character decision logic from the `ContinueTokenization` loop body.

**Files:**
- Create: `src/Tokenizer/Tokenization/TokenMatchRouter.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Engine/TokenMatchRouterTests.cs`

- [ ] **Step 1: Write failing tests for TokenMatchRouter**

```csharp
// tests/Tokenizer.Tests/Tokenization/Engine/TokenMatchRouterTests.cs
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tokens.Tokenization.Engine;

public class TokenMatchRouterTests
{
    [Fact]
    public void GivenNoMatchInInput_WhenRouteNext_ThenAccumulatesCharacter()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);
        var router = new TokenMatchRouter(template, processor,
            NullDiagnosticCollector.Instance, null);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("x"));
        context.Enumerator.FillBuffer();

        // Act
        router.RouteNext(context);

        // Assert — character accumulated in replacement buffer
        Assert.Equal("x", context.Replacement.ToString());
    }

    [Fact]
    public void GivenMatchingPreamble_WhenRouteNextWithNoCandidates_ThenSetsUpFirstMatch()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);
        var router = new TokenMatchRouter(template, processor,
            NullDiagnosticCollector.Instance, null);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));
        context.Enumerator.FillBuffer();

        // Act
        router.RouteNext(context);

        // Assert — candidates should now be set
        Assert.True(context.Candidates.HasCandidates);
    }

    [Fact]
    public void GivenSecondTokenMatch_WhenRouteNextWithExistingValue_ThenSwitchesTokens()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("A:{First}B:{Second}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);
        var router = new TokenMatchRouter(template, processor,
            NullDiagnosticCollector.Instance, null);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("A:helloB:world"));
        context.Enumerator.FillBuffer();

        // Simulate: first token already matched, value accumulated
        // Route through the input until second token is reached
        while (!context.Enumerator.IsEmpty && result.Tokens.Matches.Count == 0)
        {
            router.RouteNext(context);
            // Once we have candidates and hit B:, the switch should assign First
            if (result.Tokens.Matches.Count > 0) break;
        }

        // Assert — first token should have been assigned
        Assert.True(result.Tokens.Matches.Count >= 1);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenMatchRouterTests"`
Expected: FAIL — `TokenMatchRouter` does not exist

- [ ] **Step 3: Implement TokenMatchRouter**

```csharp
// src/Tokenizer/Tokenization/TokenMatchRouter.cs
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Routes each character to the appropriate processing path during tokenization.
/// Handles the per-character decision: repeated token, newline-terminated, new match, or accumulate.
/// </summary>
internal sealed class TokenMatchRouter
{
    private readonly Template template;
    private readonly CandidateProcessor candidateProcessor;
    private readonly IDiagnosticCollector collector;
    private readonly IHintStrategy? hintStrategy;

    public TokenMatchRouter(
        Template template,
        CandidateProcessor candidateProcessor,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy)
    {
        this.template = template;
        this.candidateProcessor = candidateProcessor;
        this.collector = collector;
        this.hintStrategy = hintStrategy;
    }

    /// <summary>
    /// Examines the next character in the input and routes to the appropriate handler.
    /// Returns false if the repeated-token path cleared candidates (caller should continue the loop).
    /// Returns true for all other paths.
    /// </summary>
    public bool RouteNext(TokenizationContext context)
    {
        var next = context.Enumerator.Peek();

        // Check for repeated current token
        if (context.Candidates.HasCandidates &&
            context.Enumerator.TryMatch(context.Candidates.Preamble) &&
            context.Candidates.Preamble.Length > 0)
        {
            if (!candidateProcessor.HandleRepeat(context))
            {
                return false;
            }
        }

        // Assign newline terminated token
        if (context.Candidates.HasCandidates && context.Candidates.TerminateOnNewLine && next == '\n')
        {
            candidateProcessor.HandleNewline(context);
            return true;
        }

        // Check for next token
        if (context.Enumerator.TryMatch(
            template.TokensExcluding(context.MatchIds, context.Candidates, context.DisabledRepeatingTokens, context.ExclusionBuffer, context.TokenFilterBuffer, context.TokenFilterIds),
            template.Options.OutOfOrderTokens,
            context.MatchBuffer))
        {
            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.PreambleMatched,
                    tokenName: string.Join(", ", context.MatchBuffer.Select(m => m.Name)),
                    location: context.Enumerator.Location);
            }

            // Notify hint strategy of matched tokens
            if (hintStrategy != null)
            {
                foreach (var match in context.MatchBuffer)
                {
                    hintStrategy.OnTokenMatched(match);
                }
            }

            // First token found — prepare to read token value
            if (context.Candidates.HasCandidates == false)
            {
                context.Candidates.AddRange(context.MatchBuffer);
                context.ClearReplacement();
                context.Enumerator.Advance(context.Candidates.Preamble.Length);
                return true;
            }

            // Switch if we've accumulated a value — otherwise consume a character first
            if (context.Replacement.Length > 0)
            {
                candidateProcessor.TryAssign(context, context.ReplacementLocation);

                context.ClearCandidates();
                context.Candidates.AddRange(context.MatchBuffer);
                context.ClearReplacement();
                context.Enumerator.Advance(context.Candidates.Preamble.Length);
                context.ReplacementLocation = context.Enumerator.Location;
            }
            else
            {
                context.Replacement.Append(next);
                context.Enumerator.Next();
            }
        }
        else
        {
            context.Replacement.Append(next);
            context.Enumerator.Next();
        }

        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenMatchRouterTests"`
Expected: PASS

- [ ] **Step 5: Run full test suite to verify no regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenMatchRouter.cs tests/Tokenizer.Tests/Tokenization/Engine/TokenMatchRouterTests.cs
git commit -m "feat: extract TokenMatchRouter from TokenizationEngine"
```

---

### Task 5: Create `TokenizationSession`

Create the session class that replaces `TokenizationContinuation` and the Begin/Continue/End protocol with `Run`/`RunAsync` entry points.

**Files:**
- Create: `src/Tokenizer/Tokenization/TokenizationSession.cs`
- Test: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationSessionTests.cs`

- [ ] **Step 1: Write failing tests for TokenizationSession**

```csharp
// tests/Tokenizer.Tests/Tokenization/Engine/TokenizationSessionTests.cs
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Tokenization.Engine;

public class TokenizationSessionTests
{
    [Fact]
    public void GivenValidInput_WhenRun_ThenTokenizesSuccessfully()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var session = CreateSession(template, null, result);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));

        // Act
        session.Run(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Alice", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public async Task GivenValidInput_WhenRunAsync_ThenTokenizesSuccessfully()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var session = CreateSession(template, null, result);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));

        // Act
        await session.RunAsync(context, CancellationToken.None);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Alice", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenRunAndRunAsync_WhenSameInput_ThenProduceIdenticalResults()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("A:{First}B:{Second}").Template;

        var syncResult = new TokenizeResultBuilder().WithTemplate(template).Build();
        var asyncResult = new TokenizeResultBuilder().WithTemplate(template).Build();

        var input = "A:helloB:world";

        // Act — sync
        var syncSession = CreateSession(template, null, syncResult);
        var syncContext = new TokenizationContext();
        syncContext.Initialize(new System.IO.StringReader(input));
        syncSession.Run(syncContext);

        // Act — async
        var asyncSession = CreateSession(template, null, asyncResult);
        var asyncContext = new TokenizationContext();
        asyncContext.Initialize(new System.IO.StringReader(input));
        asyncSession.RunAsync(asyncContext, CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        Assert.Equal(syncResult.Tokens.Matches.Count, asyncResult.Tokens.Matches.Count);
        for (var i = 0; i < syncResult.Tokens.Matches.Count; i++)
        {
            Assert.Equal(syncResult.Tokens.Matches[i].Token.Name, asyncResult.Tokens.Matches[i].Token.Name);
            Assert.Equal(syncResult.Tokens.Matches[i].Value, asyncResult.Tokens.Matches[i].Value);
        }
    }

    [Fact]
    public void GivenExplicitIterationLimit_WhenExceeded_ThenThrowsTokenizerException()
    {
        // Arrange — use a very low limit to trigger the guard
        var options = new TokenizerOptions { MaxIterations = 1 };
        var parser = new TemplateCompiler(options);
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var session = CreateSession(template, null, result);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: A long value that exceeds one iteration"));

        // Act & Assert
        Assert.Throws<TokenizerException>(() => session.Run(context));
    }

    [Fact]
    public async Task GivenCancelledToken_WhenRunAsync_ThenThrowsOperationCancelledException()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var session = CreateSession(template, null, result);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            session.RunAsync(context, cts.Token));
    }

    private static TokenizationSession CreateSession(
        Template template, object? target, TokenizeResultBase result,
        IDiagnosticCollector? collector = null)
    {
        return new TokenizationSession(
            template, target, result,
            collector ?? NullDiagnosticCollector.Instance,
            null,
            NullLogger<TokenizationEngine>.Instance);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizationSessionTests"`
Expected: FAIL — `TokenizationSession` does not exist

- [ ] **Step 3: Implement TokenizationSession**

```csharp
// src/Tokenizer/Tokenization/TokenizationSession.cs
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tokens.Diagnostics;
using Tokens.Exceptions;

namespace Tokens.Tokenization;

/// <summary>
/// Coordinates a single tokenization run. Created by <see cref="TokenizationEngine.CreateSession"/>
/// and holds all session-scoped state and sub-components. Provides <see cref="Run"/> and
/// <see cref="RunAsync"/> entry points that share a single <see cref="ProcessChunk"/> algorithm.
/// </summary>
internal sealed class TokenizationSession
{
    private readonly Template template;
    private readonly TokenizeResultBase result;
    private readonly IDiagnosticCollector collector;
    private readonly TokenMatchRouter router;
    private readonly CandidateProcessor candidateProcessor;
    private readonly bool hasExplicitLimit;
    private int iterationCount;

    public TokenizationSession(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy,
        ILogger logger)
    {
        this.template = template;
        this.result = result;
        this.collector = collector;
        this.hasExplicitLimit = template.Options.MaxIterations > 0;

        candidateProcessor = new CandidateProcessor(
            targetObject, result, template, collector, logger);
        router = new TokenMatchRouter(
            template, candidateProcessor, collector, hintStrategy);
    }

    /// <summary>
    /// Runs tokenization synchronously. The enumerator is pre-filled, so FillBuffer is a no-op.
    /// </summary>
    public void Run(TokenizationContext context)
    {
        Initialize(context);

        do
        {
            context.Enumerator.FillBuffer();
        }
        while (!ProcessChunk(context, CancellationToken.None));

        Finalize(context);
    }

    /// <summary>
    /// Runs tokenization asynchronously with cooperative buffer refills.
    /// </summary>
    public async Task RunAsync(TokenizationContext context, CancellationToken ct)
    {
        Initialize(context);

        do
        {
            await context.Enumerator.FillBufferAsync(ct).ConfigureAwait(false);

            if (template.Options.MaxInputLength > 0 &&
                context.Enumerator.TotalCharactersSeen > template.Options.MaxInputLength)
            {
                throw new TokenizerException(
                    $"Input length exceeds maximum allowed length of {template.Options.MaxInputLength:N0}. " +
                    "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
            }
        }
        while (!ProcessChunk(context, ct));

        Finalize(context);
    }

    private void Initialize(TokenizationContext context)
    {
        collector.Record(DiagnosticEventType.TokenizationStarted,
            detail: $"Template: {template.Name}, Tokens: {template.Tokens.Count}");
        context.MatchBuffer.Clear();
        iterationCount = 0;
    }

    /// <summary>
    /// Processes the current buffer contents. Returns true when input is fully consumed,
    /// false when the enumerator needs a buffer refill.
    /// </summary>
    private bool ProcessChunk(TokenizationContext context, CancellationToken ct)
    {
        while (context.Enumerator.IsEmpty == false)
        {
            if (context.Enumerator.NeedsRefill)
                return false;

            ct.ThrowIfCancellationRequested();

            iterationCount++;
            if (hasExplicitLimit && iterationCount > template.Options.MaxIterations)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded maximum iteration count of {template.Options.MaxIterations:N0}. " +
                    "This may indicate a problematic template pattern. " +
                    "Increase TokenizerOptions.MaxIterations to allow more iterations.");
            }

            if (!hasExplicitLimit && iterationCount > context.Enumerator.CharactersConsumed * 2 + 100)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded derived iteration limit (iterations: {iterationCount:N0}, " +
                    $"characters consumed: {context.Enumerator.CharactersConsumed:N0}). " +
                    "This may indicate a problematic template pattern. " +
                    "Set TokenizerOptions.MaxIterations to override the automatic limit.");
            }

            router.RouteNext(context);
        }

        return true;
    }

    private void Finalize(TokenizationContext context)
    {
        candidateProcessor.ProcessRemaining(context);
        FrontMatterProcessor.Process(template, result, collector, context.Enumerator.Location);
        collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: $"Matches: {result.Tokens.Matches.Count}, Misses: {result.Tokens.Misses.Count}");
    }
}
```

**Note:** The `FrontMatterProcessor.Process` call in `Finalize` passes `result` which already holds the `targetObject` reference via `CandidateProcessor`. However, `FrontMatterProcessor` is a static class that needs `targetObject` explicitly — check the actual signature from Task 2 and adjust. The correct call is:

```csharp
FrontMatterProcessor.Process(template, targetObject, result, collector, context.Enumerator.Location);
```

This means the session needs to hold `targetObject` as a field. Add it:

```csharp
private readonly object? targetObject;
```

And assign in the constructor. Then `Finalize` becomes:

```csharp
private void Finalize(TokenizationContext context)
{
    candidateProcessor.ProcessRemaining(context);
    FrontMatterProcessor.Process(template, targetObject, result, collector, context.Enumerator.Location);
    collector.Record(DiagnosticEventType.TokenizationCompleted,
        detail: $"Matches: {result.Tokens.Matches.Count}, Misses: {result.Tokens.Misses.Count}");
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizationSessionTests"`
Expected: PASS — all 5 tests green

- [ ] **Step 5: Run full test suite to verify no regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationSession.cs tests/Tokenizer.Tests/Tokenization/Engine/TokenizationSessionTests.cs
git commit -m "feat: create TokenizationSession with Run/RunAsync entry points"
```

---

### Task 6: Rewire `TokenizationEngine` as thin orchestrator

Replace the monolithic engine with a thin factory that delegates to `TokenizationSession`.

**Files:**
- Modify: `src/Tokenizer/Tokenization/ITokenizationEngine.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- Delete: `src/Tokenizer/Tokenization/TokenizationContinuation.cs`

- [ ] **Step 1: Update ITokenizationEngine to single-method interface**

Replace the contents of `ITokenizationEngine.cs`:

```csharp
// src/Tokenizer/Tokenization/ITokenizationEngine.cs
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Factory for creating tokenization sessions. Internal interface with a single
/// implementor, exposed for test substitution.
/// </summary>
internal interface ITokenizationEngine
{
    /// <summary>
    /// Creates a tokenization session that can be run synchronously or asynchronously.
    /// Validates the target object before returning.
    /// </summary>
    TokenizationSession CreateSession(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);
}
```

- [ ] **Step 2: Replace TokenizationEngine with thin orchestrator**

Replace the contents of `TokenizationEngine.cs`:

```csharp
// src/Tokenizer/Tokenization/TokenizationEngine.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

internal static class ArgumentValidation
{
#if NETSTANDARD2_0
    public static void ThrowIfNull(object argument, string paramName)
    {
        if (argument == null) throw new ArgumentNullException(paramName);
    }
#else
    public static void ThrowIfNull(object argument, string paramName)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }
#endif
}

/// <summary>
/// Thin orchestrator that validates inputs and creates tokenization sessions.
/// All tokenization logic lives in <see cref="TokenizationSession"/> and its sub-components.
/// </summary>
internal class TokenizationEngine : ITokenizationEngine
{
    private readonly ILogger<TokenizationEngine> log;

    public TokenizationEngine() : this(null)
    {
    }

    public TokenizationEngine(ILogger<TokenizationEngine>? logger)
    {
        log = logger ?? NullLogger<TokenizationEngine>.Instance;
    }

    public TokenizationSession CreateSession(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null)
    {
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        InputValidator.ValidateTargetObject(targetObject, log);

        return new TokenizationSession(
            template, targetObject, result, collector, hintStrategy, log);
    }
}
```

- [ ] **Step 3: Delete TokenizationContinuation.cs**

```bash
git rm src/Tokenizer/Tokenization/TokenizationContinuation.cs
```

- [ ] **Step 4: Build to check for compilation errors**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: May have errors in `Tokenizer.cs` (callers not yet updated) and test files. That's expected — we fix those in the next tasks.

- [ ] **Step 5: Commit the engine changes (even if callers aren't updated yet)**

```bash
git add src/Tokenizer/Tokenization/ITokenizationEngine.cs src/Tokenizer/Tokenization/TokenizationEngine.cs
git commit -m "refactor: simplify TokenizationEngine to thin session factory"
```

---

### Task 7: Update callers in `Tokenizer.cs`

Update the sync and async call sites to use the new session API.

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`

- [ ] **Step 1: Update sync call site in `TokenizeCore`**

In `Tokenizer.cs`, find the line:
```csharp
tokenizationEngine.ProcessTokenization(template, value, context, result, collector, hintStrategy);
```

Replace with:
```csharp
var session = tokenizationEngine.CreateSession(template, value, result, collector, hintStrategy);
session.Run(context);
```

- [ ] **Step 2: Update async call site in `TokenizeAsyncCore`**

In `Tokenizer.cs`, find the block:
```csharp
var continuation = tokenizationEngine.BeginTokenization(template, value, context, result, collector, hintStrategy);
do
{
    await context.Enumerator.FillBufferAsync(ct).ConfigureAwait(false);

    if (template.Options.MaxInputLength > 0 &&
        context.Enumerator.TotalCharactersSeen > template.Options.MaxInputLength)
    {
        throw new TokenizerException(
            $"Input length exceeds maximum allowed length of {template.Options.MaxInputLength:N0}. " +
            "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
    }
}
while (!tokenizationEngine.ContinueTokenization(continuation, context, ct));
tokenizationEngine.EndTokenization(continuation, context);
```

Replace with:
```csharp
var session = tokenizationEngine.CreateSession(template, value, result, collector, hintStrategy);
await session.RunAsync(context, ct).ConfigureAwait(false);
```

- [ ] **Step 3: Build to verify compilation succeeds**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: PASS — no compilation errors

- [ ] **Step 4: Run the full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All existing tests pass (some engine tests may fail — those are updated in Task 8)

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs
git commit -m "refactor: update Tokenizer to use session-based tokenization API"
```

---

### Task 8: Update existing engine tests

Update the 8 existing engine test files to use `CreateSession`/`Run` instead of `ProcessTokenization`.

**Files:**
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineBasicTests.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineStateTests.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEdgeCaseTests.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineTokenMatchingTests.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEnginePerformanceTests.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEmptyPreambleTests.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs`

- [ ] **Step 1: Update BasicTests**

In `TokenizationEngineBasicTests.cs`, replace all calls from:
```csharp
_engine.ProcessTokenization(template, value, context, result, NullDiagnosticCollector.Instance);
```
to:
```csharp
var session = _engine.CreateSession(template, value, result, NullDiagnosticCollector.Instance);
session.Run(context);
```

Apply the same pattern for calls that pass `null` as targetObject:
```csharp
_engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);
```
becomes:
```csharp
var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
session.Run(context);
```

- [ ] **Step 2: Update ErrorTests**

In `TokenizationEngineErrorTests.cs`, update all `ProcessTokenization` calls to `CreateSession`/`Run`.

For the null-argument tests, the exception should now throw from `CreateSession`:
```csharp
// GivenNullTemplate — throws from CreateSession
Assert.Throws<ArgumentNullException>(() =>
    _engine.CreateSession(null!, value, result, NullDiagnosticCollector.Instance));

// GivenNullResult — throws from CreateSession
Assert.Throws<ArgumentNullException>(() =>
    _engine.CreateSession(template, value, null!, NullDiagnosticCollector.Instance));

// GivenReadOnlyTargetObject — throws from CreateSession
Assert.Throws<ArgumentException>(() =>
    _engine.CreateSession(template, readOnlyTarget, result, NullDiagnosticCollector.Instance));
```

The `GivenNullContext` test currently tests passing `null` context to `ProcessTokenization`, which no longer exists. `CreateSession` doesn't take a context. Delete this test — the null context scenario is now caught by `NullReferenceException` if someone passes null to `session.Run(null!)`, which is standard .NET behavior and doesn't need a dedicated test.

- [ ] **Step 3: Update StateTests**

In `TokenizationEngineStateTests.cs`, replace `ProcessTokenization` calls:
```csharp
_engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);
```
becomes:
```csharp
var session = _engine.CreateSession(template, null, result, NullDiagnosticCollector.Instance);
session.Run(context);
```

- [ ] **Step 4: Update EmptyPreambleTests**

In `TokenizationEngineEmptyPreambleTests.cs`, replace all `ProcessTokenization` calls with `CreateSession`/`Run`. The `ReadOnlyTarget` test should now use `CreateSession`:
```csharp
Assert.Throws<ArgumentException>(() =>
    _engine.CreateSession(template, target, result, NullDiagnosticCollector.Instance));
```

- [ ] **Step 5: Update remaining test files**

Apply the same `ProcessTokenization` → `CreateSession`/`Run` replacement in:
- `TokenizationEngineEdgeCaseTests.cs`
- `TokenizationEngineTokenMatchingTests.cs`
- `TokenizationEnginePerformanceTests.cs`

For each file, search for `ProcessTokenization` and replace with the two-line pattern.

- [ ] **Step 6: Build and run all tests**

Run: `dotnet build ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add tests/Tokenizer.Tests/Tokenization/Engine/
git commit -m "refactor: update engine tests for CreateSession/Run API"
```

---

### Task 9: Remove `ITokenizationContext` interface

Remove the now-unused interface. All consumers use the concrete `TokenizationContext` directly.

**Files:**
- Delete: `src/Tokenizer/Tokenization/ITokenizationContext.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationContext.cs`

- [ ] **Step 1: Verify no remaining references to ITokenizationContext**

Run: `grep -r "ITokenizationContext" src/ tests/ --include="*.cs" -l`

Expected: Only `ITokenizationContext.cs` and `TokenizationContext.cs` (the `: ITokenizationContext` declaration).

If other files reference it, update them to use `TokenizationContext` first.

- [ ] **Step 2: Remove the interface from TokenizationContext**

In `TokenizationContext.cs`, change:
```csharp
internal sealed class TokenizationContext : ITokenizationContext, IDisposable
```
to:
```csharp
internal sealed class TokenizationContext : IDisposable
```

- [ ] **Step 3: Delete ITokenizationContext.cs**

```bash
git rm src/Tokenizer/Tokenization/ITokenizationContext.cs
```

- [ ] **Step 4: Build and run all tests**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release && dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: Build succeeds, all tests pass

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationContext.cs
git commit -m "refactor: remove unused ITokenizationContext interface"
```

---

### Task 10: Clean up and verify

Remove the `TokenizationContinuation` deletion commit (if not already done in Task 6), remove any dead `using` statements, and run the full suite.

**Files:**
- Modify: Various files — clean up unused `using` directives

- [ ] **Step 1: Check for any remaining references to deleted types**

Run: `grep -r "TokenizationContinuation\|ProcessTokenization\|BeginTokenization\|ContinueTokenization\|EndTokenization" src/ tests/ --include="*.cs" -l`

Expected: No results (or only comments/docs). If any remain, fix them.

- [ ] **Step 2: Check for unused using directives in modified files**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release 2>&1 | grep -i "warning.*using"`

Fix any unused `using` warnings in the files we modified.

- [ ] **Step 3: Run the full test suite one final time**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass — zero failures, zero skipped

- [ ] **Step 4: Verify the build produces no warnings**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release -warnaserror`
Expected: Build succeeds with no warnings

- [ ] **Step 5: Commit any cleanup changes**

```bash
git add -A
git status
git commit -m "chore: clean up unused references after engine refactoring"
```
