# TokenizeResult Simplification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Simplify the tokenization result API — `Assign<T>()` returns `T`, `Tokenize<T>()` returns `T?`, `TokenMatcher` becomes `TemplateMatcher`, all generic result wrappers deleted.

**Architecture:** Two-stage pipeline: Stage 1 (`Tokenize()`) returns `TokenizeResult` with matches. Stage 2 (`Assign<T>()`) projects matches onto a typed object via reflection, returning `T` directly. All generic wrapper types (`TokenizeResult<T>`, `TokenMatcherResult<T>`) are eliminated.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 8.0 + .NET 10.0, xUnit, NSubstitute

## Global Constraints

- Targets .NET Standard 2.0, .NET 8.0, .NET 10.0
- Root namespace: `Tokens`
- `LangVersion=latest`, nullable reference types enabled
- TDD: write failing test first, then implement
- Allman brace style, `_camelCase` private fields
- Never use `#region`
- Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
- Build: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`

---

### Task 1: Create AssignmentFailedException

**Files:**
- Create: `src/Tokenizer/Exceptions/AssignmentFailedException.cs`
- Create: `tests/Tokenizer.Tests/Exceptions/AssignmentFailedExceptionTests.cs`

**Interfaces:**
- Consumes: `TokenizerException` base class from `src/Tokenizer/Exceptions/TokenizerException.cs`
- Produces: `AssignmentFailedException(string message, IReadOnlyList<Exception> errors)` with `IReadOnlyList<Exception> Errors` property — used by Task 5's `Assign<T>()`

- [ ] **Step 1: Write the failing test**

In `tests/Tokenizer.Tests/Exceptions/AssignmentFailedExceptionTests.cs`:

```csharp
using Tokens.Exceptions;
using Xunit;

namespace Tokens;

public class AssignmentFailedExceptionTests
{
    [Fact]
    public void GivenMessageAndErrors_WhenConstructed_ThenErrorsPropertyIsSet()
    {
        // Arrange
        var inner = new List<Exception>
        {
            new InvalidOperationException("first"),
            new ArgumentException("second"),
        };

        // Act
        var exception = new AssignmentFailedException("Assignment failed", inner);

        // Assert
        Assert.Equal("Assignment failed", exception.Message);
        Assert.Equal(2, exception.Errors.Count);
        Assert.IsType<InvalidOperationException>(exception.Errors[0]);
        Assert.IsType<ArgumentException>(exception.Errors[1]);
    }

    [Fact]
    public void GivenAssignmentFailedException_WhenChecked_ThenIsTokenizerException()
    {
        // Arrange & Act
        var exception = new AssignmentFailedException("test", new List<Exception>());

        // Assert
        Assert.IsAssignableFrom<TokenizerException>(exception);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "AssignmentFailedExceptionTests"`
Expected: Build failure — `AssignmentFailedException` does not exist

- [ ] **Step 3: Write minimal implementation**

In `src/Tokenizer/Exceptions/AssignmentFailedException.cs`:

```csharp
namespace Tokens.Exceptions;

/// <summary>
/// Thrown when one or more errors occur while assigning matched token values
/// to the target object's properties.
/// </summary>
public sealed class AssignmentFailedException : TokenizerException
{
    /// <summary>
    /// Initializes a new instance with a message and the individual errors that occurred.
    /// </summary>
    /// <param name="message">A summary message describing the failure.</param>
    /// <param name="errors">The individual exceptions encountered during assignment.</param>
    public AssignmentFailedException(string message, IReadOnlyList<Exception> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>
    /// The individual exceptions that occurred during assignment.
    /// </summary>
    public IReadOnlyList<Exception> Errors { get; }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "AssignmentFailedExceptionTests"`
Expected: 2 tests PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All 1365+ tests PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Exceptions/AssignmentFailedException.cs tests/Tokenizer.Tests/Exceptions/AssignmentFailedExceptionTests.cs
git commit -m "feat: add AssignmentFailedException for aggregate assignment errors"
```

---

### Task 2: Remove query methods from TokenizeResult

**Files:**
- Modify: `src/Tokenizer/TokenizeResult.cs` — remove `First()`, `First<T>()`, `FirstOrDefault()`, `FirstOrDefault<T>()`, `All()`, `Contains()`
- Modify: `tests/Tokenizer.Tests/TokenizerTests.cs:670-677` — replace `First<DateTime>()` with LINQ on `Matches`
- Modify: `tests/Tokenizer.Tests/TokenMatcherTests.cs:219,220,265,266` — replace `First()` with LINQ on `Matches`
- Modify: `tests/Tokenizer.Tests/SampleTests.cs:417-418` — replace `First()` with LINQ on `Matches`
- Modify: `tests/Tokenizer.Tests/ImmutableCollectionsTests.cs:128-135` — delete the `All()` return-type test

**Interfaces:**
- Consumes: `TokenizeResult.Matches` (`IReadOnlyList<TokenMatch>`)
- Produces: Nothing new — removes methods only

- [ ] **Step 1: Update test call sites to use Matches with LINQ**

In `tests/Tokenizer.Tests/TokenizerTests.cs`, replace the last test (lines 662-678):

```csharp
    [Fact]
    public void GivenPatternWithMultipleOptionalDateTokens_WhenOneMatches_ThenReturnsSingleMatch()
    {
        // Arrange
        const string pattern = @"Date: { Date? : ToDateTime('dd MMM yyyy') }Date: { Date? : ToDateTime('yyyy-MM-dd') }";
        const string input = "Date: 2001-01-01";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);
        var date = (DateTime)result.Matches.First(m => string.Equals(m.Token.Name, "Date", StringComparison.Ordinal)).Value;

        // Assert
        Assert.Equal(new DateTime(2001, 1, 1), date);
        Assert.Single(result.Matches);
    }
```

In `tests/Tokenizer.Tests/TokenMatcherTests.cs`, replace `First()` calls:

Line 219: `Assert.Equal("Alice", match.First("Name"));` →
```csharp
        Assert.Equal("Alice", match.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
```

Line 220: `Assert.Equal("30", match.First("Age"));` →
```csharp
        Assert.Equal("30", match.Matches.First(m => string.Equals(m.Token.Name, "Age", StringComparison.Ordinal)).Value);
```

Line 265: `Assert.Equal("template1", match.Template.Name);` — this line is fine.

Line 265-266: `Assert.Equal("Alice", match.First("Name"));` and `Assert.Equal("30", match.First("Age"));` →
```csharp
        Assert.Equal("Alice", match.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
        Assert.Equal("30", match.Matches.First(m => string.Equals(m.Token.Name, "Age", StringComparison.Ordinal)).Value);
```

In `tests/Tokenizer.Tests/SampleTests.cs`, replace lines 417-418:

```csharp
        Assert.Equal("u34jedzcq.co.ca", match.BestMatch!.Matches.First(m => string.Equals(m.Token.Name, "DomainName", StringComparison.Ordinal)).Value);
        Assert.Equal("NotFound", match.BestMatch.Matches.First(m => string.Equals(m.Token.Name, "Status", StringComparison.Ordinal)).Value);
```

In `tests/Tokenizer.Tests/ImmutableCollectionsTests.cs`, delete the test `GivenTokenizeResult_WhenCallingAll_ThenReturnTypeIsIReadOnlyList` (lines 128-135).

- [ ] **Step 2: Remove query methods from TokenizeResult**

In `src/Tokenizer/TokenizeResult.cs`, delete these methods (keeping only `Matches` property and `Assign<T>()`):
- `First(string key)` (lines 31-40)
- `First<T>(string key)` (lines 50-59)
- `FirstOrDefault(string key)` (lines 67-76)
- `FirstOrDefault<T>(string key)` (lines 85-94)
- `All(string key)` (lines 101-107)
- `Contains(string key)` (lines 114-117)

- [ ] **Step 3: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS (one fewer test due to deleted `All()` reflection test)

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/TokenizeResult.cs tests/Tokenizer.Tests/TokenizerTests.cs tests/Tokenizer.Tests/TokenMatcherTests.cs tests/Tokenizer.Tests/SampleTests.cs tests/Tokenizer.Tests/ImmutableCollectionsTests.cs
git commit -m "refactor: remove query methods from TokenizeResult, callers use Matches directly"
```

---

### Task 3: Remove dictionary assignment path from Assign<T>()

**Files:**
- Modify: `src/Tokenizer/TokenizeResult.cs` — delete `AssignToDictionary()` method and the `IDictionary` branch in `Assign<T>()`
- Modify: `tests/Tokenizer.Tests/TokenizeResultAssignTests.cs` — delete dictionary tests

**Interfaces:**
- Consumes: Nothing new
- Produces: `Assign<T>()` no longer accepts `Dictionary<string, object>` as `T`

- [ ] **Step 1: Delete dictionary tests from TokenizeResultAssignTests**

In `tests/Tokenizer.Tests/TokenizeResultAssignTests.cs`, delete these two test methods:
- `GivenDictionaryTarget_WhenAssign_ThenSetsKeyValues` (lines 145-159)
- `GivenRepeatingTokenWithDictionaryTarget_WhenAssign_ThenBuildsListValue` (lines 162-181)

- [ ] **Step 2: Remove dictionary code from Assign<T>()**

In `src/Tokenizer/TokenizeResult.cs`, modify `Assign<T>()` to remove the dictionary branch:

Replace the current `Assign<T>()` method body (the `if (target is IDictionary...)` / `else` block) with just the object assignment:

```csharp
    public TokenizeResult<T> Assign<T>() where T : class, new()
    {
        var typed = new TokenizeResult<T>(Template, Tokens, Hints, Diagnostics);
        var target = typed.Value;
        var options = Template.Options;

        AssignToObject(target, options, typed);

        return typed;
    }
```

Delete the `AssignToDictionary` method entirely (lines 144-171).

- [ ] **Step 3: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS (two fewer tests)

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/TokenizeResult.cs tests/Tokenizer.Tests/TokenizeResultAssignTests.cs
git commit -m "refactor: remove dictionary assignment path from Assign<T>()"
```

---

### Task 4: Collapse TokenizeResultBase into TokenizeResult

**Files:**
- Modify: `src/Tokenizer/TokenizeResult.cs` — absorb all `TokenizeResultBase` members
- Delete: `src/Tokenizer/TokenizeResultBase.cs`
- Modify (TokenizeResultBase → TokenizeResult in signatures): These files reference `TokenizeResultBase` as a parameter/return type:
  - `src/Tokenizer/Tokenizer.cs:102,126`
  - `src/Tokenizer/TokenMatcher.cs:168-169`
  - `src/Tokenizer/Tokenization/CandidateProcessor.cs:9`
  - `src/Tokenizer/Tokenization/TokenizationSession.cs` (field type)
  - `src/Tokenizer/Tokenization/FrontMatterProcessor.cs:17`
  - `src/Tokenizer/Tokenization/TokenizationEngine.cs`
  - `src/Tokenizer/Tokenization/ITokenizationEngine.cs:15`
  - `src/Tokenizer/Tokenization/IResultBuilder.cs`
  - `src/Tokenizer/Tokenization/ResultBuilder.cs`
  - `src/Tokenizer/Tokenization/IHintStrategy.cs`
  - `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs`
  - `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs`
- Modify: `tests/Tokenizer.Tests/ImmutableCollectionsTests.cs:48-55` — change `typeof(TokenizeResultBase)` to `typeof(TokenizeResult)`
- Modify: `tests/Tokenizer.Tests/Tokenization/TokenizationSessionTests.cs` — update any `TokenizeResultBase` references

**Interfaces:**
- Consumes: Current `TokenizeResultBase` API
- Produces: `TokenizeResult` (non-abstract, sealed) with all members that were on `TokenizeResultBase`. Internal code uses `TokenizeResult` where it previously used `TokenizeResultBase`.

**Important:** `TokenizeResult<T>` still extends `TokenizeResultBase` at this point. Since we're deleting `TokenizeResultBase`, `TokenizeResult<T>` must temporarily extend `TokenizeResult`. This works because `TokenizeResult<T>` only adds `Value` and overrides `Success`.

However, `TokenizeResult` is currently `sealed`. We need to temporarily unseal it so `TokenizeResult<T>` can extend it. This is fine — Task 5 deletes `TokenizeResult<T>` and re-seals `TokenizeResult`.

- [ ] **Step 1: Absorb TokenizeResultBase into TokenizeResult**

In `src/Tokenizer/TokenizeResult.cs`, make `TokenizeResult` non-sealed and add all members from `TokenizeResultBase`:

Replace the class declaration and add all base members. The full resulting `TokenizeResult` class should be:

```csharp
/// <summary>
/// Holds the result of attempting to parse an input string against a
/// <see cref="Template"/>.
/// </summary>
public class TokenizeResult
{
    private readonly List<Exception> _exceptions;

    /// <summary>
    /// Creates a new result bound to the specified <paramref name="template"/>.
    /// </summary>
    public TokenizeResult(Template template)
    {
        _exceptions = new List<Exception>();
        Hints = new HintResult();
        Tokens = new TokenResult();
        Template = template;
    }

    /// <summary>
    /// Creates a projected result carrying forward state from a completed tokenization.
    /// </summary>
    internal TokenizeResult(Template template, TokenResult tokens, HintResult hints, Diagnostics.DiagnosticResult? diagnostics)
    {
        _exceptions = new List<Exception>();
        Template = template;
        Tokens = tokens;
        Hints = hints;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// The template used for the tokenization attempt.
    /// </summary>
    public Template Template { get; init; }

    /// <summary>
    /// A list of any exceptions that occurred during the matching process.
    /// </summary>
    public IReadOnlyList<Exception> Exceptions => _exceptions;

    /// <summary>
    /// The matches that were made during the tokenization process.
    /// </summary>
    public TokenResult Tokens { get; init; }

    /// <summary>
    /// Gets the hints found in the input.
    /// </summary>
    public HintResult Hints { get; init; }

    internal void AddException(Exception exception)
    {
        _exceptions.Add(exception);
    }

    /// <summary>
    /// Structured diagnostic output from the tokenization process.
    /// </summary>
    public Diagnostics.DiagnosticResult? Diagnostics { get; internal set; }

    /// <summary>
    /// Determines whether the matching process was successful.
    /// </summary>
    public virtual bool Success => Tokens.HasMatches &&
                                   !Tokens.HasMissingRequiredTokens &&
                                   !Hints.HasMissingRequiredHints &&
                                   (Template.HasOnlyFrontMatterTokens || Tokens.Matches.Any(m => !m.Token.IsFrontMatterToken));

    /// <summary>
    /// A read-only list of values extracted from the input string.
    /// </summary>
    public IReadOnlyList<TokenMatch> Matches => Tokens.Matches;

    /// <inheritdoc />
    public override string ToString() =>
        $"TokenizeResult('{Template.Name}': {Tokens.Matches.Count} matched, {Tokens.Misses.Count} missed)";

    // ... Assign<T>() and AssignToObject remain unchanged ...
}
```

Keep `Success` as `virtual` for now — `TokenizeResult<T>` still overrides it. Task 5 will make it non-virtual.

Update `TokenizeResult<T>` to extend `TokenizeResult` instead of `TokenizeResultBase`:

```csharp
public sealed class TokenizeResult<T> : TokenizeResult where T : class, new()
```

- [ ] **Step 2: Replace all TokenizeResultBase references with TokenizeResult**

In every file listed above, find-and-replace `TokenizeResultBase` → `TokenizeResult`. This is a mechanical change — the type name changes but the API is identical.

Key files and what to change:
- `src/Tokenizer/Tokenizer.cs`: method parameter types `TokenizeResultBase result` → `TokenizeResult result`
- `src/Tokenizer/TokenMatcher.cs`: lambda parameter types, `MatchCore` generic constraint
- `src/Tokenizer/Tokenization/CandidateProcessor.cs`: `_result` field type
- `src/Tokenizer/Tokenization/TokenizationSession.cs`: `_result` field type  
- `src/Tokenizer/Tokenization/FrontMatterProcessor.cs`: method parameter type
- `src/Tokenizer/Tokenization/TokenizationEngine.cs`: method parameter/return types
- `src/Tokenizer/Tokenization/ITokenizationEngine.cs`: method parameter type
- `src/Tokenizer/Tokenization/IResultBuilder.cs`: method parameter type
- `src/Tokenizer/Tokenization/ResultBuilder.cs`: method parameter type
- `src/Tokenizer/Tokenization/IHintStrategy.cs`: method parameter type
- `src/Tokenizer/Tokenization/Strategies/ContainsHintStrategy.cs`: method parameter type
- `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs`: method parameter type

- [ ] **Step 3: Update test references**

In `tests/Tokenizer.Tests/ImmutableCollectionsTests.cs`, change:
```csharp
var propertyType = typeof(TokenizeResultBase).GetProperty("Exceptions")!.PropertyType;
```
to:
```csharp
var propertyType = typeof(TokenizeResult).GetProperty("Exceptions")!.PropertyType;
```

In `tests/Tokenizer.Tests/Tokenization/TokenizationSessionTests.cs`, update any `TokenizeResultBase` references to `TokenizeResult`.

- [ ] **Step 4: Delete TokenizeResultBase.cs**

```bash
git rm src/Tokenizer/TokenizeResultBase.cs
```

- [ ] **Step 5: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS

- [ ] **Step 6: Commit**

```bash
git add -u
git commit -m "refactor: collapse TokenizeResultBase into TokenizeResult"
```

---

### Task 5: Change Assign<T>() to return T, delete TokenizeResult<T>, update Tokenizer + all consumers

This is the largest task — changing `Assign<T>()`'s return type cascades to every call site.

**Files:**
- Modify: `src/Tokenizer/TokenizeResult.cs` — `Assign<T>()` returns `T`, throws `AssignmentFailedException`, re-seal class, make `Success` non-virtual, delete `TokenizeResult<T>` class, remove projection constructor
- Modify: `src/Tokenizer/ITokenizer.cs` — `Tokenize<T>()` returns `T?`, `TokenizeAsync<T>()` returns `Task<T?>`
- Modify: `src/Tokenizer/Tokenizer.cs` — implement updated signatures
- Modify: `tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs` — delete generic `TokenizeResultBuilder<T>`
- Modify: `tests/Tokenizer.Tests/TokenizeResultAssignTests.cs` — update for `T` return + `AssignmentFailedException`
- Modify: `tests/Tokenizer.Tests/TokenizerTests.cs` — remove `.Value` accesses, use two-stage where result metadata needed
- Modify: `tests/Tokenizer.Tests/MultilineTests.cs` — remove `.Value`
- Modify: `tests/Tokenizer.Tests/ConcatenationTests.cs` — remove `.Value`
- Modify: `tests/Tokenizer.Tests/AllocationOptimizationTests.cs` — remove `.Value`
- Modify: `tests/Tokenizer.Tests/SplitTests.cs` — remove `.Value`
- Modify: `tests/Tokenizer.Tests/Tokenizer.Enum.Tests.cs` — remove `.Value`
- Modify: `tests/Tokenizer.Tests/Tokenizer.Bool.Tests.cs` — remove `.Value`
- Modify: `tests/Tokenizer.Tests/TokenPropertyImmutabilityTests.cs` — remove `.Value`
- Modify: `tests/Tokenizer.Tests/TokenizerOptionsTests.cs` — remove `.Value`
- Modify: `tests/Tokenizer.Tests/HintTests.cs` — remove `.Value`
- Modify: `tests/Tokenizer.Tests/TokenizerAsyncTests.cs` — remove `.Value` from `TokenizeAsync<T>()` call sites
- Modify: `tests/Tokenizer.Tests/Tokenization/ResultBuilder_Basic_Tests.cs` — remove `TokenizeResult<T>` references if any
- Modify: `tests/Tokenizer.Tests/ImmutableCollectionsTests.cs` — delete `TokenMatcherResult<T>` test (line 68-75)

**Interfaces:**
- Consumes: `AssignmentFailedException` from Task 1
- Produces: `Assign<T>()` returns `T` (throws `AssignmentFailedException`). `ITokenizer.Tokenize<T>()` returns `T?`. `ITokenizer.TokenizeAsync<T>()` returns `Task<T?>`.

- [ ] **Step 1: Update TokenizeResultAssignTests for new return type**

In `tests/Tokenizer.Tests/TokenizeResultAssignTests.cs`, rewrite all tests. Key changes:
- `result.Assign<Person>()` now returns `Person` not `TokenizeResult<Person>`
- No `.Value` access — the result IS the Person
- Error cases use `Assert.Throws<AssignmentFailedException>`
- Stage-one exception isolation test changes — no typed result to check

Full replacement for the test class:

```csharp
using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenizeResultAssignTests : TokenizerTestBase
{
    public TokenizeResultAssignTests(ITestOutputHelper output) : base(output)
    {
    }

    public class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public int? Score { get; set; }
    }

    public class PersonSummary
    {
        public string Name { get; set; } = null!;
    }

    [Fact]
    public void GivenMatchesWithStringValue_WhenAssign_ThenPopulatesProperty()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .Build();

        // Act
        var person = result.Assign<Person>();

        // Assert
        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void GivenMatchesWithMultipleProperties_WhenAssign_ThenPopulatesAll()
    {
        // Arrange
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var ageToken = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, ageToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Bob", new FileLocation()),
                new TokenMatch(ageToken, 30, new FileLocation()))
            .Build();

        // Act
        var person = result.Assign<Person>();

        // Assert
        Assert.Equal("Bob", person.Name);
        Assert.Equal(30, person.Age);
    }

    [Fact]
    public void GivenTypeConversionFailure_WhenAssign_ThenThrowsAssignmentFailedException()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "not-a-number", new FileLocation()))
            .Build();

        // Act & Assert
        var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign<Person>());
        Assert.Single(ex.Errors);
        Assert.IsType<TypeConversionException>(ex.Errors[0]);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreEnabled_WhenAssign_ThenReturnsSuccessfully()
    {
        // Arrange
        var token = new TokenBuilder().WithName("NonExistent").Build();
        var options = new TokenizerOptions { IgnoreMissingProperties = true };
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithOptions(options).Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        // Act
        var person = result.Assign<Person>();

        // Assert — no exception thrown, person has default values
        Assert.NotNull(person);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreDisabled_WhenAssign_ThenThrowsAssignmentFailedException()
    {
        // Arrange
        var token = new TokenBuilder().WithName("NonExistent").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        // Act & Assert
        var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign<Person>());
        Assert.Single(ex.Errors);
        Assert.IsType<MissingMemberException>(ex.Errors[0]);
    }

    [Fact]
    public void GivenConcatenatableToken_WhenAssign_ThenSetsValue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        token.CanConcatenate = true;
        token.ConcatenationString = ", ";
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice, Bob", new FileLocation()))
            .Build();

        // Act
        var person = result.Assign<Person>();

        // Assert
        Assert.Equal("Alice, Bob", person.Name);
    }

    [Fact]
    public void GivenResult_WhenAssignCalledTwice_ThenBothSucceed()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .Build();

        // Act
        var first = result.Assign<Person>();
        var second = result.Assign<PersonSummary>();

        // Assert
        Assert.Equal("Alice", first.Name);
        Assert.Equal("Alice", second.Name);
        Assert.NotSame((object)first, second);
    }
}
```

- [ ] **Step 2: Update Assign<T>() to return T and throw AssignmentFailedException**

In `src/Tokenizer/TokenizeResult.cs`:

1. Re-seal the class: `public sealed class TokenizeResult`
2. Make `Success` non-virtual: remove `virtual` keyword
3. Remove the projection constructor (the internal one with tokens/hints/diagnostics params)
4. Delete the entire `TokenizeResult<T>` class at the bottom of the file

Replace `Assign<T>()`:

```csharp
    /// <summary>
    /// Projects matches onto a new instance of <typeparamref name="T"/>,
    /// assigning matched values to the object's properties via reflection.
    /// </summary>
    /// <typeparam name="T">The type to populate with matched values.</typeparam>
    /// <returns>A new instance of <typeparamref name="T"/> with populated properties.</returns>
    /// <exception cref="Exceptions.AssignmentFailedException">
    /// Thrown when one or more matched values cannot be assigned to the target's properties.
    /// </exception>
    public T Assign<T>() where T : class, new()
    {
        var target = new T();
        var options = Template.Options;
        var errors = new List<Exception>();

        foreach (var match in Matches)
        {
            try
            {
                target.SetValue(match.Token.Name, match.Value, StringComparison.Ordinal);
            }
            catch (MissingMemberException)
            {
                if (!options.IgnoreMissingProperties)
                {
                    errors.Add(new MissingMemberException(
                        $"Property '{match.Token.Name}' not found on type '{target.GetType().Name}'."));
                }
            }
            catch (TypeConversionException ex)
            {
                errors.Add(ex);
            }
            catch (TokenAssignmentException ex)
            {
                errors.Add(ex);
            }
            catch (ArgumentException ex)
            {
                errors.Add(ex);
            }
        }

        if (errors.Count > 0)
        {
            throw new AssignmentFailedException(
                $"Failed to assign {errors.Count} value(s) to type '{typeof(T).Name}'.", errors);
        }

        return target;
    }
```

Add required using at top of file: `using Tokens.Exceptions;`

- [ ] **Step 3: Update ITokenizer interface**

In `src/Tokenizer/ITokenizer.cs`, change the typed signatures:

```csharp
    /// <summary>
    /// Tokenizes the input using a pre-compiled template, mapping values onto a new <typeparamref name="T"/>.
    /// Returns null if matching fails.
    /// </summary>
    public T? Tokenize<T>(Template template, string input) where T : class, new();
```

And for each async typed overload, change `Task<TokenizeResult<T>>` to `Task<T?>`:

```csharp
    public Task<T?> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct = default) where T : class, new();
    public Task<T?> TokenizeAsync<T>(Template template, Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new();
```

- [ ] **Step 4: Update Tokenizer implementation**

In `src/Tokenizer/Tokenizer.cs`:

```csharp
    public T? Tokenize<T>(Template template, string input) where T : class, new()
    {
        var result = Tokenize(template, input);
        if (!result.Success) return null;
        return result.Assign<T>();
    }
```

For `TokenizeAsync<T>` with TextReader:
```csharp
    public async Task<T?> TokenizeAsync<T>(Template template, TextReader input, CancellationToken ct = default) where T : class, new()
    {
        var result = await TokenizeAsync(template, input, ct).ConfigureAwait(false);
        if (!result.Success) return null;
        return result.Assign<T>();
    }
```

For `TokenizeAsync<T>` with Stream:
```csharp
    public async Task<T?> TokenizeAsync<T>(Template template, Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new()
    {
        var result = await TokenizeAsync(template, input, encoding, ct).ConfigureAwait(false);
        if (!result.Success) return null;
        return result.Assign<T>();
    }
```

- [ ] **Step 5: Delete generic TokenizeResultBuilder<T>**

In `tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs`, delete the entire `TokenizeResultBuilder<T>` class (lines 69-143). Keep only the non-generic `TokenizeResultBuilder`.

- [ ] **Step 6: Update all test files — remove `.Value` accesses**

This is a mechanical find-and-replace across many test files. The pattern is:

**Simple case** — `_tokenizer.Tokenize<T>(template, input).Value` becomes `_tokenizer.Tokenize<T>(template, input)`:

Files with this pattern (replace `.Tokenize<SomeType>(template, input).Value` with `.Tokenize<SomeType>(template, input)`):
- `tests/Tokenizer.Tests/TokenizerTests.cs` — lines 67, 82, 98, 129, 161, 177, 192, 208, 225, 241, 257, 338, 503, 519, 544, 562, 609
- `tests/Tokenizer.Tests/MultilineTests.cs` — lines containing `.Value`
- `tests/Tokenizer.Tests/ConcatenationTests.cs`
- `tests/Tokenizer.Tests/AllocationOptimizationTests.cs`
- `tests/Tokenizer.Tests/SplitTests.cs`
- `tests/Tokenizer.Tests/Tokenizer.Enum.Tests.cs`
- `tests/Tokenizer.Tests/Tokenizer.Bool.Tests.cs`
- `tests/Tokenizer.Tests/TokenPropertyImmutabilityTests.cs`
- `tests/Tokenizer.Tests/TokenizerOptionsTests.cs`
- `tests/Tokenizer.Tests/HintTests.cs`
- `tests/Tokenizer.Tests/TokenizerAsyncTests.cs`
- `tests/Tokenizer.Tests/TokenizerOptionsTests.cs`

**Two-stage case** — tests that access BOTH `.Value` AND result metadata (`.Success`, `.Tokens.Misses`, etc.). These need to use the two-stage pattern:

In `tests/Tokenizer.Tests/TokenizerTests.cs`:

Lines 273-281 (optional token test):
```csharp
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);
        var student = result.Assign<Student>();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", student.FirstName);
        Assert.Equal("Smith", student.LastName);
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Student.MiddleName", result.Tokens.Misses[0].Name);
```

Lines 293-302 (optional token with validator):
```csharp
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);
        var student = result.Assign<Student>();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", student.FirstName);
        Assert.Equal("Smith", student.LastName);
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Student.Enrolled", result.Tokens.Misses[0].Name);
```

Lines 317-326 (optional token with failing transformer):
```csharp
        var template = blowsUpTokenizer.Compile(pattern).Template;
        var result = blowsUpTokenizer.Tokenize(template, input);
        var student = result.Assign<Student>();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", student.FirstName);
        Assert.Equal("Smith", student.LastName);
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Student.Enrolled", result.Tokens.Misses[0].Name);
```

Lines 357-368 (required token missing):
```csharp
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);
        var student = result.Assign<Student>();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Bob", student.FirstName);
        Assert.Equal("Smith", student.LastName);
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Student.MiddleName", result.Tokens.Misses[0].Name);
```

Lines 388-394 (token not present — `Tokenize<T>` returns null):
```csharp
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);

        // Assert
        Assert.Null(result);
```

Lines 465-477 (missing property test — `IgnoreMissingProperties` defaults to `false`, so `Assign<T>()` will now throw `AssignmentFailedException`). Rewrite as:
```csharp
    [Fact]
    public void GivenPatternWithMissingProperty_WhenTokenizing_ThenThrowsAssignmentFailedException()
    {
        // Arrange
        const string pattern = "Hello {TestClass.MissingPropertyName}";
        const string input = "Hello World";

        // Act
        var template = _tokenizer.Compile(pattern).Template;

        // Assert
        Assert.Throws<AssignmentFailedException>(() => _tokenizer.Tokenize<TestClass>(template, input));
    }
```
Add `using Tokens.Exceptions;` to the file's usings if not already present.

Lines 654-660 (IgnoreMissingProperties test — uses two-stage):
```csharp
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);
        var student = result.Assign<Student>();

        // Assert
        Assert.Equal("John", student.FirstName);
        Assert.Equal("Smith", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Foo", StringComparison.Ordinal)).Value);
```

- [ ] **Step 7: Update ImmutableCollectionsTests**

Delete the `GivenGenericTokenMatcherResult_WhenAccessingResults_ThenPropertyTypeIsIReadOnlyList` test (lines 68-75) — `TokenMatcherResult<T>` no longer exists.

- [ ] **Step 8: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS

- [ ] **Step 9: Commit**

```bash
git add -u
git commit -m "feat: Assign<T>() returns T, Tokenize<T>() returns T?, delete TokenizeResult<T>"
```

---

### Task 6: Rename TokenMatcher → TemplateMatcher, Match → Tokenize, simplify internals

**Files:**
- Rename: `src/Tokenizer/TokenMatcher.cs` → `src/Tokenizer/TemplateMatcher.cs`
- Rename: `src/Tokenizer/ITokenMatcher.cs` → `src/Tokenizer/ITemplateMatcher.cs`
- Rename: `src/Tokenizer/TokenMatcherResult.cs` → `src/Tokenizer/TemplateMatchResult.cs`
- Rename: `src/Tokenizer/Exceptions/TokenMatcherException.cs` → `src/Tokenizer/Exceptions/TemplateMatcherException.cs`
- Modify: `src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs` — update DI registrations
- Rename: `tests/Tokenizer.Tests/TokenMatcherTests.cs` → `tests/Tokenizer.Tests/TemplateMatcherTests.cs`
- Rename: `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs` → `tests/Tokenizer.Tests/TemplateMatcherAsyncTests.cs`
- Rename: `tests/Tokenizer.Tests/TokenMatcherResultTests.cs` → `tests/Tokenizer.Tests/TemplateMatchResultTests.cs`
- Rename: `tests/Tokenizer.Tests/Exceptions/TokenMatcherExceptionTests.cs` → `tests/Tokenizer.Tests/Exceptions/TemplateMatcherExceptionTests.cs`
- Modify: `tests/Tokenizer.Tests/SealedClassTests.cs` — update type references
- Modify: `tests/Tokenizer.Tests/SampleTests.cs` — update matcher usage

**Interfaces:**
- Consumes: `TokenizeResult` with `Assign<T>()` from Task 5
- Produces: `ITemplateMatcher`, `TemplateMatcher`, `TemplateMatchResult` (non-generic), `TemplateMatcherException`. `Tokenize()` returns `TemplateMatchResult`, `Tokenize<T>()` returns `T?`.

- [ ] **Step 1: Rename files with git mv**

```bash
cd /Users/work/Source/tokenizer
git mv src/Tokenizer/TokenMatcher.cs src/Tokenizer/TemplateMatcher.cs
git mv src/Tokenizer/ITokenMatcher.cs src/Tokenizer/ITemplateMatcher.cs
git mv src/Tokenizer/TokenMatcherResult.cs src/Tokenizer/TemplateMatchResult.cs
git mv src/Tokenizer/Exceptions/TokenMatcherException.cs src/Tokenizer/Exceptions/TemplateMatcherException.cs
git mv tests/Tokenizer.Tests/TokenMatcherTests.cs tests/Tokenizer.Tests/TemplateMatcherTests.cs
git mv tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs tests/Tokenizer.Tests/TemplateMatcherAsyncTests.cs
git mv tests/Tokenizer.Tests/TokenMatcherResultTests.cs tests/Tokenizer.Tests/TemplateMatchResultTests.cs
git mv tests/Tokenizer.Tests/Exceptions/TokenMatcherExceptionTests.cs tests/Tokenizer.Tests/Exceptions/TemplateMatcherExceptionTests.cs
```

- [ ] **Step 2: Rename exception class**

In `src/Tokenizer/Exceptions/TemplateMatcherException.cs`:
- Rename class `TokenMatcherException` → `TemplateMatcherException`
- Update constructor names

In `tests/Tokenizer.Tests/Exceptions/TemplateMatcherExceptionTests.cs`:
- Rename test class `TokenMatcherExceptionTests` → `TemplateMatcherExceptionTests`
- Replace `TokenMatcherException` with `TemplateMatcherException` in test body

- [ ] **Step 3: Rewrite TemplateMatchResult (non-generic only)**

In `src/Tokenizer/TemplateMatchResult.cs`, replace entire contents:

```csharp
namespace Tokens;

/// <summary>
/// Contains the result of running a tokenization against multiple registered
/// templates with the <see cref="TemplateMatcher"/>.
/// </summary>
public sealed class TemplateMatchResult
{
    private readonly List<TokenizeResult> _results;

    /// <summary>
    /// Initializes a new, empty <see cref="TemplateMatchResult"/>.
    /// </summary>
    public TemplateMatchResult()
    {
        _results = new List<TokenizeResult>();
    }

    /// <summary>
    /// Contains the result of processing each template against the input text.
    /// </summary>
    public IReadOnlyList<TokenizeResult> Results => _results;

    /// <summary>
    /// Returns the best matching result.
    /// </summary>
    public TokenizeResult? BestMatch { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether any template produced a successful match.
    /// </summary>
    public bool Success => BestMatch != null;

    internal void AddResult(TokenizeResult result)
    {
        _results.Add(result);
    }

    internal TokenizeResult? GetBestMatch() => _results
        .Where(r => r.Success)
        .OrderByDescending(r => r.Hints.Matches.Count)
        .ThenByDescending(r => r.Tokens.Matches.Count)
        .ThenBy(r => r.Template.Tokens.Count)
        .ThenBy(r => r.Template.Id)
        .FirstOrDefault();
}
```

- [ ] **Step 4: Rewrite ITemplateMatcher interface**

In `src/Tokenizer/ITemplateMatcher.cs`, rename all types and change `Match`/`MatchAsync` → `Tokenize`/`TokenizeAsync`. Remove all generic `<T>` overloads that returned `TokenMatcherResult<T>` — the typed `Tokenize<T>()` returns `T?`:

```csharp
using System.Text;

namespace Tokens;

/// <summary>
/// Matches input text against multiple registered templates and returns the best match.
/// </summary>
public interface ITemplateMatcher
{
    /// <summary>
    /// The collection of templates that will be matched against input strings.
    /// </summary>
    public TemplateCollection Templates { get; }

    /// <summary>
    /// Compiles and registers a template pattern string.
    /// </summary>
    public ITemplateMatcher RegisterTemplate(string content);

    /// <summary>
    /// Compiles and registers a template pattern string with an explicit name.
    /// </summary>
    public ITemplateMatcher RegisterTemplate(string content, string name);

    /// <summary>
    /// Registers a pre-compiled template.
    /// </summary>
    public ITemplateMatcher RegisterTemplate(Template template);

    /// <summary>
    /// Tokenizes the input against all registered templates and returns the results.
    /// </summary>
    public TemplateMatchResult Tokenize(string input);

    /// <summary>
    /// Tokenizes the input against registered templates filtered by tags.
    /// </summary>
    public TemplateMatchResult Tokenize(string input, string[]? tags);

    /// <summary>
    /// Tokenizes the input against all registered templates, returning the best match assigned to a new <typeparamref name="T"/>.
    /// Returns null if no template matched.
    /// </summary>
    public T? Tokenize<T>(string input) where T : class, new();

    /// <summary>
    /// Tokenizes the input against registered templates filtered by tags, returning the best match assigned to a new <typeparamref name="T"/>.
    /// Returns null if no template matched.
    /// </summary>
    public T? Tokenize<T>(string input, string[]? tags) where T : class, new();

    /// <summary>
    /// Compiles and registers a template read from a <see cref="TextReader"/>.
    /// </summary>
    public Task<ITemplateMatcher> RegisterTemplateAsync(TextReader reader, CancellationToken ct = default);

    /// <summary>
    /// Compiles and registers a template read from a <see cref="Stream"/>.
    /// </summary>
    public Task<ITemplateMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/> against all registered templates.
    /// </summary>
    public Task<TemplateMatchResult> TokenizeAsync(TextReader input, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/> filtered by tags.
    /// </summary>
    public Task<TemplateMatchResult> TokenizeAsync(TextReader input, string[]? tags, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/>, returning the best match assigned to a new <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> TokenizeAsync<T>(TextReader input, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="TextReader"/> filtered by tags, returning the best match assigned to a new <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> TokenizeAsync<T>(TextReader input, string[]? tags, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/> against all registered templates.
    /// </summary>
    public Task<TemplateMatchResult> TokenizeAsync(Stream input, Encoding encoding, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/> filtered by tags.
    /// </summary>
    public Task<TemplateMatchResult> TokenizeAsync(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/>, returning the best match assigned to a new <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> TokenizeAsync<T>(Stream input, Encoding encoding, CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Asynchronously tokenizes input from a <see cref="Stream"/> filtered by tags, returning the best match assigned to a new <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> TokenizeAsync<T>(Stream input, Encoding encoding, string[]? tags, CancellationToken ct = default) where T : class, new();
}
```

- [ ] **Step 5: Rewrite TemplateMatcher implementation**

In `src/Tokenizer/TemplateMatcher.cs`:

1. Rename class `TokenMatcher` → `TemplateMatcher`
2. Replace `ITokenMatcher` with `ITemplateMatcher` everywhere
3. Replace `TokenMatcherException` with `TemplateMatcherException` everywhere
4. Replace `TokenMatcherResult` with `TemplateMatchResult`
5. Rename `Match`/`MatchAsync` methods → `Tokenize`/`TokenizeAsync`
6. Add typed `Tokenize<T>()` that delegates to untyped + Assign:

For sync `Tokenize`:
```csharp
    public TemplateMatchResult Tokenize(string input)
    {
        return Tokenize(input, tags: null);
    }

    public TemplateMatchResult Tokenize(string input, string[]? tags)
    {
        var results = new TemplateMatchResult();
        tags ??= Array.Empty<string>();

        foreach (var template in Templates)
        {
            if (!CheckTemplateTags(template, tags)) continue;

            try
            {
                var result = _tokenizer.Tokenize(template, input);
                results.AddResult(result);
            }
            catch (Exception e)
            {
                var exception = new TemplateMatcherException(e.Message, template, e);
                _log.LogError(e, "Error processing template: {TemplateName}", template.Name);
                throw exception;
            }
        }

        results.BestMatch = results.GetBestMatch();
        return results;
    }

    public T? Tokenize<T>(string input) where T : class, new()
    {
        return Tokenize<T>(input, tags: null);
    }

    public T? Tokenize<T>(string input, string[]? tags) where T : class, new()
    {
        var results = Tokenize(input, tags);
        if (results.BestMatch == null) return null;
        return results.BestMatch.Assign<T>();
    }
```

7. Remove the generic `MatchCore` method. The sync untyped `Tokenize` implementation above replaces it.

8. For async methods, follow the same pattern. The untyped async methods run Stage 1 on all templates. The typed async methods delegate to untyped + Assign:

```csharp
    public async Task<T?> TokenizeAsync<T>(TextReader input, CancellationToken ct = default) where T : class, new()
        => await TokenizeAsync<T>(input, tags: null, ct).ConfigureAwait(false);

    public async Task<T?> TokenizeAsync<T>(TextReader input, string[]? tags, CancellationToken ct = default) where T : class, new()
    {
        var results = await TokenizeAsync(input, tags, ct).ConfigureAwait(false);
        if (results.BestMatch == null) return null;
        return results.BestMatch.Assign<T>();
    }
```

Same for Stream overloads.

9. The untyped async methods (`TokenizeAsync` returning `TemplateMatchResult`) keep the existing buffering/seeking logic but use `_tokenizer.TokenizeAsync(template, reader, ct)` (the untyped overload) instead of the generic one. Replace the generic `MatchAsyncFromSeekableStream` with a non-generic version.

10. Remove `RegisterTemplate` return type `ITokenMatcher` → `ITemplateMatcher`.

- [ ] **Step 5b: Update XML doc references in Template.cs**

In `src/Tokenizer/Template.cs`, update `<see cref="TokenMatcher"/>` references in XML doc comments to `<see cref="TemplateMatcher"/>` (lines 39 and 46).

- [ ] **Step 6: Update DI registrations**

In `src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs`:

```csharp
        services.TryAddSingleton<ITemplateMatcher>(sp =>
        {
            var tokenizer = sp.GetRequiredService<ITokenizer>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new TemplateMatcher(tokenizer, loggerFactory);
        });
```

- [ ] **Step 7: Update SealedClassTests**

In `tests/Tokenizer.Tests/SealedClassTests.cs`:
- Replace `typeof(TokenMatcherResult)` with `typeof(TemplateMatchResult)`
- Replace `typeof(TokenMatcher)` with `typeof(TemplateMatcher)`

- [ ] **Step 8: Update ImmutableCollectionsTests**

In `tests/Tokenizer.Tests/ImmutableCollectionsTests.cs`:
- Update `typeof(TokenMatcherResult)` → `typeof(TemplateMatchResult)` in the `GivenTokenMatcherResult_WhenAccessingResults_ThenPropertyTypeIsIReadOnlyList` test
- Rename the test method to `GivenTemplateMatchResult_...`

- [ ] **Step 9: Rewrite TemplateMatcherTests**

In `tests/Tokenizer.Tests/TemplateMatcherTests.cs`:
- Rename class to `TemplateMatcherTests`
- Replace `ITokenMatcher` → `ITemplateMatcher`
- Replace `new TokenMatcher()` → `new TemplateMatcher()`
- Replace all `.Match<Person>(...)` → `.Tokenize<Person>(...)` (returns `T?` now, not `TokenMatcherResult<T>`)
- Replace all `.Match(...)` → `.Tokenize(...)` (returns `TemplateMatchResult`)

For typed tests (previously accessed `result.BestMatch!.Value`), the new pattern is just:
```csharp
var person = _matcher.Tokenize<Person>("Name: Alice");
Assert.Equal("Alice", person!.Name);
```

For tests that need BOTH the typed value AND template metadata (e.g., checking `match.Template.Name`), use the two-stage pattern:
```csharp
var result = _matcher.Tokenize("Name: Alice, Age: 30");
var match = result.BestMatch!;
var person = match.Assign<Person>();
Assert.Equal("Alice", person.Name);
Assert.Equal("with-age", match.Template.Name);
```

- [ ] **Step 10: Rewrite TemplateMatcherAsyncTests**

Same pattern as Step 9 but for async methods. Replace `MatchAsync` → `TokenizeAsync`, update result access patterns.

- [ ] **Step 11: Update TemplateMatchResultTests**

In `tests/Tokenizer.Tests/TemplateMatchResultTests.cs`:
- Update class name to `TemplateMatchResultTests`
- Replace `TokenMatcher` → `TemplateMatcher`
- Replace `.Match(...)` → `.Tokenize(...)`
- Result type is now `TemplateMatchResult`

- [ ] **Step 12: Update SampleTests**

In `tests/Tokenizer.Tests/SampleTests.cs`:
- Replace `new TokenMatcher()` → `new TemplateMatcher()`
- Replace `.Match(...)` → `.Tokenize(...)` (line 415)

- [ ] **Step 13: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS

- [ ] **Step 14: Commit**

```bash
git add -u
git commit -m "refactor: rename TokenMatcher to TemplateMatcher, Match to Tokenize, simplify to non-generic"
```

---

### Task 7: Update benchmarks

**Files:**
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/TokenizationBenchmarks.cs`
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/AsyncTokenizationBenchmarks.cs`
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/CompilationCacheBenchmarks.cs`
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/MatcherBenchmarks.cs`
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/AsyncMatcherBenchmarks.cs`
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/ConcurrencyBenchmarks.cs`

**Interfaces:**
- Consumes: All API changes from Tasks 1-6

- [ ] **Step 1: Update tokenization benchmarks**

In `TokenizationBenchmarks.cs`, `CompilationCacheBenchmarks.cs`, `AsyncTokenizationBenchmarks.cs`:
- `Tokenize<T>()` now returns `T?` not `TokenizeResult<T>` — no `.Value` access needed
- Return types of benchmark methods may need to change from `TokenizeResult<T>` to `T?`

In `AsyncTokenizationBenchmarks.cs`:
```csharp
    // Was: => _tokenizer.Tokenize<MediumRecord>(_mediumTemplate, _mediumInput);
    // Now returns MediumRecord? directly
```

- [ ] **Step 2: Update matcher benchmarks**

In `MatcherBenchmarks.cs`, `AsyncMatcherBenchmarks.cs`, `ConcurrencyBenchmarks.cs`:
- Replace `TokenMatcher` → `TemplateMatcher`
- Replace `TokenMatcherResult<T>` → `T?` (for typed methods) or `TemplateMatchResult` (for untyped)
- Replace `.Match<T>(...)` → `.Tokenize<T>(...)`
- Replace `.MatchAsync<T>(...)` → `.TokenizeAsync<T>(...)`
- Replace `.Match(...)` → `.Tokenize(...)`

- [ ] **Step 3: Build benchmarks**

Run: `dotnet build ./benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add -u
git commit -m "chore: update benchmarks for simplified result API"
```
