### Product Requirements Document: Template Definition Parser (Minimal AST) — Phase 1: Front Matter

#### Introduction/Overview

This PRD defines Phase 1 of a new, minimal-AST parser that consumes tokens from `TemplateLexer` and produces a structured syntax tree for template definitions. The scope for this phase focuses on front matter parsing and binding, while representing subsequent template content as minimal token stub nodes (no binding for non-front-matter tokens yet). The goal is to validate the AST approach, ensure parity with existing behavior for front matter, and provide a solid foundation for subsequent phases (preamble and full token parsing).

Key outcomes:
- Parse front matter blocks (`---` delimited) into a typed AST.
- Bind front matter options and directives into `TemplateDefinition` (including `set:` token directive behavior).
- Represent non-front-matter content using minimal stub nodes for later phases.
- Maintain precise location info for rich errors and future diagnostics.

#### Goals

- Establish a minimal AST for template definitions with a clear separation of concerns (syntax vs. binding/semantics).
- Achieve full front matter parity with the current parser (v3) while using `TemplateLexer` tokens.
- Provide fail-fast, precise errors with line/column via token locations.
- Enable differential testing against the current parser using existing samples/tests, specifically for front matter behavior.
- Keep runtime and memory overhead low and streaming-friendly.

#### User Stories

- As a maintainer, I can read a small, focused parser that consumes tokens and builds a minimal AST, making the codebase easier to extend.
- As a developer, I can run tests that verify front matter parsing and binding match the current behavior (including error handling).
- As a future contributor, I can extend the AST and binder incrementally to cover preamble and token parsing in later phases.

#### Functional Requirements

- FR-1: AST Construction (Front Matter)
  - The parser must produce a `TemplateDocument` AST root that optionally contains a single `FrontMatterBlock` followed by a sequence of content nodes (stub for Phase 1).
  - `FrontMatterBlock` must contain a list of entries, each being one of:
    - `FrontMatterEntry` (key/value)
    - `FrontMatterComment`
    - `SetTokenDirective` (special entry representing `set: {tokenName}` semantics)
  - All nodes must carry source location (line, column, start, length) derived from token locations.

- FR-2: Token Consumption and Trivia Handling
  - The parser must consume from `TemplateLexer` via a `TokenReader` abstraction providing: `Peek(k)`, `Consume()`, `TryConsume(kind)`, `Expect(kind)`, `SkipWhitespace()`, `SkipNewlines()` as needed per grammar.
  - Newlines inside front matter terminate entries; outside of front matter they are preserved for later phases.

- FR-3: Front Matter Grammar
  - Recognize a front matter block when the input starts with `---` followed by a newline (`\n` or `\r\n`).
  - Inside front matter, each logical line is one of:
    - Comment line beginning with `#` (emits `FrontMatterComment`).
    - `key: value` option (emits `FrontMatterEntry`). Keys are identifiers or text until `:`; value captures the remainder of the line including quoted strings.
    - `set: {tokenName}` directive (emits `SetTokenDirective`).
  - The block ends at a line that is exactly `---` (followed by newline) per lexer tokenization.

- FR-4: Option Handling (Strict Mode)
  - Unknown front matter options must cause an error (fail fast). The error must include the offending key and location.

- FR-5: Boolean Values
  - Boolean options accept case-insensitive values: `true/false/yes/no/on/off`. Map `yes/on` → `true`, `no/off` → `false`.
  - Errors must be thrown for non-boolean values where a boolean is expected, with location context.

- FR-6: Quoted Values and Whitespace Preservation
  - Single and double quoted values must be supported.
  - Leading/trailing spaces within quotes must be preserved in the parsed value. Trimming rules apply only outside quotes.

- FR-7: Duplicate Options
  - If the same option appears multiple times, the last one wins (later entries override earlier ones during binding).

- FR-8: Error Reporting
  - Fail fast on the first error with a descriptive message and include line/column information from the offending token.
  - Include the expected syntax (e.g., “expected Colon after option name”).

- FR-9: Binding (Front Matter Only in Phase 1)
  - Bind front matter AST into `TemplateDefinition`:
    - Apply recognized options to `TokenizerOptions` and `Template` properties: `TrimLeadingWhitespaceInTokenPreamble`, `TrimTrailingWhiteSpace`, `TrimPreambleBeforeNewLine`, `OutOfOrderTokens`, `TerminateOnNewline`, `IgnoreMissingProperties`, `CaseSensitive`, `Name`, `Hint`/`Hint?`, `Tag`.
    - For `set:` directives, create corresponding token entries consistent with current behavior (v3) as front-matter tokens.
  - Non-front-matter content remains as stub nodes and is not bound in Phase 1.

- FR-10: Compatibility & Parity
  - Behavior must match the current parser for front matter, including errors, accepted values, and trimming rules.
  - Use the existing `TemplateLexer` and `FileLocation` for tokenization and position tracking.

#### Non-Goals (Out of Scope for Phase 1)

- No full token/decorator parsing or binding beyond `set:` directives.
- No AST optimizations or advanced transformations.
- No separate async parsing logic; sync and async share the same core logic and token source style.

#### Design Considerations

- Minimal AST (Phase 1):
  - `TemplateDocument` (root)
  - `FrontMatterBlock` (optional)
  - Entries: `FrontMatterEntry(key, value)`, `FrontMatterComment(text)`, `SetTokenDirective(tokenName)`
  - `ContentNode` (stub) to represent non-front-matter content to be detailed in later phases

- Parser Architecture:
  - Handwritten recursive-descent over tokens using `TokenReader` with small lookahead (1–2 tokens typical).
  - Clear separation: syntax parse → semantic binding. Binder maps supported front matter entries into `TemplateDefinition`.

- Whitespace & Newlines:
  - Leverage lexer’s newline normalization. Within front matter, a newline terminates an entry. Whitespace around keys/colons is permitted per existing behavior but must not alter quoted value contents.

- Error Strategy:
  - Centralized error factory producing consistent messages, attaching the current or lookahead token location.

#### Technical Considerations

- Dependencies: Reuse `TemplateLexer`, `LexerToken`, `LexerTokenKind`, `FileLocation`, and existing exception hierarchy. No external dependencies.
- Streaming: Do not materialize all tokens; parse as they are produced. Keep lookahead minimal.
- Performance: Favor single-pass, low-allocation parsing; AST nodes are minimal POCOs with location.
- Multi-target: Behavior must be identical across .NET Standard 2.0 and .NET 6.0+.

#### Success Metrics

- 100% passing differential tests versus the current parser for front matter scenarios across existing samples in `tests/Tokenizer.Tests/Samples`.
- Unit tests cover: delimiters, option lines (including booleans and quoting), `set:` directive, comments, duplicates (last wins), unknown options (error), and invalid syntax (error locations correct).
- No regressions in error messaging clarity; all errors report line/column and expected constructs.
- Parser remains streaming-friendly; no unbounded buffering.

#### Open Questions

- None for Phase 1 (decisions provided). Any future changes (e.g., additional recognized front matter options) will be captured in Phase 2 PRD.

#### Test Plan

- Differential Tests
  - Parse existing front matter samples using both the current parser and the new AST-based parser; compare resulting `TemplateDefinition` fields affected by front matter (options, name, hints, tags, and `set:` token behavior).

- Unit Tests (Front Matter Productions)
  - Delimiter recognition at start and end of front matter.
  - Option lines:
    - Boolean options: accept `true/false/yes/no/on/off` (case-insensitive); map correctly; error on invalid.
    - String options: `name`, `tag` preserve quoted contents; outside-quote trimming rules match existing behavior.
  - `set:` directive:
    - Accept `{tokenName}` and bind a token consistent with current semantics, including location handling.
  - Comments: lines starting with `#` are captured as `FrontMatterComment` and ignored by the binder.
  - Duplicates: later option overrides earlier occurrences.
  - Unknown options: strict error with location and expected guidance.
  - Quoted values: single and double quotes; preserve spaces within quotes; handle empty quoted values.

- Error Cases
  - Missing closing delimiter `---`.
  - Missing `:` after option key.
  - Empty key before `:` (invalid).
  - `set:` without a valid token stub `{...}`.
  - Non-boolean value for a boolean option.

#### Example Inputs (Reuse Existing Test Samples)

Use the existing examples and samples from `tests/Tokenizer.Tests` and `tests/Tokenizer.Tests/Samples` to instantiate the following patterns (no duplication of sample content here; rely on existing files to drive tests):
- Valid front matter with multiple options, comments, tags, and hints (including `hint` and `hint?`).
- `set:` directives defining one or more front-matter tokens.
- Mixed quoting and whitespace around values.
- Unknown option and invalid boolean value cases for negative tests.

#### Deliverables (Phase 1)

- Minimal AST types and `TokenReader` abstraction.
- Parser producing AST with full front matter coverage.
- Binder applying front matter semantics to `TemplateDefinition` (including `set:` behavior) and leaving non-front-matter as stubs.
- Comprehensive unit tests and differential tests validating parity with the current parser for front matter.

#### File Location and Naming

- PRD filename: `template-definition-parser-ast-front-matter.md` (this document).
- Code placement (when implemented):
  - AST under `src/Tokenizer/Compilation/Ast/` using namespace `Tokens.Compilation.Ast`.
  - Parser under `src/Tokenizer/Compilation/Parsing/` using namespace `Tokens.Compilation.Parsing`.
  - Tests: AST tests under `tests/Tokenizer.Tests/Compilation/Ast/`; parser tests under `tests/Tokenizer.Tests/Compilation/Parsing/`. Reuse existing `Samples`.
  - Optional adapter: expose an experimental `AstTemplateDefinitionParser` in `Tokens.Compilation.Parsing` for behind-flag evaluation.


