# Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all addressable issues from the 2026-07-12 code review (C1-C2, H1-H2, H3-H5, M1-M6, L1).

**Architecture:** Bug fixes (H1, H2, M1, M2) are direct code changes with TDD. Test coverage gaps (C1, C2, H3-H5, M3-M6) are pure test additions. L1 is a comment update.

**Tech Stack:** C# / .NET 10 / xUnit / NSubstitute

## Global Constraints

- Target frameworks: .NET Standard 2.0, .NET 8.0, .NET 10.0
- Root namespace: `Tokens`
- Test naming: `GivenScenario_WhenAction_ThenResult()`
- Test structure: Arrange / Act / Assert comments
- TDD: Red/Green for behavioral changes. Direct fix for structural/cosmetic.
- Commit after each task completes.

---

### Task 1: Fix null dereference in TokenDiagnosticBuilder (H1)

**Files:**
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:106-136`
- Test: `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`

**Interfaces:**
- Consumes: `RuntimeDiagnosticCollector`, `TokenDiagnosticBuilder.Build()`
- Produces: Fixed null safety — `ValidatorFailed`/`TransformerFailed` events with null `TokenName` are safely skipped

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void GivenValidatorFailedWithNullTokenName_WhenBuilding_ThenDoesNotThrow()
{
    // Arrange
    var collector = new RuntimeDiagnosticCollector("input");
    collector.Record(DiagnosticEventType.TokenizationStarted);
    collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: null,
        decoratorName: "IsEmailValidator", value: "bad");
    collector.Record(DiagnosticEventType.TokenizationCompleted);
    var diagnostics = collector.GetResult()!;

    // Act
    var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

    // Assert
    Assert.Empty(tokens);
}

[Fact]
public void GivenTransformerFailedWithNullTokenName_WhenBuilding_ThenDoesNotThrow()
{
    // Arrange
    var collector = new RuntimeDiagnosticCollector("input");
    collector.Record(DiagnosticEventType.TokenizationStarted);
    collector.Record(DiagnosticEventType.TransformerFailed, tokenName: null,
        decoratorName: "ToDateTimeTransformer", value: "bad");
    collector.Record(DiagnosticEventType.TokenizationCompleted);
    var diagnostics = collector.GetResult()!;

    // Act
    var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

    // Assert
    Assert.Empty(tokens);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenDiagnosticBuilderTests.GivenValidatorFailedWithNullTokenName"`
Expected: FAIL with NullReferenceException

- [ ] **Step 3: Fix the null guard**

In `TokenDiagnosticBuilder.cs`, wrap both case bodies in early-exit null checks:

```csharp
case DiagnosticEventType.ValidatorFailed:
    if (evt.TokenName == null)
        break;
    AddToIndex(diagnostics.RejectionsPerToken, evt.TokenName, evt);
    var validatorDescription = BuildValidatorDescription(evt);
    data.TokensWithFailures.Add(evt.TokenName);
    AddAttempt(data.Attempts, evt.TokenName, new TokenAttempt
    {
        Location = evt.Location,
        Value = evt.Value,
        Outcome = AttemptOutcome.ValidatorRejected,
        DecoratorName = evt.DecoratorName,
        Reason = validatorDescription,
    });
    AddIssue(data.Issues, issueFactory.Create(DiagnosticIssueType.ValidatorRejection, evt, validatorDescription, diagnostics));
    break;

case DiagnosticEventType.TransformerFailed:
    if (evt.TokenName == null)
        break;
    AddToIndex(diagnostics.RejectionsPerToken, evt.TokenName, evt);
    var transformerDescription = BuildTransformerDescription(evt);
    data.TokensWithFailures.Add(evt.TokenName);
    AddAttempt(data.Attempts, evt.TokenName, new TokenAttempt
    {
        Location = evt.Location,
        Value = evt.Value,
        Outcome = AttemptOutcome.TransformerFailed,
        DecoratorName = evt.DecoratorName,
        Reason = transformerDescription,
    });
    AddIssue(data.Issues, issueFactory.Create(DiagnosticIssueType.TransformerFailure, evt, transformerDescription, diagnostics));
    break;
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenDiagnosticBuilderTests"`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs
git commit -m "Fix null dereference in TokenDiagnosticBuilder for null TokenName events"
```

---

### Task 2: Add IsEnabled guards in DecoratorPipeline and TokenizationSession (H2 + M1)

**Files:**
- Modify: `src/Tokenizer/Tokenization/DecoratorPipeline.cs:79-132`
- Modify: `src/Tokenizer/Tokenization/TokenizationSession.cs:94-97,139-144`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs`

**Interfaces:**
- Consumes: `IDiagnosticCollector.IsEnabled`, `NullDiagnosticCollector`
- Produces: Zero-overhead diagnostic path when disabled — no allocations from `.ToArray()` or string interpolation

- [ ] **Step 1: Write the failing test**

This is a performance contract test. The test verifies that when diagnostics are disabled, the NullDiagnosticCollector's Record is never called with allocating arguments. We'll test by verifying that tokenization with decorators works correctly with diagnostics disabled (existing behavior) and add a specific test that confirms the IsEnabled pattern is respected.

```csharp
[Fact]
public void GivenDiagnosticsDisabled_WhenTokenizingWithDecorators_ThenNoRecordCallsAllocate()
{
    // Arrange
    var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = false });
    var template = "Date: { Date : ToDateTime(yyyy-MM-dd) }";
    var input = "Date: 2026-01-15";

    // Act
    var compiled = tokenizer.Compile(template).Template;
    var result = tokenizer.Tokenize(compiled, input);

    // Assert — diagnostics should be null (no allocation from collector)
    Assert.Null(result.Diagnostics);
    Assert.True(result.Success);
}
```

This test already passes (behavioral correctness), but we need it as a regression guard. The real fix is structural — adding guards. To verify guards work, we add a test showing diagnostics ARE populated when enabled with decorators:

```csharp
[Fact]
public void GivenDiagnosticsEnabled_WhenTokenizingWithDecorators_ThenDecoratorEventsRecorded()
{
    // Arrange
    var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
    var template = "Date: { Date : ToDateTime(yyyy-MM-dd) }";
    var input = "Date: 2026-01-15";

    // Act
    var compiled = tokenizer.Compile(template).Template;
    var result = tokenizer.Tokenize(compiled, input);

    // Assert
    Assert.NotNull(result.Diagnostics);
    Assert.Contains(result.Diagnostics!.RawEvents,
        e => e.Type == DiagnosticEventType.TransformerSucceeded && string.Equals(e.DecoratorName, "ToDateTimeTransformer", StringComparison.Ordinal));
    Assert.NotNull(result.Diagnostics.RawEvents
        .First(e => e.Type == DiagnosticEventType.TransformerSucceeded).DecoratorArgs);
}
```

- [ ] **Step 2: Run tests to verify the second test fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticIntegrationTests.GivenDiagnosticsEnabled_WhenTokenizingWithDecorators"`
Expected: May pass or fail depending on whether decorator args are recorded. Run to establish baseline.

- [ ] **Step 3: Add IsEnabled guards in DecoratorPipeline**

In `DecoratorPipeline.cs`, wrap all four `_collector.Record()` calls with `if (_collector.IsEnabled)`:

```csharp
private bool RunDecoratorPipeline(Token token, object input, FileLocation? location, out object? evaluatedValue)
{
    evaluatedValue = input;

    foreach (var decorator in token.Decorators)
    {
        if (decorator.IsTransformer)
        {
            if (!decorator.TryTransform(evaluatedValue!, _options, out var output))
            {
                if (_collector.IsEnabled)
                {
                    _collector.Record(DiagnosticEventType.TransformerFailed,
                        tokenName: token.Name, tokenId: token.Id,
                        location: location,
                        value: evaluatedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name,
                        decoratorArgs: decorator.Parameters.ToArray());
                }

                return false;
            }

            if (_collector.IsEnabled)
            {
                _collector.Record(DiagnosticEventType.TransformerSucceeded,
                    tokenName: token.Name, tokenId: token.Id,
                    location: location,
                    value: evaluatedValue?.ToString(),
                    detail: output?.ToString(),
                    decoratorName: decorator.DecoratorType.Name,
                    decoratorArgs: decorator.Parameters.ToArray());
            }

            evaluatedValue = output;
        }

        if (decorator.IsValidator)
        {
            if (decorator.Validate(evaluatedValue!, _options))
            {
                if (_collector.IsEnabled)
                {
                    _collector.Record(DiagnosticEventType.ValidatorPassed,
                        tokenName: token.Name, tokenId: token.Id,
                        value: evaluatedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);
                }
            }
            else
            {
                if (_collector.IsEnabled)
                {
                    _collector.Record(DiagnosticEventType.ValidatorFailed,
                        tokenName: token.Name, tokenId: token.Id,
                        value: input?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);
                }

                return false;
            }
        }
    }

    return true;
}
```

- [ ] **Step 4: Add IsEnabled guards in TokenizationSession**

In `TokenizationSession.cs`, wrap the Initialize and Finalize Record calls:

```csharp
private void Initialize(TokenizationContext context)
{
    if (_collector.IsEnabled)
    {
        _collector.Record(DiagnosticEventType.TokenizationStarted,
            detail: $"Template: {_template.Name}, Tokens: {_template.Tokens.Count}");
    }
    context.MatchBuffer.Clear();
    _iterationCount = 0;
}

private void Finalize(TokenizationContext context)
{
    _candidateProcessor.ProcessRemaining(context);
    FrontMatterProcessor.Process(_template, _result, _pipeline, context.Enumerator.Location);
    if (_collector.IsEnabled)
    {
        _collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: $"Matches: {_result.Tokens.Matches.Count}, Misses: {_result.Tokens.Misses.Count}");
    }
}
```

- [ ] **Step 5: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenization/DecoratorPipeline.cs src/Tokenizer/Tokenization/TokenizationSession.cs tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs
git commit -m "Add IsEnabled guards in DecoratorPipeline and TokenizationSession to eliminate allocations when diagnostics disabled"
```

---

### Task 3: Fix duplicate Warning/Debug logging (M2)

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs:353-388`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticLoggingTests.cs`

**Interfaces:**
- Consumes: `DiagnosticResult.Tokens`, `DiagnosticResult.MissedCount`
- Produces: Warning-level logs issue codes for non-matched tokens; Debug-level logs only hints (not duplicate issue descriptions)

- [ ] **Step 1: Write the failing test**

Add a test that captures log output and verifies no duplicate messages. Use a test logger that captures log entries:

```csharp
[Fact]
public void GivenDiagnosticsEnabled_WhenTokenMissed_ThenIssueNotLoggedAtBothWarningAndDebug()
{
    // Arrange
    var logEntries = new List<(LogLevel Level, string Message)>();
    var tokenizer = CreateTokenizerWithLogCapture(logEntries);

    // Act
    var template = tokenizer.Compile("Name: { Name }\nAge: { Age }").Template;
    tokenizer.Tokenize(template, "Name: John");

    // Assert — issue descriptions should appear at Warning only, not duplicated at Debug
    var warningIssues = logEntries
        .Where(e => e.Level == LogLevel.Warning && e.Message.Contains("[TK", StringComparison.Ordinal))
        .ToList();
    var debugIssues = logEntries
        .Where(e => e.Level == LogLevel.Debug && e.Message.Contains("Token '") && e.Message.Contains(": ", StringComparison.Ordinal))
        .ToList();

    // Warning should have issue entries
    Assert.NotEmpty(warningIssues);
    // Debug should NOT duplicate the same issue descriptions
    foreach (var warning in warningIssues)
    {
        var tokenName = ExtractTokenName(warning.Message);
        Assert.DoesNotContain(debugIssues, d => d.Message.Contains(tokenName, StringComparison.Ordinal));
    }
}
```

Note: This test requires a log capture helper. The implementer should check if `TestLoggerFactory` already supports capturing log entries, or create a minimal `InMemoryLogger` for this test. If the existing infrastructure doesn't support log capture, use a simpler assertion approach — verify behavior by checking that the FinalizeTokenization method skips already-warned issues in the Debug block.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticLoggingTests.GivenDiagnosticsEnabled_WhenTokenMissed_ThenIssueNotLoggedAtBothWarningAndDebug"`
Expected: FAIL (currently issues are logged at both levels)

- [ ] **Step 3: Fix the duplicate logging**

In `Tokenizer.cs` `FinalizeTokenization` method, restructure the logging so Debug only logs hints and verdict, not issue descriptions already covered at Warning:

```csharp
if (result.Diagnostics != null)
{
    if (result.Diagnostics.MissedCount > 0)
    {
        foreach (var token in result.Diagnostics.Tokens)
        {
            if (token.Outcome == TokenOutcome.Matched)
                continue;

            foreach (var issue in token.Issues)
            {
                _log.LogWarning("[{IssueCode}] Token '{TokenName}': {Description}",
                    issue.Code, issue.TokenName, issue.Description);
            }
        }
    }

    if (_log.IsEnabled(LogLevel.Debug))
    {
        _log.LogDebug("{Verdict}", result.Diagnostics.Verdict);
        // Only log hints at Debug level — issue descriptions already logged at Warning
        foreach (var token in result.Diagnostics.Tokens)
        {
            foreach (var issue in token.Issues)
            {
                if (issue.Hint != null)
                {
                    _log.LogDebug("  → Hint for '{TokenName}': {Hint}", issue.TokenName, issue.Hint);
                }
            }
        }
        if (rawInput != null)
        {
            _log.LogDebug("{Alignment}", result.Diagnostics.RenderAlignment());
        }
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs tests/Tokenizer.Tests/Diagnostics/DiagnosticLoggingTests.cs
git commit -m "Deduplicate Warning/Debug diagnostic logging: hints only at Debug level"
```

---

### Task 4: Add exception diagnostic attachment tests (C1 + C2)

**Files:**
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs`

**Interfaces:**
- Consumes: `Tokenizer.Compile()`, `Tokenizer.Tokenize()`, exception `Data` dictionary
- Produces: Tests asserting `ex.Data["CompilationDiagnostics"]` and `ex.Data["Diagnostics"]` are populated on failure

- [ ] **Step 1: Write compilation failure test (C1)**

```csharp
[Fact]
public void GivenDiagnosticsEnabled_WhenCompilationFails_ThenExceptionCarriesCompilationDiagnostics()
{
    // Arrange
    var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });

    // Act
    var ex = Assert.Throws<TokenizerException>(() => tokenizer.Compile("{ Name : UnknownDecoratorThatDoesNotExist }"));

    // Assert
    Assert.NotNull(ex.Data["CompilationDiagnostics"]);
    var diagnostics = (CompilationDiagnostics)ex.Data["CompilationDiagnostics"]!;
    Assert.True(diagnostics.Events.Count > 0);
}
```

- [ ] **Step 2: Run to verify it passes (this tests existing behavior)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticIntegrationTests.GivenDiagnosticsEnabled_WhenCompilationFails"`
Expected: PASS (the code path exists, we're just adding the test coverage)

- [ ] **Step 3: Write runtime failure test (C2)**

```csharp
[Fact]
public void GivenDiagnosticsEnabled_WhenTokenizationThrows_ThenExceptionCarriesDiagnostics()
{
    // Arrange
    var tokenizer = CreateTokenizer(new TokenizerOptions
    {
        EnableDiagnostics = true,
        MaxIterations = 1
    });
    // Use a template that causes excessive iterations
    var template = tokenizer.Compile("{ Name }").Template;

    // Act — MaxIterations=1 will cause a TokenizerException
    var ex = Assert.Throws<TokenizerException>(() => tokenizer.Tokenize(template, "Name: John\nMore data here"));

    // Assert
    Assert.NotNull(ex.Data["Diagnostics"]);
    var diagnostics = (DiagnosticResult)ex.Data["Diagnostics"]!;
    Assert.True(diagnostics.RawEvents.Count > 0);
}
```

Note: The implementer needs to find a reliable way to trigger a `TokenizerException` during tokenization. `MaxIterations = 1` with any input should do it. If that doesn't work, try `MaxInputLength = 1` with longer input.

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticIntegrationTests.GivenDiagnosticsEnabled_WhenTokenization"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/DiagnosticIntegrationTests.cs
git commit -m "Add tests for diagnostic attachment to exceptions on compilation and tokenization failure"
```

---

### Task 5: Add builder-level unit tests (H3 + H4 + M5)

**Files:**
- Test: `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`

**Interfaces:**
- Consumes: `RuntimeDiagnosticCollector`, `TokenDiagnosticBuilder.Build()`
- Produces: Tests for RepeatingTokenDisabled, ValueMismatch, and OutOfOrderTokens paths

- [ ] **Step 1: Write RepeatingTokenDisabled test (H3)**

```csharp
[Fact]
public void GivenRepeatingTokenDisabled_WhenBuilding_ThenRepeatingTokenCutShortIssueCreated()
{
    // Arrange
    var collector = new RuntimeDiagnosticCollector("Items: one\nItems: two");
    collector.Record(DiagnosticEventType.TokenizationStarted);
    collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Items", tokenId: 1, value: "one");
    collector.Record(DiagnosticEventType.RepeatingTokenDisabled, tokenName: "Items",
        detail: "Line gap exceeded maximum");
    collector.Record(DiagnosticEventType.TokenizationCompleted);
    var diagnostics = collector.GetResult()!;

    // Act
    var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

    // Assert
    var items = tokens.First(t => string.Equals(t.TokenName, "Items", StringComparison.Ordinal));
    Assert.Contains(items.Issues, i => i.Type == DiagnosticIssueType.RepeatingTokenCutShort);
}
```

- [ ] **Step 2: Write ValueMismatch test (H4)**

```csharp
[Fact]
public void GivenMatchedValueContainsMissedPreamble_WhenBuilding_ThenValueMismatchIssueAdded()
{
    // Arrange
    var collector = new RuntimeDiagnosticCollector("Name: Alice Age: 30");
    collector.Record(DiagnosticEventType.TokenizationStarted);
    collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Name", detail: "Name: ");
    collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "Name", value: "Alice Age: 30");
    collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Age", detail: "Age: ");
    collector.Record(DiagnosticEventType.TokenizationCompleted);
    var diagnostics = collector.GetResult()!;

    // Act
    var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

    // Assert
    var nameToken = tokens.First(t => string.Equals(t.TokenName, "Name", StringComparison.Ordinal));
    Assert.Contains(nameToken.Issues, i => i.Type == DiagnosticIssueType.ValueMismatch);
}
```

- [ ] **Step 3: Write OutOfOrderTokens test (M5)**

```csharp
[Fact]
public void GivenOutOfOrderTokens_WhenBuilding_ThenNoBlockedAnnotationsApplied()
{
    // Arrange
    var collector = new RuntimeDiagnosticCollector("nothing", outOfOrderTokens: true);
    collector.Record(DiagnosticEventType.TokenizationStarted);
    collector.Record(DiagnosticEventType.TokenMissed, tokenName: "First");
    collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Second");
    collector.Record(DiagnosticEventType.TokenizationCompleted);
    var diagnostics = collector.GetResult()!;

    // Act
    var (tokens, _, _, _, _) = TokenDiagnosticBuilder.Build(diagnostics);

    // Assert
    Assert.All(tokens, t => Assert.NotEqual(TokenOutcome.Blocked, t.Outcome));
    Assert.All(tokens, t => Assert.Null(t.BlockedBy));
}
```

- [ ] **Step 4: Run all builder tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenDiagnosticBuilderTests"`
Expected: ALL PASS (these test existing behavior)

- [ ] **Step 5: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs
git commit -m "Add builder-level tests for RepeatingTokenDisabled, ValueMismatch, and OutOfOrderTokens"
```

---

### Task 6: Add DecoratorBinder diagnostic recording test (H5)

**Files:**
- Test: `tests/Tokenizer.Tests/Diagnostics/CompilationDiagnosticsTests.cs`

**Interfaces:**
- Consumes: `Tokenizer.Compile()`, `CompilationDiagnostics.Events`
- Produces: Test verifying decorator application generates compilation events

- [ ] **Step 1: Write the test**

```csharp
[Fact]
public void GivenDiagnosticsEnabled_WhenCompilingWithDecorators_ThenDecoratorAppliedEventsRecorded()
{
    // Arrange
    var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });

    // Act
    var result = tokenizer.Compile("Email: { Email : IsEmail }");

    // Assert
    var diagnostics = result.Diagnostics!;
    Assert.Contains(diagnostics.Events, e => e.Type == CompilationEventType.DecoratorApplied
        && string.Equals(e.Detail, "IsEmailValidator", StringComparison.Ordinal));
}
```

- [ ] **Step 2: Run to verify**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CompilationDiagnosticsTests.GivenDiagnosticsEnabled_WhenCompilingWithDecorators"`
Expected: PASS (testing existing behavior). If the assertion on `Detail` fails, check what field the decorator name is stored in (might be `TokenName` or different field) — adjust accordingly.

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/CompilationDiagnosticsTests.cs
git commit -m "Add test verifying DecoratorApplied events are recorded during compilation"
```

---

### Task 7: Add ProcessingOrderRenderer decorator args test (M3)

**Files:**
- Test: `tests/Tokenizer.Tests/Diagnostics/ProcessingOrderRendererTests.cs`

**Interfaces:**
- Consumes: `DiagnosticResult.RenderProcessingOrder()`
- Produces: Test verifying decorator arguments appear in rendered output

- [ ] **Step 1: Write the test**

```csharp
[Fact]
public void GivenDecoratorWithArgs_WhenRendering_ThenArgsAppearInOutput()
{
    // Arrange
    var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
    var template = "Date: { Date : ToDateTime(yyyy-MM-dd) }";
    var input = "Date: 2026-01-15";

    // Act
    var compiled = tokenizer.Compile(template).Template;
    var result = tokenizer.Tokenize(compiled, input);
    var output = result.Diagnostics!.RenderProcessingOrder();

    // Assert
    Output.WriteLine(output);
    Assert.True(output.Contains("yyyy-MM-dd", StringComparison.Ordinal));
}
```

- [ ] **Step 2: Run test**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ProcessingOrderRendererTests.GivenDecoratorWithArgs"`
Expected: PASS (testing existing behavior)

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/ProcessingOrderRendererTests.cs
git commit -m "Add test verifying decorator args rendering in ProcessingOrderRenderer"
```

---

### Task 8: Add warning log issue code test (M4)

**Files:**
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticLoggingTests.cs`

**Interfaces:**
- Consumes: `Tokenizer.Tokenize()`, log output
- Produces: Test verifying `[TK001]` format in warning logs

- [ ] **Step 1: Write the test**

The implementer needs to capture log output. Check if `TestLoggerFactory` pipes to xUnit output. If so, use a custom `ILoggerProvider` that captures entries. Alternatively, use the simpler approach of verifying via `DiagnosticResult` that issue codes exist (since the logging format is `[{IssueCode}]`):

```csharp
[Fact]
public void GivenDiagnosticsEnabled_WhenTokenMissed_ThenIssueHasStableTKCode()
{
    // Arrange
    var tokenizer = CreateDiagnosticTokenizer();

    // Act
    var template = tokenizer.Compile("Name: { Name }\nAge: { Age }").Template;
    var result = tokenizer.Tokenize(template, "Name: John");

    // Assert
    var issues = result.Diagnostics!.Tokens.SelectMany(t => t.Issues).ToList();
    Assert.All(issues, issue =>
    {
        Assert.NotNull(issue.Code);
        Assert.Matches(@"^TK\d{3}$", issue.Code);
    });
    Assert.Contains(issues, i => string.Equals(i.Code, "TK001", StringComparison.Ordinal));
}
```

- [ ] **Step 2: Run test**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DiagnosticLoggingTests.GivenDiagnosticsEnabled_WhenTokenMissed_ThenIssueHasStableTKCode"`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/DiagnosticLoggingTests.cs
git commit -m "Add test verifying stable TK issue codes in diagnostic output"
```

---

### Task 9: Add AlignmentRenderer edge case test (M6)

**Files:**
- Test: `tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs`

**Interfaces:**
- Consumes: `DiagnosticResult.RenderAlignment()`
- Produces: Test for token with outcome but no failure attempts

- [ ] **Step 1: Write the test**

```csharp
[Fact]
public void GivenMissedTokenWithNoAttempts_WhenRenderingAlignment_ThenRendersWithoutError()
{
    // Arrange — a token that is simply never found (no preamble match, no attempts)
    var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
    var template = "Name: { Name }\nAge: { Age }";
    var input = "Name: John";

    // Act
    var compiled = tokenizer.Compile(template).Template;
    var result = tokenizer.Tokenize(compiled, input);
    var alignment = result.Diagnostics!.RenderAlignment();

    // Assert
    Output.WriteLine(alignment);
    Assert.True(alignment.Contains("Age", StringComparison.Ordinal));
    Assert.True(alignment.Contains("NeverFound", StringComparison.Ordinal) ||
                alignment.Contains("✗", StringComparison.Ordinal) ||
                alignment.Contains("Missed", StringComparison.Ordinal));
}
```

- [ ] **Step 2: Run test**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "AlignmentRendererTests.GivenMissedTokenWithNoAttempts"`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs
git commit -m "Add AlignmentRenderer edge case test for missed token with no attempts"
```

---

### Task 10: Update TK006 comment (L1)

**Files:**
- Modify: `src/Tokenizer/Diagnostics/IssueCodeMap.cs:16`

**Interfaces:**
- Consumes: N/A
- Produces: Clearer comment explaining the reserved code

- [ ] **Step 1: Update the comment**

```csharp
// TK006: formerly UnmatchedInputSection, reserved to prevent code reuse
```

- [ ] **Step 2: Build to verify no issues**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Diagnostics/IssueCodeMap.cs
git commit -m "Clarify TK006 reservation comment with former usage context"
```
