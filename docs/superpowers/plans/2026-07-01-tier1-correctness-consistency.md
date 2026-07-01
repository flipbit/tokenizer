# Tier 1: Correctness and Consistency — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix bugs, typos, and inconsistencies in the public API before more users depend on v3 names.

**Architecture:** Six independent correctness fixes applied in sequence. Each task is a self-contained commit. No new features — only renames, doc fixes, and dead code removal.

**Tech Stack:** C# / .NET (xUnit tests, `dotnet test` runner)

**Spec:** `docs/superpowers/specs/2026-07-01-tier1-correctness-consistency-design.md`

---

### Task 1: Fix XML Doc Copy-Paste Errors

**Files:**
- Modify: `src/Tokenizer/Validators/MinLengthValidator.cs:7,18`
- Modify: `src/Tokenizer/Validators/MaxLengthValidator.cs:18`
- Modify: `src/Tokenizer/Transformers/SplitTransformer.cs:7`
- Modify: `src/Tokenizer/Transformers/SubstringAfterLastTransformer.cs:7`
- Modify: `src/Tokenizer/Transformers/SubstringBeforeLastTransformer.cs:7`
- Modify: `src/Tokenizer/Transformers/SetTransformer.cs:14`

- [ ] **Step 1: Fix MinLengthValidator.cs line 7**

Change:
```csharp
/// Validator to determine if a token value meets a maximum length requirement
```
To:
```csharp
/// Validator to determine if a token value meets a minimum length requirement
```

- [ ] **Step 2: Fix MinLengthValidator.cs line 18**

Change:
```csharp
throw new ValidationException("You must specified a MinLength value, e.g. 'MinLength(50)'");
```
To:
```csharp
throw new ValidationException("You must specify a MinLength value, e.g. 'MinLength(50)'");
```

- [ ] **Step 3: Fix MaxLengthValidator.cs line 18**

Change:
```csharp
throw new ValidationException("You must specified a MaxLength value, e.g. 'MaxLength(255)'");
```
To:
```csharp
throw new ValidationException("You must specify a MaxLength value, e.g. 'MaxLength(255)'");
```

- [ ] **Step 4: Fix SplitTransformer.cs line 7**

Change:
```csharp
/// Removes occurrences of a string from then end of a token value
```
To:
```csharp
/// Splits a token value on a specified delimiter
```

- [ ] **Step 5: Fix SubstringAfterLastTransformer.cs line 7**

Change:
```csharp
/// Trims the token value after the first occurence of the given string 
```
To:
```csharp
/// Trims the token value after the last occurrence of the given string
```

- [ ] **Step 6: Fix SubstringBeforeLastTransformer.cs line 7**

Change:
```csharp
/// Trims the token value after the first occurence of the given string 
```
To:
```csharp
/// Trims the token value before the last occurrence of the given string
```

- [ ] **Step 7: Fix SetTransformer.cs line 14**

Change:
```csharp
throw new ArgumentException("Set() must specified one argument to set - Set( value)");
```
To:
```csharp
throw new ArgumentException("Set() must specify one argument to set - Set(value)");
```

- [ ] **Step 8: Build to verify no compilation errors**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeded.

- [ ] **Step 9: Run all tests to verify no regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 10: Commit**

```bash
git add src/Tokenizer/Validators/MinLengthValidator.cs src/Tokenizer/Validators/MaxLengthValidator.cs src/Tokenizer/Transformers/SplitTransformer.cs src/Tokenizer/Transformers/SubstringAfterLastTransformer.cs src/Tokenizer/Transformers/SubstringBeforeLastTransformer.cs src/Tokenizer/Transformers/SetTransformer.cs
git commit -m "Fix XML doc copy-paste errors in validators and transformers"
```

---

### Task 2: Unify Exception Types to ArgumentException

**Files:**
- Modify: `src/Tokenizer/Validators/MinLengthValidator.cs:2,18,29`
- Modify: `src/Tokenizer/Validators/MaxLengthValidator.cs:2,18,29`
- Modify: `src/Tokenizer/Validators/ContainsValidator.cs:21`
- Modify: `src/Tokenizer/Validators/EndsWithValidator.cs:21`
- Modify: `src/Tokenizer/Transformers/SplitTransformer.cs:2,19`
- Modify: `src/Tokenizer/Transformers/RemoveEndTransformer.cs:19`
- Modify: `src/Tokenizer/Transformers/SubstringAfterTransformer.cs:19`
- Modify: `src/Tokenizer/Transformers/SubstringAfterLastTransformer.cs:19`
- Modify: `src/Tokenizer/Transformers/SubstringBeforeTransformer.cs:19`
- Modify: `src/Tokenizer/Transformers/SubstringBeforeLastTransformer.cs:19`
- Modify: `tests/Tokenizer.Tests/Validators/MinLengthValidatorTests.cs:50,61`
- Modify: `tests/Tokenizer.Tests/Validators/MaxLengthValidatorTests.cs:50,61`
- Modify: `tests/Tokenizer.Tests/Validators/ContainsValidatorTest.cs:29`
- Modify: `tests/Tokenizer.Tests/Validators/ContainsValidatorTests.cs:50`
- Modify: `tests/Tokenizer.Tests/Validators/EndsWithValidatorTest.cs:29`
- Modify: `tests/Tokenizer.Tests/Validators/EndsWithValidatorTests.cs:50`
- Modify: `tests/Tokenizer.Tests/Transformers/RemoveEndTransformerTest.cs:31`
- Modify: `tests/Tokenizer.Tests/Transformers/RemoveEndTransformerTests.cs:52`
- Modify: `tests/Tokenizer.Tests/Transformers/RemoveStartTransformerTest.cs:31`
- Modify: `tests/Tokenizer.Tests/Transformers/RemoveStartTransformerTests.cs:52`
- Modify: `tests/Tokenizer.Tests/Transformers/RemoveTransformerTest.cs:22`
- Modify: `tests/Tokenizer.Tests/Transformers/RemoveTransformerTests.cs:32`
- Modify: `tests/Tokenizer.Tests/Transformers/SplitTransformerTest.cs:38`
- Modify: `tests/Tokenizer.Tests/Transformers/SplitTransformerTests.cs:53`
- Modify: `tests/Tokenizer.Tests/Transformers/SubstringAfterTransformerTests.cs:32`
- Modify: `tests/Tokenizer.Tests/Transformers/SubstringAfterLastTransformerTests.cs:32`
- Modify: `tests/Tokenizer.Tests/Transformers/SubstringBeforeTransformerTests.cs:32`
- Modify: `tests/Tokenizer.Tests/Transformers/SubstringBeforeLastTransformerTests.cs:32`

**Note:** The singular test files (e.g., `ContainsValidatorTest.cs`) will be deleted in Task 5 — but we still update them here so tests pass at every commit.

- [ ] **Step 1: Update MinLengthValidator.cs**

Remove the `using Tokens.Exceptions;` import (line 2). Replace both `ValidationException` throws:

Line 18 (already says "must specify" after Task 1) — change:
```csharp
throw new ValidationException("You must specify a MinLength value, e.g. 'MinLength(50)'");
```
To:
```csharp
throw new ArgumentException("You must specify a MinLength value, e.g. 'MinLength(50)'");
```

Line 29 — change:
```csharp
throw new ValidationException("MinLength parameter must be an integer", ex);
```
To:
```csharp
throw new ArgumentException("MinLength parameter must be an integer", ex);
```

Add `using System;` if not already present (it is — line 1).

- [ ] **Step 2: Update MaxLengthValidator.cs**

Remove the `using Tokens.Exceptions;` import (line 2). Replace both `ValidationException` throws:

Line 18 (already says "must specify" after Task 1) — change:
```csharp
throw new ValidationException("You must specify a MaxLength value, e.g. 'MaxLength(255)'");
```
To:
```csharp
throw new ArgumentException("You must specify a MaxLength value, e.g. 'MaxLength(255)'");
```

Line 29 — change:
```csharp
throw new ValidationException("MaxLength parameter must be an integer", ex);
```
To:
```csharp
throw new ArgumentException("MaxLength parameter must be an integer", ex);
```

- [ ] **Step 3: Update ContainsValidator.cs**

Line 21 — change:
```csharp
throw new TokenizerException($"Contains(): missing argument processing: {value}");
```
To:
```csharp
throw new ArgumentException($"Contains(): missing argument processing: {value}");
```

Remove the `using Tokens.Exceptions;` import if it becomes unused.

- [ ] **Step 4: Update EndsWithValidator.cs**

Line 21 — change:
```csharp
throw new TokenizerException($"EndsWith(): missing argument processing: {value}");
```
To:
```csharp
throw new ArgumentException($"EndsWith(): missing argument processing: {value}");
```

Remove the `using Tokens.Exceptions;` import if it becomes unused.

- [ ] **Step 5: Update SplitTransformer.cs**

Remove the `using Tokens.Exceptions;` import (line 2). Line 19 — change:
```csharp
if (args == null || args.Length != 1) throw new TokenizerException($"Split(value): missing arguments processing: {value}");
```
To:
```csharp
if (args == null || args.Length != 1) throw new ArgumentException($"Split(value): missing arguments processing: {value}");
```

Add `using System;` if not already present (it is — line 1).

- [ ] **Step 6: Update RemoveEndTransformer.cs**

Line 19 — change `TokenizerException` to `ArgumentException`. Remove `using Tokens.Exceptions;` if unused. Add `using System;` if needed.

- [ ] **Step 7: Update SubstringAfterTransformer.cs**

Line 19 — change `TokenizerException` to `ArgumentException`. Remove `using Tokens.Exceptions;` if unused. Add `using System;` if needed.

- [ ] **Step 8: Update SubstringAfterLastTransformer.cs**

Line 19 — change `TokenizerException` to `ArgumentException`. Remove `using Tokens.Exceptions;` if unused. Add `using System;` if needed.

- [ ] **Step 9: Update SubstringBeforeTransformer.cs**

Line 19 — change `TokenizerException` to `ArgumentException`. Remove `using Tokens.Exceptions;` if unused. Add `using System;` if needed.

- [ ] **Step 10: Update SubstringBeforeLastTransformer.cs**

Line 19 — change `TokenizerException` to `ArgumentException`. Remove `using Tokens.Exceptions;` if unused. Add `using System;` if needed.

- [ ] **Step 11: Build to verify compilation**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeded.

- [ ] **Step 12: Update test files — replace exception type assertions**

In every test file listed above, replace:
- `Assert.Throws<ValidationException>` → `Assert.Throws<ArgumentException>`
- `Assert.Throws<TokenizerException>` → `Assert.Throws<ArgumentException>`

Also update `using` statements in test files:
- Remove `using Tokens.Exceptions;` where it becomes unused
- Add `using System;` where needed for `ArgumentException`

- [ ] **Step 13: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 14: Commit**

```bash
git add src/Tokenizer/Validators/ src/Tokenizer/Transformers/ tests/Tokenizer.Tests/Validators/ tests/Tokenizer.Tests/Transformers/
git commit -m "Unify decorator argument exceptions to ArgumentException"
```

---

### Task 3: Fix NewLine Casing Inconsistency

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs:20,63,99`
- Modify: `src/Tokenizer/Compilation/TokenParser.cs:171,186`
- Modify: `src/Tokenizer/Compilation/Definitions/TokenDefinition.cs:37`
- Modify: `src/Tokenizer/Compilation/Binders/TemplateBinder.cs:21,78,124,126`
- Modify: `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs:89`
- Modify: All test files referencing `TerminateOnNewline`

- [ ] **Step 1: Rename in TokenizerOptions.cs**

Replace all occurrences of `TerminateOnNewline` with `TerminateOnNewLine` (3 occurrences at lines 20, 63, 99).

- [ ] **Step 2: Rename in TokenDefinition.cs**

Line 37 — change:
```csharp
public bool TerminateOnNewline { get; set; }
```
To:
```csharp
public bool TerminateOnNewLine { get; set; }
```

- [ ] **Step 3: Rename in TokenParser.cs**

Line 171 — change:
```csharp
token.TerminateOnNewLine = preToken.TerminateOnNewline;
```
To:
```csharp
token.TerminateOnNewLine = preToken.TerminateOnNewLine;
```

Line 186 — change:
```csharp
if (token.TerminateOnNewLine == false && template.Options.TerminateOnNewline)
```
To:
```csharp
if (token.TerminateOnNewLine == false && template.Options.TerminateOnNewLine)
```

- [ ] **Step 4: Rename in TemplateBinder.cs**

Replace all occurrences of `TerminateOnNewline` with `TerminateOnNewLine` (4 occurrences at lines 21, 78, 124, 126).

Note: The local variable `globalTerminateOnNewline` (line 21) should also be renamed to `globalTerminateOnNewLine` for consistency.

- [ ] **Step 5: Rename in FrontMatterBinder.cs**

Line 89 — change:
```csharp
template.Options.TerminateOnNewline = ParseBoolean(value, rawName, entry);
```
To:
```csharp
template.Options.TerminateOnNewLine = ParseBoolean(value, rawName, entry);
```

- [ ] **Step 6: Update test files**

Search all test files for `TerminateOnNewline` (lowercase 'l') and replace with `TerminateOnNewLine`. There are approximately 24 occurrences across test files.

Run: Search for `TerminateOnNewline` across `tests/` directory and replace all with `TerminateOnNewLine`.

- [ ] **Step 7: Build to verify compilation**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeded.

- [ ] **Step 8: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 9: Commit**

```bash
git add -u
git commit -m "Standardize TerminateOnNewLine casing to match .NET convention"
```

---

### Task 4: Rename Boolean Properties on Token

**Files:**
- Modify: `src/Tokenizer/Token.cs` — property declarations + private method rename
- Modify: `src/Tokenizer/Diagnostics/DiagnosticEventType.cs` — enum rename
- Modify: All source files referencing `.Optional`, `.Repeating`, `.Required`, `.Concatenate`, `.ConsiderOnce`, `ConsiderOnceTokenRemoved`
- Modify: All test files referencing the above

This task has the largest blast radius. Use find-and-replace across the codebase for each rename.

- [ ] **Step 1: Rename `Optional` → `IsOptional`**

In `src/Tokenizer/Token.cs:60`, rename the property declaration. Then find-and-replace `.Optional` → `.IsOptional` across the entire `src/` and `tests/` directories.

**Caution:** Do NOT replace occurrences inside string literals, XML comments that refer to user-facing template syntax, or the word "Optional" when it's part of a different identifier. Verify each replacement is a property access.

- [ ] **Step 2: Rename `Repeating` → `IsRepeating`**

In `src/Tokenizer/Token.cs:66`, rename the property declaration. Then find-and-replace `.Repeating` → `.IsRepeating` across the entire `src/` and `tests/` directories.

Same caution as above — only replace property accesses.

- [ ] **Step 3: Rename `Required` → `IsRequired`**

In `src/Tokenizer/Token.cs:78`, rename the property declaration. Then find-and-replace `.Required` → `.IsRequired` across the entire `src/` and `tests/` directories.

**Extra caution:** `Required` may appear in contexts unrelated to the `Token` property (e.g., XML doc text, string literals). Only replace property accesses.

- [ ] **Step 4: Rename `Concatenate` → `CanConcatenate` (property)**

In `src/Tokenizer/Token.cs:111`, rename the property declaration. Then find-and-replace `.Concatenate` → `.CanConcatenate` across the entire `src/` and `tests/` directories.

**Do NOT replace:** `ConcatenationString`, `ConcatenateValues`, or other identifiers containing "Concatenate" as a substring.

- [ ] **Step 5: Rename private method `CanConcatenate` → `CanConcatenateValues`**

In `src/Tokenizer/Token.cs:364`, rename the internal method:

Change:
```csharp
internal bool CanConcatenate(object? existingValue, object newValue)
```
To:
```csharp
internal bool CanConcatenateValues(object? existingValue, object newValue)
```

Also update the call site in `Token.cs` (in the `Assign` method, approximately line 252) where `CanConcatenate(current, assignedValue)` is called — change to `CanConcatenateValues(current, assignedValue)`.

- [ ] **Step 6: Rename `ConsiderOnce` → `IsSingleUse`**

In `src/Tokenizer/Token.cs:122`, rename the property declaration. Then find-and-replace `.ConsiderOnce` → `.IsSingleUse` across the entire `src/` and `tests/` directories.

Also update `TokenDefinition.cs` which has its own `ConsiderOnce` property.

- [ ] **Step 7: Rename `DiagnosticEventType.ConsiderOnceTokenRemoved` → `SingleUseTokenRemoved`**

Find-and-replace `ConsiderOnceTokenRemoved` → `SingleUseTokenRemoved` across the entire codebase. This appears in:
- `src/Tokenizer/Diagnostics/DiagnosticEventType.cs` (definition)
- `src/Tokenizer/Tokenization/TokenizationEngine.cs` (usage)
- Any diagnostic-related test files

- [ ] **Step 8: Update log messages**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs:458` area, update the log message:

Change:
```csharp
log.LogTrace("Backtracking: Removing ConsiderOnce token '{TokenName}' ({TokenId}) and marking as miss",
```
To:
```csharp
log.LogTrace("Backtracking: Removing single-use token '{TokenName}' ({TokenId}) and marking as miss",
```

- [ ] **Step 9: Build to verify compilation**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeded.

- [ ] **Step 10: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 11: Commit**

```bash
git add -u
git commit -m "Rename boolean properties on Token to follow .NET naming guidelines"
```

---

### Task 5: Remove Duplicate Test Files

**Files:**
- Delete: `tests/Tokenizer.Tests/Validators/ContainsValidatorTest.cs`
- Delete: `tests/Tokenizer.Tests/Validators/EndsWithValidatorTest.cs`
- Delete: `tests/Tokenizer.Tests/Transformers/RemoveEndTransformerTest.cs`
- Delete: `tests/Tokenizer.Tests/Transformers/RemoveStartTransformerTest.cs`
- Delete: `tests/Tokenizer.Tests/Transformers/RemoveTransformerTest.cs`
- Delete: `tests/Tokenizer.Tests/Transformers/SplitTransformerTest.cs`

All singular files have been verified as strict subsets of their plural counterparts — every test scenario in the singular file has equivalent coverage in the plural file (with Gherkin-style naming). No unique tests exist in any singular file.

- [ ] **Step 1: Delete all 6 singular test files**

```bash
git rm tests/Tokenizer.Tests/Validators/ContainsValidatorTest.cs
git rm tests/Tokenizer.Tests/Validators/EndsWithValidatorTest.cs
git rm tests/Tokenizer.Tests/Transformers/RemoveEndTransformerTest.cs
git rm tests/Tokenizer.Tests/Transformers/RemoveStartTransformerTest.cs
git rm tests/Tokenizer.Tests/Transformers/RemoveTransformerTest.cs
git rm tests/Tokenizer.Tests/Transformers/SplitTransformerTest.cs
```

- [ ] **Step 2: Run all tests to verify no coverage loss**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass. Test count should decrease (duplicate tests removed), but no test scenarios are lost.

- [ ] **Step 3: Commit**

```bash
git commit -m "Remove duplicate test files (singular 'Test' variants)"
```

---

### Task 6: Clean Up Stale Preprocessor Guards

**Files:**
- Modify: `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs` — 8 occurrences of `NET6_0_OR_GREATER`
- Modify: `src/Tokenizer/Extensions/StringExtensions.cs:235-249` — remove `DOTNET35` guard

**Note:** The `.csproj` already targets `netstandard2.0;net8.0;net10.0` — `net6.0` was previously removed. This task only updates the preprocessor directives to match.

- [ ] **Step 1: Replace `NET6_0_OR_GREATER` with `NET8_0_OR_GREATER` in TemplateLexer.cs**

Replace all 8 occurrences at lines 59, 70, 81, 92, 104, 120, 136, 378:

Find: `NET6_0_OR_GREATER`
Replace: `NET8_0_OR_GREATER`

- [ ] **Step 2: Remove `DOTNET35` guard in StringExtensions.cs**

Replace lines 235-249:
```csharp
    public static bool IsNullOrWhiteSpace(this string value)
    {
#if DOTNET35
        var result = string.IsNullOrEmpty(value);

        if (!result)
        {
            result = value.Trim() == string.Empty;
        }

        return result;
#else
        return string.IsNullOrWhiteSpace(value);
#endif
    }
```

With:
```csharp
    public static bool IsNullOrWhiteSpace(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
```

- [ ] **Step 3: Build to verify compilation on all targets**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeded for all target frameworks (netstandard2.0, net8.0, net10.0).

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Lexer/TemplateLexer.cs src/Tokenizer/Extensions/StringExtensions.cs
git commit -m "Update stale preprocessor guards: NET6->NET8, remove DOTNET35"
```
