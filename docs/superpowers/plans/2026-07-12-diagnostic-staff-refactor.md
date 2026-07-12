# Diagnostic Staff-Level Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the diagnostic subsystem to staff-level quality — eliminate ISP violations, temporal coupling, mutable side effects, and naming inconsistencies.

**Architecture:** Three waves: (1) Structural renames + interface split + generic event, (2) BuildContext + instance builder + sealed record, (3) Behavioral fixes + polish. Each wave leaves tests green.

**Tech Stack:** C# / .NET 10 / xUnit / LangVersion=latest

## Global Constraints

- Targets: .NET Standard 2.0, .NET 8.0, .NET 10.0
- Root namespace: `Tokens`
- Braces: Allman style
- Test naming: `GivenScenario_WhenAction_ThenResult()`
- Test structure: Arrange / Act / Assert comments
- Private fields: `_camelCase`
- No `#region` blocks
- `global using` aliases require .NET 8+ — use conditional compilation or place in net8+ TFM only if needed (check if project has global usings already)
- TDD for behavioral changes. Direct refactor for structural/rename changes.
- Commit after each task.
- Run `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj` after every task to verify green.
- Run `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release` to verify no warnings.

---

### Task 1: Generic Event Type + Rename DiagnosticEventType → TokenizationEventType

**Files:**
- Create: `src/Tokenizer/Diagnostics/DiagnosticEvent{T}.cs` (generic event class)
- Create: `src/Tokenizer/Diagnostics/GlobalUsings.cs` (global using aliases)
- Rename: `src/Tokenizer/Diagnostics/DiagnosticEventType.cs` → content becomes `TokenizationEventType`
- Delete: `src/Tokenizer/Diagnostics/DiagnosticEvent.cs` (replaced by generic)
- Delete: `src/Tokenizer/Diagnostics/CompilationEvent.cs` (replaced by generic)
- Modify: `src/Tokenizer/Diagnostics/CompilationDiagnostics.cs` (use generic event type)
- Modify: All files referencing `DiagnosticEvent` or `DiagnosticEventType` or `CompilationEvent`

**Interfaces:**
- Produces: `DiagnosticEvent<TType>` generic class, `TokenizationEventType` enum, global `using TokenizationEvent = ...` and `using CompilationEvent = ...` aliases

- [ ] **Step 1: Create the generic event class**

Create `src/Tokenizer/Diagnostics/DiagnosticEvent{T}.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// A single diagnostic event recorded during compilation or tokenization,
/// representing one decision point in the process.
/// </summary>
/// <typeparam name="TType">The enum type identifying the event kind.</typeparam>
public sealed class DiagnosticEvent<TType> where TType : struct, Enum
{
    /// <summary>
    /// The type of decision or event.
    /// </summary>
    public TType Type { get; init; }

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
    /// The position in the input/source text where this event occurred.
    /// </summary>
    public FileLocation? Location { get; init; }

    /// <summary>
    /// The value being tested, assigned, or accumulated.
    /// Meaning varies by event type.
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Human-readable explanation providing additional context.
    /// For TransformerSucceeded, contains the transformed output value.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// The name of the decorator (validator or transformer) involved,
    /// or null for non-decorator events.
    /// </summary>
    public string? DecoratorName { get; init; }

    /// <summary>
    /// The parameters passed to the decorator, or null.
    /// </summary>
    public string[]? DecoratorArgs { get; init; }
}
```

- [ ] **Step 2: Create GlobalUsings.cs**

Create `src/Tokenizer/Diagnostics/GlobalUsings.cs`:

```csharp
global using TokenizationEvent = Tokens.Diagnostics.DiagnosticEvent<Tokens.Diagnostics.TokenizationEventType>;
global using CompilationEvent = Tokens.Diagnostics.DiagnosticEvent<Tokens.Diagnostics.CompilationEventType>;
```

- [ ] **Step 3: Rename DiagnosticEventType → TokenizationEventType**

In `src/Tokenizer/Diagnostics/DiagnosticEventType.cs`:
- Rename the file to `TokenizationEventType.cs`
- Rename the enum from `DiagnosticEventType` to `TokenizationEventType`
- Keep all enum members and XML docs unchanged

Then do a project-wide find/replace: `DiagnosticEventType` → `TokenizationEventType` in all `.cs` files under `src/Tokenizer/` and `tests/Tokenizer.Tests/`.

**Exact files to update** (use find/replace `DiagnosticEventType` → `TokenizationEventType`):
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs`
- `src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/ProcessingOrderRenderer.cs`
- `src/Tokenizer/Diagnostics/DiagnosticResult.cs`
- `src/Tokenizer/Tokenization/DecoratorPipeline.cs`
- `src/Tokenizer/Tokenization/TokenizationSession.cs`
- `src/Tokenizer/Tokenization/CandidateProcessor.cs`
- `src/Tokenizer/Tokenization/TokenMatchRouter.cs`
- `src/Tokenizer/Tokenization/ResultBuilder.cs`
- `src/Tokenizer/Tokenization/Strategies/UpfrontHintStrategy.cs`
- `src/Tokenizer/Tokenization/Strategies/StreamingHintStrategy.cs`
- All test files under `tests/Tokenizer.Tests/Diagnostics/`

- [ ] **Step 4: Delete old non-generic event files**

Delete:
- `src/Tokenizer/Diagnostics/DiagnosticEvent.cs`
- `src/Tokenizer/Diagnostics/CompilationEvent.cs`

The `global using` aliases (`TokenizationEvent` and `CompilationEvent`) now resolve to `DiagnosticEvent<TokenizationEventType>` and `DiagnosticEvent<CompilationEventType>` respectively. All existing code that used `DiagnosticEvent` now uses `TokenizationEvent` (same name pattern via alias). Code that used `CompilationEvent` continues working via the alias.

**Important:** Any file that previously had `using Tokens.Diagnostics;` will already see the global aliases. Check that no file has a local type named `TokenizationEvent` or `CompilationEvent` that conflicts.

- [ ] **Step 5: Update CompilationDiagnostics.cs**

The `CompilationDiagnostics` class has a `List<CompilationEvent>` field. With the global alias, `CompilationEvent` now resolves to `DiagnosticEvent<CompilationEventType>`. Verify the file compiles without changes (the alias should handle it). If it uses the full type name anywhere, update to use the alias.

- [ ] **Step 6: Update references from `DiagnosticEvent` to `TokenizationEvent`**

Any code using the bare `DiagnosticEvent` class name (without the old `CompilationEvent`) needs updating to `TokenizationEvent`. Do a project-wide replace: `DiagnosticEvent` → `TokenizationEvent` but ONLY where it refers to the old runtime event class (not the new generic `DiagnosticEvent<T>`).

**Strategy:** Use regex replace `\bDiagnosticEvent\b` → `TokenizationEvent` in all files EXCEPT `DiagnosticEvent{T}.cs` and `GlobalUsings.cs`. Then manually fix any false positives (e.g., if `DiagnosticEvent` appears in XML comments referring to the generic class).

Key files:
- `src/Tokenizer/Diagnostics/DiagnosticResult.cs` — `List<DiagnosticEvent>` → `List<TokenizationEvent>`
- `src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs` — `new DiagnosticEvent` → `new TokenizationEvent`
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` — all references
- `src/Tokenizer/Diagnostics/IssueFactory.cs` — parameter and construction
- `src/Tokenizer/Diagnostics/Hints/*.cs` — `DiagnosticEvent sourceEvent` → `TokenizationEvent sourceEvent`
- All test files referencing `DiagnosticEvent`

- [ ] **Step 7: Build and test**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: Build succeeds with 0 warnings, all tests pass.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Replace DiagnosticEvent/CompilationEvent with generic DiagnosticEvent<T>, rename DiagnosticEventType to TokenizationEventType"
```

---

### Task 2: Split IDiagnosticCollector + Rename Collectors

**Files:**
- Create: `src/Tokenizer/Diagnostics/ITokenizationDiagnosticCollector.cs`
- Create: `src/Tokenizer/Diagnostics/ICompilationDiagnosticCollector.cs`
- Create: `src/Tokenizer/Diagnostics/NullTokenizationDiagnosticCollector.cs`
- Create: `src/Tokenizer/Diagnostics/NullCompilationDiagnosticCollector.cs`
- Rename: `src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs` → `TokenizationDiagnosticCollector.cs`
- Delete: `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`
- Delete: `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs`
- Modify: All files referencing `IDiagnosticCollector`, `NullDiagnosticCollector`, `RuntimeDiagnosticCollector`

**Interfaces:**
- Produces: `ITokenizationDiagnosticCollector`, `ICompilationDiagnosticCollector`, `TokenizationDiagnosticCollector`, `NullTokenizationDiagnosticCollector`, `NullCompilationDiagnosticCollector`

- [ ] **Step 1: Create ITokenizationDiagnosticCollector**

Create `src/Tokenizer/Diagnostics/ITokenizationDiagnosticCollector.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Collects diagnostic events during tokenization.
/// Implementations must be safe for single-threaded use within one tokenization call.
/// </summary>
internal interface ITokenizationDiagnosticCollector
{
    /// <summary>
    /// Returns true when this collector is actively recording events.
    /// Use this to guard expensive argument evaluation at call sites.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Records a tokenization diagnostic event.
    /// </summary>
    void Record(TokenizationEventType type, string? tokenName = null, int? tokenId = null,
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);

    /// <summary>
    /// Returns the collected diagnostics, or null if collection is disabled.
    /// </summary>
    TokenizationDiagnostics? GetResult();
}
```

Note: `TokenizationDiagnostics` doesn't exist yet (it's the rename of `DiagnosticResult` in Task 3). For now, return `DiagnosticResult?` and we'll update in Task 3. OR — do the `DiagnosticResult` rename in this task. **Decision: include the DiagnosticResult → TokenizationDiagnostics rename in this task** to avoid a two-phase interface definition.

Actually, to keep tasks independent: use `DiagnosticResult?` for now. Task 3 will rename it.

Correction — use `DiagnosticResult?` as return type:

```csharp
    DiagnosticResult? GetResult();
```

- [ ] **Step 2: Create ICompilationDiagnosticCollector**

Create `src/Tokenizer/Diagnostics/ICompilationDiagnosticCollector.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Collects diagnostic events during template compilation.
/// Implementations must be safe for single-threaded use within one compilation call.
/// </summary>
internal interface ICompilationDiagnosticCollector
{
    /// <summary>
    /// Returns true when this collector is actively recording events.
    /// Use this to guard expensive argument evaluation at call sites.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Records a compilation diagnostic event.
    /// </summary>
    void Record(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                FileLocation? location = null, string? value = null, string? detail = null,
                string? decoratorName = null, string[]? decoratorArgs = null);

    /// <summary>
    /// Returns the collected compilation diagnostics, or null if collection is disabled.
    /// </summary>
    CompilationDiagnostics? GetResult();
}
```

- [ ] **Step 3: Create NullTokenizationDiagnosticCollector**

Create `src/Tokenizer/Diagnostics/NullTokenizationDiagnosticCollector.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// No-op tokenization diagnostic collector used when diagnostics are disabled.
/// All operations are discarded. Use <see cref="Instance"/> to avoid allocations.
/// </summary>
internal sealed class NullTokenizationDiagnosticCollector : ITokenizationDiagnosticCollector
{
    public static readonly NullTokenizationDiagnosticCollector Instance = new();

    private NullTokenizationDiagnosticCollector() { }

    public bool IsEnabled => false;

    public void Record(TokenizationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
    }

    public DiagnosticResult? GetResult() => null;
}
```

- [ ] **Step 4: Create NullCompilationDiagnosticCollector**

Create `src/Tokenizer/Diagnostics/NullCompilationDiagnosticCollector.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// No-op compilation diagnostic collector used when diagnostics are disabled.
/// All operations are discarded. Use <see cref="Instance"/> to avoid allocations.
/// </summary>
internal sealed class NullCompilationDiagnosticCollector : ICompilationDiagnosticCollector
{
    public static readonly NullCompilationDiagnosticCollector Instance = new();

    private NullCompilationDiagnosticCollector() { }

    public bool IsEnabled => false;

    public void Record(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
    }

    public CompilationDiagnostics? GetResult() => null;
}
```

- [ ] **Step 5: Rename RuntimeDiagnosticCollector → TokenizationDiagnosticCollector**

Rename the file and class. Change it to implement `ITokenizationDiagnosticCollector` instead of `IDiagnosticCollector`. Remove the `RecordCompilation` and `GetCompilationResult` methods. Rename `Record`'s first parameter type from the old to `TokenizationEventType` (already done in Task 1).

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Active diagnostic collector that records events during tokenization.
/// Create one instance per tokenization call and pass it through the pipeline.
/// </summary>
internal sealed class TokenizationDiagnosticCollector : ITokenizationDiagnosticCollector
{
    private readonly DiagnosticResult _diagnostics;

    public TokenizationDiagnosticCollector(string? inputContent, bool outOfOrderTokens = false, HashSet<string>? optionalTokenNames = null)
    {
        _diagnostics = new DiagnosticResult(inputContent, outOfOrderTokens, optionalTokenNames);
    }

    public bool IsEnabled => true;

    public void Record(TokenizationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
        _diagnostics.AddEvent(new TokenizationEvent
        {
            Type = type,
            TokenName = tokenName,
            TokenId = tokenId,
            Location = location?.Clone(),
            Value = value,
            Detail = detail,
            DecoratorName = decoratorName,
            DecoratorArgs = decoratorArgs,
        });
    }

    public DiagnosticResult? GetResult() => _diagnostics;
}
```

- [ ] **Step 6: Update CompilationDiagnosticCollector**

Change to implement `ICompilationDiagnosticCollector` instead of `IDiagnosticCollector`. Remove `Record` (runtime) and `GetResult` methods. Rename `RecordCompilation` → `Record`, `GetCompilationResult` → `GetResult`.

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// Active diagnostic collector that records events during template compilation.
/// Create one instance per compilation call and pass it through the pipeline.
/// </summary>
internal sealed class CompilationDiagnosticCollector : ICompilationDiagnosticCollector
{
    private readonly CompilationDiagnostics _compilationDiagnostics;

    public CompilationDiagnosticCollector()
    {
        _compilationDiagnostics = new CompilationDiagnostics();
    }

    public bool IsEnabled => true;

    public void Record(CompilationEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
        _compilationDiagnostics.AddEvent(new CompilationEvent
        {
            Type = type,
            TokenName = tokenName,
            TokenId = tokenId,
            Location = location?.Clone(),
            Value = value,
            Detail = detail,
            DecoratorName = decoratorName,
            DecoratorArgs = decoratorArgs,
        });
    }

    public CompilationDiagnostics? GetResult() => _compilationDiagnostics;
}
```

- [ ] **Step 7: Update all tokenization call sites**

Replace `IDiagnosticCollector` → `ITokenizationDiagnosticCollector` in these files:
- `src/Tokenizer/Tokenization/DecoratorPipeline.cs` (field, constructor, property)
- `src/Tokenizer/Tokenization/TokenizationSession.cs` (field, constructor)
- `src/Tokenizer/Tokenization/CandidateProcessor.cs` (field, constructor)
- `src/Tokenizer/Tokenization/TokenMatchRouter.cs` (field, constructor)
- `src/Tokenizer/Tokenization/ResultBuilder.cs` (method parameter)
- `src/Tokenizer/Tokenization/IResultBuilder.cs` (method parameter)
- `src/Tokenizer/Tokenization/TokenizationEngine.cs` (method parameter)
- `src/Tokenizer/Tokenization/ITokenizationEngine.cs` (method parameter)
- `src/Tokenizer/Tokenization/IHintStrategy.cs` (method parameter)
- `src/Tokenizer/Tokenization/Strategies/UpfrontHintStrategy.cs` (method parameter)
- `src/Tokenizer/Tokenization/Strategies/StreamingHintStrategy.cs` (method parameter)

Also update `NullDiagnosticCollector.Instance` → `NullTokenizationDiagnosticCollector.Instance` in:
- `src/Tokenizer/Tokenizer.cs` (line ~240)

And `RuntimeDiagnosticCollector` → `TokenizationDiagnosticCollector` in:
- `src/Tokenizer/Tokenizer.cs` (line ~236)

- [ ] **Step 8: Update all compilation call sites**

Replace `IDiagnosticCollector` → `ICompilationDiagnosticCollector` in these files:
- `src/Tokenizer/Compilation/Binders/HintBinder.cs`
- `src/Tokenizer/Compilation/Binders/TagBinder.cs`
- `src/Tokenizer/Compilation/Binders/TokenFactory.cs`
- `src/Tokenizer/Compilation/Binders/OptionApplier.cs`
- `src/Tokenizer/Compilation/Binders/RepeatingTokenLinker.cs`
- `src/Tokenizer/Compilation/Binders/DecoratorBinder.cs`
- `src/Tokenizer/Compilation/Binders/TokenBinder.cs`

In these files, also rename `.RecordCompilation(` → `.Record(` (since the method is now just `Record` on `ICompilationDiagnosticCollector`).

Update `TemplateCompiler.cs`:
- `NullDiagnosticCollector.Instance` → `NullCompilationDiagnosticCollector.Instance`
- `CompilationDiagnosticCollector` constructor stays the same
- `.GetCompilationResult()` → `.GetResult()`

- [ ] **Step 9: Delete old files**

Delete:
- `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs`
- `src/Tokenizer/Diagnostics/RuntimeDiagnosticCollector.cs` (replaced by `TokenizationDiagnosticCollector.cs`)

- [ ] **Step 10: Update test files**

Update all test files that reference the old types:
- `RuntimeDiagnosticCollector` → `TokenizationDiagnosticCollector`
- `NullDiagnosticCollector` → `NullTokenizationDiagnosticCollector` or `NullCompilationDiagnosticCollector` as appropriate
- `IDiagnosticCollector` → appropriate split interface
- `.RecordCompilation(` → `.Record(` in compilation tests
- `.GetCompilationResult()` → `.GetResult()` in compilation tests

Key test files:
- `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/CompilationDiagnosticsTests.cs`
- `tests/Tokenizer.Tests/Compilation/Binders/*.cs`

- [ ] **Step 11: Build and test**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: 0 warnings, all tests pass.

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "Split IDiagnosticCollector into ITokenizationDiagnosticCollector and ICompilationDiagnosticCollector"
```

---

### Task 3: Rename DiagnosticResult → TokenizationDiagnostics

**Files:**
- Rename: `src/Tokenizer/Diagnostics/DiagnosticResult.cs` → `TokenizationDiagnostics.cs`
- Modify: All files referencing `DiagnosticResult`

**Interfaces:**
- Consumes: `ITokenizationDiagnosticCollector.GetResult()` (currently returns `DiagnosticResult?`)
- Produces: `TokenizationDiagnostics` class (same API, new name)

- [ ] **Step 1: Rename class and file**

Rename `src/Tokenizer/Diagnostics/DiagnosticResult.cs` to `TokenizationDiagnostics.cs`. Rename the class from `DiagnosticResult` to `TokenizationDiagnostics`.

- [ ] **Step 2: Project-wide find/replace**

Replace `DiagnosticResult` → `TokenizationDiagnostics` across all `.cs` files in `src/` and `tests/`.

Key files affected:
- `src/Tokenizer/Diagnostics/ITokenizationDiagnosticCollector.cs` (return type)
- `src/Tokenizer/Diagnostics/TokenizationDiagnosticCollector.cs` (field type)
- `src/Tokenizer/Diagnostics/NullTokenizationDiagnosticCollector.cs` (return type)
- `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` (parameter type)
- `src/Tokenizer/Diagnostics/IssueFactory.cs` (parameter type)
- `src/Tokenizer/Diagnostics/AlignmentRenderer.cs` (parameter type)
- `src/Tokenizer/Diagnostics/ProcessingOrderRenderer.cs` (parameter type)
- `src/Tokenizer/Diagnostics/Hints/*.cs` (parameter type in all 9 generators)
- `src/Tokenizer/Tokenizer.cs` (local variable type, result property)
- `src/Tokenizer/TokenizeResult.cs` (property type — this is public API)
- All test files referencing `DiagnosticResult`

- [ ] **Step 3: Build and test**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Rename DiagnosticResult to TokenizationDiagnostics for naming consistency"
```

---

### Task 4: TokenDiagnostic → Sealed Record

**Files:**
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnostic.cs`
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` (use `with` in ApplyBlockedAnnotations)

**Interfaces:**
- Produces: `sealed record TokenDiagnostic` with same properties

- [ ] **Step 1: Convert to sealed record**

Replace content of `src/Tokenizer/Diagnostics/TokenDiagnostic.cs`:

```csharp
using Tokens.Enumerators;

namespace Tokens.Diagnostics;

/// <summary>
/// The complete diagnostic story for a single token during tokenization.
/// </summary>
public sealed record TokenDiagnostic
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
    /// The name of the token that blocked this one from being searched,
    /// or null if this token was not blocked. Only populated when
    /// <see cref="Outcome"/> is <see cref="TokenOutcome.Blocked"/>.
    /// </summary>
    public string? BlockedBy { get; init; }

    /// <summary>
    /// Issues identified for this token (with adaptive hints).
    /// </summary>
    public IReadOnlyList<DiagnosticIssue> Issues { get; init; } = [];
}
```

- [ ] **Step 2: Update ApplyBlockedAnnotations to use `with`**

In `TokenDiagnosticBuilder.cs`, find the `ApplyBlockedAnnotations` method where it creates a new `TokenDiagnostic`. Replace:

```csharp
tokens[i] = new TokenDiagnostic
{
    TokenName = token.TokenName,
    TokenId = token.TokenId,
    Outcome = TokenOutcome.Blocked,
    BlockedBy = blockerName,
    Attempts = token.Attempts,
    AssignedValue = token.AssignedValue,
    AssignedLocation = token.AssignedLocation,
    Issues = new List<DiagnosticIssue>(token.Issues)
    {
        issueFactory.CreateBlocked(token.TokenName, blockerName, diagnostics),
    },
};
```

With:

```csharp
tokens[i] = token with
{
    Outcome = TokenOutcome.Blocked,
    BlockedBy = blockerName,
    Issues = new List<DiagnosticIssue>(token.Issues)
    {
        issueFactory.CreateBlocked(token.TokenName, blockerName, diagnostics),
    },
};
```

- [ ] **Step 3: Build and test**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Diagnostics/TokenDiagnostic.cs src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs
git commit -m "Convert TokenDiagnostic to sealed record, use with-expression in blocked annotation"
```

---

### Task 5: BuildContext + Instance Builder + IssueFactory/IHintGenerator Updates

**Files:**
- Create: `src/Tokenizer/Diagnostics/BuildContext.cs`
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` (static → instance, use BuildContext)
- Modify: `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs` (remove mutable properties, update GetBuilt)
- Modify: `src/Tokenizer/Diagnostics/IssueFactory.cs` (accept BuildContext)
- Modify: `src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs` (accept BuildContext)
- Modify: All 9 hint generators (accept BuildContext)
- Modify: Test files that construct IssueFactory or test hints

**Interfaces:**
- Produces: `BuildContext` class, instance `TokenDiagnosticBuilder`, updated `IHintGenerator` and `IssueFactory` signatures

- [ ] **Step 1: Create BuildContext**

Create `src/Tokenizer/Diagnostics/BuildContext.cs`:

```csharp
namespace Tokens.Diagnostics;

/// <summary>
/// Holds all mutable state needed during the diagnostic build phase.
/// Passed to hint generators and issue factory to eliminate temporal coupling
/// with the immutable <see cref="TokenizationDiagnostics"/> result.
/// </summary>
internal sealed class BuildContext
{
    /// <summary>
    /// The raw input text that was tokenized.
    /// </summary>
    public string? InputContent { get; }

    /// <summary>
    /// Input split by newlines, eagerly computed for hint generators.
    /// </summary>
    public string[] InputLines { get; }

    /// <summary>
    /// Whether the template uses out-of-order token matching.
    /// </summary>
    public bool OutOfOrderTokens { get; }

    /// <summary>
    /// Token names that are optional (won't block subsequent tokens in ordered mode).
    /// </summary>
    public HashSet<string> OptionalTokenNames { get; }

    /// <summary>
    /// Index of rejection events per token name, built during event collection.
    /// </summary>
    public Dictionary<string, List<TokenizationEvent>> RejectionsPerToken { get; }

    /// <summary>
    /// Index of decorator success events per token name, built during event collection.
    /// </summary>
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

- [ ] **Step 2: Update IHintGenerator signature**

In `src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs`:

```csharp
namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates an adaptive hint for a diagnostic issue by analyzing the
/// event context. Returns null if no actionable hint can be produced.
/// </summary>
internal interface IHintGenerator
{
    /// <summary>
    /// Attempts to generate a hint for the given issue.
    /// </summary>
    /// <param name="type">The diagnostic issue type</param>
    /// <param name="tokenName">The name of the token associated with the issue</param>
    /// <param name="sourceEvent">The diagnostic event that caused the issue</param>
    /// <param name="context">The build context for cross-referencing</param>
    /// <returns>A human-readable hint string, or null if no hint applies</returns>
    string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                            TokenizationEvent sourceEvent, BuildContext context);
}
```

- [ ] **Step 3: Update all 9 hint generators**

In each hint generator file under `src/Tokenizer/Diagnostics/Hints/`, change the signature from:
```csharp
public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                               TokenizationEvent sourceEvent, TokenizationDiagnostics trace)
```
To:
```csharp
public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                               TokenizationEvent sourceEvent, BuildContext context)
```

Then update the body references:
- `trace.InputContent` → `context.InputContent`
- `trace.CachedInputLines` → `context.InputLines` (remove mutation — just read directly)
- `trace.RejectionsPerToken` → `context.RejectionsPerToken`
- `trace.DecoratorSuccessesPerToken` → `context.DecoratorSuccessesPerToken`

**Special: PreambleNearMissHintGenerator** — remove the `CachedInputLines` mutation entirely. Replace:
```csharp
if (trace.CachedInputLines == null)
{
    trace.CachedInputLines = inputContent!.Split('\n');
}
var lines = trace.CachedInputLines;
```
With:
```csharp
var lines = context.InputLines;
```

Files to update:
- `BlockedTokenHintGenerator.cs`
- `ChainedDecoratorHintGenerator.cs`
- `DateFormatHintGenerator.cs`
- `MultipleRejectionHintGenerator.cs`
- `OptionalTokenHintGenerator.cs`
- `PreambleNearMissHintGenerator.cs`
- `RepeatingTokenHintGenerator.cs`
- `ValidatorValueHintGenerator.cs`
- `ValueMismatchHintGenerator.cs`

- [ ] **Step 4: Update IssueFactory**

In `src/Tokenizer/Diagnostics/IssueFactory.cs`, change `TokenizationDiagnostics diagnostics` → `BuildContext context` in all methods:

```csharp
internal sealed class IssueFactory
{
    private readonly IHintGenerator[] _hintGenerators;

    internal IssueFactory(IHintGenerator[] hintGenerators)
    {
        _hintGenerators = hintGenerators;
    }

    internal DiagnosticIssue Create(DiagnosticIssueType type, TokenizationEvent sourceEvent,
                                    string description, BuildContext context)
    {
        var hint = GenerateHint(type, sourceEvent, context);
        return new DiagnosticIssue
        {
            Type = type,
            TokenName = sourceEvent.TokenName,
            Description = description,
            Location = sourceEvent.Location,
            Hint = hint,
        };
    }

    internal DiagnosticIssue CreateValueMismatch(string tokenName, string missedTokenName, BuildContext context)
    {
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenAssigned,
            TokenName = tokenName,
            Detail = missedTokenName,
        };
        return Create(DiagnosticIssueType.ValueMismatch, sourceEvent,
            $"Token '{tokenName}' captured value containing preamble of token '{missedTokenName}'.",
            context);
    }

    internal DiagnosticIssue CreateBlocked(string tokenName, string blockerName, BuildContext context)
    {
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = tokenName,
            Detail = blockerName,
        };
        return Create(DiagnosticIssueType.Blocked, sourceEvent,
            $"Token '{tokenName}' was not searched for because '{blockerName}' was not matched.",
            context);
    }

    private string? GenerateHint(DiagnosticIssueType type, TokenizationEvent sourceEvent,
                                  BuildContext context)
    {
        foreach (var generator in _hintGenerators)
        {
            var hint = generator.TryGenerateHint(type, sourceEvent.TokenName, sourceEvent, context);
            if (hint != null)
                return hint;
        }
        return null;
    }
}
```

- [ ] **Step 5: Convert TokenDiagnosticBuilder to instance class**

Rewrite `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` as an instance class. The key changes:
- Remove `static` from class declaration
- Add constructor that takes `TokenizationDiagnostics` and creates `BuildContext`
- Change `Build` from `static` to instance method
- All phase methods become private instance methods that use `_context` and `_diagnostics`
- Pass `_context` instead of `diagnostics` to `IssueFactory` methods
- Keep `DefaultIssueFactory` as a `private static readonly` field (stateless — safe to share)
- Add phase ordering documentation comment on `Build()`

The internal structure stays the same — `CollectedEventData` inner class, same phase methods. Just remove `static` and thread `_context`/`_diagnostics` through instance fields instead of parameters.

- [ ] **Step 6: Remove mutable properties from TokenizationDiagnostics**

In `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs` (formerly DiagnosticResult):
- Delete: `internal Dictionary<string, List<TokenizationEvent>>? RejectionsPerToken { get; set; }`
- Delete: `internal Dictionary<string, List<TokenizationEvent>>? DecoratorSuccessesPerToken { get; set; }`
- Delete: `internal string[]? CachedInputLines { get; set; }`

Update `GetBuilt()`:
```csharp
private BuiltResult GetBuilt()
{
    if (_built != null)
        return _built;

    var builder = new TokenDiagnosticBuilder(this);
    var (tokens, verdict, matched, missed, total) = builder.Build();
    _built = new BuiltResult(tokens, verdict, matched, missed, total);
    return _built;
}
```

- [ ] **Step 7: Update tests**

Update test files that:
- Construct `IssueFactory` with test doubles — update `IHintGenerator` mock signatures
- Use `DiagnosticResult.RejectionsPerToken` etc. — these properties no longer exist
- Directly test hint generators — update to pass `BuildContext` instead of `TokenizationDiagnostics`

Key test files:
- `tests/Tokenizer.Tests/Diagnostics/Hints/*.cs` (all hint generator tests)
- `tests/Tokenizer.Tests/Diagnostics/IssueFactoryTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`
- `tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs`

- [ ] **Step 8: Build and test**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Extract BuildContext, convert TokenDiagnosticBuilder to instance class, eliminate mutable state on TokenizationDiagnostics"
```

---

### Task 6: Repeating-Token Count Fix (M1) — TDD

**Files:**
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` (count logic)
- Test: `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`

**Interfaces:**
- Produces: `TotalCount == Tokens.Count` invariant for repeating tokens

- [ ] **Step 1: Write failing tests**

Add to `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`:

```csharp
[Fact]
public void GivenRepeatingTokenWithMultipleMatches_WhenBuilding_ThenCountsAsOneMatchedToken()
{
    // Arrange
    var collector = new TokenizationDiagnosticCollector("Items: one\nItems: two");
    collector.Record(TokenizationEventType.TokenizationStarted);
    collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Items", tokenId: 1, value: "one");
    collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Items", tokenId: 2, value: "two");
    collector.Record(TokenizationEventType.TokenizationCompleted);
    var diagnostics = collector.GetResult()!;

    // Act
    var builder = new TokenDiagnosticBuilder(diagnostics);
    var (tokens, _, matched, missed, total) = builder.Build();

    // Assert
    Assert.Equal(1, tokens.Count);
    Assert.Equal(1, matched);
    Assert.Equal(0, missed);
    Assert.Equal(1, total);
    Assert.Equal(total, tokens.Count);
}

[Fact]
public void GivenRepeatingTokenWithZeroMatches_WhenBuilding_ThenCountsAsOneMissedToken()
{
    // Arrange
    var collector = new TokenizationDiagnosticCollector("nothing");
    collector.Record(TokenizationEventType.TokenizationStarted);
    collector.Record(TokenizationEventType.TokenMissed, tokenName: "Items", tokenId: 1);
    collector.Record(TokenizationEventType.TokenMissed, tokenName: "Items", tokenId: 2);
    collector.Record(TokenizationEventType.TokenizationCompleted);
    var diagnostics = collector.GetResult()!;

    // Act
    var builder = new TokenDiagnosticBuilder(diagnostics);
    var (tokens, _, matched, missed, total) = builder.Build();

    // Assert
    Assert.Equal(1, tokens.Count);
    Assert.Equal(0, matched);
    Assert.Equal(1, missed);
    Assert.Equal(1, total);
    Assert.Equal(total, tokens.Count);
}
```

- [ ] **Step 2: Run tests to verify failure**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenDiagnosticBuilderTests.GivenRepeatingToken"
```

Expected: FAIL (MatchedCount will be 2 instead of 1, TotalCount will be 2 instead of 1)

- [ ] **Step 3: Fix the counting logic**

In `TokenDiagnosticBuilder.CollectEvents()`, change the `TokenAssigned` case:

```csharp
case TokenizationEventType.TokenAssigned:
    if (evt.TokenName != null)
    {
        if (!data.AssignedTokens.ContainsKey(evt.TokenName))
        {
            data.MatchedCount++;
        }
        data.AssignedTokens[evt.TokenName] = (evt.Value, evt.Location);
        AddAttempt(data.Attempts, evt.TokenName, new TokenAttempt
        {
            Location = evt.Location,
            Value = evt.Value,
            Outcome = AttemptOutcome.Assigned,
        });
    }
    break;
```

And change the `TokenMissed` case — only increment `MissedCount` if the token hasn't already been assigned AND hasn't already been counted as missed:

```csharp
case TokenizationEventType.TokenMissed:
    if (evt.TokenName != null)
    {
        if (!data.AssignedTokens.ContainsKey(evt.TokenName) && data.MissedTokenNames.Add(evt.TokenName))
        {
            data.MissedCount++;
        }
        // ... rest of case (preamble text, issue creation) stays the same
        // but guard issue creation with the same "first miss" check
    }
    break;
```

Note: `MissedTokenNames.Add()` returns `false` if already present — use this as the dedup gate. Also ensure issue creation (PreambleNeverFound) only fires on first miss per name.

- [ ] **Step 4: Run tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs
git commit -m "Fix repeating-token count: deduplicate by name so TotalCount == Tokens.Count"
```

---

### Task 7: Warning Log Guard + Documentation (L1, M4, D7)

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs` (log guard)
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs` (comments)

- [ ] **Step 1: Add IsEnabled(LogLevel.Warning) guard**

In `src/Tokenizer/Tokenizer.cs`, `FinalizeTokenization` method, change:

```csharp
if (result.Diagnostics.MissedCount > 0)
```

To:

```csharp
if (_log.IsEnabled(LogLevel.Warning) && result.Diagnostics.MissedCount > 0)
```

- [ ] **Step 2: Add blocked annotation documentation**

In `TokenDiagnosticBuilder.cs`, in `ApplyBlockedAnnotations`, add comment before the `if (token.Outcome == TokenOutcome.NeverFound)` check:

```csharp
// Only NeverFound tokens are reclassified as Blocked. Rejected tokens were
// actively attempted and carry their own diagnostic value (validator feedback, hints).
if (token.Outcome == TokenOutcome.NeverFound)
```

- [ ] **Step 3: Add complexity documentation**

In `TokenDiagnosticBuilder.cs`, add XML doc on `ApplyValueMismatchIssues`:

```csharp
/// <summary>
/// Detects tokens whose assigned value contains the preamble of a missed/rejected token.
/// Complexity: O(matched × missed × value_length). Bounded by template token count
/// (typically &lt;50) and short preamble/value strings. Acceptable at current scale.
/// </summary>
private void ApplyValueMismatchIssues(CollectedEventData data)
```

- [ ] **Step 4: Build and test**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs
git commit -m "Add warning log guard, document blocked annotation semantics and value mismatch complexity"
```

---

### Task 8: [Theory] Test Refactor (D6)

**Files:**
- Modify: `tests/Tokenizer.Tests/Diagnostics/Characterisation/ValidatorRejectionTests.cs`

- [ ] **Step 1: Refactor structurally identical tests to Theory**

The file has tests like `GivenIsEmailValidator_WhenValueIsInvalid_...`, `GivenIsNumericValidator_WhenValueIsText_...`, `GivenIsPhoneNumberValidator_WhenValueIsGibberish_...` that all follow the same pattern: compile template with validator, tokenize invalid input, assert `ValidatorFailed` event with expected decorator name.

Replace the duplicate rejection tests with a `[Theory]`:

```csharp
public static IEnumerable<object[]> ValidatorRejectionCases => new List<object[]>
{
    new object[] { "Email: { Email : IsEmail }", "Email: notanemail", "IsEmailValidator" },
    new object[] { "Count: { Count : IsNumeric }", "Count: twelve", "IsNumericValidator" },
    new object[] { "Phone: { Phone : IsPhoneNumber }", "Phone: abc", "IsPhoneNumberValidator" },
};

[Theory]
[MemberData(nameof(ValidatorRejectionCases))]
public void GivenValidatorRejectsValue_WhenTokenizing_ThenDiagnosticsShowRejection(
    string template, string input, string expectedDecoratorName)
{
    // Act
    var result = TokenizeWithDiagnostics(template, input);

    // Assert
    var diagnostics = result.Diagnostics!;
    Assert.Contains(diagnostics.RawEvents,
        e => e.Type == TokenizationEventType.ValidatorFailed
          && string.Equals(e.DecoratorName, expectedDecoratorName, StringComparison.Ordinal));
    Assert.Contains(diagnostics.Tokens.SelectMany(t => t.Issues),
        i => i.Type == DiagnosticIssueType.ValidatorRejection);
}
```

Keep any tests that have UNIQUE assertions (e.g., `GivenIsEmailValidator_WhenValueIsValid_ThenTokenMatchedNoIssues` tests the happy path — that's a different pattern and should stay as a `[Fact]`).

- [ ] **Step 2: Run tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ValidatorRejectionTests"
```

Expected: ALL PASS

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/Characterisation/ValidatorRejectionTests.cs
git commit -m "Refactor structurally identical validator rejection tests to [Theory] with [MemberData]"
```
