## Relevant Files

- `src/Tokenizer/Compilation/Ast/` - New directory for AST types under the `Tokens.Compilation.Ast` namespace.
- `src/Tokenizer/Compilation/Ast/SyntaxNode.cs` - Base AST node with location metadata.
- `src/Tokenizer/Compilation/Ast/TemplateDocument.cs` - AST root node representing the whole template document.
- `src/Tokenizer/Compilation/Ast/FrontMatterBlock.cs` - Node representing the front matter block.
- `src/Tokenizer/Compilation/Ast/FrontMatterEntry.cs` - Node for `key: value` entries.
- `src/Tokenizer/Compilation/Ast/FrontMatterComment.cs` - Node for front matter comment lines.
- `src/Tokenizer/Compilation/Ast/SetTokenDirective.cs` - Node for `set: {tokenName}` directives.
- `src/Tokenizer/Compilation/Ast/ContentNode.cs` - Stub node for non-front-matter content (Phase 1 placeholder).
- `src/Tokenizer/Compilation/Parsing/TokenReader.cs` - Abstraction over `TemplateLexer` for token consumption, lookahead, and expectations.
- `src/Tokenizer/Compilation/Parsing/FrontMatterParser.cs` - Parser producing the front matter AST from tokens.
- `src/Tokenizer/Compilation/Parsing/TemplateParser.cs` - Coordinator that detects/coordinates blocks and builds `TemplateDocument` (Phase 1: front matter + stubs).
- `src/Tokenizer/Compilation/Parsing/FrontMatterBinder.cs` - Binder mapping front matter AST into `TemplateDefinition` and `TokenizerOptions`.
- `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs` - Existing lexer providing tokens.
- `src/Tokenizer/Enumerators/FileLocation.cs` - Existing location tracking used by tokens and nodes.
- `src/Tokenizer/Exceptions/ParsingException.cs` - Exception type for parse/bind errors; ensure reused consistently.
- `src/Tokenizer/Compilation/Definitions/TemplateDefinition.cs` - Target object for binding.
- `src/Tokenizer/TokenizerOptions.cs` - Target options for front matter boolean/string options.

- `tests/Tokenizer.Tests/Compilation/Ast/` - New directory for AST unit tests.
- `tests/Tokenizer.Tests/Compilation/Ast/SyntaxNodeTests.cs` - Unit tests for base node location capture and immutability.
- `tests/Tokenizer.Tests/Compilation/Ast/FrontMatterAstTests.cs` - Unit tests for `FrontMatterBlock`, `FrontMatterEntry`, `FrontMatterComment`, `SetTokenDirective` node construction.
- `tests/Tokenizer.Tests/Compilation/Parsing/` - Parser and binder tests (unit + differential).
- `tests/Tokenizer.Tests/Compilation/Parsing/TokenReaderTests.cs` - Unit tests for token navigation and skipping helpers.
- `tests/Tokenizer.Tests/Compilation/Parsing/FrontMatterParserTests.cs` - Unit tests for front matter grammar productions and errors.
- `tests/Tokenizer.Tests/Compilation/Parsing/FrontMatterBinderTests.cs` - Unit tests for binding options, booleans, duplicates, and set directive.
- `tests/Tokenizer.Tests/Compilation/Parsing/TemplateParserPhase1Tests.cs` - Unit tests for document coordination (front matter + stubs).
- `tests/Tokenizer.Tests/Compilation/Parsing/DifferentialFrontMatterTests.cs` - Differential tests vs current parser using `Samples`.
- `tests/Tokenizer.Tests/Samples/` - Existing sample inputs used for differential tests.

### Notes

- Use `dotnet test` at the solution root to run all tests; filter with `--filter FullyQualifiedName~Pattern` as needed.
- Project uses xUnit (`[Fact]`, `[Theory]`), keep tests deterministic and small.
- Prefer constructing inputs from existing files in `tests/Tokenizer.Tests/Samples/` for differential tests.
- Ensure new files are included in the appropriate `.csproj` (the solution multi-targets; keep behavior identical across TFMs).

## Tasks

- [x] 1.0 Establish AST foundation (Tokens.Compilation.Ast)
  - [x] 1.1 Create `SyntaxNode` with immutable location (line, column, start, length) referencing `FileLocation` snapshot.
  - [x] 1.1.1 Add constructor overloads accepting token-based and explicit location inputs.
  - [x] 1.1.2 Validate arguments; throw `ArgumentNullException` for null locations.
  - [x] 1.1.3 XML docs with examples showing location usage.
  - [x] 1.2 Add `TemplateDocument` with optional `FrontMatterBlock` and a list of `ContentNode` stubs.
  - [x] 1.2.1 Ensure collection properties are read-only (expose as `IReadOnlyList<T>`).
  - [x] 1.2.2 Provide factory helpers for common document shapes (with/without front matter).
  - [x] 1.3 Add `FrontMatterBlock` holding ordered entries.
  - [x] 1.3.1 Preserve entry order; expose as `IReadOnlyList<SyntaxNode>`.
  - [x] 1.4 Add `FrontMatterEntry` (key, value, location); preserve quoted value whitespace.
  - [x] 1.4.1 Include raw and normalized value fields if needed by binder (document intent).
  - [x] 1.4.2 Enforce non-empty key; validation occurs in parser with clear error.
  - [x] 1.5 Add `FrontMatterComment` (text, location).
  - [x] 1.6 Add `SetTokenDirective` (tokenName, location).
  - [x] 1.7 Add `ContentNode` as a Phase 1 placeholder (no binding).
  - [x] 1.8 XML-doc all public types/members; include remarks tying to PRD decisions.
  - [x] 1.9 Tests: `SyntaxNodeTests`, `FrontMatterAstTests` covering constructors, immutability, and location accuracy.

- [ ] 2.0 TokenReader abstraction over TemplateLexer
  - [x] 2.1 Implement `TokenReader` with `Peek(int)`, `Consume()`, `TryConsume(kind)`, `Expect(kind)`, `SkipWhitespace()`, `SkipNewlines()` and error helpers.
  - [x] 2.2 Keep lookahead minimal and streaming-friendly; do not materialize all tokens.
  - [x] 2.3 Add `CaptureWindow(int before, int after)` for error context in messages.
  - [x] 2.4 Handle EndOfInput gracefully; `Peek` beyond end returns sentinel.
  - [x] 2.5 Unit tests: token navigation, expectation failures include locations, newline/whitespace skipping correctness, EndOfInput behavior.

- [ ] 3.0 Front matter parser (syntax only)
  - [x] 3.1 Detect opening delimiter `---` at start followed by newline; fail if absent when front matter is attempted.
  - [x] 3.2 Parse comment lines beginning with `#` → `FrontMatterComment`.
  - [x] 3.3 Parse option lines `key: value` → `FrontMatterEntry` (support quoted values; trim only outside quotes).
  - [x] 3.4 Parse `set: {tokenName}` → `SetTokenDirective`.
  - [x] 3.5 Detect closing delimiter `---`; error on EOF without closing.
  - [x] 3.6 Preserve accurate locations on all nodes.
  - [x] 3.7 Enforce strict unknown options at parse-time or bind-time per design (document where enforced).
  - [x] 3.8 Unit tests per production: delimiter, comment, entry, set directive, closing, quoted values, invalid/missing colon, empty key.

- [ ] 4.0 Template parser coordinator (Phase 1)
  - [x] 4.1 Implement `TemplateParser` to build `TemplateDocument` by: optional front matter → content stubs.
  - [x] 4.2 Ensure non-front-matter content is preserved as stubs for Phase 2.
  - [x] 4.3 Unit tests: document with and without front matter; mixed lines; locations propagate correctly.

- [ ] 5.0 Front matter binder (semantics)
  - [x] 5.1 Map recognized options to `TokenizerOptions` and `TemplateDefinition` fields: `TrimLeadingWhitespaceInTokenPreamble`, `TrimTrailingWhiteSpace`, `TrimPreambleBeforeNewLine`, `OutOfOrderTokens`, `TerminateOnNewline`, `IgnoreMissingProperties`, `CaseSensitive`, `Name`, `Hint`/`Hint?`, `Tag`.
  - [x] 5.2 Boolean parsing accepts `true/false/yes/no/on/off` (case-insensitive); map `yes/on`→true, `no/off`→false; error otherwise; unit tests table-driven.
  - [x] 5.3 Duplicates: last one wins; implement deterministic override order.
  - [x] 5.4 Implement `set:` directive binding to create front-matter tokens matching v3 semantics.
  - [x] 5.5 Unit tests: each option, quoting behavior (preserve intra-quote spaces), duplicates, set directive mapping (including location transfer if applicable).
  - [x] 5.6 Differential assertions: resulting `TemplateDefinition` fields match current parser for front matter-only inputs.

- [ ] 6.0 Error handling (fail-fast, strict)
  - [x] 6.1 Unknown option → error with key and location.
  - [x] 6.2 Missing `:` after option key → error with expected token in message.
  - [x] 6.3 Missing closing delimiter → error.
  - [x] 6.4 Invalid boolean value → error.
  - [x] 6.5 Invalid `set:` directive (e.g., missing `{name}`) → error.
  - [x] 6.6 Tests assert message clarity and accurate line/column.

- [ ] 7.0 Differential tests vs current parser (front matter parity)
  - [x] 7.1 Use samples in `tests/Tokenizer.Tests/Samples/` to compare resulting `TemplateDefinition` (name, hints, tags, options, set behavior).
  - [x] 7.2 Normalize differences in whitespace/newline normalization as needed to match behavior.
  - [x] 7.3 Ensure no behavior regressions; add targeted samples if gaps are found.

- [ ] 8.0 Integration and project wiring
  - [x] 8.1 Include new files in `Tokenizer.csproj` (multi-targets intact). [Note: SDK-style csproj includes by default]
  - [x] 8.2 Keep existing `TemplateDefinitionParser` untouched; new implementation coexists for Phase 1.
  - [x] 8.3 Optionally expose a factory or behind-flag usage via `ITemplateDefinitionParser` for experiments.
  - [x] 8.4 Ensure namespaces: AST in `Tokens.Compilation.Ast`, parser in `Tokens.Compilation.Parsing`.

- [ ] 9.0 Documentation and developer experience
  - [x] 9.1 XML docs: examples and remarks referencing PRD decisions (front matter scope, strict mode, boolean variants).
  - [x] 9.2 Add README snippet (or internal docs) describing how to run differential tests and interpret failures.
  - [x] 9.3 Reference where front matter strictness is enforced (parser vs binder) and rationale.

- [ ] 10.0 Quality gates
  - [x] 10.1 Ensure zero compiler warnings and consistent style.
  - [x] 10.2 Validate cross-TFM behavior by running tests on all targets.
  - [x] 10.3 Final pass on error message consistency and locations.
  - [x] 10.4 Verify no allocations of entire input; confirm streaming iteration via unit tests.


