# Product Requirements Document: Template Definition Lexer

## Introduction/Overview

This document outlines the requirements for creating a **Template Definition Lexer** for the Tokenizer library. The lexer will perform lexical analysis on template definition input strings, breaking them down into a stream of tokens that represent the grammatical elements of the template definition language.

### Problem Statement
Currently, the `TemplateDefinitionParser` combines lexical analysis (character-by-character scanning) and parsing (structural analysis) into a single monolithic class of 963 lines. This makes the code difficult to understand, test, and maintain. By separating lexical analysis into a dedicated lexer, we can:

- Improve code maintainability and testability
- Create a clear separation of concerns
- Establish foundation for future AST (Abstract Syntax Tree) generation
- Enable better error reporting with precise token location information

### Goal
Create a standalone, memory-efficient, and thoroughly tested lexer that can tokenize template definition input following the existing grammar used by `TemplateDefinitionParser`. The lexer will serve as a foundation for future parser improvements and AST generation, though it will not be integrated with the parser in this initial phase.

## Goals

1. **Extract a complete lexer** from the existing `TemplateDefinitionParser` that can identify all token types in the template definition grammar
2. **Provide synchronous and asynchronous APIs** for flexible integration patterns
3. **Ensure memory efficiency** through streaming input processing and modern .NET features (Span<T>, Memory<T>)
4. **Achieve comprehensive test coverage** across all token types, error scenarios, and performance characteristics
5. **Follow existing coding standards** as defined in the project's C# coding rules
6. **Maintain multi-target compatibility** for .NET Standard 2.0 and .NET 6.0+
7. **Create a standalone, testable component** that follows the existing grammar and can be extended for future AST generation
8. **Provide comprehensive, educational XML documentation** targeting both human developers and AI assistants with decision rationale, background information, and practical examples
9. **Demonstrate expert-level code quality** following TDD and SOLID principles as expected from a staff-level C# engineer

## Key Design Decisions

The following design decisions have been finalized for this implementation:

1. **Token Context**: Lexer does NOT distinguish between contexts (e.g., `:` is always `Colon`, regardless of whether it's in front matter or a token decorator). Context interpretation is the parser's responsibility.

2. **Whitespace Handling**: Emit ALL whitespace and newline tokens. The lexer is grammar-agnostic and provides complete token stream.

3. **Quoted Strings**: Emit as single `QuotedString` token with value excluding the quotes. The `RawText` property preserves the original quoted form.

4. **Identifiers vs Text**: Lexer emits generic `Identifier` or `Text` tokens. Semantic meaning (token name, decorator name, etc.) is determined by the parser.

5. **Line Ending Normalization**: Both `\n` and `\r\n` emit a single `Newline` token. Line endings are normalized for consistency.

6. **Token Metadata**: Current design (Kind, Value, Location, Start/End, RawText) is sufficient for future AST integration. Additional properties can be added later if needed.

7. **Performance Benchmarks**: Formal performance benchmarks are NOT included in this phase. Focus is on correctness and comprehensive unit testing. Performance testing will be added in a future iteration.

## User Stories

### US-1: Developer Using Lexer Directly
**As a** developer working on the Tokenizer library  
**I want to** use the lexer to tokenize template definition strings  
**So that** I can analyze template structure programmatically without full parsing

**Acceptance Criteria:**
- Can create a lexer instance and pass it a template definition string
- Receive an enumerable sequence of tokens with type, value, and location information
- Access both sync and async APIs based on my use case

### US-2: Developer Writing Tests
**As a** developer writing unit tests  
**I want to** verify that the lexer correctly identifies all token types  
**So that** I can ensure the lexer handles all valid and invalid input correctly

**Acceptance Criteria:**
- Can write tests for each token type independently
- Can verify token location information (line, column)
- Can test error scenarios and exception handling
- Can test performance with large inputs

### US-3: Future Parser Developer
**As a** developer who will build the future AST-based parser  
**I want to** have a well-defined token stream from the lexer  
**So that** I can build a parser that processes tokens instead of raw characters

**Acceptance Criteria:**
- Lexer produces a clean token stream following the grammar
- Token types are clearly defined and documented
- Location information is accurate for error reporting
- API is extensible for future needs

### US-4: Performance-Conscious Developer
**As a** developer working with large template files  
**I want to** process templates without loading entire content into memory  
**So that** I can handle large files efficiently

**Acceptance Criteria:**
- Lexer can process streaming input (TextReader/Stream)
- Uses Span<T>/Memory<T> for .NET 6.0+ to minimize allocations
- Processes input incrementally without buffering entire content
- String inputs are converted to streams internally for consistent processing

## Functional Requirements

### FR-1: Token Type Recognition
The lexer **must** recognize and emit the following 21 token types:

#### FR-1.1: Structural Tokens
- `FrontMatterDelimiter`: The `---` sequence that starts/ends front matter
- `OpenBrace`: `{` character (token start, unless escaped)
- `CloseBrace`: `}` character (token end, unless escaped)
- `Colon`: `:` character (decorator/option separator)
- `Equals`: `=` character (value assignment)
- `Comma`: `,` character (argument separator)
- `OpenParen`: `(` character (decorator argument start)
- `CloseParen`: `)` character (decorator argument end)

#### FR-1.2: Modifier Tokens
- `Question`: `?` character (optional marker)
- `Asterisk`: `*` character (repeating marker)
- `Exclamation`: `!` character (required/not-decorator marker)
- `Dollar`: `$` character (terminate-on-newline marker)
- `Hash`: `#` character (comment marker in front matter)

#### FR-1.3: Literal Tokens
- `QuotedString`: Complete quoted string (single or double quotes); value excludes the quotes themselves
- `Identifier`: Unquoted text sequences (token names, decorator names, values)
- `Text`: Generic text content not matching other categories

**Note**: Individual quote characters are not emitted as separate tokens. Quoted strings are recognized as a single `QuotedString` token with the value containing the content between quotes. The `RawText` property preserves the original form including quotes.

#### FR-1.4: Whitespace and Control Tokens
- `Whitespace`: Space and tab characters
- `Newline`: `\n` or `\r\n` sequences (normalized to single token)

**Note**: Line endings are normalized. Both Unix-style (`\n`) and Windows-style (`\r\n`) line endings produce a single `Newline` token. Standalone `\r` characters (if encountered) are treated as newlines as well.

#### FR-1.5: Escape Sequences
- `EscapedOpenBrace`: `{{` sequence (represents literal `{`)
- `EscapedCloseBrace`: `}}` sequence (represents literal `}`)

#### FR-1.6: End-of-Input Token
- `EndOfInput`: Marks the end of the input stream

### FR-2: Token Structure
Each emitted token **must** contain:

#### FR-2.1: Core Properties
- `Kind`: The token type (enum value from `LexerTokenKind`)
- `Value`: The string value of the token
- `Location`: A `FileLocation` instance indicating line, column, and paragraph

#### FR-2.2: Span Information
- `Start`: The absolute character position where the token starts
- `Length`: The length of the token in characters
- `End`: The absolute character position where the token ends (Start + Length)

#### FR-2.3: Raw Text
- `RawText`: The original text representation including any delimiters (e.g., quotes for quoted strings)

### FR-3: Lexer API Design

#### FR-3.1: Synchronous API
The lexer **must** provide synchronous methods:
```csharp
IEnumerable<LexerToken> Tokenize(string input)
IEnumerable<LexerToken> Tokenize(TextReader input)
IEnumerable<LexerToken> Tokenize(Stream input)
```

#### FR-3.2: Asynchronous API
The lexer **must** provide asynchronous methods:
```csharp
IAsyncEnumerable<LexerToken> TokenizeAsync(string input, CancellationToken cancellationToken = default)
IAsyncEnumerable<LexerToken> TokenizeAsync(TextReader input, CancellationToken cancellationToken = default)
IAsyncEnumerable<LexerToken> TokenizeAsync(Stream input, CancellationToken cancellationToken = default)
```

#### FR-3.3: Input Conversion
For string inputs, the lexer **must**:
- Convert the string to a `TextReader` (specifically `StringReader`)
- Process through the same streaming code path as TextReader/Stream inputs
- Maintain only one execution path for consistency

### FR-4: Location Tracking

#### FR-4.1: Accurate Position Information
The lexer **must**:
- Track line numbers (1-based)
- Track column numbers (1-based)
- Track paragraph numbers (1-based, following existing FileLocation semantics)
- Handle both `\n` and `\r\n` line endings correctly

#### FR-4.2: FileLocation Integration
The lexer **must**:
- Reuse the existing `FileLocation` class from `Tokens.Enumerators`
- Maintain a current location that updates as characters are consumed
- Capture the location at the start of each token
- Clone location objects to prevent mutation issues

### FR-5: Error Handling

#### FR-5.1: Strict Mode
The lexer **must** operate in strict mode:
- Throw exceptions for invalid/unrecognized input
- Use `ParsingException` (or new `LexerException` derived from `TokenizerException`)
- Include location information in error messages
- Fail fast on the first error encountered

#### FR-5.2: Exception Information
Exceptions **must** include:
- Descriptive error message
- Current `FileLocation` (line, column, paragraph)
- The problematic character or sequence
- Context about what was expected

### FR-6: Memory Efficiency

#### FR-6.1: Span-Based Processing (.NET 6.0+)
For .NET 6.0 and later, the lexer **must**:
- Use `ReadOnlySpan<char>` for character buffer processing
- Use `ReadOnlyMemory<char>` for token value storage where appropriate
- Minimize string allocations during tokenization
- Use conditional compilation (`#if NET6_0_OR_GREATER`)

#### FR-6.2: Streaming Input Processing
The lexer **must**:
- Process input incrementally from TextReader/Stream
- Use buffered reading for efficient I/O
- Not load the entire input into memory at once
- Yield tokens as they are identified (lazy evaluation)

#### FR-6.3: .NET Standard 2.0 Compatibility
For .NET Standard 2.0, the lexer **must**:
- Provide equivalent functionality without Span<T>
- Use traditional string-based processing
- Maintain the same API surface
- Use conditional compilation for platform-specific optimizations

### FR-7: Namespace and Organization

#### FR-7.1: Code Location
The lexer implementation **must** be placed in:
- Namespace: `Tokens.Compilation.Lexer`
- Directory: `/src/Tokenizer/Compilation/Lexer/`

#### FR-7.2: Class Structure
Create the following classes:
- `TemplateLexer`: Main lexer implementation
- `LexerToken`: Token structure/class
- `LexerTokenKind`: Enum defining all token types
- Supporting classes as needed (e.g., `LexerException` if not using `ParsingException`)

### FR-8: Unit Test Requirements

#### FR-8.1: Test Location
Unit tests **must** be placed in:
- Namespace: `Tokens.Tests.Compilation.Lexer`
- Directory: `/tests/Tokenizer.Tests/Compilation/Lexer/`

#### FR-8.2: Test Coverage - Token Recognition
Tests **must** verify:
- Each token type can be correctly identified
- Token values are extracted accurately
- Location information is correct for each token
- Multi-character tokens (e.g., `---`, `{{`, `}}`) are recognized
- Quoted strings (single and double) are parsed correctly
- Escape sequences are handled properly

#### FR-8.3: Test Coverage - Error Scenarios
Tests **must** verify:
- Invalid characters throw appropriate exceptions
- Unclosed quoted strings are detected
- Exception messages include location information
- Edge cases (empty input, whitespace-only input, etc.)
- Malformed escape sequences

#### FR-8.4: Test Coverage - Input Methods
Tests **must** verify:
- String input processing
- TextReader input processing
- Stream input processing
- Async enumeration works correctly
- Sync enumeration works correctly

#### FR-8.5: Test Coverage - Streaming and Memory
Tests **must** verify:
- Large input files can be processed
- Memory usage demonstrates streaming behavior (not loading entire input)
- Tokens are yielded incrementally (lazy evaluation)
- Both sync and async enumeration work with large inputs

#### FR-8.6: Test Coverage - Location Tracking
Tests **must** verify:
- Line numbers increment correctly
- Column numbers increment correctly
- Paragraph numbers increment correctly
- Both `\n` and `\r\n` line endings are handled
- Location is captured at token start, not end

#### FR-8.7: Test Coverage - Multi-Target Framework
Tests **must** verify:
- All functionality works on .NET Standard 2.0
- All functionality works on .NET 6.0+
- Span-based optimizations work correctly on .NET 6.0+
- String-based fallbacks work correctly on .NET Standard 2.0

### FR-9: Grammar Adherence

#### FR-9.1: Follow Existing Grammar
The lexer **must**:
- Recognize the same token patterns as `TemplateDefinitionParser`
- Handle front matter syntax (`---`, options, comments)
- Handle token syntax (`{name}`, `{name=value}`, `{name:decorator}`)
- Handle decorator arguments (`decorator(arg1, arg2)`)
- Handle quoted values with single and double quotes
- Handle escape sequences (`{{`, `}}`)
- Handle all token modifiers (`?`, `*`, `!`, `$`)

#### FR-9.2: Grammar Reference
The lexer **must** support tokenizing inputs that match these patterns:

**Front Matter:**
```
---
option: value
set: {tokenName}
# comment
---
```

**Tokens:**
```
{name}              // Simple token
{name?}             // Optional token
{name*}             // Repeating token
{name!}             // Required token
{name$}             // Terminate on newline
{name=value}        // Token with value
{name='value'}      // Token with quoted value
{name:decorator}    // Token with decorator
{name:decorator(arg1, arg2)}  // Decorator with arguments
```

**Escape Sequences:**
```
{{ and }} represent literal braces in preamble text
```

### FR-10: Documentation and Code Quality Standards

#### FR-10.1: XML Documentation Requirements
All code **must** include comprehensive XML documentation:

**Public API Documentation:**
- Every public class, interface, enum, and struct
- Every public method, property, field, and event
- Every public constructor
- All generic type parameters
- All method parameters and return values
- All exceptions that can be thrown

**Documentation Audience:**
The XML documentation **must** be written for two audiences:
1. **Human developers** (junior to senior level) who will use, maintain, or extend the code
2. **AI assistants** (like GitHub Copilot, Claude, ChatGPT) that will help developers understand and work with the code

#### FR-10.2: Documentation Content Requirements
XML documentation **must** include:

**Summary Tags:**
- Clear, concise description of what the type/member does
- Written in complete sentences
- Focus on "what" and "why", not just "how"

**Remarks Tags:**
For non-trivial classes and methods, include:
- **Design decisions and rationale**: Why was this approach chosen over alternatives?
- **Background information**: Context about the problem being solved
- **Grammar references**: How the code relates to the template definition grammar
- **Implementation notes**: Important details about the approach
- **Performance characteristics**: Time/space complexity where relevant
- **Thread safety**: Whether the type/method is thread-safe
- **Multi-framework considerations**: Differences between .NET Standard 2.0 and .NET 6.0+ implementations

**Example Tags:**
Include `<example>` tags showing:
- **Input examples**: Template definition strings that will be tokenized
- **Output examples**: The tokens that would be produced
- **Grammar mapping**: How input syntax maps to token types
- **Usage patterns**: How to use the API in common scenarios
- **Edge cases**: How the code handles unusual or boundary cases

**Code Examples in Documentation:**
```csharp
/// <example>
/// Given the input:
/// <code>
/// {name:ToUpper}
/// </code>
/// The lexer produces these tokens:
/// <code>
/// OpenBrace("{")
/// Identifier("name")
/// Colon(":")
/// Identifier("ToUpper")
/// CloseBrace("}")
/// EndOfInput
/// </code>
/// </example>
```

#### FR-10.3: Internal Documentation
For internal/private members:
- Complex algorithms **must** have explanatory comments
- State machines **must** document states and transitions
- Non-obvious logic **must** explain the reasoning
- Performance-critical code **must** explain optimizations

#### FR-10.4: Decision Documentation
For code that implements specific design decisions (from "Key Design Decisions" section):
- Reference the decision number in comments or remarks
- Explain how the code implements the decision
- Document alternatives that were considered and rejected

**Example:**
```csharp
/// <remarks>
/// Per design decision #3 (Quoted String Tokenization), this method emits
/// a single QuotedString token with the value excluding the quote delimiters.
/// The alternative of emitting separate quote tokens was rejected to simplify
/// parser consumption. The original quoted form is preserved in the RawText property.
/// </remarks>
```

#### FR-10.5: Code Quality Expectations
The implementation **must** demonstrate expert-level (staff engineer) code quality:

**Test-Driven Development (TDD):**
- Code should be designed for testability
- Unit tests should drive the design
- All public APIs should be test-friendly
- Dependencies should be abstracted for testing
- Edge cases should be identified and tested early

**SOLID Principles:**
- **Single Responsibility**: Each class/method has one reason to change
- **Open/Closed**: Open for extension (future token types), closed for modification
- **Liskov Substitution**: All implementations respect their contracts
- **Interface Segregation**: Small, focused interfaces (if any are needed)
- **Dependency Inversion**: Depend on abstractions (IEnumerable, TextReader) not concretions

**Clean Code Practices:**
- Descriptive, intention-revealing names
- Small, focused methods (ideally <20 lines, max 50 lines)
- Minimal cyclomatic complexity (ideally <10, max 15)
- No code duplication (DRY principle)
- Proper separation of concerns
- Clear, linear logic flow
- Early returns to reduce nesting
- Guard clauses for validation
- Consistent abstraction levels within methods

**Error Handling:**
- Fail fast on invalid input
- Provide clear, actionable error messages
- Include location information in exceptions
- Use specific exception types
- Document all exception cases

**Performance Awareness:**
- Use appropriate data structures
- Avoid premature optimization but recognize hot paths
- Leverage Span<T> and Memory<T> where beneficial (.NET 6.0+)
- Minimize allocations in tight loops
- Document performance characteristics

#### FR-10.6: Code Review Readiness
The code **must** be written as if it will be reviewed by:
- Senior/Staff engineers who expect high-quality, maintainable code
- AI assistants that will use it as reference for similar implementations
- Future maintainers who may not have context on original decisions

This means:
- Self-documenting code supplemented with excellent XML docs
- Clear commit messages (out of scope for this PRD)
- No "TODO" or "HACK" comments in final code
- All magic numbers/strings extracted to named constants
- Complex algorithms explained with comments and diagrams if needed

#### FR-10.7: Documentation Examples - Template
Every significant class should include documentation following this template:

```csharp
/// <summary>
/// [Clear one-line description of what this class does]
/// </summary>
/// <remarks>
/// <para>
/// [Background: Why does this class exist? What problem does it solve?]
/// </para>
/// <para>
/// [Design Decision: Reference any key design decisions from the PRD]
/// </para>
/// <para>
/// [Grammar Context: How does this relate to the template definition grammar?]
/// </para>
/// <para>
/// [Implementation Notes: Important details about the approach]
/// </para>
/// <para>
/// [Performance: Any performance characteristics worth noting]
/// </para>
/// <para>
/// [Thread Safety: Is this class thread-safe? Any concurrency considerations?]
/// </para>
/// </remarks>
/// <example>
/// [Show practical usage with input and output examples]
/// </example>
```

## Non-Goals (Out of Scope)

### NG-1: Parser Integration
- **Not** integrating the lexer with the existing `TemplateDefinitionParser` in this phase
- **Not** modifying the parser to consume lexer tokens
- **Not** creating an AST (Abstract Syntax Tree) - this is future work

### NG-2: Parser Functionality
- **Not** performing structural validation (parser's responsibility)
- **Not** building token hierarchies or trees
- **Not** checking semantic correctness (e.g., duplicate token names)

### NG-3: Advanced Optimizations
- **Not** implementing string interning/pooling (future optimization)
- **Not** implementing token lookahead or buffering beyond basic streaming
- **Not** implementing incremental/resumable lexing

### NG-4: Configuration
- **Not** providing configuration options for lexer behavior
- **Not** supporting alternative grammars or dialects
- **Not** providing a "lenient" or "error recovery" mode (strict only)

### NG-5: Additional Features
- **Not** providing syntax highlighting information
- **Not** providing token classification beyond basic kinds
- **Not** supporting preprocessor directives or conditional compilation

## Technical Considerations

### TC-1: Multi-Target Framework Support
- Target both .NET Standard 2.0 and .NET 6.0 (consistent with project standards)
- Use conditional compilation for framework-specific features
- Test on both target frameworks

### TC-2: Dependencies
- Leverage existing `FileLocation` class from `Tokens.Enumerators`
- Leverage existing exception types (`TokenizerException`, `ParsingException`)
- Leverage existing extension methods if applicable
- No new external dependencies

### TC-3: Performance Characteristics
- **Streaming**: Process input incrementally, yielding tokens as identified
- **Lazy Evaluation**: Use `yield return` for synchronous enumeration
- **Async Streaming**: Use `IAsyncEnumerable<T>` for async scenarios
- **Memory**: Minimize allocations through Span<T> usage on .NET 6.0+

### TC-4: Character Encoding
- Assume input is UTF-16 encoded (standard .NET string/char encoding)
- Handle Unicode characters in identifiers and text values
- No special handling for BOM (Byte Order Mark) needed for string input

### TC-5: Thread Safety
- Lexer instances **do not** need to be thread-safe
- Each tokenization operation creates its own state
- Multiple concurrent tokenizations should use separate lexer instances or calls

### TC-6: API Design Patterns
- Follow existing Tokenizer library patterns
- Use method overloads for different input types
- Use `CancellationToken` for async methods
- Return `IEnumerable<T>` and `IAsyncEnumerable<T>` for streaming

## Design Considerations

### DC-1: LexerToken as Class vs. Struct
**Recommendation**: Use a **class** for `LexerToken`

**Rationale**:
- Contains reference type (`FileLocation`)
- Contains string values (reference type)
- Yielded in enumerations (value type boxing would occur)
- Consistency with existing codebase patterns

### DC-2: Single Execution Path
**Recommendation**: Convert all input types to `TextReader` internally

**Rationale**:
- Maintains single, well-tested code path
- `string` → `StringReader` → `TextReader` path
- `Stream` → `StreamReader` → `TextReader` path
- `TextReader` → direct processing
- Simplifies maintenance and testing

### DC-3: Lookahead Strategy
**Recommendation**: Implement single-character lookahead (peek)

**Rationale**:
- Grammar requires checking next character in many cases
- `TextReader.Peek()` provides this capability
- Multi-character lookahead can be built from single-char peek
- Consistent with existing `TemplateDefinitionEnumerator` pattern

### DC-4: Token Value Representation
**Recommendation**: Store token values as strings, with optional span-based processing

**Rationale**:
- Need to return values from iterators (can't return spans from `yield`)
- For .NET 6.0+, can use span internally and convert to string for token
- For .NET Standard 2.0, use strings throughout
- Balance between performance and API usability

### DC-5: Exception Type
**Recommendation**: Create new `LexerException` deriving from `TokenizerException`

**Rationale**:
- Provides clear distinction between lexer and parser errors
- Allows specific catch blocks for lexer errors
- Includes location information
- Follows existing exception hierarchy

## Success Metrics

### SM-1: Code Quality
- **Zero** compiler warnings
- **Zero** static analysis warnings
- **100%** XML documentation coverage for public APIs
- **100%** XML documentation includes `<remarks>` tags for non-trivial members
- **≥80%** of public classes/methods include `<example>` tags with input/output examples
- Code follows all rules in C# coding guidelines
- Code demonstrates staff-level engineering quality (TDD, SOLID principles)
- Cyclomatic complexity ≤15 for all methods (ideally ≤10)
- Method length ≤50 lines (ideally ≤20)

### SM-2: Test Coverage
- **≥95%** code coverage for the lexer implementation
- **≥90%** code coverage for supporting classes
- All token types have dedicated tests
- All error paths have dedicated tests
- All input methods (string, TextReader, Stream, sync, async) are tested

### SM-3: Performance
- Token yielding is truly lazy (verified through tests)
- Memory usage demonstrates streaming behavior (not loading entire input)
- Note: Formal performance benchmarks will be added in a future phase

### SM-4: Compatibility
- All tests pass on .NET Standard 2.0 target
- All tests pass on .NET 6.0 target
- Span-based code compiles and runs correctly on .NET 6.0+
- Fallback code compiles and runs correctly on .NET Standard 2.0

### SM-5: Documentation Quality
- Every public class/method/property has comprehensive XML summary documentation
- All XML documentation is written for both human developers and AI assistants
- Non-trivial classes/methods include `<remarks>` with design decisions and rationale
- Public APIs include `<example>` tags with practical input/output examples
- Complex internal methods have explanatory comments
- Design decision references are included in code comments where applicable
- Documentation includes grammar references showing how code relates to template syntax
- All exceptions are documented with `<exception>` tags
- Documentation quality is suitable for use as educational reference material

## Decisions on Open Questions

### OQ-1: Token Kind Granularity ✅ DECIDED
**Question**: Should we distinguish between different contexts for the same character?
- Example: `:` in front matter (option separator) vs. `:` in token (decorator separator)

**Decision**: Do **NOT** distinguish between token contexts. The lexer should be concerned only with the grammar, not semantic context. Use a single token kind per character and let the parser determine the contextual meaning.

**Rationale**: Keeps the lexer simple, stateless, and focused on lexical analysis. Context-awareness is the parser's responsibility.

### OQ-2: Whitespace Handling ✅ DECIDED
**Question**: Should the lexer emit all whitespace tokens or only significant ones?

**Decision**: **Option A** - Emit all whitespace and newlines (grammar-agnostic, maximum information)

**Rationale**: Provides complete token stream without making assumptions about significance. The parser or consumer can filter whitespace as needed. This keeps the lexer simple and grammar-agnostic.

### OQ-3: Quoted String Tokenization ✅ DECIDED
**Question**: Should quoted strings emit as separate quote tokens or composite tokens?

**Decision**: **Option B** - Single `QuotedString` token with value excluding quotes

**Rationale**: Simplifies parser consumption and provides cleaner API. The token's `RawText` property will preserve the original quoted form if needed.

### OQ-4: Identifier vs. Text ✅ DECIDED
**Question**: How should we distinguish between identifiers (token names, decorator names) and generic text?

**Decision**: **Option A** - Lexer emits generic `Identifier` or `Text` tokens; parser determines semantic meaning

**Rationale**: Keeps lexer stateless and simple. The lexer doesn't need to understand whether an identifier is a token name, decorator name, or option name - that's semantic information for the parser.

### OQ-5: End-of-Line Normalization ✅ DECIDED
**Question**: Should the lexer normalize line endings?

**Decision**: **Option A** - Emit `\r\n` as single `Newline` token

**Rationale**: Simplifies token stream and is consistent with existing FileLocation handling. Both `\n` and `\r\n` will produce a single `Newline` token.

### OQ-6: Future AST Integration ✅ DECIDED
**Question**: What token metadata should we include now to facilitate future AST generation?

**Decision**: Current design is sufficient (Kind, Value, Location, Start/End positions, RawText)

**Rationale**: This provides all necessary information for AST construction. Additional properties can be added to LexerToken class in the future if needed without breaking changes.

### OQ-7: Benchmark Targets ✅ DECIDED
**Question**: What should be the performance benchmarks for "acceptable" performance?

**Decision**: Do **NOT** add formal benchmarks in this phase

**Rationale**: Focus on correctness and comprehensive unit testing first. Performance testing and benchmarking can be added in a future phase once the implementation is stable and proven correct.

---

## Implementation Notes for Developer

### Phase 1: Core Lexer Structure (TDD Approach)
1. Create `LexerTokenKind` enum with all token types and comprehensive XML documentation
2. Create `LexerToken` class with all required properties and comprehensive XML documentation
3. Create `LexerException` class with XML documentation
4. Create `TemplateLexer` class skeleton with API methods and comprehensive XML documentation
5. Write initial unit tests for the API surface before implementation
6. Ensure all code includes XML `<summary>`, `<remarks>`, and `<example>` tags

### Phase 2: Basic Tokenization (TDD)
1. Write tests for single-character tokens FIRST
2. Implement TextReader-based tokenization loop
3. Implement character reading and location tracking (referencing FileLocation design)
4. Implement single-character token recognition
5. Add XML documentation with grammar mapping examples
6. Refactor to ensure SOLID principles and clean code practices

### Phase 3: Complex Tokens (TDD)
1. Write tests for multi-character tokens and quoted strings FIRST
2. Implement multi-character tokens (`---`, `{{`, `}}`)
3. Implement quoted string tokenization (per design decision #3)
4. Implement identifier/text tokenization
5. Add XML documentation with input/output examples
6. Document design decisions in `<remarks>` tags
7. Refactor for code quality (method length, complexity)

### Phase 4: Input Adapters (TDD)
1. Write tests for all input methods FIRST
2. Implement string → StringReader conversion (single execution path)
3. Implement Stream → StreamReader conversion
4. Implement async enumeration with IAsyncEnumerable
5. Add comprehensive XML documentation with usage examples
6. Document async patterns and cancellation token handling
7. Verify thread safety considerations and document in remarks

### Phase 5: Optimization & Polish
1. Add Span<T>-based optimizations for .NET 6.0+ with conditional compilation
2. Implement comprehensive error handling with LexerException
3. Verify streaming behavior and lazy evaluation through tests
4. Review and enhance all XML documentation for completeness
5. Ensure all `<example>` tags show real template syntax
6. Add performance notes to `<remarks>` where applicable
7. Code review against SOLID principles and clean code checklist

### Phase 6: Comprehensive Testing & Documentation Review
1. Verify tests for all token types with grammar examples
2. Verify tests for all error scenarios with clear assertions
3. Verify tests for location tracking accuracy
4. Verify tests for multi-target frameworks (both .NET Standard 2.0 and .NET 6.0+)
5. Verify tests for streaming behavior and lazy evaluation
6. Review all XML documentation for educational quality
7. Verify design decision references in code comments
8. Ensure documentation is suitable for both human developers and AI assistants
9. Verify cyclomatic complexity and method length metrics

## Appendix A: Complete Token Kind List

```csharp
public enum LexerTokenKind
{
    // Structural
    FrontMatterDelimiter,    // ---
    OpenBrace,               // {
    CloseBrace,              // }
    Colon,                   // :
    Equals,                  // =
    Comma,                   // ,
    OpenParen,               // (
    CloseParen,              // )
    
    // Modifiers
    Question,                // ?
    Asterisk,                // *
    Exclamation,             // !
    Dollar,                  // $
    Hash,                    // #
    
    // Literals
    QuotedString,            // 'text' or "text" (value excludes quotes)
    Identifier,              // alphanumeric sequences
    Text,                    // other text content
    
    // Whitespace
    Whitespace,              // space, tab
    Newline,                 // \n or \r\n (normalized to single token)
    
    // Escape sequences
    EscapedOpenBrace,        // {{
    EscapedCloseBrace,       // }}
    
    // Control
    EndOfInput               // end of stream
}
```

## Appendix B: Example Usage

```csharp
// Synchronous usage with string
var lexer = new TemplateLexer();
foreach (var token in lexer.Tokenize("{name:ToUpper}"))
{
    Console.WriteLine($"{token.Kind}: '{token.Value}' at {token.Location}");
}

// Asynchronous usage with stream
await using var stream = File.OpenRead("template.txt");
await foreach (var token in lexer.TokenizeAsync(stream))
{
    Console.WriteLine($"{token.Kind}: '{token.Value}' at {token.Location}");
}
```

## Appendix C: Reference to Existing Parser

The lexer should recognize patterns used by `TemplateDefinitionParser`. Key methods to reference for grammar understanding:

- `ParseFrontMatter` (lines 160-197): Front matter structure
- `ParsePreamble` (lines 332-367): Text and escape sequences
- `ParseTokenName` (lines 369-528): Token structure and modifiers
- `ParseTokenValue` (lines 530-597): Value assignment and quotes
- `ParseDecorator` (lines 673-734): Decorator syntax
- `ParseDecoratorArgument` (lines 736-790): Argument syntax

## Appendix D: Documentation Examples

This appendix provides concrete examples of the expected documentation quality for the lexer implementation.

### Example 1: LexerTokenKind Enum Documentation

```csharp
/// <summary>
/// Defines the types of tokens that can be recognized by the <see cref="TemplateLexer"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each token kind represents a distinct lexical element in the template definition grammar.
/// The lexer operates in a context-free manner - it identifies token types based purely on
/// character patterns, not on semantic context. This design (Decision #1) keeps the lexer
/// simple and stateless, delegating context interpretation to the parser.
/// </para>
/// <para>
/// Token kinds are organized into categories:
/// - Structural: Delimiters and brackets that define template structure
/// - Modifiers: Special characters that modify token behavior (?, *, !, $, #)
/// - Literals: Text content and quoted strings
/// - Whitespace: Spaces, tabs, and normalized line endings
/// - Escape sequences: Escaped brace characters
/// - Control: End of input marker
/// </para>
/// <para>
/// Note: Per Design Decision #3, quoted strings are represented as a single QuotedString
/// token rather than separate quote and content tokens. Per Decision #5, both \n and \r\n
/// line endings are normalized to a single Newline token.
/// </para>
/// </remarks>
/// <example>
/// Token kinds map to grammar elements as follows:
/// <code>
/// Input: {name:ToUpper}
/// Tokens:
///   OpenBrace
///   Identifier (value: "name")
///   Colon
///   Identifier (value: "ToUpper")
///   CloseBrace
///   EndOfInput
/// </code>
/// </example>
public enum LexerTokenKind
{
    /// <summary>Front matter delimiter (---)</summary>
    FrontMatterDelimiter,
    
    /// <summary>Opening brace ({) marking start of a token</summary>
    OpenBrace,
    
    /// <summary>Closing brace (}) marking end of a token</summary>
    CloseBrace,
    
    // ... etc
}
```

### Example 2: LexerToken Class Documentation

```csharp
/// <summary>
/// Represents a single lexical token produced by the <see cref="TemplateLexer"/>.
/// </summary>
/// <remarks>
/// <para>
/// A LexerToken encapsulates all information about a recognized token including its type,
/// value, and location in the source text. This class is designed to be immutable after
/// construction to ensure thread-safety when tokens are shared across contexts.
/// </para>
/// <para>
/// Token location tracking uses the existing <see cref="FileLocation"/> class to maintain
/// consistency with the rest of the Tokenizer library. Location information is captured
/// at the START of each token, not the end, to facilitate error reporting.
/// </para>
/// <para>
/// The RawText property preserves the original text representation including any delimiters.
/// For example, a quoted string token will have Value="hello" but RawText="'hello'" or
/// RawText="\"hello\"". This supports scenarios where the original formatting matters.
/// </para>
/// <para>
/// Thread Safety: LexerToken instances are immutable and safe to share across threads.
/// The FileLocation is cloned during construction to prevent external mutation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var token = new LexerToken(
///     kind: LexerTokenKind.QuotedString,
///     value: "hello world",
///     rawText: "'hello world'",
///     location: currentLocation.Clone(),
///     start: 10,
///     length: 13
/// );
/// 
/// Console.WriteLine(token.Value);     // Output: hello world
/// Console.WriteLine(token.RawText);   // Output: 'hello world'
/// Console.WriteLine(token.Location);  // Output: Ln: 1 Col: 11 Para: 1
/// </code>
/// </example>
public class LexerToken
{
    /// <summary>
    /// Gets the type of this token.
    /// </summary>
    public LexerTokenKind Kind { get; }
    
    /// <summary>
    /// Gets the string value of this token, excluding any delimiters.
    /// </summary>
    /// <remarks>
    /// For quoted strings (per Design Decision #3), this value excludes the quote
    /// characters. For example, the input 'hello' produces Value="hello".
    /// For all other tokens, this is the literal character sequence.
    /// </remarks>
    public string Value { get; }
    
    // ... additional properties with similar documentation
}
```

### Example 3: TemplateLexer Class Documentation

```csharp
/// <summary>
/// Performs lexical analysis on template definition strings, converting them into
/// a stream of <see cref="LexerToken"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Background:</strong>
/// The TemplateLexer separates lexical analysis from parsing in the Tokenizer library.
/// Previously, the TemplateDefinitionParser combined both responsibilities in a single
/// 963-line class. This lexer extracts the character-by-character scanning logic into
/// a dedicated, testable component.
/// </para>
/// <para>
/// <strong>Design Philosophy:</strong>
/// The lexer is intentionally context-free and stateless (Design Decision #1). It identifies
/// tokens based purely on character patterns without understanding semantic meaning. For
/// example, the ':' character always produces a Colon token regardless of whether it appears
/// in front matter or a decorator - context interpretation is the parser's responsibility.
/// </para>
/// <para>
/// <strong>Grammar Support:</strong>
/// The lexer recognizes all elements of the template definition grammar including:
/// - Front matter blocks (delimited by ---)
/// - Token definitions ({name}, {name=value}, {name:decorator})
/// - Modifiers (?, *, !, $)
/// - Quoted strings (single and double quotes)
/// - Escape sequences ({{ and }})
/// - Decorator arguments (decorator(arg1, arg2))
/// </para>
/// <para>
/// <strong>Implementation Approach:</strong>
/// All input types (string, TextReader, Stream) are converted to TextReader internally
/// to maintain a single execution path (Design Decision #4). This simplifies testing and
/// ensures consistent behavior across input methods.
/// </para>
/// <para>
/// <strong>Performance:</strong>
/// The lexer uses streaming/lazy evaluation - tokens are yielded as they are identified
/// without loading the entire input into memory. For .NET 6.0+, Span&lt;T&gt; and
/// Memory&lt;T&gt; optimizations minimize allocations during tokenization.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong>
/// TemplateLexer instances are NOT thread-safe. Each tokenization operation should use
/// a separate lexer instance or method call. However, the produced LexerToken instances
/// are immutable and safe to share across threads.
/// </para>
/// </remarks>
/// <example>
/// Basic synchronous usage:
/// <code>
/// var lexer = new TemplateLexer();
/// var input = "{name:ToUpper}";
/// 
/// foreach (var token in lexer.Tokenize(input))
/// {
///     Console.WriteLine($"{token.Kind}: {token.Value}");
/// }
/// 
/// // Output:
/// // OpenBrace: {
/// // Identifier: name
/// // Colon: :
/// // Identifier: ToUpper
/// // CloseBrace: }
/// // EndOfInput: 
/// </code>
/// </example>
/// <example>
/// Asynchronous usage with cancellation:
/// <code>
/// var lexer = new TemplateLexer();
/// await using var stream = File.OpenRead("template.txt");
/// var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
/// 
/// await foreach (var token in lexer.TokenizeAsync(stream, cts.Token))
/// {
///     // Process token
///     if (token.Kind == LexerTokenKind.EndOfInput) break;
/// }
/// </code>
/// </example>
public class TemplateLexer
{
    /// <summary>
    /// Tokenizes the specified template definition string.
    /// </summary>
    /// <param name="input">The template definition string to tokenize.</param>
    /// <returns>
    /// An enumerable sequence of <see cref="LexerToken"/> instances representing
    /// the lexical elements of the input string.
    /// </returns>
    /// <remarks>
    /// This method converts the string to a StringReader internally and delegates
    /// to the TextReader-based implementation to ensure a single execution path.
    /// Tokens are yielded lazily as they are identified.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="input"/> is null.
    /// </exception>
    /// <exception cref="LexerException">
    /// Thrown when invalid or unrecognized characters are encountered in the input.
    /// The exception includes location information (line, column) to aid debugging.
    /// </exception>
    /// <example>
    /// <code>
    /// var lexer = new TemplateLexer();
    /// foreach (var token in lexer.Tokenize("---\nname: MyTemplate\n---\n{value}"))
    /// {
    ///     Console.WriteLine($"{token.Kind} at {token.Location}");
    /// }
    /// </code>
    /// </example>
    public IEnumerable<LexerToken> Tokenize(string input)
    {
        // Implementation
    }
}
```

### Example 4: Internal Method Documentation

```csharp
/// <summary>
/// Attempts to read a quoted string token from the input stream.
/// </summary>
/// <param name="reader">The text reader to read from.</param>
/// <param name="quoteChar">The quote character that started the string (' or ").</param>
/// <param name="location">The location where the quoted string started.</param>
/// <returns>A LexerToken representing the quoted string.</returns>
/// <remarks>
/// <para>
/// Per Design Decision #3, this method emits a single QuotedString token with the
/// value excluding the quote delimiters. The alternative of emitting separate tokens
/// for quotes and content was rejected to simplify parser consumption.
/// </para>
/// <para>
/// The token's Value property contains the string content without quotes.
/// The token's RawText property preserves the original form including quotes.
/// For example: input 'hello' produces Value="hello" and RawText="'hello'".
/// </para>
/// <para>
/// Implementation note: This method reads characters until it encounters the matching
/// closing quote. Unclosed strings (EOF before closing quote) result in a LexerException.
/// There is no escape sequence support within quoted strings in this grammar.
/// </para>
/// </remarks>
/// <exception cref="LexerException">
/// Thrown if EOF is reached before the closing quote is found.
/// </exception>
private LexerToken ReadQuotedString(TextReader reader, char quoteChar, FileLocation location)
{
    // Implementation with clear, well-commented logic
}
```

### Example 5: Test Class Documentation

```csharp
/// <summary>
/// Unit tests for the <see cref="TemplateLexer"/> class.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the lexer correctly tokenizes all elements of the template
/// definition grammar. Tests are organized by token type and cover:
/// - Basic token recognition for each token kind
/// - Multi-character tokens (---, {{, }})
/// - Quoted string handling (per Design Decision #3)
/// - Line ending normalization (per Design Decision #5)
/// - Error scenarios and exception handling
/// - Location tracking accuracy
/// - Streaming behavior and lazy evaluation
/// </para>
/// <para>
/// Test naming follows the Gherkin convention: GivenScenario_WhenAction_ThenResult
/// </para>
/// </remarks>
public class TemplateLexerTests
{
    /// <summary>
    /// Verifies that a simple token definition is tokenized correctly.
    /// </summary>
    /// <remarks>
    /// This test validates the basic lexer functionality with a token that includes
    /// a name, decorator, and decorator argument. It verifies that each grammar element
    /// is recognized and emitted as the correct token type.
    /// 
    /// Grammar mapping:
    /// {name:ToUpper()} → OpenBrace, Identifier, Colon, Identifier, OpenParen, CloseParen, CloseBrace
    /// </remarks>
    [Fact]
    public void GivenSimpleTokenWithDecorator_WhenTokenizing_ThenEmitsCorrectTokens()
    {
        // Arrange
        var lexer = new TemplateLexer();
        var input = "{name:ToUpper()}";
        
        // Act
        var tokens = lexer.Tokenize(input).ToList();
        
        // Assert
        Assert.Equal(7, tokens.Count);
        Assert.Equal(LexerTokenKind.OpenBrace, tokens[0].Kind);
        Assert.Equal(LexerTokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("name", tokens[1].Value);
        // ... etc
    }
}
```

These examples demonstrate the expected level of detail and educational value in the documentation. Every class, method, and design decision should be clearly explained with practical examples that show real template syntax and its token representation.

---

**Document Version:** 1.1  
**Created:** 2025-10-14  
**Last Updated:** 2025-10-14  
**Target Implementation:** Tokenizer Library v3  
**Author:** AI Assistant  
**Status:** Approved - All Design Decisions Finalized - Ready for Implementation

