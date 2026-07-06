# Template Compiler Restructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Decompose the monolithic `TemplateCompiler.Compile()` method into focused, testable binder classes with unified `IDiagnosticCollector`-based observability.

**Architecture:** Each compilation responsibility becomes a small static class in `Tokens.Compilation.Binders`. `TemplateCompiler` becomes a thin orchestrator that calls them in sequence. Diagnostics use the existing `IDiagnosticCollector` pattern from the tokenization pipeline, extended with compilation-specific event types.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 6.0 dual-target, xUnit, NSubstitute

---

### Task 1: Add Compilation Diagnostic Event Types

**Files:**
- Modify: `src/Tokenizer/Diagnostics/DiagnosticEventType.cs`
- Test: `tests/Tokenizer.Tests/Diagnostics/DiagnosticCollectorTests.cs` (verify existing tests still pass)

- [ ] **Step 1: Add compilation event types to the enum**

Add these members at the end of the `DiagnosticEventType` enum in `src/Tokenizer/Diagnostics/DiagnosticEventType.cs`:

```csharp
/// <summary>
/// A hint was added to the template during compilation.
/// Detail contains the hint text.
/// </summary>
HintAdded,

/// <summary>
/// A tag was added to the template during compilation.
/// Detail contains the tag string.
/// </summary>
TagAdded,

/// <summary>
/// A token was created from a token definition during compilation.
/// TokenName and TokenId identify the created token.
/// </summary>
TokenCreated,

/// <summary>
/// A template-level option was applied to a token during compilation.
/// TokenName identifies the token. Detail describes the option applied.
/// </summary>
OptionApplied,

/// <summary>
/// A decorator (transformer or validator) was applied to a token during compilation.
/// TokenName identifies the token. DecoratorName identifies the decorator.
/// </summary>
DecoratorApplied,

/// <summary>
/// A concatenation decorator was applied to a token during compilation.
/// TokenName identifies the token. Detail contains the joining string.
/// </summary>
ConcatenationApplied,

/// <summary>
/// A repeating token was linked to its non-repeating counterpart during compilation.
/// TokenName and TokenId identify the repeating token.
/// Detail contains the id of the linked non-repeating token.
/// </summary>
RepeatingTokenLinked,

/// <summary>
/// Template compilation has completed.
/// Detail contains the template name and token count.
/// </summary>
CompilationCompleted,
```

- [ ] **Step 2: Run existing tests to verify no regressions**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "Diagnostics"`
Expected: All existing diagnostic tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/Tokenizer/Diagnostics/DiagnosticEventType.cs
git commit -m "feat: add compilation diagnostic event types to DiagnosticEventType enum"
```

---

### Task 2: Simplify Template Constructors

**Files:**
- Modify: `src/Tokenizer/Template.cs`
- Modify: `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs`
- Modify: `tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs`
- Modify: `tests/Tokenizer.Tests/TemplateTests.cs`
- Modify: `tests/Tokenizer.Tests/TemplateCollectionTests.cs`
- Modify: `tests/Tokenizer.Tests/TokenizeResultTests.cs`
- Modify: `tests/Tokenizer.Tests/StringComparisonTests.cs`

- [ ] **Step 1: Write a failing test for the new constructor**

Add to `tests/Tokenizer.Tests/TemplateTests.cs`:

```csharp
[Fact]
public void GivenIdAndOptions_WhenConstructed_ThenIdAndOptionsAreSet()
{
    // Arrange
    var options = new TokenizerOptions { TrimTrailingWhiteSpace = false };

    // Act
    var template = new TemplateBuilder()
        .WithOptions(options)
        .Build();

    // Assert
    Assert.False(template.Options.TrimTrailingWhiteSpace);
}
```

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "GivenIdAndOptions_WhenConstructed_ThenIdAndOptionsAreSet"`
Expected: FAIL — `TemplateBuilder.Build()` doesn't use the new constructor yet.

- [ ] **Step 2: Replace Template constructors**

In `src/Tokenizer/Template.cs`, remove all four existing constructors and replace with a single internal constructor:

```csharp
/// <summary>
/// Creates a new template with the given content-based id and options.
/// </summary>
/// <param name="id">Content-based identity hash.</param>
/// <param name="options">The options to use when parsing this template.</param>
internal Template(ulong id, TokenizerOptions options)
{
    tokens = new List<Token>();
    hints = new List<Hint>();
    tags = new List<string>();
    Options = options;
    Id = id;
    Name = string.Empty;
}
```

Remove the `using Tokens.Extensions;` import (no longer needed — `ComputeHash` call removed).

- [ ] **Step 3: Update TemplateBuilder to use new constructor**

In `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs`, add an `_id` field and update `Build()`:

```csharp
namespace Tokens.Builders;

/// <summary>
/// Builder for creating Template instances for testing
/// </summary>
public class TemplateBuilder
{
    private readonly List<Token> _tokens = new();
    private readonly List<Hint> _hints = new();
    private readonly List<string> _tags = new();
    private string _name = string.Empty;
    private ulong _id;
    private TokenizerOptions _options = new();

    public TemplateBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TemplateBuilder WithId(ulong id)
    {
        _id = id;
        return this;
    }

    public TemplateBuilder WithTokens(params Token[] tokens)
    {
        _tokens.AddRange(tokens);
        return this;
    }

    public TemplateBuilder WithHints(params Hint[] hints)
    {
        _hints.AddRange(hints);
        return this;
    }

    public TemplateBuilder WithTags(params string[] tags)
    {
        _tags.AddRange(tags);
        return this;
    }

    public TemplateBuilder WithOptions(TokenizerOptions options)
    {
        _options = options;
        return this;
    }

    public TemplateBuilder WithDefaultOptions()
    {
        _options = new TokenizerOptions();
        return this;
    }

    public Template Build()
    {
        var template = new Template(_id, _options);
        template.Name = _name;
        foreach (var token in _tokens) template.AddToken(token);
        foreach (var hint in _hints) template.AddHint(hint);
        foreach (var tag in _tags) template.AddTag(tag);
        return template;
    }
}
```

- [ ] **Step 4: Update TokenizeResultBuilder to use TemplateBuilder**

In `tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs`, change the default template initialization:

Line 10: Change `private TokenizeResult _result = new(new Template(string.Empty));` to:
```csharp
private TokenizeResult _result = new(new TemplateBuilder().Build());
```

Line 74: Change `private Template _template = new(string.Empty);` to:
```csharp
private Template _template = new TemplateBuilder().Build();
```

- [ ] **Step 5: Update TemplateTests.cs**

Replace all `new Template(string.Empty)` and `new Template("name")` calls with `TemplateBuilder`:

- `new Template(string.Empty)` becomes `new TemplateBuilder().Build()`
- `new Template("invoice")` becomes `new TemplateBuilder().WithName("invoice").Build()`
- `new Template("test", options)` becomes `new TemplateBuilder().WithName("test").WithOptions(options).Build()`

For example, `TestHasTagWhenTrue`:
```csharp
[Fact]
public void TestHasTagWhenTrue()
{
    var template = new TemplateBuilder().Build();
    template.AddTag("One");

    Assert.True(template.HasTag("One"));
}
```

And `GivenNamedTemplate_WhenToString_ThenReturnsName`:
```csharp
[Fact]
public void GivenNamedTemplate_WhenToString_ThenReturnsName()
{
    // Arrange
    var template = new TemplateBuilder().WithName("invoice").Build();

    // Act
    var result = template.ToString();

    // Assert
    Assert.Equal("Template('invoice')", result);
}
```

Apply the same pattern to all tests in the file.

- [ ] **Step 6: Update TemplateCollectionTests.cs**

Replace direct `Template` construction. For tests that use `new Template(string.Empty)` and add tags, use `TemplateBuilder`. Each template in a collection needs a unique Id to avoid key collisions:

```csharp
// TestCollectionContainsTagWhenTrue
var template = new TemplateBuilder().WithId(1).Build();
template.AddTag("One");
```

For `TestCollectionContainsAllTagsWhenTrue`:
```csharp
var template = new TemplateBuilder().WithId(1).Build();
template.AddTag("One");
template.AddTag("Two");
```

Apply to all tests using `new Template(string.Empty)` in this file.

- [ ] **Step 7: Update TokenizeResultTests.cs**

```csharp
[Fact]
public void GivenTokenizeResult_WhenToString_ThenReturnsCompactFormat()
{
    // Arrange
    var template = new TemplateBuilder().WithName("test-template").Build();
    var result = new TokenizeResult(template);

    // Act
    var output = result.ToString();

    // Assert
    Assert.Equal("TokenizeResult('test-template': 0 matched, 0 missed)", output);
}
```

- [ ] **Step 8: Update StringComparisonTests.cs**

```csharp
[Theory]
[InlineData("test", "TEST")]
[InlineData("Test", "test")]
public void GivenTemplate_WhenCheckingTagCaseInsensitive_ThenFindsTag(string tagToAdd, string tagToFind)
{
    var template = new TemplateBuilder().WithName("content").Build();
    template.AddTag(tagToAdd);
    Assert.True(template.HasTag(tagToFind));
}

[Fact]
public void GivenTemplate_WhenCheckingNonexistentTag_ThenReturnsFalse()
{
    var template = new TemplateBuilder().WithName("content").Build();
    template.AddTag("existing");
    Assert.False(template.HasTag("nonexistent"));
}
```

- [ ] **Step 9: Update TemplateCompiler to use new constructor**

In `src/Tokenizer/Compilation/TemplateCompiler.cs`, line 62, change:
```csharp
var template = new Template(content, name, preTemplate.Options);
```
to:
```csharp
var id = content.ComputeHash();
var template = new Template(id, preTemplate.Options);
template.Name = name;
```

Add `using Tokens.Extensions;` to the top of the file if not already present.

- [ ] **Step 10: Run all tests**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 11: Commit**

```bash
git add src/Tokenizer/Template.cs src/Tokenizer/Compilation/TemplateCompiler.cs tests/Tokenizer.Tests/Builders/TemplateBuilder.cs tests/Tokenizer.Tests/Builders/TokenizeResultBuilder.cs tests/Tokenizer.Tests/TemplateTests.cs tests/Tokenizer.Tests/TemplateCollectionTests.cs tests/Tokenizer.Tests/TokenizeResultTests.cs tests/Tokenizer.Tests/StringComparisonTests.cs
git commit -m "refactor: simplify Template to single internal constructor with ulong id"
```

---

### Task 3: Extract TemplateLengthValidator

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/TemplateLengthValidator.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/TemplateLengthValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/TemplateLengthValidatorTests.cs`:

```csharp
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TemplateLengthValidatorTests
{
    [Fact]
    public void GivenContentExceedingMaxLength_WhenValidating_ThenThrowsParsingException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTemplateLength = 10 };
        var content = new string('x', 11);

        // Act & Assert
        var ex = Assert.Throws<ParsingException>(() => TemplateLengthValidator.Validate(content, options));
        Assert.Contains("exceeds maximum allowed length", ex.Message);
    }

    [Fact]
    public void GivenContentAtMaxLength_WhenValidating_ThenDoesNotThrow()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTemplateLength = 10 };
        var content = new string('x', 10);

        // Act & Assert
        TemplateLengthValidator.Validate(content, options);
    }

    [Fact]
    public void GivenMaxLengthDisabled_WhenValidating_ThenDoesNotThrow()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTemplateLength = 0 };
        var content = new string('x', 10000);

        // Act & Assert
        TemplateLengthValidator.Validate(content, options);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateLengthValidatorTests"`
Expected: FAIL — `TemplateLengthValidator` class does not exist.

- [ ] **Step 3: Implement TemplateLengthValidator**

Create `src/Tokenizer/Compilation/Binders/TemplateLengthValidator.cs`:

```csharp
using Tokens.Exceptions;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Validates that template content does not exceed the configured maximum length.
/// </summary>
internal static class TemplateLengthValidator
{
    public static void Validate(string content, TokenizerOptions options)
    {
        if (options.MaxTemplateLength > 0 && content.Length > options.MaxTemplateLength)
        {
            throw new ParsingException(
                $"Template length {content.Length:N0} exceeds maximum allowed length of {options.MaxTemplateLength:N0}. " +
                "Increase TokenizerOptions.MaxTemplateLength to allow larger templates.",
                new Enumerators.FileLocation());
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateLengthValidatorTests"`
Expected: All 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/TemplateLengthValidator.cs tests/Tokenizer.Tests/Compilation/Binders/TemplateLengthValidatorTests.cs
git commit -m "feat: extract TemplateLengthValidator from TemplateCompiler"
```

---

### Task 4: Extract TokenCountValidator

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/TokenCountValidator.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/TokenCountValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/TokenCountValidatorTests.cs`:

```csharp
using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TokenCountValidatorTests
{
    [Fact]
    public void GivenTokenCountExceedingMax_WhenValidating_ThenThrowsParsingException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTokenCount = 2 };
        var template = new TemplateBuilder()
            .WithOptions(options)
            .WithTokens(
                new Token("a", "A", "", new FileLocation()),
                new Token("b", "B", "", new FileLocation()),
                new Token("c", "C", "", new FileLocation()))
            .Build();

        // Act & Assert
        var ex = Assert.Throws<ParsingException>(() => TokenCountValidator.Validate(template, options));
        Assert.Contains("exceeding maximum", ex.Message);
    }

    [Fact]
    public void GivenTokenCountAtMax_WhenValidating_ThenDoesNotThrow()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTokenCount = 2 };
        var template = new TemplateBuilder()
            .WithOptions(options)
            .WithTokens(
                new Token("a", "A", "", new FileLocation()),
                new Token("b", "B", "", new FileLocation()))
            .Build();

        // Act & Assert
        TokenCountValidator.Validate(template, options);
    }

    [Fact]
    public void GivenMaxTokenCountDisabled_WhenValidating_ThenDoesNotThrow()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTokenCount = 0 };
        var template = new TemplateBuilder()
            .WithOptions(options)
            .WithTokens(
                new Token("a", "A", "", new FileLocation()),
                new Token("b", "B", "", new FileLocation()),
                new Token("c", "C", "", new FileLocation()))
            .Build();

        // Act & Assert
        TokenCountValidator.Validate(template, options);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenCountValidatorTests"`
Expected: FAIL — `TokenCountValidator` class does not exist.

- [ ] **Step 3: Implement TokenCountValidator**

Create `src/Tokenizer/Compilation/Binders/TokenCountValidator.cs`:

```csharp
using Tokens.Exceptions;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Validates that a compiled template does not exceed the configured maximum token count.
/// </summary>
internal static class TokenCountValidator
{
    public static void Validate(Template template, TokenizerOptions options)
    {
        if (options.MaxTokenCount > 0 && template.Tokens.Count > options.MaxTokenCount)
        {
            throw new ParsingException(
                $"Template contains {template.Tokens.Count} tokens, exceeding maximum of {options.MaxTokenCount:N0}. " +
                "Increase TokenizerOptions.MaxTokenCount to allow more tokens.",
                new Enumerators.FileLocation());
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenCountValidatorTests"`
Expected: All 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/TokenCountValidator.cs tests/Tokenizer.Tests/Compilation/Binders/TokenCountValidatorTests.cs
git commit -m "feat: extract TokenCountValidator from TemplateCompiler"
```

---

### Task 5: Extract TemplateFactory

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/TemplateFactory.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/TemplateFactoryTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/TemplateFactoryTests.cs`:

```csharp
using Tokens.Compilation.Definitions;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TemplateFactoryTests
{
    [Fact]
    public void GivenDefinitionWithName_WhenCreating_ThenTemplateHasName()
    {
        // Arrange
        var definition = new TemplateDefinition { Name = "My Template" };

        // Act
        var template = TemplateFactory.Create(42UL, definition);

        // Assert
        Assert.Equal("My Template", template.Name);
    }

    [Fact]
    public void GivenDefinitionWithoutName_WhenCreating_ThenNameIsAutoGenerated()
    {
        // Arrange
        var definition = new TemplateDefinition { Name = string.Empty };

        // Act
        var template = TemplateFactory.Create(42UL, definition);

        // Assert
        Assert.StartsWith("Template_", template.Name);
    }

    [Fact]
    public void GivenDefinitionWithWhitespaceName_WhenCreating_ThenNameIsAutoGenerated()
    {
        // Arrange
        var definition = new TemplateDefinition { Name = "   " };

        // Act
        var template = TemplateFactory.Create(42UL, definition);

        // Assert
        Assert.StartsWith("Template_", template.Name);
    }

    [Fact]
    public void GivenId_WhenCreating_ThenTemplateHasId()
    {
        // Arrange
        var definition = new TemplateDefinition();

        // Act
        var template = TemplateFactory.Create(123UL, definition);

        // Assert
        Assert.Equal(123UL, template.Id);
    }

    [Fact]
    public void GivenDefinitionWithOptions_WhenCreating_ThenTemplateHasOptions()
    {
        // Arrange
        var options = new TokenizerOptions { OutOfOrderTokens = true };
        var definition = new TemplateDefinition { Options = options };

        // Act
        var template = TemplateFactory.Create(42UL, definition);

        // Assert
        Assert.True(template.Options.OutOfOrderTokens);
    }

    [Fact]
    public void GivenMultipleCreations_WhenNoName_ThenCounterIncrements()
    {
        // Arrange
        var definition = new TemplateDefinition { Name = string.Empty };

        // Act
        var t1 = TemplateFactory.Create(1UL, definition);
        var t2 = TemplateFactory.Create(2UL, definition);

        // Assert
        Assert.NotEqual(t1.Name, t2.Name);
        Assert.StartsWith("Template_", t1.Name);
        Assert.StartsWith("Template_", t2.Name);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateFactoryTests"`
Expected: FAIL — `TemplateFactory` class does not exist.

- [ ] **Step 3: Implement TemplateFactory**

Create `src/Tokenizer/Compilation/Binders/TemplateFactory.cs`:

```csharp
using Tokens.Compilation.Definitions;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Creates <see cref="Template"/> instances from parsed <see cref="TemplateDefinition"/>s.
/// Owns auto-naming via an incrementing counter.
/// </summary>
internal static class TemplateFactory
{
    private static int templateCounter;

    public static Template Create(ulong id, TemplateDefinition definition)
    {
        var template = new Template(id, definition.Options);

        template.Name = string.IsNullOrWhiteSpace(definition.Name)
            ? $"Template_{Interlocked.Increment(ref templateCounter)}"
            : definition.Name;

        return template;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateFactoryTests"`
Expected: All 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/TemplateFactory.cs tests/Tokenizer.Tests/Compilation/Binders/TemplateFactoryTests.cs
git commit -m "feat: extract TemplateFactory from TemplateCompiler"
```

---

### Task 6: Extract HintBinder

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/HintBinder.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/HintBinderTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/HintBinderTests.cs`:

```csharp
using Tokens.Builders;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Compilation.Binders;

public class HintBinderTests
{
    [Fact]
    public void GivenDefinitionWithHints_WhenBinding_ThenTemplateHasHints()
    {
        // Arrange
        var definition = new TemplateDefinition();
        definition.Hints.Add(new Hint("invoice", false));
        definition.Hints.Add(new Hint("receipt", false));
        var template = new TemplateBuilder().Build();

        // Act
        HintBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(2, template.Hints.Count);
        Assert.Equal("invoice", template.Hints[0].Text);
        Assert.Equal("receipt", template.Hints[1].Text);
    }

    [Fact]
    public void GivenDefinitionWithDuplicateHints_WhenBinding_ThenDuplicatesAreSkipped()
    {
        // Arrange
        var definition = new TemplateDefinition();
        definition.Hints.Add(new Hint("invoice", false));
        definition.Hints.Add(new Hint("invoice", false));
        var template = new TemplateBuilder().Build();

        // Act
        HintBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(template.Hints);
    }

    [Fact]
    public void GivenDefinitionWithNoHints_WhenBinding_ThenTemplateHasNoHints()
    {
        // Arrange
        var definition = new TemplateDefinition();
        var template = new TemplateBuilder().Build();

        // Act
        HintBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Empty(template.Hints);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenBinding_ThenRecordsHintAddedEvents()
    {
        // Arrange
        var definition = new TemplateDefinition();
        definition.Hints.Add(new Hint("invoice", false));
        var template = new TemplateBuilder().Build();
        var collector = new DiagnosticCollector(null, null);

        // Act
        HintBinder.Bind(definition, template, collector);

        // Assert
        var diagnostics = collector.GetResult()!;
        Assert.Single(diagnostics.Events);
        Assert.Equal(DiagnosticEventType.HintAdded, diagnostics.Events[0].Type);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HintBinderTests"`
Expected: FAIL — `HintBinder` class does not exist.

- [ ] **Step 3: Implement HintBinder**

Create `src/Tokenizer/Compilation/Binders/HintBinder.cs`:

```csharp
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Assigns hints from a <see cref="TemplateDefinition"/> to a <see cref="Template"/>,
/// skipping duplicates.
/// </summary>
internal static class HintBinder
{
    public static void Bind(TemplateDefinition definition, Template template, IDiagnosticCollector collector)
    {
        foreach (var hint in definition.Hints)
        {
            if (template.Hints.Any(h => h == hint))
                continue;

            template.AddHint(hint);

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.HintAdded, detail: hint.Text);
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "HintBinderTests"`
Expected: All 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/HintBinder.cs tests/Tokenizer.Tests/Compilation/Binders/HintBinderTests.cs
git commit -m "feat: extract HintBinder from TemplateCompiler"
```

---

### Task 7: Extract TagBinder

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/TagBinder.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/TagBinderTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/TagBinderTests.cs`:

```csharp
using Tokens.Builders;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TagBinderTests
{
    [Fact]
    public void GivenDefinitionWithTags_WhenBinding_ThenTemplateHasTags()
    {
        // Arrange
        var definition = new TemplateDefinition();
        definition.Tags.Add("invoice");
        definition.Tags.Add("receipt");
        var template = new TemplateBuilder().Build();

        // Act
        TagBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(2, template.Tags.Count);
        Assert.Equal("invoice", template.Tags[0]);
        Assert.Equal("receipt", template.Tags[1]);
    }

    [Fact]
    public void GivenDefinitionWithDuplicateTags_WhenBinding_ThenDuplicatesAreSkipped()
    {
        // Arrange
        var definition = new TemplateDefinition();
        definition.Tags.Add("invoice");
        definition.Tags.Add("invoice");
        var template = new TemplateBuilder().Build();

        // Act
        TagBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(template.Tags);
    }

    [Fact]
    public void GivenDefinitionWithNoTags_WhenBinding_ThenTemplateHasNoTags()
    {
        // Arrange
        var definition = new TemplateDefinition();
        var template = new TemplateBuilder().Build();

        // Act
        TagBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Empty(template.Tags);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenBinding_ThenRecordsTagAddedEvents()
    {
        // Arrange
        var definition = new TemplateDefinition();
        definition.Tags.Add("invoice");
        var template = new TemplateBuilder().Build();
        var collector = new DiagnosticCollector(null, null);

        // Act
        TagBinder.Bind(definition, template, collector);

        // Assert
        var diagnostics = collector.GetResult()!;
        Assert.Single(diagnostics.Events);
        Assert.Equal(DiagnosticEventType.TagAdded, diagnostics.Events[0].Type);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TagBinderTests"`
Expected: FAIL — `TagBinder` class does not exist.

- [ ] **Step 3: Implement TagBinder**

Create `src/Tokenizer/Compilation/Binders/TagBinder.cs`:

```csharp
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Assigns tags from a <see cref="TemplateDefinition"/> to a <see cref="Template"/>,
/// skipping duplicates.
/// </summary>
internal static class TagBinder
{
    public static void Bind(TemplateDefinition definition, Template template, IDiagnosticCollector collector)
    {
        foreach (var tag in definition.Tags)
        {
            if (template.Tags.Any(t => t == tag))
                continue;

            template.AddTag(tag);

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.TagAdded, detail: tag);
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TagBinderTests"`
Expected: All 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/TagBinder.cs tests/Tokenizer.Tests/Compilation/Binders/TagBinderTests.cs
git commit -m "feat: extract TagBinder from TemplateCompiler"
```

---

### Task 8: Extract TokenFactory (token creation + preamble computation)

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/TokenFactory.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/TokenFactoryTests.cs`

Note: This is the *token-level* factory (creates `Token` from `TokenDefinition`), not to be confused with `TemplateFactory` (Task 5) which creates `Template` from `TemplateDefinition`.

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/TokenFactoryTests.cs`:

```csharp
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TokenFactoryTests
{
    [Fact]
    public void GivenTokenDefinition_WhenCreating_ThenPropertiesAreMapped()
    {
        // Arrange
        var definition = new TokenDefinition
        {
            Content = "{Name}",
            IsOptional = true,
            IsRepeating = true,
            TerminateOnNewLine = true,
            IsRequired = true,
            DependsOnId = 5,
            IsFrontMatterToken = true,
            IsNull = true,
            IsSingleUse = true
        };
        definition.AppendName("Name");
        definition.AppendPreamble("Preamble: ");

        // Act
        var token = TokenFactory.Create(definition, new TokenizerOptions(), NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal("Name", token.Name);
        Assert.Equal("Preamble: ", token.Preamble);
        Assert.Equal("{Name}", token.ToString());
        Assert.True(token.IsOptional);
        Assert.True(token.IsRepeating);
        Assert.True(token.TerminateOnNewLine);
        Assert.True(token.IsRequired);
        Assert.Equal(5, token.DependsOnId);
        Assert.True(token.IsFrontMatterToken);
        Assert.True(token.IsNull);
        Assert.True(token.IsSingleUse);
    }

    [Fact]
    public void GivenTokenDefinitionWithNullName_WhenCreating_ThenNameDefaultsToEmpty()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "literal" };

        // Act
        var token = TokenFactory.Create(definition, new TokenizerOptions(), NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(string.Empty, token.Name);
    }

    [Fact]
    public void GivenTrimLeadingWhitespaceEnabled_WhenPreambleHasLeadingWhitespace_ThenPreambleIsTrimmed()
    {
        // Arrange
        var options = new TokenizerOptions { TrimLeadingWhitespaceInTokenPreamble = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("\n  Hello");

        // Act
        var token = TokenFactory.Create(definition, options, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal("Hello", token.Preamble);
    }

    [Fact]
    public void GivenTrimLeadingWhitespaceEnabled_WhenPreambleIsOnlySpaces_ThenPreambleIsPreserved()
    {
        // Arrange
        var options = new TokenizerOptions { TrimLeadingWhitespaceInTokenPreamble = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("   ");

        // Act
        var token = TokenFactory.Create(definition, options, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal("   ", token.Preamble);
    }

    [Fact]
    public void GivenTrimLeadingWhitespaceEnabled_WhenPreambleIsWhitespaceOnly_ThenLeadingSpacesTrimmed()
    {
        // Arrange
        var options = new TokenizerOptions { TrimLeadingWhitespaceInTokenPreamble = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("\t \n");

        // Act
        var token = TokenFactory.Create(definition, options, NullDiagnosticCollector.Instance);

        // Assert — TrimLeadingSpaces removes only leading space chars
        Assert.Equal("\n", token.Preamble);
    }

    [Fact]
    public void GivenTrimPreambleBeforeNewLineEnabled_WhenPreambleContainsNewline_ThenKeepsTextAfterLastNewline()
    {
        // Arrange
        var options = new TokenizerOptions { TrimPreambleBeforeNewLine = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("First line\nSecond line");

        // Act
        var token = TokenFactory.Create(definition, options, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal("Second line", token.Preamble);
    }

    [Fact]
    public void GivenTrimPreambleBeforeNewLineEnabled_WhenPreambleHasNoNewline_ThenPreambleUnchanged()
    {
        // Arrange
        var options = new TokenizerOptions { TrimPreambleBeforeNewLine = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("No newline here");

        // Act
        var token = TokenFactory.Create(definition, options, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal("No newline here", token.Preamble);
    }

    [Fact]
    public void GivenTokenDefinitionWithLocation_WhenCreating_ThenLocationIsSet()
    {
        // Arrange
        var location = new FileLocation { LineNumber = 5 };
        var definition = new TokenDefinition
        {
            Content = "{Token}",
            Location = location
        };

        // Act
        var token = TokenFactory.Create(definition, new TokenizerOptions(), NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(5, token.Location.LineNumber);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenFactoryTests"`
Expected: FAIL — `TokenFactory` class does not exist (note: `TemplateFactory` exists from Task 5 but `TokenFactory` does not).

- [ ] **Step 3: Implement TokenFactory**

Create `src/Tokenizer/Compilation/Binders/TokenFactory.cs`:

```csharp
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Tokens.Extensions;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Creates <see cref="Token"/> instances from <see cref="TokenDefinition"/>s.
/// Owns preamble computation logic.
/// </summary>
internal static class TokenFactory
{
    public static Token Create(TokenDefinition definition, TokenizerOptions options, IDiagnosticCollector collector)
    {
        var preamble = ComputePreamble(definition, options);
        var location = definition.Location ?? new Enumerators.FileLocation();
        var token = new Token(definition.Content, definition.Name ?? string.Empty, preamble, location);

        token.IsOptional = definition.IsOptional;
        token.IsRepeating = definition.IsRepeating;
        token.TerminateOnNewLine = definition.TerminateOnNewLine;
        token.IsRequired = definition.IsRequired;
        token.DependsOnId = definition.DependsOnId;
        token.IsFrontMatterToken = definition.IsFrontMatterToken;
        token.IsNull = definition.IsNull;
        token.IsSingleUse = definition.IsSingleUse;

        if (collector.IsEnabled)
        {
            collector.Record(DiagnosticEventType.TokenCreated,
                tokenName: token.Name,
                tokenId: definition.Id,
                detail: $"Content={definition.Content}, Optional={token.IsOptional}, Repeating={token.IsRepeating}");
        }

        return token;
    }

    private static string ComputePreamble(TokenDefinition definition, TokenizerOptions options)
    {
        string preamble;

        if (options.TrimLeadingWhitespaceInTokenPreamble)
        {
            if (definition.Preamble.IsOnlySpaces())
            {
                preamble = definition.Preamble;
            }
            else if (string.IsNullOrWhiteSpace(definition.Preamble))
            {
                preamble = definition.Preamble.TrimLeadingSpaces();
            }
            else
            {
                preamble = definition.Preamble.TrimStart();
            }
        }
        else
        {
            preamble = definition.Preamble;
        }

        if (options.TrimPreambleBeforeNewLine)
        {
            if (string.IsNullOrEmpty(preamble) == false && preamble.IndexOf('\n') > -1)
            {
                var idx = preamble.LastIndexOf('\n');
                preamble = preamble.Substring(idx + 1);
            }
        }

        return preamble;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenFactoryTests"`
Expected: All 8 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/TokenFactory.cs tests/Tokenizer.Tests/Compilation/Binders/TokenFactoryTests.cs
git commit -m "feat: extract TokenFactory with preamble computation from TemplateCompiler"
```

---

### Task 9: Extract OptionApplier

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/OptionApplier.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/OptionApplierTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/OptionApplierTests.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Compilation.Binders;

public class OptionApplierTests
{
    [Fact]
    public void GivenOutOfOrderTokensEnabled_WhenApplying_ThenTokenIsOptional()
    {
        // Arrange
        var options = new TokenizerOptions { OutOfOrderTokens = true };
        var token = new Token("{Name}", "Name", "Preamble", new FileLocation());

        // Act
        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(token.IsOptional);
    }

    [Fact]
    public void GivenOutOfOrderTokensDisabled_WhenApplying_ThenTokenOptionalUnchanged()
    {
        // Arrange
        var options = new TokenizerOptions { OutOfOrderTokens = false };
        var token = new Token("{Name}", "Name", "Preamble", new FileLocation());

        // Act
        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(token.IsOptional);
    }

    [Fact]
    public void GivenGlobalTerminateOnNewLine_WhenTokenDoesNotSetIt_ThenTokenGetsNewLineTermination()
    {
        // Arrange
        var options = new TokenizerOptions { TerminateOnNewLine = true };
        var token = new Token("{Name}", "Name", "Preamble", new FileLocation());
        token.TerminateOnNewLine = false;

        // Act
        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenGlobalTerminateOnNewLine_WhenTokenAlreadySetsIt_ThenTokenUnchanged()
    {
        // Arrange
        var options = new TokenizerOptions { TerminateOnNewLine = true };
        var token = new Token("{Name}", "Name", "Preamble", new FileLocation());
        token.TerminateOnNewLine = true;

        // Act
        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenNoGlobalTerminateOnNewLine_WhenApplying_ThenTokenNewLineUnchanged()
    {
        // Arrange
        var options = new TokenizerOptions { TerminateOnNewLine = false };
        var token = new Token("{Name}", "Name", "Preamble", new FileLocation());
        token.TerminateOnNewLine = false;

        // Act
        OptionApplier.Apply(token, options, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(token.TerminateOnNewLine);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenOptionApplied_ThenRecordsEvent()
    {
        // Arrange
        var options = new TokenizerOptions { OutOfOrderTokens = true };
        var token = new Token("{Name}", "Name", "Preamble", new FileLocation());
        var collector = new DiagnosticCollector(null, null);

        // Act
        OptionApplier.Apply(token, options, collector);

        // Assert
        var diagnostics = collector.GetResult()!;
        Assert.Contains(diagnostics.Events, e => e.Type == DiagnosticEventType.OptionApplied);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "OptionApplierTests"`
Expected: FAIL — `OptionApplier` class does not exist.

- [ ] **Step 3: Implement OptionApplier**

Create `src/Tokenizer/Compilation/Binders/OptionApplier.cs`:

```csharp
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Applies template-level option overrides to individual tokens.
/// </summary>
internal static class OptionApplier
{
    public static void Apply(Token token, TokenizerOptions options, IDiagnosticCollector collector)
    {
        if (options.OutOfOrderTokens)
        {
            token.IsOptional = true;

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.OptionApplied,
                    tokenName: token.Name,
                    detail: "OutOfOrderTokens: marked as optional");
            }
        }

        if (token.TerminateOnNewLine == false && options.TerminateOnNewLine)
        {
            token.TerminateOnNewLine = true;

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.OptionApplied,
                    tokenName: token.Name,
                    detail: "TerminateOnNewLine: applied from global option");
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "OptionApplierTests"`
Expected: All 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/OptionApplier.cs tests/Tokenizer.Tests/Compilation/Binders/OptionApplierTests.cs
git commit -m "feat: extract OptionApplier from TemplateCompiler"
```

---

### Task 10: Extract DecoratorBinder

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/DecoratorBinder.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/DecoratorBinderTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/DecoratorBinderTests.cs`:

```csharp
using System.Collections.Concurrent;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Transformers;
using Tokens.Validators;
using Xunit;

namespace Tokens.Compilation.Binders;

public class DecoratorBinderTests
{
    private readonly DecoratorRegistry registry = new(new TokenizerOptions());
    private readonly ConcurrentDictionary<Type, ITokenDecorator> decoratorCache = new();

    [Fact]
    public void GivenTokenDefinitionWithValue_WhenBinding_ThenSetTransformerIsAdded()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "{Foo}" };
        definition.AppendName("Foo");
        definition.AppendValue("bar");
        var token = new Token("{Foo}", "Foo", "", new FileLocation());

        // Act
        DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(token.Decorators);
        Assert.Equal(typeof(SetTransformer), token.Decorators[0].DecoratorType);
        Assert.Equal("bar", token.Decorators[0].Parameters[0]);
    }

    [Fact]
    public void GivenTransformerDecorator_WhenBinding_ThenTransformerIsApplied()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "{Date}" };
        definition.AppendName("Date");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("ToDateTime");
        decorator.Args.Add("yyyy-MM-dd");
        definition.Decorators.Add(decorator);
        var token = new Token("{Date}", "Date", "", new FileLocation());

        // Act
        DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(token.Decorators);
        Assert.Equal(typeof(ToDateTimeTransformer), token.Decorators[0].DecoratorType);
        Assert.Equal("yyyy-MM-dd", token.Decorators[0].Parameters[0]);
    }

    [Fact]
    public void GivenValidatorDecorator_WhenBinding_ThenValidatorIsApplied()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "{Amount}" };
        definition.AppendName("Amount");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("IsNumeric");
        definition.Decorators.Add(decorator);
        var token = new Token("{Amount}", "Amount", "", new FileLocation());

        // Act
        DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(token.Decorators);
        Assert.Equal(typeof(IsNumericValidator), token.Decorators[0].DecoratorType);
    }

    [Fact]
    public void GivenNotValidator_WhenBinding_ThenIsNotValidatorIsSet()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "{Amount}" };
        definition.AppendName("Amount");
        var decorator = new DecoratorDefinition { IsNotDecorator = true };
        decorator.AppendName("IsNumeric");
        definition.Decorators.Add(decorator);
        var token = new Token("{Amount}", "Amount", "", new FileLocation());

        // Act
        DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(token.Decorators);
        Assert.True(token.Decorators[0].IsNotValidator);
    }

    [Fact]
    public void GivenNotTransformer_WhenBinding_ThenThrowsTokenizerException()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "{Date}" };
        definition.AppendName("Date");
        var decorator = new DecoratorDefinition { IsNotDecorator = true };
        decorator.AppendName("ToDateTime");
        decorator.Args.Add("yyyy-MM-dd");
        definition.Decorators.Add(decorator);
        var token = new Token("{Date}", "Date", "", new FileLocation());

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() =>
            DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance));
        Assert.Contains("cannot be prefixed with '!'", ex.Message);
    }

    [Fact]
    public void GivenConcatDecorator_WhenBinding_ThenTokenCanConcatenate()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "{Items}" };
        definition.AppendName("Items");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("concat");
        decorator.Args.Add(", ");
        definition.Decorators.Add(decorator);
        var token = new Token("{Items}", "Items", "", new FileLocation());

        // Act
        DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(token.CanConcatenate);
        Assert.Equal(", ", token.ConcatenationString);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenConcatWithNoArgs_WhenBinding_ThenConcatenationStringIsNull()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "{Items}" };
        definition.AppendName("Items");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("concat");
        definition.Decorators.Add(decorator);
        var token = new Token("{Items}", "Items", "", new FileLocation());

        // Act
        DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(token.CanConcatenate);
        Assert.Null(token.ConcatenationString);
    }

    [Fact]
    public void GivenConcatWithTooManyArgs_WhenBinding_ThenThrowsTokenizerException()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "{Items}" };
        definition.AppendName("Items");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("concat");
        decorator.Args.Add(", ");
        decorator.Args.Add("extra");
        definition.Decorators.Add(decorator);
        var token = new Token("{Items}", "Items", "", new FileLocation());

        // Act & Assert
        Assert.Throws<TokenizerException>(() =>
            DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenUnknownDecorator_WhenBinding_ThenThrowsTokenizerException()
    {
        // Arrange
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendName("Token");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("NonExistentDecorator");
        definition.Decorators.Add(decorator);
        var token = new Token("{Token}", "Token", "", new FileLocation());

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() =>
            DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance));
        Assert.Contains("Unknown Token Operation", ex.Message);
    }

    [Fact]
    public void GivenFrontMatterTokenWithoutSetTransformer_WhenBinding_ThenThrowsTokenizerException()
    {
        // Arrange
        var definition = new TokenDefinition
        {
            Content = "{Decorator}",
            IsFrontMatterToken = true
        };
        definition.AppendName("Decorator");
        var token = new Token("{Decorator}", "Decorator", "", new FileLocation());
        token.IsFrontMatterToken = true;

        // Act & Assert
        var ex = Assert.Throws<TokenizerException>(() =>
            DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance));
        Assert.Contains("must have an assignment operation", ex.Message);
    }

    [Fact]
    public void GivenFrontMatterTokenWithSetTransformer_WhenBinding_ThenSucceeds()
    {
        // Arrange
        var definition = new TokenDefinition
        {
            Content = "{Foo}",
            IsFrontMatterToken = true
        };
        definition.AppendName("Foo");
        definition.AppendValue("bar");
        var token = new Token("{Foo}", "Foo", "", new FileLocation());
        token.IsFrontMatterToken = true;

        // Act
        DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(token.Decorators);
        Assert.Equal(typeof(SetTransformer), token.Decorators[0].DecoratorType);
    }

    [Fact]
    public void GivenTransformerWithShortName_WhenBinding_ThenTransformerIsResolved()
    {
        // Arrange — "ToUpper" should resolve to "ToUpperTransformer"
        var definition = new TokenDefinition { Content = "{Name}" };
        definition.AppendName("Name");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("ToUpper");
        definition.Decorators.Add(decorator);
        var token = new Token("{Name}", "Name", "", new FileLocation());

        // Act
        DecoratorBinder.Bind(definition, token, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(token.Decorators);
        Assert.True(token.Decorators[0].IsTransformer);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DecoratorBinderTests"`
Expected: FAIL — `DecoratorBinder` class does not exist.

- [ ] **Step 3: Implement DecoratorBinder**

Create `src/Tokenizer/Compilation/Binders/DecoratorBinder.cs`:

```csharp
using System.Collections.Concurrent;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Tokens.Exceptions;
using Tokens.Transformers;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Resolves decorator definitions against a <see cref="DecoratorRegistry"/> and creates
/// <see cref="TokenDecoratorContext"/> instances on the target <see cref="Token"/>.
/// </summary>
internal static class DecoratorBinder
{
    public static void Bind(TokenDefinition definition, Token token,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        IDiagnosticCollector collector)
    {
        if (string.IsNullOrEmpty(definition.Value) == false)
        {
            var setContext = new TokenDecoratorContext(typeof(SetTransformer), decoratorCache);
            setContext.AddParameter(definition.Value);
            token.AddDecorator(setContext);

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.DecoratorApplied,
                    tokenName: token.Name,
                    decoratorName: nameof(SetTransformer),
                    detail: definition.Value);
            }
        }

        foreach (var decorator in definition.Decorators)
        {
            if (TryApplyConcatenation(definition.Name ?? string.Empty, decorator, token, collector))
                continue;

            if (TryApplyTransformer(definition, decorator, token, registry, decoratorCache, collector))
                continue;

            if (TryApplyValidator(definition, decorator, token, registry, decoratorCache, collector))
                continue;

            throw new TokenizerException($"Unknown Token Operation: {decorator.Name}");
        }

        ValidateFrontMatterToken(definition, token);
    }

    private static bool TryApplyConcatenation(string tokenName, DecoratorDefinition decorator, Token token, IDiagnosticCollector collector)
    {
        if (!string.Equals("concat", decorator.Name, StringComparison.InvariantCultureIgnoreCase))
            return false;

        if (decorator.Args.Count > 1)
        {
            throw new TokenizerException($"Token '{tokenName}' Concat() must have a single argument.");
        }

        token.CanConcatenate = true;

        if (decorator.Args.Count == 1)
        {
            token.ConcatenationString = decorator.Args[0];
        }

        if (collector.IsEnabled)
        {
            collector.Record(DiagnosticEventType.ConcatenationApplied,
                tokenName: tokenName,
                detail: token.ConcatenationString ?? "(empty)");
        }

        return true;
    }

    private static bool TryApplyTransformer(TokenDefinition definition, DecoratorDefinition decorator, Token token,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        IDiagnosticCollector collector)
    {
        foreach (var transformerType in registry.Transformers)
        {
            if (string.Equals(decorator.Name, transformerType.Name, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals($"{decorator.Name}Transformer", transformerType.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                if (decorator.IsNotDecorator)
                {
                    throw new TokenizerException($"{decorator.Name} cannot be prefixed with '!' character.");
                }

                var context = new TokenDecoratorContext(transformerType, decoratorCache);
                foreach (var arg in decorator.Args)
                {
                    context.AddParameter(arg);
                }

                token.AddDecorator(context);

                if (collector.IsEnabled)
                {
                    collector.Record(DiagnosticEventType.DecoratorApplied,
                        tokenName: token.Name,
                        decoratorName: transformerType.Name,
                        decoratorArgs: decorator.Args.ToArray());
                }

                return true;
            }
        }

        return false;
    }

    private static bool TryApplyValidator(TokenDefinition definition, DecoratorDefinition decorator, Token token,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        IDiagnosticCollector collector)
    {
        foreach (var validatorType in registry.Validators)
        {
            if (string.Equals(decorator.Name, validatorType.Name, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals($"{decorator.Name}Validator", validatorType.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                var context = new TokenDecoratorContext(validatorType, decoratorCache);
                foreach (var arg in decorator.Args)
                {
                    context.AddParameter(arg);
                }

                context.IsNotValidator = decorator.IsNotDecorator;
                token.AddDecorator(context);

                if (collector.IsEnabled)
                {
                    collector.Record(DiagnosticEventType.DecoratorApplied,
                        tokenName: token.Name,
                        decoratorName: validatorType.Name,
                        decoratorArgs: decorator.Args.ToArray());
                }

                return true;
            }
        }

        return false;
    }

    private static void ValidateFrontMatterToken(TokenDefinition definition, Token token)
    {
        if (definition.IsFrontMatterToken)
        {
            var hasSetTransformer = token.Decorators.Any(d => d.DecoratorType == typeof(SetTransformer));
            if (hasSetTransformer == false)
            {
                throw new TokenizerException($"Front Matter Token '{definition.Name}' must have an assignment operation.");
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DecoratorBinderTests"`
Expected: All 13 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/DecoratorBinder.cs tests/Tokenizer.Tests/Compilation/Binders/DecoratorBinderTests.cs
git commit -m "feat: extract DecoratorBinder from TemplateCompiler"
```

---

### Task 11: Extract RepeatingTokenLinker

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/RepeatingTokenLinker.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/RepeatingTokenLinkerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/RepeatingTokenLinkerTests.cs`:

```csharp
using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Compilation.Binders;

public class RepeatingTokenLinkerTests
{
    [Fact]
    public void GivenRepeatingTokenAfterNonRepeatingWithSameName_WhenLinking_ThenDependsOnIdIsSet()
    {
        // Arrange
        var nonRepeating = new Token("{Name}", "Name", "Preamble\n", new FileLocation());
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        var template = new TemplateBuilder()
            .WithTokens(nonRepeating, repeating)
            .Build();

        // Act
        RepeatingTokenLinker.Link(repeating, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(nonRepeating.Id, repeating.DependsOnId);
    }

    [Fact]
    public void GivenRepeatingTokenWithDifferentNameFromPrevious_WhenLinking_ThenDependsOnIdUnchanged()
    {
        // Arrange
        var nonRepeating = new Token("{Other}", "Other", "Preamble", new FileLocation());
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        var template = new TemplateBuilder()
            .WithTokens(nonRepeating, repeating)
            .Build();

        // Act
        RepeatingTokenLinker.Link(repeating, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(-1, repeating.DependsOnId);
    }

    [Fact]
    public void GivenNonRepeatingToken_WhenLinking_ThenNothingHappens()
    {
        // Arrange
        var token = new Token("{Name}", "Name", "Preamble", new FileLocation());
        token.IsRepeating = false;
        var template = new TemplateBuilder()
            .WithTokens(token)
            .Build();

        // Act
        RepeatingTokenLinker.Link(token, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(-1, token.DependsOnId);
    }

    [Fact]
    public void GivenRepeatingTokenAlreadyLinked_WhenLinking_ThenDependsOnIdUnchanged()
    {
        // Arrange
        var nonRepeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        repeating.DependsOnId = 99;
        var template = new TemplateBuilder()
            .WithTokens(nonRepeating, repeating)
            .Build();

        // Act
        RepeatingTokenLinker.Link(repeating, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(99, repeating.DependsOnId);
    }

    [Fact]
    public void GivenOnlyOneTokenInTemplate_WhenLinking_ThenNothingHappens()
    {
        // Arrange
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        var template = new TemplateBuilder()
            .WithTokens(repeating)
            .Build();

        // Act
        RepeatingTokenLinker.Link(repeating, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(-1, repeating.DependsOnId);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenLinkingSucceeds_ThenRecordsEvent()
    {
        // Arrange
        var nonRepeating = new Token("{Name}", "Name", "Preamble\n", new FileLocation());
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        var template = new TemplateBuilder()
            .WithTokens(nonRepeating, repeating)
            .Build();
        var collector = new DiagnosticCollector(null, null);

        // Act
        RepeatingTokenLinker.Link(repeating, template, collector);

        // Assert
        var diagnostics = collector.GetResult()!;
        Assert.Single(diagnostics.Events);
        Assert.Equal(DiagnosticEventType.RepeatingTokenLinked, diagnostics.Events[0].Type);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "RepeatingTokenLinkerTests"`
Expected: FAIL — `RepeatingTokenLinker` class does not exist.

- [ ] **Step 3: Implement RepeatingTokenLinker**

Create `src/Tokenizer/Compilation/Binders/RepeatingTokenLinker.cs`:

```csharp
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Links repeating tokens to their non-repeating counterpart with the same name.
/// When the binder splits a Repeating token with a multiline preamble,
/// it produces a non-repeating token followed by a repeating one with the same name.
/// The repeating token should not match until the non-repeating one has been consumed.
/// </summary>
internal static class RepeatingTokenLinker
{
    public static void Link(Token token, Template template, IDiagnosticCollector collector)
    {
        if (!token.IsRepeating || token.DependsOnId != -1 || template.Tokens.Count < 2)
            return;

        var previous = template.Tokens.Last(t => t.Id != token.Id);

        if (previous.Name == token.Name && previous.IsRepeating == false)
        {
            token.DependsOnId = previous.Id;

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.RepeatingTokenLinked,
                    tokenName: token.Name,
                    tokenId: token.Id,
                    detail: $"Linked to token {previous.Id}");
            }
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "RepeatingTokenLinkerTests"`
Expected: All 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/RepeatingTokenLinker.cs tests/Tokenizer.Tests/Compilation/Binders/RepeatingTokenLinkerTests.cs
git commit -m "feat: extract RepeatingTokenLinker from TemplateCompiler"
```

---

### Task 12: Extract TokenBinder (orchestrator)

**Files:**
- Create: `src/Tokenizer/Compilation/Binders/TokenBinder.cs`
- Create: `tests/Tokenizer.Tests/Compilation/Binders/TokenBinderTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Compilation/Binders/TokenBinderTests.cs`:

```csharp
using System.Collections.Concurrent;
using Tokens.Builders;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TokenBinderTests
{
    private readonly DecoratorRegistry registry = new(new TokenizerOptions());
    private readonly ConcurrentDictionary<Type, ITokenDecorator> decoratorCache = new();

    [Fact]
    public void GivenDefinitionWithTokens_WhenBinding_ThenTemplateHasTokens()
    {
        // Arrange
        var definition = new TemplateDefinition();
        var tokenDef = new TokenDefinition { Content = "{Name}" };
        tokenDef.AppendName("Name");
        tokenDef.AppendPreamble("Preamble: ");
        definition.Tokens.Add(tokenDef);
        var template = new TemplateBuilder().Build();

        // Act
        TokenBinder.Bind(definition, template, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(template.Tokens);
        Assert.Equal("Name", template.Tokens.First().Name);
        Assert.Equal("Preamble: ", template.Tokens.First().Preamble);
    }

    [Fact]
    public void GivenMultipleTokenDefinitions_WhenBinding_ThenAllTokensBound()
    {
        // Arrange
        var definition = new TemplateDefinition();
        var td1 = new TokenDefinition { Content = "{First}" };
        td1.AppendName("First");
        td1.AppendPreamble("A: ");
        var td2 = new TokenDefinition { Content = "{Second}" };
        td2.AppendName("Second");
        td2.AppendPreamble("B: ");
        definition.Tokens.Add(td1);
        definition.Tokens.Add(td2);
        var template = new TemplateBuilder().Build();

        // Act
        TokenBinder.Bind(definition, template, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(2, template.Tokens.Count);
    }

    [Fact]
    public void GivenOutOfOrderOptions_WhenBinding_ThenTokensAreOptional()
    {
        // Arrange
        var definition = new TemplateDefinition
        {
            Options = new TokenizerOptions { OutOfOrderTokens = true }
        };
        var tokenDef = new TokenDefinition { Content = "{Name}" };
        tokenDef.AppendName("Name");
        definition.Tokens.Add(tokenDef);
        var template = new TemplateBuilder()
            .WithOptions(new TokenizerOptions { OutOfOrderTokens = true })
            .Build();

        // Act
        TokenBinder.Bind(definition, template, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(template.Tokens.First().IsOptional);
    }

    [Fact]
    public void GivenEmptyDefinition_WhenBinding_ThenTemplateHasNoTokens()
    {
        // Arrange
        var definition = new TemplateDefinition();
        var template = new TemplateBuilder().Build();

        // Act
        TokenBinder.Bind(definition, template, registry, decoratorCache, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Empty(template.Tokens);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenBinderTests"`
Expected: FAIL — `TokenBinder` class does not exist.

- [ ] **Step 3: Implement TokenBinder**

Create `src/Tokenizer/Compilation/Binders/TokenBinder.cs`:

```csharp
using System.Collections.Concurrent;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Orchestrates per-token compilation by delegating to focused sub-components.
/// </summary>
internal static class TokenBinder
{
    public static void Bind(TemplateDefinition definition, Template template,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        IDiagnosticCollector collector)
    {
        foreach (var tokenDef in definition.Tokens)
        {
            var token = TokenFactory.Create(tokenDef, template.Options, collector);
            OptionApplier.Apply(token, template.Options, collector);
            DecoratorBinder.Bind(tokenDef, token, registry, decoratorCache, collector);
            template.AddToken(token);
            RepeatingTokenLinker.Link(token, template, collector);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenBinderTests"`
Expected: All 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/TokenBinder.cs tests/Tokenizer.Tests/Compilation/Binders/TokenBinderTests.cs
git commit -m "feat: extract TokenBinder orchestrator from TemplateCompiler"
```

---

### Task 13: Rewrite TemplateCompiler as Orchestrator

**Files:**
- Modify: `src/Tokenizer/Compilation/TemplateCompiler.cs`
- Modify: `tests/Tokenizer.Tests/Compilation/TemplateCompilerTests.cs`

- [ ] **Step 1: Run all existing TemplateCompiler tests as baseline**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateCompilerTests"`
Expected: All 11 tests pass (baseline).

- [ ] **Step 2: Rewrite TemplateCompiler**

Replace the entire contents of `src/Tokenizer/Compilation/TemplateCompiler.cs` with:

```csharp
using System.Collections.Concurrent;
using Tokens.Compilation.Binders;
using Tokens.Compilation.Definitions;
using Tokens.Compilation.Parsing;
using Tokens.Diagnostics;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Compilation;

/// <summary>
/// Compiles template pattern strings into <see cref="Template"/> objects
/// that can be used to extract structured data from input text.
/// </summary>
internal class TemplateCompiler
{
    private readonly DecoratorRegistry registry;
    private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache = new();

    public TokenizerOptions Options { get; }

    public TemplateCompiler(TokenizerOptions options)
    {
        Options = options;
        registry = new DecoratorRegistry(options);
    }

    public Template Compile(string content)
    {
        IDiagnosticCollector collector = Options.EnableDiagnostics
            ? new DiagnosticCollector(content, null)
            : NullDiagnosticCollector.Instance;

        TemplateLengthValidator.Validate(content, Options);

        try
        {
            var definition = new AstTemplateDefinitionParser().Parse(content, Options);
            var id = content.ComputeHash();
            var template = TemplateFactory.Create(id, definition);

            HintBinder.Bind(definition, template, collector);
            TagBinder.Bind(definition, template, collector);
            TokenBinder.Bind(definition, template, registry, _decoratorCache, collector);
            TokenCountValidator.Validate(template, Options);

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.CompilationCompleted,
                    detail: $"Template '{template.Name}' compiled with {template.Tokens.Count} token(s)");
            }

            return template;
        }
        catch (TokenizerException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TokenizerException($"Unexpected error during template compilation: {ex.Message}", ex);
        }
    }
}
```

- [ ] **Step 3: Update TemplateCompiler constructor calls**

Search for any callers that pass an `ILogger<TemplateCompiler>` parameter. The `ILogger` parameter has been removed. Update callers to use the new single-parameter constructor `TemplateCompiler(TokenizerOptions)`.

Check `src/Tokenizer/Tokenizer.cs` for the construction site:

Run: `grep -n "new TemplateCompiler" src/Tokenizer/Tokenizer.cs`

Update the call to remove the logger parameter.

- [ ] **Step 4: Run existing TemplateCompiler tests**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateCompilerTests"`
Expected: All 11 tests pass.

- [ ] **Step 5: Run the full test suite**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Compilation/TemplateCompiler.cs src/Tokenizer/Tokenizer.cs
git commit -m "refactor: rewrite TemplateCompiler as thin orchestrator over binder classes"
```

---

### Task 14: Final Verification

**Files:** None (verification only)

- [ ] **Step 1: Build in Release mode**

Run: `dotnet build src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with no warnings.

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 3: Verify file layout**

Run: `ls src/Tokenizer/Compilation/Binders/`

Expected files:
```
DecoratorBinder.cs
FrontMatterBinder.cs
HintBinder.cs
OptionApplier.cs
RepeatingTokenLinker.cs
TagBinder.cs
TemplateBinder.cs (existing, unchanged)
TemplateFactory.cs
TemplateLengthValidator.cs
TokenBinder.cs
TokenCountValidator.cs
TokenFactory.cs
```

- [ ] **Step 4: Verify TemplateCompiler is slim**

Run: `wc -l src/Tokenizer/Compilation/TemplateCompiler.cs`
Expected: Under 60 lines.

- [ ] **Step 5: Commit any final cleanup**

If any fixes were needed, commit them:
```bash
git add -A
git commit -m "chore: final cleanup after template compiler restructure"
```
