# Compilation Namespace Refactoring Tasks

## Relevant Files

### Source Files to be Renamed and Moved
- `src/Tokenizer/PreToken.cs` - Main definition class for token data structure
- `src/Tokenizer/PreTemplate.cs` - Container class for template definitions
- `src/Tokenizer/PreTokenDecorator.cs` - Decorator definition class
- `src/Tokenizer/Parsers/PreTokenParser.cs` - Main parser class for template strings (will become TemplateDefinitionParser)
- `src/Tokenizer/Enumerators/PreTokenEnumerator.cs` - Character enumerator for parsing (will become TemplateDefinitionEnumerator)

### Source Files Updated
- `src/Tokenizer/Parsers/TokenParser.cs` - Main token parser that uses PreTokenParser ✅
- `src/Tokenizer/Exceptions/ParsingException.cs` - Exception class that may reference Pre* classes ✅
- `src/Tokenizer/Tokenizer.csproj` - Project file to update file references

### Test Files to be Renamed and Moved
- `tests/Tokenizer.Tests/Parsers/PreTokenParserTests.cs` - Unit tests for PreTokenParser
- `tests/Tokenizer.Tests/Tokenizer.Tests.csproj` - Test project file to update references

### New Files Created
- `src/Tokenizer/Compilation/Definitions/TokenDefinition.cs` - Renamed from PreToken.cs ✅
- `src/Tokenizer/Compilation/Definitions/TemplateDefinition.cs` - Renamed from PreTemplate.cs ✅
- `src/Tokenizer/Compilation/Definitions/DecoratorDefinition.cs` - Renamed from PreTokenDecorator.cs ✅
- `src/Tokenizer/Compilation/Parsing/TemplateDefinitionParser.cs` - Renamed from PreTokenParser.cs ✅
- `src/Tokenizer/Compilation/Parsing/TemplateDefinitionEnumerator.cs` - Renamed from PreTokenEnumerator.cs ✅
- `src/Tokenizer/Compilation/Parsing/TemplateDefinitionParserState.cs` - Moved and renamed enum from PreTokenParser.cs ✅
- `tests/Tokenizer.Tests/Compilation/Definitions/TokenDefinitionTests.cs` - Renamed test file
- `tests/Tokenizer.Tests/Compilation/Definitions/TemplateDefinitionTests.cs` - New test file
- `tests/Tokenizer.Tests/Compilation/Definitions/DecoratorDefinitionTests.cs` - New test file
- `tests/Tokenizer.Tests/Compilation/Parsing/TemplateDefinitionParserTests.cs` - Renamed test file
- `tests/Tokenizer.Tests/Compilation/Parsing/TemplateDefinitionEnumeratorTests.cs` - New test file
- `tests/Tokenizer.Tests/Compilation/Parsing/TemplateDefinitionParserStateTests.cs` - New test file for enum

### Notes

- Unit tests should be placed alongside the code files they are testing in the corresponding test folder structure
- Use `dotnet build` to build the solution and `dotnet test` to run tests
- All changes are internal to the library - no public API changes required
- The FlatTokenParserState enum will be extracted from PreTokenParser.cs into its own file and renamed to TemplateDefinitionParserState

## Tasks

- [x] 1.0 Create New Folder Structure and Namespaces
  - [x] 1.1 Create `src/Tokenizer/Compilation/Definitions/` folder
  - [x] 1.2 Create `src/Tokenizer/Compilation/Parsing/` folder
  - [x] 1.3 Create `tests/Tokenizer.Tests/Compilation/Definitions/` folder
  - [x] 1.4 Create `tests/Tokenizer.Tests/Compilation/Parsing/` folder
  - [x] 1.5 Verify folder structure matches namespace organization

- [x] 2.0 Rename and Move Definition Classes
  - [x] 2.1 Copy `PreToken.cs` to `src/Tokenizer/Compilation/Definitions/TokenDefinition.cs`
  - [x] 2.2 Update namespace in TokenDefinition.cs to `Tokens.Compilation.Definitions`
  - [x] 2.3 Rename class from `PreToken` to `TokenDefinition`
  - [x] 2.4 Update XML documentation to reflect new class name
  - [x] 2.5 Copy `PreTemplate.cs` to `src/Tokenizer/Compilation/Definitions/TemplateDefinition.cs`
  - [x] 2.6 Update namespace in TemplateDefinition.cs to `Tokens.Compilation.Definitions`
  - [x] 2.7 Rename class from `PreTemplate` to `TemplateDefinition`
  - [x] 2.8 Update XML documentation and internal references to use `TokenDefinition`
  - [x] 2.9 Copy `PreTokenDecorator.cs` to `src/Tokenizer/Compilation/Definitions/DecoratorDefinition.cs`
  - [x] 2.10 Update namespace in DecoratorDefinition.cs to `Tokens.Compilation.Definitions`
  - [x] 2.11 Rename class from `PreTokenDecorator` to `DecoratorDefinition`
  - [x] 2.12 Update XML documentation to reflect new class name

- [x] 3.0 Rename and Move Parsing Classes
  - [x] 3.1 Extract `FlatTokenParserState` enum from `PreTokenParser.cs` to `src/Tokenizer/Compilation/Parsing/TemplateDefinitionParserState.cs`
  - [x] 3.2 Update namespace for TemplateDefinitionParserState enum to `Tokens.Compilation.Parsing`
  - [x] 3.3 Copy `PreTokenParser.cs` to `src/Tokenizer/Compilation/Parsing/TemplateDefinitionParser.cs`
  - [x] 3.4 Update namespace in TemplateDefinitionParser.cs to `Tokens.Compilation.Parsing`
  - [x] 3.5 Rename class from `PreTokenParser` to `TemplateDefinitionParser`
  - [x] 3.6 Update XML documentation to reflect new class name and purpose
  - [x] 3.7 Update internal references to use new class names (TokenDefinition, TemplateDefinition, DecoratorDefinition, TemplateDefinitionParserState)
  - [x] 3.8 Copy `PreTokenEnumerator.cs` to `src/Tokenizer/Compilation/Parsing/TemplateDefinitionEnumerator.cs`
  - [x] 3.9 Update namespace in TemplateDefinitionEnumerator.cs to `Tokens.Compilation.Parsing`
  - [x] 3.10 Rename class from `PreTokenEnumerator` to `TemplateDefinitionEnumerator`
  - [x] 3.11 Update XML documentation to reflect new class name

- [x] 4.0 Update Internal References
  - [x] 4.1 Update `TokenParser.cs` to use new class names and namespaces
  - [x] 4.2 Add appropriate using statements for `Tokens.Compilation.Definitions` and `Tokens.Compilation.Parsing`
  - [x] 4.3 Update all variable declarations and instantiations to use new class names
  - [x] 4.4 Update method parameters and return types to use new class names
  - [x] 4.5 Update XML documentation references to use new class names
  - [x] 4.6 Check `ParsingException.cs` for any references to Pre* classes and update if needed
  - [x] 4.7 Remove unused using statements from all modified files
  - [x] 4.8 Verify no circular dependencies are introduced

- [x] 5.0 Reorganize and Update Tests
  - [x] 5.1 Copy `PreTokenParserTests.cs` to `tests/Tokenizer.Tests/Compilation/Parsing/TemplateDefinitionParserTests.cs`
  - [x] 5.2 Update namespace in TemplateDefinitionParserTests.cs to `Tokens.Tests.Compilation.Parsing`
  - [x] 5.3 Rename test class from `PreTokenParserTests` to `TemplateDefinitionParserTests`
  - [x] 5.4 Update all test method names and assertions to use new class names (TemplateDefinitionParser, TemplateDefinitionParserState)
  - [x] 5.5 Create `tests/Tokenizer.Tests/Compilation/Definitions/TokenDefinitionTests.cs`
  - [x] 5.6 Create unit tests for TokenDefinition class covering all public methods and properties
  - [x] 5.7 Create `tests/Tokenizer.Tests/Compilation/Definitions/TemplateDefinitionTests.cs`
  - [x] 5.8 Create unit tests for TemplateDefinition class covering all public methods and properties
  - [x] 5.9 Create `tests/Tokenizer.Tests/Compilation/Definitions/DecoratorDefinitionTests.cs`
  - [x] 5.10 Create unit tests for DecoratorDefinition class covering all public methods and properties
  - [x] 5.11 Create `tests/Tokenizer.Tests/Compilation/Parsing/TemplateDefinitionEnumeratorTests.cs`
  - [x] 5.12 Create unit tests for TemplateDefinitionEnumerator class covering all public methods
  - [x] 5.13 Create unit tests for TemplateDefinitionParserState enum covering all enum values
  - [x] 5.14 Update test project file to include new test files

- [x] 6.0 Verify Build and Test Success
  - [x] 6.1 Update `src/Tokenizer/Tokenizer.csproj` to include new file references
  - [x] 6.2 Update `tests/Tokenizer.Tests/Tokenizer.Tests.csproj` to include new test file references
  - [x] 6.3 Run `dotnet build` to verify all projects compile successfully
  - [x] 6.4 Run `dotnet test` to verify all unit tests pass
  - [x] 6.5 Verify no compiler warnings are introduced
  - [x] 6.6 Delete original Pre* class files after successful verification
  - [x] 6.7 Delete original test files after successful verification
  - [x] 6.8 Run full test suite one final time to ensure no regressions
  - [x] 6.9 Verify public API remains unchanged by checking that existing public classes and methods are unaffected
