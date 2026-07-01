## Relevant Files

- `src/Tokenizer/Compilation/Parsing/TemplateDefinitionParser.cs` - Existing parser to be refactored/orchestrate new components.
- `src/Tokenizer/Compilation/Parsing/TemplateDefinitionEnumerator.cs` - Reference for current enumeration/location tracking.
- `src/Tokenizer/Enumerators/FileLocation.cs` - Location tracking type used by lexer/parser.
- `src/Tokenizer/Compilation/Definitions/*` - Consumed definitions (`TemplateDefinition`, `TokenDefinition`, `DecoratorDefinition`).
- `tests/Tokenizer.Tests/Compilation/Parsing/*` - Existing parser/enumerator tests to remain passing.
- `tests/Tokenizer.Tests/Compilation/Definitions/DecoratorDefinitionTests.cs` - Decorator behavior reference.

- New (proposed) files (namespaces under `Tokens.Compilation.Parsing` unless noted):
  - Lexer (namespace `Tokens.Compilation.Lexer`):
    - `src/Tokenizer/Compilation/Lexer/ITemplateLexer.cs` - Lexer contract.
    - `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs` - Lexer implementation.
    - `src/Tokenizer/Compilation/Lexer/Lexeme.cs` - `Lexeme` struct and `LexemeKind` enum.
    - `src/Tokenizer/Compilation/Lexer/ICharReader.cs` - Character reader contract.
    - `src/Tokenizer/Compilation/Lexer/CharReader.cs` - Character reader implementation.
    - `src/Tokenizer/Compilation/Lexer/ILexemeReader.cs` - Lexeme reader contract.
    - `src/Tokenizer/Compilation/Lexer/LexemeReader.cs` - Lexeme reader implementation.
  - `src/Tokenizer/Compilation/Parsing/ITemplateParser.cs` - Parser contract.
  - `src/Tokenizer/Compilation/Parsing/TemplateParser.cs` - Parser implementation (replaces internal logic of `TemplateDefinitionParser`).
  - `src/Tokenizer/Compilation/Parsing/IFrontMatterParser.cs` - Front matter parser contract.
  - `src/Tokenizer/Compilation/Parsing/FrontMatterParser.cs` - Front matter parser implementation.
  - Decorators (namespace `Tokens.Compilation.Decorators`):
    - `src/Tokenizer/Compilation/Decorators/IDecoratorParser.cs` - Decorator parser contract.
    - `src/Tokenizer/Compilation/Decorators/DecoratorParser.cs` - Decorator parser implementation.
    - `src/Tokenizer/Compilation/Decorators/IDecoratorRegistry.cs` - Decorator registry contract.
    - `src/Tokenizer/Compilation/Decorators/DecoratorRegistry.cs` - Decorator registry implementation.
  - Assembly (namespace `Tokens.Compilation.Assembly`):
    - `src/Tokenizer/Compilation/Assembly/ITokenAssembler.cs` - Token completion/assembly contract.
    - `src/Tokenizer/Compilation/Assembly/TokenAssembler.cs` - Token completion/assembly implementation.
    - `src/Tokenizer/Compilation/Assembly/IPreambleRepeater.cs` - Repeating preamble contract.
    - `src/Tokenizer/Compilation/Assembly/PreambleRepeater.cs` - Repeating preamble implementation.
  - Validation (namespace `Tokens.Compilation.Validation`):
    - `src/Tokenizer/Compilation/Validation/INameCharPolicy.cs` - Name character policy contract.
    - `src/Tokenizer/Compilation/Validation/NameCharPolicy.cs` - Name character policy implementation.
  - Errors (namespace `Tokens.Compilation.Errors`):
    - `src/Tokenizer/Compilation/Errors/IErrorReporter.cs` - Error reporting contract.
    - `src/Tokenizer/Compilation/Errors/ErrorReporter.cs` - Error reporting implementation.

### Notes

- Follow coding standards in `.cursor/rules/csharp-rules.md` (naming, comparisons, XML docs, conditional compilation).
- Follow testing standards in `.cursor/rules/unit-testing-rules.md` (xUnit, Arrange/Act/Assert, focused assertions).
- Phase 1: high-level tasks only. Respond "Go" to generate detailed sub-tasks.

## Tasks

- [ ] 1. Define lexer contracts and core types (Lexeme, LexemeKind)
  - [x] 1.1 Create `ITemplateLexer` interface (Tokens.Compilation.Lexer)
  - [x] 1.2 Define `Lexeme` struct and `LexemeKind` enum with XML docs
  - [x] 1.3 Add `ILexemeReader` interface with `Peek/Next/Location`
  - [x] 1.4 Add `ICharReader` interface with `Peek/Next/Location`
  - [x] 1.5 Ensure namespaces match Tokens.Compilation.Lexer

- [ ] 2. Implement newline normalization and `FileLocation` tracking in lexer
  - [x] 2.1 Implement `CharReader` (CR/LF/CRLF -> \n) with location updates
  - [x] 2.2 Add comprehensive XML docs and remarks on normalization
  - [x] 2.3 Unit tests: newline variants normalize to \n; location correctness

- [x] 3. Create `CharReader` and `LexemeReader` abstractions
  - [x] 3.1 Implement `CharReader` (Tokens.Compilation.Lexer)
  - [x] 3.2 Implement `LexemeReader` over `ITemplateLexer` output
  - [x] 3.3 XML docs for both readers

- [ ] 4. Implement the `TemplateLexer` (char-based, allocation-light)
  - [x] 4.1 Tokenize braces, sigils, separators, quotes, identifiers, text
  - [x] 4.2 Emit `Whitespace` and `Newline` kinds; consider trimming policy later
  - [x] 4.3 Track `FileLocation` per lexeme; add EOF token
  - [x] 4.4 Unit tests for tokenization coverage and edge cases

- [ ] 5. Define parser contracts and orchestrator (`ITemplateParser`)
  - [x] 5.1 Interface in Tokens.Compilation.Parsing with `Parse(IEnumerable<Lexeme>, TokenizerOptions)`
  - [x] 5.2 XML docs describing responsibilities and exceptions

- [ ] 6. Implement `FrontMatterParser` for fenced front matter and `set:` tokens
  - [x] 6.1 Parse options: `name`, `tag`, `hint`, `hint?`, booleans (trim options)
  - [x] 6.2 Validate values and convert booleans using safe parsing
  - [x] 6.3 Map to `TemplateDefinition.Options`, `Name`, `Hints`, `Tags`
  - [x] 6.4 Unit tests for all options and malformed input
  - [x] 6.5 XML docs (summary, params, exceptions)

- [ ] 7. Implement `DecoratorParser` and `DecoratorRegistry` with aliases and validations
  - [x] 7.1 Parse name, `!` (not), parentheses and comma-separated args
  - [x] 7.2 Registry: aliases (`eol`/`$`, `optional`/`?`, `repeating`/`*`, `required`/`!`, `once`)
  - [x] 7.3 Validate arg counts for built-ins; unknown decorators preserved
  - [x] 7.4 Unit tests covering aliases, args, errors
  - [x] 7.5 XML docs

- [ ] 8. Implement `TokenAssembler` and `PreambleRepeater` (including fixed multiline preamble logic)
  - [x] 8.1 Centralize completion of name/value/decorators, assign ids/dependencies
  - [x] 8.2 Implement corrected trailing preamble logic and repeat token creation
  - [x] 8.3 Unit tests for token completion and repeat generation
  - [x] 8.4 XML docs

- [ ] 9. Implement `NameCharPolicy` with fast predicate checks
  - [x] 9.1 `char.IsLetterOrDigit(c) || c == '_' || c == '.'`
  - [x] 9.2 Unit tests: allowed/disallowed cases
  - [x] 9.3 XML docs

- [ ] 10. Implement `ErrorReporter` consolidating `ParsingException` creation with context
  - [x] 10.1 Methods for unexpected char/lexeme with context and location
  - [ ] 10.2 Replace ad-hoc throws in new components with reporter
  - [x] 10.3 Unit tests for message/position correctness
  - [x] 10.4 XML docs

- [ ] 11. Refactor `TemplateDefinitionParser` to orchestrate new components
  - [x] 11.1 Replace internal state machine with calls to lexer/parser stack
  - [x] 11.2 Preserve public API and behavior
  - [x] 11.3 Ensure options (`TokenizerOptions`) still applied equivalently

- [ ] 12. Standardize exception usage to `ParsingException` in parsing flow
  - [x] 12.1 Replace mixed `TokenizerException` in parse paths with `ParsingException`
  - [x] 12.2 Ensure all exceptions carry `FileLocation`

- [ ] 13. Optimize hot paths (char comparisons, Ordinal/IgnoreCase, minimal trimming)
  - [x] 13.1 Replace per-char string allocations with `char` ops
  - [x] 13.2 Use `StringComparison.Ordinal[IgnoreCase]` for keywords/aliases
  - [x] 13.3 Avoid repeated `ToLowerInvariant()`; normalize at boundaries

- [ ] 14. Add comprehensive XML documentation across new interfaces, classes, and methods
  - [x] 14.1 Summaries for responsibilities/roles
  - [x] 14.2 Params/returns/exceptions; remarks for newline normalization
  - [x] 14.3 Cross-reference related types (lexer/parser/registry)

- [ ] 15. Create unit tests for lexer (tokens, newlines, locations)
  - [x] 15.1 Cover CR, LF, CRLF normalization
  - [x] 15.2 Cover identifiers, text, sigils, separators, quotes, EOF
  - [x] 15.3 Verify accurate `FileLocation`

- [ ] 16. Create unit tests for parser (front matter, names, values, decorators, run-off)
  - [x] 16.1 Front matter option matrix and `set:` tokens
  - [x] 16.2 Quoted/unquoted values; embedded quotes; run-off enforcement
  - [x] 16.3 Decorator args/aliases; unknown decorators preserved

- [ ] 17. Create unit tests for repeating multiline preamble behavior
  - [x] 17.1 Indentation preservation; correct repeat linkage
  - [x] 17.2 Edge cases with whitespace-only trailing segments

- [ ] 18. Ensure all existing tests pass unchanged (backward compatibility)
  - [ ] 18.1 Run full test suite; fix regressions without altering public behavior

- [ ] 19. Optional: add performance/benchmark-style tests for large templates
  - [x] 19.1 Measure allocations/time on .NET 6
  - [x] 19.2 Document baseline vs. new results

- [ ] 20. Update developer docs/README to describe the architecture and extension points
  - [x] 20.1 Document component responsibilities and data flow
  - [x] 20.2 Include examples of registering decorators


