# Token Assignment Extraction

## Overview

Extract assignment logic from `Token` into a dedicated `TokenAssigner` class, making `Token` a pure data model. Addresses code review issues D1, D2, H9, M5, and L1.

## Motivation

`Token` currently violates SRP by mixing domain model properties with assignment logic, decorator pipeline execution, value concatenation, and target object mutation. This refactoring separates these concerns, simplifies exception handling, and improves testability.

## Design

### 1. Token becomes pure data

**File:** `src/Tokenizer/Token.cs`

Remove from `Token`:

- `_content` private field
- `content` constructor parameter
- `ToString()` override
- `Assign()` method
- `CanAssign()` method
- `PrepareValue()` method
- `RunDecoratorPipeline()` method
- `SetDictionaryValue()` method
- `CanConcatenateValues()` static method
- `ConcatenateValues()` static method

Add `[DebuggerDisplay]` attribute:

```csharp
[DebuggerDisplay("{Name} (Id={Id}, Optional={IsOptional})")]
public sealed class Token
```

Constructor becomes:

```csharp
public Token(string name, string preamble, FileLocation location)
```

What remains: all public properties, `AddDecorator()`, `Decorators`.

### 2. TokenAssigner — session-scoped assignment

**New file:** `src/Tokenizer/Tokenization/TokenAssigner.cs`

```
internal sealed class TokenAssigner
```

**Constructor:**

```csharp
internal TokenAssigner(TokenizerOptions options, IDiagnosticCollector collector)
```

**Public methods:**

- `bool Assign(Token token, object? target, string value, FileLocation location, out object? assignedValue)` — full pipeline: prepare value, run decorator pipeline, assign to target (reflection, dictionary, or concatenation)
- `bool CanAssign(Token token, string value)` — dry-run: prepare + decorator pipeline only, no side effects

**Private helpers (moved from Token):**

- `string? PrepareValue(Token token, string value)` — null/empty checks, trim trailing newline, newline termination
- `bool RunDecoratorPipeline(Token token, object input, FileLocation? location, out object? assignedValue)` — runs transformers and validators in sequence, records diagnostics
- `bool SetDictionaryValue(Token token, IDictionary<string, object> dictionary, object input)` — handles dictionary target with repeating token support

**Exception handling (resolves H9 — single layer of wrapping):**

- `TokenAssigner.Assign` handles `MissingMemberException` internally (respects `IgnoreMissingProperties` option, records diagnostic, does not rethrow when ignored)
- `TokenAssigner.Assign` handles `TypeConversionException` internally (records diagnostic, returns false)
- `TokenAssigner.Assign` throws `TokenAssignmentException` explicitly only for the concatenation type mismatch case
- All other exceptions propagate naturally — no generic `catch (Exception)` wrapping in `TokenAssigner`

### 3. ValueConcatenation — static utility

**New file:** `src/Tokenizer/Extensions/ValueConcatenation.cs`

```
internal static class ValueConcatenation
```

**Static methods:**

- `bool CanConcatenate(object? existingValue, object newValue)` — returns true if both values are strings (addresses L1: name no longer implies generality beyond what it supports)
- `object? Concatenate(object? existingValue, object newValue, string? concatenationString)` — concatenates string values with `<CR>` → `Environment.NewLine` replacement

**Callers:**

- `TokenAssigner.Assign` — when `token.CanConcatenate` is true
- `TokenResult.TryConcatMatch` — when merging repeated matches in the result list

### 4. CandidateProcessor catch consolidation (D2, H9)

**File:** `src/Tokenizer/Tokenization/CandidateProcessor.cs`

Replace four identical catch blocks (`TokenAssignmentException`, `TypeConversionException`, `MissingMemberException`, `Exception`) with a single `catch (Exception)`:

```csharp
catch (Exception e)
{
    if (_logger.IsEnabled(LogLevel.Warning))
    {
        _logger.LogWarning(e, "Error Assigning Value: {Message}", e.Message);
    }
    _result.AddException(e);
    return false;
}
```

This is safe because `TokenAssigner` now handles the specific exception cases internally. By the time an exception reaches `CandidateProcessor`, it is a genuine failure that should be logged and recorded regardless of type.

### 5. CandidateTokenList signature changes

**File:** `src/Tokenizer/CandidateTokenList.cs`

- `TryAssign` — replace `TokenizerOptions options` and `IDiagnosticCollector collector` parameters with `TokenAssigner assigner` parameter. Calls `assigner.Assign(token, ...)` instead of `token.Assign(...)`.
- `CanAnyAssign` — add `TokenAssigner assigner` parameter. Calls `assigner.CanAssign(token, ...)` instead of `token.CanAssign(...)`.

### 6. FrontMatterProcessor integration

**File:** `src/Tokenizer/Tokenization/FrontMatterProcessor.cs`

Add `TokenAssigner assigner` parameter, remove `IDiagnosticCollector collector` parameter:

```csharp
public static void Process(
    Template template,
    object? targetObject,
    TokenizeResultBase result,
    TokenAssigner assigner,
    FileLocation location)
```

Calls `assigner.Assign(token, targetObject, string.Empty, location, out var assignedValue)`. Diagnostic recording (`FrontMatterTokenAssigned`, `FrontMatterTokenFailed`) stays in `FrontMatterProcessor` — it is caller-level context, not assignment-level.

### 7. TokenizationSession wiring

**File:** `src/Tokenizer/Tokenization/TokenizationSession.cs`

Create `TokenAssigner` as a session-scoped field:

```csharp
private readonly TokenAssigner _assigner;
```

Constructed in `TokenizationSession` constructor:

```csharp
_assigner = new TokenAssigner(_template.Options, collector);
```

Passed to:

- `CandidateProcessor` constructor (stored as a field, passed as a parameter to `CandidateTokenList.TryAssign` and `CanAnyAssign` at each call site)
- `FrontMatterProcessor.Process` in `Finalize()`

### 8. ObjectExtensions default alignment (M5)

**File:** `src/Tokenizer/Extensions/ObjectExtensions.cs`

Change the single-parameter `SetValue` overload default from `StringComparison.InvariantCulture` to `StringComparison.Ordinal`:

```csharp
public static T SetValue<T>(this T @object, string propertyPath, object value) where T : class
{
    return SetValue(@object, propertyPath, value, StringComparison.Ordinal);
}
```

Every production caller already passes `Ordinal` explicitly. This aligns the default with actual usage.

### 9. TokenFactory update

**File:** `src/Tokenizer/Compilation/Binders/TokenFactory.cs`

Drop `definition.Content` from the `Token` constructor call:

```csharp
var token = new Token(definition.Name ?? string.Empty, preamble, location);
```

The `Content={definition.Content}` in the diagnostic `detail` string can remain — it reads from `definition.Content`, not from Token.

## Files changed

| File | Change |
|------|--------|
| `src/Tokenizer/Token.cs` | Strip to pure data model, add `[DebuggerDisplay]` |
| `src/Tokenizer/Tokenization/TokenAssigner.cs` | New — session-scoped assignment logic |
| `src/Tokenizer/Extensions/ValueConcatenation.cs` | New — static concat utility |
| `src/Tokenizer/Tokenization/CandidateProcessor.cs` | Consolidate 4 catch blocks to 1 |
| `src/Tokenizer/CandidateTokenList.cs` | Change `TryAssign`/`CanAnyAssign` signatures |
| `src/Tokenizer/Tokenization/FrontMatterProcessor.cs` | Add `TokenAssigner` param, remove `IDiagnosticCollector` |
| `src/Tokenizer/Tokenization/TokenizationSession.cs` | Create and wire `TokenAssigner` |
| `src/Tokenizer/Extensions/ObjectExtensions.cs` | Change default comparison to `Ordinal` |
| `src/Tokenizer/Compilation/Binders/TokenFactory.cs` | Drop `content` constructor param |
| `src/Tokenizer/TokenResult.cs` | Use `ValueConcatenation` instead of `Token` statics |
| `tests/Tokenizer.Tests/TokenTests.cs` | Rewrite to test via `TokenAssigner` |
| `tests/Tokenizer.Tests/Compilation/Binders/TokenFactoryTests.cs` | Remove `ToString()` assertion, update constructor |

## Review issues addressed

| Issue | Resolution |
|-------|-----------|
| D1 | Token stripped to pure data model, assignment logic extracted to `TokenAssigner` |
| D2 | Four identical catch blocks in `CandidateProcessor` consolidated to one |
| H9 | Double exception wrapping eliminated — `TokenAssigner` handles specific cases, `CandidateProcessor` handles generic |
| M5 | `ObjectExtensions.SetValue` default changed from `InvariantCulture` to `Ordinal` |
| L1 | Concat methods renamed from `CanConcatenateValues`/`ConcatenateValues` to `CanConcatenate`/`Concatenate` in `ValueConcatenation` |
