# Two-Stage Tokenization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Separate the tokenization pipeline into Stage 1 (matching + decorator evaluation) and Stage 2 (object reflection via `Assign<T>()`), removing the target object from the matching pipeline entirely.

**Architecture:** `TokenAssigner` is renamed to `DecoratorPipeline` and stripped of all reflection logic. A new `Assign<T>()` method on `TokenizeResult` handles object construction and property assignment from matches. `Tokenize<T>()` becomes `Tokenize().Assign<T>()`.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 8.0 + .NET 10.0, xUnit, NSubstitute

## Global Constraints

- Target frameworks: .NET Standard 2.0, .NET 8.0, .NET 10.0 — conditional compilation where needed
- Root namespace: `Tokens`
- Braces: Allman style
- Private fields: `_camelCase`
- No `#region`
- Naming: `GivenScenario_WhenAction_ThenResult()` for tests
- Build must pass: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
- Tests must pass: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
- TDD: write failing test first, then implement
- Commit after each task

---

### Task 1: Rename TokenAssigner to DecoratorPipeline and strip reflection

Rename the class file, update method names (`Assign` → `Evaluate`, `CanAssign` → `CanEvaluate`), remove all reflection/target-object logic from `Evaluate()`. Update all call sites and tests.

**Files:**
- Rename: `src/Tokenizer/Tokenization/TokenAssigner.cs` → `src/Tokenizer/Tokenization/DecoratorPipeline.cs`
- Modify: `src/Tokenizer/CandidateTokenList.cs`
- Modify: `src/Tokenizer/Tokenization/CandidateProcessor.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationSession.cs`
- Modify: `src/Tokenizer/Tokenization/FrontMatterProcessor.cs`
- Rename: `tests/Tokenizer.Tests/Tokenization/TokenAssignerTests.cs` → `tests/Tokenizer.Tests/Tokenization/DecoratorPipelineTests.cs`
- Modify: `tests/Tokenizer.Tests/CandidateTokenListTests.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/CandidateProcessorTests.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/FrontMatterProcessorTests.cs`

**Interfaces:**
- Produces: `DecoratorPipeline.Evaluate(Token token, string value, FileLocation location, out object? evaluatedValue) → bool`
- Produces: `DecoratorPipeline.CanEvaluate(Token token, string value) → bool`
- Produces: `CandidateTokenList.TryEvaluate(StringBuilder value, DecoratorPipeline pipeline, FileLocation location, out Token? evaluated, out object? evaluatedValue) → bool`
- Produces: `CandidateTokenList.CanAnyEvaluate(string value, DecoratorPipeline pipeline) → bool`

- [ ] **Step 1: Update DecoratorPipeline tests**

Rename `tests/Tokenizer.Tests/Tokenization/TokenAssignerTests.cs` to `DecoratorPipelineTests.cs`. Update the class to test the new `DecoratorPipeline` with `Evaluate()` instead of `Assign()`. Remove tests that test reflection/target-object behavior (those move to Task 3). Keep tests for: decorator pipeline evaluation, value preparation, trimming, CanEvaluate.

```csharp
// tests/Tokenizer.Tests/Tokenization/DecoratorPipelineTests.cs
using System.Collections.Concurrent;
using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Validators;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tokenization;

public class DecoratorPipelineTests : TokenizerTestBase
{
    private readonly DecoratorPipeline _pipeline;

    public DecoratorPipelineTests(ITestOutputHelper output) : base(output)
    {
        _pipeline = new DecoratorPipeline(new TokenizerOptions(), NullDiagnosticCollector.Instance);
    }

    [Fact]
    public void GivenTokenWithValidValue_WhenEvaluating_ThenReturnsTrueWithValue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _pipeline.Evaluate(token, "Sue", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("Sue", value);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenEvaluatingValidNumber_ThenReturnsTrueWithValue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Age").Build();
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var result = _pipeline.Evaluate(token, "20", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("20", value);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenEvaluatingInvalidNumber_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Age").Build();
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var result = _pipeline.Evaluate(token, "Twenty", new FileLocation(), out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTokenWithTerminateOnNewLine_WhenValueContainsNewLine_ThenTruncatesAtNewLine()
    {
        // Arrange
        var token = new TokenBuilder()
            .WithName("Name")
            .WithTerminateOnNewLine(true)
            .Build();

        // Act
        _pipeline.Evaluate(token, "Alice\nBob", new FileLocation(), out var value);

        // Assert
        Assert.Equal("Alice", value);
    }

    [Fact]
    public void GivenEmptyValue_WhenEvaluating_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _pipeline.Evaluate(token, string.Empty, new FileLocation(), out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTokenWithTrimTrailingWhitespace_WhenEvaluating_ThenTrimsValue()
    {
        // Arrange
        var options = new TokenizerOptions { TrimTrailingWhiteSpace = true };
        var pipeline = new DecoratorPipeline(options, NullDiagnosticCollector.Instance);
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        pipeline.Evaluate(token, "Sue   ", new FileLocation(), out var value);

        // Assert
        Assert.Equal("Sue", value);
    }

    [Fact]
    public void GivenTokenWithValidValue_WhenCanEvaluate_ThenReturnsTrue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _pipeline.CanEvaluate(token, "Sue");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenEmptyValue_WhenCanEvaluate_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _pipeline.CanEvaluate(token, string.Empty);

        // Assert
        Assert.False(result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DecoratorPipelineTests"`
Expected: FAIL — `DecoratorPipeline` class does not exist yet.

- [ ] **Step 3: Implement DecoratorPipeline**

Rename `src/Tokenizer/Tokenization/TokenAssigner.cs` to `src/Tokenizer/Tokenization/DecoratorPipeline.cs`. Replace the class:

```csharp
// src/Tokenizer/Tokenization/DecoratorPipeline.cs
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Runs the decorator pipeline (transformers + validators) on matched token values.
/// Session-scoped: constructed once per tokenization session with shared options and diagnostics.
/// </summary>
internal sealed class DecoratorPipeline
{
    private readonly TokenizerOptions _options;
    private readonly IDiagnosticCollector _collector;

    internal DecoratorPipeline(TokenizerOptions options, IDiagnosticCollector collector)
    {
        _options = options;
        _collector = collector;
    }

    internal IDiagnosticCollector Collector => _collector;

    /// <summary>
    /// Prepares the value and runs the decorator pipeline (transformers then validators).
    /// Returns true if the value passes all decorators; the evaluated (potentially transformed)
    /// value is returned via <paramref name="evaluatedValue"/>.
    /// </summary>
    internal bool Evaluate(Token token, string value, FileLocation location, out object? evaluatedValue)
    {
        evaluatedValue = null;

        var prepared = PrepareValue(token, value);
        if (prepared == null) return false;

        if (_options.TrimTrailingWhiteSpace)
        {
            prepared = prepared.TrimEnd();
        }

        if (!RunDecoratorPipeline(token, prepared, location, out evaluatedValue)) return false;

        return true;
    }

    /// <summary>
    /// Dry-run: checks whether the value can pass through preparation and the decorator pipeline.
    /// </summary>
    internal bool CanEvaluate(Token token, string value)
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

    private bool RunDecoratorPipeline(Token token, object input, FileLocation? location, out object? evaluatedValue)
    {
        evaluatedValue = input;

        foreach (var decorator in token.Decorators)
        {
            if (decorator.IsTransformer)
            {
                if (!decorator.TryTransform(evaluatedValue!, out var output))
                {
                    _collector.Record(DiagnosticEventType.TransformerFailed,
                        tokenName: token.Name, tokenId: token.Id,
                        location: location,
                        value: evaluatedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name,
                        decoratorArgs: decorator.Parameters.ToArray());

                    return false;
                }

                _collector.Record(DiagnosticEventType.TransformerSucceeded,
                    tokenName: token.Name, tokenId: token.Id,
                    location: location,
                    value: evaluatedValue?.ToString(),
                    detail: output?.ToString(),
                    decoratorName: decorator.DecoratorType.Name,
                    decoratorArgs: decorator.Parameters.ToArray());

                evaluatedValue = output;
            }

            if (decorator.IsValidator)
            {
                if (decorator.Validate(evaluatedValue!))
                {
                    _collector.Record(DiagnosticEventType.ValidatorPassed,
                        tokenName: token.Name, tokenId: token.Id,
                        value: evaluatedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);
                }
                else
                {
                    _collector.Record(DiagnosticEventType.ValidatorFailed,
                        tokenName: token.Name, tokenId: token.Id,
                        value: input?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);

                    return false;
                }
            }
        }

        return true;
    }
}
```

- [ ] **Step 4: Update CandidateTokenList**

In `src/Tokenizer/CandidateTokenList.cs`, rename `TryAssign` → `TryEvaluate`, `CanAnyAssign` → `CanAnyEvaluate`, replace `TokenAssigner` parameter type with `DecoratorPipeline`, remove `target` parameter from `TryEvaluate`:

```csharp
// In CandidateTokenList.cs — replace TryAssign method (lines 57-85)
/// <summary>
/// Evaluates the given string value against each candidate token using the decorator pipeline.
/// Returns true if a candidate's decorators accept the value.
/// </summary>
public bool TryEvaluate(StringBuilder value, DecoratorPipeline pipeline, FileLocation location, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Token? evaluated, out object? evaluatedValue)
{
    evaluated = null;
    evaluatedValue = null;

    var valueString = value.ToString();

    foreach (var token in _tokens)
    {
        if (pipeline.Evaluate(token, valueString, location, out evaluatedValue))
        {
            evaluated = token;
            return true;
        }
    }

    return false;
}

// Replace CanAnyAssign method (lines 87-105)
/// <summary>
/// Returns true if at least one candidate token's decorators would accept the given value.
/// </summary>
public bool CanAnyEvaluate(string value, DecoratorPipeline pipeline)
{
    foreach (var token in _tokens)
    {
        if (pipeline.CanEvaluate(token, value))
        {
            return true;
        }
    }

    return false;
}
```

- [ ] **Step 5: Update CandidateProcessor — remove target object**

In `src/Tokenizer/Tokenization/CandidateProcessor.cs`:
- Remove `_targetObject` field and constructor parameter
- Update `TryAssign` to call `context.Candidates.TryEvaluate(...)` without target
- Update `HandleRepeat` to call `context.Candidates.CanAnyEvaluate(...)`

```csharp
// src/Tokenizer/Tokenization/CandidateProcessor.cs — full replacement
using Microsoft.Extensions.Logging;
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Handles token candidate assignment, backtracking, and newline-terminated token processing.
/// Constructed once per tokenization session with session-scoped dependencies.
/// </summary>
internal sealed class CandidateProcessor
{
    private readonly TokenizeResultBase _result;
    private readonly Template _template;
    private readonly DecoratorPipeline _pipeline;
    private readonly IDiagnosticCollector _collector;
    private readonly ILogger _logger;

    public CandidateProcessor(
        TokenizeResultBase result,
        Template template,
        DecoratorPipeline pipeline,
        IDiagnosticCollector collector,
        ILogger logger)
    {
        _result = result;
        _template = template;
        _pipeline = pipeline;
        _collector = collector;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to evaluate the accumulated replacement value against candidate tokens.
    /// Returns true if evaluation succeeded and a match was recorded.
    /// </summary>
    public bool TryAssign(TokenizationContext context, FileLocation location)
    {
        if (_collector.IsEnabled)
        {
            _collector.Record(DiagnosticEventType.TokenAssignmentAttempted,
                tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                location: location,
                value: context.Replacement.ToString());
        }

        try
        {
            if (context.Candidates.TryEvaluate(context.Replacement, _pipeline, location, out var evaluated, out var evaluatedValue))
            {
                if (_collector.IsEnabled)
                {
                    _collector.Record(DiagnosticEventType.TokenAssigned,
                        tokenName: evaluated.Name, tokenId: evaluated.Id,
                        location: location,
                        value: evaluatedValue?.ToString());
                }

                if (evaluatedValue != null)
                {
                    _result.Tokens.AddMatch(evaluated, evaluatedValue, location);
                    AddMatchedTokenIds(evaluated, context.MatchIds);
                }

                return true;
            }

            if (_collector.IsEnabled)
            {
                _collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                    tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                    location: location,
                    value: context.Replacement.ToString());
            }

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
    }

    /// <summary>
    /// Handles repeated token backtracking when the accumulated value cannot be assigned.
    /// Returns true if the outer loop should continue processing, false if candidates were cleared.
    /// </summary>
    public bool HandleRepeat(TokenizationContext context)
    {
        var replacementValue = context.Replacement.ToString();

        if (!context.Candidates.CanAnyEvaluate(replacementValue, _pipeline))
        {
            if (_collector.IsEnabled)
            {
                _collector.Record(DiagnosticEventType.BacktrackStarted,
                    tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                    location: context.Enumerator.Location,
                    value: replacementValue);
            }

            var advanceLength = context.Candidates.Preamble.Length;
            if (advanceLength == 0 && context.Candidates.Tokens.Count > 0)
            {
                var tokenNames = string.Join(", ", context.Candidates.Tokens.Select(t => t.Name));
                _logger.LogError(
                    "Infinite loop detected: Cannot backtrack with empty preamble for tokens [{TokenNames}]. " +
                    "This occurs when consecutive tokens have no separator and assignment fails. " +
                    "Current position: Line {Line}, Column {Column}",
                    tokenNames, context.Enumerator.Location.Line, context.Enumerator.Location.Column);

                throw new InvalidOperationException(
                    "Tokenization cannot proceed: tokens with empty preambles (" + tokenNames + ") cannot be " +
                    "distinguished from each other. Add separators (preambles) between consecutive tokens, " +
                    "or ensure the target object has writable properties.");
            }

            for (var i = 0; i < context.Candidates.Tokens.Count; i++)
            {
                var token = context.Candidates.Tokens[i];
                if (WasLastMatchedToken(token) && string.IsNullOrWhiteSpace(token.Preamble) && string.IsNullOrWhiteSpace(replacementValue))
                {
                    if (_collector.IsEnabled)
                    {
                        _collector.Record(DiagnosticEventType.RepeatingTokenDisabled,
                            tokenName: token.Name, tokenId: token.Id,
                            location: context.Enumerator.Location);
                    }
                    context.DisabledRepeatingTokens.Add(token.Id);
                    context.Candidates.Remove(token);
                    i--;
                }
                else if (token.IsSingleUse)
                {
                    if (_collector.IsEnabled)
                    {
                        _collector.Record(DiagnosticEventType.SingleUseTokenRemoved,
                            tokenName: token.Name, tokenId: token.Id,
                            location: context.Enumerator.Location);
                    }
                    context.Candidates.Remove(token);
                    _result.Tokens.AddMiss(token);
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
    /// Handles newline-terminated token processing: optionally disables repeating tokens
    /// that span non-adjacent lines, attempts assignment, then clears candidates,
    /// replacement, and updates the replacement location.
    /// </summary>
    public void HandleNewline(TokenizationContext context)
    {
        var location = context.Enumerator.Location;
        var firstToken = context.Candidates.Tokens[0];

        if (_collector.IsEnabled)
        {
            _collector.Record(DiagnosticEventType.NewlineTerminatedTokenProcessed,
                tokenName: firstToken.Name,
                tokenId: firstToken.Id,
                value: context.Replacement.ToString(),
                location: location);
        }

        if (firstToken.IsRepeating &&
            string.IsNullOrWhiteSpace(context.Candidates.Preamble) &&
            _result.Tokens.HasMatches)
        {
            var matches = _result.Tokens.Matches;
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
        _template.GetTokenIdsUpTo(matchedToken, matchIds);
    }

    private bool WasLastMatchedToken(Token token)
    {
        var matches = _result.Tokens.Matches;
        if (matches.Count == 0)
        {
            return false;
        }

        return matches[matches.Count - 1].Token.Id == token.Id;
    }
}
```

- [ ] **Step 6: Update TokenizationSession — remove target object**

In `src/Tokenizer/Tokenization/TokenizationSession.cs`:
- Remove `_targetObject` field and constructor parameter
- Update `CandidateProcessor` construction (no target)
- Update `FrontMatterProcessor.Process()` call (no target)
- Rename `_assigner` to `_pipeline`, type to `DecoratorPipeline`

```csharp
// In TokenizationSession constructor — replace lines 25-43:
public TokenizationSession(
    Template template,
    TokenizeResultBase result,
    IDiagnosticCollector collector,
    IHintStrategy? hintStrategy,
    ILogger logger)
{
    _template = template;
    _result = result;
    _collector = collector;
    _hasExplicitLimit = _template.Options.MaxIterations > 0;

    _pipeline = new DecoratorPipeline(_template.Options, collector);
    _candidateProcessor = new CandidateProcessor(
        result, template, _pipeline, collector, logger);
    _router = new TokenMatchRouter(
        template, _candidateProcessor, collector, hintStrategy);
}
```

Update the field declarations (lines 15-23):
```csharp
private readonly Template _template;
private readonly TokenizeResultBase _result;
private readonly IDiagnosticCollector _collector;
private readonly DecoratorPipeline _pipeline;
private readonly TokenMatchRouter _router;
private readonly CandidateProcessor _candidateProcessor;
private readonly bool _hasExplicitLimit;
private int _iterationCount;
```

Update `Finalize` method (line 142):
```csharp
private void Finalize(TokenizationContext context)
{
    _candidateProcessor.ProcessRemaining(context);
    FrontMatterProcessor.Process(_template, _result, _pipeline, context.Enumerator.Location);
    _collector.Record(DiagnosticEventType.TokenizationCompleted,
        detail: $"Matches: {_result.Tokens.Matches.Count}, Misses: {_result.Tokens.Misses.Count}");
}
```

- [ ] **Step 7: Update FrontMatterProcessor — remove target object**

In `src/Tokenizer/Tokenization/FrontMatterProcessor.cs`, remove `targetObject` parameter, replace `TokenAssigner` with `DecoratorPipeline`, rename `assigner` to `pipeline`:

```csharp
// src/Tokenizer/Tokenization/FrontMatterProcessor.cs — full replacement
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Processes front matter tokens that don't require input text matching.
/// </summary>
internal static class FrontMatterProcessor
{
    /// <summary>
    /// Iterates template tokens and evaluates values for any front matter tokens.
    /// </summary>
    public static void Process(
        Template template,
        TokenizeResultBase result,
        DecoratorPipeline pipeline,
        FileLocation location)
    {
        foreach (var token in template.Tokens)
        {
            if (!token.IsFrontMatterToken) continue;

            if (pipeline.Evaluate(token, string.Empty, location, out var evaluatedValue))
            {
                if (pipeline.Collector.IsEnabled)
                {
                    pipeline.Collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                        tokenName: token.Name, tokenId: token.Id,
                        value: evaluatedValue?.ToString());
                }
                if (evaluatedValue != null)
                {
                    result.Tokens.AddMatch(token, evaluatedValue, token.Location);
                }
            }
            else
            {
                if (pipeline.Collector.IsEnabled)
                {
                    pipeline.Collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                        tokenName: token.Name, tokenId: token.Id);
                }
            }
        }
    }
}
```

- [ ] **Step 8: Update TokenizationEngine and ITokenizationEngine — remove target object**

In `src/Tokenizer/Tokenization/ITokenizationEngine.cs`:
```csharp
internal interface ITokenizationEngine
{
    public TokenizationSession CreateSession(
        Template template,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null);
}
```

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`:
```csharp
public TokenizationSession CreateSession(
    Template template,
    TokenizeResultBase result,
    IDiagnosticCollector collector,
    IHintStrategy? hintStrategy = null)
{
    ArgumentValidation.ThrowIfNull(template, nameof(template));
    ArgumentValidation.ThrowIfNull(result, nameof(result));

    return new TokenizationSession(
        template, result, collector, hintStrategy, _log);
}
```

Remove the `InputValidator.ValidateTargetObject` call. Delete `src/Tokenizer/Tokenization/InputValidator.cs` (and its test if one exists).

- [ ] **Step 9: Update CandidateTokenList and CandidateProcessor tests**

Update `tests/Tokenizer.Tests/CandidateTokenListTests.cs`:
- Replace `TokenAssigner` with `DecoratorPipeline` in the static field
- Replace `TryAssign` calls with `TryEvaluate` (remove `target:` parameter)
- Replace `CanAnyAssign` calls with `CanAnyEvaluate`

```csharp
// Line 14 — change:
private static readonly DecoratorPipeline DefaultPipeline = new DecoratorPipeline(DefaultOptions, NullDiagnosticCollector.Instance);

// All TryAssign calls become TryEvaluate without target, e.g. line 197:
var result = list.TryEvaluate(value, DefaultPipeline, NoLocation, out var evaluated, out var evaluatedValue);

// All CanAnyAssign calls become CanAnyEvaluate, e.g. line 251:
var result = list.CanAnyEvaluate("some value", DefaultPipeline);
```

Update `tests/Tokenizer.Tests/Tokenization/CandidateProcessorTests.cs`:
- Replace `TokenAssigner` with `DecoratorPipeline`
- Remove `targetObject:` parameter from `CandidateProcessor` constructor calls
- Remove the `ThrowingTarget` test (that tested reflection, which moves to Task 3)

```csharp
// Each CandidateProcessor construction becomes:
var processor = new CandidateProcessor(
    result, template,
    new DecoratorPipeline(new TokenizerOptions(), NullDiagnosticCollector.Instance),
    NullDiagnosticCollector.Instance,
    NullLogger<TokenizationEngine>.Instance);
```

Update `tests/Tokenizer.Tests/Tokenization/FrontMatterProcessorTests.cs`:
- Replace `TokenAssigner` with `DecoratorPipeline`
- Remove `targetObject:` parameter from `Process()` calls

```csharp
// Each FrontMatterProcessor.Process call becomes:
var pipeline = new DecoratorPipeline(template.Options, collector);
FrontMatterProcessor.Process(template, result, pipeline, location);
```

- [ ] **Step 10: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS. The pipeline refactor should not change behavior — matching still produces the same `TokenMatch` records.

Note: Tests that use `Tokenize<T>()` will still pass because they go through `Tokenizer.cs` which still passes target objects. That path gets updated in Task 2.

- [ ] **Step 11: Commit**

```bash
git add -A
git commit -m "refactor: rename TokenAssigner to DecoratorPipeline, remove target object from matching pipeline"
```

---

### Task 2: Rewire Tokenizer to use two-stage flow

Update `Tokenizer.cs` to remove the target object from `TokenizeCore`/`TokenizeAsyncCore`. `Tokenize<T>()` becomes `Tokenize().Assign<T>()` (after Task 3 adds `Assign<T>()`). This task prepares the `Tokenizer` class but defers the `Assign<T>()` call to Task 3.

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/TokenizeResultBase.cs` (make `Success` virtual)
- Modify: `src/Tokenizer/TokenizeResult.cs` (override `Success`)

**Interfaces:**
- Consumes: `ITokenizationEngine.CreateSession(Template, TokenizeResultBase, IDiagnosticCollector, IHintStrategy?)` from Task 1
- Produces: `TokenizeResultBase.Success` as `virtual`

- [ ] **Step 1: Make Success virtual and override in TokenizeResult**

In `src/Tokenizer/TokenizeResultBase.cs`, change `Success` (line 63):
```csharp
// Before:
public bool Success => Tokens.HasMatches && ...

// After:
public virtual bool Success => Tokens.HasMatches &&
                       !Tokens.HasMissingRequiredTokens &&
                       !Hints.HasMissingRequiredHints &&
                       (Template.HasOnlyFrontMatterTokens || Tokens.Matches.Any(m => !m.Token.IsFrontMatterToken));
```

In `src/Tokenizer/TokenizeResult.cs`, add an override (the untyped result keeps the same logic, but now it's an explicit override for clarity — actually it doesn't need to override since the base logic is correct. Skip this for `TokenizeResult`).

- [ ] **Step 2: Update Tokenizer.cs — remove target object from core methods**

In `src/Tokenizer/Tokenizer.cs`:

Update `Tokenize(Template, string)` (line 79-86) — no change needed, already passes `null`.

Update `Tokenize<T>()` (lines 97-104) — temporarily keep creating the object and passing it, since `Assign<T>()` doesn't exist yet. This will be updated in Task 3. For now, just update the internal `Tokenize` call to not pass the value:

```csharp
// The private Tokenize method loses the value parameter:
private void Tokenize(TokenizeResultBase result, Template template, string input)
{
    if (template.Options.MaxInputLength > 0 && input.Length > template.Options.MaxInputLength)
    {
        throw new TokenizerException(
            $"Input length {input.Length.ToInvariant("N0")} exceeds maximum allowed length of {template.Options.MaxInputLength.ToInvariant("N0")}. " +
            "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
    }

    TokenizeCore(result, template, new StringReader(input), input);
}
```

Update `TokenizeCore` — remove `value` parameter, remove it from `CreateSession`:
```csharp
private void TokenizeCore(TokenizeResultBase result, Template template, TextReader reader, string? rawInput)
{
    // ... same as before but CreateSession call becomes:
    var session = _tokenizationEngine.CreateSession(template, result, collector, hintStrategy);
    // ... rest unchanged
}
```

Update `TokenizeAsyncCore` — same pattern, remove `value` parameter:
```csharp
private async Task TokenizeAsyncCore(TokenizeResultBase result, Template template, TextReader reader, CancellationToken ct)
{
    // ... same but CreateSession call becomes:
    var session = _tokenizationEngine.CreateSession(template, result, collector, hintStrategy);
    // ... rest unchanged
}
```

Update all callers of these private methods:
- `Tokenize(Template, string)` → `Tokenize(result, template, input)`
- `Tokenize<T>()` → `Tokenize(result, template, input)` (temporarily — will change in Task 3)
- `TokenizeAsync(Template, TextReader, CancellationToken)` → `TokenizeAsyncCore(result, template, input, ct)`
- `TokenizeAsync<T>()` → `TokenizeAsyncCore(result, template, input, ct)`

- [ ] **Step 3: Delete InputValidator**

Delete `src/Tokenizer/Tokenization/InputValidator.cs`. Check for and delete its test file if one exists.

Run: `find tests -name "InputValidator*" -type f` to locate any test file.

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS. `Tokenize<T>()` still creates the object but no longer passes it through the pipeline — the object won't be populated. Tests that assert on `result.Value` properties will fail.

Wait — this is a problem. If we remove target from the pipeline but haven't added `Assign<T>()` yet, `Tokenize<T>()` will return empty objects. We need to handle this carefully.

**Resolution:** In this step, `Tokenize<T>()` should remain as-is (calling the old path) until Task 3 provides `Assign<T>()`. Instead, only update the non-generic path and async-non-generic path in this task. The generic paths get updated in Task 3 after `Assign<T>()` exists.

Revised approach: Only update the private `Tokenize` and `TokenizeCore` to remove `value` for the non-generic callers. Keep a separate private method for the generic callers that still passes `null` (since the pipeline no longer uses it anyway after Task 1).

Actually, after Task 1, the pipeline ignores the target object entirely — it was removed from all components. So passing `null` vs not passing anything is moot. The `CreateSession` no longer takes `targetObject`. So all paths can be updated now.

The `Tokenize<T>()` still creates a `TokenizeResult<T>` with `Value = new T()` but the object won't be populated during matching. Tests that check `result.Value.Name == "Alice"` will fail. That's expected — they'll pass again after Task 3 adds `Assign<T>()`.

**Actually**, we should combine Tasks 2 and 3 to avoid a broken intermediate state. But the task structure asks for atomic commits. Let me restructure: Task 2 updates `Tokenizer.cs` AND adds `Assign<T>()` simultaneously, keeping tests green throughout.

Let me revise — merge this into Task 3.

- [ ] **Step 4 (revised): Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: Tests that use `Tokenize<T>()` and assert on `result.Value` properties will fail. This is expected — Task 3 restores them by adding `Assign<T>()`.

Note: If you want green tests at every commit, complete Task 3 steps 1-4 before committing this task. Alternatively, commit with `--no-verify` and note the temporary breakage. **Recommended: proceed directly to Task 3 and commit both together.**

- [ ] **Step 5: Commit (combined with Task 3)**

This commit is deferred to Task 3, step 7.

---

### Task 3: Add Assign\<T\>() to TokenizeResult and rewire Tokenize\<T\>()

Add the `Assign<T>()` method on `TokenizeResult` that creates a `TokenizeResult<T>` from matches. Update `TokenizeResult<T>` with a projection constructor and `Success` override. Rewire `Tokenize<T>()` to use `Tokenize().Assign<T>()`.

**Files:**
- Modify: `src/Tokenizer/TokenizeResult.cs` — add `Assign<T>()` method
- Modify: `src/Tokenizer/TokenizeResult.cs` (the `TokenizeResult<T>` class in same file) — add projection constructor, override `Success`
- Modify: `src/Tokenizer/Tokenizer.cs` — rewire `Tokenize<T>()`/`TokenizeAsync<T>()`
- Create: `tests/Tokenizer.Tests/TokenizeResultAssignTests.cs`

**Interfaces:**
- Consumes: `TokenizeResult.Matches`, `ObjectExtensions.SetValue`, `ObjectExtensions.GetValue`, `ValueConcatenation`
- Produces: `TokenizeResult.Assign<T>() → TokenizeResult<T>`

- [ ] **Step 1: Write failing tests for Assign\<T\>()**

```csharp
// tests/Tokenizer.Tests/TokenizeResultAssignTests.cs
using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenizeResultAssignTests : TokenizerTestBase
{
    public TokenizeResultAssignTests(ITestOutputHelper output) : base(output)
    {
    }

    public class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public int? Score { get; set; }
    }

    public class PersonSummary
    {
        public string Name { get; set; } = null!;
    }

    [Fact]
    public void GivenMatchesWithStringValue_WhenAssign_ThenPopulatesProperty()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.Equal("Alice", typed.Value.Name);
        Assert.True(typed.Success);
    }

    [Fact]
    public void GivenMatchesWithMultipleProperties_WhenAssign_ThenPopulatesAll()
    {
        // Arrange
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var ageToken = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, ageToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Bob", new FileLocation()),
                new TokenMatch(ageToken, 30, new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.Equal("Bob", typed.Value.Name);
        Assert.Equal(30, typed.Value.Age);
    }

    [Fact]
    public void GivenTypeConversionFailure_WhenAssign_ThenSuccessIsFalseAndExceptionRecorded()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "not-a-number", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.False(typed.Success);
        Assert.Single(typed.Exceptions);
        Assert.IsType<TypeConversionException>(typed.Exceptions[0]);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreEnabled_WhenAssign_ThenSuccessIsTrue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("NonExistent").Build();
        var options = new TokenizerOptions { IgnoreMissingProperties = true };
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithOptions(options).Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.True(typed.Success);
        Assert.Empty(typed.Exceptions);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreDisabled_WhenAssign_ThenSuccessIsFalseAndExceptionRecorded()
    {
        // Arrange
        var token = new TokenBuilder().WithName("NonExistent").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.False(typed.Success);
        Assert.Single(typed.Exceptions);
        Assert.IsType<MissingMemberException>(typed.Exceptions[0]);
    }

    [Fact]
    public void GivenConcatenatableToken_WhenAssign_ThenConcatenatesValues()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        token.CanConcatenate = true;
        token.ConcatenationString = ", ";
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        // Note: concatenation is handled in TokenResult.AddMatch during Stage 1,
        // so the match list will have a single concatenated entry.
        // But if the result has two separate matches (e.g. from repeating non-concat tokens),
        // Assign should handle SetValue for each.
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice, Bob", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.Equal("Alice, Bob", typed.Value.Name);
    }

    [Fact]
    public void GivenDictionaryTarget_WhenAssign_ThenSetsKeyValues()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Key").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Value", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Dictionary<string, object>>();

        // Assert
        Assert.Equal("Value", typed.Value["Key"]);
    }

    [Fact]
    public void GivenRepeatingTokenWithDictionaryTarget_WhenAssign_ThenBuildsListValue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Items").WithRepeating(true).Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(token, "one", new FileLocation()),
                new TokenMatch(token, "two", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Dictionary<string, object>>();

        // Assert
        var list = Assert.IsType<List<object>>(typed.Value["Items"]);
        Assert.Equal(2, list.Count);
        Assert.Equal("one", list[0]);
        Assert.Equal("two", list[1]);
    }

    [Fact]
    public void GivenResult_WhenAssignCalledTwice_ThenOriginalIsUnmodified()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .Build();

        // Act
        var first = result.Assign<Person>();
        var second = result.Assign<PersonSummary>();

        // Assert
        Assert.Equal("Alice", first.Value.Name);
        Assert.Equal("Alice", second.Value.Name);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void GivenResultWithStageOneExceptions_WhenAssign_ThenStageOneExceptionsNotCopied()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .WithExceptions(new InvalidOperationException("stage 1 error"))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.Single(result.Exceptions); // Stage 1 exception stays on original
        Assert.Empty(typed.Exceptions);    // Not copied to typed result
        Assert.True(typed.Success);
    }

    [Fact]
    public void GivenSuccessfulResult_WhenAssignedWithTypeMismatch_ThenMatchingSuccessUnaffected()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "not-a-number", new FileLocation()))
            .Build();

        // Act & Assert
        Assert.True(result.Success); // Matching succeeded
        var typed = result.Assign<Person>();
        Assert.False(typed.Success); // Assignment failed
        Assert.True(result.Success); // Original unchanged
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizeResultAssignTests"`
Expected: FAIL — `Assign<T>()` method does not exist.

- [ ] **Step 3: Add projection constructor to TokenizeResult\<T\>**

In `src/Tokenizer/TokenizeResult.cs`, add to the `TokenizeResult<T>` class:

```csharp
public sealed class TokenizeResult<T> : TokenizeResultBase where T : class, new()
{
    /// <summary>
    /// Creates a new instance with a fresh <typeparamref name="T"/> bound to the given template.
    /// </summary>
    public TokenizeResult(Template template) : base(template)
    {
        Value = new T();
    }

    /// <summary>
    /// Creates a projected result carrying forward matching state from a completed tokenization.
    /// Stage 1 exceptions are not copied — only assignment exceptions belong on typed results.
    /// </summary>
    internal TokenizeResult(Template template, TokenResult tokens, HintResult hints, Diagnostics.DiagnosticResult? diagnostics)
        : base(template, tokens, hints, diagnostics)
    {
        Value = new T();
    }

    /// <summary>
    /// An instance of <typeparamref name="T"/> populated with data from the input string.
    /// </summary>
    public T Value { get; init; }

    /// <summary>
    /// True when matching succeeded and no assignment errors occurred.
    /// </summary>
    public override bool Success => base.Success && Exceptions.Count == 0;
}
```

This requires a new `protected` constructor on `TokenizeResultBase` that accepts pre-built state:

```csharp
// Add to TokenizeResultBase — new protected constructor:
/// <summary>
/// Creates a projected result carrying forward state from a completed tokenization.
/// </summary>
protected TokenizeResultBase(Template template, TokenResult tokens, HintResult hints, Diagnostics.DiagnosticResult? diagnostics)
{
    _exceptions = new List<Exception>();
    Template = template;
    Tokens = tokens;
    Hints = hints;
    Diagnostics = diagnostics;
}
```

- [ ] **Step 4: Implement Assign\<T\>() on TokenizeResult**

Add to `src/Tokenizer/TokenizeResult.cs` in the `TokenizeResult` class:

```csharp
/// <summary>
/// Projects this result onto a new instance of <typeparamref name="T"/>,
/// assigning matched values to the object's properties via reflection.
/// The original result is not modified.
/// </summary>
/// <typeparam name="T">The type to populate with matched values.</typeparam>
/// <returns>A new <see cref="TokenizeResult{T}"/> with the populated object.</returns>
public TokenizeResult<T> Assign<T>() where T : class, new()
{
    var typed = new TokenizeResult<T>(Template, Tokens, Hints, Diagnostics);
    var target = typed.Value;
    var options = Template.Options;

    if (target is IDictionary<string, object> dictionary)
    {
        AssignToDictionary(dictionary, typed);
    }
    else
    {
        AssignToObject(target, options, typed);
    }

    return typed;
}

private static void AssignToDictionary(IDictionary<string, object> dictionary, TokenizeResultBase typed)
{
    foreach (var match in typed.Tokens.Matches)
    {
        if (match.Token.IsRepeating)
        {
            List<object> list;
            if (dictionary.ContainsKey(match.Token.Name))
            {
                list = dictionary[match.Token.Name] as List<object> ?? new List<object> { dictionary[match.Token.Name] };
            }
            else
            {
                list = new List<object>();
            }
            list.Add(match.Value);
            dictionary[match.Token.Name] = list;
        }
        else if (dictionary.ContainsKey(match.Token.Name))
        {
            dictionary[match.Token.Name] = match.Value;
        }
        else
        {
            dictionary.Add(match.Token.Name, match.Value);
        }
    }
}

private static void AssignToObject(object target, TokenizerOptions options, TokenizeResultBase typed)
{
    foreach (var match in typed.Tokens.Matches)
    {
        try
        {
            target.SetValue(match.Token.Name, match.Value, StringComparison.Ordinal);
        }
        catch (MissingMemberException)
        {
            if (!options.IgnoreMissingProperties)
            {
                typed.AddException(new MissingMemberException(
                    $"Property '{match.Token.Name}' not found on type '{target.GetType().Name}'."));
            }
        }
        catch (TypeConversionException ex)
        {
            typed.AddException(ex);
        }
        catch (Exceptions.TokenAssignmentException ex)
        {
            typed.AddException(ex);
        }
    }
}
```

Add the required `using` at the top of `TokenizeResult.cs`:
```csharp
using Tokens.Exceptions;
using Tokens.Extensions;
```

- [ ] **Step 5: Run Assign\<T\>() tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizeResultAssignTests"`
Expected: ALL PASS

- [ ] **Step 6: Rewire Tokenize\<T\>() and TokenizeAsync\<T\>() in Tokenizer.cs**

Update `src/Tokenizer/Tokenizer.cs`:

```csharp
// Tokenize<T>() — line 97
public TokenizeResult<T> Tokenize<T>(Template template, string input) where T : class, new()
{
    return Tokenize(template, input).Assign<T>();
}

// Tokenize() — line 79 — update to call without value parameter
public TokenizeResult Tokenize(Template template, string input)
{
    var result = new TokenizeResult(template);
    Tokenize(result, template, input);
    return result;
}

// Private Tokenize — remove value parameter
private void Tokenize(TokenizeResultBase result, Template template, string input)
{
    if (template.Options.MaxInputLength > 0 && input.Length > template.Options.MaxInputLength)
    {
        throw new TokenizerException(
            $"Input length {input.Length.ToInvariant("N0")} exceeds maximum allowed length of {template.Options.MaxInputLength.ToInvariant("N0")}. " +
            "Increase TokenizerOptions.MaxInputLength to allow larger inputs.");
    }

    TokenizeCore(result, template, new StringReader(input), input);
}

// TokenizeCore — remove value parameter
private void TokenizeCore(TokenizeResultBase result, Template template, TextReader reader, string? rawInput)
{
    // ... identical body but CreateSession call becomes:
    var session = _tokenizationEngine.CreateSession(template, result, collector, hintStrategy);
    // ... rest unchanged
}

// TokenizeAsync<T>() TextReader overload — line 312
public async Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct = default) where T : class, new()
{
    var result = await TokenizeAsync(template, input, ct).ConfigureAwait(false);
    return result.Assign<T>();
}

// TokenizeAsync<T>() Stream overload — line 342
public async Task<TokenizeResult<T>> TokenizeAsync<T>(Template template, Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new()
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return await TokenizeAsync<T>(template, reader, ct).ConfigureAwait(false);
}

// TokenizeAsyncCore — remove value parameter
private async Task TokenizeAsyncCore(TokenizeResultBase result, Template template, TextReader reader, CancellationToken ct)
{
    // ... identical body but CreateSession call becomes:
    var session = _tokenizationEngine.CreateSession(template, result, collector, hintStrategy);
    // ... rest unchanged
}

// TokenizeAsync (non-generic) — update calls
public async Task<TokenizeResult> TokenizeAsync(Template template, TextReader input, CancellationToken ct = default)
{
    var result = new TokenizeResult(template);
    await TokenizeAsyncCore(result, template, input, ct).ConfigureAwait(false);
    return result;
}
```

- [ ] **Step 7: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS. The two-stage pipeline now produces identical results to the old single-stage pipeline.

- [ ] **Step 8: Commit Tasks 2 and 3 together**

```bash
git add -A
git commit -m "feat: add Assign<T>() to TokenizeResult, rewire Tokenize<T>() to two-stage pipeline"
```

---

### Task 4: Update TokenizeResult\<T\> builder and remaining test infrastructure

Update the test builder for `TokenizeResult<T>` to work with the new projection constructor. Verify the `TokenizeResultBuilder` tests for `ThrowingTarget` (moved from CandidateProcessor) coverage lives in the right place.

**Files:**
- Modify: `tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs`
- Modify: `tests/Tokenizer.Tests/Tokenization/TokenizationSessionTests.cs` (if referencing target object)

**Interfaces:**
- Consumes: `TokenizeResult<T>` projection constructor from Task 3

- [ ] **Step 1: Check TokenizationSession tests for target object references**

Read `tests/Tokenizer.Tests/Tokenization/TokenizationSessionTests.cs` and check if any tests construct `TokenizationSession` directly with a target object parameter. Update any that do.

If tests use `Tokenizer.Tokenize<T>()` end-to-end, they should still pass unchanged (the public API is the same).

- [ ] **Step 2: Search for any remaining references to TokenAssigner**

Run: `grep -r "TokenAssigner" --include="*.cs" src/ tests/`

Any remaining references must be updated to `DecoratorPipeline`. Fix all hits.

- [ ] **Step 3: Search for any remaining targetObject/target references in the pipeline**

Run: `grep -r "targetObject\|target:" --include="*.cs" src/Tokenizer/Tokenization/`

Verify no matching pipeline files reference target objects. The only `target` references should be in `TokenizeResult.cs` (in `Assign<T>()`).

- [ ] **Step 4: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Run build in Release mode**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with no warnings.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore: update test infrastructure for two-stage tokenization"
```

---

### Task 5: Delete InputValidator and clean up dead code

Remove `InputValidator.cs` and its tests. Remove any other dead code from the refactor (unused usings, etc.).

**Files:**
- Delete: `src/Tokenizer/Tokenization/InputValidator.cs`
- Delete: test file for InputValidator (if exists)

- [ ] **Step 1: Check for InputValidator test file**

Run: `find tests -name "*InputValidator*" -type f`

- [ ] **Step 2: Delete InputValidator and its tests**

Delete `src/Tokenizer/Tokenization/InputValidator.cs` and any test file found in step 1.

- [ ] **Step 3: Search for remaining references**

Run: `grep -r "InputValidator" --include="*.cs" src/ tests/`

Fix any remaining references.

- [ ] **Step 4: Run dotnet format to clean up unused usings**

Run: `dotnet format style ./Tokenizer.sln --diagnostics IDE0005`

- [ ] **Step 5: Run full test suite and build**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj && dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: ALL PASS, build succeeds

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore: remove InputValidator and dead code from two-stage refactor"
```
