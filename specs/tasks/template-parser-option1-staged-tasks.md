## Relevant Files

- `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs` - Lexer under test (M1 hardening).
- `src/Tokenizer/Compilation/Lexer/LexerTokenKind.cs` / `LexerToken.cs` - Token kinds and structure.
- `src/Tokenizer/Compilation/Parsing/TokenReader.cs` - Streaming token reader for parser (M2+).
- `src/Tokenizer/Compilation/Ast/` - AST nodes; extend with token/decorator/argument/preamble nodes (M2).
- `src/Tokenizer/Compilation/Parsing/TemplateParser.cs` - Token grammar parser producing `TemplateDocument` (M2).
- `src/Tokenizer/Compilation/Parsing/AstTemplateDefinitionParser.cs` - Staged integration point to feed binder (M2/M3).
- `src/Tokenizer/Compilation/Parsing/FrontMatterBinder.cs` - Reference binder patterns for mapping AST to definitions (M3).
- `tests/Tokenizer.Tests/Compilation/Lexer/TemplateLexerTests.cs` - Expand tests for completeness (M1).
- `tests/Tokenizer.Tests/Compilation/Parsing/TokenReaderTests.cs` - Tests for `TokenReader` streaming/locations.
- `tests/Tokenizer.Tests/Compilation/Parsing/TemplateParserPhase1Tests.cs` - Unit tests for token grammar (syntax only) (M2).
- `tests/Tokenizer.Tests/Compilation/Parsing/TemplateBinderTests.cs` - New tests for AST→`TemplateDefinition` binding (M3).
- `tests/Tokenizer.Tests/Compilation/Parsing/DifferentialFrontMatterTests.cs` - Example of differential testing style; mirror for tokens/binding.
- `tests/Tokenizer.Tests/Samples/Patterns/` and `tests/Tokenizer.Tests/Samples/Data/` - Inputs for differential testing.

### Notes

- Use `dotnet test` and run both TFMs; keep tests deterministic and small.
- Follow the unit testing rules (Gherkin names, AAA structure, focused assertions).

## Tasks

- [ ] M1.0 Lexer hardening and coverage
  - [x] M1.1 Audit token kinds needed for token grammar in `LexerTokenKind.cs` and actual emissions in `TemplateLexer.cs`; list any gaps (`,`, `(`, `)`, `:`, `=`, `{`, `}`, modifiers `? * ! $`, identifiers/text, quoted strings, whitespace, newline, escapes `{{` `}}`).
    - Findings: Structural and modifier tokens present; escaped braces, whitespace/newlines, identifiers present. Quoted string escape handling (e.g., `\"`, `\\`) is not implemented and will terminate at the next quote; treat as a gap for M1. Quoting-related error handling should be in lexer; context errors like "unexpected `}` outside a token" belong to parser (address in M2 error tests).
  - [x] M1.2 Structural token tests in `tests/Tokenizer.Tests/Compilation/Lexer/TemplateLexerTests.cs`: assert kinds for `{`, `}`, `:`, `=`, `,`, `(`, `)` with surrounding text.
  - [x] M1.3 Modifiers tests: inputs like `{name?}`, `{name*}`, `{name!}`, `{name$}` produce distinct modifier tokens and correct positions.
  - [x] M1.4 Identifiers vs text: ensure names are tokenized as identifiers; plain content outside braces remains text tokens without splitting on punctuation.
  - [x] M1.5 Quoted strings (with spaces/escapes): values like `"first last"`, escaped quotes `\"`, and escaped backslash `\\` retain `Value` and `RawText` correctly.
  - [x] M1.6 Escaped braces in preamble: `{{` and `}}` become single literal `{` and `}` in text stream (not structure tokens); add lexer cases.
  - [x] M1.7 Whitespace/newlines: verify normalization and locations across `\n` and `\r\n`; assert token `Start`/`End` line/column using `FileLocation`.
  - [x] M1.8 Error cases: unclosed quote, invalid escape sequence, unexpected `}` outside a token; assert thrown exception type/message and location.
  - [x] M1.9 Differential sanity: for each sample in `tests/Tokenizer.Tests/Samples/`, pick a few token-related lines and assert produced token kinds/sequences are sensible (no crashes/regressions).
  - [x] M1.10 Run tests: execute `dotnet test` from repo root; ensure all tests compile and pass.

- [ ] M2.0 AST token grammar (syntax only)
  - [x] M2.1 Add AST node types under `src/Tokenizer/Compilation/Ast/` with locations: `TokenNode` (name, modifiers, optional `ValueNode`, optional `DecoratorNode[]`), `TokenName`, `ModifierSet`, `ValueNode` (quoted flag, text), `DecoratorNode` (name, `ArgumentNode[]`), `ArgumentNode` (quoted flag, text), `TextNode` (preamble chunks), and root `TemplateDocument`.
  - [x] M2.2 Implement `TemplateParser` over `TokenReader`: parse preamble text with `{{`/`}}` rules; parse `{name[mods][=value][:decorators(args)]}`; handle commas within decorator arg lists and `()` nesting depth 1; ignore insignificant whitespace per grammar.
  - [x] M2.3 Parser error handling: unexpected chars in token, missing `}`, malformed decorator args (unbalanced `()` or stray `,`), misplaced modifiers (after value/after decorators).
  - [x] M2.4 Unit tests in `tests/Tokenizer.Tests/Compilation/Parsing/TemplateParserPhase1Tests.cs`:
  - [x] M2.4.1 Preamble only: `Hello world` → one `TextNode`, zero `TokenNode`s.
  - [x] M2.4.2 Single token name: `Hello {name}` → one `TokenNode` with `TokenName` = `name`, no value, no decorators, no modifiers.
  - [x] M2.4.3 Modifiers: `{name?*}` populates `ModifierSet` accordingly; order-insensitive acceptance, order-preserving AST.
  - [x] M2.4.4 Value unquoted and quoted: `{id=123}`, `{user="Jane Doe"}` with `ValueNode.quoted` true/false and correct text.
  - [x] M2.4.5 Decorator without args: `{name:trim}` and with args: `{name:regex("[A-Z]+", 3)}` parse into `DecoratorNode` with `ArgumentNode[]` and quoted flags.
  - [x] M2.4.6 Multiple decorators: `{name:trim:lower()}` produces two decorators in order.
  - [x] M2.4.7 Escaped braces in preamble: `Hello {{name}}` becomes `TextNode` containing `{name}` literal; no `TokenNode`.
  - [x] M2.4.8 Newlines and locations: multi-line input with tokens across lines has accurate node `Start`/`End` locations.
  - [x] M2.5 Parser error tests in `TemplateParserPhase1Tests.cs`:
  - [x] M2.5.1 Missing closing `}` reports precise error location.
  - [x] M2.5.2 Malformed decorator args: `{name:regex((}` and `{name:regex(, )}` raise errors.
  - [x] M2.5.3 Misplaced modifiers: `{name=value?}` and `{name:trim?}` raise errors.
  - [x] M2.6 Differential structural checks on samples (no binding): for each sample file, parse to AST and assert counts and high-level shapes (number of `TokenNode`s, names, presence of value/decorators) match expectations.
  - [x] M2.7 Run tests: execute `dotnet test`; ensure all tests compile and pass.

- [ ] M3.0 Binding semantics to `TemplateDefinition`
  - [x] M3.1 Implement binder to map AST → `TemplateDefinition` (new binder under `src/Tokenizer/Compilation/Parsing/`, e.g., `TemplateBinder.cs`): map token creation; flags (optional/repeating/required/terminate) from modifiers/aliases `? * ! $`; value semantics (quoted/unquoted); decorators and arguments.
  - [x] M3.2 Preserve v3 nuances: repeating multiline preamble handling, token dependency/order (`DependsOnId` or equivalent), newline termination behavior.
  - [x] M3.3 Unit tests in `tests/Tokenizer.Tests/Compilation/Parsing/TemplateBinderTests.cs` verifying field-by-field parity for representative inputs (with/without value, with multiple decorators, with modifiers, multi-line preamble cases).
  - [x] M3.4 Differential parity across `tests/Tokenizer.Tests/Samples/`: parse with legacy parser and new pipeline; compare token list and key properties; add a flag or switch to gate cutover.
  - [x] M3.5 Run tests: execute `dotnet test`; ensure all tests compile and pass on both legacy and new paths.


