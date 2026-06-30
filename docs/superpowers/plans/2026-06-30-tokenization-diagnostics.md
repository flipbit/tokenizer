# Tokenization Diagnostics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a structured diagnostic tracing system to the Tokenizer library that records matching decisions, generates mismatch summaries with adaptive hints, and renders template-input alignment diffs.

**Architecture:** An `IDiagnosticCollector` interface with null/active implementations is threaded through the tokenization pipeline. Events are recorded at each decision point and stored in a `TokenizationDiagnostics` object on the result. Summary reports and alignment diffs are generated lazily from the event trace. Five `IHintGenerator` implementations provide adaptive "did you mean?" suggestions.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 6.0 (dual-target), xUnit, Serilog + Serilog.Sinks.XUnit

**Test commands:**
```bash
export PATH="$HOME/.dotnet:$PATH"
# Build
dotnet build src/Tokenizer/Tokenizer.csproj -c Release
# Run all tests
dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj
# Run a single test
dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName"
```

**Code conventions:**
- Allman brace style
- Gherkin test naming: `GivenScenario_WhenAction_ThenResult()`
- Arrange / Act / Assert comments in tests
- Fluent builders in `tests/Tokenizer.Tests/Builders/`
- Root namespace is `Tokens`, not `Tokenizer`
- No `#region`, no async, no emojis in code
- XMLDoc comments on all public types and members

---

### Task 1: Cleanup — Remove Legacy Logging Flags and LineTracker

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- Modify: `tests/Tokenizer.Tests/TokenizerOptionsTests.cs`
- Delete: `src/Tokenizer/Tokenization/LineTracker.cs`

- [ ] **Step 1: Remove `EnableLogging` and `EnableLineByLineLogging` from `TokenizerOptions`**

In `src/Tokenizer/TokenizerOptions.cs`, remove these properties and their assignments in the constructor and `Clone()`:

```csharp
// REMOVE from constructor:
EnableLogging = false;
EnableLineByLineLogging = true;

// REMOVE properties:
public bool EnableLogging { get; set; }
public bool EnableLineByLineLogging { get; set; }

// REMOVE from Clone():
EnableLogging = EnableLogging,
EnableLineByLineLogging = EnableLineByLineLogging,
```

- [ ] **Step 2: Remove LineTracker usage from TokenizationEngine**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`, remove:
- The `LineTracker? lineTracker` local variable creation (~line 94-96)
- All `lineTracker?.RecordMatch(...)` calls (~lines 196-199, 647-651)
- The `lineTracker?.Finalize(...)` call (~line 211)
- The `lineTracker` parameter from `HandleTokenSwitch` method signature and body

- [ ] **Step 3: Delete LineTracker.cs**

Delete file: `src/Tokenizer/Tokenization/LineTracker.cs`

- [ ] **Step 4: Fix any tests referencing removed properties**

Search `tests/` for references to `EnableLogging` or `EnableLineByLineLogging`. Update or remove any tests that set these properties.

- [ ] **Step 5: Build and run all tests**

Run: `dotnet build src/Tokenizer/Tokenizer.csproj -c Release && dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass. No compilation errors.

- [ ] **Step 6: Commit**

```bash
git add -A && git status
git commit -m "Remove dead EnableLogging flag, EnableLineByLineLogging, and LineTracker

These are superseded by the new diagnostics system. EnableLogging was
never checked in engine code. LineTracker's per-line summaries are
replaced by the diagnostic alignment diff."
```

---

### Task 2: Data Model — DiagnosticEventType, DiagnosticEvent, DiagnosticIssueType, DiagnosticIssue

**Files:**
- Create: `src/Tokenizer/Diagnostics/DiagnosticEventType.cs`
- Create: `src/Tokenizer/Diagnostics/DiagnosticEvent.cs`
- Create: `src/Tokenizer/Diagnostics/DiagnosticIssueType.cs`
- Create: `src/Tokenizer/Diagnostics/DiagnosticIssue.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticEventTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/Tokenizer.Tests/Diagnostics/DiagnosticEventTests.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tests.Diagnostics;

public class DiagnosticEventTests
{
    [Fact]
    public void GivenDiagnosticEvent_WhenCreated_ThenPropertiesAreSet()
    {
        // Arrange & Act
        var evt = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenAssigned,
            TokenName = "DomainName",
            TokenId = 1,
            Location = new FileLocation(),
            Value = "bbc.co.uk",
            Detail = "Assigned successfully",
            DecoratorName = null,
            DecoratorArgs = null
        };

        // Assert
        Assert.Equal(DiagnosticEventType.TokenAssigned, evt.Type);
        Assert.Equal("DomainName", evt.TokenName);
        Assert.Equal(1, evt.TokenId);
        Assert.NotNull(evt.Location);
        Assert.Equal("bbc.co.uk", evt.Value);
        Assert.Equal("Assigned successfully", evt.Detail);
        Assert.Null(evt.DecoratorName);
        Assert.Null(evt.DecoratorArgs);
    }

    [Fact]
    public void GivenDiagnosticEvent_WhenCreatedWithDecoratorInfo_ThenDecoratorPropertiesAreSet()
    {
        // Arrange & Act
        var evt = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Registered",
            TokenId = 5,
            Value = "21/11/2005",
            DecoratorName = "ToDateTimeUtc",
            DecoratorArgs = new[] { "yyyy-MM-dd" }
        };

        // Assert
        Assert.Equal(DiagnosticEventType.TransformerFailed, evt.Type);
        Assert.Equal("ToDateTimeUtc", evt.DecoratorName);
        Assert.Single(evt.DecoratorArgs);
        Assert.Equal("yyyy-MM-dd", evt.DecoratorArgs[0]);
    }

    [Fact]
    public void GivenDiagnosticIssue_WhenCreated_ThenPropertiesAreSet()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Registered",
            Description = "ToDateTimeUtc('yyyy-MM-dd') failed on '21/11/2005'",
            Location = new FileLocation(),
            Hint = "Value matches format 'dd/MM/yyyy'"
        };

        // Assert
        Assert.Equal(DiagnosticIssueType.TransformerFailure, issue.Type);
        Assert.Equal("Registered", issue.TokenName);
        Assert.NotNull(issue.Hint);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticEventTests"`
Expected: FAIL — types don't exist yet.

- [ ] **Step 3: Create DiagnosticEventType enum**

Create `src/Tokenizer/Diagnostics/DiagnosticEventType.cs`:

```csharp
namespace Tokens.Diagnostics
{
    /// <summary>
    /// Identifies the type of decision or event recorded during tokenization.
    /// Every event type has a corresponding diagnostic meaning that AI agents
    /// and developers can use to understand the tokenization process.
    /// </summary>
    public enum DiagnosticEventType
    {
        /// <summary>
        /// Tokenization has started for a template/input pair.
        /// Detail contains template name, token count, and input length.
        /// </summary>
        TokenizationStarted,

        /// <summary>
        /// Tokenization has completed for a template/input pair.
        /// Detail contains final match count, miss count, and success status.
        /// </summary>
        TokenizationCompleted,

        /// <summary>
        /// A required hint string was found in the input text.
        /// Value contains the hint string that was matched.
        /// </summary>
        HintMatched,

        /// <summary>
        /// A required hint string was not found in the input text.
        /// Tokenization will be skipped. Value contains the missing hint string.
        /// </summary>
        HintMissing,

        /// <summary>
        /// The engine began searching for a token's preamble string in the input.
        /// TokenName identifies the token(s) being searched for.
        /// Location is the current position in the input.
        /// </summary>
        PreambleSearchStarted,

        /// <summary>
        /// A token's preamble string was found at the current input position.
        /// TokenName and Location identify what matched and where.
        /// </summary>
        PreambleMatched,

        /// <summary>
        /// No token preamble matched at the current input position.
        /// The engine will consume one character and advance.
        /// Location is the position where matching was attempted.
        /// </summary>
        PreambleNotFound,

        /// <summary>
        /// A value has been accumulated for the current candidate token(s).
        /// Value contains the accumulated string. Emitted when the value
        /// is about to be used (before assignment), not per-character.
        /// </summary>
        ValueAccumulated,

        /// <summary>
        /// The engine is attempting to assign an accumulated value to one
        /// or more candidate tokens. Value contains the string being tested.
        /// TokenName lists the candidate token names.
        /// </summary>
        TokenAssignmentAttempted,

        /// <summary>
        /// A token's validator decorator accepted the value.
        /// DecoratorName identifies the validator (e.g. "IsEmail").
        /// Value contains the input that was validated.
        /// </summary>
        ValidatorPassed,

        /// <summary>
        /// A token's validator decorator rejected the value.
        /// DecoratorName identifies the validator. Value contains the rejected input.
        /// This causes the token assignment to fail.
        /// </summary>
        ValidatorFailed,

        /// <summary>
        /// A token's transformer decorator successfully transformed the value.
        /// DecoratorName identifies the transformer (e.g. "ToDateTimeUtc").
        /// DecoratorArgs contains the transformer parameters (e.g. ["yyyy-MM-dd"]).
        /// Value contains the input before transformation.
        /// Detail contains the output after transformation.
        /// </summary>
        TransformerSucceeded,

        /// <summary>
        /// A token's transformer decorator failed to transform the value.
        /// DecoratorName identifies the transformer. DecoratorArgs contains parameters.
        /// Value contains the input that could not be transformed.
        /// This causes the token assignment to fail.
        /// </summary>
        TransformerFailed,

        /// <summary>
        /// A token was successfully assigned a value from the input.
        /// TokenName is the assigned token. Value is the final assigned value
        /// (after all transformations). Location is where it was found in the input.
        /// </summary>
        TokenAssigned,

        /// <summary>
        /// None of the candidate tokens could accept the accumulated value.
        /// All validators/transformers in the candidate list rejected it.
        /// Value contains the rejected string. TokenName lists the candidates.
        /// </summary>
        TokenAssignmentFailed,

        /// <summary>
        /// A newline-terminated token's value was processed at a newline boundary.
        /// TokenName identifies the token. Value contains the extracted value.
        /// </summary>
        NewlineTerminatedTokenProcessed,

        /// <summary>
        /// The engine is backtracking because no candidate tokens can accept
        /// the current accumulated value. The engine will advance past the
        /// preamble and retry matching. Location is the backtrack position.
        /// </summary>
        BacktrackStarted,

        /// <summary>
        /// A repeating token has been disabled and will no longer match.
        /// This occurs when a repeating token was the last match but failed
        /// to match the next repetition, or when a line gap was detected.
        /// TokenName identifies the disabled token.
        /// </summary>
        RepeatingTokenDisabled,

        /// <summary>
        /// A ConsiderOnce token failed to match and has been permanently
        /// removed from the candidate list and recorded as a miss.
        /// TokenName identifies the removed token.
        /// </summary>
        ConsiderOnceTokenRemoved,

        /// <summary>
        /// A front matter token was successfully assigned its value.
        /// TokenName is the token name. Value is the assigned value.
        /// </summary>
        FrontMatterTokenAssigned,

        /// <summary>
        /// A front matter token failed to assign its value.
        /// TokenName is the token name.
        /// </summary>
        FrontMatterTokenFailed,

        /// <summary>
        /// A required or optional token was never matched during tokenization.
        /// Emitted during the post-tokenization summary phase.
        /// TokenName identifies the unmatched token.
        /// </summary>
        TokenMissed,
    }
}
```

- [ ] **Step 4: Create DiagnosticEvent class**

Create `src/Tokenizer/Diagnostics/DiagnosticEvent.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics
{
    /// <summary>
    /// A single diagnostic event recorded during tokenization, representing
    /// one decision point in the matching process.
    /// </summary>
    public class DiagnosticEvent
    {
        /// <summary>
        /// The type of decision or event. See <see cref="DiagnosticEventType"/>
        /// for detailed documentation of each type's semantics.
        /// </summary>
        public DiagnosticEventType Type { get; init; }

        /// <summary>
        /// The name of the token this event relates to, or null for
        /// events not specific to a single token.
        /// </summary>
        public string? TokenName { get; init; }

        /// <summary>
        /// The unique ID of the token within its template, or null
        /// for events not specific to a single token.
        /// </summary>
        public int? TokenId { get; init; }

        /// <summary>
        /// The position in the input text where this event occurred.
        /// </summary>
        public FileLocation? Location { get; init; }

        /// <summary>
        /// The value being tested, assigned, or accumulated.
        /// Meaning varies by event type — see <see cref="DiagnosticEventType"/> docs.
        /// </summary>
        public string? Value { get; init; }

        /// <summary>
        /// Human-readable explanation providing additional context.
        /// For TransformerSucceeded, contains the transformed output value.
        /// </summary>
        public string? Detail { get; init; }

        /// <summary>
        /// The name of the decorator (validator or transformer) involved,
        /// or null for non-decorator events. E.g. "ToDateTimeUtc", "IsEmail".
        /// </summary>
        public string? DecoratorName { get; init; }

        /// <summary>
        /// The parameters passed to the decorator, or null.
        /// E.g. ["yyyy-MM-dd HH:mm:ss"] for ToDateTimeUtc.
        /// </summary>
        public string[]? DecoratorArgs { get; init; }
    }
}
```

- [ ] **Step 5: Create DiagnosticIssueType enum**

Create `src/Tokenizer/Diagnostics/DiagnosticIssueType.cs`:

```csharp
namespace Tokens.Diagnostics
{
    /// <summary>
    /// Categories of issues that can be identified during tokenization diagnostics.
    /// Used for programmatic filtering and classification of diagnostic issues.
    /// </summary>
    public enum DiagnosticIssueType
    {
        /// <summary>
        /// A required token's preamble was never found in the input.
        /// The template expected to find a specific string but it was absent.
        /// </summary>
        PreambleNeverFound,

        /// <summary>
        /// A token's preamble was found but the extracted value failed validation.
        /// A validator decorator rejected the accumulated value.
        /// </summary>
        ValidatorRejection,

        /// <summary>
        /// A token's preamble was found but a transformer failed on the extracted value.
        /// A transformer decorator could not convert the accumulated value.
        /// </summary>
        TransformerFailure,

        /// <summary>
        /// A token was matched but assigned an unexpected or empty value,
        /// suggesting the template consumed too much or too little input.
        /// </summary>
        ValueMismatch,

        /// <summary>
        /// A repeating token was disabled prematurely due to a line gap or
        /// failed repetition, resulting in fewer matches than expected.
        /// </summary>
        RepeatingTokenCutShort,

        /// <summary>
        /// Input text exists that doesn't correspond to any token in the template,
        /// which may have pushed subsequent tokens out of alignment.
        /// </summary>
        UnmatchedInputSection,

        /// <summary>
        /// A required hint string was not found in the input text,
        /// causing tokenization to be skipped entirely.
        /// </summary>
        HintMissing,
    }
}
```

- [ ] **Step 6: Create DiagnosticIssue class**

Create `src/Tokenizer/Diagnostics/DiagnosticIssue.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics
{
    /// <summary>
    /// A single issue identified during tokenization, with an optional
    /// adaptive hint suggesting how to fix it.
    /// </summary>
    public class DiagnosticIssue
    {
        /// <summary>
        /// Category of the issue for programmatic filtering.
        /// </summary>
        public DiagnosticIssueType Type { get; init; }

        /// <summary>
        /// The token that failed, if applicable.
        /// </summary>
        public string? TokenName { get; init; }

        /// <summary>
        /// Human-readable explanation of what went wrong.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Location in the input where the issue occurred.
        /// </summary>
        public FileLocation? Location { get; init; }

        /// <summary>
        /// Adaptive hint suggesting how to fix the issue, if available.
        /// Null when no hint can be generated.
        /// </summary>
        public string? Hint { get; init; }
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticEventTests"`
Expected: All 3 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/Tokenizer/Diagnostics/ tests/Tokenizer.Tests/Diagnostics/
git commit -m "Add diagnostic data model types

DiagnosticEventType enum, DiagnosticEvent, DiagnosticIssueType enum,
and DiagnosticIssue with full XMLDoc comments for intellisense and
AI agent consumption."
```

---

### Task 3: Collector Infrastructure — IDiagnosticCollector, NullDiagnosticCollector, DiagnosticCollector, TokenizationDiagnostics

**Files:**
- Create: `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`
- Create: `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs`
- Create: `src/Tokenizer/Diagnostics/DiagnosticCollector.cs`
- Create: `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs`:

```csharp
using System.Linq;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tests.Diagnostics;

public class DiagnosticCollectorTests
{
    [Fact]
    public void GivenNullCollector_WhenRecordingEvent_ThenGetResultReturnsNull()
    {
        // Arrange
        var collector = NullDiagnosticCollector.Instance;

        // Act
        collector.Record(DiagnosticEventType.TokenizationStarted, value: "test");
        var result = collector.GetResult();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GivenActiveCollector_WhenRecordingEvent_ThenEventIsStored()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");

        // Act
        collector.Record(DiagnosticEventType.TokenAssigned,
            tokenName: "DomainName", tokenId: 1,
            location: new FileLocation(), value: "bbc.co.uk");
        var result = collector.GetResult();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.Events);
        Assert.Equal(DiagnosticEventType.TokenAssigned, result.Events[0].Type);
        Assert.Equal("DomainName", result.Events[0].TokenName);
        Assert.Equal("bbc.co.uk", result.Events[0].Value);
    }

    [Fact]
    public void GivenActiveCollector_WhenRecordingMultipleEvents_ThenEventsAreInOrder()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");

        // Act
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var result = collector.GetResult();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result!.Events.Count);
        Assert.Equal(DiagnosticEventType.TokenizationStarted, result.Events[0].Type);
        Assert.Equal(DiagnosticEventType.TokenizationCompleted, result.Events[3].Type);
    }

    [Fact]
    public void GivenDiagnostics_WhenQueryingFailures_ThenReturnsOnlyFailureEvents()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: "Second",
            decoratorName: "IsEmail", value: "notanemail");
        collector.Record(DiagnosticEventType.TransformerFailed, tokenName: "Third",
            decoratorName: "ToDateTimeUtc", value: "bad-date");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Fourth");

        // Act
        var result = collector.GetResult()!;

        // Assert
        var failures = result.Failures.ToList();
        Assert.Equal(3, failures.Count);
        Assert.All(failures, f => Assert.Contains(f.Type, new[]
        {
            DiagnosticEventType.ValidatorFailed,
            DiagnosticEventType.TransformerFailed,
            DiagnosticEventType.TokenMissed
        }));
    }

    [Fact]
    public void GivenDiagnostics_WhenQueryingForToken_ThenReturnsEventsForThatToken()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Second");

        // Act
        var result = collector.GetResult()!;
        var firstEvents = result.ForToken("First").ToList();

        // Assert
        Assert.Equal(2, firstEvents.Count);
        Assert.All(firstEvents, e => Assert.Equal("First", e.TokenName));
    }

    [Fact]
    public void GivenDiagnostics_WhenQueryingFirstFailure_ThenReturnsFirstFailureEvent()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: "Second");
        collector.Record(DiagnosticEventType.TransformerFailed, tokenName: "Third");

        // Act
        var result = collector.GetResult()!;

        // Assert
        Assert.NotNull(result.FirstFailure);
        Assert.Equal("Second", result.FirstFailure!.TokenName);
        Assert.Equal(DiagnosticEventType.ValidatorFailed, result.FirstFailure.Type);
    }

    [Fact]
    public void GivenDiagnosticsWithNoFailures_WhenQueryingFirstFailure_ThenReturnsNull()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");

        // Act
        var result = collector.GetResult()!;

        // Assert
        Assert.Null(result.FirstFailure);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticCollectorTests"`
Expected: FAIL — types don't exist yet.

- [ ] **Step 3: Create IDiagnosticCollector interface**

Create `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`:

```csharp
namespace Tokens.Diagnostics
{
    /// <summary>
    /// Collects diagnostic events during tokenization.
    /// Implementations must be safe for single-threaded use within one tokenization call.
    /// Created per-tokenization-call in Tokenizer.Tokenize(), passed to the engine
    /// and down into Token.Assign() as a method parameter.
    /// </summary>
    internal interface IDiagnosticCollector
    {
        /// <summary>
        /// Records a diagnostic event. Implementations may discard the event
        /// (NullDiagnosticCollector) or store it (DiagnosticCollector).
        /// </summary>
        void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                    Enumerators.FileLocation? location = null, string? value = null, string? detail = null,
                    string? decoratorName = null, string[]? decoratorArgs = null);

        /// <summary>
        /// Returns the collected diagnostics, or null if collection is disabled.
        /// </summary>
        TokenizationDiagnostics? GetResult();
    }
}
```

- [ ] **Step 4: Create NullDiagnosticCollector**

Create `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics
{
    /// <summary>
    /// No-op collector used when diagnostics are disabled.
    /// All methods are no-ops. The JIT can inline these away entirely.
    /// </summary>
    internal sealed class NullDiagnosticCollector : IDiagnosticCollector
    {
        public static readonly NullDiagnosticCollector Instance = new();

        public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                           FileLocation? location = null, string? value = null, string? detail = null,
                           string? decoratorName = null, string[]? decoratorArgs = null)
        {
        }

        public TokenizationDiagnostics? GetResult() => null;
    }
}
```

- [ ] **Step 5: Create TokenizationDiagnostics**

Create `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace Tokens.Diagnostics
{
    /// <summary>
    /// Complete diagnostic output from a tokenization run. Contains the raw
    /// event trace, a lazily-generated mismatch summary, and a visual alignment renderer.
    /// Attached to <see cref="TokenizeResultBase.Diagnostics"/> when
    /// <see cref="TokenizerOptions.EnableDiagnostics"/> is true.
    /// </summary>
    public class TokenizationDiagnostics
    {
        private static readonly DiagnosticEventType[] FailureTypes =
        {
            DiagnosticEventType.ValidatorFailed,
            DiagnosticEventType.TransformerFailed,
            DiagnosticEventType.TokenAssignmentFailed,
            DiagnosticEventType.TokenMissed,
            DiagnosticEventType.HintMissing,
            DiagnosticEventType.BacktrackStarted,
            DiagnosticEventType.RepeatingTokenDisabled,
            DiagnosticEventType.ConsiderOnceTokenRemoved,
        };

        private readonly string templateContent;
        private readonly string inputContent;
        private DiagnosticSummary? summary;
        private string? alignment;

        internal TokenizationDiagnostics(string templateContent, string inputContent)
        {
            this.templateContent = templateContent;
            this.inputContent = inputContent;
            Events = new List<DiagnosticEvent>();
        }

        /// <summary>
        /// The ordered list of diagnostic events recorded during tokenization.
        /// Events are in chronological order.
        /// </summary>
        public List<DiagnosticEvent> Events { get; }

        /// <summary>
        /// Concise summary of issues found during tokenization.
        /// Generated lazily on first access from the event trace.
        /// </summary>
        public DiagnosticSummary Summary
        {
            get
            {
                summary ??= DiagnosticSummaryBuilder.Build(this);
                return summary;
            }
        }

        /// <summary>
        /// All events where matching failed (validators, transformers, assignments, misses).
        /// Convenience filter over Events.
        /// </summary>
        public IEnumerable<DiagnosticEvent> Failures =>
            Events.Where(e => FailureTypes.Contains(e.Type));

        /// <summary>
        /// All events related to a specific token name.
        /// </summary>
        public IEnumerable<DiagnosticEvent> ForToken(string name) =>
            Events.Where(e => e.TokenName == name);

        /// <summary>
        /// The first event that represents a failure, or null if tokenization succeeded.
        /// Useful for quickly identifying the root cause.
        /// </summary>
        public DiagnosticEvent? FirstFailure =>
            Events.FirstOrDefault(e => FailureTypes.Contains(e.Type));

        /// <summary>
        /// Renders a visual alignment diff between template and input showing
        /// which tokens matched, where values were extracted, and where alignment
        /// broke down. Computed lazily on first access.
        /// </summary>
        public string RenderAlignment()
        {
            alignment ??= AlignmentRenderer.Render(this, templateContent, inputContent);
            return alignment;
        }
    }
}
```

Note: `DiagnosticSummaryBuilder` and `AlignmentRenderer` don't exist yet. Create stub classes so this compiles:

Create `src/Tokenizer/Diagnostics/DiagnosticSummary.cs`:

```csharp
using System.Collections.Generic;

namespace Tokens.Diagnostics
{
    /// <summary>
    /// A concise, human-readable summary of why tokenization failed or partially matched.
    /// Generated from the full diagnostic trace. Designed to be readable by both
    /// developers and AI agents.
    /// </summary>
    public class DiagnosticSummary
    {
        /// <summary>
        /// One-line verdict. E.g. "Matched 6 of 11 tokens. First failure at line 14."
        /// </summary>
        public string Verdict { get; init; } = string.Empty;

        /// <summary>
        /// Ordered list of issues found, most significant first.
        /// Each issue is a self-contained explanation of one failure point.
        /// </summary>
        public IReadOnlyList<DiagnosticIssue> Issues { get; init; } = new List<DiagnosticIssue>();
    }
}
```

Create `src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs` (stub):

```csharp
namespace Tokens.Diagnostics
{
    /// <summary>
    /// Builds a <see cref="DiagnosticSummary"/> from a <see cref="TokenizationDiagnostics"/> trace.
    /// </summary>
    internal static class DiagnosticSummaryBuilder
    {
        public static DiagnosticSummary Build(TokenizationDiagnostics diagnostics)
        {
            // Stub — implemented in Task 5
            return new DiagnosticSummary { Verdict = "Diagnostics collected." };
        }
    }
}
```

Create `src/Tokenizer/Diagnostics/AlignmentRenderer.cs` (stub):

```csharp
namespace Tokens.Diagnostics
{
    /// <summary>
    /// Renders a visual alignment between template and input showing which
    /// tokens matched, where values were extracted, and where alignment broke down.
    /// </summary>
    internal static class AlignmentRenderer
    {
        public static string Render(TokenizationDiagnostics diagnostics, string templateContent, string inputContent)
        {
            // Stub — implemented in Task 8
            return "Alignment rendering not yet implemented.";
        }
    }
}
```

- [ ] **Step 6: Create DiagnosticCollector**

Create `src/Tokenizer/Diagnostics/DiagnosticCollector.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics
{
    /// <summary>
    /// Active collector that records all events into a <see cref="TokenizationDiagnostics"/> instance.
    /// Created once per tokenization call when diagnostics are enabled.
    /// </summary>
    internal sealed class DiagnosticCollector : IDiagnosticCollector
    {
        private readonly TokenizationDiagnostics diagnostics;

        public DiagnosticCollector(string templateContent, string inputContent)
        {
            diagnostics = new TokenizationDiagnostics(templateContent, inputContent);
        }

        public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                           FileLocation? location = null, string? value = null, string? detail = null,
                           string? decoratorName = null, string[]? decoratorArgs = null)
        {
            diagnostics.Events.Add(new DiagnosticEvent
            {
                Type = type,
                TokenName = tokenName,
                TokenId = tokenId,
                Location = location?.Clone(),
                Value = value,
                Detail = detail,
                DecoratorName = decoratorName,
                DecoratorArgs = decoratorArgs
            });
        }

        public TokenizationDiagnostics? GetResult() => diagnostics;
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticCollectorTests"`
Expected: All 7 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/Tokenizer/Diagnostics/ tests/Tokenizer.Tests/Diagnostics/
git commit -m "Add diagnostic collector infrastructure

IDiagnosticCollector interface with NullDiagnosticCollector (no-op) and
DiagnosticCollector (active). TokenizationDiagnostics container with
Failures, ForToken, FirstFailure queries. Summary and alignment are
stubbed for later tasks."
```

---

### Task 4: Wire Collector Into Pipeline — TokenizerOptions, TokenizeResultBase, Tokenizer, Engine, Token, CandidateTokenList, HintProcessor, ResultBuilder

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Modify: `src/Tokenizer/TokenizeResultBase.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- Modify: `src/Tokenizer/Tokenization/ITokenizationEngine.cs`
- Modify: `src/Tokenizer/Token.cs`
- Modify: `src/Tokenizer/CandidateTokenList.cs`
- Modify: `src/Tokenizer/Tokenization/HintProcessor.cs`
- Modify: `src/Tokenizer/Tokenization/IHintProcessor.cs`
- Modify: `src/Tokenizer/Tokenization/ResultBuilder.cs`
- Modify: `src/Tokenizer/Tokenization/IResultBuilder.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs`

This is the largest task. It threads the collector through every decision point.

- [ ] **Step 1: Write the failing integration test**

Create `tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs`:

```csharp
using System.Linq;
using Tokens.Diagnostics;
using Tokens.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tests.Diagnostics;

public class DiagnosticIntegrationTests : TokenizerTestBase
{
    public DiagnosticIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenizingSimpleMatch_ThenDiagnosticsArePopulated()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics!.Events.Count > 0);
        Assert.Contains(result.Diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenizationStarted);
        Assert.Contains(result.Diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenizationCompleted);
        Assert.Contains(result.Diagnostics.Events,
            e => e.Type == DiagnosticEventType.TokenAssigned && e.TokenName == "Name");
    }

    [Fact]
    public void GivenDiagnosticsDisabled_WhenTokenizing_ThenDiagnosticsAreNull()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.Null(result.Diagnostics);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenValidatorFails_ThenValidatorFailedEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Email: { Email : IsEmail }";
        var input = "Email: notanemail";

        // Act
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Events,
            e => e.Type == DiagnosticEventType.ValidatorFailed
              && e.DecoratorName == "IsEmailValidator");
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTransformerSucceeds_ThenTransformerEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name : ToUpper }";
        var input = "Name: john";

        // Act
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Events,
            e => e.Type == DiagnosticEventType.TransformerSucceeded
              && e.DecoratorName == "ToUpperTransformer");
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenMissed_ThenTokenMissedEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }\nAge: { Age }";
        var input = "Name: John";

        // Act
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Events,
            e => e.Type == DiagnosticEventType.TokenMissed && e.TokenName == "Age");
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenPreambleMatches_ThenPreambleMatchedEventRecorded()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var result = tokenizer.Tokenize(template, input);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Events,
            e => e.Type == DiagnosticEventType.PreambleMatched);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticIntegrationTests"`
Expected: FAIL — `EnableDiagnostics` property doesn't exist, `Diagnostics` property doesn't exist on result.

- [ ] **Step 3: Add `EnableDiagnostics` to TokenizerOptions**

In `src/Tokenizer/TokenizerOptions.cs`, add the property and update `Clone()`:

```csharp
/// <summary>
/// When true, tokenization results include a <see cref="Diagnostics.TokenizationDiagnostics"/>
/// property with a structured trace of every matching decision, a mismatch summary
/// with adaptive hints, and a visual alignment diff.
/// Default: false. Has no performance impact when disabled.
/// </summary>
public bool EnableDiagnostics { get; set; }
```

Add to constructor: `EnableDiagnostics = false;`
Add to `Clone()`: `EnableDiagnostics = EnableDiagnostics,`

- [ ] **Step 4: Add `Diagnostics` property to TokenizeResultBase**

In `src/Tokenizer/TokenizeResultBase.cs`, add:

```csharp
/// <summary>
/// Structured diagnostic output from the tokenization process.
/// Null when <see cref="TokenizerOptions.EnableDiagnostics"/> is false.
/// </summary>
public Diagnostics.TokenizationDiagnostics? Diagnostics { get; internal set; }
```

- [ ] **Step 5: Wire collector creation and result attachment in Tokenizer.cs**

In `src/Tokenizer/Tokenizer.cs`, modify the private `Tokenize()` method to:

1. Create the appropriate collector based on `template.Options.EnableDiagnostics`
2. Pass it to the engine, hint processor, and result builder
3. After tokenization, attach `collector.GetResult()` to the result
4. Log the diagnostic summary if diagnostics are enabled

The collector needs to be passed through all the method calls. Update the signatures of `ProcessTokenization`, `FindAndValidateHints`, and `BuildUnmatchedTokens` to accept an `IDiagnosticCollector` parameter.

- [ ] **Step 6: Add collector parameter to ITokenizationEngine and TokenizationEngine**

In `src/Tokenizer/Tokenization/ITokenizationEngine.cs`, add `Diagnostics.IDiagnosticCollector collector` parameter to `ProcessTokenization`.

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`:
- Add `IDiagnosticCollector collector` parameter to `ProcessTokenization`
- Record `TokenizationStarted` at the beginning
- Record `PreambleMatched` when `context.Enumerator.Match(...)` succeeds
- Record `TokenizationCompleted` at the end
- Pass collector through to `TryAssignCandidateTokens`, `HandleTokenSwitch`, etc.
- Record `TokenAssigned` / `TokenAssignmentFailed` in `TryAssignCandidateTokens`
- Record `BacktrackStarted`, `RepeatingTokenDisabled`, `ConsiderOnceTokenRemoved` in `ProcessRepeatedTokens`
- Record `NewlineTerminatedTokenProcessed` in `ProcessNewlineTerminatedTokens`
- Record `FrontMatterTokenAssigned` / `FrontMatterTokenFailed` in `ProcessFrontMatterTokens`

- [ ] **Step 7: Add collector parameter to Token.Assign() and CandidateTokenList.TryAssign()**

In `src/Tokenizer/Token.cs`, add `IDiagnosticCollector collector` parameter to `Assign()`:
- Record `TransformerSucceeded` / `TransformerFailed` in the decorator loop
- Record `ValidatorPassed` / `ValidatorFailed` in the decorator loop

In `src/Tokenizer/CandidateTokenList.cs`, add `IDiagnosticCollector collector` parameter to `TryAssign()` and pass it through to `token.Assign()`.

- [ ] **Step 8: Add collector parameter to HintProcessor**

In `src/Tokenizer/Tokenization/IHintProcessor.cs` and `HintProcessor.cs`, add `IDiagnosticCollector collector` parameter to `FindAndValidateHints`:
- Record `HintMatched` when a hint is found
- Record `HintMissing` when a required hint is missing

- [ ] **Step 9: Add collector parameter to ResultBuilder**

In `src/Tokenizer/Tokenization/IResultBuilder.cs` and `ResultBuilder.cs`, add `IDiagnosticCollector collector` parameter to `BuildUnmatchedTokens`:
- Record `TokenMissed` for each unmatched token

- [ ] **Step 10: Fix all compilation errors from signature changes**

Update all callers of the modified methods to pass the collector. This includes:
- `Tokenizer.Tokenize()` — creates and passes the collector
- All internal engine methods that call `TryAssignCandidateTokens`
- All internal engine methods that call `Token.Assign()` via `CandidateTokenList`

- [ ] **Step 11: Run all tests**

Run: `dotnet build src/Tokenizer/Tokenizer.csproj -c Release && dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass, including new `DiagnosticIntegrationTests`.

- [ ] **Step 12: Commit**

```bash
git add -A && git status
git commit -m "Wire diagnostic collector through tokenization pipeline

Thread IDiagnosticCollector through ProcessTokenization, Token.Assign,
CandidateTokenList.TryAssign, HintProcessor, and ResultBuilder.
Record events at every decision point. NullDiagnosticCollector used
when EnableDiagnostics is false for zero overhead."
```

---

### Task 5: DiagnosticSummaryBuilder — Replace Stub With Real Implementation

**Files:**
- Modify: `src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticSummaryBuilderTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Diagnostics/DiagnosticSummaryBuilderTests.cs`:

```csharp
using System.Linq;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tests.Diagnostics;

public class DiagnosticSummaryBuilderTests
{
    [Fact]
    public void GivenSuccessfulTokenization_WhenBuildingSummary_ThenVerdictReportsSuccess()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted,
            detail: "Template: test, Tokens: 2, Input length: 20");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Second");
        collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: "Matches: 2, Misses: 0");

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        Assert.Contains("2", summary.Verdict);
        Assert.Empty(summary.Issues);
    }

    [Fact]
    public void GivenTransformerFailure_WhenBuildingSummary_ThenIssueIsCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TransformerFailed,
            tokenName: "Registered", decoratorName: "ToDateTimeUtc",
            decoratorArgs: new[] { "yyyy-MM-dd" }, value: "21/11/2005");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Registered");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        var transformerIssues = summary.Issues
            .Where(i => i.Type == DiagnosticIssueType.TransformerFailure).ToList();
        Assert.Single(transformerIssues);
        Assert.Equal("Registered", transformerIssues[0].TokenName);
        Assert.Contains("ToDateTimeUtc", transformerIssues[0].Description);
    }

    [Fact]
    public void GivenValidatorFailure_WhenBuildingSummary_ThenIssueIsCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.ValidatorFailed,
            tokenName: "Email", decoratorName: "IsEmailValidator",
            value: "notanemail");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Email");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        var validatorIssues = summary.Issues
            .Where(i => i.Type == DiagnosticIssueType.ValidatorRejection).ToList();
        Assert.Single(validatorIssues);
        Assert.Equal("Email", validatorIssues[0].TokenName);
    }

    [Fact]
    public void GivenMissedToken_WhenBuildingSummary_ThenPreambleNeverFoundIssueCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Second");
        collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: "Matches: 1, Misses: 1");

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        Assert.NotEmpty(summary.Issues);
        Assert.Contains(summary.Issues, i => i.TokenName == "Second");
    }

    [Fact]
    public void GivenRepeatingTokenDisabled_WhenBuildingSummary_ThenRepeatingTokenIssueCreated()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.RepeatingTokenDisabled,
            tokenName: "NameServers", detail: "Line gap detected");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var summary = diagnostics.Summary;

        // Assert
        var repeatingIssues = summary.Issues
            .Where(i => i.Type == DiagnosticIssueType.RepeatingTokenCutShort).ToList();
        Assert.Single(repeatingIssues);
        Assert.Equal("NameServers", repeatingIssues[0].TokenName);
    }
}
```

- [ ] **Step 2: Run test to verify the stub fails**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticSummaryBuilderTests"`
Expected: Tests fail because the stub doesn't produce real issues.

- [ ] **Step 3: Implement DiagnosticSummaryBuilder**

Replace the stub in `src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs` with the full implementation. The builder should:

1. Count `TokenAssigned` events for the verdict
2. Scan for `TransformerFailed` events → `TransformerFailure` issues
3. Scan for `ValidatorFailed` events → `ValidatorRejection` issues
4. Scan for `TokenMissed` events that have no prior `TransformerFailed`/`ValidatorFailed` → `PreambleNeverFound` issues
5. Scan for `RepeatingTokenDisabled` events → `RepeatingTokenCutShort` issues
6. Scan for `HintMissing` events → `HintMissing` issues
7. Order issues: transformer/validator failures first (they explain root cause), then missed tokens, then repeating token issues

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticSummaryBuilderTests"`
Expected: All 5 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs tests/Tokenizer.Tests/Diagnostics/DiagnosticSummaryBuilderTests.cs
git commit -m "Implement DiagnosticSummaryBuilder

Walks the diagnostic event trace to produce a verdict and ordered list
of issues. Categorizes failures as transformer, validator, preamble,
repeating token, or hint issues."
```

---

### Task 6: Hint Generator Interface and DateFormatHintGenerator

**Files:**
- Create: `src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs`
- Create: `src/Tokenizer/Diagnostics/Hints/DateFormatHintGenerator.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/DateFormatHintGeneratorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Diagnostics/Hints/DateFormatHintGeneratorTests.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Diagnostics.Hints;
using Xunit;

namespace Tokens.Tests.Diagnostics.Hints;

public class DateFormatHintGeneratorTests
{
    private readonly DateFormatHintGenerator _generator = new();

    [Fact]
    public void GivenDateWithWrongFormat_WhenGeneratingHint_ThenSuggestsCorrectFormat()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Registered"
        };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Registered",
            DecoratorName = "ToDateTimeUtcTransformer",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
            Value = "21/11/2005"
        };

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent,
            new DiagnosticCollector("t", "i").GetResult()!);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("dd/MM/yyyy", hint);
    }

    [Fact]
    public void GivenDateWithTimeAndWrongFormat_WhenGeneratingHint_ThenSuggestsFormatWithTime()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Registered"
        };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Registered",
            DecoratorName = "ToDateTimeUtcTransformer",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
            Value = "21/11/2005 15:21:32"
        };

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent,
            new DiagnosticCollector("t", "i").GetResult()!);

        // Assert
        Assert.NotNull(hint);
        Assert.Contains("dd/MM/yyyy", hint);
    }

    [Fact]
    public void GivenNonDateTransformer_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Name"
        };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Name",
            DecoratorName = "ToUpperTransformer",
            Value = "test"
        };

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent,
            new DiagnosticCollector("t", "i").GetResult()!);

        // Assert
        Assert.Null(hint);
    }

    [Fact]
    public void GivenUnparseableValue_WhenGeneratingHint_ThenReturnsNull()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Registered"
        };
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Registered",
            DecoratorName = "ToDateTimeUtcTransformer",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
            Value = "not a date at all"
        };

        // Act
        var hint = _generator.TryGenerateHint(issue, sourceEvent,
            new DiagnosticCollector("t", "i").GetResult()!);

        // Assert
        Assert.Null(hint);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DateFormatHintGeneratorTests"`
Expected: FAIL — types don't exist.

- [ ] **Step 3: Create IHintGenerator interface**

Create `src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs`:

```csharp
namespace Tokens.Diagnostics.Hints
{
    /// <summary>
    /// Generates an adaptive hint for a diagnostic issue by analyzing the
    /// event context. Returns null if no actionable hint can be produced.
    /// Implementations should prefer returning null over returning a
    /// low-confidence or misleading hint.
    /// </summary>
    internal interface IHintGenerator
    {
        /// <summary>
        /// Attempts to generate a hint for the given issue.
        /// </summary>
        /// <param name="issue">The diagnostic issue to generate a hint for</param>
        /// <param name="sourceEvent">The diagnostic event that caused the issue</param>
        /// <param name="trace">The full diagnostic trace for cross-referencing</param>
        /// <returns>A human-readable hint string, or null if no hint applies</returns>
        string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                TokenizationDiagnostics trace);
    }
}
```

- [ ] **Step 4: Implement DateFormatHintGenerator**

Create `src/Tokenizer/Diagnostics/Hints/DateFormatHintGenerator.cs`:

The generator should:
1. Only trigger for `TransformerFailed` events where `DecoratorName` contains `ToDateTime`
2. Take the `Value` and try parsing with common date format strings using `DateTime.TryParseExact`
3. If a format succeeds, suggest it in the hint
4. Common formats to try: `dd/MM/yyyy`, `MM/dd/yyyy`, `yyyy/MM/dd`, `dd-MM-yyyy`, `MM-dd-yyyy`, `dd-MMM-yyyy`, `MMM dd, yyyy`, and all of these with ` HH:mm:ss` suffix

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DateFormatHintGeneratorTests"`
Expected: All 4 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Diagnostics/Hints/ tests/Tokenizer.Tests/Diagnostics/Hints/
git commit -m "Add IHintGenerator and DateFormatHintGenerator

Tries common date format permutations against failed values and suggests
the matching format. Returns null for non-date transformers or
unparseable values."
```

---

### Task 7: Remaining Hint Generators — PreambleNearMiss, ValidatorValue, UnmatchedInput, RepeatingToken

**Files:**
- Create: `src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs`
- Create: `src/Tokenizer/Diagnostics/Hints/ValidatorValueHintGenerator.cs`
- Create: `src/Tokenizer/Diagnostics/Hints/UnmatchedInputHintGenerator.cs`
- Create: `src/Tokenizer/Diagnostics/Hints/RepeatingTokenHintGenerator.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/PreambleNearMissHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/ValidatorValueHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/UnmatchedInputHintGeneratorTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/Hints/RepeatingTokenHintGeneratorTests.cs`

- [ ] **Step 1: Write tests for PreambleNearMissHintGenerator**

Create `tests/Tokenizer.Tests/Diagnostics/Hints/PreambleNearMissHintGeneratorTests.cs`:

Test cases:
- Case-insensitive near-miss: preamble "Domain Name:" vs input containing "domain name:" → hint with line number
- Whitespace difference: preamble "Name:" vs "Name:  " → hint mentioning whitespace
- No near-miss found → returns null
- Substring match: preamble "Registrant:" vs input "Registrant type:" → hint

- [ ] **Step 2: Implement PreambleNearMissHintGenerator**

Create `src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs`:

Triggers on `TokenMissed` events. Searches the input text (stored in `TokenizationDiagnostics`) for near-matches using:
- Case-insensitive comparison
- Whitespace-normalized comparison
- Substring containment check

Reports the line number where the near-miss was found.

- [ ] **Step 3: Write tests for ValidatorValueHintGenerator**

Create `tests/Tokenizer.Tests/Diagnostics/Hints/ValidatorValueHintGeneratorTests.cs`:

Test cases:
- IsEmail fails on value without '@' → hint explains missing '@'
- IsDomainName fails on value with spaces → hint explains spaces
- IsNumeric fails on non-numeric → hint explains
- Unknown validator → returns null

- [ ] **Step 4: Implement ValidatorValueHintGenerator**

Create `src/Tokenizer/Diagnostics/Hints/ValidatorValueHintGenerator.cs`:

Triggers on `ValidatorFailed` events. For known validators, provides specific failure explanation.

- [ ] **Step 5: Write tests for RepeatingTokenHintGenerator**

Create `tests/Tokenizer.Tests/Diagnostics/Hints/RepeatingTokenHintGeneratorTests.cs`:

Test cases:
- Repeating token disabled with detail about line gap → hint explains
- Repeating token disabled with validator failure context → hint suggests fix

- [ ] **Step 6: Implement RepeatingTokenHintGenerator and UnmatchedInputHintGenerator**

Create both generators. `UnmatchedInputHintGenerator` scans for input sections not covered by any token event. `RepeatingTokenHintGenerator` explains why a repeating token was disabled.

- [ ] **Step 7: Run all tests**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HintGenerator"`
Expected: All hint generator tests PASS.

- [ ] **Step 8: Wire hint generators into DiagnosticSummaryBuilder**

Update `DiagnosticSummaryBuilder` to call hint generators for each issue and populate the `Hint` property.

- [ ] **Step 9: Run all tests**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS.

- [ ] **Step 10: Commit**

```bash
git add src/Tokenizer/Diagnostics/Hints/ tests/Tokenizer.Tests/Diagnostics/Hints/ src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs
git commit -m "Add PreambleNearMiss, ValidatorValue, UnmatchedInput, RepeatingToken hint generators

Each generator provides actionable hints for its diagnostic issue type.
Wired into DiagnosticSummaryBuilder to auto-populate issue hints."
```

---

### Task 8: AlignmentRenderer — Replace Stub With Real Implementation

**Files:**
- Modify: `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tests.Diagnostics;

public class AlignmentRendererTests
{
    [Fact]
    public void GivenSuccessfulMatch_WhenRendering_ThenShowsMatchedTokens()
    {
        // Arrange
        var collector = new DiagnosticCollector("Name: { Name }", "Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted,
            detail: "Template: test, Tokens: 1, Input length: 10");
        collector.Record(DiagnosticEventType.PreambleMatched,
            tokenName: "Name", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenAssigned,
            tokenName: "Name", value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: "Matches: 1, Misses: 0");

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Name", output);
        Assert.Contains("John", output);
        Assert.Contains("✓", output);
    }

    [Fact]
    public void GivenMissedToken_WhenRendering_ThenShowsUnmatchedSection()
    {
        // Arrange
        var collector = new DiagnosticCollector(
            "Name: { Name }\nAge: { Age }", "Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name",
            value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Age");
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Age", output);
        Assert.Contains("✗", output);
    }

    [Fact]
    public void GivenRenderedAlignment_WhenRendered_ThenContainsSummaryLine()
    {
        // Arrange
        var collector = new DiagnosticCollector("Name: { Name }", "Name: John");
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name",
            value: "John", location: new FileLocation());
        collector.Record(DiagnosticEventType.TokenizationCompleted);

        // Act
        var diagnostics = collector.GetResult()!;
        var output = diagnostics.RenderAlignment();

        // Assert
        Assert.Contains("Matched", output);
    }
}
```

- [ ] **Step 2: Run test to verify the stub fails**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "AlignmentRendererTests"`
Expected: FAIL — stub returns placeholder text.

- [ ] **Step 3: Implement AlignmentRenderer**

Replace the stub in `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`. The renderer should:

1. Parse the input into lines
2. Walk the diagnostic events chronologically
3. Group events by input line
4. For each line that has events, render: line number, input content, result (✓/✗/⚠), token name and value if matched
5. Show unmatched tokens section at bottom
6. Show summary line (matched/missed/exceptions counts)
7. Embed hints inline where available from the summary

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "AlignmentRendererTests"`
Expected: All 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Diagnostics/AlignmentRenderer.cs tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs
git commit -m "Implement AlignmentRenderer for visual template-input diff

Renders line-by-line alignment showing matched tokens, missed tokens,
and unmatched input sections with inline hints."
```

---

### Task 9: Logger Integration and CreateDiagnosticTokenizer Helper

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `tests/Tokenizer.Tests/TokenizerTestBase.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticLoggingTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/Tokenizer.Tests/Diagnostics/DiagnosticLoggingTests.cs`:

```csharp
using Tokens.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tests.Diagnostics;

public class DiagnosticLoggingTests : TokenizerTestBase
{
    public DiagnosticLoggingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenDiagnosticTokenizer_WhenTokenizing_ThenDiagnosticsArePopulated()
    {
        // Arrange
        var tokenizer = CreateDiagnosticTokenizer();

        // Act
        var result = tokenizer.Tokenize("Name: { Name }", "Name: John");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics!.Summary.Verdict);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticLoggingTests"`
Expected: FAIL — `CreateDiagnosticTokenizer` doesn't exist.

- [ ] **Step 3: Add CreateDiagnosticTokenizer to TokenizerTestBase**

In `tests/Tokenizer.Tests/TokenizerTestBase.cs`, add:

```csharp
/// <summary>
/// Creates a Tokenizer with diagnostics enabled and logging.
/// </summary>
protected Tokenizer CreateDiagnosticTokenizer()
{
    return CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
}
```

- [ ] **Step 4: Add diagnostic logging to Tokenizer.Tokenize()**

In `src/Tokenizer/Tokenizer.cs`, in the private `Tokenize()` method, after attaching diagnostics to the result, add log output:

```csharp
if (result.Diagnostics != null)
{
    log.LogInformation("{Verdict}", result.Diagnostics.Summary.Verdict);
    foreach (var issue in result.Diagnostics.Summary.Issues)
    {
        log.LogWarning("Token '{TokenName}': {Description}", issue.TokenName, issue.Description);
        if (issue.Hint != null)
        {
            log.LogWarning("  → Hint: {Hint}", issue.Hint);
        }
    }
    log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs tests/Tokenizer.Tests/TokenizerTestBase.cs tests/Tokenizer.Tests/Diagnostics/DiagnosticLoggingTests.cs
git commit -m "Add diagnostic logging and CreateDiagnosticTokenizer helper

Logs verdict at Information, issues at Warning, alignment at Debug.
Adds convenience helper for test classes."
```

---

### Task 10: Validation — Apply Diagnostics to Skipped Tests

**Files:**
- Modify: `tests/Tokenizer.Tests/SampleTests.cs`

- [ ] **Step 1: Remove Skip from TestWhoisUk and enable diagnostics**

In `tests/Tokenizer.Tests/SampleTests.cs`, change `TestWhoisUk`:
- Change `[Fact(Skip = "Ignore until debug processing is finished")]` to `[Fact]`
- Use `CreateDiagnosticTokenizer()` instead of `CreateTokenizer()`
- Temporarily wrap the assertions in a try/catch that outputs `result.Diagnostics.RenderAlignment()` on failure

- [ ] **Step 2: Run TestWhoisUk and examine diagnostic output**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TestWhoisUk" -v n`
Examine the test output for diagnostic summary, issues, and hints. Verify the diagnostics identify the root cause of the failure.

- [ ] **Step 3: Remove Skip from TestAmazonCoJp and enable diagnostics**

Same approach: remove Skip, use diagnostic tokenizer, run, examine output.

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TestAmazonCoJp" -v n`

- [ ] **Step 4: Remove Skip from TestWhoisVe and enable diagnostics**

Same approach.

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TestWhoisVe" -v n`

- [ ] **Step 5: Evaluate diagnostic quality**

For each of the three tests, verify:
- The diagnostic summary correctly identifies the root cause
- Adaptive hints provide actionable suggestions
- The alignment diff shows where template and input diverge

Document findings. If diagnostics correctly identify issues, fix the templates/tests if possible.

- [ ] **Step 6: Run full test suite**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All previously-passing tests still pass. The three previously-skipped tests may pass or fail depending on whether fixes were applied.

- [ ] **Step 7: Commit**

```bash
git add tests/Tokenizer.Tests/SampleTests.cs
git commit -m "Enable diagnostics on previously-skipped sample tests

Remove Skip attributes from TestWhoisUk, TestAmazonCoJp, TestWhoisVe.
Enable diagnostic output to verify the diagnostics system identifies
root causes of template matching failures."
```

---

## File Map Summary

### New files (16)
| File | Responsibility |
|------|---------------|
| `src/Tokenizer/Diagnostics/DiagnosticEventType.cs` | Event type enum with XMLDoc |
| `src/Tokenizer/Diagnostics/DiagnosticEvent.cs` | Event data class |
| `src/Tokenizer/Diagnostics/DiagnosticIssueType.cs` | Issue type enum |
| `src/Tokenizer/Diagnostics/DiagnosticIssue.cs` | Issue data class |
| `src/Tokenizer/Diagnostics/DiagnosticSummary.cs` | Summary data class |
| `src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs` | Summary generation |
| `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs` | Top-level container |
| `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` | Collector interface |
| `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs` | No-op implementation |
| `src/Tokenizer/Diagnostics/DiagnosticCollector.cs` | Active implementation |
| `src/Tokenizer/Diagnostics/AlignmentRenderer.cs` | Alignment diff renderer |
| `src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs` | Hint generator interface |
| `src/Tokenizer/Diagnostics/Hints/DateFormatHintGenerator.cs` | Date format hints |
| `src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs` | Preamble near-miss hints |
| `src/Tokenizer/Diagnostics/Hints/ValidatorValueHintGenerator.cs` | Validator failure hints |
| `src/Tokenizer/Diagnostics/Hints/UnmatchedInputHintGenerator.cs` | Unmatched input section hints |
| `src/Tokenizer/Diagnostics/Hints/RepeatingTokenHintGenerator.cs` | Repeating token hints |

### Modified files (12)
| File | Change |
|------|--------|
| `src/Tokenizer/TokenizerOptions.cs` | Add `EnableDiagnostics`, remove `EnableLogging`/`EnableLineByLineLogging` |
| `src/Tokenizer/TokenizeResultBase.cs` | Add `Diagnostics` property |
| `src/Tokenizer/Tokenizer.cs` | Create collector, wire to engine, log diagnostics |
| `src/Tokenizer/Tokenization/TokenizationEngine.cs` | Record events, remove LineTracker |
| `src/Tokenizer/Tokenization/ITokenizationEngine.cs` | Add collector parameter |
| `src/Tokenizer/Token.cs` | Add collector to Assign(), record decorator events |
| `src/Tokenizer/CandidateTokenList.cs` | Pass collector through TryAssign() |
| `src/Tokenizer/Tokenization/HintProcessor.cs` | Record hint events |
| `src/Tokenizer/Tokenization/IHintProcessor.cs` | Add collector parameter |
| `src/Tokenizer/Tokenization/ResultBuilder.cs` | Record miss events |
| `src/Tokenizer/Tokenization/IResultBuilder.cs` | Add collector parameter |
| `tests/Tokenizer.Tests/TokenizerTestBase.cs` | Add CreateDiagnosticTokenizer() |

### Deleted files (1)
| File | Reason |
|------|--------|
| `src/Tokenizer/Tokenization/LineTracker.cs` | Superseded by alignment diff |
