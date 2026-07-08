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
| DecoratorRegistry | `Compilation/DecoratorRegistry.cs` | Discovers built-in transformers/validators via assembly reflection, merges custom registrations from `TokenizerOptions` |

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
