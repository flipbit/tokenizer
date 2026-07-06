# CompilationResult Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Surface compilation diagnostics via a `CompilationResult` type and rename `TokenizationDiagnostics` to `DiagnosticResult`.

**Architecture:** Two-phase change: first rename `TokenizationDiagnostics` → `DiagnosticResult` across all source files (no behavioral change), then introduce `CompilationResult` as the return type from `Compile()` and wire up the diagnostic output.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 6.0 dual-target, xUnit

---

### Task 1: Rename TokenizationDiagnostics → DiagnosticResult

**Files:**
- Rename: `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs` → `src/Tokenizer/Diagnostics/DiagnosticResult.cs`
- Modify: `src/Tokenizer/Diagnostics/IDiagnosticCollector.cs`
- Modify: `src/Tokenizer/Diagnostics/DiagnosticCollector.cs`
- Modify: `src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs`
- Modify: `src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs`
- Modify: `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/RepeatingTokenHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/UnmatchedInputHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/ValidatorValueHintGenerator.cs`
- Modify: `src/Tokenizer/Diagnostics/Hints/DateFormatHintGenerator.cs`
- Modify: `src/Tokenizer/TokenizeResultBase.cs`
- Modify: `src/Tokenizer/TokenizerOptions.cs`

This is a pure rename — no behavioral change, no test logic changes needed.

- [ ] **Step 1: Rename the class and file**

Rename `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs` to `src/Tokenizer/Diagnostics/DiagnosticResult.cs`.

In the file, change the class declaration from:
```csharp
public class TokenizationDiagnostics
```
to:
```csharp
public class DiagnosticResult
```

And the constructor from:
```csharp
internal TokenizationDiagnostics(string? templateContent, string? inputContent)
```
to:
```csharp
internal DiagnosticResult(string? templateContent, string? inputContent)
```

- [ ] **Step 2: Update all references in source files**

Replace `TokenizationDiagnostics` with `DiagnosticResult` in all source files listed above. This is a global find-and-replace within `src/Tokenizer/`. The specific changes are:

`src/Tokenizer/Diagnostics/IDiagnosticCollector.cs` line 30:
```csharp
// Before
TokenizationDiagnostics? GetResult();
// After
DiagnosticResult? GetResult();
```

`src/Tokenizer/Diagnostics/DiagnosticCollector.cs` lines 11, 20, 47:
```csharp
// Before
private readonly TokenizationDiagnostics diagnostics;
diagnostics = new TokenizationDiagnostics(templateContent, inputContent);
public TokenizationDiagnostics? GetResult()
// After
private readonly DiagnosticResult diagnostics;
diagnostics = new DiagnosticResult(templateContent, inputContent);
public DiagnosticResult? GetResult()
```

`src/Tokenizer/Diagnostics/NullDiagnosticCollector.cs` line 31:
```csharp
// Before
public TokenizationDiagnostics? GetResult()
// After
public DiagnosticResult? GetResult()
```

`src/Tokenizer/Diagnostics/DiagnosticSummaryBuilder.cs` lines 18, 29, 56:
```csharp
// Replace all occurrences of TokenizationDiagnostics with DiagnosticResult
```

`src/Tokenizer/Diagnostics/AlignmentRenderer.cs` line 7:
```csharp
// Before
public static string Render(TokenizationDiagnostics diagnostics, ...)
// After
public static string Render(DiagnosticResult diagnostics, ...)
```

`src/Tokenizer/Diagnostics/Hints/IHintGenerator.cs` line 19:
```csharp
// Before
TokenizationDiagnostics trace);
// After
DiagnosticResult trace);
```

`src/Tokenizer/Diagnostics/Hints/PreambleNearMissHintGenerator.cs` line 16:
```csharp
// Before
TokenizationDiagnostics trace)
// After
DiagnosticResult trace)
```

`src/Tokenizer/Diagnostics/Hints/RepeatingTokenHintGenerator.cs` line 11:
```csharp
// Before
TokenizationDiagnostics trace)
// After
DiagnosticResult trace)
```

`src/Tokenizer/Diagnostics/Hints/UnmatchedInputHintGenerator.cs` line 11:
```csharp
// Before
TokenizationDiagnostics trace)
// After
DiagnosticResult trace)
```

`src/Tokenizer/Diagnostics/Hints/ValidatorValueHintGenerator.cs` line 11:
```csharp
// Before
TokenizationDiagnostics trace)
// After
DiagnosticResult trace)
```

`src/Tokenizer/Diagnostics/Hints/DateFormatHintGenerator.cs` line 25:
```csharp
// Before
TokenizationDiagnostics trace)
// After
DiagnosticResult trace)
```

`src/Tokenizer/TokenizeResultBase.cs` line 58:
```csharp
// Before
public Diagnostics.TokenizationDiagnostics? Diagnostics { get; internal set; }
// After
public Diagnostics.DiagnosticResult? Diagnostics { get; internal set; }
```

`src/Tokenizer/TokenizerOptions.cs` — update the XML doc comment that references `TokenizationDiagnostics`:
```csharp
// Before
/// When true, tokenization results include a <see cref="Diagnostics.TokenizationDiagnostics"/>
// After
/// When true, tokenization results include a <see cref="Diagnostics.DiagnosticResult"/>
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass — this is a pure rename with no behavioral change.

- [ ] **Step 4: Commit**

```bash
git add -A
git status
git commit -m "refactor: rename TokenizationDiagnostics to DiagnosticResult"
```

Note: use `git add -A` here because the file was renamed (old file deleted, new file created).

---

### Task 2: Create CompilationResult and Update API

**Files:**
- Create: `src/Tokenizer/CompilationResult.cs`
- Create: `tests/Tokenizer.Tests/Compilation/CompilationResultTests.cs`
- Modify: `src/Tokenizer/Compilation/TemplateCompiler.cs`
- Modify: `src/Tokenizer/ITokenizer.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/TokenMatcher.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/CompilationResultTests.cs`:

```csharp
using Tokens.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Compilation;

public class CompilationResultTests : TokenizerTestBase
{
    public CompilationResultTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenCompilationResult_WhenAccessed_ThenTemplateIsAvailable()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var result = tokenizer.Compile("Name: {Name}");

        // Assert
        Assert.NotNull(result.Template);
        Assert.Single(result.Template.Tokens);
    }

    [Fact]
    public void GivenDiagnosticsDisabled_WhenCompiling_ThenDiagnosticsIsNull()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var result = tokenizer.Compile("Name: {Name}");

        // Assert
        Assert.Null(result.Diagnostics);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenCompiling_ThenResultHasDiagnostics()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });

        // Act
        var result = tokenizer.Compile("Name: {Name}");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics.Events, e => e.Type == DiagnosticEventType.CompilationCompleted);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CompilationResultTests"`
Expected: FAIL — `CompilationResult` does not exist, and `Compile()` returns `Template` not `CompilationResult`.

- [ ] **Step 3: Create CompilationResult**

Create `src/Tokenizer/CompilationResult.cs`:

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
    public DiagnosticResult? Diagnostics { get; }

    internal CompilationResult(Template template, DiagnosticResult? diagnostics)
    {
        Template = template;
        Diagnostics = diagnostics;
    }
}
```

- [ ] **Step 4: Update TemplateCompiler to return CompilationResult**

In `src/Tokenizer/Compilation/TemplateCompiler.cs`, change the `Compile` method signature and return statement:

```csharp
public CompilationResult Compile(string content)
{
    IDiagnosticCollector collector = Options.EnableDiagnostics
        ? new DiagnosticCollector(content, null)
        : NullDiagnosticCollector.Instance;

    TemplateLengthValidator.Validate(content, Options);

    try
    {
        var definition = new AstTemplateDefinitionParser().Parse(content, Options);
        var id = content.ComputeHash();
        var template = TemplateFactory.Create(id, definition);

        HintBinder.Bind(definition, template, collector);
        TagBinder.Bind(definition, template, collector);
        TokenBinder.Bind(definition, template, registry, _decoratorCache, collector);
        TokenCountValidator.Validate(template, Options);

        if (collector.IsEnabled)
        {
            collector.Record(DiagnosticEventType.CompilationCompleted,
                detail: $"Template '{template.Name}' compiled with {template.Tokens.Count} token(s)");
        }

        return new CompilationResult(template, collector.GetResult());
    }
    catch (TokenizerException)
    {
        throw;
    }
    catch (Exception ex)
    {
        throw new TokenizerException($"Unexpected error during template compilation: {ex.Message}", ex);
    }
}
```

- [ ] **Step 5: Update ITokenizer interface**

In `src/Tokenizer/ITokenizer.cs`, change the return types:

```csharp
/// <summary>
/// Compiles a template pattern string into a reusable <see cref="Template"/>.
/// </summary>
CompilationResult Compile(string pattern);

/// <summary>
/// Asynchronously compiles a template from a <see cref="TextReader"/>.
/// </summary>
Task<CompilationResult> CompileAsync(TextReader reader, CancellationToken ct = default);

/// <summary>
/// Asynchronously compiles a template from a <see cref="Stream"/>.
/// </summary>
Task<CompilationResult> CompileAsync(Stream input, Encoding encoding, CancellationToken ct = default);
```

- [ ] **Step 6: Update Tokenizer implementation**

In `src/Tokenizer/Tokenizer.cs`, update the three `Compile`/`CompileAsync` methods:

Line 240:
```csharp
// Before
public Template Compile(string pattern) => parser.Compile(pattern);
// After
public CompilationResult Compile(string pattern) => parser.Compile(pattern);
```

Lines 243-247:
```csharp
// Before
public async Task<Template> CompileAsync(TextReader reader, CancellationToken ct = default)
{
    var content = await ReadToEndAsync(reader, ct, Options.MaxTemplateLength).ConfigureAwait(false);
    return parser.Compile(content);
}
// After
public async Task<CompilationResult> CompileAsync(TextReader reader, CancellationToken ct = default)
{
    var content = await ReadToEndAsync(reader, ct, Options.MaxTemplateLength).ConfigureAwait(false);
    return parser.Compile(content);
}
```

The `CompileAsync(Stream, ...)` overload similarly:
```csharp
// Before
public async Task<Template> CompileAsync(Stream input, Encoding encoding, CancellationToken ct = default)
// After
public async Task<CompilationResult> CompileAsync(Stream input, Encoding encoding, CancellationToken ct = default)
```

- [ ] **Step 7: Update TokenMatcher**

In `src/Tokenizer/TokenMatcher.cs`, update the two `RegisterTemplate` methods that call `Compile`:

Lines 134-141:
```csharp
public ITokenMatcher RegisterTemplate(string content)
{
    var result = tokenizer.Compile(content);

    Templates.Add(result.Template);

    return this;
}
```

Lines 149-157:
```csharp
public ITokenMatcher RegisterTemplate(string content, string name)
{
    var result = tokenizer.Compile(content);
    result.Template.Name = name;

    Templates.Add(result.Template);

    return this;
}
```

- [ ] **Step 8: Run new tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CompilationResultTests"`
Expected: All 3 tests pass.

- [ ] **Step 9: Commit**

```bash
git add src/Tokenizer/CompilationResult.cs src/Tokenizer/Compilation/TemplateCompiler.cs src/Tokenizer/ITokenizer.cs src/Tokenizer/Tokenizer.cs src/Tokenizer/TokenMatcher.cs tests/Tokenizer.Tests/Compilation/CompilationResultTests.cs
git commit -m "feat: introduce CompilationResult to surface compilation diagnostics"
```

---

### Task 3: Update test files for new Compile() return type

**Files:**
- Modify: All 36 test files that use `var template = ...Compile(...)`

Every test that stores the result of `.Compile()` in a variable now receives a `CompilationResult` instead of a `Template`. These tests need to access `.Template` from the result.

- [ ] **Step 1: Update TemplateCompilerTests**

In `tests/Tokenizer.Tests/Compilation/TemplateCompilerTests.cs`, every `parser.Compile(...)` now returns `CompilationResult`. Change each `var template = parser.Compile(...)` to `var template = parser.Compile(...).Template`:

```csharp
// For each test, change:
var template = parser.Compile("...");
// To:
var template = parser.Compile("...").Template;
```

Apply to all 10 tests that store the result. The exception test on line 111 doesn't store a result so it stays unchanged.

- [ ] **Step 2: Update CompileApiTests**

In `tests/Tokenizer.Tests/Compilation/CompileApiTests.cs`:

```csharp
// Before
var template = tokenizer.Compile(pattern);
// After
var template = tokenizer.Compile(pattern).Template;
```

- [ ] **Step 3: Update all remaining test files**

Apply the same pattern to all 34 remaining test files that use `var template = ...Compile(...)`. In each case, append `.Template` to the `Compile()` call:

```csharp
// Before
var template = tokenizer.Compile("...");
// or
var template = CreateTokenizer().Compile("...");
// After  
var template = tokenizer.Compile("...").Template;
// or
var template = CreateTokenizer().Compile("...").Template;
```

The full list of files to update (besides TemplateCompilerTests and CompileApiTests already done above):

1. `tests/Tokenizer.Tests/TemplateCollectionTests.cs`
2. `tests/Tokenizer.Tests/TemplateTests.cs`
3. `tests/Tokenizer.Tests/TokenizerTests.cs`
4. `tests/Tokenizer.Tests/Compilation/Parsing/Binding/TemplateBinderIdAssignmentTests.cs`
5. `tests/Tokenizer.Tests/Compilation/Binders/FrontMatterBinderTests.cs`
6. `tests/Tokenizer.Tests/TokenizerOptionsRegistrationTests.cs`
7. `tests/Tokenizer.Tests/TokenizerOptionsTests.cs`
8. `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEmptyPreambleTests.cs`
9. `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEnginePerformanceTests.cs`
10. `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineBasicTests.cs`
11. `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineEdgeCaseTests.cs`
12. `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineStateTests.cs`
13. `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineTokenMatchingTests.cs`
14. `tests/Tokenizer.Tests/Tokenization/ResultBuilder_Unmatched_Tests.cs`
15. `tests/Tokenizer.Tests/Tokenization/Integration/TokenizationEngineIntegrationTests.cs`
16. `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs`
17. `tests/Tokenizer.Tests/TokenizerAsyncTests.cs`
18. `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`
19. `tests/Tokenizer.Tests/AllocationOptimizationTests.cs`
20. `tests/Tokenizer.Tests/Validators/IsNumericValidatorTests.cs`
21. `tests/Tokenizer.Tests/Transformers/SetTransformerTests.cs`
22. `tests/Tokenizer.Tests/Transformers/ToDateTimeUtcTransformerTests.cs`
23. `tests/Tokenizer.Tests/Diagnostics/DiagnosticLoggingTests.cs`
24. `tests/Tokenizer.Tests/Integration/DependencyInjectionTests.cs`
25. `tests/Tokenizer.Tests/Types/EnumTests.cs`
26. `tests/Tokenizer.Tests/Types/BoolTests.cs`
27. `tests/Tokenizer.Tests/MultilineTests.cs`
28. `tests/Tokenizer.Tests/HintTests.cs`
29. `tests/Tokenizer.Tests/ListTests.cs`
30. `tests/Tokenizer.Tests/TokenPropertyImmutabilityTests.cs`
31. `tests/Tokenizer.Tests/TokenizerOptionsRecordTests.cs`
32. `tests/Tokenizer.Tests/SplitTests.cs`
33. `tests/Tokenizer.Tests/ConcatenationTests.cs`
34. `tests/Tokenizer.Tests/SampleTests.cs`

For each file, find all lines matching `var template = ...Compile(` and append `.Template` after the closing `)`.

Also check for `CompileAsync` calls that store the result as a template variable and apply the same `.Template` suffix. Specifically, `tests/Tokenizer.Tests/CompileAsyncTests.cs` has both sync and async `Compile` calls that need `.Template`:

```csharp
// Lines 26, 40: async calls
var template = (await _tokenizer.CompileAsync(reader)).Template;
var template = (await _tokenizer.CompileAsync(stream, Encoding.UTF8)).Template;

// Line 65: sync call
var syncTemplate = _tokenizer.Compile(pattern).Template;

// Lines 69: async call
var asyncTemplate = (await _tokenizer.CompileAsync(reader)).Template;
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add tests/
git commit -m "refactor: update test files for CompilationResult return type"
```

---

### Task 4: Final Verification

**Files:** None (verification only)

- [ ] **Step 1: Build in Release mode**

Run: `dotnet build src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with no warnings.

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 3: Verify no remaining references to TokenizationDiagnostics**

Run: `grep -r "TokenizationDiagnostics" src/ tests/ --include="*.cs"`
Expected: No matches (only doc/plan files may still reference it).

- [ ] **Step 4: Verify CompilationResult is accessible**

Run: `grep -rn "CompilationResult" src/Tokenizer/ --include="*.cs"`
Expected: References in `CompilationResult.cs`, `ITokenizer.cs`, `Tokenizer.cs`, `TemplateCompiler.cs`.

- [ ] **Step 5: Commit any final cleanup**

If any fixes were needed:
```bash
git add -A
git commit -m "chore: final cleanup after CompilationResult introduction"
```
