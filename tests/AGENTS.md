# Test Suite

Instructions for AI agents working on tests in this project.

## Framework

- xUnit 2.9.3 with `Serilog.Sinks.XUnit` for test output
- NSubstitute for mocks (but never mock the thing you're testing)
- Tests run against .NET 10.0: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

## Naming

Gherkin style: `GivenScenario_WhenAction_ThenResult()`

Examples:
- `GivenEmptyInput_WhenTokenizing_ThenReturnsNoMatches()`
- `GivenOptionalToken_WhenMissing_ThenSkipsToken()`
- `GivenInvalidFormat_WhenParsing_ThenReturnsFalse()`

## File Naming

Test file matches production class: `{ClassName}Tests.cs`

Place test files in the same namespace hierarchy as the production code. For example:
- `src/Tokenizer/Temporal/TemporalParser.cs` -> `tests/Tokenizer.Tests/Temporal/TemporalParserTests.cs`
- `src/Tokenizer/Tokenizer.cs` -> `tests/Tokenizer.Tests/TokenizerTests.cs`

If a single test fixture grows too large, split by scenario: `{ClassName}.{Scenario}.Tests.cs`
- Example: `TokenizeResultAssignTests.cs` covers `TokenizeResult.Assign<T>()`

## Structure

Every test uses Arrange / Act / Assert comments:

```csharp
[Fact]
public void GivenValidInput_WhenTokenizing_ThenExtractsValue()
{
    // Arrange
    var tokenizer = new Tokenizer();
    var pattern = "Name: {Name}";

    // Act
    var result = tokenizer.Tokenize<Person>(pattern, "Name: Alice");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Alice", result.Name);
}
```

## Test Data Builders

Fluent builders live in `tests/Tokenizer.Tests/Builders/`:

- `TokenBuilder` -- builds `Token` instances
- `TemplateBuilder` -- builds `Template` instances with tokens and options
- `TokenizeResultBuilder` -- builds `TokenizeResult` with matches, misses, exceptions
- `HintBuilder` -- builds `Hint` instances

Use builders instead of constructing test objects directly. They handle required fields and default values.

## Mock Setup Helpers

Use `Expect[Object][State]` naming for mock setup methods. Place at the end of the test class:

```csharp
private void ExpectEngineReturnsNoMatches()
{
    _engine.Tokenize(Arg.Any<Template>(), Arg.Any<string>())
        .Returns(new TokenizeResult(template));
}
```

## Running Tests

```bash
# All tests
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj

# Single test class
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemporalParserTests"

# Single test method
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TemporalParserTests.GivenIso8601Value_WhenParsingWithFormat_ThenReturnsDateTimeOffset"
```
