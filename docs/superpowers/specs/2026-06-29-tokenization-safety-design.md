# Tokenization Safety and DoS Protection

## Context

The tokenization engine has an infinite loop bug when templates contain consecutive tokens with no separating text (empty preambles). The template `{a}{b}{c}` with input `abc` causes the engine to spin forever because `Advance(0)` makes no progress. This bug exists in both the old monolithic code (master) and the v3 refactored engine, though v3 made it easier to trigger by removing an accidental `replacement.Length > 0` guard.

Beyond this specific bug, the library is a published NuGet package. Consumers may process untrusted templates and input, making it vulnerable to denial-of-service through crafted inputs.

## Goals

1. Fix the empty-preamble infinite loop bug
2. Add safety limits to prevent DoS from untrusted templates and input
3. Follow established .NET library patterns (System.Text.Json, Newtonsoft.Json)

## Non-Goals

- Adding async/CancellationToken support (the library is synchronous by design)
- Changing the public API surface beyond new options properties
- Performance optimization (separate concern)

## Design

### 1. Empty-Preamble Bug Fix

#### Root Cause

Two code paths in `TokenizationEngine` call `Advance(preamble.Length)` without handling the case where preamble is empty:

**Path A — `HandleFirstTokenMatch`** (line 586-597): Sets candidates, calls `Advance(0)`, then `continue`. The next iteration re-enters the match logic at the same position. If the next unmatched token also has an empty preamble, it matches immediately, triggering `HandleTokenSwitch` with an empty replacement.

**Path B — `HandleTokenSwitch`** (line 609-633): Tries to assign the current candidates with whatever replacement has been accumulated, then sets up new candidates and calls `Advance(0)`. When `replacement` is empty (no input consumed since last switch), this creates a tight loop cycling through all tokens without consuming any input.

The old code (master) had this guard before the token switch block:

```csharp
if (replacement.Length > 0)
{
    // ... try assign, switch candidates, advance, continue
}

// Fall through: consume a character
replacement.Append(next);
enumerator.Next();
```

This fall-through meant that when no input had been consumed between two token matches, the engine would consume one character before trying to match again. The v3 refactor extracted `HandleTokenSwitch` but made it unconditional, losing this behavior.

#### Fix

Restore the `replacement.Length > 0` guard in the main loop before calling `HandleTokenSwitch`. When replacement is empty (no input consumed since the last token match), fall through to `HandleNoTokenMatch` which calls `enumerator.Next()` and appends to replacement.

In the main loop (around line 125-156 of `TokenizationEngine.cs`), the token match branch becomes:

```
if match found:
    if no candidates yet:
        HandleFirstTokenMatch (set up candidates, advance past preamble)
        continue
    if replacement.Length > 0:
        HandleTokenSwitch (assign previous, set up new candidates)
    else:
        HandleNoTokenMatch (consume one character into replacement)
```

This ensures the enumerator always makes progress. With empty preambles, each token gets at least one character before the next token can match.

#### Expected Behavior for Empty-Preamble Templates

Template `{a}{b}{c}` with input `abc`:

| Step | Position | Action | Result |
|------|----------|--------|--------|
| 1 | 0 | Match `{a}` (first token, empty preamble) | candidates=[a], advance 0 |
| 2 | 0 | Match `{b}` but replacement is empty | fall through, consume `a` into replacement |
| 3 | 1 | Match `{b}`, replacement=`a` | switch: assign a="a", candidates=[b], advance 0 |
| 4 | 1 | Match `{c}` but replacement is empty | fall through, consume `b` into replacement |
| 5 | 2 | Match `{c}`, replacement=`b` | switch: assign b="b", candidates=[c], advance 0 |
| 6 | 2 | No more tokens to match | consume `c` into replacement |
| 7 | end | Process remaining candidates | assign c="c" |

Result: `a="a", b="b", c="c"`

#### Additional Empty-Preamble Test Cases

| Template | Input | Expected Result |
|----------|-------|-----------------|
| `{a}{b}{c}` | `abc` | a="a", b="b", c="c" |
| `{a}{b}{c}` | `abcdef` | a="a", b="b", c="cdef" (last token gets remainder) |
| `{a}{b}{c}` | `ab` | a="a", b="b", c not matched (miss) |
| `{a}{b}{c}` | `a` | a="a", b not matched, c not matched |
| `{a}{b}{c}` | `` | empty input throws ArgumentException (existing behavior) |
| `{a}:{b}:{c}` | `x:y:z` | a="x", b="y", c="z" (mixed preambles) |
| `{a}{b}` | `x` | a="x", b not matched |
| `X{a}{b}Y{c}` | `XabYc` | a="a", b="b", c="c" (preamble on first and last) |
| `{a}` | `hello` | a="hello" (single token, no preamble) |

### 2. Safety Limits

#### Options Properties

Add to `TokenizerOptions`:

```csharp
/// <summary>
/// Maximum allowed length for input text. Default: 1,048,576 (1MB).
/// Set to 0 to disable.
/// </summary>
public int MaxInputLength { get; set; } = 1_048_576;

/// <summary>
/// Maximum allowed length for template pattern text. Default: 65,536 (64KB).
/// Set to 0 to disable.
/// </summary>
public int MaxTemplateLength { get; set; } = 65_536;

/// <summary>
/// Maximum number of tokens allowed in a template. Default: 500.
/// Set to 0 to disable.
/// </summary>
public int MaxTokenCount { get; set; } = 500;

/// <summary>
/// Maximum number of iterations in the tokenization loop.
/// Default: 0 (auto-calculated as input.Length * 2).
/// Set to a positive value to override.
/// </summary>
public int MaxIterations { get; set; } = 0;
```

`0` means disabled for size/count limits, and auto-calculated for `MaxIterations`. This lets callers opt out if they know their inputs are safe.

#### Where Checks Are Enforced

| Limit | Checked In | When | Exception |
|-------|-----------|------|-----------|
| `MaxTemplateLength` | `TokenParser.Parse()` | Before lexing begins | `ParsingException` |
| `MaxTokenCount` | `TokenParser.Parse()` | After parsing, before returning | `ParsingException` |
| `MaxInputLength` | `Tokenizer.Tokenize()` (private method) | Before creating context | `TokenizerException` |
| `MaxIterations` | `TokenizationEngine.ProcessTokenization()` | Counter incremented each loop iteration | `TokenizerException` |

#### Exception Pattern

Follow System.Text.Json convention: throw exceptions on limit violations with clear messages. All exceptions extend `TokenizerException`, catchable with a single handler.

Example messages:
- `"Input length 2,000,000 exceeds maximum allowed length of 1,048,576. Increase TokenizerOptions.MaxInputLength to allow larger inputs."`
- `"Template contains 750 tokens, exceeding maximum of 500. Increase TokenizerOptions.MaxTokenCount to allow more tokens."`
- `"Tokenization exceeded maximum iteration count of 2,000. This may indicate a problematic template pattern. Increase TokenizerOptions.MaxIterations to allow more iterations."`

Messages include the actual value, the limit, and which option to change. This matches the developer experience of System.Text.Json's `JsonException` messages.

#### Options Propagation

`TokenizerOptions` is already passed through the pipeline:
- `TokenParser` receives it via constructor
- `Template.Options` carries it to the engine
- `TokenizationEngine` accesses it via `template.Options`

No new wiring needed. The engine already has access to options everywhere it needs them.

### 3. Testing Strategy

#### Empty-Preamble Bug (TDD red/green)

Test class: `TokenizationEngineEmptyPreambleTests`

All tests use `TokenParser.Parse()` to create templates (not builders) so the full compilation pipeline is exercised.

**Core behavior tests:**
- `GivenConsecutiveTokensWithNoPreambles_WhenTokenizing_ThenAssignsOneCharEach` — template `{a}{b}{c}`, input `abc`, assert a="a", b="b", c="c"
- `GivenConsecutiveTokensWithNoPreambles_WhenInputLongerThanTokens_ThenLastTokenGetsRemainder` — template `{a}{b}{c}`, input `abcdef`, assert c="cdef"
- `GivenConsecutiveTokensWithNoPreambles_WhenInputShorterThanTokens_ThenUnmatchedTokensAreMisses` — template `{a}{b}{c}`, input `ab`, assert 2 matches, 1 miss
- `GivenSingleTokenWithNoPreamble_WhenTokenizing_ThenGetsEntireInput` — template `{a}`, input `hello`, assert a="hello"
- `GivenMixedPreambleAndNoPreambleTokens_WhenTokenizing_ThenMatchesCorrectly` — template `X{a}{b}Y{c}`, input `XabYc`
- `GivenTwoConsecutiveTokens_WhenSingleCharInput_ThenFirstTokenMatchesSecondMisses` — template `{a}{b}`, input `x`

**Regression/safety tests:**
- `GivenManyConsecutiveTokensWithNoPreambles_WhenTokenizing_ThenCompletesWithinIterationLimit` — template with 100 consecutive tokens, verify it completes (does not hang)
- `GivenEmptyPreambleRepeatingToken_WhenTokenizing_ThenDoesNotHang` — template `{item*}` with multi-line input

#### Safety Limit Tests

Test class: `TokenizerSafetyLimitTests`

Tests use the public `Tokenizer` API (not engine directly) to verify limits are enforced end-to-end.

**Input limits:**
- `GivenInputExceedingMaxLength_WhenTokenizing_ThenThrowsTokenizerException`
- `GivenInputAtMaxLength_WhenTokenizing_ThenProcessesSuccessfully`
- `GivenMaxInputLengthDisabled_WhenTokenizingLargeInput_ThenProcessesSuccessfully`

**Template limits:**
- `GivenTemplateExceedingMaxLength_WhenParsing_ThenThrowsParsingException`
- `GivenTemplateAtMaxLength_WhenParsing_ThenProcessesSuccessfully`
- `GivenTemplateExceedingMaxTokenCount_WhenParsing_ThenThrowsParsingException`
- `GivenTemplateAtMaxTokenCount_WhenParsing_ThenProcessesSuccessfully`
- `GivenMaxTemplateLengthDisabled_WhenParsingLargeTemplate_ThenProcessesSuccessfully`

**Iteration limits:**
- `GivenMaxIterationsExceeded_WhenTokenizing_ThenThrowsTokenizerException`
- `GivenAutoMaxIterations_WhenTokenizing_ThenCalculatesFromInputLength`
- `GivenCustomMaxIterations_WhenTokenizing_ThenUsesCustomValue`

**Exception message tests:**
- `GivenInputExceedingMaxLength_WhenThrown_ThenMessageIncludesActualAndMaxValues`
- `GivenTemplateExceedingMaxTokenCount_WhenThrown_ThenMessageIncludesActualAndMaxValues`

**Default safety tests:**
- `GivenDefaultOptions_WhenTokenizingNormalInput_ThenProcessesSuccessfully` — verify defaults don't interfere with normal usage
