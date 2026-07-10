# Phase 4: Token-Centric Diagnostic Model

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the flat event list + separate issues list with a per-token diagnostic model that tells the complete story of each token.

**Architecture:** New value types (`TokenDiagnostic`, `TokenAttempt`, `TokenOutcome`, `AttemptOutcome`) aggregate raw events into per-token narratives. A `TokenDiagnosticBuilder` constructs these from the raw event stream. `DiagnosticResult` is redesigned: `.Tokens` is the primary API, `.RawEvents` keeps the flat trace, `.Verdict` replaces `Summary.Verdict`. The old API (`Summary`, `Failures`, `ForToken()`, `FirstFailure`, `Events`) and `DiagnosticSummary` are removed.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 8.0 + .NET 10.0, xUnit 2.9.3

## Global Constraints

- Targets: .NET Standard 2.0, .NET 8.0, .NET 10.0
- Root namespace: `Tokens`
- Allman braces, file-scoped namespaces, `_camelCase` private fields
- `internal` for implementation types, `public` for API surface
- Test naming: Gherkin `GivenScenario_WhenAction_ThenResult()`
- `// Arrange` / `// Act` / `// Assert` comments in all tests
- `StringComparison.Ordinal` for token name comparisons
- All existing tests must pass after each task (modifying tests that reference old API is expected)

---

### Task 1: Create new value types (TokenOutcome, AttemptOutcome, TokenAttempt, TokenDiagnostic)

**Files:**
- Create: `src/Tokenizer/Diagnostics/TokenOutcome.cs`
- Create: `src/Tokenizer/Diagnostics/AttemptOutcome.cs`
- Create: `src/Tokenizer/Diagnostics/TokenAttempt.cs`
- Create: `src/Tokenizer/Diagnostics/TokenDiagnostic.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticTests.cs` (new)

**Interfaces:**
- Consumes: `DiagnosticIssue`, `DiagnosticIssueType`, `FileLocation` (from `Tokens.Enumerators`)
- Produces: `TokenOutcome` enum, `AttemptOutcome` enum, `TokenAttempt` class, `TokenDiagnostic` class — used by Task 2's builder and Task 3's DiagnosticResult

- [ ] **Step 1: Create the four type files**

Create `src/Tokenizer/Diagnostics/TokenOutcome.cs`:

```csharp
namespace Tokens.Diagnostics;

/// <summary>
/// The final outcome of a token during tokenization.
/// </summary>
public enum TokenOutcome
{
    /// <summary>
    /// Token was successfully matched and assigned a value.
    /// </summary>
    Matched,

    /// <summary>
    /// Token's preamble was found but all values were rejected
    /// by validators or transformers.
    /// </summary>
    Rejected,

    /// <summary>
    /// Token's preamble was never found in the input.
    /// </summary>
    NeverFound,

    /// <summary>
    /// Token was not searched for because a prior required token
    /// failed to match. Defined but not populated until Phase 6.
    /// </summary>
    Blocked,
}
```

Create `src/Tokenizer/Diagnostics/AttemptOutcome.cs`:

```csharp
namespace Tokens.Diagnostics;

/// <summary>
/// The outcome of a single attempt to match a token.
/// </summary>
public enum AttemptOutcome
{
    /// <summary>
    /// Value was accepted and assigned to the token.
    /// </summary>
    Assigned,

    /// <summary>
    /// A validator rejected the value.
    /// </summary>
    ValidatorRejected,

    /// <summary>
    /// A transformer failed to convert the value.
    /// </summary>
    TransformerFailed,

    /// <summary>
    /// The engine backtracked past this match.
    /// </summary>
    Backtracked,
}
```

Create `src/Tokenizer/Diagnostics/TokenAttempt.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// A single attempt to match a token at a specific location in the input.
/// </summary>
public sealed class TokenAttempt
{
    /// <summary>
    /// Position in the input where this attempt occurred.
    /// </summary>
    public FileLocation? Location { get; init; }

    /// <summary>
    /// The value that was considered.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// What happened with this attempt.
    /// </summary>
    public AttemptOutcome Outcome { get; init; }

    /// <summary>
    /// The decorator that rejected/failed, if applicable.
    /// </summary>
    public string? DecoratorName { get; init; }

    /// <summary>
    /// Human-readable explanation of why this attempt failed.
    /// </summary>
    public string? Reason { get; init; }
}
```

Create `src/Tokenizer/Diagnostics/TokenDiagnostic.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// The complete diagnostic story for a single token during tokenization.
/// </summary>
public sealed class TokenDiagnostic
{
    /// <summary>
    /// Token name from the template.
    /// </summary>
    public string TokenName { get; init; } = string.Empty;

    /// <summary>
    /// Unique token ID within the template.
    /// </summary>
    public int TokenId { get; init; }

    /// <summary>
    /// Final outcome of this token.
    /// </summary>
    public TokenOutcome Outcome { get; init; }

    /// <summary>
    /// Every time this token was considered during tokenization.
    /// </summary>
    public IReadOnlyList<TokenAttempt> Attempts { get; init; } = [];

    /// <summary>
    /// The final assigned value, if Outcome is Matched.
    /// </summary>
    public string? AssignedValue { get; init; }

    /// <summary>
    /// Where in the input the token was matched, if Outcome is Matched.
    /// </summary>
    public FileLocation? AssignedLocation { get; init; }

    /// <summary>
    /// Issues identified for this token (with adaptive hints).
    /// </summary>
    public IReadOnlyList<DiagnosticIssue> Issues { get; init; } = [];
}
```

- [ ] **Step 2: Write basic tests for TokenDiagnostic**

Create `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticTests.cs`:

```csharp
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Diagnostics;

public class TokenDiagnosticTests
{
    [Fact]
    public void GivenMatchedToken_WhenCreated_ThenPropertiesAreAccessible()
    {
        // Arrange & Act
        var diagnostic = new TokenDiagnostic
        {
            TokenName = "Email",
            TokenId = 1,
            Outcome = TokenOutcome.Matched,
            AssignedValue = "user@example.com",
            AssignedLocation = new FileLocation(),
            Attempts = new[]
            {
                new TokenAttempt
                {
                    Value = "user@example.com",
                    Outcome = AttemptOutcome.Assigned,
                    Location = new FileLocation(),
                },
            },
        };

        // Assert
        Assert.Equal("Email", diagnostic.TokenName);
        Assert.Equal(TokenOutcome.Matched, diagnostic.Outcome);
        Assert.Equal("user@example.com", diagnostic.AssignedValue);
        Assert.Single(diagnostic.Attempts);
        Assert.Equal(AttemptOutcome.Assigned, diagnostic.Attempts[0].Outcome);
    }

    [Fact]
    public void GivenRejectedToken_WhenCreated_ThenAttemptsShowRejections()
    {
        // Arrange & Act
        var diagnostic = new TokenDiagnostic
        {
            TokenName = "Email",
            TokenId = 1,
            Outcome = TokenOutcome.Rejected,
            Attempts = new[]
            {
                new TokenAttempt
                {
                    Value = "bad1",
                    Outcome = AttemptOutcome.ValidatorRejected,
                    DecoratorName = "IsEmailValidator",
                    Reason = "Validator 'IsEmailValidator' rejected value 'bad1'.",
                },
                new TokenAttempt
                {
                    Value = "bad2",
                    Outcome = AttemptOutcome.ValidatorRejected,
                    DecoratorName = "IsEmailValidator",
                    Reason = "Validator 'IsEmailValidator' rejected value 'bad2'.",
                },
            },
            Issues = new[]
            {
                new DiagnosticIssue
                {
                    Type = DiagnosticIssueType.ValidatorRejection,
                    TokenName = "Email",
                    Description = "Validator 'IsEmailValidator' rejected value 'bad1'.",
                },
            },
        };

        // Assert
        Assert.Equal(TokenOutcome.Rejected, diagnostic.Outcome);
        Assert.Equal(2, diagnostic.Attempts.Count);
        Assert.All(diagnostic.Attempts, a => Assert.Equal(AttemptOutcome.ValidatorRejected, a.Outcome));
        Assert.Single(diagnostic.Issues);
    }

    [Fact]
    public void GivenNeverFoundToken_WhenCreated_ThenNoAttemptsAndNoAssignedValue()
    {
        // Arrange & Act
        var diagnostic = new TokenDiagnostic
        {
            TokenName = "Missing",
            TokenId = 2,
            Outcome = TokenOutcome.NeverFound,
        };

        // Assert
        Assert.Equal(TokenOutcome.NeverFound, diagnostic.Outcome);
        Assert.Null(diagnostic.AssignedValue);
        Assert.Empty(diagnostic.Attempts);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenDiagnosticTests" -v n`

Expected: PASS (3 tests)

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Diagnostics/TokenOutcome.cs src/Tokenizer/Diagnostics/AttemptOutcome.cs src/Tokenizer/Diagnostics/TokenAttempt.cs src/Tokenizer/Diagnostics/TokenDiagnostic.cs tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticTests.cs
git commit -m "Add TokenDiagnostic, TokenAttempt, TokenOutcome, AttemptOutcome types"
```

---

### Task 2: Create TokenDiagnosticBuilder

**Files:**
- Create: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs` (new)

**Interfaces:**
- Consumes: `DiagnosticResult` (for `.Events` and `.InputContent`), `DiagnosticEvent`, `DiagnosticEventType`, `DiagnosticIssue`, `DiagnosticIssueType`, `TokenDiagnostic`, `TokenAttempt`, `TokenOutcome`, `AttemptOutcome`, hint generators from `DiagnosticSummaryBuilder`
- Produces: `TokenDiagnosticBuilder.Build(DiagnosticResult)` → `(IReadOnlyList<TokenDiagnostic> tokens, string verdict)` — used by Task 3

The builder scans the raw event stream and constructs a `TokenDiagnostic` per token. It reuses the hint generation and description building logic from `DiagnosticSummaryBuilder`.

- [ ] **Step 1: Write tests for the builder**

Create `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`:

```csharp
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Diagnostics;

public class TokenDiagnosticBuilderTests
{
    [Fact]
    public void GivenSingleMatchedToken_WhenBuilding_ThenTokenHasMatchedOutcome()
    {
        // Arrange
        var collector = new DiagnosticCollector("Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Name", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Name", value: "John");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name", value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, verdict) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        Assert.Single(tokens);
        Assert.Equal("Name", tokens[0].TokenName);
        Assert.Equal(TokenOutcome.Matched, tokens[0].Outcome);
        Assert.Equal("John", tokens[0].AssignedValue);
        Assert.Single(tokens[0].Attempts);
        Assert.Equal(AttemptOutcome.Assigned, tokens[0].Attempts[0].Outcome);
        Assert.Empty(tokens[0].Issues);
    }

    [Fact]
    public void GivenMissedToken_WhenBuilding_ThenTokenHasNeverFoundOutcome()
    {
        // Arrange
        var collector = new DiagnosticCollector("nothing");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Name");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        Assert.Single(tokens);
        Assert.Equal("Name", tokens[0].TokenName);
        Assert.Equal(TokenOutcome.NeverFound, tokens[0].Outcome);
        Assert.Null(tokens[0].AssignedValue);
        Assert.Empty(tokens[0].Attempts);
        Assert.Single(tokens[0].Issues);
        Assert.Equal(DiagnosticIssueType.PreambleNeverFound, tokens[0].Issues[0].Type);
    }

    [Fact]
    public void GivenValidatorRejection_WhenBuilding_ThenTokenHasRejectedOutcomeWithAttempts()
    {
        // Arrange
        var collector = new DiagnosticCollector("Email: bad");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Email", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Email", value: "bad");
        collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: "Email",
            decoratorName: "IsEmailValidator", value: "bad", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentFailed, tokenName: "Email", value: "bad");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Email");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        Assert.Single(tokens);
        Assert.Equal(TokenOutcome.Rejected, tokens[0].Outcome);
        Assert.Single(tokens[0].Attempts);
        Assert.Equal(AttemptOutcome.ValidatorRejected, tokens[0].Attempts[0].Outcome);
        Assert.Equal("IsEmailValidator", tokens[0].Attempts[0].DecoratorName);
        Assert.Single(tokens[0].Issues);
        Assert.Equal(DiagnosticIssueType.ValidatorRejection, tokens[0].Issues[0].Type);
    }

    [Fact]
    public void GivenTransformerFailure_WhenBuilding_ThenTokenHasRejectedOutcomeWithAttempt()
    {
        // Arrange
        var collector = new DiagnosticCollector("Date: not-a-date");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Date", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Date", value: "not-a-date");
        collector.Record(DiagnosticEventType.TransformerFailed, tokenName: "Date",
            decoratorName: "ToDateTimeTransformer", value: "not-a-date", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentFailed, tokenName: "Date", value: "not-a-date");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Date");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        Assert.Single(tokens);
        Assert.Equal(TokenOutcome.Rejected, tokens[0].Outcome);
        Assert.Single(tokens[0].Attempts);
        Assert.Equal(AttemptOutcome.TransformerFailed, tokens[0].Attempts[0].Outcome);
    }

    [Fact]
    public void GivenMultipleAttemptsOneSuccess_WhenBuilding_ThenMatchedWithMultipleAttempts()
    {
        // Arrange
        var collector = new DiagnosticCollector("Email: bad\nEmail: good@email.com");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        // First attempt — rejected
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Email", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Email", value: "bad");
        collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: "Email",
            decoratorName: "IsEmailValidator", value: "bad", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentFailed, tokenName: "Email", value: "bad");
        // Second attempt — accepted
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Email", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssignmentAttempted, tokenName: "Email", value: "good@email.com");
        collector.Record(DiagnosticEventType.ValidatorPassed, tokenName: "Email",
            decoratorName: "IsEmailValidator", value: "good@email.com");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Email",
            value: "good@email.com", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        Assert.Single(tokens);
        Assert.Equal(TokenOutcome.Matched, tokens[0].Outcome);
        Assert.Equal("good@email.com", tokens[0].AssignedValue);
        Assert.Equal(2, tokens[0].Attempts.Count);
        Assert.Equal(AttemptOutcome.ValidatorRejected, tokens[0].Attempts[0].Outcome);
        Assert.Equal(AttemptOutcome.Assigned, tokens[0].Attempts[1].Outcome);
    }

    [Fact]
    public void GivenMixedTokens_WhenBuilding_ThenVerdictReflectsMatchAndMiss()
    {
        // Arrange
        var collector = new DiagnosticCollector("Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name", value: "John");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Age");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, verdict) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal("Matched 1 of 2 tokens (1 missed).", verdict);
    }

    [Fact]
    public void GivenHintMissing_WhenBuilding_ThenHintMissingIssueCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("no hint");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.HintMissing, value: "Expected text");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var diagnostics = collector.GetResult()!;

        // Act
        var (tokens, _) = TokenDiagnosticBuilder.Build(diagnostics);

        // Assert
        // Hint missing is a global issue, not per-token — it produces a TokenDiagnostic
        // with no token name if there's no token associated
        var hintIssues = tokens.SelectMany(t => t.Issues)
            .Where(i => i.Type == DiagnosticIssueType.HintMissing).ToList();
        Assert.Single(hintIssues);
    }
}
```

- [ ] **Step 2: Run to verify tests fail (type exists but builder does not)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenDiagnosticBuilderTests" -v n`

Expected: Compile error — `TokenDiagnosticBuilder` doesn't exist.

- [ ] **Step 3: Implement TokenDiagnosticBuilder**

Create `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`:

```csharp
using System.Text;
using Tokens.Diagnostics.Hints;
using Tokens.Extensions;

namespace Tokens.Diagnostics;

internal static class TokenDiagnosticBuilder
{
    private static readonly IHintGenerator[] HintGenerators =
    {
        new DateFormatHintGenerator(),
        new PreambleNearMissHintGenerator(),
        new ValidatorValueHintGenerator(),
        new UnmatchedInputHintGenerator(),
        new RepeatingTokenHintGenerator(),
    };

    public static (IReadOnlyList<TokenDiagnostic> tokens, string verdict) Build(DiagnosticResult diagnostics)
    {
        var events = diagnostics.Events;
        var attempts = new Dictionary<string, List<TokenAttempt>>(StringComparer.Ordinal);
        var issues = new Dictionary<string, List<DiagnosticIssue>>(StringComparer.Ordinal);
        var assignedTokens = new Dictionary<string, (string? value, Tokens.Enumerators.FileLocation? location)>(StringComparer.Ordinal);
        var tokenIds = new Dictionary<string, int>(StringComparer.Ordinal);

        // Collect token names that have transformer or validator failures
        var tokensWithFailures = new HashSet<string>(
            events
                .Where(e => (e.Type == DiagnosticEventType.TransformerFailed
                          || e.Type == DiagnosticEventType.ValidatorFailed)
                         && e.TokenName != null)
                .Select(e => e.TokenName!),
            StringComparer.Ordinal);

        // Track all unique token names in order of first appearance
        var tokenOrder = new List<string>();
        var seenTokens = new HashSet<string>(StringComparer.Ordinal);

        // Global issues (e.g. HintMissing without a token name)
        var globalIssues = new List<DiagnosticIssue>();

        foreach (var evt in events)
        {
            if (evt.TokenName != null && seenTokens.Add(evt.TokenName))
            {
                tokenOrder.Add(evt.TokenName);
            }

            if (evt.TokenName != null && evt.TokenId.HasValue && !tokenIds.ContainsKey(evt.TokenName))
            {
                tokenIds[evt.TokenName] = evt.TokenId.Value;
            }

            switch (evt.Type)
            {
                case DiagnosticEventType.ValidatorFailed:
                    AddAttempt(attempts, evt.TokenName!, new TokenAttempt
                    {
                        Location = evt.Location,
                        Value = evt.Value,
                        Outcome = AttemptOutcome.ValidatorRejected,
                        DecoratorName = evt.DecoratorName,
                        Reason = BuildValidatorDescription(evt),
                    });
                    AddIssue(issues, evt, DiagnosticIssueType.ValidatorRejection,
                        BuildValidatorDescription(evt), diagnostics);
                    break;

                case DiagnosticEventType.TransformerFailed:
                    AddAttempt(attempts, evt.TokenName!, new TokenAttempt
                    {
                        Location = evt.Location,
                        Value = evt.Value,
                        Outcome = AttemptOutcome.TransformerFailed,
                        DecoratorName = evt.DecoratorName,
                        Reason = BuildTransformerDescription(evt),
                    });
                    AddIssue(issues, evt, DiagnosticIssueType.TransformerFailure,
                        BuildTransformerDescription(evt), diagnostics);
                    break;

                case DiagnosticEventType.TokenAssigned:
                    if (evt.TokenName != null)
                    {
                        assignedTokens[evt.TokenName] = (evt.Value, evt.Location);
                        AddAttempt(attempts, evt.TokenName, new TokenAttempt
                        {
                            Location = evt.Location,
                            Value = evt.Value,
                            Outcome = AttemptOutcome.Assigned,
                        });
                    }
                    break;

                case DiagnosticEventType.BacktrackStarted:
                    if (evt.TokenName != null)
                    {
                        AddAttempt(attempts, evt.TokenName, new TokenAttempt
                        {
                            Location = evt.Location,
                            Value = evt.Value,
                            Outcome = AttemptOutcome.Backtracked,
                        });
                    }
                    break;

                case DiagnosticEventType.TokenMissed:
                    if (evt.TokenName != null && !tokensWithFailures.Contains(evt.TokenName))
                    {
                        AddIssue(issues, evt, DiagnosticIssueType.PreambleNeverFound,
                            $"Token '{evt.TokenName}' was never matched in the input.", diagnostics);
                    }
                    break;

                case DiagnosticEventType.RepeatingTokenDisabled:
                    if (evt.TokenName != null)
                    {
                        AddIssue(issues, evt, DiagnosticIssueType.RepeatingTokenCutShort,
                            BuildRepeatingTokenDescription(evt), diagnostics);
                    }
                    break;

                case DiagnosticEventType.HintMissing:
                    var hintDesc = string.IsNullOrEmpty(evt.Value)
                        ? "A required hint was not found in the input."
                        : $"Required hint not found in input: '{evt.Value}'.";
                    if (evt.TokenName != null)
                    {
                        AddIssue(issues, evt, DiagnosticIssueType.HintMissing, hintDesc, diagnostics);
                    }
                    else
                    {
                        globalIssues.Add(CreateIssue(DiagnosticIssueType.HintMissing, evt, hintDesc, diagnostics));
                    }
                    break;
            }
        }

        // Build TokenDiagnostic list
        var result = new List<TokenDiagnostic>();

        foreach (var tokenName in tokenOrder)
        {
            // Skip lifecycle-only events (TokenizationStarted/Completed have no token name,
            // but some events like PreambleSearchStarted mention tokens without being failures)
            var isAssigned = assignedTokens.ContainsKey(tokenName);
            var hasFailures = tokensWithFailures.Contains(tokenName);
            var isMissed = events.Any(e => e.Type == DiagnosticEventType.TokenMissed
                && string.Equals(e.TokenName, tokenName, StringComparison.Ordinal));

            TokenOutcome outcome;
            if (isAssigned)
                outcome = TokenOutcome.Matched;
            else if (hasFailures)
                outcome = TokenOutcome.Rejected;
            else if (isMissed)
                outcome = TokenOutcome.NeverFound;
            else
                continue; // Token was mentioned in events but has no terminal state (e.g. PreambleSearchStarted only)

            var tokenAttempts = attempts.TryGetValue(tokenName, out var a) ? a : new List<TokenAttempt>();
            var tokenIssues = issues.TryGetValue(tokenName, out var i) ? i : new List<DiagnosticIssue>();
            var assigned = isAssigned ? assignedTokens[tokenName] : default;
            tokenIds.TryGetValue(tokenName, out var tokenId);

            result.Add(new TokenDiagnostic
            {
                TokenName = tokenName,
                TokenId = tokenId,
                Outcome = outcome,
                Attempts = tokenAttempts,
                AssignedValue = assigned.value,
                AssignedLocation = assigned.location,
                Issues = tokenIssues,
            });
        }

        // Handle global hint-missing issues: create a synthetic TokenDiagnostic for them
        if (globalIssues.Count > 0)
        {
            result.Add(new TokenDiagnostic
            {
                TokenName = "(global)",
                Outcome = TokenOutcome.NeverFound,
                Issues = globalIssues,
            });
        }

        // Build verdict
        var matchedCount = events.Count(e => e.Type == DiagnosticEventType.TokenAssigned);
        var missedCount = events.Count(e => e.Type == DiagnosticEventType.TokenMissed);
        var totalCount = matchedCount + missedCount;
        var verdict = BuildVerdict(matchedCount, totalCount, missedCount);

        return (result, verdict);
    }

    private static void AddAttempt(Dictionary<string, List<TokenAttempt>> attempts, string tokenName, TokenAttempt attempt)
    {
        if (!attempts.TryGetValue(tokenName, out var list))
        {
            list = new List<TokenAttempt>();
            attempts[tokenName] = list;
        }
        list.Add(attempt);
    }

    private static void AddIssue(Dictionary<string, List<DiagnosticIssue>> issues, DiagnosticEvent evt,
                                   DiagnosticIssueType type, string description, DiagnosticResult diagnostics)
    {
        var tokenName = evt.TokenName!;
        if (!issues.TryGetValue(tokenName, out var list))
        {
            list = new List<DiagnosticIssue>();
            issues[tokenName] = list;
        }
        list.Add(CreateIssue(type, evt, description, diagnostics));
    }

    private static DiagnosticIssue CreateIssue(DiagnosticIssueType type, DiagnosticEvent sourceEvent,
                                                string description, DiagnosticResult diagnostics)
    {
        var issue = new DiagnosticIssue
        {
            Type = type,
            TokenName = sourceEvent.TokenName,
            Description = description,
            Location = sourceEvent.Location,
        };
        return new DiagnosticIssue
        {
            Type = issue.Type,
            TokenName = issue.TokenName,
            Description = issue.Description,
            Location = issue.Location,
            Hint = GenerateHint(issue, sourceEvent, diagnostics),
        };
    }

    private static string? GenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                        DiagnosticResult diagnostics)
    {
        foreach (var generator in HintGenerators)
        {
            var hint = generator.TryGenerateHint(issue, sourceEvent, diagnostics);
            if (hint != null)
                return hint;
        }
        return null;
    }

    private static string BuildVerdict(int matched, int total, int missed)
    {
        if (missed == 0)
            return $"Matched {matched.ToInvariant()} of {total.ToInvariant()} tokens.";

        return $"Matched {matched.ToInvariant()} of {total.ToInvariant()} tokens ({missed.ToInvariant()} missed).";
    }

    private static string BuildTransformerDescription(DiagnosticEvent evt)
    {
        var sb = new StringBuilder();
        sb.Append("Transformer '").Append(evt.DecoratorName ?? "unknown").Append('\'');

        if (evt.DecoratorArgs != null && evt.DecoratorArgs.Length > 0)
#if NETSTANDARD2_0
            sb.Append('(').Append(string.Join(", ", evt.DecoratorArgs)).Append(')');
#else
            sb.Append('(').AppendJoin(", ", evt.DecoratorArgs).Append(')');
#endif

        sb.Append(" failed to transform value '").Append(evt.Value ?? "null").Append('\'');

        if (evt.TokenName != null)
            sb.Append(" for token '").Append(evt.TokenName).Append('\'');

        sb.Append('.');
        return sb.ToString();
    }

    private static string BuildValidatorDescription(DiagnosticEvent evt)
    {
        var sb = new StringBuilder();
        sb.Append("Validator '").Append(evt.DecoratorName ?? "unknown").Append('\'');
        sb.Append(" rejected value '").Append(evt.Value ?? "null").Append('\'');

        if (evt.TokenName != null)
            sb.Append(" for token '").Append(evt.TokenName).Append('\'');

        sb.Append('.');
        return sb.ToString();
    }

    private static string BuildRepeatingTokenDescription(DiagnosticEvent evt)
    {
        var sb = new StringBuilder();
        sb.Append("Repeating token '").Append(evt.TokenName ?? "unknown").Append("' was cut short");

        if (!string.IsNullOrEmpty(evt.Detail))
            sb.Append(": ").Append(evt.Detail);

        sb.Append('.');
        return sb.ToString();
    }
}
```

- [ ] **Step 4: Run builder tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenDiagnosticBuilderTests" -v n`

Expected: All tests pass. If any fail, adjust the builder logic to match the test expectations.

- [ ] **Step 5: Run full test suite to ensure no regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v n`

Expected: All tests pass (builder is additive — no existing code changed).

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs
git commit -m "Add TokenDiagnosticBuilder to aggregate events into per-token diagnostics"
```

---

### Task 3: Redesign DiagnosticResult public API

**Files:**
- Modify: `src/Tokenizer/Diagnostics/DiagnosticResult.cs`
- Modify: `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`
- Modify: `src/Tokenizer/Tokenizer.cs` (logging code at lines 350-356)
- Delete: `src/Tokenizer/Diagnostics/DiagnosticSummary.cs`
- Delete: `src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs`
- Modify: All test files that reference old API (see list below)

**Interfaces:**
- Consumes: `TokenDiagnosticBuilder.Build()`, `TokenDiagnostic`, `TokenOutcome`
- Produces: Redesigned `DiagnosticResult` with `.Tokens`, `.Verdict`, `.RawEvents`, `.RenderAlignment()`

This is the breaking change task. Every test referencing `.Summary`, `.Events`, `.Failures`, `.ForToken()`, or `.FirstFailure` on `DiagnosticResult` must be updated.

**Test files that need updating (found by grep):**

| File | What references old API |
|---|---|
| `DiagnosticResultTests.cs` | `.Failures`, `.ForToken()`, `.FirstFailure`, `.Events` |
| `DiagnosticCollectorTests.cs` | `.Events`, `.Failures`, `.ForToken()`, `.FirstFailure` |
| `DiagnosticSummaryBuilderTests.cs` | `.Summary` — **DELETE this file** |
| `DiagnosticLoggingTests.cs` | `.Summary.Verdict`, `.Summary.Issues` |
| `DiagnosticIntegrationTests.cs` | `.Events` |
| `CompilationDiagnosticsTests.cs` | `.Events` (on runtime result) |
| `TemplateOptionsCascadeTests.cs` | `.Events` |
| `SampleTests.cs` | `.Summary.Verdict`, `.Summary.Issues` |
| `AlignmentRendererTests.cs` | `.Summary` (indirectly through render) |
| 10 characterisation test files | `.Events`, `.Summary.Verdict`, `.Summary.Issues` |

- [ ] **Step 1: Redesign DiagnosticResult**

Replace the contents of `src/Tokenizer/Diagnostics/DiagnosticResult.cs`:

```csharp
namespace Tokens.Diagnostics;

/// <summary>
/// Contains all diagnostic information from a single tokenization call.
/// The primary API is <see cref="Tokens"/> which provides per-token narratives.
/// <see cref="RawEvents"/> retains the full event trace for power users.
/// </summary>
public sealed class DiagnosticResult
{
    private readonly List<DiagnosticEvent> _events;
    private readonly string? _inputContent;
    private IReadOnlyList<TokenDiagnostic>? _tokens;
    private string? _verdict;
    private string? _alignment;

    internal DiagnosticResult(string? inputContent)
    {
        _inputContent = inputContent;
        _events = new List<DiagnosticEvent>();
    }

    /// <summary>
    /// The input text that was tokenized. Used by hint generators for near-miss analysis.
    /// </summary>
    internal string? InputContent => _inputContent;

    /// <summary>
    /// Per-token diagnostic narratives — the primary diagnostic API.
    /// Each entry tells the complete story of one token: every consideration,
    /// every rejection, and the final outcome.
    /// </summary>
    public IReadOnlyList<TokenDiagnostic> Tokens
    {
        get
        {
            EnsureBuilt();
            return _tokens!;
        }
    }

    /// <summary>
    /// A human-readable verdict describing the overall outcome.
    /// E.g. "Matched 3 of 5 tokens (2 missed)."
    /// </summary>
    public string Verdict
    {
        get
        {
            EnsureBuilt();
            return _verdict!;
        }
    }

    /// <summary>
    /// All events recorded during this tokenization call, in the order they occurred.
    /// This is the raw event trace for power users and engine debugging.
    /// For most use cases, prefer <see cref="Tokens"/> instead.
    /// </summary>
    public IReadOnlyList<DiagnosticEvent> RawEvents => _events;

    internal void AddEvent(DiagnosticEvent evt) => _events.Add(evt);

    /// <summary>
    /// Renders an alignment view showing how the template tokens mapped onto the input text.
    /// The result is cached after the first call.
    /// </summary>
    public string RenderAlignment()
    {
        _alignment ??= AlignmentRenderer.Render(this, _inputContent);
        return _alignment;
    }

    private void EnsureBuilt()
    {
        if (_tokens != null)
            return;

        var (tokens, verdict) = TokenDiagnosticBuilder.Build(this);
        _tokens = tokens;
        _verdict = verdict;
    }
}
```

- [ ] **Step 2: Update AlignmentRenderer to use the new API**

Replace the `Render` method in `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`. The renderer now consumes `diagnostics.Tokens` and `diagnostics.Verdict` instead of re-deriving from events:

```csharp
    public static string Render(DiagnosticResult diagnostics, string? inputContent)
    {
        var sb = new StringBuilder();
        var tokens = diagnostics.Tokens;

        var matchedTokens = tokens.Where(t => t.Outcome == TokenOutcome.Matched).ToList();
        var rejectedTokens = tokens.Where(t => t.Outcome == TokenOutcome.Rejected).ToList();
        var neverFoundTokens = tokens.Where(t => t.Outcome == TokenOutcome.NeverFound).ToList();

        var inputLineCount = CountLines(inputContent);
        var totalTokens = matchedTokens.Count + rejectedTokens.Count + neverFoundTokens.Count;

        // Header
        sb.AppendLine("═══ Tokenization Alignment ═══");
        sb.Append("Tokens: ").Append(totalTokens).Append(" | Input: ").Append(inputLineCount).Append(" lines | ").AppendLine(diagnostics.Verdict);

        // Matched tokens
        if (matchedTokens.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Matched Tokens ──");
            foreach (var token in matchedTokens)
            {
                var line = token.AssignedLocation != null ? $" (line {token.AssignedLocation.Line.ToInvariant()})" : string.Empty;
                sb.Append("  ✓ ").Append(token.TokenName).Append(" = \"").Append(token.AssignedValue).Append('"').AppendLine(line);
            }
        }

        // Failures (rejected tokens with attempts)
        if (rejectedTokens.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Failures ──");
            foreach (var token in rejectedTokens)
            {
                foreach (var attempt in token.Attempts)
                {
                    var decoratorDesc = !string.IsNullOrEmpty(attempt.DecoratorName) ? attempt.DecoratorName : "decorator";
                    sb.Append("  ✗ ").Append(token.TokenName).Append(": ").Append(attempt.Outcome).Append(" — ").Append(decoratorDesc).Append(" failed on '").Append(attempt.Value).AppendLine("'");
                }

                foreach (var issue in token.Issues)
                {
                    if (issue.Hint != null)
                        sb.Append("      Hint: ").AppendLine(issue.Hint);
                }
            }
        }

        // Unmatched tokens (never found)
        if (neverFoundTokens.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Unmatched Tokens ──");
            foreach (var token in neverFoundTokens)
            {
                sb.Append("  ✗ ").Append(token.TokenName).AppendLine(" — preamble never found");

                foreach (var issue in token.Issues)
                {
                    if (issue.Hint != null)
                        sb.Append("      Hint: ").AppendLine(issue.Hint);
                }
            }
        }

        // Summary
        sb.AppendLine();
        sb.AppendLine("═══ Summary ═══");
        sb.Append("  Matched: ").Append(matchedTokens.Count).Append(" | Missed: ").Append(rejectedTokens.Count + neverFoundTokens.Count).Append(" | Failures: ").Append(rejectedTokens.Sum(t => t.Attempts.Count));

        return sb.ToString();
    }
```

- [ ] **Step 3: Update Tokenizer.cs logging**

In `src/Tokenizer/Tokenizer.cs`, find the logging block (around line 350) and update to use the new API:

Change:
```csharp
            _log.LogDebug("{Verdict}", result.Diagnostics.Summary.Verdict);
            ...
            foreach (var issue in result.Diagnostics.Summary.Issues)
```

To:
```csharp
            _log.LogDebug("{Verdict}", result.Diagnostics.Verdict);
            ...
            foreach (var token in result.Diagnostics.Tokens)
            {
                foreach (var issue in token.Issues)
```

(Close both foreach loops with `}` appropriately.)

- [ ] **Step 4: Delete DiagnosticSummary.cs and DiagnosticSummaryBuilder.cs**

```bash
rm src/Tokenizer/Diagnostics/DiagnosticSummary.cs
rm src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs
```

- [ ] **Step 5: Delete DiagnosticSummaryBuilderTests.cs**

```bash
rm tests/Tokenizer.Tests/Diagnostics/DiagnosticSummaryBuilderTests.cs
```

- [ ] **Step 6: Update DiagnosticResultTests.cs**

The old tests reference `.Failures`, `.ForToken()`, `.FirstFailure`, `.Events`. Replace the file to test the new API:

```csharp
using Xunit;

namespace Tokens.Diagnostics;

public class DiagnosticResultTests
{
    [Fact]
    public void GivenMatchedAndMissedTokens_WhenTokensAccessed_ThenPerTokenDiagnosticsAvailable()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Name", Value = "John" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Age" });

        // Act
        var tokens = result.Tokens;

        // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenOutcome.Matched, tokens[0].Outcome);
        Assert.Equal(TokenOutcome.NeverFound, tokens[1].Outcome);
    }

    [Fact]
    public void GivenEvents_WhenRawEventsAccessed_ThenAllEventsAvailable()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Name" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Age" });

        // Act & Assert
        Assert.Equal(2, result.RawEvents.Count);
    }

    [Fact]
    public void GivenEmptyResult_WhenQueried_ThenReturnsEmptyCollections()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);

        // Act & Assert
        Assert.Empty(result.RawEvents);
        Assert.Empty(result.Tokens);
        Assert.Equal("Matched 0 of 0 tokens.", result.Verdict);
    }

    [Fact]
    public void GivenResult_WhenVerdictAccessed_ThenReturnsVerdictString()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "First" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Second" });

        // Assert
        Assert.Equal("Matched 1 of 2 tokens (1 missed).", result.Verdict);
    }
}
```

- [ ] **Step 7: Update DiagnosticCollectorTests.cs**

Replace references to `.Events` with `.RawEvents`, and remove tests for `.Failures`, `.ForToken()`, `.FirstFailure` (those helpers are gone — the new API uses `.Tokens` instead). The implementer should read the current file and update all `result.Events` → `result.RawEvents`, and remove or rewrite tests that reference removed members.

- [ ] **Step 8: Update DiagnosticLoggingTests.cs**

Change `.Summary.Verdict` → `.Verdict` and `.Summary.Issues` → token-based issue access. The implementer should read the current file and update accordingly.

- [ ] **Step 9: Update DiagnosticIntegrationTests.cs**

Change `.Events` → `.RawEvents`. The implementer should read the current file and update all references.

- [ ] **Step 10: Update TemplateOptionsCascadeTests.cs**

Change `.Events` → `.RawEvents`. The implementer should read the current file and update.

- [ ] **Step 11: Update SampleTests.cs**

Change `.Summary.Verdict` → `.Verdict` and `.Summary.Issues` → token-based access. The implementer should read the current file and update the diagnostic logging blocks.

- [ ] **Step 12: Update AlignmentRendererTests.cs**

The renderer now consumes `.Tokens` internally, so its tests should still work since they call `.RenderAlignment()`. But if any test references `.Summary` directly, update it. The implementer should verify these tests compile and pass.

- [ ] **Step 13: Update all 10 characterisation test fixtures**

Every characterisation test file in `tests/Tokenizer.Tests/Diagnostics/Characterisation/` needs updating:
- `.Events` → `.RawEvents`
- `.Summary.Verdict` → `.Verdict`
- `.Summary.Issues` → token-based issue access via `.Tokens`
- Assertions on `DiagnosticIssueType` should now be found via `diagnostics.Tokens.SelectMany(t => t.Issues)` or per-token

The implementer should go through each of the 10 files and update all references. The test names and logic should stay the same — only the API access paths change.

- [ ] **Step 14: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v n`

Expected: All tests pass. Fix any remaining compilation errors.

- [ ] **Step 15: Commit**

```bash
git add -A
git commit -m "Redesign DiagnosticResult: token-centric model with Tokens, Verdict, RawEvents

Replace Summary/Events/Failures/ForToken/FirstFailure with token-centric
API. DiagnosticResult.Tokens provides per-token narratives. RawEvents
retains the flat trace. DiagnosticSummary and DiagnosticSummaryBuilder
removed. All tests updated to new API."
```
