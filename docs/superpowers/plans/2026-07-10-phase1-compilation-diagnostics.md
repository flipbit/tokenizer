# Phase 1: Separate Compilation from Runtime Diagnostics

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give compilation diagnostics their own type (`CompilationDiagnostics`) so runtime `DiagnosticResult` no longer contains compilation events.

**Architecture:** The compilation and runtime collectors are already independent — `TemplateCompiler.Compile()` creates its own `DiagnosticCollector` separate from the runtime `Tokenizer.RunCoreAsync()` collector. Both currently produce a `DiagnosticResult`. This phase introduces `CompilationDiagnostics` as the compilation-specific type, updates `CompilationResult` to use it, and moves compilation event types into a separate enum. The runtime `DiagnosticResult` is unchanged.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 8.0 + .NET 10.0, xUnit 2.9.3

## Global Constraints

- Targets: .NET Standard 2.0, .NET 8.0, .NET 10.0
- Root namespace: `Tokens` (not `Tokenizer`)
- Braces: Allman style
- Private fields: `_camelCase`
- File-scoped namespace declarations
- No `#region`
- `internal` access for implementation types; `public` for API surface
- Test naming: Gherkin `GivenScenario_WhenAction_ThenResult()`
- All 1665 existing tests must continue to pass

---

### Task 1: Create CompilationDiagnostics type and update CompilationResult

**Files:**
- Create: `src/Tokenizer/Diagnostics/CompilationDiagnostics.cs`
- Modify: `src/Tokenizer/CompilationResult.cs`
- Modify: `src/Tokenizer/Compilation/TemplateCompiler.cs`
- Modify: `src/Tokenizer/Diagnostics/DiagnosticCollector.cs`
- Test: `tests/Tokenizer.Tests/Compilation/CompilationResultTests.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/CompilationDiagnosticsTests.cs` (new)

**Interfaces:**
- Consumes: `DiagnosticEvent`, `DiagnosticEventType`, `IDiagnosticCollector`, `DiagnosticCollector`
- Produces: `CompilationDiagnostics` class (public), `CompilationResult.Diagnostics` changes type from `DiagnosticResult?` to `CompilationDiagnostics?`

- [ ] **Step 1: Write a test for CompilationDiagnostics**

Create `tests/Tokenizer.Tests/Diagnostics/CompilationDiagnosticsTests.cs`:

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics;

public class CompilationDiagnosticsTests : TokenizerTestBase
{
    public CompilationDiagnosticsTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenCompiling_ThenCompilationDiagnosticsHasEvents()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });

        // Act
        var result = tokenizer.Compile("Name: { Name }");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics!.Events.Count > 0);
        Assert.Contains(result.Diagnostics.Events,
            e => e.Type == DiagnosticEventType.CompilationCompleted);
    }

    [Fact]
    public void GivenDiagnosticsDisabled_WhenCompiling_ThenCompilationDiagnosticsIsNull()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var result = tokenizer.Compile("Name: { Name }");

        // Assert
        Assert.Null(result.Diagnostics);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenCompiling_ThenEventsContainOnlyCompilationEvents()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });

        // Act
        var result = tokenizer.Compile("Name: { Name : IsEmail }");

        // Assert
        var diagnostics = result.Diagnostics!;
        // Should contain compilation events
        Assert.Contains(diagnostics.Events, e => e.Type == DiagnosticEventType.TokenCreated);
        Assert.Contains(diagnostics.Events, e => e.Type == DiagnosticEventType.DecoratorApplied);
        Assert.Contains(diagnostics.Events, e => e.Type == DiagnosticEventType.CompilationCompleted);
        // Should NOT contain runtime events
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.TokenizationStarted);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.PreambleMatched);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.TokenAssigned);
    }
}
```

- [ ] **Step 2: Run to verify tests fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CompilationDiagnosticsTests" -v n`

Expected: Compile error — `CompilationDiagnostics` type doesn't exist yet, `result.Diagnostics` is `DiagnosticResult?` not `CompilationDiagnostics?`.

- [ ] **Step 3: Create CompilationDiagnostics class**

Create `src/Tokenizer/Diagnostics/CompilationDiagnostics.cs`:

```csharp
namespace Tokens.Diagnostics;

/// <summary>
/// Contains diagnostic events recorded during template compilation.
/// Separate from runtime <see cref="DiagnosticResult"/> which covers tokenization.
/// </summary>
public sealed class CompilationDiagnostics
{
    private readonly List<DiagnosticEvent> _events;

    internal CompilationDiagnostics()
    {
        _events = new List<DiagnosticEvent>();
    }

    /// <summary>
    /// All events recorded during compilation, in the order they occurred.
    /// </summary>
    public IReadOnlyList<DiagnosticEvent> Events => _events;

    internal void AddEvent(DiagnosticEvent evt) => _events.Add(evt);
}
```

- [ ] **Step 4: Add GetCompilationResult() to IDiagnosticCollector and implementations**

Modify `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` — add a new method:

```csharp
    /// <summary>
    /// Returns the collected compilation diagnostics, or null if collection is disabled.
    /// </summary>
    public CompilationDiagnostics? GetCompilationResult();
```

Modify `src/Tokenizer/Diagnostics/DiagnosticCollector.cs` — add the field and method:

```csharp
internal sealed class DiagnosticCollector : IDiagnosticCollector
{
    private readonly DiagnosticResult? _diagnostics;
    private readonly CompilationDiagnostics? _compilationDiagnostics;

    /// <summary>
    /// Initialises a collector for runtime tokenization.
    /// </summary>
    public DiagnosticCollector(string? inputContent)
    {
        _diagnostics = new DiagnosticResult(inputContent);
    }

    /// <summary>
    /// Initialises a collector for compilation.
    /// </summary>
    public DiagnosticCollector()
    {
        _compilationDiagnostics = new CompilationDiagnostics();
    }

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                       FileLocation? location = null, string? value = null, string? detail = null,
                       string? decoratorName = null, string[]? decoratorArgs = null)
    {
        var evt = new DiagnosticEvent
        {
            Type = type,
            TokenName = tokenName,
            TokenId = tokenId,
            Location = location?.Clone(),
            Value = value,
            Detail = detail,
            DecoratorName = decoratorName,
            DecoratorArgs = decoratorArgs,
        };

        if (_diagnostics != null)
            _diagnostics.AddEvent(evt);
        else
            _compilationDiagnostics!.AddEvent(evt);
    }

    /// <inheritdoc />
    public DiagnosticResult? GetResult() => _diagnostics;

    /// <inheritdoc />
    public CompilationDiagnostics? GetCompilationResult() => _compilationDiagnostics;
}
```

Modify `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs` — add the method:

```csharp
    /// <inheritdoc />
    public CompilationDiagnostics? GetCompilationResult() => null;
```

- [ ] **Step 5: Update TemplateCompiler to use compilation collector**

Modify `src/Tokenizer/Compilation/TemplateCompiler.cs` line 34 — change the collector creation:

```csharp
        IDiagnosticCollector collector = Options.EnableDiagnostics
            ? new DiagnosticCollector()  // compilation collector (no inputContent)
            : NullDiagnosticCollector.Instance;
```

And line 61 — change the result construction:

```csharp
            return new CompilationResult(template, collector.GetCompilationResult());
```

And line 66 — change the exception data attachment:

```csharp
            ex.Data["CompilationDiagnostics"] = collector.GetCompilationResult();
```

- [ ] **Step 6: Update CompilationResult to use CompilationDiagnostics**

Modify `src/Tokenizer/CompilationResult.cs`:

```csharp
using Tokens.Diagnostics;

namespace Tokens;

/// <summary>
/// Holds the result of compiling a template pattern string,
/// including the compiled template and optional diagnostics.
/// </summary>
public sealed class CompilationResult
{
    /// <summary>
    /// The compiled template.
    /// </summary>
    public Template Template { get; }

    /// <summary>
    /// Structured diagnostic output from the compilation process.
    /// Null when <see cref="TokenizerOptions.EnableDiagnostics"/> is false.
    /// </summary>
    public CompilationDiagnostics? Diagnostics { get; }

    internal CompilationResult(Template template, CompilationDiagnostics? diagnostics)
    {
        Template = template;
        Diagnostics = diagnostics;
    }
}
```

- [ ] **Step 7: Update CompilationResultTests to use CompilationDiagnostics**

Modify `tests/Tokenizer.Tests/Compilation/CompilationResultTests.cs` — the third test references `result.Diagnostics.Events` which now returns `CompilationDiagnostics` instead of `DiagnosticResult`. The test should still work since both types expose `Events`. Verify the test compiles and passes.

- [ ] **Step 8: Update existing binder tests that use DiagnosticCollector for compilation**

These tests create a `DiagnosticCollector(inputContent: null)` and call binder methods directly. They need to use the new compilation constructor `DiagnosticCollector()` and call `GetCompilationResult()` instead of `GetResult()`.

Files to update (each follows the same pattern — change constructor and GetResult call):

**`tests/Tokenizer.Tests/Compilation/Binders/TagBinderTests.cs`** — find the diagnostic test method, change:
- `new DiagnosticCollector(inputContent: null)` → `new DiagnosticCollector()`
- `collector.GetResult()!` → `collector.GetCompilationResult()!`

**`tests/Tokenizer.Tests/Compilation/Binders/OptionApplierTests.cs`** — same changes.

**`tests/Tokenizer.Tests/Compilation/Binders/HintBinderTests.cs`** — same changes.

**`tests/Tokenizer.Tests/Compilation/Binders/RepeatingTokenLinkerTests.cs`** — same changes.

- [ ] **Step 9: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v n`

Expected: All tests pass. The characterisation tests in `Diagnostics/Characterisation/` access `result.Diagnostics` on `TokenizeResult` (runtime), which is unchanged.

- [ ] **Step 10: Commit**

```bash
git add src/Tokenizer/Diagnostics/CompilationDiagnostics.cs src/Tokenizer/Diagnostics/IDiagnosticCollector.cs src/Tokenizer/Diagnostics/DiagnosticCollector.cs src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs src/Tokenizer/CompilationResult.cs src/Tokenizer/Compilation/TemplateCompiler.cs tests/Tokenizer.Tests/Diagnostics/CompilationDiagnosticsTests.cs tests/Tokenizer.Tests/Compilation/CompilationResultTests.cs tests/Tokenizer.Tests/Compilation/Binders/TagBinderTests.cs tests/Tokenizer.Tests/Compilation/Binders/OptionApplierTests.cs tests/Tokenizer.Tests/Compilation/Binders/HintBinderTests.cs tests/Tokenizer.Tests/Compilation/Binders/RepeatingTokenLinkerTests.cs
git commit -m "Separate compilation diagnostics from runtime diagnostics

Introduce CompilationDiagnostics type for compilation events. CompilationResult
now exposes CompilationDiagnostics? instead of DiagnosticResult?. Runtime
DiagnosticResult is unchanged. DiagnosticCollector gains a compilation-mode
constructor."
```

---

### Task 2: Verify runtime DiagnosticResult no longer contains compilation events

**Files:**
- Test: `tests/Tokenizer.Tests/Diagnostics/Characterisation/DiagnosticOutputFormatTests.cs` (modify existing)
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs` (modify existing)

**Interfaces:**
- Consumes: `CompilationDiagnostics`, `DiagnosticResult`, `DiagnosticEventType`
- Produces: Test verification that the two diagnostic streams are properly separated

- [ ] **Step 1: Add a test verifying runtime diagnostics have no compilation events**

Add to `tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs`:

```csharp
    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenizing_ThenRuntimeDiagnosticsContainNoCompilationEvents()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = "Name: { Name }";
        var input = "Name: John";

        // Act
        var compiled = tokenizer.Compile(template).Template;
        var result = tokenizer.Tokenize(compiled, input);

        // Assert
        var diagnostics = result.Diagnostics!;
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.TokenCreated);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.DecoratorApplied);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.OptionApplied);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.ConcatenationApplied);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.TagAdded);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.HintAdded);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.RepeatingTokenLinked);
        Assert.DoesNotContain(diagnostics.Events, e => e.Type == DiagnosticEventType.CompilationCompleted);
    }
```

- [ ] **Step 2: Run the test**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenDiagnosticsEnabled_WhenTokenizing_ThenRuntimeDiagnosticsContainNoCompilationEvents" -v n`

Expected: PASS — the runtime collector was already separate from the compilation collector.

- [ ] **Step 3: Run full test suite to verify no regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v n`

Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs
git commit -m "Add test verifying runtime diagnostics exclude compilation events"
```

---

### Task 3: Verify Phase 0 characterisation tests still pass and update any that referenced compilation events

**Files:**
- Test: `tests/Tokenizer.Tests/Diagnostics/Characterisation/` (all fixture files)

**Interfaces:**
- Consumes: Phase 0 characterisation test suite, `DiagnosticResult`, `CompilationDiagnostics`
- Produces: Verification that Phase 0 tests are unaffected

- [ ] **Step 1: Run the full characterisation suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "Tokens.Diagnostics.Characterisation" -v n`

Expected: All 61 tests pass. These tests access `TokenizeResult.Diagnostics` (runtime), not `CompilationResult.Diagnostics` (compilation). Since runtime `DiagnosticResult` type is unchanged, no test modifications should be needed.

- [ ] **Step 2: Grep characterisation tests for any compilation event references**

Run: `grep -rn "CompilationCompleted\|TokenCreated\|DecoratorApplied\|OptionApplied\|ConcatenationApplied\|TagAdded\|HintAdded\|RepeatingTokenLinked" tests/Tokenizer.Tests/Diagnostics/Characterisation/`

Expected: No matches — characterisation tests don't assert on compilation events.

- [ ] **Step 3: Run full test suite one final time**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v n`

Expected: All tests pass. No commit needed — this is verification only.
