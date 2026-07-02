# Tier 7: Template Compilation Caching — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add internal template compilation caching with LRU eviction, extract `ITokenizer`/`ITokenMatcher` interfaces, move transformer/validator registration to `TokenizerOptions`, add `TextReader` compilation overloads, and remove `Template.Content`.

**Architecture:** Instance-scoped `TemplateCache` on `Tokenizer` with `ConcurrentDictionary` keyed by SHA256 hash. String-based `Tokenize()` overloads check cache before compiling. `TextReader` overloads bypass cache. `TokenMatcher` takes `ITokenizer` dependency for shared compilation path. Registration of transformers/validators moves to `TokenizerOptions` to avoid duplication.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 6.0 dual-target, xUnit, BenchmarkDotNet

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `src/Tokenizer/TokenizerOptions.cs` | Modify | Add `CompilationCacheMaxSize`, transformer/validator registration |
| `src/Tokenizer/Template.cs` | Modify | Remove `Content`, fix `Name` auto-generation |
| `src/Tokenizer/Compilation/TemplateCache.cs` | Create | LRU cache with `ConcurrentDictionary`, SHA256 keys |
| `src/Tokenizer/ITokenizer.cs` | Create | Public interface for compilation + tokenization |
| `src/Tokenizer/Tokenizer.cs` | Modify | Implement `ITokenizer`, add `Compile()`, wire cache, remove registration methods |
| `src/Tokenizer/ITokenMatcher.cs` | Create | Public interface for multi-template matching |
| `src/Tokenizer/TokenMatcher.cs` | Modify | Implement `ITokenMatcher`, take `ITokenizer` dependency, remove registration |
| `src/Tokenizer/Compilation/TokenParser.cs` | Modify | Accept registrations from options, add `TextReader` overload |
| `src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs` | Modify | Register `ITokenizer`, `ITokenMatcher` |
| `src/Tokenizer/Diagnostics/DiagnosticCollector.cs` | Modify | Accept nullable template content |
| `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs` | Modify | Handle null template content |
| `src/Tokenizer/Diagnostics/AlignmentRenderer.cs` | Modify | Handle null template content |
| `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs` | Modify | Remove `WithContent`, update `Build()` |
| `tests/Tokenizer.Tests/TokenizerTests.cs` | Modify | Update for interface changes |
| `tests/Tokenizer.Tests/TokenMatcherTests.cs` | Modify | Update for new constructor |
| `tests/Tokenizer.Tests/TokenizerTestBase.cs` | Modify | Update helper methods |
| `tests/Tokenizer.Tests/Compilation/TemplateCacheTests.cs` | Create | Cache hit/miss/eviction/thread-safety tests |
| `tests/Tokenizer.Tests/Compilation/CompileApiTests.cs` | Create | `Compile()` method tests |
| `tests/Tokenizer.Tests/TokenizerOptionsRegistrationTests.cs` | Create | Options registration tests |
| `tests/Tokenizer.Tests/Extensions/TokenizerServiceCollectionExtensionsTests.cs` | Modify | DI registration tests |
| `benchmarks/Tokenizer.Benchmarks/Benchmarks/CompilationCacheBenchmarks.cs` | Create | Cache performance benchmarks |

---

### Task 1: Move transformer/validator registration to `TokenizerOptions`

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`
- Create: `tests/Tokenizer.Tests/TokenizerOptionsRegistrationTests.cs`

- [ ] **Step 1: Write failing tests for registration on options**

```csharp
// tests/Tokenizer.Tests/TokenizerOptionsRegistrationTests.cs
using Tokens.Transformers;
using Tokens.Validators;
using Xunit;

namespace Tokens;

public class TokenizerOptionsRegistrationTests
{
    [Fact]
    public void GivenNewOptions_WhenRegisteringTransformer_ThenTransformerTypeIsStored()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        options.RegisterTransformer<ToUpperTransformer>();

        // Assert
        Assert.Contains(typeof(ToUpperTransformer), options.Transformers);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringValidator_ThenValidatorTypeIsStored()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        options.RegisterValidator<IsNumericValidator>();

        // Assert
        Assert.Contains(typeof(IsNumericValidator), options.Validators);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringTransformer_ThenReturnsSameOptionsForChaining()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = options.RegisterTransformer<ToUpperTransformer>();

        // Assert
        Assert.Same(options, result);
    }

    [Fact]
    public void GivenNewOptions_WhenCheckingDefaults_ThenCompilationCacheMaxSizeIs500()
    {
        // Arrange & Act
        var options = new TokenizerOptions();

        // Assert
        Assert.Equal(500, options.CompilationCacheMaxSize);
    }

    [Fact]
    public void GivenNewOptions_WhenSettingCacheMaxSizeToZero_ThenCachingIsDisabled()
    {
        // Arrange & Act
        var options = new TokenizerOptions { CompilationCacheMaxSize = 0 };

        // Assert
        Assert.Equal(0, options.CompilationCacheMaxSize);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsRegistrationTests"`
Expected: FAIL — `Transformers`, `Validators`, `RegisterTransformer`, `RegisterValidator`, `CompilationCacheMaxSize` do not exist on `TokenizerOptions`

- [ ] **Step 3: Add registration methods and properties to `TokenizerOptions`**

Add to `src/Tokenizer/TokenizerOptions.cs` before the closing brace:

```csharp
/// <summary>
/// Maximum number of compiled templates to cache. Default: 500.
/// Set to 0 to disable compilation caching.
/// </summary>
public int CompilationCacheMaxSize { get; init; } = 500;

/// <summary>
/// Custom transformer types registered for use in template patterns.
/// </summary>
public List<Type> Transformers { get; } = new();

/// <summary>
/// Custom validator types registered for use in template patterns.
/// </summary>
public List<Type> Validators { get; } = new();

/// <summary>
/// Registers a custom transformer type for use in template patterns.
/// </summary>
/// <typeparam name="T">The transformer type. Must implement <see cref="Transformers.ITokenTransformer"/>.</typeparam>
/// <returns>This options instance for method chaining.</returns>
public TokenizerOptions RegisterTransformer<T>() where T : Transformers.ITokenTransformer
{
    Transformers.Add(typeof(T));
    return this;
}

/// <summary>
/// Registers a custom validator type for use in template patterns.
/// </summary>
/// <typeparam name="T">The validator type. Must implement <see cref="Validators.ITokenValidator"/>.</typeparam>
/// <returns>This options instance for method chaining.</returns>
public TokenizerOptions RegisterValidator<T>() where T : Validators.ITokenValidator
{
    Validators.Add(typeof(T));
    return this;
}
```

Add usings at the top of `TokenizerOptions.cs`:

```csharp
using Tokens.Transformers;
using Tokens.Validators;
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsRegistrationTests"`
Expected: PASS (all 5 tests)

- [ ] **Step 5: Update `TokenParser` to read registrations from options**

In `src/Tokenizer/Compilation/TokenParser.cs`, at the end of the constructor (after line 89), add:

```csharp
// Register custom transformers/validators from options
foreach (var transformerType in options.Transformers)
{
    if (!transformers.Contains(transformerType))
    {
        transformers.Add(transformerType);
        log.LogDebug("Registered custom transformer from options: {TransformerType}", transformerType.Name);
    }
}

foreach (var validatorType in options.Validators)
{
    if (!validators.Contains(validatorType))
    {
        validators.Add(validatorType);
        log.LogDebug("Registered custom validator from options: {ValidatorType}", validatorType.Name);
    }
}
```

- [ ] **Step 6: Run all tests to verify no regressions**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS

- [ ] **Step 7: Commit**

```bash
git add tests/Tokenizer.Tests/TokenizerOptionsRegistrationTests.cs src/Tokenizer/TokenizerOptions.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add transformer/validator registration to TokenizerOptions"
```

---

### Task 2: Remove `Template.Content` and fix `Name` auto-generation

**Files:**
- Modify: `src/Tokenizer/Template.cs`
- Modify: `src/Tokenizer/Compilation/TokenParser.cs`
- Modify: `src/Tokenizer/Diagnostics/DiagnosticCollector.cs`
- Modify: `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs`
- Modify: `src/Tokenizer/Diagnostics/AlignmentRenderer.cs`
- Modify: `src/Tokenizer/Tokenizer.cs:166` (DiagnosticCollector call)
- Modify: `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs`

- [ ] **Step 1: Update `Template` — remove `Content`, fix `Name` auto-gen**

In `src/Tokenizer/Template.cs`:

Replace the `using` and class fields/constructors (lines 1-38) with:

```csharp
namespace Tokens;

/// <summary>
/// Represents a template to use to extract data from
/// free text.
/// </summary>
public sealed class Template
{
    private static int templateCounter;

    private readonly List<Token> tokens;
    private readonly List<Hint> hints;
    private readonly List<string> tags;
    private string name;

    /// <summary>
    /// Creates a new unnamed template.
    /// An auto-generated name will be assigned.
    /// </summary>
    public Template() : this(string.Empty)
    {
    }

    /// <summary>
    /// Creates a new template with the given name.
    /// </summary>
    /// <param name="name">A name that identifies this template.</param>
    public Template(string name)
    {
        tokens = new List<Token>();
        hints = new List<Hint>();
        tags = new List<string>();
        Options = new TokenizerOptions();
        this.name = name;
    }
```

Replace the `Content` and `Name` properties (lines 40-61) with:

```csharp
    /// <summary>
    /// The name of the template. If no name was specified, an auto-generated name is assigned.
    /// </summary>
    public string Name
    {
        get
        {
            if (string.IsNullOrEmpty(name))
            {
                name = $"Template_{Interlocked.Increment(ref templateCounter)}";
            }

            return name;
        }
        set => name = value;
    }
```

Remove the `using Tokens.Extensions;` at line 1 (no longer needed for `ToMd5`).

Add `using System.Threading;` at the top if not already present (for `Interlocked`). Note: `System.Threading` is in the global namespace for modern .NET, but check if needed for netstandard2.0.

- [ ] **Step 2: Fix `TokenParser.Parse` — remove `content` param from `Template` constructor**

In `src/Tokenizer/Compilation/TokenParser.cs` line 137, change:

```csharp
var template = new Template(name, content);
```

to:

```csharp
var template = new Template(name);
```

- [ ] **Step 3: Fix diagnostics to handle missing template content**

In `src/Tokenizer/Diagnostics/DiagnosticCollector.cs` line 18, change the constructor parameter type:

```csharp
public DiagnosticCollector(string? templateContent, string inputContent)
```

In `src/Tokenizer/Diagnostics/TokenizationDiagnostics.cs` lines 21 and 26, change:

```csharp
private readonly string? templateContent;
```

```csharp
internal TokenizationDiagnostics(string? templateContent, string inputContent)
```

In `src/Tokenizer/Diagnostics/AlignmentRenderer.cs` line 7, change:

```csharp
public static string Render(TokenizationDiagnostics diagnostics, string? templateContent, string inputContent)
```

And at line 26, guard against null:

```csharp
var inputLineCount = CountLines(inputContent);
```

No change needed — `templateContent` is only passed through to `AlignmentRenderer` which already doesn't use it for rendering the current output format (it uses diagnostic events, not raw template content).

In `src/Tokenizer/Tokenizer.cs` line 166, change:

```csharp
? new DiagnosticCollector(template.Content, input)
```

to:

```csharp
? new DiagnosticCollector(null, input)
```

- [ ] **Step 4: Update `TemplateBuilder`**

In `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs`:

Remove the `_content` field (line 15) and the `WithContent` method (lines 24-28).

Change the `Build()` method (line 76) from:

```csharp
var template = new Template(_name, _content);
```

to:

```csharp
var template = new Template(_name);
```

- [ ] **Step 5: Fix any remaining compilation errors**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with no errors

If there are remaining references to `Template.Content` or the two-argument `Template` constructor, fix them.

- [ ] **Step 6: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS. Some tests may need updating if they use `TemplateBuilder.WithContent()` or the `Template(name, content)` constructor — fix any failures.

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Template.cs src/Tokenizer/Compilation/TokenParser.cs src/Tokenizer/Tokenizer.cs src/Tokenizer/Diagnostics/ tests/Tokenizer.Tests/Builders/TemplateBuilder.cs
git commit -m "Remove Template.Content, use auto-generated names"
```

---

### Task 3: Create `TemplateCache` internal class

**Files:**
- Create: `src/Tokenizer/Compilation/TemplateCache.cs`
- Create: `tests/Tokenizer.Tests/Compilation/TemplateCacheTests.cs`

- [ ] **Step 1: Write failing tests for `TemplateCache`**

```csharp
// tests/Tokenizer.Tests/Compilation/TemplateCacheTests.cs
using System.Collections.Concurrent;
using Tokens.Compilation;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TemplateCacheTests : TokenizerTestBase
{
    public TemplateCacheTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenEmptyCache_WhenGettingTemplate_ThenCompileFuncIsCalled()
    {
        // Arrange
        var cache = new TemplateCache(10);
        var compiled = false;

        // Act
        var template = cache.GetOrAdd("pattern", _ =>
        {
            compiled = true;
            return new Template("test");
        });

        // Assert
        Assert.True(compiled);
        Assert.Equal("test", template.Name);
    }

    [Fact]
    public void GivenCachedTemplate_WhenGettingSamePattern_ThenCompileFuncIsNotCalled()
    {
        // Arrange
        var cache = new TemplateCache(10);
        cache.GetOrAdd("pattern", _ => new Template("first"));

        // Act
        var compiled = false;
        var template = cache.GetOrAdd("pattern", _ =>
        {
            compiled = true;
            return new Template("second");
        });

        // Assert
        Assert.False(compiled);
        Assert.Equal("first", template.Name);
    }

    [Fact]
    public void GivenDifferentPatterns_WhenGetting_ThenEachCompiledSeparately()
    {
        // Arrange
        var cache = new TemplateCache(10);

        // Act
        var t1 = cache.GetOrAdd("pattern1", _ => new Template("first"));
        var t2 = cache.GetOrAdd("pattern2", _ => new Template("second"));

        // Assert
        Assert.Equal("first", t1.Name);
        Assert.Equal("second", t2.Name);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void GivenFullCache_WhenAddingNew_ThenLeastRecentlyUsedIsEvicted()
    {
        // Arrange
        var cache = new TemplateCache(2);
        cache.GetOrAdd("oldest", _ => new Template("oldest"));
        cache.GetOrAdd("newer", _ => new Template("newer"));

        // Touch "newer" to make "oldest" the LRU
        cache.GetOrAdd("newer", _ => new Template("should-not-compile"));

        // Act — this should evict "oldest"
        cache.GetOrAdd("newest", _ => new Template("newest"));

        // Assert
        Assert.Equal(2, cache.Count);

        // "oldest" was evicted, so recompiling should call the func
        var recompiled = false;
        cache.GetOrAdd("oldest", _ =>
        {
            recompiled = true;
            return new Template("recompiled");
        });
        Assert.True(recompiled);
    }

    [Fact]
    public void GivenCacheWithEntries_WhenClearing_ThenCacheIsEmpty()
    {
        // Arrange
        var cache = new TemplateCache(10);
        cache.GetOrAdd("a", _ => new Template("a"));
        cache.GetOrAdd("b", _ => new Template("b"));

        // Act
        cache.Clear();

        // Assert
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void GivenZeroMaxSize_WhenGetting_ThenNeverCaches()
    {
        // Arrange
        var cache = new TemplateCache(0);
        cache.GetOrAdd("pattern", _ => new Template("first"));

        // Act
        var compiled = false;
        cache.GetOrAdd("pattern", _ =>
        {
            compiled = true;
            return new Template("second");
        });

        // Assert
        Assert.True(compiled);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void GivenCache_WhenAccessedFromMultipleThreads_ThenNoExceptions()
    {
        // Arrange
        var cache = new TemplateCache(50);
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, 100, i =>
        {
            try
            {
                var pattern = $"pattern {i % 20}";
                cache.GetOrAdd(pattern, p => new Template($"template-{p}"));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.Empty(exceptions);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateCacheTests"`
Expected: FAIL — `TemplateCache` does not exist

- [ ] **Step 3: Implement `TemplateCache`**

```csharp
// src/Tokenizer/Compilation/TemplateCache.cs
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Tokens.Compilation;

/// <summary>
/// Thread-safe compilation cache with LRU eviction.
/// Keys are SHA256 hashes of template pattern strings.
/// </summary>
internal sealed class TemplateCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> cache = new();
    private readonly int maxSize;

    public TemplateCache(int maxSize)
    {
        this.maxSize = maxSize;
    }

    public int Count => cache.Count;

    public Template GetOrAdd(string pattern, Func<string, Template> compile)
    {
        if (maxSize <= 0)
        {
            return compile(pattern);
        }

        var key = ComputeHash(pattern);

        if (cache.TryGetValue(key, out var existing))
        {
            Interlocked.Exchange(ref existing.LastAccessed, GetTimestamp());
            return existing.Template;
        }

        var template = compile(pattern);
        var entry = new CacheEntry { Template = template, LastAccessed = GetTimestamp() };

        if (cache.TryAdd(key, entry))
        {
            EvictIfOverCapacity();
        }

        return template;
    }

    public void Clear()
    {
        cache.Clear();
    }

    private void EvictIfOverCapacity()
    {
        while (cache.Count > maxSize)
        {
            var oldest = default(KeyValuePair<string, CacheEntry>);
            var oldestTime = long.MaxValue;

            foreach (var kvp in cache)
            {
                var accessed = Interlocked.Read(ref kvp.Value.LastAccessed);
                if (accessed < oldestTime)
                {
                    oldestTime = accessed;
                    oldest = kvp;
                }
            }

            if (oldest.Key != null)
            {
                cache.TryRemove(oldest.Key, out _);
            }
        }
    }

    private static long GetTimestamp()
    {
#if NET6_0_OR_GREATER
        return Environment.TickCount64;
#else
        return DateTime.UtcNow.Ticks;
#endif
    }

    private static string ComputeHash(string input)
    {
#if NET6_0_OR_GREATER
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
#else
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
#endif
    }

    private sealed class CacheEntry
    {
        public Template Template { get; init; } = null!;
        public long LastAccessed;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateCacheTests"`
Expected: PASS (all 7 tests)

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/TemplateCache.cs tests/Tokenizer.Tests/Compilation/TemplateCacheTests.cs
git commit -m "Add TemplateCache with LRU eviction and SHA256 keys"
```

---

### Task 4: Extract `ITokenizer` interface and add `Compile()` methods

**Files:**
- Create: `src/Tokenizer/ITokenizer.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`
- Create: `tests/Tokenizer.Tests/Compilation/CompileApiTests.cs`

- [ ] **Step 1: Write failing tests for `Compile()` methods**

```csharp
// tests/Tokenizer.Tests/Compilation/CompileApiTests.cs
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class CompileApiTests : TokenizerTestBase
{
    private readonly ITokenizer tokenizer;

    public CompileApiTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenPattern_WhenCompiling_ThenReturnsTemplateWithTokens()
    {
        // Arrange
        const string pattern = "Name: {Name}";

        // Act
        var template = tokenizer.Compile(pattern);

        // Assert
        Assert.NotNull(template);
        Assert.Single(template.Tokens);
    }

    [Fact]
    public void GivenPatternAndName_WhenCompiling_ThenTemplateHasExplicitName()
    {
        // Arrange
        const string pattern = "Name: {Name}";

        // Act
        var template = tokenizer.Compile(pattern, "my-template");

        // Assert
        Assert.Equal("my-template", template.Name);
    }

    [Fact]
    public void GivenTextReader_WhenCompiling_ThenReturnsTemplateWithTokens()
    {
        // Arrange
        using var reader = new StringReader("Name: {Name}");

        // Act
        var template = tokenizer.Compile(reader);

        // Assert
        Assert.NotNull(template);
        Assert.Single(template.Tokens);
    }

    [Fact]
    public void GivenTextReaderAndName_WhenCompiling_ThenTemplateHasExplicitName()
    {
        // Arrange
        using var reader = new StringReader("Name: {Name}");

        // Act
        var template = tokenizer.Compile(reader, "reader-template");

        // Assert
        Assert.Equal("reader-template", template.Name);
    }

    [Fact]
    public void GivenSamePatternCompiledTwice_WhenUsingStringOverload_ThenCacheReturnsSameTemplate()
    {
        // Arrange
        const string pattern = "Name: {Name}";

        // Act
        var t1 = tokenizer.Compile(pattern);
        var t2 = tokenizer.Compile(pattern);

        // Assert
        Assert.Same(t1, t2);
    }

    [Fact]
    public void GivenTextReaderCompilation_WhenCompiledTwice_ThenCacheIsNotUsed()
    {
        // Arrange & Act
        var t1 = tokenizer.Compile(new StringReader("Name: {Name}"));
        var t2 = tokenizer.Compile(new StringReader("Name: {Name}"));

        // Assert — different instances because TextReader bypasses cache
        Assert.NotSame(t1, t2);
    }

    [Fact]
    public void GivenTokenizer_WhenClearingCache_ThenNextCompileReturnsNewInstance()
    {
        // Arrange
        const string pattern = "Name: {Name}";
        var t1 = tokenizer.Compile(pattern);

        // Act
        tokenizer.ClearCompilationCache();
        var t2 = tokenizer.Compile(pattern);

        // Assert
        Assert.NotSame(t1, t2);
    }

    [Fact]
    public void GivenCachingDisabled_WhenCompilingSamePattern_ThenReturnsNewInstanceEachTime()
    {
        // Arrange
        var noCacheTokenizer = CreateTokenizer(new TokenizerOptions { CompilationCacheMaxSize = 0 });
        const string pattern = "Name: {Name}";

        // Act
        var t1 = noCacheTokenizer.Compile(pattern);
        var t2 = noCacheTokenizer.Compile(pattern);

        // Assert
        Assert.NotSame(t1, t2);
    }

    [Fact]
    public void GivenCustomTransformersOnOptions_WhenCompiling_ThenTransformersAreAvailable()
    {
        // Arrange
        const string pattern = "Name: {Name : Trim}";

        // Act
        var template = tokenizer.Compile(pattern);

        // Assert — Trim is a built-in transformer, so compilation should succeed
        Assert.NotNull(template);
        Assert.Single(template.Tokens);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "CompileApiTests"`
Expected: FAIL — `ITokenizer` does not exist, `Compile()` methods do not exist

- [ ] **Step 3: Create `ITokenizer` interface**

```csharp
// src/Tokenizer/ITokenizer.cs
namespace Tokens;

/// <summary>
/// Interface for compiling template patterns and tokenizing input strings.
/// </summary>
public interface ITokenizer
{
    /// <summary>Gets the options.</summary>
    TokenizerOptions Options { get; }

    /// <summary>
    /// Compiles a template pattern string into a reusable <see cref="Template"/>.
    /// Uses the compilation cache for string overloads.
    /// </summary>
    Template Compile(string pattern);

    /// <summary>
    /// Compiles a template pattern string with an explicit name.
    /// Uses the compilation cache for string overloads.
    /// </summary>
    Template Compile(string pattern, string name);

    /// <summary>
    /// Compiles a template from a <see cref="TextReader"/>. Bypasses the compilation cache.
    /// </summary>
    Template Compile(TextReader reader);

    /// <summary>
    /// Compiles a template from a <see cref="TextReader"/> with an explicit name. Bypasses the compilation cache.
    /// </summary>
    Template Compile(TextReader reader, string name);

    /// <summary>
    /// Parses the given template pattern and tokenizes the input string against it.
    /// </summary>
    TokenizeResult Tokenize(string template, string input);

    /// <summary>
    /// Tokenizes the input string using a pre-compiled template.
    /// </summary>
    TokenizeResult Tokenize(Template template, string input);

    /// <summary>
    /// Parses the given pattern and tokenizes the input, mapping values onto a new instance of <typeparamref name="T"/>.
    /// </summary>
    TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new();

    /// <summary>
    /// Tokenizes the input using a pre-compiled template, mapping values onto a new instance of <typeparamref name="T"/>.
    /// </summary>
    TokenizeResult<T> Tokenize<T>(Template template, string input) where T : class, new();

    /// <summary>
    /// Clears the compilation cache.
    /// </summary>
    void ClearCompilationCache();
}
```

- [ ] **Step 4: Implement `ITokenizer` on `Tokenizer`, add `Compile()`, wire cache**

In `src/Tokenizer/Tokenizer.cs`:

Change class declaration (line 17) to:

```csharp
public sealed class Tokenizer : ITokenizer
```

Add a `TemplateCache` field after the existing fields (after line 23):

```csharp
private readonly TemplateCache compilationCache;
```

Add `using Tokens.Compilation;` to the usings if not already present.

In the public constructor (lines 45-55), add after `resultBuilder` initialization:

```csharp
compilationCache = new TemplateCache(Options.CompilationCacheMaxSize);
```

In the internal DI constructor (lines 60-74), add after the last assignment:

```csharp
compilationCache = new TemplateCache(Options.CompilationCacheMaxSize);
```

Add the `Compile()` methods and `ClearCompilationCache()` before the private `Tokenize` method:

```csharp
/// <inheritdoc />
public Template Compile(string pattern)
{
    return compilationCache.GetOrAdd(pattern, p => parser.Parse(p));
}

/// <inheritdoc />
public Template Compile(string pattern, string name)
{
    return compilationCache.GetOrAdd(pattern, p => parser.Parse(p, name));
}

/// <inheritdoc />
public Template Compile(TextReader reader)
{
    return parser.Parse(reader);
}

/// <inheritdoc />
public Template Compile(TextReader reader, string name)
{
    return parser.Parse(reader, name);
}

/// <inheritdoc />
public void ClearCompilationCache()
{
    compilationCache.Clear();
}
```

Change the string-based `Tokenize` methods to use the cache. Replace lines 82-87:

```csharp
public TokenizeResult Tokenize(string template, string input)
{
    var t = Compile(template);

    return Tokenize(t, input);
}
```

Replace lines 113-118:

```csharp
public TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new()
{
    var template = Compile(pattern);

    return Tokenize<T>(template, input);
}
```

Remove the `RegisterTransformer<T>()` and `RegisterValidator<T>()` methods (lines 224-241).

- [ ] **Step 5: Add `TextReader` overloads to `TokenParser`**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the existing `Parse(string content, string name)` method (after line 259):

```csharp
public Template Parse(TextReader reader)
{
    var content = reader.ReadToEnd();
    var name = GenerateTemplateName(content);
    return Parse(content, name);
}

public Template Parse(TextReader reader, string name)
{
    var content = reader.ReadToEnd();
    return Parse(content, name);
}
```

Note: For v1, `TextReader` overloads read the full content into a string and delegate to the existing `Parse(string)` method. This is simpler than modifying the lexer pipeline and still provides the API surface. The key benefit is that callers don't hold the string — the `Template` doesn't store it. A future optimization could feed `TextReader` directly to the lexer.

- [ ] **Step 6: Update `TokenizerTestBase` to return `ITokenizer`**

In `tests/Tokenizer.Tests/TokenizerTestBase.cs`, update the helper methods:

Change `CreateTokenizer()` return type (but keep the implementation returning concrete `Tokenizer`):

```csharp
protected ITokenizer CreateTokenizer()
{
    return new Tokenizer(new TokenizerOptions(), LoggerFactory);
}

protected ITokenizer CreateTokenizer(TokenizerOptions options)
{
    return new Tokenizer(options, LoggerFactory);
}

protected ITokenizer CreateDiagnosticTokenizer()
{
    return CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
}
```

- [ ] **Step 7: Fix any test compilation errors caused by return type change**

Run: `dotnet build ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

Tests that use `Tokenizer`-specific methods (like `RegisterTransformer`) will need updating. Fix them to use options-based registration instead. For example, in `TokenizerTests.cs`, if any tests call `tokenizer.RegisterTransformer<T>()`, change them to pass the transformer via options.

- [ ] **Step 8: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS

- [ ] **Step 9: Commit**

```bash
git add src/Tokenizer/ITokenizer.cs src/Tokenizer/Tokenizer.cs src/Tokenizer/Compilation/TokenParser.cs tests/Tokenizer.Tests/Compilation/CompileApiTests.cs tests/Tokenizer.Tests/TokenizerTestBase.cs
git commit -m "Extract ITokenizer interface, add Compile() API with cache"
```

---

### Task 5: Extract `ITokenMatcher` interface and refactor `TokenMatcher`

**Files:**
- Create: `src/Tokenizer/ITokenMatcher.cs`
- Modify: `src/Tokenizer/TokenMatcher.cs`
- Modify: `tests/Tokenizer.Tests/TokenMatcherTests.cs`

- [ ] **Step 1: Create `ITokenMatcher` interface**

```csharp
// src/Tokenizer/ITokenMatcher.cs
namespace Tokens;

/// <summary>
/// Interface for matching input strings against a collection of templates to find the best match.
/// </summary>
public interface ITokenMatcher
{
    /// <summary>
    /// The collection of templates that will be matched against input strings.
    /// </summary>
    TemplateCollection Templates { get; }

    /// <summary>
    /// Registers a template from a pattern string.
    /// </summary>
    ITokenMatcher RegisterTemplate(string content);

    /// <summary>
    /// Registers a template from a pattern string with an explicit name.
    /// </summary>
    ITokenMatcher RegisterTemplate(string content, string name);

    /// <summary>
    /// Registers a template from a <see cref="TextReader"/>.
    /// </summary>
    ITokenMatcher RegisterTemplate(TextReader reader);

    /// <summary>
    /// Registers a template from a <see cref="TextReader"/> with an explicit name.
    /// </summary>
    ITokenMatcher RegisterTemplate(TextReader reader, string name);

    /// <summary>
    /// Registers a pre-compiled template.
    /// </summary>
    ITokenMatcher RegisterTemplate(Template template);

    /// <summary>
    /// Matches the input against all registered templates.
    /// </summary>
    TokenMatcherResult Match(string input);

    /// <summary>
    /// Matches the input against registered templates filtered by tags.
    /// </summary>
    TokenMatcherResult Match(string input, string[]? tags);

    /// <summary>
    /// Matches the input against all registered templates, populating a new <typeparamref name="T"/>.
    /// </summary>
    TokenMatcherResult<T> Match<T>(string input) where T : class, new();

    /// <summary>
    /// Matches the input against registered templates filtered by tags, populating a new <typeparamref name="T"/>.
    /// </summary>
    TokenMatcherResult<T> Match<T>(string input, string[]? tags) where T : class, new();
}
```

- [ ] **Step 2: Refactor `TokenMatcher` to implement `ITokenMatcher` and take `ITokenizer`**

Rewrite `src/Tokenizer/TokenMatcher.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Exceptions;

namespace Tokens;

/// <summary>
/// Matcher class that can hold multiple <see cref="Template"/> objects, and use
/// the best match to populate an object from an input string.
/// </summary>
public sealed class TokenMatcher : ITokenMatcher
{
    private readonly ITokenizer tokenizer;
    private readonly ILogger<TokenMatcher> log;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with default options.
    /// </summary>
    public TokenMatcher() : this(new TokenizerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with the specified options.
    /// </summary>
    public TokenMatcher(TokenizerOptions options) : this(new Tokenizer(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with the specified options and logger factory.
    /// </summary>
    public TokenMatcher(TokenizerOptions options, ILoggerFactory? loggerFactory)
        : this(new Tokenizer(options, loggerFactory), loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with an existing tokenizer.
    /// </summary>
    public TokenMatcher(ITokenizer tokenizer) : this(tokenizer, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenMatcher"/> with an existing tokenizer and logger factory.
    /// </summary>
    public TokenMatcher(ITokenizer tokenizer, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        this.tokenizer = tokenizer;
        log = loggerFactory.CreateLogger<TokenMatcher>();
        Templates = new TemplateCollection();
    }

    /// <inheritdoc />
    public TemplateCollection Templates { get; }

    /// <inheritdoc />
    public TokenMatcherResult Match(string input)
    {
        return Match(input, null);
    }

    /// <inheritdoc />
    public TokenMatcherResult Match(string input, string[]? tags)
    {
        if (tags == null) tags = Array.Empty<string>();

        var results = new TokenMatcherResult();

        foreach (var name in Templates.Names)
        {
            if (!Templates.TryGet(name, out var template)) continue;

            log.LogTrace("Start: Matching: {TemplateName}", template.Name);

            if (CheckTemplateTags(template, tags) == false)
            {
                continue;
            }

            try
            {
                var result = tokenizer.Tokenize(template, input);

                results.AddResult(result);

                log.LogTrace("Match Success: {Success}", result.Success);
                log.LogTrace("Total Matches: {MatchCount}", result.Tokens.Matches.Count);
                log.LogTrace("Total Errors : {ErrorCount}", result.Exceptions.Count);
            }
            catch (Exception e)
            {
                var exception = new TokenMatcherException(e.Message, template, e);

                log.LogError(e, "Error processing template: {TemplateName}", template.Name);

                throw exception;
            }

            log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
        }

        results.BestMatch = results.GetBestMatch();

        return results;
    }

    /// <inheritdoc />
    public TokenMatcherResult<T> Match<T>(string input) where T : class, new()
    {
        return Match<T>(input, null);
    }

    /// <inheritdoc />
    public TokenMatcherResult<T> Match<T>(string input, string[]? tags) where T : class, new()
    {
        if (tags == null) tags = Array.Empty<string>();

        var results = new TokenMatcherResult<T>();

        foreach (var name in Templates.Names)
        {
            if (!Templates.TryGet(name, out var template)) continue;

            log.LogTrace("Start: Matching: {TemplateName}", template.Name);

            if (CheckTemplateTags(template, tags) == false)
            {
                continue;
            }

            try
            {
                var result = tokenizer.Tokenize<T>(template, input);

                results.AddResult(result);

                log.LogTrace("Match Success: {Success}", result.Success);
                log.LogTrace("Total Matches: {MatchCount}", result.Tokens.Matches.Count);
                log.LogTrace("Total Errors : {ErrorCount}", result.Exceptions.Count);
            }
            catch (Exception e)
            {
                var exception = new TokenMatcherException(e.Message, template, e);

                log.LogError(e, "Error processing template: {TemplateName}", template.Name);

                throw exception;
            }

            log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
        }

        results.BestMatch = results.GetBestMatch();

        return results;
    }

    /// <inheritdoc />
    public ITokenMatcher RegisterTemplate(string content, string name)
    {
        var template = tokenizer.Compile(content, name);

        Templates.Add(template);

        return this;
    }

    /// <inheritdoc />
    public ITokenMatcher RegisterTemplate(string content)
    {
        var template = tokenizer.Compile(content);

        Templates.Add(template);

        return this;
    }

    /// <inheritdoc />
    public ITokenMatcher RegisterTemplate(TextReader reader)
    {
        var template = tokenizer.Compile(reader);

        Templates.Add(template);

        return this;
    }

    /// <inheritdoc />
    public ITokenMatcher RegisterTemplate(TextReader reader, string name)
    {
        var template = tokenizer.Compile(reader, name);

        Templates.Add(template);

        return this;
    }

    /// <inheritdoc />
    public ITokenMatcher RegisterTemplate(Template template)
    {
        Templates.Add(template);

        return this;
    }

    private bool CheckTemplateTags(Template template, string[] tags)
    {
        if (tags.Length == 0) return true;

        if (template.Tags.Any())
        {
            if (template.HasTags(tags, out var missing) == false)
            {
                log.LogTrace("No tags matching: {MissingTags}", missing);
                log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
                return false;
            }

            log.LogTrace("Found tag matching: {Tags}", string.Join(", ", tags));
            return true;
        }

        return false;
    }
}
```

- [ ] **Step 3: Update `TokenMatcherTests` for new constructors**

In `tests/Tokenizer.Tests/TokenMatcherTests.cs`:

The `matcher` field can stay as `TokenMatcher` (or change to `ITokenMatcher`). The key changes:
- Remove `parser` field — no longer needed, `TokenMatcher` compiles via its internal `ITokenizer`
- Update constructor: `matcher = new TokenMatcher();` still works (parameterless constructor)
- Tests that use `parser.Parse()` directly need updating to use `tokenizer.Compile()` or `matcher.RegisterTemplate()`
- Tests that call `matcher.RegisterTransformer<T>()` or `matcher.RegisterValidator<T>()` need to pass registrations via options

Update the constructor and field:

```csharp
private readonly ITokenMatcher matcher;

public TokenMatcherTests(ITestOutputHelper output) : base(output)
{
    matcher = new TokenMatcher();
}
```

For tests that use `parser.Parse()` to create templates and add them directly to `matcher.Templates`, change to use `matcher.RegisterTemplate(Template)` or create templates via a local tokenizer's `Compile()`.

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/ITokenMatcher.cs src/Tokenizer/TokenMatcher.cs tests/Tokenizer.Tests/TokenMatcherTests.cs
git commit -m "Extract ITokenMatcher interface, refactor TokenMatcher to use ITokenizer"
```

---

### Task 6: Update DI registration

**Files:**
- Modify: `src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs`
- Modify: `tests/Tokenizer.Tests/Extensions/TokenizerServiceCollectionExtensionsTests.cs` (if exists)

- [ ] **Step 1: Check for existing DI tests**

Run: `find /Users/work/Source/tokenizer/tests -name "*ServiceCollection*" -o -name "*DependencyInjection*"`

- [ ] **Step 2: Update `RegisterCoreServices` to register interfaces**

In `src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs`, update `RegisterCoreServices` (lines 71-114):

Change the `Tokenizer` registration (lines 102-113) to register as `ITokenizer`:

```csharp
services.TryAddSingleton<ITokenizer>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<TokenizerOptions>>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<Tokenizer>();
    var parser = sp.GetRequiredService<Compilation.TokenParser>();
    var tokenizationEngine = sp.GetRequiredService<ITokenizationEngine>();
    var hintProcessor = sp.GetRequiredService<IHintProcessor>();
    var resultBuilder = sp.GetRequiredService<IResultBuilder>();

    return new Tokenizer(opts, logger, parser, tokenizationEngine, hintProcessor, resultBuilder);
});
```

Add `ITokenMatcher` registration after the `ITokenizer` registration:

```csharp
services.TryAddSingleton<ITokenMatcher>(sp =>
{
    var tokenizer = sp.GetRequiredService<ITokenizer>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return new TokenMatcher(tokenizer, loggerFactory);
});
```

Also add a `Tokenizer` concrete registration that resolves from `ITokenizer` for backward compatibility during the transition:

```csharp
services.TryAddSingleton<Tokenizer>(sp => (Tokenizer)sp.GetRequiredService<ITokenizer>());
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs
git commit -m "Register ITokenizer and ITokenMatcher in DI container"
```

---

### Task 7: Remove registration methods from `Tokenizer`

This is done as a separate task after DI is updated to minimize the blast radius.

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs` (if not already done in Task 4)
- Modify: any remaining test files that call `tokenizer.RegisterTransformer<T>()`

- [ ] **Step 1: Verify `RegisterTransformer`/`RegisterValidator` are removed from `Tokenizer`**

These should already be removed in Task 4 Step 4. Verify:

Run: `grep -rn "RegisterTransformer\|RegisterValidator" src/Tokenizer/Tokenizer.cs`
Expected: No matches

- [ ] **Step 2: Find and fix any remaining callers**

Run: `grep -rn "\.RegisterTransformer\|\.RegisterValidator" tests/ benchmarks/`

For each caller:
- If it's on a `Tokenizer` instance: move registration to `TokenizerOptions`
- If it's on a `TokenMatcher` instance: move registration to `TokenizerOptions` passed to the constructor

- [ ] **Step 3: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS

- [ ] **Step 4: Commit**

```bash
git add -u
git commit -m "Remove RegisterTransformer/RegisterValidator from Tokenizer and TokenMatcher"
```

---

### Task 8: Add compilation cache benchmarks

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Benchmarks/CompilationCacheBenchmarks.cs`

- [ ] **Step 1: Create cache benchmarks**

```csharp
// benchmarks/Tokenizer.Benchmarks/Benchmarks/CompilationCacheBenchmarks.cs
using BenchmarkDotNet.Attributes;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures the impact of the compilation cache on tokenization performance.
/// Compares cached vs uncached vs pre-compiled template paths.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CompilationCacheBenchmarks
{
    private Tokenizer cachedTokenizer = null!;
    private Tokenizer uncachedTokenizer = null!;
    private Tokenizer precompiledTokenizer = null!;
    private Template precompiledSmall = null!;
    private Template precompiledMedium = null!;
    private Template precompiledLarge = null!;
    private string smallTemplate = null!;
    private string mediumTemplate = null!;
    private string largeTemplate = null!;
    private string smallInput = null!;
    private string mediumInput = null!;
    private string largeInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        cachedTokenizer = new Tokenizer();
        uncachedTokenizer = new Tokenizer(new TokenizerOptions { CompilationCacheMaxSize = 0 });
        precompiledTokenizer = new Tokenizer();

        smallTemplate = WorkloadGenerator.SmallTemplate();
        mediumTemplate = WorkloadGenerator.MediumTemplate();
        largeTemplate = WorkloadGenerator.LargeTemplate();

        smallInput = WorkloadGenerator.SmallInput();
        mediumInput = WorkloadGenerator.MediumInput();
        largeInput = WorkloadGenerator.LargeInput();

        // Pre-compile templates
        precompiledSmall = precompiledTokenizer.Compile(smallTemplate, "small");
        precompiledMedium = precompiledTokenizer.Compile(mediumTemplate, "medium");
        precompiledLarge = precompiledTokenizer.Compile(largeTemplate, "large");

        // Warm up the cached tokenizer
        cachedTokenizer.Compile(smallTemplate);
        cachedTokenizer.Compile(mediumTemplate);
        cachedTokenizer.Compile(largeTemplate);
    }

    // --- Cache hit (warm cache) ---

    [Benchmark(Description = "Cache hit: small (3 tokens)")]
    public TokenizeResult<SmallRecord> CacheHit_Small()
        => cachedTokenizer.Tokenize<SmallRecord>(smallTemplate, smallInput);

    [Benchmark(Description = "Cache hit: medium (12 tokens)")]
    public TokenizeResult<MediumRecord> CacheHit_Medium()
        => cachedTokenizer.Tokenize<MediumRecord>(mediumTemplate, mediumInput);

    [Benchmark(Description = "Cache hit: large (39 tokens)")]
    public TokenizeResult<LargeRecord> CacheHit_Large()
        => cachedTokenizer.Tokenize<LargeRecord>(largeTemplate, largeInput);

    // --- Pre-compiled (baseline, no cache lookup) ---

    [Benchmark(Description = "Pre-compiled: small (3 tokens)", Baseline = true)]
    public TokenizeResult<SmallRecord> PreCompiled_Small()
        => precompiledTokenizer.Tokenize<SmallRecord>(precompiledSmall, smallInput);

    [Benchmark(Description = "Pre-compiled: medium (12 tokens)")]
    public TokenizeResult<MediumRecord> PreCompiled_Medium()
        => precompiledTokenizer.Tokenize<MediumRecord>(precompiledMedium, mediumInput);

    [Benchmark(Description = "Pre-compiled: large (39 tokens)")]
    public TokenizeResult<LargeRecord> PreCompiled_Large()
        => precompiledTokenizer.Tokenize<LargeRecord>(precompiledLarge, largeInput);

    // --- Cache miss (no caching) ---

    [Benchmark(Description = "No cache: small (3 tokens)")]
    public TokenizeResult<SmallRecord> NoCache_Small()
        => uncachedTokenizer.Tokenize<SmallRecord>(smallTemplate, smallInput);

    [Benchmark(Description = "No cache: medium (12 tokens)")]
    public TokenizeResult<MediumRecord> NoCache_Medium()
        => uncachedTokenizer.Tokenize<MediumRecord>(mediumTemplate, mediumInput);

    [Benchmark(Description = "No cache: large (39 tokens)")]
    public TokenizeResult<LargeRecord> NoCache_Large()
        => uncachedTokenizer.Tokenize<LargeRecord>(largeTemplate, largeInput);

    // --- Concurrent cache access ---

    [Benchmark(Description = "Concurrent cache hit: 8 threads, large")]
    public void ConcurrentCacheHit()
    {
        Parallel.For(0, 8, _ =>
        {
            cachedTokenizer.Tokenize<LargeRecord>(largeTemplate, largeInput);
        });
    }
}
```

- [ ] **Step 2: Verify benchmarks build**

Run: `dotnet build ./benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release`
Expected: Build succeeds

- [ ] **Step 3: Run a quick smoke test (not full benchmark)**

Run: `dotnet run --project ./benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release -- --filter "*CacheHit_Small*" --job short`

This validates the benchmark runs without errors. Full benchmark runs are done separately.

- [ ] **Step 4: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Benchmarks/CompilationCacheBenchmarks.cs
git commit -m "Add compilation cache benchmarks"
```

---

### Task 9: Run full test suite and benchmark baseline

- [ ] **Step 1: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests PASS

- [ ] **Step 2: Build release**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with no warnings

- [ ] **Step 3: Update ROADMAP.md**

Mark Tier 7 items as complete in `docs/ROADMAP.md`:

```markdown
## Tier 7: Template Compilation Caching

Prevent repeated parsing of the same template pattern.

- [x] **Introduce internal compilation cache** — `ConcurrentDictionary<string, Template>` behind the string-overload `Tokenize()` methods
- [x] **Expose `Compile()` API** — let users explicitly compile a template for reuse, making the `CompiledTemplate` concept first-class
- [x] **Add compilation cache benchmarks** — measure the impact in the benchmark suite
```

- [ ] **Step 4: Commit**

```bash
git add docs/ROADMAP.md
git commit -m "Update ROADMAP.md: mark Tier 7 items complete"
```
