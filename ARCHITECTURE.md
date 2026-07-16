# Architecture

Tokenizer processes text in two phases: **compilation** (parsing a template pattern into an internal representation) and **tokenization** (matching input text against a compiled template to extract values).

## Compilation Pipeline

Template patterns are compiled through a multi-stage pipeline:

```
pattern string
    -> TemplateLexer (character scanning -> LexerTokens)
    -> TemplateParser (LexerTokens -> AST: TemplateDocument/TemplateNodes)
    -> AstTemplateDefinitionParser (AST -> Template definition)
    -> FrontMatterBinder (extracts YAML config from --- markers)
    -> TemplateCompiler (orchestrates the full pipeline)
```

| Stage | Location | Responsibility |
|-------|----------|---------------|
| TemplateLexer | `Compilation/Lexer/` | Character-by-character scanning, produces `LexerToken`s with `FileLocation` tracking |
| TemplateParser | `Compilation/Parsing/` | Converts lexer tokens into an AST (`TemplateDocument` with `TemplateNode`s) |
| AstTemplateDefinitionParser | `Compilation/Definitions/` | Transforms AST into `Template` definition objects |
| FrontMatterBinder | `Compilation/Binders/` | Extracts YAML front matter configuration from between `---` markers |
| TemplateCompiler | `Compilation/TemplateCompiler.cs` | Orchestrates the full compilation pipeline |
| DecoratorRegistry | `Compilation/DecoratorRegistry.cs` | Registers built-in transformers/validators via static factory dictionary, merges custom registrations from `TokenizerOptions` |

Compiled templates are cached internally by pattern string, so repeated calls to `Tokenize(pattern, input)` only compile once.

## Tokenization Engine

Once compiled, templates extract data from input text:

| Component | Location | Responsibility |
|-----------|----------|---------------|
| TokenizationEngine | `Tokenization/TokenizationEngine.cs` | Core processing: matches input against template tokens sequentially |
| HintProcessor | `Tokenization/HintProcessor.cs` | Pre-filters templates by checking if hint strings exist in the input before full tokenization |
| ResultBuilder | `Tokenization/ResultBuilder.cs` | Aggregates matched/unmatched tokens into `TokenizeResult` |
| TokenizationContext | `Tokenization/TokenizationContext.cs` | Maintains state (position, matches so far) during a tokenization pass |

The engine walks the input text looking for each token's **preamble** (the literal text preceding the token). When found, it extracts the value up to the next preamble or terminator, runs validators, applies transformers, and records the match.

## Extension Points

**Transformers** (`Transformers/`) modify extracted values before assignment. Implement `ITokenTransformer`:

```csharp
bool TryTransform(object value, string[] args, out object transformed);
```

**Validators** (`Validators/`) accept or reject extracted values. Implement `ITokenValidator`:

```csharp
bool IsValid(object value, params string[] args);
```

Register custom implementations via `TokenizerOptions`:

```csharp
var options = new TokenizerOptions()
    .WithTransformer<MyTransformer>()
    .WithValidator<MyValidator>();
```

## Async Path

The core compilation and tokenization logic is synchronous. `Tokenizer` and `TemplateMatcher` expose async overloads (`CompileAsync`, `TokenizeAsync`) for stream/reader-based I/O. The async path uses cooperative buffer refills via `TokenEnumerator.FillBufferAsync`, allowing tokenization of inputs larger than memory.

## Entry Points

| Class | Purpose |
|-------|---------|
| `Tokenizer` | Single-template tokenization. Compile a pattern, tokenize input against it. |
| `TemplateMatcher` | Multi-template matching. Register multiple templates, find the best match for an input. |

Both are available via DI using `services.AddTokenizer()`.

## Diagnostics Subsystem

The diagnostics subsystem provides structured tracing at two levels: **compilation** (template construction) and **tokenization** (runtime matching). It is opt-in via `TokenizerOptions.EnableDiagnostics` and uses null-object collectors to avoid any allocation overhead when disabled.

### Collectors

Diagnostics are recorded through two collector interfaces, each with an active implementation and a no-op null singleton:

| Interface | Active Implementation | Null Implementation | Scope |
|-----------|----------------------|--------------------|----|
| `ICompilationDiagnosticCollector` | `CompilationDiagnosticCollector` | `NullCompilationDiagnosticCollector` | Template construction — hints, tokens, decorators |
| `ITokenizationDiagnosticCollector` | `TokenizationDiagnosticCollector` | `NullTokenizationDiagnosticCollector` | Runtime matching — preambles, values, validators |

Both expose `bool IsEnabled`, used as a guard at call sites to skip argument evaluation when diagnostics are off:

```csharp
if (_collector.IsEnabled)
{
    _collector.Record(TokenizationEventType.PreambleMatched, tokenName, ...);
}
```

### Event Model

Events are stored as `DiagnosticEvent<TType>`, a generic container parameterised by event kind:

```
DiagnosticEvent<TType>
├── TType Type           — event kind enum value
├── string? TokenName    — token this event relates to
├── int? TokenId         — unique token ID within template
├── FileLocation? Location — position in input/source
├── string? Value        — value being tested/assigned
├── string? Detail       — human-readable explanation
├── string? DecoratorName — validator/transformer name
└── string[]? DecoratorArgs — decorator parameters
```

Global type aliases simplify usage:

```csharp
global using TokenizationEvent = DiagnosticEvent<TokenizationEventType>;
global using CompilationEvent = DiagnosticEvent<CompilationEventType>;
```

**CompilationEventType** (8 events): `HintAdded`, `TagAdded`, `TokenCreated`, `OptionApplied`, `DecoratorApplied`, `ConcatenationApplied`, `RepeatingTokenLinked`, `CompilationCompleted`.

**TokenizationEventType** (20 events): `TokenizationStarted`, `TokenizationCompleted`, `HintMatched`, `HintMissing`, `PreambleSearchStarted`, `PreambleMatched`, `PreambleNotFound`, `ValueAccumulated`, `TokenAssignmentAttempted`, `ValidatorPassed`, `ValidatorFailed`, `TransformerSucceeded`, `TransformerFailed`, `TokenAssigned`, `TokenAssignmentFailed`, `NewlineTerminatedTokenProcessed`, `BacktrackStarted`, `RepeatingTokenDisabled`, `SingleUseTokenRemoved`, `FrontMatterTokenAssigned`, `FrontMatterTokenFailed`, `TokenMissed`.

### Token-Centric Model

Raw events are transformed into a per-token diagnostic view by `TokenDiagnosticBuilder`. Each token gets a `TokenDiagnostic` that tells its complete story:

```
TokenDiagnostic
├── TokenName, TokenId
├── TokenOutcome         — Matched | Rejected | NeverFound | Blocked
├── TokenAttempt[]       — every consideration during tokenization
│   ├── Location, Value
│   ├── AttemptOutcome   — Assigned | ValidatorRejected | TransformerFailed | Backtracked
│   └── DecoratorName, Reason
├── AssignedValues[]     — matched values (multiple for repeating tokens)
├── AssignedLocations[]  — parallel to AssignedValues
├── BlockedBy            — name of blocker token (Blocked outcome only)
└── DiagnosticIssue[]    — problems with adaptive hints
    ├── Code             — stable issue code (TK001–TK008)
    ├── DiagnosticIssueType
    ├── Description
    ├── Location
    └── Hint             — contextual suggestion from hint generators
```

The token-centric view is built lazily on first access and cached. Raw events remain available via `RawEvents` for low-level tracing.

### Builder Pipeline

`TokenDiagnosticBuilder` transforms raw `TokenizationEvent` lists into `TokenDiagnostic` arrays through four ordered phases:

1. **CollectEvents** — walks all raw events, builds per-token attempt lists, issue lists, assigned value entries, and context indexes for cross-referencing.

2. **ClassifyOutcomes** — creates `TokenDiagnostic` objects. Determines each token's `TokenOutcome` based on whether it was assigned, rejected, or missed. Runs `ValueMismatch` detection (checks whether a matched token's value contains the preamble of a missed token, suggesting greedy capture).

3. **ApplyBlockedAnnotations** — causality analysis for ordered mode only. Finds the first non-optional unmatched token (the "blocker") and reclassifies subsequent `NeverFound` tokens as `Blocked`.

4. **BuildVerdict** — generates a human-readable summary (e.g. "Matched 3 of 5 tokens (2 missed).").

### Issue Codes

Each `DiagnosticIssue` carries a stable code from `IssueCodeMap` for programmatic filtering and documentation linking:

| Code | DiagnosticIssueType | Meaning |
|------|-------------------|---------|
| TK001 | `PreambleNeverFound` | Preamble text not found in input |
| TK002 | `ValidatorRejection` | Validator rejected extracted value |
| TK003 | `TransformerFailure` | Transformer conversion failed |
| TK004 | `ValueMismatch` | Greedy capture consumed another token's preamble |
| TK005 | `RepeatingTokenCutShort` | Repeating token disabled prematurely |
| TK006 | *(reserved)* | Formerly `UnmatchedInputSection`, reserved to prevent reuse |
| TK007 | `HintMissing` | Required hint string not found in input |
| TK008 | `Blocked` | Not searched due to prior required token failure |

### Hint Generators

`IssueFactory` chains `IHintGenerator` implementations to produce contextual suggestions for each issue. All generators are stateless and shared via a static default factory. Each receives the source event and a `BuildContext` containing cross-token indexes (input lines, rejections per token, decorator successes, optional token names).

| Generator | Detects | Example Hint |
|-----------|---------|-------------|
| `PreambleNearMissHintGenerator` | Case/whitespace near-matches in input | "Input contains 'name:' at line 3 (case difference)." |
| `ValidatorValueHintGenerator` | Known validator patterns (IsEmail, IsNumeric, etc.) | "Value 'abc' is not a valid number." |
| `DateFormatHintGenerator` | Failed date transformers, tries 18 common formats | "Value matches format 'dd/MM/yyyy'. Change transformer to use it." |
| `ChainedDecoratorHintGenerator` | Prior decorator succeeded, next failed | "Decorator chain: 'Trim' succeeded → 'ToInt' rejected value 'abc'." |
| `MultipleRejectionHintGenerator` | Token rejected 2+ times | "Token was rejected 3 times. Values tried: 'a', 'b', 'c'." |
| `OptionalTokenHintGenerator` | Optional token not found | "Token 'MiddleName' is optional — no action needed." |
| `RepeatingTokenHintGenerator` | Repeating token disabled early | "Repeating token disabled. Value 'x' failed IsNumeric validation." |
| `ValueMismatchHintGenerator` | Greedy capture swallowed another preamble | "Consider adding '$' to prevent greedy capture." |
| `BlockedTokenHintGenerator` | Token blocked by prior failure | "Fix 'FirstName' first — this token may match once resolved." |

### Renderers

Two renderers produce human-readable diagnostic output:

**AlignmentRenderer** (`AlignmentRenderer.Render`) — structured view of template-to-input mapping with sections for matched tokens, failures, unmatched tokens, and blocked tokens. Includes assigned values with line locations, decorator details, and hints.

**ProcessingOrderRenderer** (`ProcessingOrderRenderer.Render`) — chronological walk-through of every engine decision, showing event type, token name, location, value, decorator, and detail for each step.

Both are called automatically during `FinalizeTokenization` when diagnostics are enabled, with alignment logged at Warning level and processing order at Debug level.

### Integration Points

```
TemplateCompiler.Compile()
├── Creates ICompilationDiagnosticCollector (real or null based on EnableDiagnostics)
├── Passes collector to HintBinder, TagBinder, TokenBinder, DecoratorPipeline
├── Records CompilationCompleted
└── Returns CompilationResult with CompilationDiagnostics

Tokenizer.Tokenize() / RunCoreAsync()
├── Creates ITokenizationDiagnosticCollector (real or null based on EnableDiagnostics)
├── Passes collector to TokenizationSession
│   ├── TokenizationSession records start/complete, delegates to sub-components
│   ├── CandidateProcessor records preamble matching, value accumulation
│   ├── DecoratorPipeline records validator/transformer results
│   └── TokenMatchRouter records assignment decisions
├── ResultBuilder.BuildUnmatchedTokens records TokenMissed events
├── collector.GetResult() → TokenizationDiagnostics (attached to TokenizeResult.Diagnostics)
└── Logs issues, alignment, and processing order via ILogger
```
