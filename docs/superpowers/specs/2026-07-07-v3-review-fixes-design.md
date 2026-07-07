# v3 Review Fixes Design

## Overview

Address all 38 High, Medium, and Low issues from the 2026-07-07 v3 branch review, plus add a new TemplateOptionsCascadeTests class to verify option cascading at all three levels (default, instance, front matter). Delta-to-staff (D) items are out of scope.

Source: `docs/superpowers/specs/2026-07-07-v3-review.md`

---

## Group 1: Token & Tokenization Bugs

**Issues:** H2, H3, M1, M3, M7, L4, L6

### H2 — TerminateOnNewLine off-by-one

**File:** `src/Tokenizer/Token.cs:253`
**Fix:** Change `if (index > 0)` to `if (index >= 0)` in `PrepareValue`. A value starting with `\n` should be truncated to empty string when `TerminateOnNewLine` is set.

### H3 — SetDictionaryValue discards existing non-list value

**File:** `src/Tokenizer/Token.cs:211`
**Fix:** When `IsRepeating` and the existing dictionary value is not a `List<object>`, wrap it instead of discarding:
```csharp
list = dictionary[Name] as List<object> ?? new List<object> { dictionary[Name] };
```

### M1 — TokenizeResult double iteration

**File:** `src/Tokenizer/TokenizeResult.cs:32-88`
**Fix:** Rewrite all four `First`/`FirstOrDefault` methods as single-pass foreach loops:
- `First(key)`: iterate, return on match, throw at end
- `First<T>(key)`: same with cast
- `FirstOrDefault(key)`: iterate, return on match, return null at end
- `FirstOrDefault<T>(key)`: same with default(T)

### M3 — Token excessive mutable surface

**File:** `src/Tokenizer/Token.cs:39-111`
**Fix:** Audit each `internal set` property. For properties never mutated after `TokenFactory.Create()` / `TokenBinder.Bind()`, remove the setter and set via constructor or init-only. Likely candidates for locking down: `Location`, `Id`, `IsFrontMatterToken`, `IsNull`, `DependsOnId`. Properties that must remain `internal set` (mutated by `OptionApplier` or tokenization): `IsOptional`, `TerminateOnNewLine`.

### M7 — Swallowed MissingMemberException

**File:** `src/Tokenizer/Token.cs:179-185`
**Fix:** In the `catch (MissingMemberException)` block, when `IgnoreMissingProperties` is true, record a diagnostic event before continuing:
```csharp
catch (MissingMemberException)
{
    if (!options.IgnoreMissingProperties) throw;
    collector.Record(DiagnosticEventType.TokenMissed,
        tokenName: Name, tokenId: Id,
        detail: $"Property '{Name}' not found on target type; ignored via IgnoreMissingProperties");
}
```
The `collector` parameter is already available in `Assign`.

### L4 — IsAssignableFrom reflection per call

**File:** `src/Tokenizer/TokenDecoratorContext.cs:69,75`
**Fix:** Replace computed properties with `readonly bool` fields set in the constructor:
```csharp
private readonly bool _isTransformer;
private readonly bool _isValidator;

public TokenDecoratorContext(Type decoratorType)
{
    DecoratorType = decoratorType;
    _isTransformer = typeof(ITokenTransformer).IsAssignableFrom(decoratorType);
    _isValidator = typeof(ITokenValidator).IsAssignableFrom(decoratorType);
}

public bool IsTransformer => _isTransformer;
public bool IsValidator => _isValidator;
```

### H4 — IntegratedHintStrategy diverges from ContainsHintStrategy

**File:** `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs:28-51`
**Fix:** `IntegratedHintStrategy.OnTokenMatched` only tracks `token.Preamble`. If a hint refers to text that appears in the extracted token *value* rather than the preamble, it will never match — whereas `ContainsHintStrategy` searches the full raw input. Fix: also track matched token values. Add a second `HashSet<string> _matchedValues`. In `OnTokenMatched`, also add the replacement/value text when available. In `PostProcess`, check both `_matchedPreambles` and `_matchedValues` for hint satisfaction. Add XML doc on `TokenizeAsync` noting that hint matching in streaming mode is approximated from matched preambles and values rather than searching the full input.

### L6 — IsOnlySpaces unnecessary ToCharArray

**File:** `src/Tokenizer/Extensions/StringExtensions.cs:208`
**Fix:** Change `foreach (var character in value.ToCharArray())` to `foreach (var character in value)`.

---

## Group 2: Compilation Pipeline Cleanup

**Issues:** H1, H5, M2, M9, M10, L2, L3, L7

### H1 — Duplicated boolean parsing

**Files:** `FrontMatterBinder.cs:137-147`, `TemplateBinder.cs:256-273`
**Fix:** Make `FrontMatterBinder.ParseBoolean` `internal static` (currently `private static`). Remove the inline true/false/yes/no/on/off parsing from `TemplateBinder.IsFrontMatterOptionTrue` and call the shared method instead. Error policy: throw on unknown values (matching FrontMatterBinder's existing behavior — silent-false-on-unknown is a bug).

Note: this method is eliminated entirely by H5, but if H5 lands first, the shared method still serves as the single source of truth for FrontMatterBinder.

### H5 — TemplateBinder re-parses front matter

**Files:** `TemplateBinder.cs:20-21`, `AstTemplateDefinitionParser.cs:32-34`
**Fix:** Change `TemplateBinder.Bind(TemplateDocument)` signature to `TemplateBinder.Bind(TemplateDocument, TokenizerOptions)`. Read `options.TerminateOnNewLine` and `options.TrimPreambleBeforeNewLine` from the parameter instead of re-parsing AST front matter via `IsFrontMatterOptionTrue`. Remove `IsFrontMatterOptionTrue` entirely. Update `AstTemplateDefinitionParser.Parse` to pass `result.Options` (which already has front matter applied by `FrontMatterBinder.Bind`).

### M2 — TerminateOnNewLine double application

**Files:** `TemplateBinder.cs:124-128`, `OptionApplier.cs:24-34`
**Fix:** With H5 applied, remove lines 124-128 from `TemplateBinder.Bind` (the `if (globalTerminateOnNewLine)` block). `OptionApplier.Apply` is the single point of responsibility for applying global options to tokens after they are created.

### M9 — No logging in compilation pipeline

**File:** `src/Tokenizer/Compilation/TemplateCompiler.cs`
**Fix:** Inject `ILogger<TemplateCompiler>` via constructor. Add:
- Debug-level log on compilation start with template content length
- Debug-level log on compilation success with token count
- Error-level log in catch blocks before rethrowing, with template content length and exception

The parser and binders are static/lightweight — logging at the compiler orchestrator level is sufficient.

### M10 — Stale conditional compilation guard

**File:** `src/Tokenizer/TemplateCollection.cs:32`
**Fix:** Change `#if NET6_0_OR_GREATER` to `#if NET8_0_OR_GREATER`.

### L2 — TokenReader doesn't dispose enumerator

**File:** `src/Tokenizer/Compilation/Parsing/TokenReader.cs:19`
**Fix:** Make `TokenReader` implement `IDisposable`. Dispose `_enumerator` in `Dispose()`. Update callers to use `using` statements.

### L3 — TemplateDefinitionEnumerator dead code

**File:** `src/Tokenizer/Compilation/Parsing/TemplateDefinitionEnumerator.cs`
**Fix:** Grep for references. If no production callers exist, remove the source file and its corresponding test file.

### L7 — Compilation failure discards diagnostics

**File:** `src/Tokenizer/Compilation/TemplateCompiler.cs:54-61`
**Fix:** In the catch block, attach the partial diagnostic result to the exception before rethrowing:
```csharp
catch (TokenizerException ex)
{
    ex.Data["DiagnosticResult"] = collector.GetResult();
    throw;
}
```

---

## Group 3: Safety & Security Hardening

**Issues:** M5, M6, L1, L9, L15

### M5 — BufferTextReaderAsync unbounded

**File:** `src/Tokenizer/TokenMatcher.cs:285-307`
**Fix:** Accept `TokenizerOptions` (or just `long maxInputLength`) as a parameter. Track total bytes written in the loop. After each `WriteAsync`, check: if `maxInputLength > 0 && totalBytes > maxInputLength`, throw `TokenizerException` with a message explaining the input exceeded `MaxInputLength` during buffering.

### M6 — EnsureSeekableAsync unbounded

**File:** `src/Tokenizer/TokenMatcher.cs:309-328`
**Fix:** Replace `CopyToAsync` with a chunked copy loop. Read in 81920-byte chunks, track total, throw `TokenizerException` if total exceeds `MaxInputLength` (when > 0).

### L1 — Regex infinite timeout on netstandard2.0

**File:** `src/Tokenizer/Extensions/StringExtensions.cs:17`
**Fix:** Change `TimeSpan.FromMilliseconds(-1)` to `TimeSpan.FromSeconds(1)`.

### L9 — CandidateProcessor bare catch

**File:** `src/Tokenizer/Tokenization/CandidateProcessor.cs:78-86`
**Fix:** Replace `catch (Exception e)` with specific catches:
```csharp
catch (TokenAssignmentException e) { /* existing warning logic */ }
catch (TypeConversionException e) { /* existing warning logic */ }
```
Let all other exceptions propagate.

### L15 — Unbounded regex cache

**File:** `src/Tokenizer/Validators/MatchesRegexValidator.cs:11`
**Fix:** Before `GetOrAdd`, check `RegexCache.Count`. If `>= 1024`, call `RegexCache.Clear()`. This is a cold path (templates are compiled once). Add XML doc comment noting that patterns should be a finite, developer-controlled set.

---

## Group 4: Diagnostics Fixes

**Issues:** M8, L8

### M8 — PreambleNearMissHintGenerator dead code

**File:** `src/Tokenizer/Tokenization/ResultBuilder.cs:159-160`
**Fix:** Pass the token's preamble when recording the missed event:
```csharp
collector.Record(DiagnosticEventType.TokenMissed,
    tokenName: token.Name, tokenId: token.Id,
    detail: token.Preamble);
```

### L8 — Async diagnostics null inputContent

**File:** `src/Tokenizer/Tokenizer.cs:333-334`
**Fix:** Document as a known limitation rather than attempting to buffer. Add XML doc comment on `TokenizeAsync` noting that alignment rendering and near-miss hints require the full input string, which is unavailable during streaming tokenization. Add an inline comment at the `DiagnosticCollector(inputContent: null)` call explaining the trade-off.

---

## Group 5: Spec Compliance

**Issues:** L10, L11, L12

### L10 — TokenizerOptions not sealed

**File:** `src/Tokenizer/TokenizerOptions.cs:9`
**Fix:** Add XML doc comment: `Record classes with protected copy constructors cannot be sealed in C#. The copy constructor is required for deep-copy semantics via 'with {}' expressions.`

### L11 — TokenizationContext dropped IDisposable

**File:** `src/Tokenizer/Tokenization/TokenizationContext.cs`
**Fix:** Add comment: `IDisposable removed — the enumerator lifecycle is managed by TokenizationSession, and there are no other disposable resources.`

### L12 — Unsealed public classes

**Fix:** Add `sealed` to all public classes that are not designed as extension points:
- `TemplateCollection`
- `TokenEnumerator`
- `FileLocation`
- `DiagnosticEvent`
- `DiagnosticIssue`
- `DiagnosticSummary`
- `DiagnosticResult`

Leave unsealed:
- Exception classes (standard .NET convention — exceptions are designed to be subclassed)
- AST node classes (`ContentNode`, `SyntaxNode` — use inheritance for `TextNode`/`TokenNode`)

---

## Group 6: Object Extensions

**Issues:** M4, L5

### M4 — List.Add type validation

**File:** `src/Tokenizer/Extensions/ObjectExtensions.cs:98`
**Fix:** Before `addMethod.Invoke`, resolve the list's element type via generic argument. Check `value.GetType()` is assignable to it. If not, attempt `Convert.ChangeType`. If that also fails, throw `TypeConversionException` with the token name (from caller context), value type, and target element type.

### L5 — propertyPath.Split per call

**File:** `src/Tokenizer/Extensions/ObjectExtensions.cs:45`
**Fix:** Add a `string[]? _pathSegments` field on `Token`. Populate lazily on first access. `ObjectExtensions.SetValue`/`GetValue` accept the cached segments instead of calling `Split('.')` each time. Alternatively, if the segments are always determined at compile time, set them in `TokenFactory.Create`.

---

## Group 7: Test Coverage

**Issues:** H7, H8, M11, M12, M13, M14, L13, L14, plus new TemplateOptionsCascadeTests

### H7 — TokenAssignmentException tests

**New file:** `tests/Tokenizer.Tests/Exceptions/TokenAssignmentExceptionTests.cs`
**Tests:**
- Constructor sets `Token` property correctly
- `Message` includes token name
- Inner exception is preserved
- Integration: tokenization with type mismatch throws `TokenAssignmentException`

### H8 — TypeConversionException + TokenMatcherException tests

**New files:**
- `tests/Tokenizer.Tests/Exceptions/TypeConversionExceptionTests.cs`
- `tests/Tokenizer.Tests/Exceptions/TokenMatcherExceptionTests.cs`

**Tests:** Constructor sets properties, message formatting, inner exception preservation.

### M11 — LexerException tests

**File:** `tests/Tokenizer.Tests/Exceptions/ExceptionLocationTests.cs` (extend existing)
**Tests:**
- `Message` override appends line/column to base message
- Inner-exception constructor preserves inner exception

### M12 — ParsingException tests

**File:** `tests/Tokenizer.Tests/Exceptions/ExceptionLocationTests.cs` (extend existing)
**Tests:**
- `Message` override appends line/column to base message

### M13 — DiagnosticResult tests

**New file:** `tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs`
**Tests:**
- `Failures` filters by `FailureTypes` correctly
- `ForToken(name)` returns only events for that token
- `FirstFailure` returns first event matching failure types
- Empty result returns empty collections

### M14 — TokenMatcherResult.GetBestMatch tests

**New file:** `tests/Tokenizer.Tests/TokenMatcherResultTests.cs`
**Tests:**
- Best match selected by hint match count (most wins)
- Tiebreak by token match count (most wins)
- Tiebreak by token count (fewest wins)
- Tiebreak by template ID (lowest wins)

### L13 — UnmatchedInputHintGenerator test

**New file:** `tests/Tokenizer.Tests/Diagnostics/Hints/UnmatchedInputHintGeneratorTests.cs`
**Tests:**
- `TryGenerateHint` returns null (documenting stub behavior)

### L14 — TokenizeResultBase.Success with HasOnlyFrontMatterTokens

**File:** Extend existing `TokenizeResultTests.cs`
**Tests:**
- Template with only front matter tokens, all matched → `Success` is true

### TemplateOptionsCascadeTests (new)

**New file:** `tests/Tokenizer.Tests/Compilation/TemplateOptionsCascadeTests.cs`

Verifies the three-level option cascade for every cascadable option:

1. **Default** — `Tokenizer.Create()` with no custom options → template gets default values
2. **Instance-level** — `Tokenizer.Create(new TokenizerOptions { X = value })` → template inherits instance option
3. **Front matter override** — Template with `---\nX: value\n---` overrides instance-level setting

Options to test:
- `TrimPreambleBeforeNewLine`
- `TerminateOnNewLine`
- `OutOfOrderTokens`
- `TrimLeadingWhitespaceInTokenPreamble`
- `EnableDiagnostics`
- `IgnoreMissingProperties`

Each option gets a test group with three scenarios. Tests compile a template and assert `Template.Options` values, then tokenize against sample input to verify runtime behavior matches the option setting.

---

## Group 8: Observability

**Issues:** H6

### H6 — Sync TokenizeCore no exception logging

**File:** `src/Tokenizer/Tokenizer.cs:131-196`
**Fix:** Wrap the body of `TokenizeCore` in a try/catch that mirrors the async path:
```csharp
try
{
    // existing body
}
catch (TokenizerException ex)
{
    _log.LogError(ex, "Tokenization failed for template {TemplateName}", template.Name);
    throw;
}
```

---

## Execution Order

Groups should be executed in this order due to dependencies:

1. **Group 2** (compilation pipeline) — H5/M2 change binder signatures that other groups depend on
2. **Group 1** (token bugs) — fixes in Token.cs, including M3 which may affect Group 2's output
3. **Group 6** (object extensions) — L5 adds path segments to Token, touches same file as Group 1
4. **Group 3** (safety) — independent, touches TokenMatcher and validators
5. **Group 4** (diagnostics) — depends on Group 1's M7 (both touch diagnostic recording)
6. **Group 5** (spec compliance) — independent, mostly documentation + sealing
7. **Group 8** (observability) — independent, touches Tokenizer.cs
8. **Group 7** (tests) — must run last, tests verify all other groups' fixes
