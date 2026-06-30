# Tokenization Diagnostics System

**Date:** 2026-06-30
**Status:** Draft
**Scope:** Structured diagnostic tracing, mismatch summaries, adaptive hints, and alignment diffs for the Tokenizer library

## Problem

When a template/input pair doesn't match, it's hard to determine why. The existing logging tells you _what_ happened (token assignment failed) but not _why_ (the date format was wrong, the preamble had a case difference, an unmatched input section pushed tokens out of alignment). Debugging requires manually comparing template and input character by character.

This impacts:
- **Library developers** writing and debugging templates
- **Unit test authors** investigating skipped/failing tests
- **AI agents** that need machine-readable failure context to automatically identify root causes

## Goals

1. Structured diagnostic trace that records every matching decision
2. Concise mismatch summary that identifies root cause in 2-3 lines
3. Adaptive hints that suggest specific fixes ("did you mean this date format?")
4. Visual alignment diff showing template-vs-input matching
5. Zero overhead when disabled — no allocations, no string formatting
6. Opt-in via a single flag: `TokenizerOptions.EnableDiagnostics`
7. Output usable by both humans (via ILogger/XUnit) and AI agents (via structured API)

## Non-Goals

- Interactive step-through debugger
- Template compilation diagnostics (linting bad templates before matching)
- Snapshot/golden-file diffing

## Cleanup: Remove Legacy Logging Flags

### Remove `EnableLogging`

`TokenizerOptions.EnableLogging` is a dead flag — it's declared but never checked in the engine code. All logging flows through `ILogger` and is controlled by `ILoggerFactory` log level configuration (the standard .NET pattern). This flag will be removed.

### Remove `EnableLineByLineLogging` and `LineTracker`

`EnableLineByLineLogging` controls whether a `LineTracker` instance is created in `ProcessTokenization()`. When enabled (default: `true`), it emits per-line summary messages at `Information` level showing matched/remaining tokens.

This is superseded by the new diagnostics alignment diff, which provides strictly more information in a better format. The `LineTracker` class, `EnableLineByLineLogging` option, and all `LineTracker`-related log messages will be removed entirely (not demoted to a lower log level).

### Post-Cleanup State

`TokenizerOptions` will have one diagnostic flag:
- `EnableDiagnostics` — controls the entire structured trace/summary/hints/alignment system

All existing `log.LogTrace` / `log.LogDebug` calls throughout the engine remain unchanged. Their verbosity is controlled by standard `ILoggerFactory` log level configuration.

Files affected:
- `TokenizerOptions.cs` — remove `EnableLogging`, `EnableLineByLineLogging` properties and their usage in `Clone()`
- `Tokenization/LineTracker.cs` — delete file
- `Tokenization/TokenizationEngine.cs` — remove `LineTracker` creation and usage

---

## Architecture

### Component Overview

```
TokenizerOptions.EnableDiagnostics = true
         │
         ▼
┌──────────────────────┐
│  DiagnosticCollector  │  (implements IDiagnosticCollector)
│  - Records events     │  (NullDiagnosticCollector when disabled)
└──────────┬───────────┘
           │ events recorded during tokenization
           ▼
┌──────────────────────────┐
│  TokenizationDiagnostics  │  (attached to TokenizeResultBase.Diagnostics)
│  - Events list            │
│  - Summary (lazy)         │  ◄── DiagnosticSummaryBuilder
│  - RenderAlignment()      │  ◄── AlignmentRenderer
└──────────────────────────┘
                                    │
                              ┌─────┴──────┐
                              │ IHintGenerator │
                              │ implementations │
                              └────────────────┘
```

### Opt-In Mechanism

```csharp
// In TokenizerOptions
/// <summary>
/// When true, tokenization results include a <see cref="TokenizationDiagnostics"/>
/// property with a structured trace of every matching decision, a mismatch summary
/// with adaptive hints, and a visual alignment diff.
/// Default: false. Has no performance impact when disabled.
/// </summary>
public bool EnableDiagnostics { get; set; }
```

When disabled, `NullDiagnosticCollector.Instance` is used — all `Record()` calls are no-ops that the JIT can inline away. `result.Diagnostics` remains null.

---

## Deliverable 1: Structured Diagnostic Trace

### DiagnosticEventType Enum

```csharp
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
```

### DiagnosticEvent

```csharp
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
```

### TokenizationDiagnostics

```csharp
/// <summary>
/// Complete diagnostic output from a tokenization run. Contains the raw
/// event trace, a lazily-generated mismatch summary, and a visual alignment renderer.
/// Attached to <see cref="TokenizeResultBase.Diagnostics"/> when
/// <see cref="TokenizerOptions.EnableDiagnostics"/> is true.
/// </summary>
public class TokenizationDiagnostics
{
    /// <summary>
    /// The ordered list of diagnostic events recorded during tokenization.
    /// Events are in chronological order.
    /// </summary>
    public List<DiagnosticEvent> Events { get; }

    /// <summary>
    /// Concise summary of issues found during tokenization.
    /// Generated lazily on first access from the event trace.
    /// </summary>
    public DiagnosticSummary Summary { get; }

    /// <summary>
    /// All events where matching failed (validators, transformers, assignments, misses).
    /// Convenience filter over Events.
    /// </summary>
    public IEnumerable<DiagnosticEvent> Failures { get; }

    /// <summary>
    /// All events related to a specific token name.
    /// </summary>
    public IEnumerable<DiagnosticEvent> ForToken(string name);

    /// <summary>
    /// The first event that represents a failure, or null if tokenization succeeded.
    /// Useful for quickly identifying the root cause.
    /// </summary>
    public DiagnosticEvent? FirstFailure { get; }

    /// <summary>
    /// Renders a visual alignment diff between template and input showing
    /// which tokens matched, where values were extracted, and where alignment
    /// broke down. Computed lazily on first access.
    /// </summary>
    public string RenderAlignment();
}
```

### IDiagnosticCollector

```csharp
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
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);

    /// <summary>
    /// Returns the collected diagnostics, or null if collection is disabled.
    /// </summary>
    TokenizationDiagnostics? GetResult();
}

/// <summary>
/// No-op collector used when diagnostics are disabled.
/// All methods are no-ops. The JIT can inline these away entirely.
/// </summary>
internal sealed class NullDiagnosticCollector : IDiagnosticCollector
{
    public static readonly NullDiagnosticCollector Instance = new();

    public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null) { }

    public TokenizationDiagnostics? GetResult() => null;
}

/// <summary>
/// Active collector that records all events into a TokenizationDiagnostics instance.
/// </summary>
internal sealed class DiagnosticCollector : IDiagnosticCollector
{
    // Stores events and builds the TokenizationDiagnostics on GetResult()
}
```

### Attachment to Result

```csharp
// In TokenizeResultBase
/// <summary>
/// Structured diagnostic output from the tokenization process.
/// Null when <see cref="TokenizerOptions.EnableDiagnostics"/> is false.
/// </summary>
public TokenizationDiagnostics? Diagnostics { get; internal set; }
```

---

## Deliverable 2: Mismatch Summary Report

### DiagnosticSummary

```csharp
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
    public string Verdict { get; init; }

    /// <summary>
    /// Ordered list of issues found, most significant first.
    /// Each issue is a self-contained explanation of one failure point.
    /// </summary>
    public IReadOnlyList<DiagnosticIssue> Issues { get; init; }
}
```

### DiagnosticIssue

```csharp
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
    public string Description { get; init; }

    /// <summary>
    /// Location in the input where the issue occurred.
    /// </summary>
    public FileLocation? Location { get; init; }

    /// <summary>
    /// Adaptive hint suggesting how to fix the issue, if available.
    /// Null when no hint can be generated. See Deliverable 3.
    /// </summary>
    public string? Hint { get; init; }
}
```

### DiagnosticIssueType

```csharp
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
```

### Summary Generation Logic

`DiagnosticSummaryBuilder` walks the trace events:

1. Count `TokenAssigned` vs total template tokens → `Verdict`
2. Find the first `TransformerFailed`, `ValidatorFailed`, or `TokenAssignmentFailed` event → first issue
3. Collect all `TokenMissed` events → one issue per missed required token
4. Identify `RepeatingTokenDisabled` events → `RepeatingTokenCutShort` issues
5. Detect input gaps (regions where characters were consumed without any preamble match or token assignment) → `UnmatchedInputSection` issues
6. For each issue, invoke the hint generator pipeline to populate the `Hint` field

---

## Deliverable 3: Adaptive Hints

### IHintGenerator Interface

```csharp
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
```

Generators are tried in order; the first non-null hint wins.

### Built-In Generators

#### 1. DateFormatHintGenerator

**Triggers on:** `TransformerFailed` where decorator is `ToDateTimeTransformer` or `ToDateTimeUtcTransformer`

**Logic:** Takes the failed value and tries parsing against common date formats. If one succeeds, suggests it.

**Formats tried:**
- `yyyy-MM-dd`, `dd-MM-yyyy`, `MM-dd-yyyy`
- `dd/MM/yyyy`, `MM/dd/yyyy`, `yyyy/MM/dd`
- All of the above with ` HH:mm:ss` suffix
- `dd-MMM-yyyy`, `MMM dd, yyyy`
- ISO 8601 variants

**Example output:**
```
ToDateTimeUtc('yyyy-MM-dd HH:mm:ss') failed on value '21/11/2005 15:21:32'.
Value matches format 'dd/MM/yyyy HH:mm:ss'. Change transformer to
ToDateTimeUtc('dd/MM/yyyy HH:mm:ss'), or reformat the input.
```

#### 2. PreambleNearMissHintGenerator

**Triggers on:** `TokenMissed` where the preamble was never found

**Logic:** Searches the input text for near-matches:
- Case-insensitive match
- Whitespace normalization (extra/missing spaces)
- Leading/trailing whitespace differences
- Substring containment

**Example output:**
```
Input contains 'Registrant Type:' at line 9 (case difference).
Update template preamble to match.
```

#### 3. ValidatorValueHintGenerator

**Triggers on:** `ValidatorFailed`

**Logic:** Shows the actual value and, for known validators, explains why it failed:
- `IsEmail` — "Value 'notanemail' doesn't contain '@'"
- `IsPhoneNumber` — "Value '2418246437 (FAX) 2418246437' contains non-phone characters. Consider adding SubstringBefore(' (FAX)') transformer before the validator"
- `IsDomainName` — "Value 'not a domain' contains spaces"
- `IsNumeric` — "Value '12.3.4' is not a valid number"

#### 4. UnmatchedInputHintGenerator

**Triggers on:** `UnmatchedInputSection`

**Logic:** When large sections of input are consumed without matching any token, checks if the unmatched text contains content that looks like a section header the template doesn't account for.

**Example output:**
```
Input contains section 'Registrant type:\n    UK Corporation by Royal Charter'
which has no corresponding token. This may push subsequent tokens out of alignment.
Consider adding a null token to skip this section.
```

#### 5. RepeatingTokenHintGenerator

**Triggers on:** `RepeatingTokenCutShort`

**Logic:** Explains why a repeating token was disabled and suggests fixes based on the failure context.

**Example output:**
```
Repeating token 'NameServers' disabled after matching 0 times.
The first value failed IsDomainName validation on '- ns10.tepuyserver.net'.
The '- ' prefix needs a Remove('- ') transformer before the IsDomainName validator.
```

### Extensibility

Hint generators are registered in a list. Adding new generators is implementing `IHintGenerator` and adding it to the pipeline. The system is designed to grow as common failure patterns are discovered.

---

## Deliverable 4: Template-Input Alignment Diff

### AlignmentRenderer

```csharp
/// <summary>
/// Renders a visual alignment between template and input showing which
/// tokens matched, where values were extracted, and where alignment broke down.
/// Consumes the diagnostic event trace to produce the output.
/// </summary>
internal class AlignmentRenderer
{
    /// <summary>
    /// Produces a multi-line string showing template/input alignment.
    /// Groups output by input line, showing only interesting lines
    /// (matches, failures, unmatched gaps). Embeds hints inline where available.
    /// </summary>
    public string Render(TokenizationDiagnostics diagnostics, Template template, string input);
}
```

### Output Format

```
═══ Tokenization Alignment: whois.nic.uk/uk/Found ═══
Template: 11 tokens | Input: 54 lines | Result: FAILED (6/11 matched)

── Line 3 ──────────────────────────────────────────
  Template:  "    Domain name:"
  Input:     "    Domain name:"
  Result:    ✓ Preamble matched

── Line 4 ──────────────────────────────────────────
  Template:  "        { DomainName : IsDomainName, ToLower }"
  Input:     "        bbc.co.uk"
  Result:    ✓ DomainName = "bbc.co.uk"

── Line 9-11 ───────────────────────────────────────
  Template:  (no token)
  Input:     "    Registrant type:\n        UK Corporation..."
  Result:    ⚠ Unmatched input — 3 lines consumed without a token
  Hint:      Section 'Registrant type:' has no corresponding token.
             This may push subsequent tokens out of alignment.

── Line 27 ─────────────────────────────────────────
  Template:  "        Registered on: { Registered ? : ... ToDateTimeUtc("dd-MMM-yyyy") }"
  Input:     "        Registered on: before Aug-1996"
  Result:    ✓ Registered = 1996-08-01 (transformed via Replace('before ', '01-'))

═══ Unmatched Tokens ═══
  ✗ Registrant.TelephoneNumber (optional) — preamble never found
  ✗ Registrar.IanaId (required) — preamble never found
      Hint: Input contains 'IANA ID:' at line 22 (case/format difference)

═══ Summary ═══
  Matched: 6 | Missed: 5 (2 required) | Exceptions: 0
```

### Design Decisions

- **Group by input line, not template line** — the input is what the developer is looking at
- **Only show interesting lines** — skip lines where nothing happened (pure noise consumed). Show: preamble matches, token assignments, failures, unmatched input gaps
- **Embed hints inline** — when a hint is available, show it at the point of failure
- **Unmatched tokens section at bottom** — tokens that never appeared in the input
- **Concise by default** — curated view, not a dump of every event. Raw `Events` list available for full trace

### Data Requirements

`TokenizationDiagnostics` must hold references to the template and input string, captured at creation time. This is acceptable since diagnostics is opt-in and the strings are already in scope during tokenization.

---

## Logger Integration

### Rendering Strategy: On Completion, Not Per-Event

Diagnostic events are not emitted through `ILogger` individually. Instead, the summary and alignment are rendered as log messages after tokenization completes.

### Log Levels

| Content | Level | Rationale |
|---------|-------|-----------|
| Verdict one-liner | Information | Always visible when diagnostics enabled |
| Each issue + hint | Warning | Stands out in test output |
| Full alignment diff | Debug | Available when verbose output wanted |
| Raw event trace | Trace | Existing engine logging, unchanged |

### Wiring in Tokenizer.Tokenize()

After the engine finishes, if diagnostics are enabled:

```csharp
result.Diagnostics = collector.GetResult();

log.LogInformation("{Summary}", result.Diagnostics.Summary.Verdict);
foreach (var issue in result.Diagnostics.Summary.Issues)
{
    log.LogWarning("Token '{TokenName}': {Description}", issue.TokenName, issue.Description);
    if (issue.Hint != null)
    {
        log.LogWarning("  → Hint: {Hint}", issue.Hint);
    }
}
log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
```

### XUnit Test Usage

Since `TestLoggerFactory` configures Serilog at `MinimumLevel.Verbose()`, all diagnostic output flows to test output automatically:

```csharp
// Enable diagnostics for a test
var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
var result = tokenizer.Tokenize(template, input);

// Diagnostics appear in test output via ILogger automatically

// Programmatic access also available:
Assert.NotNull(result.Diagnostics);
Output.WriteLine(result.Diagnostics.RenderAlignment());
```

A convenience helper on `TokenizerTestBase`:

```csharp
protected Tokenizer CreateDiagnosticTokenizer()
{
    return CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
}
```

---

## Integration Points

Where `IDiagnosticCollector.Record()` is called in existing code:

### TokenizationEngine.ProcessTokenization()
- Loop entry → `TokenizationStarted`
- `context.Enumerator.Match(tokens...)` returns matches → `PreambleMatched`
- No matches at current position → `PreambleNotFound`
- After main loop → `TokenizationCompleted`

### TokenizationEngine.TryAssignCandidateTokens()
- Entry → `TokenAssignmentAttempted`
- Success → `TokenAssigned`
- Failure → `TokenAssignmentFailed`

### TokenizationEngine.ProcessRepeatedTokens()
- `CanAnyAssign` returns false → `BacktrackStarted`
- Repeating token disabled → `RepeatingTokenDisabled`
- ConsiderOnce removed → `ConsiderOnceTokenRemoved`

### TokenizationEngine.ProcessNewlineTerminatedTokens()
- Processing → `NewlineTerminatedTokenProcessed`

### TokenizationEngine.ProcessFrontMatterTokens()
- Assigned → `FrontMatterTokenAssigned`
- Failed → `FrontMatterTokenFailed`

### Token.Assign() (new `IDiagnosticCollector` parameter)
- Transformer succeeds → `TransformerSucceeded`
- Transformer fails → `TransformerFailed`
- Validator passes → `ValidatorPassed`
- Validator fails → `ValidatorFailed`

### HintProcessor.FindAndValidateHints()
- Hint found → `HintMatched`
- Hint missing → `HintMissing`

### ResultBuilder.BuildUnmatchedTokens()
- Each unmatched token → `TokenMissed`

### Note on Token.Assign()

`Token.Assign()` currently has no access to instance-level services (it uses a static `NullLogger<Token>`). The `IDiagnosticCollector` will be passed as a method parameter to `Token.Assign()` and propagated through `CandidateTokenList.TryAssign()`. This avoids changing Token's constructor or adding instance state.

### Note on ValueAccumulated

Emitted once when the accumulated value is about to be used (before assignment), not per-character. This keeps event count proportional to token count, not input length.

---

## File Organization

New files (all under `src/Tokenizer/Diagnostics/`):
- `DiagnosticEventType.cs` — event type enum with XMLDoc comments
- `DiagnosticEvent.cs` — event data class
- `DiagnosticIssueType.cs` — issue type enum
- `DiagnosticIssue.cs` — issue data class
- `DiagnosticSummary.cs` — summary data class
- `DiagnosticSummaryBuilder.cs` — summary generation logic
- `TokenizationDiagnostics.cs` — top-level diagnostics container
- `IDiagnosticCollector.cs` — collector interface
- `NullDiagnosticCollector.cs` — no-op implementation
- `DiagnosticCollector.cs` — active implementation
- `AlignmentRenderer.cs` — alignment diff renderer
- `Hints/IHintGenerator.cs` — hint generator interface
- `Hints/DateFormatHintGenerator.cs`
- `Hints/PreambleNearMissHintGenerator.cs`
- `Hints/ValidatorValueHintGenerator.cs`
- `Hints/UnmatchedInputHintGenerator.cs`
- `Hints/RepeatingTokenHintGenerator.cs`

Modified files:
- `TokenizerOptions.cs` — add `EnableDiagnostics`, remove `EnableLogging`, remove `EnableLineByLineLogging`
- `TokenizeResultBase.cs` — add `Diagnostics` property
- `Tokenizer.cs` — create collector, wire to engine, attach result
- `Tokenization/TokenizationEngine.cs` — add collector parameter, record events, remove `LineTracker` usage
- `Token.cs` — add collector parameter to `Assign()`, record decorator events
- `CandidateTokenList.cs` — pass collector through `TryAssign()`
- `Tokenization/HintProcessor.cs` — record hint events
- `Tokenization/ResultBuilder.cs` — record miss events
- `TokenizerTestBase.cs` — add `CreateDiagnosticTokenizer()` helper

Deleted files:
- `Tokenization/LineTracker.cs`

---

## Validation Plan

After implementation, enable diagnostics on the three currently-skipped tests:

1. **TestWhoisUk** — verify diagnostics identify why the UK template fails to match
2. **TestAmazonCoJp** — verify diagnostics explain JPRS template matching failure
3. **TestWhoisVe** — verify diagnostics explain Venezuela template matching failure

For each test:
1. Remove the `Skip` attribute
2. Enable `EnableDiagnostics = true`
3. Run the test and examine diagnostic output
4. Verify the summary and hints correctly identify root cause
5. If the diagnostics correctly identify the issue, fix the template/test and un-skip
