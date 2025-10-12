# Task List: Tokenizer Service-Based Decomposition Refactoring

## Relevant Files

- `src/Tokenizer/Tokenizer.cs` - Main Tokenizer class that needs refactoring (currently 388 lines)
- `src/Tokenizer/Tokenization/ITokenizationEngine.cs` - Interface for core tokenization logic service
- `src/Tokenizer/Tokenization/TokenizationEngine.cs` - Service containing main tokenization algorithm (lines 79-301)
- `src/Tokenizer/Tokenization/IHintProcessor.cs` - Interface for hint processing service
- `src/Tokenizer/Tokenization/HintProcessor.cs` - Service for hint finding and validation (lines 328-362)
- `src/Tokenizer/Tokenization/IResultBuilder.cs` - Interface for result building service
- `src/Tokenizer/Tokenization/ResultBuilder.cs` - Service for result object creation and population
- `src/Tokenizer/Tokenization/ITokenizationContext.cs` - Interface for shared state management
- `src/Tokenizer/Tokenization/TokenizationContext.cs` - Context object for shared state during tokenization
- `tests/Tokenizer.Tests/Tokenization/TokenizationEngineTests.cs` - Unit tests for TokenizationEngine service
- `tests/Tokenizer.Tests/Tokenization/HintProcessorTests.cs` - Unit tests for HintProcessor service
- `tests/Tokenizer.Tests/Tokenization/ResultBuilderTests.cs` - Unit tests for ResultBuilder service
- `tests/Tokenizer.Tests/Tokenization/TokenizationContextTests.cs` - Unit tests for TokenizationContext
- `tests/Tokenizer.Tests/Tokenization/ServiceIntegrationTests.cs` - Integration tests for service interactions

### Notes

- Unit tests should be placed in the `tests/Tokenizer.Tests/Tokenization/` directory to match the source structure
- Use `dotnet test` to run tests. Running without a path executes all tests found by the test configuration
- All existing tests in `tests/Tokenizer.Tests/TokenizerTests.cs` must continue to pass without modification
- New services follow existing interface patterns (ITokenTransformer, ITokenValidator) with focused, minimal interfaces
- All tokenization-related services and interfaces are grouped in the `Tokenization/` subdirectory for better organization and discoverability

## Tasks

- [x] 1.0 Create Service Interfaces and Contracts
  - [x] 1.1 Create `ITokenizationEngine` interface with method signatures for main tokenization algorithm
  - [x] 1.2 Create `IHintProcessor` interface with methods for hint finding and validation
  - [x] 1.3 Create `IResultBuilder` interface with methods for result object creation and population
  - [x] 1.4 Create `ITokenizationContext` interface for shared state management during tokenization
  - [x] 1.5 Add XML documentation to all interfaces following existing codebase patterns
  - [x] 1.6 Ensure interfaces follow existing patterns (ITokenTransformer, ITokenValidator) with focused, minimal contracts

- [x] 2.0 Extract TokenizationEngine Service
  - [x] 2.1 Create `TokenizationEngine` class implementing `ITokenizationEngine`
  - [x] 2.2 Extract main tokenization algorithm (lines 79-301 from Tokenizer.cs) into the service
  - [x] 2.3 Move candidate token processing and assignment logic to the service
  - [x] 2.4 Move input enumeration and token matching logic to the service
  - [x] 2.5 Move front matter token processing logic to the service
  - [x] 2.6 Add constructor that accepts necessary dependencies (logger, options)
  - [x] 2.7 Ensure service is stateless and thread-safe
  - [x] 2.8 Add comprehensive XML documentation to the service class

- [x] 3.0 Extract HintProcessor Service
  - [x] 3.1 Create `HintProcessor` class implementing `IHintProcessor`
  - [x] 3.2 Extract hint finding and validation logic (lines 328-362 from Tokenizer.cs) into the service
  - [x] 3.3 Move hint matching and missing hint detection logic to the service
  - [x] 3.4 Move enumerator reset logic after hint processing to the service
  - [x] 3.5 Add constructor that accepts necessary dependencies (logger)
  - [x] 3.6 Ensure service is stateless and thread-safe
  - [x] 3.7 Add comprehensive XML documentation to the service class

- [x] 4.0 Extract ResultBuilder Service
  - [x] 4.1 Create `ResultBuilder` class implementing `IResultBuilder`
  - [x] 4.2 Extract result object creation logic from Tokenizer methods
  - [x] 4.3 Move token matches and misses management to the service
  - [x] 4.4 Move exception collection and reporting logic to the service
  - [x] 4.5 Add methods for creating `TokenizeResult` and `TokenizeResult<T>` objects
  - [x] 4.6 Add constructor that accepts necessary dependencies (logger)
  - [x] 4.7 Ensure service is stateless and thread-safe
  - [x] 4.8 Add comprehensive XML documentation to the service class

- [x] 5.0 Create TokenizationContext
  - [x] 5.1 Create `TokenizationContext` class implementing `ITokenizationContext`
  - [x] 5.2 Encapsulate candidate token list, enumerator, and replacement state
  - [x] 5.3 Add properties for match IDs and disabled repeating tokens management
  - [x] 5.4 Add properties for replacement location and StringBuilder state tracking
  - [x] 5.5 Add methods for state initialization and cleanup
  - [x] 5.6 Ensure context is properly initialized and disposed
  - [x] 5.7 Add comprehensive XML documentation to the context class

- [x] 6.0 Refactor Main Tokenizer Class
  - [x] 6.1 Update Tokenizer constructor to create service instances
  - [x] 6.2 Refactor `Tokenize(TokenizeResultBase, object, Template, string)` method to orchestrate services
  - [x] 6.3 Replace extracted logic with service method calls
  - [x] 6.4 Ensure all existing public methods remain unchanged
  - [x] 6.5 Maintain existing error handling and exception collection behavior
  - [x] 6.6 Preserve all existing logging behavior and verbosity levels
  - [x] 6.7 Ensure registration methods (`RegisterTransformer`, `RegisterValidator`) continue to work
  - [x] 6.8 Add XML documentation updates for any modified methods

- [ ] 7.0 Create Comprehensive Unit Tests
  - [ ] 7.1 Create `TokenizationEngineTests` with isolated test scenarios for core tokenization logic
  - [ ] 7.2 Create `HintProcessorTests` for hint processing logic with various hint configurations
  - [ ] 7.3 Create `ResultBuilderTests` for result building functionality with different result types
  - [ ] 7.4 Create `TokenizationContextTests` for state management and context lifecycle
  - [ ] 7.5 Add tests for error handling and exception scenarios in each service
  - [ ] 7.6 Add tests for edge cases and boundary conditions
  - [ ] 7.7 Ensure all new tests achieve >90% code coverage
  - [ ] 7.8 Add tests for service constructor validation and dependency injection

- [ ] 8.0 Integration Testing and Validation
  - [ ] 8.1 Create `ServiceIntegrationTests` to verify service interactions work correctly
  - [ ] 8.2 Run all existing unit tests to ensure they continue to pass without modification
  - [ ] 8.3 Verify no performance regression by running existing performance benchmarks
  - [ ] 8.4 Test all existing public APIs to ensure backward compatibility
  - [ ] 8.5 Validate that all tokenization scenarios work identically to before refactoring
  - [ ] 8.6 Test error handling and exception scenarios end-to-end
  - [ ] 8.7 Verify logging output remains consistent with existing behavior
  - [ ] 8.8 Perform code review to ensure SOLID principles are followed
  - [ ] 8.9 Update any relevant architectural documentation
