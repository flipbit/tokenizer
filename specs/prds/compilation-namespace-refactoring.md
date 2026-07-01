# Product Requirements Document: Compilation Namespace Refactoring

## Introduction/Overview

This feature refactors the internal parsing and compilation classes within the Tokenizer library to improve code organization, naming conventions, and maintainability. The refactoring will rename classes with the "Pre" prefix to use "Definition" suffix and reorganize them into a dedicated "Compilation" namespace with proper folder structure. This change is internal to the library and will not affect the public API or external consumers.

**Problem:** The current "Pre" prefix naming is ambiguous and doesn't clearly convey the purpose of these intermediate parsing classes. Additionally, the parsing-related classes are scattered throughout the codebase without a clear organizational structure.

**Goal:** Create a well-organized, clearly named compilation subsystem that separates parsing concerns from the main tokenization logic.

## Goals

1. **Improve Code Organization**: Consolidate all parsing and compilation-related classes into a dedicated namespace and folder structure
2. **Enhance Naming Clarity**: Replace ambiguous "Pre" prefix with descriptive "Definition" suffix
3. **Maintain Public API Stability**: Ensure no breaking changes to external consumers
4. **Improve Developer Experience**: Make the codebase more intuitive for developers working with the parsing subsystem
5. **Establish Clear Separation of Concerns**: Clearly separate compilation/parsing logic from execution logic

## User Stories

**As a developer working on the Tokenizer library**, I want parsing classes to have clear, descriptive names so that I can quickly understand their purpose and role in the system.

**As a developer maintaining the codebase**, I want related parsing classes to be organized in a dedicated namespace so that I can easily locate and modify parsing logic.

**As a developer extending the tokenizer**, I want the compilation subsystem to be well-organized so that I can understand how to add new parsing features.

**As a library consumer**, I want the public API to remain unchanged so that my existing code continues to work without modification.

## Functional Requirements

### 1. Class Renaming
1.1. Rename `PreToken` to `TokenDefinition`
1.2. Rename `PreTemplate` to `TemplateDefinition`  
1.3. Rename `PreTokenDecorator` to `DecoratorDefinition`
1.4. Rename `PreTokenParser` to `TemplateDefinitionParser`
1.5. Rename `PreTokenEnumerator` to `TemplateDefinitionEnumerator`

### 2. Namespace Reorganization
2.1. Create new namespace `Tokens.Compilation.Definitions` for definition classes
2.2. Create new namespace `Tokens.Compilation.Parsing` for parsing classes
2.3. Move `FlatTokenParserState` enum to `Tokens.Compilation.Parsing` namespace and rename to `TemplateDefinitionParserState`
2.4. Update all internal references to use new namespaces

### 3. Folder Structure
3.1. Create folder `src/Tokenizer/Compilation/Definitions/` for definition classes
3.2. Create folder `src/Tokenizer/Compilation/Parsing/` for parsing classes
3.3. Move files to appropriate folders matching namespace structure
3.4. Update project file references

### 4. Test Reorganization
4.1. Create corresponding test folder structure mirroring source organization
4.2. Move existing test files to appropriate test folders
4.3. Update test namespaces to match new source namespaces
4.4. Update test class names to reflect new naming convention
4.5. Ensure all unit tests pass after refactoring

### 5. Internal API Updates
5.1. Update `TokenParser` class to use new `TemplateDefinitionParser` and `TemplateDefinition`
5.2. Update all internal references to use new class names
5.3. Update XML documentation to reflect new naming
5.4. Ensure `Tokenizer` class and public interfaces remain unchanged

### 6. Code Quality Improvements
6.1. Update XML documentation for all renamed classes
6.2. Ensure consistent naming conventions throughout compilation subsystem
6.3. Add appropriate using statements and remove unused imports
6.4. Verify no circular dependencies are introduced

## Non-Goals (Out of Scope)

- **Public API Changes**: No changes to public interfaces, classes, or methods
- **Breaking Changes**: No breaking changes for external consumers
- **Performance Optimizations**: This refactoring focuses on organization, not performance
- **New Features**: No new functionality will be added during this refactoring
- **Backward Compatibility**: No temporary aliases or deprecated attributes will be maintained
- **External Dependencies**: No changes to NuGet package dependencies

## Design Considerations

### Namespace Structure
```
Tokens.Compilation.Definitions/
├── TokenDefinition.cs
├── TemplateDefinition.cs
└── DecoratorDefinition.cs

Tokens.Compilation.Parsing/
├── TemplateDefinitionParser.cs
├── TemplateDefinitionEnumerator.cs
└── TemplateDefinitionParserState.cs (enum)
```

### Folder Structure
```
src/Tokenizer/Compilation/
├── Definitions/
│   ├── TokenDefinition.cs
│   ├── TemplateDefinition.cs
│   └── DecoratorDefinition.cs
└── Parsing/
    ├── TemplateDefinitionParser.cs
    ├── TemplateDefinitionEnumerator.cs
    └── TemplateDefinitionParserState.cs
```

### Test Structure
```
tests/Tokenizer.Tests/Compilation/
├── Definitions/
│   ├── TokenDefinitionTests.cs
│   ├── TemplateDefinitionTests.cs
│   └── DecoratorDefinitionTests.cs
└── Parsing/
    ├── TemplateDefinitionParserTests.cs
    ├── TemplateDefinitionEnumeratorTests.cs
    └── TemplateDefinitionParserStateTests.cs
```

## Technical Considerations

### Dependencies
- **Internal Dependencies**: Update all internal references to use new class names and namespaces
- **No External Dependencies**: This change is purely internal to the library
- **Project Files**: Update .csproj files to reflect new file locations

### Implementation Approach
1. **Create New Structure**: Create new folders and namespaces first
2. **Copy and Rename**: Copy existing classes to new locations with new names
3. **Update References**: Update all internal references to use new names
4. **Move Tests**: Reorganize test files to match new structure
5. **Remove Old Files**: Delete original files after verification
6. **Verify Build**: Ensure all projects build successfully

### Risk Mitigation
- **Comprehensive Testing**: Run full test suite after each major change
- **Incremental Changes**: Make changes in small, verifiable steps
- **Build Verification**: Verify build success after each namespace/folder change
- **Public API Testing**: Ensure public API remains unchanged through testing

## Success Metrics

### Primary Success Criteria
- **Build Success**: All projects build without errors or warnings
- **Test Pass Rate**: 100% of existing tests pass after refactoring
- **Public API Stability**: No changes to public interfaces or classes
- **Code Organization**: Clear separation between compilation and execution concerns

### Secondary Success Criteria
- **Improved Maintainability**: Easier to locate and modify parsing-related code
- **Enhanced Readability**: Clear, descriptive class names that convey purpose
- **Consistent Structure**: Uniform namespace and folder organization
- **Documentation Quality**: Updated XML documentation for all renamed classes

## Open Questions

1. **Enum Naming**: `FlatTokenParserState` will be renamed to `TemplateDefinitionParserState` for consistency.

2. **Parser Class Naming**: `TokenDefinitionParser` will be renamed to `TemplateDefinitionParser` since it creates `TemplateDefinition` objects.

3. **Namespace Granularity**: Should we consider further sub-namespaces like `Tokens.Compilation.Parsing.Enumerators` for the enumerator class?

4. **Documentation Updates**: Should we update any external documentation or README files to reflect the new internal structure?

5. **Code Analysis**: Should we run any static analysis tools to verify the refactoring doesn't introduce code quality issues?

---

**Target Audience**: Junior to mid-level developers working on the Tokenizer library
**Estimated Effort**: 2-3 days for a single developer
**Priority**: Medium (improves maintainability but not critical functionality)
