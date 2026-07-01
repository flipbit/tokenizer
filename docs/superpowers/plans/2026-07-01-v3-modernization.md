# Tokenizer v3 Modernization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Modernize the Tokenizer library across all 20 roadmap items, shipping as v3.0.0 with breaking API changes, internal performance improvements, and infrastructure polish.

**Architecture:** Risk-first approach — all breaking API changes first (target frameworks, exception hierarchy, immutable collections, sealed classes, property immutability), then internal improvements (reflection caching, dispose simplification, culture fixes), then infrastructure and polish (CI, build props, cosmetic fixes, docs).

**Tech Stack:** C# / .NET Standard 2.0 + .NET 8.0 + .NET 10.0, xUnit, GitHub Actions

---

## Phase 1: Breaking API Changes

### Task 1: Update target frameworks and version (roadmap items 6, 20)

**Files:**
- Modify: `src/Tokenizer/Tokenizer.csproj`
- Modify: `src/Tokenizer/Extensions/StringExtensions.cs`

- [ ] **Step 1: Write a test that verifies the assembly version is 3.0.0**

```csharp
// In an existing test file or new file: tests/Tokenizer.Tests/VersionTests.cs
using Xunit;

namespace Tokens
{
    public class VersionTests
    {
        [Fact]
        public void GivenAssembly_WhenCheckingVersion_ThenVersionIs3()
        {
            // Arrange
            var assembly = typeof(Tokenizer).Assembly;

            // Act
            var version = assembly.GetName().Version;

            // Assert
            Assert.Equal(3, version!.Major);
            Assert.Equal(0, version.Minor);
            Assert.Equal(0, version.Build);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~VersionTests"`
Expected: FAIL — version is currently 2.3.0

- [ ] **Step 3: Update `Tokenizer.csproj` targets and version**

Change the `<PropertyGroup>` in `src/Tokenizer/Tokenizer.csproj`:

```xml
<TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>
<Version>3.0.0.0</Version>
<PackageVersion>3.0.0</PackageVersion>
```

- [ ] **Step 4: Update conditional compilation in `StringExtensions.cs`**

The `ToMd5` method uses `#if NETSTANDARD2_0` which remains valid. No `#if NET6_0_OR_GREATER` guards exist in the source currently. Grep the entire `src/` directory for `NET6_0` to confirm:

Run: `grep -r "NET6_0" src/`

If any are found, update them to `NET8_0_OR_GREATER`. If none are found, this step is complete.

- [ ] **Step 5: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenizer.csproj src/Tokenizer/Extensions/StringExtensions.cs tests/Tokenizer.Tests/VersionTests.cs
git commit -m "Bump version to 3.0.0, target netstandard2.0/net8.0/net10.0"
```

---

### Task 2: Fix `ValidationException` inheritance (roadmap item 1)

**Files:**
- Modify: `src/Tokenizer/Exceptions/ValidationException.cs`
- Test: `tests/Tokenizer.Tests/Exceptions/ValidationExceptionTests.cs` (create if not exists)

- [ ] **Step 1: Write tests verifying `ValidationException` inherits from `TokenizerException`**

```csharp
// tests/Tokenizer.Tests/Exceptions/ValidationExceptionTests.cs
using System;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Exceptions
{
    public class ValidationExceptionTests
    {
        [Fact]
        public void GivenValidationException_WhenCreated_ThenInheritsFromTokenizerException()
        {
            // Arrange & Act
            var exception = new ValidationException("test");

            // Assert
            Assert.IsAssignableFrom<TokenizerException>(exception);
        }

        [Fact]
        public void GivenValidationException_WhenCaughtAsTokenizerException_ThenIsCaught()
        {
            // Arrange & Act & Assert
            Assert.Throws<TokenizerException>(() =>
            {
                throw new ValidationException("test");
            });
        }

        [Fact]
        public void GivenValidationExceptionWithInner_WhenCreated_ThenPreservesInnerException()
        {
            // Arrange
            var inner = new InvalidOperationException("inner");

            // Act
            var exception = new ValidationException("outer", inner);

            // Assert
            Assert.IsAssignableFrom<TokenizerException>(exception);
            Assert.Same(inner, exception.InnerException);
            Assert.Equal("outer", exception.Message);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~ValidationExceptionTests"`
Expected: FAIL — `ValidationException` currently inherits from `Exception`, not `TokenizerException`

- [ ] **Step 3: Change `ValidationException` base class**

In `src/Tokenizer/Exceptions/ValidationException.cs`, change:

```csharp
public class ValidationException : Exception
```

to:

```csharp
public class ValidationException : TokenizerException
```

No other changes needed — both `Exception` and `TokenizerException` have `(string)` and `(string, Exception)` constructors.

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Exceptions/ValidationException.cs tests/Tokenizer.Tests/Exceptions/ValidationExceptionTests.cs
git commit -m "Fix ValidationException to inherit from TokenizerException"
```

---

### Task 3: Make `Token` properties `internal set` (roadmap item 8)

**Files:**
- Modify: `src/Tokenizer/Token.cs`

- [ ] **Step 1: Write a compilation-verification test**

This is a breaking change for external consumers, but internal code (like `TokenParser`) must still set these properties. Write a test verifying that token properties are set correctly during compilation (which exercises the internal setters):

```csharp
// tests/Tokenizer.Tests/TokenPropertyImmutabilityTests.cs
using Xunit;

namespace Tokens
{
    public class TokenPropertyImmutabilityTests
    {
        [Fact]
        public void GivenTemplate_WhenCompiled_ThenTokenPropertiesAreSet()
        {
            // Arrange
            var tokenizer = Tokenizer.Create();

            // Act
            var result = tokenizer.Tokenize<TestClass>("Name: Alice\nAge: 30", "Name: {TestClass.Name}\nAge: {TestClass.Age}");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Alice", result.Value.Name);
        }

        [Fact]
        public void GivenOptionalToken_WhenCompiled_ThenOptionalIsTrue()
        {
            // Arrange
            var tokenizer = Tokenizer.Create();

            // Act
            var result = tokenizer.Tokenize("Name: Alice", "Name: {Name?}");

            // Assert
            var nameToken = Assert.Single(result.Tokens.Matches, m => m.Token.Name == "Name");
            Assert.True(nameToken.Token.Optional);
        }

        public class TestClass
        {
            public string? Name { get; set; }
            public string? Age { get; set; }
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (baseline)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenPropertyImmutabilityTests"`
Expected: PASS (we're verifying the baseline behavior still works after the change)

- [ ] **Step 3: Change all public setters to `internal set` on `Token`**

In `src/Tokenizer/Token.cs`, change each of these properties:

```csharp
public string Preamble { get; internal set; }
public string Name { get; internal set; }
public bool Optional { get; internal set; }
public bool Repeating { get; internal set; }
public bool TerminateOnNewLine { get; internal set; }
public bool Required { get; internal set; }
public bool IsFrontMatterToken { get; internal set; }
public bool IsNull { get; internal set; }
public FileLocation Location { get; internal set; }
public bool Concatenate { get; internal set; }
public string? ConcatenationString { get; internal set; }
public bool ConsiderOnce { get; internal set; }
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS — internal code and tests (same assembly via `InternalsVisibleTo` or same namespace) should still compile. If test compilation fails because tests set these properties directly, update those tests to use builders or the compilation pipeline instead.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Token.cs tests/Tokenizer.Tests/TokenPropertyImmutabilityTests.cs
git commit -m "Make Token properties internal set for immutability"
```

---

### Task 4: Make exception location properties `internal set` (roadmap item 16)

**Files:**
- Modify: `src/Tokenizer/Exceptions/LexerException.cs`
- Modify: `src/Tokenizer/Exceptions/ParsingException.cs`

- [ ] **Step 1: Write tests verifying location properties are set from constructors**

```csharp
// tests/Tokenizer.Tests/Exceptions/ExceptionLocationTests.cs
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Exceptions
{
    public class ExceptionLocationTests
    {
        [Fact]
        public void GivenLexerException_WhenCreatedWithLocation_ThenLocationPropertiesAreSet()
        {
            // Arrange
            var location = new FileLocation { Line = 5, Column = 10 };

            // Act
            var exception = new LexerException("test error", location);

            // Assert
            Assert.Equal(5, exception.Line);
            Assert.Equal(10, exception.Column);
        }

        [Fact]
        public void GivenParsingException_WhenCreatedWithLocation_ThenLocationPropertiesAreSet()
        {
            // Arrange
            var location = new FileLocation { Line = 3, Column = 7 };

            // Act
            var exception = new ParsingException("test error", location);

            // Assert
            Assert.Equal(3, exception.Line);
            Assert.Equal(7, exception.Column);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (baseline)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~ExceptionLocationTests"`
Expected: PASS

- [ ] **Step 3: Change properties to `internal set`**

In `src/Tokenizer/Exceptions/LexerException.cs`:

```csharp
public int Line { get; internal set; }
public int Column { get; internal set; }
```

In `src/Tokenizer/Exceptions/ParsingException.cs`:

```csharp
public int Line { get; internal set; }
public int Column { get; internal set; }
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Exceptions/LexerException.cs src/Tokenizer/Exceptions/ParsingException.cs tests/Tokenizer.Tests/Exceptions/ExceptionLocationTests.cs
git commit -m "Make exception Line/Column properties internal set"
```

---

### Task 5: Immutable public collections (roadmap item 4)

**Files:**
- Modify: `src/Tokenizer/TokenResult.cs`
- Modify: `src/Tokenizer/TokenizeResultBase.cs`
- Modify: `src/Tokenizer/TokenMatcherResult.cs`
- Modify: `src/Tokenizer/HintResult.cs`
- Modify: `src/Tokenizer/Template.cs`
- Modify: `src/Tokenizer/Token.cs`
- Modify: `src/Tokenizer/TokenDecoratorContext.cs`

This task has many files. Work through them one at a time, running tests after each file change.

- [ ] **Step 1: Write a test verifying collections are `IReadOnlyList<T>`**

```csharp
// tests/Tokenizer.Tests/ImmutableCollectionsTests.cs
using System.Collections.Generic;
using Xunit;

namespace Tokens
{
    public class ImmutableCollectionsTests
    {
        [Fact]
        public void GivenTokenResult_WhenAccessingMatches_ThenReturnsReadOnlyList()
        {
            // Arrange
            var result = new TokenResult();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<Match>>(result.Matches);
        }

        [Fact]
        public void GivenTokenResult_WhenAccessingMisses_ThenReturnsReadOnlyList()
        {
            // Arrange
            var result = new TokenResult();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<Token>>(result.Misses);
        }

        [Fact]
        public void GivenTemplate_WhenAccessingHints_ThenReturnsReadOnlyList()
        {
            // Arrange
            var template = new Template("test");

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<Hint>>(template.Hints);
        }

        [Fact]
        public void GivenTemplate_WhenAccessingTags_ThenReturnsReadOnlyList()
        {
            // Arrange
            var template = new Template("test");

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<string>>(template.Tags);
        }

        [Fact]
        public void GivenHintResult_WhenAccessingMatches_ThenReturnsReadOnlyList()
        {
            // Arrange
            var result = new HintResult();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<HintMatch>>(result.Matches);
        }

        [Fact]
        public void GivenHintResult_WhenAccessingMisses_ThenReturnsReadOnlyList()
        {
            // Arrange
            var result = new HintResult();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<Hint>>(result.Misses);
        }

        [Fact]
        public void GivenTokenizeResultBase_WhenAccessingExceptions_ThenReturnsReadOnlyList()
        {
            // Arrange
            var template = new Template("test");
            var result = new TokenizeResult(template);

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<System.Exception>>(result.Exceptions);
        }

        [Fact]
        public void GivenTokenMatcherResult_WhenAccessingResults_ThenReturnsReadOnlyList()
        {
            // Arrange
            var result = new TokenMatcherResult();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<TokenizeResult>>(result.Results);
        }

        [Fact]
        public void GivenToken_WhenAccessingDecorators_ThenReturnsReadOnlyList()
        {
            // Arrange
            var token = new Token("content", "name", "preamble", new Enumerators.FileLocation());

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<TokenDecoratorContext>>(token.Decorators);
        }

        [Fact]
        public void GivenTokenDecoratorContext_WhenAccessingParameters_ThenReturnsReadOnlyList()
        {
            // Arrange
            var context = new TokenDecoratorContext(typeof(Transformers.ToLowerTransformer));

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<string>>(context.Parameters);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~ImmutableCollectionsTests"`
Expected: FAIL — properties currently return `IList<T>`, not `IReadOnlyList<T>`

- [ ] **Step 3: Update `TokenResult.cs`**

Change the backing fields and properties:

```csharp
public class TokenResult
{
    private readonly List<Match> _matches;
    private readonly List<Token> _misses;

    public TokenResult()
    {
        _matches = new List<Match>();
        _misses = new List<Token>();
    }

    public IReadOnlyList<Match> Matches => _matches;

    public IReadOnlyList<Token> Misses => _misses;

    internal void AddMatch(Token token, object value, FileLocation location)
    {
        if (TryConcatMatch(token, value, location)) return;

        _matches.Add(new Match(token, value, location.Clone()));
    }

    private bool TryConcatMatch(Token token, object value, FileLocation location)
    {
        if (token.Concatenate == false) return false;

        if (_matches.Any(m => m.Token.Name == token.Name) == false) return false;

        var match = _matches.First(m => m.Token.Name == token.Name);

        if (token.CanConcatenate(match.Value, value) == false) return false;

        var concatenated = token.ConcatenateValues(match.Value, value, token.ConcatenationString);
        if (concatenated != null) match.Value = concatenated;

        return true;
    }

    internal void AddMiss(Token token)
    {
        _misses.Add(token);
    }

    public bool HasMissingRequiredTokens => _misses.Any(m => m.Required);

    public bool HasMatches => _matches.Any();
}
```

Run tests: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS (fix any compilation errors from internal code still using `IList<T>` operations like `.Add()`)

- [ ] **Step 4: Update `HintResult.cs`**

```csharp
public class HintResult
{
    private readonly List<HintMatch> _matches;
    private readonly List<Hint> _misses;

    public HintResult()
    {
        _matches = new List<HintMatch>();
        _misses = new List<Hint>();
    }

    public IReadOnlyList<HintMatch> Matches => _matches;

    public IReadOnlyList<Hint> Misses => _misses;

    internal bool AddMatch(Hint hint, TokenEnumerator enumerator)
    {
        if (_matches.Any(m => m.Text == hint.Text)) return false;

        _matches.Add(new HintMatch(hint.Text, hint.Optional, enumerator.Location.Clone()));

        return true;
    }

    internal bool AddMiss(Hint hint)
    {
        if (_misses.Any(m => m.Text == hint.Text) ||
            _matches.Any(m => m.Text == hint.Text)) return false;

        _misses.Add(hint.Clone());

        return true;
    }

    public bool HasMissingRequiredHints => _misses.Any(m => m.Optional == false);
}
```

Run tests: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

- [ ] **Step 5: Update `TokenizeResultBase.cs`**

```csharp
public class TokenizeResultBase
{
    private readonly List<Exception> _exceptions;

    public TokenizeResultBase(Template template)
    {
        _exceptions = new List<Exception>();

        Hints = new HintResult();
        Tokens = new TokenResult();

        Template = template;
    }

    public Template Template { get; init; }

    public IReadOnlyList<Exception> Exceptions => _exceptions;

    public TokenResult Tokens { get; init; }

    public HintResult Hints { get; init; }

    public Diagnostics.TokenizationDiagnostics? Diagnostics { get; internal set; }

    public bool Success => Tokens.HasMatches &&
                           Tokens.HasMissingRequiredTokens == false &&
                           Hints.HasMissingRequiredHints == false &&
                           (Template.HasOnlyFrontMatterTokens || Tokens.Matches.Any(m => !m.Token.IsFrontMatterToken));

    internal void AddException(Exception exception)
    {
        _exceptions.Add(exception);
    }
}
```

After this change, find all places in the codebase that call `.Exceptions.Add(...)` and update them to use the new `AddException` internal method. Search with:

Run: `grep -rn "\.Exceptions\.Add\(" src/`

Update each call site to use `AddException(exception)` instead of `Exceptions.Add(exception)`.

Run tests: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

- [ ] **Step 6: Update `TokenMatcherResult.cs`**

```csharp
public class TokenMatcherResult
{
    private readonly List<TokenizeResult> _results;

    public TokenMatcherResult()
    {
        _results = new List<TokenizeResult>();
    }

    public IReadOnlyList<TokenizeResult> Results => _results;

    public TokenizeResult? BestMatch { get; internal set; }

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
        .ThenBy(r => r.Template.Name)
        .FirstOrDefault();
}

public class TokenMatcherResult<T> where T : class, new()
{
    private readonly List<TokenizeResult<T>> _results;

    public TokenMatcherResult()
    {
        _results = new List<TokenizeResult<T>>();
    }

    public IReadOnlyList<TokenizeResult<T>> Results => _results;

    public TokenizeResult<T>? BestMatch { get; internal set; }

    public bool Success => BestMatch != null;

    internal void AddResult(TokenizeResult<T> result)
    {
        _results.Add(result);
    }

    internal TokenizeResult<T>? GetBestMatch() => _results
        .Where(r => r.Success)
        .OrderByDescending(r => r.Hints.Matches.Count)
        .ThenByDescending(r => r.Tokens.Matches.Count)
        .ThenBy(r => r.Template.Tokens.Count)
        .ThenBy(r => r.Template.Name)
        .FirstOrDefault();
}
```

Update `TokenMatcher.cs` to use `results.AddResult(result)` instead of `results.Results.Add(result)`.

Run tests: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

- [ ] **Step 7: Update `Template.cs`**

Change the `Hints` and `Tags` properties. The constructor already creates `List<Hint>` and `List<string>` backing fields — expose them as `IReadOnlyList<T>`:

```csharp
public class Template
{
    private readonly List<Token> tokens;
    private readonly List<Hint> _hints;
    private readonly List<string> _tags;
    private string name;

    public Template(string content) : this(string.Empty, content)
    {
    }

    public Template(string name, string content)
    {
        tokens = new List<Token>();
        _hints = new List<Hint>();
        _tags = new List<string>();
        Options = new TokenizerOptions();
        this.name = name;
        Content = content;
    }

    // ... (keep other members unchanged)

    public IReadOnlyList<Hint> Hints => _hints;

    public IReadOnlyList<string> Tags => _tags;

    // ... keep existing members ...

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;

        foreach (var candidate in _tags)
        {
            if (string.Compare(candidate, tag, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return true;
            }
        }

        return false;
    }

    // Add internal methods for mutation:
    internal void AddHint(Hint hint)
    {
        _hints.Add(hint);
    }

    internal void AddTag(string tag)
    {
        _tags.Add(tag);
    }
}
```

Update `TokenParser.cs` to use `template.AddHint(hint)` and `template.AddTag(tag)` instead of `template.Hints.Add(hint)` and `template.Tags.Add(tag)`.

Run tests: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

- [ ] **Step 8: Update `Token.cs` — Decorators**

Change the `Decorators` property:

```csharp
private readonly List<TokenDecoratorContext> _decorators;

public Token(string content, string name, string preamble, FileLocation location)
{
    this.content = content;
    Name = name;
    Preamble = preamble;
    Location = location;
    _decorators = new List<TokenDecoratorContext>();
}

public IReadOnlyList<TokenDecoratorContext> Decorators => _decorators;

// Add internal method:
internal void AddDecorator(TokenDecoratorContext decorator)
{
    _decorators.Add(decorator);
}
```

Update `TokenParser.cs` to use `token.AddDecorator(context)` instead of `token.Decorators.Add(context)`.

Run tests: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

- [ ] **Step 9: Update `TokenDecoratorContext.cs` — Parameters**

```csharp
public class TokenDecoratorContext
{
    private readonly List<string> _parameters;

    public TokenDecoratorContext(Type tokenDecorator)
    {
        DecoratorType = tokenDecorator;
        _parameters = new List<string>();
    }

    public IReadOnlyList<string> Parameters => _parameters;

    // Add internal method:
    internal void AddParameter(string parameter)
    {
        _parameters.Add(parameter);
    }

    // Update CanTransform and Validate to use _parameters:
    public bool CanTransform(object value, out object transformed)
    {
        var instance = (ITokenTransformer) CreateDecorator();
        return instance.CanTransform(value, _parameters.ToArray(), out transformed);
    }

    public bool Validate(object value)
    {
        var instance = (ITokenValidator) CreateDecorator();
        if (IsNotValidator)
        {
            return !instance.IsValid(value, _parameters.ToArray());
        }
        return instance.IsValid(value, _parameters.ToArray());
    }
}
```

Update `TokenParser.cs` to use `context.AddParameter(arg)` and `setContext.AddParameter(preToken.Value)` instead of `context.Parameters.Add(...)`.

- [ ] **Step 10: Update `TokenizeResult.cs` — Matches property**

The `TokenizeResult.Matches` property delegates to `Tokens.Matches` which is now `IReadOnlyList<Match>`. Update its return type:

```csharp
public IReadOnlyList<Match> Matches => Tokens.Matches;
```

Also update `All` method:

```csharp
public IReadOnlyList<object> All(string key)
{
    return Matches
        .Where(m => m.Token.Name == key)
        .Select(m => m.Value)
        .ToList();
}
```

- [ ] **Step 11: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 12: Commit**

```bash
git add src/Tokenizer/TokenResult.cs src/Tokenizer/HintResult.cs src/Tokenizer/TokenizeResultBase.cs src/Tokenizer/TokenizeResult.cs src/Tokenizer/TokenMatcherResult.cs src/Tokenizer/Template.cs src/Tokenizer/Token.cs src/Tokenizer/TokenDecoratorContext.cs src/Tokenizer/TokenMatcher.cs src/Tokenizer/Compilation/TokenParser.cs tests/Tokenizer.Tests/ImmutableCollectionsTests.cs
git status
git commit -m "Replace mutable IList<T> with IReadOnlyList<T> on public API"
```

---

### Task 6: Seal public classes (roadmap item 7)

**Files:**
- Modify: `src/Tokenizer/Token.cs`
- Modify: `src/Tokenizer/Template.cs`
- Modify: `src/Tokenizer/Hint.cs`
- Modify: `src/Tokenizer/Match.cs`
- Modify: `src/Tokenizer/TokenizeResult.cs` (both `TokenizeResult` and `TokenizeResult<T>`)
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `src/Tokenizer/HintResult.cs`
- Modify: `src/Tokenizer/TokenResult.cs`
- Modify: `src/Tokenizer/TokenMatcherResult.cs` (both classes)
- Modify: `src/Tokenizer/TokenizeResultBase.cs`
- Modify: `src/Tokenizer/TokenDecoratorContext.cs`
- Modify: All files in `src/Tokenizer/Transformers/` (except `ITokenTransformer.cs`)
- Modify: All files in `src/Tokenizer/Validators/` (except `ITokenValidator.cs`)

- [ ] **Step 1: Write tests verifying classes are sealed**

```csharp
// tests/Tokenizer.Tests/SealedClassTests.cs
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Tokens
{
    public class SealedClassTests
    {
        [Theory]
        [InlineData(typeof(Token))]
        [InlineData(typeof(Template))]
        [InlineData(typeof(Hint))]
        [InlineData(typeof(Match))]
        [InlineData(typeof(TokenizeResult))]
        [InlineData(typeof(TokenizerOptions))]
        [InlineData(typeof(Tokenizer))]
        [InlineData(typeof(HintResult))]
        [InlineData(typeof(TokenResult))]
        [InlineData(typeof(TokenMatcherResult))]
        [InlineData(typeof(TokenDecoratorContext))]
        public void GivenPublicClass_WhenChecked_ThenIsSealed(Type type)
        {
            Assert.True(type.IsSealed, $"{type.Name} should be sealed");
        }

        [Fact]
        public void GivenAllTransformers_WhenChecked_ThenAreSealed()
        {
            // Arrange
            var transformerTypes = typeof(Tokenizer).Assembly
                .GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract)
                .Where(t => typeof(Transformers.ITokenTransformer).IsAssignableFrom(t));

            // Assert
            foreach (var type in transformerTypes)
            {
                Assert.True(type.IsSealed, $"Transformer {type.Name} should be sealed");
            }
        }

        [Fact]
        public void GivenAllValidators_WhenChecked_ThenAreSealed()
        {
            // Arrange
            var validatorTypes = typeof(Tokenizer).Assembly
                .GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract)
                .Where(t => typeof(Validators.ITokenValidator).IsAssignableFrom(t));

            // Assert
            foreach (var type in validatorTypes)
            {
                Assert.True(type.IsSealed, $"Validator {type.Name} should be sealed");
            }
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~SealedClassTests"`
Expected: FAIL

- [ ] **Step 3: Add `sealed` to all target classes**

For each file, add `sealed` to the class declaration. Example pattern:

```csharp
// Before:
public class Token
// After:
public sealed class Token
```

Do this for:
- Core classes: `Token`, `Template`, `Hint`, `Match`, `TokenizeResult`, `TokenizeResult<T>`, `TokenizerOptions`, `Tokenizer`, `HintResult`, `TokenResult`, `TokenMatcherResult`, `TokenMatcherResult<T>`, `TokenizeResultBase`, `TokenDecoratorContext`, `HintMatch`
- All transformer classes in `src/Tokenizer/Transformers/` (15 files, skip `ITokenTransformer.cs`)
- All validator classes in `src/Tokenizer/Validators/` (15 files, skip `ITokenValidator.cs`)

Note: `TokenizationContext` has `protected virtual void Dispose(bool disposing)` — since we're also simplifying that dispose in Phase 2, we can seal it here and will remove `virtual` in Task 10. Change `protected virtual` to `private` when sealing, or defer until Task 10.

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git status
git add -u src/Tokenizer/
git add tests/Tokenizer.Tests/SealedClassTests.cs
git commit -m "Seal all public classes that are not extension points"
```

---

## Phase 2: Internal Improvements

### Task 7: Cache `Activator.CreateInstance` in `TokenDecoratorContext` (roadmap item 2)

**Files:**
- Modify: `src/Tokenizer/TokenDecoratorContext.cs`

- [ ] **Step 1: Write a performance-oriented test verifying caching works**

```csharp
// tests/Tokenizer.Tests/TokenDecoratorContextCachingTests.cs
using System;
using Tokens.Transformers;
using Xunit;

namespace Tokens
{
    public class TokenDecoratorContextCachingTests
    {
        [Fact]
        public void GivenSameDecoratorType_WhenCreatingMultipleDecorators_ThenReturnsSameInstance()
        {
            // Arrange
            var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer));
            var context2 = new TokenDecoratorContext(typeof(ToLowerTransformer));

            // Act
            var decorator1 = context1.CreateDecorator();
            var decorator2 = context2.CreateDecorator();

            // Assert
            Assert.Same(decorator1, decorator2);
        }

        [Fact]
        public void GivenDifferentDecoratorTypes_WhenCreatingDecorators_ThenReturnsDifferentInstances()
        {
            // Arrange
            var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer));
            var context2 = new TokenDecoratorContext(typeof(ToUpperTransformer));

            // Act
            var decorator1 = context1.CreateDecorator();
            var decorator2 = context2.CreateDecorator();

            // Assert
            Assert.NotSame(decorator1, decorator2);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenDecoratorContextCachingTests"`
Expected: FAIL — `CreateDecorator()` currently creates a new instance each time

- [ ] **Step 3: Add caching to `TokenDecoratorContext`**

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens
{
    public sealed class TokenDecoratorContext
    {
        private static readonly ConcurrentDictionary<Type, ITokenDecorator> DecoratorCache = new();

        private readonly List<string> _parameters;

        public TokenDecoratorContext(Type tokenDecorator)
        {
            DecoratorType = tokenDecorator;
            _parameters = new List<string>();
        }

        public Type DecoratorType { get; }

        public ITokenDecorator CreateDecorator()
        {
            return DecoratorCache.GetOrAdd(DecoratorType, type =>
            {
                var instance = Activator.CreateInstance(type)
                    ?? throw new InvalidOperationException($"Failed to create instance of {type.Name}");
                return (ITokenDecorator) instance;
            });
        }

        // ... rest unchanged
    }
}
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/TokenDecoratorContext.cs tests/Tokenizer.Tests/TokenDecoratorContextCachingTests.cs
git commit -m "Cache decorator instances in TokenDecoratorContext"
```

---

### Task 8: Cache `GetType().GetProperties()` in `ObjectExtensions` (roadmap item 3)

**Files:**
- Modify: `src/Tokenizer/Extensions/ObjectExtensions.cs`

- [ ] **Step 1: Write a test verifying reflection caching works correctly**

```csharp
// tests/Tokenizer.Tests/Extensions/ObjectExtensionsPropertyCacheTests.cs
using Tokens.Extensions;
using Xunit;

namespace Tokens.Extensions
{
    public class ObjectExtensionsPropertyCacheTests
    {
        [Fact]
        public void GivenSameType_WhenSettingMultipleProperties_ThenSucceeds()
        {
            // Arrange
            var target1 = new TestTarget();
            var target2 = new TestTarget();

            // Act — second call should use cached properties
            target1.SetValue("Name", "Alice");
            target2.SetValue("Name", "Bob");

            // Assert
            Assert.Equal("Alice", target1.Name);
            Assert.Equal("Bob", target2.Name);
        }

        [Fact]
        public void GivenNestedType_WhenSettingProperty_ThenSucceeds()
        {
            // Arrange
            var target = new TestTarget();

            // Act
            target.SetValue("Inner.Value", "test");

            // Assert
            Assert.NotNull(target.Inner);
            Assert.Equal("test", target.Inner!.Value);
        }

        public class TestTarget
        {
            public string? Name { get; set; }
            public TestInner? Inner { get; set; }
        }

        public class TestInner
        {
            public string? Value { get; set; }
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (baseline)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~ObjectExtensionsPropertyCacheTests"`
Expected: PASS

- [ ] **Step 3: Add property cache to `ObjectExtensions`**

Add at the top of the `ObjectExtensions` class:

```csharp
using System.Collections.Concurrent;
using System.Reflection;

public static class ObjectExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private static PropertyInfo[] GetCachedProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, t => t.GetProperties());
    }
```

Then replace both calls to `@object.GetType().GetProperties()` (in `SetInnerValue` at line 45 and `GetInnerValue` at line 243) with:

```csharp
var propertyInfos = GetCachedProperties(@object.GetType());
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Extensions/ObjectExtensions.cs tests/Tokenizer.Tests/Extensions/ObjectExtensionsPropertyCacheTests.cs
git commit -m "Cache GetProperties() reflection results in ObjectExtensions"
```

---

### Task 9: Culture-invariant transformers (roadmap item 11)

**Files:**
- Modify: `src/Tokenizer/Transformers/ToLowerTransformer.cs`
- Modify: `src/Tokenizer/Transformers/ToUpperTransformer.cs`

- [ ] **Step 1: Write tests verifying culture-invariant behavior**

```csharp
// tests/Tokenizer.Tests/Transformers/CultureInvariantTransformerTests.cs
using Tokens.Transformers;
using Xunit;

namespace Tokens.Transformers
{
    public class CultureInvariantTransformerTests
    {
        [Fact]
        public void GivenToLowerTransformer_WhenTransformingTurkishI_ThenUsesInvariantCulture()
        {
            // Arrange
            var transformer = new ToLowerTransformer();

            // Act
            transformer.CanTransform("TITLE", Array.Empty<string>(), out var result);

            // Assert — invariant lowercase of 'I' is 'i', not Turkish 'ı'
            Assert.Equal("title", result);
        }

        [Fact]
        public void GivenToUpperTransformer_WhenTransformingTurkishI_ThenUsesInvariantCulture()
        {
            // Arrange
            var transformer = new ToUpperTransformer();

            // Act
            transformer.CanTransform("title", Array.Empty<string>(), out var result);

            // Assert — invariant uppercase of 'i' is 'I', not Turkish 'İ'
            Assert.Equal("TITLE", result);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (baseline — they should pass in most locales)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~CultureInvariantTransformerTests"`
Expected: PASS (in most dev environments)

- [ ] **Step 3: Update transformers**

In `src/Tokenizer/Transformers/ToLowerTransformer.cs`, change:

```csharp
transformed = valueString.ToLowerInvariant();
```

In `src/Tokenizer/Transformers/ToUpperTransformer.cs`, change:

```csharp
transformed = valueString.ToUpperInvariant();
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Transformers/ToLowerTransformer.cs src/Tokenizer/Transformers/ToUpperTransformer.cs tests/Tokenizer.Tests/Transformers/CultureInvariantTransformerTests.cs
git commit -m "Use culture-invariant ToLower/ToUpper in transformers"
```

---

### Task 10: Simplify `TokenizationContext` dispose (roadmap item 9)

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationContext.cs`

- [ ] **Step 1: Write a test verifying dispose works**

```csharp
// tests/Tokenizer.Tests/Tokenization/TokenizationContextDisposeTests.cs
using Tokens.Tokenization;
using Xunit;

namespace Tokens.Tokenization
{
    public class TokenizationContextDisposeTests
    {
        [Fact]
        public void GivenTokenizationContext_WhenDisposed_ThenCanBeDisposedAgainWithoutError()
        {
            // Arrange
            var context = new TokenizationContext();
            context.Initialize("test input");

            // Act & Assert — double dispose should not throw
            context.Dispose();
            context.Dispose();
        }

        [Fact]
        public void GivenTokenizationContext_WhenUsedInUsingBlock_ThenDisposesCleanly()
        {
            // Arrange & Act & Assert
            using (var context = new TokenizationContext())
            {
                context.Initialize("test input");
            }
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (baseline)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~TokenizationContextDisposeTests"`
Expected: PASS

- [ ] **Step 3: Simplify the dispose implementation**

Replace the dispose region in `src/Tokenizer/Tokenization/TokenizationContext.cs`:

```csharp
public sealed class TokenizationContext : ITokenizationContext, IDisposable
{
    private bool _disposed;

    // ... (keep constructor and other members unchanged)

    public void Dispose()
    {
        if (_disposed) return;

        if (Enumerator is IDisposable disposableEnumerator)
        {
            disposableEnumerator.Dispose();
        }

        _disposed = true;
    }
}
```

Remove:
- The `Dispose(bool disposing)` method
- The `~TokenizationContext()` finalizer
- The `GC.SuppressFinalize(this)` call

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationContext.cs tests/Tokenizer.Tests/Tokenization/TokenizationContextDisposeTests.cs
git commit -m "Remove unnecessary finalizer from TokenizationContext"
```

---

### Task 11: Replace `string.Compare` with `string.Equals` (roadmap item 14)

**Files:**
- Modify: `src/Tokenizer/Template.cs`
- Modify: `src/Tokenizer/Compilation/TokenParser.cs`

- [ ] **Step 1: Write tests verifying string comparison behavior is preserved**

```csharp
// tests/Tokenizer.Tests/StringComparisonTests.cs
using Xunit;

namespace Tokens
{
    public class StringComparisonTests
    {
        [Theory]
        [InlineData("test", "TEST")]
        [InlineData("Test", "test")]
        public void GivenTemplate_WhenCheckingTagCaseInsensitive_ThenFindsTag(string tagToAdd, string tagToFind)
        {
            // Arrange
            var template = new Template("content");
            template.AddTag(tagToAdd);

            // Act & Assert
            Assert.True(template.HasTag(tagToFind));
        }

        [Fact]
        public void GivenTemplate_WhenCheckingNonexistentTag_ThenReturnsFalse()
        {
            // Arrange
            var template = new Template("content");
            template.AddTag("existing");

            // Act & Assert
            Assert.False(template.HasTag("nonexistent"));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they pass (baseline)**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~StringComparisonTests"`
Expected: PASS

- [ ] **Step 3: Update `Template.cs`**

In `HasTag` method, change:

```csharp
if (string.Compare(candidate, tag, StringComparison.InvariantCultureIgnoreCase) == 0)
```

to:

```csharp
if (string.Equals(candidate, tag, StringComparison.InvariantCultureIgnoreCase))
```

- [ ] **Step 4: Update `TokenParser.cs`**

Find all `string.Compare(...)` calls and replace with `string.Equals(...)`. There are 4 locations:

Line 280-281 (transformer matching):
```csharp
// Before:
if (string.Compare(decorator.Name, operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0 ||
    string.Compare($"{decorator.Name}Transformer", operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
// After:
if (string.Equals(decorator.Name, operatorType.Name, StringComparison.InvariantCultureIgnoreCase) ||
    string.Equals($"{decorator.Name}Transformer", operatorType.Name, StringComparison.InvariantCultureIgnoreCase))
```

Line 310-311 (validator matching):
```csharp
// Before:
if (string.Compare(decorator.Name, validatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0 ||
    string.Compare($"{decorator.Name}Validator", validatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
// After:
if (string.Equals(decorator.Name, validatorType.Name, StringComparison.InvariantCultureIgnoreCase) ||
    string.Equals($"{decorator.Name}Validator", validatorType.Name, StringComparison.InvariantCultureIgnoreCase))
```

Line 361 (concat decorator):
```csharp
// Before:
if (string.Compare("concat", decorator.Name, StringComparison.InvariantCultureIgnoreCase) != 0) return false;
// After:
if (!string.Equals("concat", decorator.Name, StringComparison.InvariantCultureIgnoreCase)) return false;
```

Also check `ObjectExtensions.cs` for `string.Compare` calls (lines 30, 49, 228, 247) — these take a `StringComparison` parameter passed by the caller, so update them too:

```csharp
// Before:
if (string.Compare(objectType, segments[0], stringComparison) == 0)
// After:
if (string.Equals(objectType, segments[0], stringComparison))
```

```csharp
// Before:
if (string.Compare(propertyInfo.Name, path[0], stringComparison) != 0) continue;
// After:
if (!string.Equals(propertyInfo.Name, path[0], stringComparison)) continue;
```

- [ ] **Step 5: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Template.cs src/Tokenizer/Compilation/TokenParser.cs src/Tokenizer/Extensions/ObjectExtensions.cs tests/Tokenizer.Tests/StringComparisonTests.cs
git commit -m "Replace string.Compare with string.Equals for clarity"
```

---

## Phase 3: Infrastructure & Polish

### Task 12: Add GitHub Actions CI workflow (roadmap item 5)

**Files:**
- Create: `.github/workflows/build-and-test.yml`
- Delete: `appveyor.yml`

- [ ] **Step 1: Create the workflow file**

```yaml
# .github/workflows/build-and-test.yml
name: Build and Test

on:
  push:
    branches: [ master, v3 ]
  pull_request:
    branches: [ master ]

jobs:
  build:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        dotnet-version: ['8.0.x', '10.0.x']

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET ${{ matrix.dotnet-version }}
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ matrix.dotnet-version }}

    - name: Restore
      run: dotnet restore ./src/Tokenizer/Tokenizer.csproj

    - name: Build
      run: dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release --no-restore

    - name: Test
      run: dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --verbosity normal
```

- [ ] **Step 2: Delete the stale `appveyor.yml`**

```bash
rm appveyor.yml
```

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/build-and-test.yml
git rm appveyor.yml
git commit -m "Replace stale AppVeyor config with GitHub Actions CI"
```

---

### Task 13: Add `Directory.Build.props` and `.editorconfig` (roadmap item 10)

**Files:**
- Create: `Directory.Build.props` (repo root)
- Create: `.editorconfig` (repo root)
- Modify: `src/Tokenizer/Tokenizer.csproj`

- [ ] **Step 1: Create `Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Remove duplicated properties from `Tokenizer.csproj`**

Remove these lines from the first `<PropertyGroup>` in `src/Tokenizer/Tokenizer.csproj`:

```xml
   <LangVersion>latest</LangVersion>
   <Nullable>enable</Nullable>
   <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
   <ImplicitUsings>enable</ImplicitUsings>
```

These are now inherited from `Directory.Build.props`.

- [ ] **Step 3: Create `.editorconfig`**

```ini
# .editorconfig
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
# Allman brace style
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true

# Naming conventions
dotnet_naming_style.pascal_case.capitalization = pascal_case
dotnet_naming_style.camel_case.capitalization = camel_case

dotnet_naming_rule.public_members.symbols = public_symbols
dotnet_naming_rule.public_members.style = pascal_case
dotnet_naming_rule.public_members.severity = warning
dotnet_naming_symbols.public_symbols.applicable_kinds = property, method, event
dotnet_naming_symbols.public_symbols.applicable_accessibilities = public

dotnet_naming_rule.private_fields.symbols = private_field_symbols
dotnet_naming_rule.private_fields.style = camel_case
dotnet_naming_rule.private_fields.severity = warning
dotnet_naming_symbols.private_field_symbols.applicable_kinds = field
dotnet_naming_symbols.private_field_symbols.applicable_accessibilities = private

[*.{xml,csproj,props}]
indent_size = 2
```

- [ ] **Step 4: Build and test**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add Directory.Build.props .editorconfig src/Tokenizer/Tokenizer.csproj
git commit -m "Add Directory.Build.props and .editorconfig"
```

---

### Task 14: Replace `new string[0]` with `Array.Empty<string>()` (roadmap item 13)

**Files:**
- Modify: `src/Tokenizer/TokenMatcher.cs`
- Modify: `src/Tokenizer/Extensions/StringExtensions.cs`

- [ ] **Step 1: Update `TokenMatcher.cs`**

Line 49 and line 102, change:
```csharp
// Before:
if (tags == null) tags = new string[0];
// After:
if (tags == null) tags = Array.Empty<string>();
```

- [ ] **Step 2: Update `StringExtensions.cs`**

Line 187, change:
```csharp
// Before:
return new string[0];
// After:
return Array.Empty<string>();
```

- [ ] **Step 3: Grep for any remaining instances**

Run: `grep -rn "new string\[0\]" src/`
Expected: No results

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/TokenMatcher.cs src/Tokenizer/Extensions/StringExtensions.cs
git commit -m "Replace new string[0] with Array.Empty<string>()"
```

---

### Task 15: NuGet package improvements (roadmap items 12, 15)

**Files:**
- Modify: `src/Tokenizer/Tokenizer.csproj`

- [ ] **Step 1: Add `PackageReadmeFile` and `EmbedUntrackedSources`**

Add to the first `<PropertyGroup>` in `src/Tokenizer/Tokenizer.csproj`:

```xml
<PackageReadmeFile>README.md</PackageReadmeFile>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
```

Add a new `<ItemGroup>` for the README:

```xml
<ItemGroup>
  <None Include="../../README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

- [ ] **Step 2: Verify it builds**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds. If README.md doesn't exist at the repo root, create a minimal one or adjust the path.

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Tokenizer.csproj
git commit -m "Add PackageReadmeFile and EmbedUntrackedSources to csproj"
```

---

### Task 16: Replace `ArgumentValidation.ThrowIfNull` with BCL method (roadmap item 19)

**Files:**
- Modify: `src/Tokenizer/Tokenization/TokenizationEngine.cs`
- Modify: `src/Tokenizer/Tokenization/HintProcessor.cs`
- Modify: `src/Tokenizer/Tokenization/ResultBuilder.cs`

- [ ] **Step 1: Update `ArgumentValidation` class with conditional compilation**

In `src/Tokenizer/Tokenization/TokenizationEngine.cs`, update the `ArgumentValidation` class:

```csharp
internal static class ArgumentValidation
{
#if NETSTANDARD2_0
    public static void ThrowIfNull(object argument, string paramName)
    {
        if (argument == null) throw new ArgumentNullException(paramName);
    }
#else
    public static void ThrowIfNull(object argument, string paramName)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }
#endif
}
```

- [ ] **Step 2: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Tokenization/TokenizationEngine.cs
git commit -m "Use BCL ArgumentNullException.ThrowIfNull on modern targets"
```

---

### Task 17: XML doc coverage (roadmap item 17)

**Files:**
- Modify: `src/Tokenizer/HintMatch.cs`
- Modify: `src/Tokenizer/TokenResult.cs`
- Modify: `src/Tokenizer/Exceptions/ParsingException.cs`
- Modify: `src/Tokenizer/Exceptions/TokenAssignmentException.cs`
- Modify: `src/Tokenizer/Exceptions/TokenMatcherException.cs`
- Modify: `src/Tokenizer/TokenizerOptions.cs`

- [ ] **Step 1: Add XML docs to undocumented public types and properties**

For each file, add `/// <summary>` docs to any public type or member that lacks them. Read each file first, identify missing docs, and add them. Key items:

`HintMatch` — add class-level summary and property docs.
`TokenResult` — add class-level summary.
`ParsingException` — add class-level summary, property docs for `Line`/`Column`.
`TokenAssignmentException` — add class-level summary.
`TokenMatcherException` — add class-level summary.
`TokenizerOptions` — add docs to: `IgnoreMissingProperties`, `TrimLeadingWhitespaceInTokenPreamble`, `TrimPreambleBeforeNewLine`, `OutOfOrderTokens`.

- [ ] **Step 2: Build to verify no warnings**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with no warnings

- [ ] **Step 3: Commit**

```bash
git status
git add -u src/Tokenizer/
git commit -m "Add XML doc coverage to remaining public types"
```

---

### Task 18: File-scoped namespaces (roadmap item 18)

**Files:**
- Modify: All `.cs` files in `src/Tokenizer/` and `tests/Tokenizer.Tests/`

- [ ] **Step 1: Convert source files to file-scoped namespaces**

Use `dotnet format` or manual conversion. For each `.cs` file that uses block-scoped namespaces:

```csharp
// Before:
namespace Tokens
{
    public class Foo
    {
    }
}

// After:
namespace Tokens;

public class Foo
{
}
```

Process all files in `src/Tokenizer/` first. This is a mechanical transformation — for each file:
1. Replace `namespace X\n{` with `namespace X;`
2. Remove the closing `}` for the namespace
3. Dedent all code by one level

- [ ] **Step 2: Convert test files to file-scoped namespaces**

Same transformation for all files in `tests/Tokenizer.Tests/`.

- [ ] **Step 3: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 4: Commit**

```bash
git add -u src/Tokenizer/ tests/Tokenizer.Tests/
git commit -m "Convert to file-scoped namespaces"
```

---

## Final Verification

### Task 19: Full verification pass

- [ ] **Step 1: Clean build**

```bash
dotnet clean ./src/Tokenizer/Tokenizer.csproj
dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release
```
Expected: Build succeeds with no warnings

- [ ] **Step 2: Run all tests**

```bash
dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --verbosity normal
```
Expected: ALL PASS

- [ ] **Step 3: Verify roadmap coverage**

Review each of the 20 roadmap items against the commits:

| # | Item | Task |
|---|------|------|
| 1 | Fix `ValidationException` inheritance | Task 2 |
| 2 | Cache `Activator.CreateInstance` | Task 7 |
| 3 | Cache `GetType().GetProperties()` | Task 8 |
| 4 | Replace mutable `IList<T>` | Task 5 |
| 5 | Add CI workflow | Task 12 |
| 6 | Bump version to 3.0.0 | Task 1 |
| 7 | Seal public classes | Task 6 |
| 8 | Make `Token` properties immutable | Task 3 |
| 9 | Remove finalizer | Task 10 |
| 10 | Add `Directory.Build.props`/`.editorconfig` | Task 13 |
| 11 | Culture-invariant transformers | Task 9 |
| 12 | Add `PackageReadmeFile` | Task 15 |
| 13 | Replace `new string[0]` | Task 14 |
| 14 | Replace `string.Compare` | Task 11 |
| 15 | Add `EmbedUntrackedSources` | Task 15 |
| 16 | Exception location properties | Task 4 |
| 17 | XML doc coverage | Task 17 |
| 18 | File-scoped namespaces | Task 18 |
| 19 | Use `ArgumentNullException.ThrowIfNull` | Task 16 |
| 20 | Target supported .NET version | Task 1 |
