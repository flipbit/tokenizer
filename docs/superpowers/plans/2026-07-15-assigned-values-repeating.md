# AssignedValues for Repeating Tokens — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Change `TokenDiagnostic.AssignedValue` (singular) to `AssignedValues` (list) so repeating tokens expose all matched values, not just the last one.

**Architecture:** Replace the single-value `AssignedValue`/`AssignedLocation` properties with `AssignedValues`/`AssignedLocations` list properties. Change the backing store in `TokenDiagnosticBuilder.CollectedEventData` from a dictionary of single tuples to a dictionary of lists, appending on each `TokenAssigned` event. Update the `AlignmentRenderer` to render multiple values for repeating tokens. Update all consumers (tests) for the new shape.

**Tech Stack:** C# / .NET, xUnit

## Global Constraints

- Target frameworks: .NET Standard 2.0, .NET 8.0, .NET 10.0
- `LangVersion=latest`, nullable reference types enabled
- Collection expression syntax (`[]`) is used for empty list defaults in this codebase
- `TreatWarningsAsErrors` is enabled — code must compile clean
- Allman brace style, `_camelCase` private fields

---

### Task 1: Change `TokenDiagnostic` properties from singular to list

**Files:**
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnostic.cs:30-38`

**Interfaces:**
- Produces: `AssignedValues` (`IReadOnlyList<string>`, default `[]`), `AssignedLocations` (`IReadOnlyList<FileLocation>`, default `[]`)

- [ ] **Step 1: Write the failing test**

Add a new test to `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticTests.cs`:

```csharp
[Fact]
public void GivenMatchedToken_WhenCreated_ThenAssignedValuesContainsSingleValue()
{
    // Arrange & Act
    var diagnostic = new TokenDiagnostic
    {
        TokenName = "Email",
        TokenId = 1,
        Outcome = TokenOutcome.Matched,
        AssignedValues = new[] { "user@example.com" },
        AssignedLocations = new[] { new FileLocation() },
    };

    // Assert
    Assert.Single(diagnostic.AssignedValues);
    Assert.Equal("user@example.com", diagnostic.AssignedValues[0]);
    Assert.Single(diagnostic.AssignedLocations);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenDiagnosticTests.GivenMatchedToken_WhenCreated_ThenAssignedValuesContainsSingleValue"`
Expected: Build failure — `AssignedValues` does not exist yet.

- [ ] **Step 3: Replace properties in TokenDiagnostic.cs**

In `src/Tokenizer/Diagnostics/TokenDiagnostic.cs`, replace lines 29–38:

```csharp
    /// <summary>
    /// All assigned values, in input order. Single-element for non-repeating tokens,
    /// multiple elements for repeating tokens. Empty if Outcome is not Matched.
    /// </summary>
    public IReadOnlyList<string> AssignedValues { get; internal init; } = [];

    /// <summary>
    /// Locations where each value was matched, parallel to <see cref="AssignedValues"/>.
    /// </summary>
    public IReadOnlyList<FileLocation> AssignedLocations { get; internal init; } = [];
```

This will cause build failures in all consumers — that's expected. The remaining tasks fix each consumer.

- [ ] **Step 4: Run the new test to verify it passes**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenDiagnosticTests.GivenMatchedToken_WhenCreated_ThenAssignedValuesContainsSingleValue"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Diagnostics/TokenDiagnostic.cs tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticTests.cs
git commit -m "feat: replace AssignedValue/AssignedLocation with list-based AssignedValues/AssignedLocations"
```

Note: The build will not compile fully until Tasks 2–4 are complete. That's fine — commit the model change as a checkpoint.

---

### Task 2: Update `TokenDiagnosticBuilder` to collect and emit lists

**Files:**
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:69-88` (CollectedEventData)
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:159-166` (TokenAssigned case)
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:270-282` (ClassifyOutcomes)
- Modify: `src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs:293-338` (ApplyValueMismatchIssues)

**Interfaces:**
- Consumes: `TokenDiagnostic.AssignedValues`, `TokenDiagnostic.AssignedLocations` (from Task 1)
- Produces: Builder now populates lists instead of single values

- [ ] **Step 1: Write the failing test**

Add to `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`:

```csharp
[Fact]
public void GivenRepeatingTokenWithThreeMatches_WhenBuilding_ThenAssignedValuesContainsAllInOrder()
{
    // Arrange
    var collector = new TokenizationDiagnosticCollector("Item: A\nItem: B\nItem: C");
    collector.Record(TokenizationEventType.TokenizationStarted);
    collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Item", tokenId: 1,
        value: "A", location: new FileLocation());
    collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Item", tokenId: 1,
        value: "B", location: new FileLocation());
    collector.Record(TokenizationEventType.TokenAssigned, tokenName: "Item", tokenId: 1,
        value: "C", location: new FileLocation());
    collector.Record(TokenizationEventType.TokenizationCompleted);
    var diagnostics = collector.GetResult()!;

    // Act
    var (tokens, _, matched, missed, total) = new TokenDiagnosticBuilder(diagnostics).Build();

    // Assert
    var item = Assert.Single(tokens);
    Assert.Equal(TokenOutcome.Matched, item.Outcome);
    Assert.Equal(3, item.AssignedValues.Count);
    Assert.Equal("A", item.AssignedValues[0]);
    Assert.Equal("B", item.AssignedValues[1]);
    Assert.Equal("C", item.AssignedValues[2]);
    Assert.Equal(3, item.AssignedLocations.Count);
    Assert.Equal(1, matched);  // still 1 unique token
    Assert.Equal(0, missed);
    Assert.Equal(1, total);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenDiagnosticBuilderTests.GivenRepeatingTokenWithThreeMatches_WhenBuilding_ThenAssignedValuesContainsAllInOrder"`
Expected: Build failure or test failure — builder still uses single-value dictionary.

- [ ] **Step 3: Update CollectedEventData**

In `TokenDiagnosticBuilder.cs`, change the `AssignedTokens` property in `CollectedEventData` (line 73):

Old:
```csharp
public Dictionary<string, (string? value, Enumerators.FileLocation? location)> AssignedTokens { get; } = new(StringComparer.Ordinal);
```

New:
```csharp
public Dictionary<string, List<(string? value, Enumerators.FileLocation? location)>> AssignedTokens { get; } = new(StringComparer.Ordinal);
```

- [ ] **Step 4: Update the TokenAssigned case in CollectEvents**

In `CollectEvents()`, replace the `TokenizationEventType.TokenAssigned` case (lines 159–173):

Old:
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

New:
```csharp
case TokenizationEventType.TokenAssigned:
    if (evt.TokenName != null)
    {
        if (!data.AssignedTokens.TryGetValue(evt.TokenName, out var assignedList))
        {
            assignedList = new List<(string?, Enumerators.FileLocation?)>();
            data.AssignedTokens[evt.TokenName] = assignedList;
            data.MatchedCount++;
        }
        assignedList.Add((evt.Value, evt.Location));
        AddAttempt(data.Attempts, evt.TokenName, new TokenAttempt
        {
            Location = evt.Location,
            Value = evt.Value,
            Outcome = AttemptOutcome.Assigned,
        });
    }
    break;
```

- [ ] **Step 5: Update ClassifyOutcomes**

In `ClassifyOutcomes()`, replace the section that reads from `AssignedTokens` and populates the `TokenDiagnostic` (around lines 254–282):

Old:
```csharp
var assigned = isAssigned ? data.AssignedTokens[tokenName] : default;
...
result.Add(new TokenDiagnostic
{
    ...
    AssignedValue = assigned.value,
    AssignedLocation = assigned.location,
    ...
});
```

New:
```csharp
var assignedEntries = isAssigned ? data.AssignedTokens[tokenName] : null;
...
result.Add(new TokenDiagnostic
{
    ...
    AssignedValues = assignedEntries?.Select(e => e.value!).ToList() ?? [],
    AssignedLocations = assignedEntries?.Select(e => e.location!).ToList() ?? [],
    ...
});
```

Note: When `isAssigned` is true, values were added via `TokenAssigned` events which always have non-null `evt.Value`. The `!` null-forgiving operator is safe here.

- [ ] **Step 6: Update ApplyValueMismatchIssues**

In `ApplyValueMismatchIssues()`, the code reads `data.AssignedTokens[tokenName].value` (line 315). Update to check all values in the list:

Old:
```csharp
var assignedValue = data.AssignedTokens[tokenName].value;
if (string.IsNullOrEmpty(assignedValue))
    continue;

foreach (var missedName in missedOrRejected)
{
    ...
    if (assignedValue!.IndexOf(preamble, StringComparison.Ordinal) >= 0)
    {
        ...
        break;
    }
}
```

New:
```csharp
var assignedEntries = data.AssignedTokens[tokenName];
// For value-mismatch detection, check the last assigned value — this is the
// one most likely to have "swallowed" a subsequent token's preamble.
var lastValue = assignedEntries[assignedEntries.Count - 1].value;
if (string.IsNullOrEmpty(lastValue))
    continue;

foreach (var missedName in missedOrRejected)
{
    ...
    if (lastValue!.IndexOf(preamble, StringComparison.Ordinal) >= 0)
    {
        ...
        break;
    }
}
```

- [ ] **Step 7: Run the new test to verify it passes**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenDiagnosticBuilderTests.GivenRepeatingTokenWithThreeMatches_WhenBuilding_ThenAssignedValuesContainsAllInOrder"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/Tokenizer/Diagnostics/TokenDiagnosticBuilder.cs tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs
git commit -m "feat: TokenDiagnosticBuilder collects all assigned values for repeating tokens"
```

---

### Task 3: Update `AlignmentRenderer` for multi-value display

**Files:**
- Modify: `src/Tokenizer/Diagnostics/AlignmentRenderer.cs:49-53`

**Interfaces:**
- Consumes: `TokenDiagnostic.AssignedValues`, `TokenDiagnostic.AssignedLocations` (from Task 1)
- Produces: Matched token lines render as:
  - Single value: `  ✓ Name = "John" (line 1)` (unchanged format)
  - Multiple values: `  ✓ Items = "A", "B", "C" (lines 1–3)`

- [ ] **Step 1: Write the failing test**

Add to `tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs`:

```csharp
[Fact]
public void GivenRepeatingToken_WhenRendering_ThenShowsAllValuesAndLineRange()
{
    // Arrange
    var collector = new TokenizationDiagnosticCollector("Item: A\nItem: B\nItem: C");
    collector.Record(TokenizationEventType.TokenizationStarted);
    var loc1 = new FileLocation();
    var loc2 = new FileLocation();
    loc2.NewLine();
    var loc3 = new FileLocation();
    loc3.NewLine();
    loc3.NewLine();
    collector.Record(TokenizationEventType.TokenAssigned,
        tokenName: "Item", value: "A", location: loc1);
    collector.Record(TokenizationEventType.TokenAssigned,
        tokenName: "Item", value: "B", location: loc2);
    collector.Record(TokenizationEventType.TokenAssigned,
        tokenName: "Item", value: "C", location: loc3);
    collector.Record(TokenizationEventType.TokenizationCompleted);

    // Act
    var diagnostics = collector.GetResult()!;
    var output = diagnostics.RenderAlignment();
    Output.WriteLine(output);

    // Assert
    Assert.Contains("\"A\", \"B\", \"C\"", output, StringComparison.Ordinal);
    Assert.Contains("lines", output, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~AlignmentRendererTests.GivenRepeatingToken_WhenRendering_ThenShowsAllValuesAndLineRange"`
Expected: FAIL — renderer still uses old singular property.

- [ ] **Step 3: Update the matched tokens rendering block**

In `AlignmentRenderer.cs`, replace lines 49–53 (the matched tokens foreach body):

Old:
```csharp
foreach (var token in matchedTokens)
{
    var line = token.AssignedLocation != null ? $" (line {token.AssignedLocation.Line.ToInvariant()})" : string.Empty;
    sb.Append("  ✓ ").Append(token.TokenName).Append(" = \"").Append(token.AssignedValue).Append('"').AppendLine(line);
}
```

New:
```csharp
foreach (var token in matchedTokens)
{
    sb.Append("  ✓ ").Append(token.TokenName).Append(" = ");

    if (token.AssignedValues.Count <= 1)
    {
        // Single value — preserve existing format
        sb.Append('"').Append(token.AssignedValues.Count == 1 ? token.AssignedValues[0] : string.Empty).Append('"');
        if (token.AssignedLocations.Count == 1 && token.AssignedLocations[0] != null)
        {
            sb.Append(" (line ").Append(token.AssignedLocations[0].Line.ToInvariant()).Append(')');
        }
    }
    else
    {
        // Multiple values — comma-separated with line range
        for (var vi = 0; vi < token.AssignedValues.Count; vi++)
        {
            if (vi > 0) sb.Append(", ");
            sb.Append('"').Append(token.AssignedValues[vi]).Append('"');
        }

        if (token.AssignedLocations.Count >= 2)
        {
            var firstLine = token.AssignedLocations[0]?.Line ?? 0;
            var lastLine = token.AssignedLocations[token.AssignedLocations.Count - 1]?.Line ?? 0;
            if (firstLine > 0 && lastLine > 0)
            {
                sb.Append(" (lines ").Append(firstLine.ToInvariant()).Append('–').Append(lastLine.ToInvariant()).Append(')');
            }
        }
    }

    sb.AppendLine();
}
```

- [ ] **Step 4: Run the new test to verify it passes**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~AlignmentRendererTests.GivenRepeatingToken_WhenRendering_ThenShowsAllValuesAndLineRange"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Diagnostics/AlignmentRenderer.cs tests/Tokenizer.Tests/Diagnostics/AlignmentRendererTests.cs
git commit -m "feat: AlignmentRenderer displays all values for repeating tokens"
```

---

### Task 4: Update all existing tests to use the new list properties

**Files:**
- Modify: `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticTests.cs`
- Modify: `tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs`

**Interfaces:**
- Consumes: `TokenDiagnostic.AssignedValues`, `TokenDiagnostic.AssignedLocations` (from Task 1)

There are exactly these references to update:

**In `TokenDiagnosticTests.cs`:**

1. `GivenMatchedToken_WhenCreated_ThenPropertiesAreAccessible` (line 17–33): Change init from `AssignedValue`/`AssignedLocation` to `AssignedValues`/`AssignedLocations`, change assertion from `Assert.Equal("user@example.com", diagnostic.AssignedValue)` to `Assert.Equal("user@example.com", diagnostic.AssignedValues[0])`.

2. `GivenNeverFoundToken_WhenCreated_ThenNoAttemptsAndNoAssignedValue` (line 95): Change `Assert.Null(diagnostic.AssignedValue)` to `Assert.Empty(diagnostic.AssignedValues)`.

**In `TokenDiagnosticBuilderTests.cs`:**

3. `GivenSingleMatchedToken_WhenBuilding_ThenTokenHasMatchedOutcome` (line 27): Change `Assert.Equal("John", tokens[0].AssignedValue)` to `Assert.Equal("John", tokens[0].AssignedValues[0])`.

4. `GivenMissedToken_WhenBuilding_ThenTokenHasNeverFoundOutcome` (line 50): Change `Assert.Null(tokens[0].AssignedValue)` to `Assert.Empty(tokens[0].AssignedValues)`.

5. `GivenMultipleAttemptsOneSuccess_WhenBuilding_ThenMatchedWithMultipleAttempts` (line 137): Change `Assert.Equal("good@email.com", tokens[0].AssignedValue)` to `Assert.Equal("good@email.com", tokens[0].AssignedValues[0])`.

- [ ] **Step 1: Update `TokenDiagnosticTests.cs`**

In `GivenMatchedToken_WhenCreated_ThenPropertiesAreAccessible`, change:
```csharp
// Old
AssignedValue = "user@example.com",
AssignedLocation = new FileLocation(),
// New
AssignedValues = new[] { "user@example.com" },
AssignedLocations = new[] { new FileLocation() },
```

And the assertion:
```csharp
// Old
Assert.Equal("user@example.com", diagnostic.AssignedValue);
// New
Assert.Equal("user@example.com", diagnostic.AssignedValues[0]);
```

In `GivenNeverFoundToken_WhenCreated_ThenNoAttemptsAndNoAssignedValue`, change:
```csharp
// Old
Assert.Null(diagnostic.AssignedValue);
// New
Assert.Empty(diagnostic.AssignedValues);
```

- [ ] **Step 2: Update `TokenDiagnosticBuilderTests.cs`**

Line 27 — change `tokens[0].AssignedValue` to `tokens[0].AssignedValues[0]`.

Line 50 — change `Assert.Null(tokens[0].AssignedValue)` to `Assert.Empty(tokens[0].AssignedValues)`.

Line 137 — change `tokens[0].AssignedValue` to `tokens[0].AssignedValues[0]`.

- [ ] **Step 3: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS. Zero build warnings.

- [ ] **Step 4: Commit**

```bash
git add tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticTests.cs tests/Tokenizer.Tests/Diagnostics/TokenDiagnosticBuilderTests.cs
git commit -m "test: update all AssignedValue/AssignedLocation references to list properties"
```
