# Tier 1: Correctness and Consistency

Fix bugs, typos, and inconsistencies that erode trust in the public API before more users depend on v3 names.

## 1. XML Doc Copy-Paste Errors

Straightforward text corrections. No behavioral change.

| File | Line | Current | Fix |
|------|------|---------|-----|
| `MinLengthValidator.cs` | 7 | "maximum length requirement" | "minimum length requirement" |
| `MinLengthValidator.cs` | 18 | "must specified" | "must specify" |
| `MaxLengthValidator.cs` | 18 | "must specified" | "must specify" |
| `SplitTransformer.cs` | 7 | "Removes occurrences of a string from then end" | "Splits a token value on a specified delimiter" |
| `SubstringAfterLastTransformer.cs` | 7 | "first occurence" | "last occurrence" |
| `SubstringBeforeLastTransformer.cs` | 7 | "first occurence" | "last occurrence" |
| `SetTransformer.cs` | 14 | "must specified" | "must specify" |

## 2. Unify Exception Types

Replace all `ValidationException` and `TokenizerException` usages for missing/invalid decorator arguments with `ArgumentException`. This follows standard .NET conventions — these errors all mean "the template author misconfigured a decorator's arguments."

### Files to change

| File | Current Exception | New Exception |
|------|-------------------|---------------|
| `MinLengthValidator.cs` (lines 18, 29) | `ValidationException` | `ArgumentException` |
| `MaxLengthValidator.cs` (lines 18, 29) | `ValidationException` | `ArgumentException` |
| `ContainsValidator.cs:21` | `TokenizerException` | `ArgumentException` |
| `EndsWithValidator.cs:21` | `TokenizerException` | `ArgumentException` |
| `SplitTransformer.cs:19` | `TokenizerException` | `ArgumentException` |
| `RemoveEndTransformer.cs:19` | `TokenizerException` | `ArgumentException` |
| `SubstringAfterTransformer.cs:19` | `TokenizerException` | `ArgumentException` |
| `SubstringAfterLastTransformer.cs:19` | `TokenizerException` | `ArgumentException` |
| `SubstringBeforeTransformer.cs:19` | `TokenizerException` | `ArgumentException` |
| `SubstringBeforeLastTransformer.cs:19` | `TokenizerException` | `ArgumentException` |

`IsNotValidator.cs` and `SetTransformer.cs` already use `ArgumentException` — no change needed.

### Scope

Only unifying "misconfigured args" exceptions. Not touching `TokenizerException` usages elsewhere (e.g., parsing errors). Not removing `ValidationException` or `TokenizerException` classes themselves.

Existing tests that assert on the old exception types must be updated to expect `ArgumentException`.

## 3. Fix NewLine Casing

Standardize on `TerminateOnNewLine` (matching `Environment.NewLine` .NET convention).

- Rename `TokenizerOptions.TerminateOnNewline` to `TerminateOnNewLine`
- `Token.TerminateOnNewLine` already uses the correct casing — no change
- Update all references in: `TemplateBinder`, `TokenDefinition`, `TokenParser`, `FrontMatterBinder`, and tests

## 4. Boolean Property Renames on Token

Rename boolean properties to follow .NET Framework Design Guidelines (`Is` for state, `Can` for capability):

| Current | New | Rationale |
|---------|-----|-----------|
| `Optional` | `IsOptional` | State property |
| `Repeating` | `IsRepeating` | State property |
| `Required` | `IsRequired` | State property |
| `Concatenate` | `CanConcatenate` | Capability flag |
| `ConsiderOnce` | `IsSingleUse` | State property |

### Additional renames

- Private method `CanConcatenate(current, assignedValue)` in `Token.cs` renamed to `CanConcatenateValues` to avoid collision with the new property name
- `DiagnosticEventType.ConsiderOnceTokenRemoved` renamed to `SingleUseTokenRemoved`
- All references across source and tests updated

## 5. Remove Duplicate Test Files

Six pairs of duplicate test files exist (singular `Test` vs plural `Tests`). Standardize on plural `Tests` convention.

### Pairs

**Validators:**
1. `ContainsValidatorTest.cs` / `ContainsValidatorTests.cs`
2. `EndsWithValidatorTest.cs` / `EndsWithValidatorTests.cs`

**Transformers:**
3. `RemoveEndTransformerTest.cs` / `RemoveEndTransformerTests.cs`
4. `RemoveStartTransformerTest.cs` / `RemoveStartTransformerTests.cs`
5. `RemoveTransformerTest.cs` / `RemoveTransformerTests.cs`
6. `SplitTransformerTest.cs` / `SplitTransformerTests.cs`

### Process for each pair

1. Diff the two files to identify unique tests
2. Merge any unique tests from the singular file into the plural file
3. Delete the singular file

## 6. Clean Up Stale Preprocessor Guards

### Drop net6.0 target

.NET 6 is EOL (November 2024). The project targets .NET Standard 2.0 (covers .NET 6 consumers), .NET 8 (current LTS), and .NET 10 (latest). The explicit net6.0 target adds no value.

- Remove `net6.0` from target frameworks in the `.csproj`

### Update conditional compilation

- Change `NET6_0_OR_GREATER` to `NET8_0_OR_GREATER` in `TemplateLexer.cs` (8 occurrences) and anywhere else they appear
- Remove `#if DOTNET35` guard in `StringExtensions.cs:237` — dead code for a framework the library has never targeted

## Implementation Order

1. **XML doc fixes** (item 1) — zero risk, independent
2. **Exception unification** (item 2) — independent, update tests in same commit
3. **NewLine casing** (item 3) — independent rename
4. **Boolean renames** (item 4) — touches the most files, highest rename volume
5. **Duplicate test files** (item 5) — isolated to tests
6. **Stale guards + drop net6.0** (item 6) — affects build, should go last to surface CI issues separately
