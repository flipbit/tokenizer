## Introduction / Overview

This PRD defines a refactor of the `TemplateDefinitionParser` responsible for constructing a `TemplateDefinition` from a template string. The goal is to reduce complexity, improve correctness and performance, and align the implementation with project-wide standards while maintaining full backward compatibility.

The refactor introduces a clear separation of concerns (lexer vs. parser), normalizes newline handling, standardizes error reporting, and optimizes hot paths to reduce allocations. It also codifies an extensibility point for decorators. All changes must conform to the project’s C# coding standards and testing guidelines.

References:
- Project C# standards: see `@csharp-rules.md`
- Unit testing rules: see `@unit-testing-rules.md`

## Goals

1. Split scanning and parsing into a two-stage pipeline (lexer + parser) for clarity and maintainability.
2. Normalize newline handling and remove scattered CR/LF special-casing.
3. Standardize error handling around a single parse exception type with location context.
4. Improve performance by eliminating string-per-char allocations and unnecessary `string` operations.
5. Clarify and harden edge-case logic (e.g., name characters, run-off states, repeating multiline preambles).
6. Provide a simple, testable extension model for decorators.
7. Maintain full backward compatibility with existing templates and tests.

## User Stories

- As a maintainer, I want the parser logic separated into small, testable units so that I can safely evolve the grammar.
- As a user of the library, I want consistent and actionable error messages (with locations) so I can quickly fix patterns.
- As a performance-focused engineer, I want fewer allocations and faster parsing so large templates process efficiently.
- As an integrator, I want to add custom decorators without modifying the parser core.

## Functional Requirements

1. Lexer (namespace `Tokens.Compilation.Lexer`)
   1.1. Convert template input into a stream of typed tokens: braces, identifiers, sigils (`?`, `*`, `!`, `$`), `:`, `,`, `=`, quotes, whitespace, and newline.
   1.2. Operate on `char`/spans to avoid per-character string allocations.
   1.3. Normalize newlines (CR, LF, CRLF) to `\n` before token emission.
   1.4. Track `FileLocation` (line/column) for each emitted token.

2. Parser
   2.1. Consume lexer tokens to build `TemplateDefinition`, `TokenDefinition` objects, and decorators.
   2.2. Implement front matter parsing (`---` fenced) for options (`name`, `hint`, `hint?`, `tag`, booleans) and `set:` tokens.
   2.3. Implement token name/value/decorator grammar including quoted values/args and run-off states.
   2.4. Centralize “end-of-token” and “end-of-decorator” completion in helper(s) to avoid duplication across states.
   2.5. Validate token-name characters using predicate: letter/digit/`_`/`.`; reject others with consistent errors.
   2.6. Preserve current semantics for optional/repeating/required, EOL termination, and `ConsiderOnce`.
   2.7. Generate repeating tokens for multiline preambles exactly as before, with corrected trailing preamble logic (see 5.2).

3. Error Handling
   3.1. Use a single exception type for parse errors (e.g., `ParsingException`).
   3.2. All errors include unexpected token/character info and `FileLocation`.
   3.3. Replace existing mixed `TokenizerException` throws within parse flow with `ParsingException` where appropriate.

4. Decorator Handling
   4.1. Implement a small registry (case-insensitive) mapping canonical decorator names (and aliases) to actions.
   4.2. Built-in names/aliases: `eol`/`$`, `optional`/`?`, `repeating`/`*`, `required`/`!`, `once`.
   4.3. Unknown decorators are preserved as `DecoratorDefinition` for downstream processing.

5. Correctness Improvements
   5.1. Replace literal `ValidTokenNameCharacters` with a predicate: `char.IsLetterOrDigit(c) || c == '_' || c == '.'`.
   5.2. Fix `GetRepeatingMultilinePreamble` logic so the trailing fragment after the last newline (indent/whitespace) is correctly reused; add tests for indentation preservation.
   5.3. Normalize newline handling to avoid duplicated CR/LF checks across states.
   5.4. Ensure run-off states only accept valid trailing input and error consistently otherwise.

6. Performance
   6.1. Switch to `char` comparisons throughout; eliminate one-character string allocations.
   6.2. Prefer `Ordinal`/`OrdinalIgnoreCase` comparisons for keywords and decorators.
   6.3. Minimize `ToLowerInvariant()`/`Trim()` in hot paths; normalize at boundaries or use case-insensitive comparisons.
   6.4. Use `StringBuilder` only where necessary; consider span slicing for content capture on .NET 6+ with conditional compilation.

7. Compatibility & API
   7.1. Public surface area remains unchanged; behavior remains backward compatible.
   7.2. Internal types can be marked `sealed` where appropriate; helper methods made `private static`.

## Component Split and Contracts

Introduce focused components with clear responsibilities and interfaces. Names are suggestions; internal visibility is acceptable where appropriate.

1. Lexer
   - Purpose: Convert input characters to typed lexemes and track positions; normalize newlines.
   - Interface:
     - `ITemplateLexer.Lex(ReadOnlySpan<char> input) : IEnumerable<Lexeme>`
   - Types:
     - `readonly struct Lexeme { LexemeKind Kind; ReadOnlyMemory<char> Value; FileLocation Start; int Length; }`
     - `enum LexemeKind { LeftBrace, RightBrace, Colon, Comma, Equals, Question, Asterisk, Bang, Dollar, QuoteSingle, QuoteDouble, Identifier, Text, Whitespace, Newline, Eof }`

2. CharReader / LexemeReader (namespace `Tokens.Compilation.Lexer`)
   - Purpose: Provide cursor semantics with `Peek`, `Next`, and `Location` for chars and lexemes.
   - Interfaces:
     - `ICharReader.Peek(int lookahead = 0) : char`
     - `ICharReader.Next() : char`
     - `ICharReader.Location : FileLocation`
     - `ILexemeReader.Peek(int lookahead = 0) : Lexeme`
     - `ILexemeReader.Next() : Lexeme`
     - `ILexemeReader.Location : FileLocation`

3. Parser
   - Purpose: Consume lexemes to construct `TemplateDefinition`.
   - Interface:
     - `ITemplateParser.Parse(IEnumerable<Lexeme> lexemes, TokenizerOptions options) : TemplateDefinition`

4. FrontMatterParser
   - Purpose: Parse fenced front matter and map options and `set:` tokens to `TemplateDefinition`.
   - Interface:
     - `IFrontMatterParser.Parse(ILexemeReader reader, TemplateDefinition template, TokenizerOptions options) : void`

5. DecoratorParser and DecoratorRegistry (namespace `Tokens.Compilation.Decorators`)
   - Purpose: Parse decorator names/arguments; apply built-in decorators; preserve unknown.
   - Interfaces:
     - `IDecoratorParser.Parse(ILexemeReader reader) : DecoratorDefinition`
     - `IDecoratorRegistry.Apply(TokenDefinition token, DecoratorDefinition decorator, FileLocation location) : void`
   - Notes: Registry is case-insensitive; supports aliases (`eol`/`$`, `optional`/`?`, `repeating`/`*`, `required`/`!`, `once`).

6. TokenAssembler (namespace `Tokens.Compilation.Assembly`)
   - Purpose: Centralize token completion (end-of-name/value/decorators), assign ids, dependencies, and finalize content.
   - Interface:
     - `ITokenAssembler.CompleteToken(TemplateDefinition template, TokenDefinition token, bool inFrontMatter) : void`

7. PreambleRepeater (namespace `Tokens.Compilation.Assembly`)
   - Purpose: Compute repeating multiline preamble and generate the repeat token when applicable.
   - Interface:
     - `IPreambleRepeater.TryCreateRepeat(TemplateDefinition template, TokenDefinition token) : bool`

8. NameCharPolicy (namespace `Tokens.Compilation.Validation`)
   - Purpose: Validate token-name characters with a fast predicate.
   - Interface:
     - `INameCharPolicy.IsValid(char c) : bool`

9. ErrorReporter (namespace `Tokens.Compilation.Errors`)
   - Purpose: Produce consistent `ParsingException` with context and `FileLocation`.
   - Interface:
     - `IErrorReporter.Unexpected(char c, string context, FileLocation location) : ParsingException`
     - `IErrorReporter.Unexpected(Lexeme lexeme, string context) : ParsingException`

## Non-Goals (Out of Scope)

- Introducing asynchronous parsing.
- Changing the external parser API or the template language semantics.
- Adding new built-in decorators beyond those already supported.

## Design Considerations

- Follow project C# standards in `@csharp-rules.md`:
  - Target `netstandard2.0` and `net6.0` with conditional compilation.
  - Prefer `Ordinal` comparisons for parser keywords and decorator names.
  - Keep methods small, use guard clauses, avoid deep nesting.
  - Use clear naming; avoid abbreviations; prefer immutable local state where possible.

- Conditional compilation examples:
  - For .NET 6+: use `ReadOnlySpan<char>` in lexer hot paths when beneficial.
  - For .NET Standard 2.0: provide equivalent string/char-based implementations.

- Extensibility:
  - Decorator registry implemented as a case-insensitive dictionary of handlers; aliases map to the same handler.

## Technical Considerations

- Newline normalization occurs at the lexer boundary; the parser only sees `\n` for line breaks.
- Location tracking must account for normalization so reported positions match source.
- Replace ad-hoc `string` comparisons with `char`-based switches and constants for sigils/terminators.
- Ensure all parse errors use `ParsingException` and include `FileLocation`.

## Documentation Requirements

- Add comprehensive XML documentation to all new public and internal interfaces/classes introduced by this refactor, consistent with `@csharp-rules.md`:
  - Each interface and class must include a `<summary>` describing its responsibility and role in the pipeline.
  - Methods must document parameters, return values, and any exceptions (e.g., `ParsingException`).
  - Data structures (`Lexeme`, `LexemeKind`) must document semantics and usage, including normalization assumptions (e.g., newline normalization to `\n`).
  - Where behavior depends on options (e.g., `TokenizerOptions`), include remarks explaining impacts on parsing outcomes.
  - Maintainers should be able to understand component contracts and extension points (e.g., decorator registry) solely from XML docs.

## Success Metrics

1. All existing tests pass without modification (backward compatibility guaranteed).
2. New unit tests (see below) cover edge cases and reach >90% coverage for new lexer and parser components.
3. Allocation count and elapsed time for parsing representative large templates improve measurably (target: ≥20% fewer allocations, ≥10% throughput improvement on .NET 6 builds).
4. Error messages include token/character context and accurate locations for all failure modes covered by tests.

## Testing Plan (Unit & Integration)

Follow `@unit-testing-rules.md` (xUnit, Arrange/Act/Assert, focused assertions) and keep tests in `Tokenizer.Tests`:

- Lexer Tests
  - Normalize CR/LF/CRLF to `\n`.
  - Emit correct token kinds for braces, sigils, identifiers, quotes, separators, whitespace.
  - Accurate `FileLocation` tracking across lines and columns.

- Parser Tests
  - Front matter: options (`name`, `tag`, `hint`, `hint?`, all boolean options) and `set:` tokens.
  - Token names: allowed vs. disallowed characters; consistent error messages with locations.
  - Values: unquoted, single-quoted, double-quoted; embedded quotes; run-off handling.
  - Decorators: aliases, argument parsing, `!` handling; unknown decorators preserved.
  - Repeating multiline preamble: indentation preservation and token generation correctness.
  - Error consistency: all invalid transitions throw `ParsingException` with location.

- Performance/Regression Tests (as feasible)
  - Parse a large template and assert time/allocations within thresholds (benchmark-style tests optional, excluded from CI if flaky).

## Acceptance Criteria

- Code adheres to `@csharp-rules.md` (names, structure, comparisons, conditional compilation) and passes linters.
- Tests adhere to `@unit-testing-rules.md` and cover stated scenarios.
- No breaking changes to public APIs or behavior; all existing templates continue to parse identically.
- Documented decorator registry and newline normalization in code comments and README notes where applicable.

## Open Questions

1. Should decorator handling errors (e.g., unexpected args for aliases like `?`) be warnings or hard errors? (Current behavior: hard errors.)
2. Should the parser surface a diagnostics collection (warnings) in addition to throwing on errors?
3. Any need to expose a public hook to register custom decorator handlers at runtime, or keep registry internal for now?


