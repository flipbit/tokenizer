# Tier 2: API Naming and Shape — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename and reshape the public API to follow .NET Framework Design Guidelines before v3 ships.

**Architecture:** Seven independent, sequential commits — each a mechanical rename, record conversion, or visibility change. No new functionality. TDD approach: verify existing tests pass after each change.

**Tech Stack:** C# / .NET 8, xUnit, NSubstitute

---

### Task 1: Rename `CanTransform` to `TryTransform`

**Files:**
- Modify: `src/Tokenizer/Transformers/ITokenTransformer.cs:11`
- Modify: `src/Tokenizer/TokenDecoratorContext.cs:75,79`
- Modify: `src/Tokenizer/Token.cs:163,343`
- Modify: All 16 transformer implementations in `src/Tokenizer/Transformers/`
- Modify: All 17 transformer test files in `tests/Tokenizer.Tests/Transformers/`

- [ ] **Step 1: Run tests to establish green baseline**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 2: Rename in the interface**

In `src/Tokenizer/Transformers/ITokenTransformer.cs`, change:

```csharp
bool CanTransform(object value, string[] args, out object transformed);
```

to:

```csharp
bool TryTransform(object value, string[] args, out object transformed);
```

- [ ] **Step 3: Rename in TokenDecoratorContext**

In `src/Tokenizer/TokenDecoratorContext.cs`, rename the method and its internal call:

```csharp
public bool TryTransform(object value, out object transformed)
{
    var instance = (ITokenTransformer)CreateDecorator();

    return instance.TryTransform(value, _parameters.ToArray(), out transformed);
}
```

- [ ] **Step 4: Rename call sites in Token.cs**

In `src/Tokenizer/Token.cs:163`, change:

```csharp
var transformed = decorator.CanTransform(assignedValue!, out var output);
```

to:

```csharp
var transformed = decorator.TryTransform(assignedValue!, out var output);
```

In `src/Tokenizer/Token.cs:343`, change:

```csharp
if (decorator.CanTransform(input, out var output) == false)
```

to:

```csharp
if (decorator.TryTransform(input, out var output) == false)
```

- [ ] **Step 5: Rename in all transformer implementations**

In each of the following files, rename the method `CanTransform` to `TryTransform` (method signature only — the body is unchanged):

- `src/Tokenizer/Transformers/TrimTransformer.cs:8`
- `src/Tokenizer/Transformers/SplitTransformer.cs:10`
- `src/Tokenizer/Transformers/ToUpperTransformer.cs:8`
- `src/Tokenizer/Transformers/ToLowerTransformer.cs:8`
- `src/Tokenizer/Transformers/ReplaceTransformer.cs:10`
- `src/Tokenizer/Transformers/RemoveTransformer.cs:10`
- `src/Tokenizer/Transformers/RemoveStartTransformer.cs:11`
- `src/Tokenizer/Transformers/RemoveEndTransformer.cs:11`
- `src/Tokenizer/Transformers/SetTransformer.cs:10`
- `src/Tokenizer/Transformers/SubstringBeforeTransformer.cs:11`
- `src/Tokenizer/Transformers/SubstringAfterTransformer.cs:11`
- `src/Tokenizer/Transformers/SubstringBeforeLastTransformer.cs:11`
- `src/Tokenizer/Transformers/SubstringAfterLastTransformer.cs:11`
- `src/Tokenizer/Transformers/ToDateTimeTransformer.cs:24`
- `src/Tokenizer/Transformers/ToDateTimeUtcTransformer.cs:12`

- [ ] **Step 6: Rename in all test files**

In each transformer test file under `tests/Tokenizer.Tests/Transformers/`, rename all occurrences of `.CanTransform(` to `.TryTransform(`. Also rename `CanTransform` in `BlowsUpTransformer.cs:7`.

Test files to update:
- `RemoveEndTransformerTests.cs`
- `RemoveStartTransformerTests.cs`
- `RemoveTransformerTests.cs`
- `ReplaceTransformerTests.cs`
- `SetTransformerTests.cs`
- `SplitTransformerTests.cs`
- `SubstringAfterTransformerTests.cs`
- `SubstringAfterLastTransformerTests.cs`
- `SubstringBeforeTransformerTests.cs`
- `SubstringBeforeLastTransformerTests.cs`
- `ToDateTimeTransformerTests.cs`
- `ToDateTimeUtcTransformerTests.cs`
- `ToLowerTransformerTests.cs`
- `ToUpperTransformerTests.cs`
- `TrimTransformerTests.cs`
- `CultureInvariantTransformerTests.cs`
- `BlowsUpTransformer.cs`

- [ ] **Step 7: Run tests to verify**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git status
git add -A
git commit -m "Rename CanTransform to TryTransform to follow .NET TryX convention"
```

---

### Task 2: Rename `Match` to `TokenMatch` and convert to record

**Files:**
- Modify: `src/Tokenizer/Match.cs` (rename file to `TokenMatch.cs`)
- Modify: `src/Tokenizer/TokenResult.cs:13,18,22,30,44`
- Modify: `src/Tokenizer/TokenizeResult.cs:23`
- Modify: All files referencing `Match` type or `.Matches` property

- [ ] **Step 1: Run tests to establish green baseline**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 2: Convert Match to a positional record and rename to TokenMatch**

Replace the entire content of `src/Tokenizer/Match.cs` with:

```csharp
using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Represents a <see cref="Token"/> match in a <see cref="Template"/>
/// </summary>
public sealed record TokenMatch(Token Token, object Value, FileLocation Location);
```

Then rename the file from `Match.cs` to `TokenMatch.cs`:

```bash
git mv src/Tokenizer/Match.cs src/Tokenizer/TokenMatch.cs
```

- [ ] **Step 3: Update TokenResult to use TokenMatch**

In `src/Tokenizer/TokenResult.cs`, update:

1. Change the field type (line 13):
```csharp
private readonly List<TokenMatch> _matches;
```

2. Change the property type (line 22):
```csharp
public IReadOnlyList<TokenMatch> Matches => _matches;
```

3. Change the constructor (line 18):
```csharp
_matches = new List<TokenMatch>();
```

4. Change AddMatch (line 30):
```csharp
_matches.Add(new TokenMatch(token, value, location.Clone()));
```

5. Refactor TryConcatMatch to use `with` expression instead of mutating (lines 33-47):
```csharp
private bool TryConcatMatch(Token token, object value, FileLocation location)
{
    if (token.CanConcatenate == false) return false;

    var index = _matches.FindIndex(m => m.Token.Name == token.Name);
    if (index < 0) return false;

    var match = _matches[index];

    if (token.CanConcatenateValues(match.Value, value) == false) return false;

    var concatenated = token.ConcatenateValues(match.Value, value, token.ConcatenationString);
    if (concatenated != null) _matches[index] = match with { Value = concatenated };

    return true;
}
```

- [ ] **Step 4: Update TokenizeResult to use TokenMatch**

In `src/Tokenizer/TokenizeResult.cs`, change line 23:

```csharp
public IReadOnlyList<TokenMatch> Matches => Tokens.Matches;
```

- [ ] **Step 5: Update all remaining source references from Match to TokenMatch**

Search all source files under `src/Tokenizer/` for references to the `Match` type (not string `.Match(` method calls) and update them to `TokenMatch`. Key files include any that use `IReadOnlyList<Match>` or `List<Match>`.

- [ ] **Step 6: Update test references from Match to TokenMatch**

Search all test files under `tests/Tokenizer.Tests/` for references to the `Match` type and update them to `TokenMatch`. Key files include:
- `tests/Tokenizer.Tests/Tokenization/ResultBuilder/ResultBuilderBasicTests.cs`
- `tests/Tokenizer.Tests/ImmutableCollectionsTests.cs`
- Any builders or helpers referencing the `Match` type

- [ ] **Step 7: Run tests to verify**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git status
git add -A
git commit -m "Rename Match to TokenMatch and convert to record"
```

---

### Task 3: Convert `Hint` to record

**Files:**
- Modify: `src/Tokenizer/Hint.cs`
- Modify: `src/Tokenizer/HintResult.cs:46`
- Modify: `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs:103,106`
- Modify: `tests/Tokenizer.Tests/Builders/HintBuilder.cs`
- Modify: Test files that construct Hints

- [ ] **Step 1: Run tests to establish green baseline**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 2: Convert Hint to a positional record**

Replace the entire content of `src/Tokenizer/Hint.cs` with:

```csharp
namespace Tokens;

/// <summary>
/// Defines a string of text that can occur in a template's input.
/// A hint can optionally be required to be present.
/// Hints are used when determining whether the input is valid, and to determine
/// the best matched template for a given input.
/// </summary>
/// <param name="Text">The text to appear in the input</param>
/// <param name="Optional">If <c>true</c> then this hint must appear in the input in order for the
/// <see cref="Template"/> to be considered successfully matched.</param>
public sealed record Hint(string Text = "", bool Optional = false);
```

- [ ] **Step 3: Update HintResult to use `with` instead of Clone()**

In `src/Tokenizer/HintResult.cs:46`, change:

```csharp
_misses.Add(hint.Clone());
```

to:

```csharp
_misses.Add(hint with { });
```

- [ ] **Step 4: Update FrontMatterBinder to use constructor syntax**

In `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs`, change lines 103 and 106:

```csharp
case "hint":
    template.Hints.Add(new Hint(Text: entry.RawValue.Trim(), Optional: false));
    break;
case "hint?":
    template.Hints.Add(new Hint(Text: entry.RawValue.Trim(), Optional: true));
    break;
```

- [ ] **Step 5: Update HintBuilder to use constructor + with expressions**

Replace `tests/Tokenizer.Tests/Builders/HintBuilder.cs` with:

```csharp
namespace Tokens.Builders;

/// <summary>
/// Builder for creating Hint instances for testing
/// </summary>
public class HintBuilder
{
    private Hint _hint = new();

    public HintBuilder WithText(string text)
    {
        _hint = _hint with { Text = text };
        return this;
    }

    public HintBuilder WithOptional(bool optional = true)
    {
        _hint = _hint with { Optional = optional };
        return this;
    }

    public HintBuilder WithRequired(bool required = true)
    {
        _hint = _hint with { Optional = !required };
        return this;
    }

    public Hint Build()
    {
        return _hint;
    }
}
```

- [ ] **Step 6: Update test files that construct Hints**

Search all test files for `new Hint {` object initializer syntax and convert to constructor syntax. Key files:
- `tests/Tokenizer.Tests/Compilation/Definitions/TemplateDefinitionTests.cs` — multiple occurrences
- `tests/Tokenizer.Tests/Tokenization/HintProcessor/HintProcessorBasicTests.cs` — multiple occurrences
- `tests/Tokenizer.Tests/Tokenization/HintProcessor/HintProcessorEdgeCaseTests.cs` — multiple occurrences
- `tests/Tokenizer.Tests/TokenMatcherTests.cs` — multiple occurrences
- `tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs` — multiple occurrences

For each, change patterns like `new Hint { Text = "foo", Optional = true }` to `new Hint(Text: "foo", Optional: true)` and `new Hint { Text = "foo" }` to `new Hint(Text: "foo")`.

- [ ] **Step 7: Run tests to verify**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git status
git add -A
git commit -m "Convert Hint to positional record, remove manual Clone()"
```

---

### Task 4: Rename `CandidateTokenList.Any` to `HasCandidates`

**Files:**
- Modify: `src/Tokenizer/CandidateTokenList.cs:77`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs:165,208,628,655,663`
- Modify: Test files referencing `.Any` on CandidateTokenList

- [ ] **Step 1: Run tests to establish green baseline**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 2: Rename the property**

In `src/Tokenizer/CandidateTokenList.cs:77`, change:

```csharp
public bool Any => Count > 0;
```

to:

```csharp
public bool HasCandidates => Count > 0;
```

- [ ] **Step 3: Update all call sites in TokenizationEngine**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`, replace all occurrences of `candidates.Any` (or similar variable names like `context.Candidates.Any`) with the corresponding `.HasCandidates`. There are 5 references at approximately lines 165, 208, 628, 655, 663.

- [ ] **Step 4: Update test references**

Search test files for `.Any` usage on `CandidateTokenList` instances and update to `.HasCandidates`. Key files:
- `tests/Tokenizer.Tests/Tokenization/Context/TokenizationContextTests.cs`
- `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineStateTests.cs`

- [ ] **Step 5: Run tests to verify**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git status
git add -A
git commit -m "Rename CandidateTokenList.Any to HasCandidates for clarity"
```

---

### Task 5: Rename `TokenEnumerator.Match()` to `TryMatch()`

**Files:**
- Modify: `src/Tokenizer/Enumerators/TokenEnumerator.cs:87,107,120`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs:148,629`
- Modify: `src/Tokenizer/Tokenization/HintProcessor.cs:136`
- Modify: Test files referencing `TokenEnumerator.Match`

- [ ] **Step 1: Run tests to establish green baseline**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 2: Rename both overloads**

In `src/Tokenizer/Enumerators/TokenEnumerator.cs`:

Line 87 — rename string overload:
```csharp
public bool TryMatch(string value)
```

Line 107 — rename tokens overload:
```csharp
public bool TryMatch(IEnumerable<Token> tokens, bool outOfOrderTokens, IList<Token> matches)
```

Line 120 — update the internal self-call within the tokens overload:
```csharp
if (TryMatch(token.Preamble))
```

- [ ] **Step 3: Update call sites in TokenizationEngine**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`, replace `.Match(` calls on `TokenEnumerator` instances with `.TryMatch(` at approximately lines 148 and 629.

- [ ] **Step 4: Update call site in HintProcessor**

In `src/Tokenizer/Tokenization/HintProcessor.cs:136`, change the `.Match(` call to `.TryMatch(`.

- [ ] **Step 5: Update test references**

Search test files for `TokenEnumerator` `.Match(` calls and rename to `.TryMatch(`.

- [ ] **Step 6: Run tests to verify**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git status
git add -A
git commit -m "Rename TokenEnumerator.Match() to TryMatch() for clarity"
```

---

### Task 6: Make `TokenizeResult<T>.Value` init-only

**Files:**
- Modify: `src/Tokenizer/TokenizeResult.cs:96`

- [ ] **Step 1: Run tests to establish green baseline**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 2: Change setter to init**

In `src/Tokenizer/TokenizeResult.cs:96`, change:

```csharp
public T Value { get; set; }
```

to:

```csharp
public T Value { get; init; }
```

- [ ] **Step 3: Run tests to verify**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass. If any test assigns to `.Value` after construction, update it to use object initializer syntax instead.

- [ ] **Step 4: Commit**

```bash
git status
git add -A
git commit -m "Make TokenizeResult<T>.Value init-only to prevent consumer reassignment"
```

---

### Task 7: Make tokenization infrastructure types internal

**Files:**
- Modify: `src/Tokenizer/Tokenization/ITokenizationEngine.cs:13` — `public interface` → `internal interface`
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs:33` — `public class` → `internal class`
- Modify: `src/Tokenizer/Tokenization/IHintProcessor.cs:10` — `public interface` → `internal interface`
- Modify: `src/Tokenizer/Tokenization/HintProcessor.cs:13` — `public class` → `internal class`
- Modify: `src/Tokenizer/Tokenization/IResultBuilder.cs:12` — `public interface` → `internal interface`
- Modify: `src/Tokenizer/Tokenization/ResultBuilder.cs:15` — `public class` → `internal class`
- Modify: `src/Tokenizer/Tokenization/ITokenizationContext.cs:11` — `public interface` → `internal interface`
- Modify: `src/Tokenizer/Tokenization/TokenizationContext.cs:13` — `public sealed class` → `internal sealed class`

- [ ] **Step 1: Run tests to establish green baseline**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 2: Verify InternalsVisibleTo is configured**

Check that `src/Tokenizer/Properties/AssemblyInfo.cs` contains:

```csharp
[assembly: InternalsVisibleTo("Tokenizer.Tests")]
```

This is already in place — just verify it before proceeding.

- [ ] **Step 3: Change ITokenizationEngine and TokenizationEngine to internal**

In `src/Tokenizer/Tokenization/ITokenizationEngine.cs:13`, change:
```csharp
internal interface ITokenizationEngine
```

In `src/Tokenizer/Tokenization/TokenizationEngine.cs:33`, change:
```csharp
internal class TokenizationEngine : ITokenizationEngine
```

- [ ] **Step 4: Change IHintProcessor and HintProcessor to internal**

In `src/Tokenizer/Tokenization/IHintProcessor.cs:10`, change:
```csharp
internal interface IHintProcessor
```

In `src/Tokenizer/Tokenization/HintProcessor.cs:13`, change:
```csharp
internal class HintProcessor : IHintProcessor
```

- [ ] **Step 5: Change IResultBuilder and ResultBuilder to internal**

In `src/Tokenizer/Tokenization/IResultBuilder.cs:12`, change:
```csharp
internal interface IResultBuilder
```

In `src/Tokenizer/Tokenization/ResultBuilder.cs:15`, change:
```csharp
internal class ResultBuilder : IResultBuilder
```

- [ ] **Step 6: Change ITokenizationContext and TokenizationContext to internal**

In `src/Tokenizer/Tokenization/ITokenizationContext.cs:11`, change:
```csharp
internal interface ITokenizationContext
```

In `src/Tokenizer/Tokenization/TokenizationContext.cs:13`, change:
```csharp
internal sealed class TokenizationContext : ITokenizationContext, IDisposable
```

- [ ] **Step 7: Run tests to verify**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass (InternalsVisibleTo gives the test project access).

- [ ] **Step 8: Build in Release to verify no public API leaks**

```bash
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```

Expected: Build succeeds with no warnings about internal types in public signatures.

- [ ] **Step 9: Commit**

```bash
git status
git add -A
git commit -m "Make tokenization infrastructure types internal"
```
