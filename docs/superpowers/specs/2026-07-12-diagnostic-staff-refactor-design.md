# Diagnostic Subsystem Staff-Level Refactor

## Goal

Refactor the diagnostic subsystem to staff-level quality: eliminate ISP violations, temporal coupling, mutable side effects, and naming inconsistencies. All changes are internal to the `Tokens.Diagnostics` namespace with the exception of public API renames (acceptable — beta).

## Architecture

The refactoring has 9 concerns grouped into 3 waves:

1. **Structural** (D2, D3, D5): Interface split, generic event type, record conversion
2. **Builder redesign** (D1, D4, M1): BuildContext extraction, instance builder, count deduplication
3. **Polish** (M4, L1, D6, D7): Documentation, log guard, test refactor

Each wave produces working, testable software. Tests remain green after each task.

## Naming Convention

The two diagnostic paths align with their user-facing API methods:

| API Method | Prefix | Example |
|-----------|--------|---------|
| `Tokenize()` | `Tokenization*` | `TokenizationDiagnostics`, `TokenizationDiagnosticCollector` |
| `Compile()` | `Compilation*` | `CompilationDiagnostics`, `CompilationDiagnosticCollector` |

### Full Rename Table

| Current | New |
|---------|-----|
| `IDiagnosticCollector` | Split → `ITokenizationDiagnosticCollector` + `ICompilationDiagnosticCollector` |
| `RuntimeDiagnosticCollector` | `TokenizationDiagnosticCollector` |
| `CompilationDiagnosticCollector` | `CompilationDiagnosticCollector` (unchanged) |
| `NullDiagnosticCollector` | `NullTokenizationDiagnosticCollector` + `NullCompilationDiagnosticCollector` |
| `DiagnosticResult` | `TokenizationDiagnostics` |
| `DiagnosticEvent` | `DiagnosticEvent<TokenizationEventType>` (alias: `TokenizationEvent`) |
| `CompilationEvent` | `DiagnosticEvent<CompilationEventType>` (alias: `CompilationEvent`) |
| `DiagnosticEventType` | `TokenizationEventType` |

### Unchanged Names (shared output model)

`DiagnosticIssue`, `DiagnosticIssueType`, `TokenDiagnostic`, `TokenAttempt`, `TokenOutcome`, `IssueCodeMap`, `IssueFactory`

## Design Decisions

**Why not a generic `IDiagnosticCollector<TEventType>` base interface?**
The two collector interfaces have identical `Record(...)` signatures differing only in the enum type parameter. A generic base would eliminate the signature duplication. We intentionally reject this: there is no code that needs to be generic over "any collector," and the two paths may diverge in future (e.g., compilation may gain structured severity levels). The DRY cost is one signature line maintained in two places — acceptable for full decoupling. YAGNI.

## Detailed Design

### 1. Interface Split (D2)

```csharp
internal interface ITokenizationDiagnosticCollector
{
    bool IsEnabled { get; }
    void Record(TokenizationEventType type, string? tokenName = null, int? tokenId = null,
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);
    TokenizationDiagnostics? GetResult();
}

internal interface ICompilationDiagnosticCollector
{
    bool IsEnabled { get; }
    void Record(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);
    CompilationDiagnostics? GetResult();
}
```

Implementations:
- `TokenizationDiagnosticCollector : ITokenizationDiagnosticCollector` — active runtime collector
- `CompilationDiagnosticCollector : ICompilationDiagnosticCollector` — active compilation collector
- `NullTokenizationDiagnosticCollector : ITokenizationDiagnosticCollector` — singleton no-op
- `NullCompilationDiagnosticCollector : ICompilationDiagnosticCollector` — singleton no-op

Call sites:
- Compilation binders (`HintBinder`, `TagBinder`, `TokenFactory`, `OptionApplier`, `RepeatingTokenLinker`, `DecoratorBinder`) accept `ICompilationDiagnosticCollector`
- Tokenization components (`TokenizationSession`, `DecoratorPipeline`, `CandidateProcessor`, `TokenMatchRouter`, `ResultBuilder`, hint strategies) accept `ITokenizationDiagnosticCollector`
- `TemplateCompiler.Compile()` creates `CompilationDiagnosticCollector` or `NullCompilationDiagnosticCollector`
- `Tokenizer.RunCoreAsync()` creates `TokenizationDiagnosticCollector` or `NullTokenizationDiagnosticCollector`

### 2. Generic Event Type (D3)

Single generic class replaces two identical classes:

```csharp
public sealed class DiagnosticEvent<TType> where TType : struct, Enum
{
    public TType Type { get; init; }
    public string? TokenName { get; init; }
    public int? TokenId { get; init; }
    public FileLocation? Location { get; init; }
    public string? Value { get; init; }
    public string? Detail { get; init; }
    public string? DecoratorName { get; init; }
    public string[]? DecoratorArgs { get; init; }
}
```

Internal code uses `global using` aliases (in `src/Tokenizer/Diagnostics/GlobalUsings.cs`) to avoid generic syntax throughout the project:

```csharp
global using TokenizationEvent = Tokens.Diagnostics.DiagnosticEvent<Tokens.Diagnostics.TokenizationEventType>;
global using CompilationEvent = Tokens.Diagnostics.DiagnosticEvent<Tokens.Diagnostics.CompilationEventType>;
```

Public API surface:
- `TokenizationDiagnostics.RawEvents` → `IReadOnlyList<DiagnosticEvent<TokenizationEventType>>`
- `CompilationDiagnostics.Events` → `IReadOnlyList<DiagnosticEvent<CompilationEventType>>`

### 3. TokenDiagnostic as Sealed Record (D5)

```csharp
public sealed record TokenDiagnostic
{
    public string TokenName { get; init; } = string.Empty;
    public int TokenId { get; init; }
    public TokenOutcome Outcome { get; init; }
    public IReadOnlyList<TokenAttempt> Attempts { get; init; } = [];
    public string? AssignedValue { get; init; }
    public FileLocation? AssignedLocation { get; init; }
    public string? BlockedBy { get; init; }
    public IReadOnlyList<DiagnosticIssue> Issues { get; init; } = [];
}
```

`ApplyBlockedAnnotations` uses `with` expressions for the reclassification.

### 4. BuildContext Extraction (D1)

New internal class holds all state that was previously mutated onto `TokenizationDiagnostics`.
Constructor accepts individual values (not the diagnostics object) for testability and to avoid coupling to the result type's shape:

```csharp
internal sealed class BuildContext
{
    public string? InputContent { get; }
    public string[] InputLines { get; }  // eagerly computed from InputContent
    public bool OutOfOrderTokens { get; }
    public HashSet<string> OptionalTokenNames { get; }
    public Dictionary<string, List<TokenizationEvent>> RejectionsPerToken { get; }
    public Dictionary<string, List<TokenizationEvent>> DecoratorSuccessesPerToken { get; }

    public BuildContext(string? inputContent, bool outOfOrderTokens, HashSet<string> optionalTokenNames)
    {
        InputContent = inputContent;
        InputLines = inputContent?.Split('\n') ?? Array.Empty<string>();
        OutOfOrderTokens = outOfOrderTokens;
        OptionalTokenNames = optionalTokenNames;
        RejectionsPerToken = new Dictionary<string, List<TokenizationEvent>>(StringComparer.Ordinal);
        DecoratorSuccessesPerToken = new Dictionary<string, List<TokenizationEvent>>(StringComparer.Ordinal);
    }
}
```

`TokenizationDiagnostics` removes:
- `internal Dictionary<string, List<...>>? RejectionsPerToken { get; set; }` — gone
- `internal Dictionary<string, List<...>>? DecoratorSuccessesPerToken { get; set; }` — gone
- `internal string[]? CachedInputLines { get; set; }` — gone

These properties no longer exist. `TokenizationDiagnostics` is immutable after construction.

### 5. Builder Instance (D4)

```csharp
internal sealed class TokenDiagnosticBuilder
{
    // Static shared instance — safe because IssueFactory and all IHintGenerator
    // implementations are stateless (no mutable fields).
    private static readonly IssueFactory DefaultIssueFactory = new IssueFactory(new IHintGenerator[] { ... });

    private readonly TokenizationDiagnostics _diagnostics;
    private readonly IssueFactory _issueFactory;
    private readonly BuildContext _context;

    public TokenDiagnosticBuilder(TokenizationDiagnostics diagnostics, IssueFactory? issueFactory = null)
    {
        _diagnostics = diagnostics;
        _issueFactory = issueFactory ?? DefaultIssueFactory;
        _context = new BuildContext(diagnostics.InputContent, diagnostics.OutOfOrderTokens, diagnostics.OptionalTokenNames);
    }

    /// <summary>
    /// Executes the build pipeline. Phases must run in this order:
    /// 1. CollectEvents — populates context indexes and collects attempts/issues
    /// 2. ClassifyOutcomes — creates TokenDiagnostics from collected data (calls ApplyValueMismatchIssues)
    /// 3. ApplyBlockedAnnotations — reclassifies NeverFound tokens downstream of a blocker
    /// 4. BuildVerdict — generates the human-readable summary string
    /// </summary>
    public (IReadOnlyList<TokenDiagnostic> tokens, string verdict, int matched, int missed, int total) Build()
    {
        var collected = CollectEvents();
        var result = ClassifyOutcomes(collected);
        if (!_context.OutOfOrderTokens)
            ApplyBlockedAnnotations(result, collected);
        if (collected.GlobalIssues.Count > 0)
            AddGlobalDiagnostic(result, collected);
        var verdict = BuildVerdict(collected);
        return (result, verdict, collected.MatchedCount, collected.MissedCount, collected.TotalCount);
    }

    // Private phase methods: CollectEvents, ClassifyOutcomes,
    // ApplyValueMismatchIssues, ApplyBlockedAnnotations, BuildVerdict
}
```

`TokenizationDiagnostics.GetBuilt()` changes from:
```csharp
var (tokens, verdict, matched, missed, total) = TokenDiagnosticBuilder.Build(this);
```
To:
```csharp
var builder = new TokenDiagnosticBuilder(this);
var (tokens, verdict, matched, missed, total) = builder.Build();
```

### 6. IHintGenerator and IssueFactory Signature Updates

```csharp
internal interface IHintGenerator
{
    string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                            TokenizationEvent sourceEvent, BuildContext context);
}
```

All 9 hint generators update to accept `BuildContext` instead of `TokenizationDiagnostics`. `PreambleNearMissHintGenerator` reads `context.InputLines` directly (no mutation).

`IssueFactory` also updates to accept `BuildContext`:

```csharp
internal DiagnosticIssue Create(DiagnosticIssueType type, TokenizationEvent sourceEvent,
                                string description, BuildContext context)
{
    var hint = GenerateHint(type, sourceEvent, context);
    return new DiagnosticIssue { ... };
}

internal DiagnosticIssue CreateValueMismatch(string tokenName, string missedTokenName, BuildContext context) { ... }
internal DiagnosticIssue CreateBlocked(string tokenName, string blockerName, BuildContext context) { ... }
```

### 7. Repeating-Token Count Fix (M1)

In `CollectEvents`, counting changes:
- `MatchedCount` increments only on first `TokenAssigned` per unique token name
- `MissedCount` increments only for token names that have no assignment in `AssignedTokens`
- `TotalCount = MatchedCount + MissedCount` (always equals `Tokens.Count`)

Repeating token repetition details remain in `RawEvents` for power users.

**Acceptance criteria:**
- A repeating token with N repetitions (N ≥ 1 matched) counts as 1 matched token
- A repeating token with 0 matches counts as 1 missed token
- `TotalCount` equals the number of unique token names in the template
- `TotalCount == Tokens.Count` always holds

### 8. Warning Log IsEnabled Guard (L1)

```csharp
if (_log.IsEnabled(LogLevel.Warning) && result.Diagnostics.MissedCount > 0)
{
    foreach (var token in result.Diagnostics.Tokens) { ... }
}
```

Prevents `GetBuilt()` trigger and iteration when Warning is disabled.

### 9. Blocked Annotation Documentation (M4)

Comment in `ApplyBlockedAnnotations`:
```csharp
// Only NeverFound tokens are reclassified as Blocked. Rejected tokens were
// actively attempted and carry their own diagnostic value (validator feedback, hints).
```

### 10. [Theory] Test Refactor (D6)

`ValidatorRejectionTests.cs` structurally identical tests collapse into:
```csharp
[Theory]
[MemberData(nameof(ValidatorRejectionCases))]
public void GivenValidatorRejectsValue_WhenTokenizing_ThenDiagnosticsShowRejection(
    string template, string input, string expectedDecoratorName) { ... }
```

### 11. Complexity Documentation (D7)

XML doc on the value mismatch method:
```csharp
/// <summary>
/// Detects tokens whose assigned value contains the preamble of a missed/rejected token.
/// Complexity: O(matched × missed × value_length). Bounded by template token count
/// (typically &lt;50) and short preamble/value strings. Acceptable at current scale.
/// </summary>
```

## Files Affected

### New Files
- `src/Tokenizer/Diagnostics/ITokenizationDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/ICompilationDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/NullTokenizationDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/NullCompilationDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/DiagnosticEvent.cs` (generic, replaces two files)
- `src/Tokenizer/Diagnostics/BuildContext.cs`
- `src/Tokenizer/Diagnostics/GlobalUsings.cs` (global using aliases for event types)

### Renamed Files
- `DiagnosticResult.cs` → `TokenizationDiagnostics.cs`
- `DiagnosticEventType.cs` → `TokenizationEventType.cs`
- `RuntimeDiagnosticCollector.cs` → `TokenizationDiagnosticCollector.cs`

### Deleted Files
- `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` (split into two)
- `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs` (split into two)
- `src/Tokenizer/Diagnostics/CompilationEvent.cs` (merged into generic)
- `src/Tokenizer/Diagnostics/DiagnosticEvent.cs` (merged into generic)

### Modified Files (signature/type updates)
- All compilation binders (6 files): `IDiagnosticCollector` → `ICompilationDiagnosticCollector`
- All tokenization components (7 files): `IDiagnosticCollector` → `ITokenizationDiagnosticCollector`
- `TemplateCompiler.cs`: collector creation
- `Tokenizer.cs`: collector creation, log guard
- `TokenDiagnosticBuilder.cs`: instance class, BuildContext, count fix
- `IssueFactory.cs`: `BuildContext` parameter
- All 9 hint generators: `BuildContext` parameter
- `AlignmentRenderer.cs`, `ProcessingOrderRenderer.cs`: `TokenizationDiagnostics` type
- `TokenDiagnostic.cs`: sealed record
- `CompilationDiagnostics.cs`: generic event type
- Test files: type renames, Theory refactor

## Constraints

- All existing tests must pass after each task (may need type/name updates)
- Public API changes: `DiagnosticResult` → `TokenizationDiagnostics`, `DiagnosticEvent` → `DiagnosticEvent<TokenizationEventType>`, `DiagnosticEventType` → `TokenizationEventType`. Acceptable in beta.
- No behavioral changes to diagnostic output (same events, same issues, same hints)
- TDD for new behavior (count fix, log guard). Direct refactor for structural changes.
