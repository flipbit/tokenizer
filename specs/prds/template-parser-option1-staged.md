### Product Requirements Document: Template Parser Refactor — Option 1 (Staged Completion)

#### Introduction/Overview

This PRD defines a staged plan (Option 1) to complete the `TemplateDefinitionParser` refactor in three milestones, each independently testable and shippable. We will progress from verifying and hardening the lexer, to implementing full token grammar in an AST, and finally binding semantics to produce `TemplateDefinition`. Every milestone includes comprehensive unit tests and differential tests against the current parser before proceeding.

#### Goals

- Replace the monolithic parser with a maintainable, testable pipeline without a risky big‑bang.
- Maintain backward compatibility by proving parity at each milestone via differential tests.
- Keep streaming behavior, precise locations, and strict errors.

#### Scope & Milestones

- Milestone 1: Lexer hardening and coverage
  - Verify that `TemplateLexer` recognizes all needed token kinds for token grammar: `{`, `}`, `:`, `=`, `,`, `(`, `)`, modifiers `? * ! $`, identifiers/text, quoted strings, whitespace, newline, escape sequences `{{` `}}`.
  - Add/adjust lexer unit tests to cover token shapes, raw/value, locations, and tricky cases (nested braces in text, escaped braces, quoted content across lines).
  - Non-goal: No AST changes; parser untouched.

- Milestone 2: AST token grammar (syntax only)
  - Extend AST to include token nodes: `TokenNode` (name, modifiers, value, decorators), `DecoratorNode` (name, args), `ArgumentNode` (quoted/unquoted), `TextNode` for preamble.
  - Implement a token parser over `TokenReader` covering: preamble with `{{`/`}}`, `{name[mods][=value][:decorators(args)]}`, comma and parenthesis rules, whitespace/newline rules, accurate locations.
  - Unit tests per grammar production and error scenarios; differential structural comparisons where feasible (e.g., token count, names, presence of value/decorators) using existing Samples.
  - Non-goal: No binding to `TemplateDefinition` yet.

- Milestone 3: Binding semantics to `TemplateDefinition`
  - Map AST to `TemplateDefinition`: token creation; flags (optional/repeating/required/terminate); token values (quoted/unquoted); decorator mapping (including aliases `? * ! $`), arguments.
  - Preserve v3 nuances: repeating multiline preamble behavior; token dependency (`DependsOnId`), newline termination behavior, and ordering.
  - Unit tests verifying property parity; differential tests across Samples for token list and key properties.

#### Functional Requirements

- FR-1: Lexer completeness (M1)
  - The lexer must produce the full token set required by token grammar with correct `Value` and `RawText`, location accuracy, newline normalization, and escape handling.
  - Tests must cover: structural tokens, modifiers, quoted strings, identifiers/text, `{{`/`}}`, whitespace/newlines, edge cases and error cases.

- FR-2: AST token grammar (M2)
  - The parser must consume tokens and produce a syntax tree for: preamble text, token blocks, modifiers, values, decorators/arguments.
  - Minimal lookahead, streaming-friendly; consistent with front matter rules already implemented.
  - Errors are strict and include locations; whitespace rules explicit.

- FR-3: Binding to `TemplateDefinition` (M3)
  - Binder converts AST tokens to `TemplateDefinition` with full parity: token flags, value semantics, decorator mapping and args, ordering, and repeating preamble nuance.
  - Unknown decorators remain as regular decorators; special decorators (`? * ! $`) map to flags, error if given args.

#### Non-Goals

- No big-bang replacement; legacy parser remains until parity proven.
- No performance benchmarking in this phase.

#### Technical Considerations

- Namespaces/locations:
  - AST under `Tokens.Compilation.Ast` (`/src/Tokenizer/Compilation/Ast/`)
  - Parser/binder under `Tokens.Compilation.Parsing` (`/src/Tokenizer/Compilation/Parsing/`)
  - Tests under `tests/Tokenizer.Tests/Compilation/...`
  - Continue using `TemplateLexer` and `FileLocation`.

- Testing strategy:
  - Unit tests per production and error path.
  - Differential tests vs legacy parser on `tests/Tokenizer.Tests/Samples/` after each milestone.
  - Gherkin-style names, AAA structure, strict and readable assertions.

#### Success Metrics

- M1: Lexer tests ≥ 95% coverage for new/adjusted cases; no regressions.
- M2: Parser tests cover all grammar paths and errors; structural parity on Samples.
- M3: Full parity for token lists and key properties on Samples; tests all pass on both TFMs.


