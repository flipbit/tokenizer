## Relevant Files

- `specs/prds/template-definition-lexer.md` - Source PRD defining requirements and decisions.
- `src/Tokenizer/Compilation/Lexer/TemplateLexer.cs` - Main streaming lexer implementation (sync/async APIs).
- `src/Tokenizer/Compilation/Lexer/LexerToken.cs` - Token model (Kind, Value, RawText, FileLocation, Start/Length/End).
- `src/Tokenizer/Compilation/Lexer/LexerTokenKind.cs` - Enum of all token kinds per grammar.
- `src/Tokenizer/Compilation/Lexer/LexerException.cs` - Lexer-specific exception deriving from `TokenizerException`.
- `src/Tokenizer/Enumerators/FileLocation.cs` - Existing location tracking used by lexer (reference).
- `tests/Tokenizer.Tests/Compilation/Lexer/TemplateLexerTests.cs` - Unit tests for lexer behavior (all categories from PRD).

### Notes

- Use xUnit for tests (per repo standards). Run with: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj -c Debug`.
- Follow project rules in `/.cursor/rules/csharp-rules.md` and `/.cursor/rules/unit-testing-rules.md`.
- Tests should be organized by behavior (token kinds, errors, inputs, streaming, location, frameworks) using Gherkin-style names.
- All public code must include comprehensive XML documentation with `<summary>`, `<remarks>`, and `<example>` per PRD FR-10.
- Implement with TDD where feasible; write tests first for each behavior, then implement.

## Tasks

- [ ] 1.0 Define lexer public API and core types
  - [x] 1.1 Create namespace `Tokens.Compilation.Lexer` and directory structure
  - [x] 1.2 Add `LexerTokenKind.cs` with all 21 kinds and XML docs (summary/remarks/example)
  - [x] 1.3 Add `LexerToken.cs` (Kind, Value, RawText, FileLocation, Start, Length, End) immutable with XML docs
  - [x] 1.4 Add `LexerException.cs` deriving `TokenizerException`, include location in message; XML docs
  - [x] 1.5 Add `TemplateLexer.cs` with method signatures (sync/async overloads) and XML docs
  - [x] 1.6 Ensure `Tokenizer.csproj` targets `netstandard2.0;net6.0` (no regressions)
  - [ ] 1.7 Compile to confirm types are discoverable by tests

- [ ] 2.0 Implement streaming tokenization core (single TextReader execution path)
  - [x] 2.1 Implement internal TextReader-based scanning loop with lazy `yield return`
  - [x] 2.2 Implement `Peek`/`Read` helpers and small buffer to support lookahead
  - [x] 2.3 Integrate `FileLocation` tracking (increment column, handle new lines, clone on token start)
  - [x] 2.4 Normalize `\n` and `\r\n` to a single `Newline` token
  - [x] 2.5 Emit `Whitespace` tokens (spaces, tabs) without coalescing decisions for parser
  - [x] 2.6 Implement end-of-input emission (`EndOfInput`)
  - [x] 2.7 Add cancellation checks to async path (without changing sync behavior)

- [ ] 3.0 Implement token recognition per grammar (21 token kinds)
  - [x] 3.1 Structural: `{`, `}`, `:`, `=`, `,`, `(`, `)`
  - [x] 3.2 Modifiers: `?`, `*`, `!`, `$`, `#`
  - [x] 3.3 Front matter delimiter `---` (recognize triple-dash as a single token)
  - [x] 3.4 Identifiers: reuse parser’s allowed characters or define consistent rule (alnum, `_`, `.`)
  - [x] 3.5 Text: capture non-identifier sequences that are not other kinds
  - [x] 3.6 Coalesce runs (Identifiers/Text/Whitespace) appropriately to minimize allocations

- [ ] 4.0 Implement quoted strings and escape sequences (`---`, `{{`, `}}`)
  - [x] 4.1 Single-quoted strings: read until next `'`; throw on EOF (strict)
  - [x] 4.2 Double-quoted strings: read until next `"`; throw on EOF (strict)
  - [x] 4.3 Set `Value` to inner content; set `RawText` with quotes
  - [x] 4.4 Recognize `{{` → `EscapedOpenBrace`, `}}` → `EscapedCloseBrace`
  - [x] 4.5 Do not implement escape sequences inside quotes (per PRD)

- [ ] 5.0 Implement synchronous and asynchronous enumeration APIs
  - [x] 5.1 Implement `IEnumerable<LexerToken>` via iterator blocks
  - [x] 5.2 Implement `IAsyncEnumerable<LexerToken>` with async reader path and cancellation
  - [x] 5.3 Ensure async path does not buffer entire input; matches sync token boundaries
  - [x] 5.4 Add XML docs with examples for sync/async usage

- [ ] 6.0 Implement input adapters (string/Stream → TextReader) ensuring one code path
  - [x] 6.1 `Tokenize(string)` → wraps `StringReader`, delegates to TextReader core
  - [x] 6.2 `Tokenize(Stream)` → wraps `StreamReader`, delegates to TextReader core
  - [x] 6.3 Async overloads mirror the above
  - [x] 6.4 XML docs note single execution path design decision

- [ ] 7.0 Add comprehensive XML documentation with remarks, decisions, and examples
  - [x] 7.1 Document design decisions (OQ-1..7) in `<remarks>` where relevant
  - [x] 7.2 Include grammar mapping examples (`{name:ToUpper}` etc.) in `<example>`
  - [x] 7.3 Reference `FileLocation` behavior and newline normalization in docs
  - [x] 7.4 Add exception documentation for strict-mode errors (e.g., unclosed quotes)

- [x] 8.0 Write unit tests (recognition, errors, inputs, streaming/memory, location, multi-target)
  - [x] 8.1 Token kinds: tests for each of the 21 kinds (happy paths)
  - [x] 8.2 Escapes and delimiters: `---`, `{{`, `}}`
  - [x] 8.3 Quoted strings: single/double, raw vs value, EOF without closing quote (throws)
  - [x] 8.4 Inputs: string, TextReader, Stream
  - [x] 8.5 Async enumeration: cancellation support and parity with sync
  - [x] 8.6 Streaming/laziness: enumerate first N tokens only; verify reader not fully consumed
  - [x] 8.7 Location tracking: line/column/paragraph; `\n` vs `\r\n` normalization
  - [x] 8.8 Large input: ensure reasonable performance and no OOM (behavioral not benchmark)

- [ ] 9.0 Optimize .NET 6.0+ path with Span<T>; add .NET Standard 2.0 fallbacks
  - [x] 9.1 Use conditional compilation `#if NET6_0_OR_GREATER` for Span/Memory optimizations
  - [x] 9.2 Buffer reads to `char[]` or `Memory<char>` for chunk processing
  - [x] 9.3 Keep identical observable behavior across targets
  - [x] 9.4 Verify allocations are minimized in hot paths (Identifiers/Text coalescing)

- [ ] 10.0 Enforce code quality (SOLID, TDD, cyclomatic complexity, method size limits)
  - [x] 10.1 Refactor to keep methods ≤50 lines and complexity ≤15 (ideally ≤20 lines/≤10 cc)
  - [x] 10.2 Apply guard clauses and early returns to reduce nesting
  - [x] 10.3 Extract small, focused helpers (read identifier, read quoted, read newline, etc.)
  - [x] 10.4 No magic values; extract constants for character classes and tokens
  - [x] 10.5 Ensure clear naming and separation of concerns

- [ ] 11.0 Add developer examples/README updates showing API usage and grammar mapping
  - [ ] 11.1 Add examples to PRD appendices if needed; or create `docs/lexer.md`
  - [ ] 11.2 Update `README.md` with short usage section (sync/async examples)
  - [ ] 11.3 Cross-link to tests as executable documentation

- [ ] 12.0 Validate on both target frameworks and ensure CI/test stability
  - [ ] 12.1 Build and run tests for `netstandard2.0` target
  - [ ] 12.2 Build and run tests for `net6.0` target
  - [ ] 12.3 Verify no analyzer/linter warnings and documentation warnings
  - [ ] 12.4 Final review against PRD FR-1..FR-10 and Success Metrics


