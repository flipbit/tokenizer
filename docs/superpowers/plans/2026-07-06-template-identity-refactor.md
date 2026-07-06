# Template Identity Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Decouple Template.Name from compilation and identity, introduce content-based Template.Id, remove TemplateCache, and simplify the ITokenizer/Tokenizer API surface.

**Architecture:** Template gets a `ulong Id` computed from the raw pattern string hash. Name becomes a user-facing label set by TemplateCompiler (from front matter or atomic counter). TemplateCollection rekeys by Id. TemplateCache is removed. String-based Compile/Tokenize overloads are removed from ITokenizer.

**Tech Stack:** C# / .NET Standard 2.0 + .NET 8.0 dual target, xUnit, XxHash64 / FNV-1a

---

### Task 1: Add String Hash Extension Method

**Files:**
- Create: `src/Tokenizer/Extensions/StringHashExtensions.cs`
- Create: `tests/Tokenizer.Tests/Extensions/StringHashExtensionTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Tokenizer.Tests/Extensions/StringHashExtensionTests.cs
using Tokens.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class StringHashExtensionTests : TokenizerTestBase
{
    public StringHashExtensionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenSameString_WhenComputingHash_ThenReturnsSameValue()
    {
        // Arrange
        const string input = "Name: {Name}";

        // Act
        var hash1 = input.ComputeHash();
        var hash2 = input.ComputeHash();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GivenDifferentStrings_WhenComputingHash_ThenReturnsDifferentValues()
    {
        // Arrange
        const string input1 = "Name: {Name}";
        const string input2 = "Age: {Age}";

        // Act
        var hash1 = input1.ComputeHash();
        var hash2 = input2.ComputeHash();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GivenEmptyString_WhenComputingHash_ThenReturnsConsistentValue()
    {
        // Arrange & Act
        var hash1 = string.Empty.ComputeHash();
        var hash2 = string.Empty.ComputeHash();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GivenString_WhenComputingHash_ThenReturnsNonZero()
    {
        // Arrange & Act
        var hash = "Name: {Name}".ComputeHash();

        // Assert
        Assert.NotEqual(0UL, hash);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "StringHashExtensionTests"`
Expected: FAIL — `ComputeHash` does not exist

- [ ] **Step 3: Implement the hash extension method**

```csharp
// src/Tokenizer/Extensions/StringHashExtensions.cs
#if NET8_0_OR_GREATER
using System.IO.Hashing;
#endif

namespace Tokens.Extensions;

/// <summary>
/// Provides a non-cryptographic hash function for strings.
/// </summary>
internal static class StringHashExtensions
{
    /// <summary>
    /// Computes a non-cryptographic 64-bit hash of the string.
    /// Uses XxHash64 on .NET 8+ and FNV-1a on .NET Standard 2.0.
    /// </summary>
    public static ulong ComputeHash(this string input)
    {
#if NET8_0_OR_GREATER
        return XxHash64.HashToUInt64(System.Runtime.InteropServices.MemoryMarshal.AsBytes(input.AsSpan()));
#else
        const ulong fnvOffset = 14695981039346656037;
        const ulong fnvPrime = 1099511628211;

        var hash = fnvOffset;

        foreach (var c in input)
        {
            hash ^= c;
            hash *= fnvPrime;
        }

        return hash;
#endif
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "StringHashExtensionTests"`
Expected: PASS (all 4 tests)

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Extensions/StringHashExtensions.cs tests/Tokenizer.Tests/Extensions/StringHashExtensionTests.cs
git commit -m "feat: add ComputeHash string extension method"
```

---

### Task 2: Add Template.Id Property

**Files:**
- Modify: `src/Tokenizer/Template.cs`
- Modify: `src/Tokenizer/Compilation/TemplateCompiler.cs:88` (pass pattern string to Template constructor)
- Modify: `tests/Tokenizer.Tests/TemplateTests.cs`
- Modify: `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs`

The Template constructor changes to accept a `string pattern` parameter (used only to compute the Id). The existing `string name` parameter is kept for now to avoid breaking all call sites simultaneously — it will be removed in Task 5. The new constructor signature is `Template(string pattern, string name, TokenizerOptions options)`.

- [ ] **Step 1: Write the failing tests**

Add to `tests/Tokenizer.Tests/TemplateTests.cs`:

```csharp
[Fact]
public void GivenTemplate_WhenCompiled_ThenHasContentBasedId()
{
    // Arrange
    var tokenizer = CreateTokenizer();

    // Act
    var template = tokenizer.Compile("Name: {Name}");

    // Assert
    Assert.NotEqual(0UL, template.Id);
}

[Fact]
public void GivenSamePattern_WhenCompiledTwice_ThenIdIsIdentical()
{
    // Arrange
    var tokenizer = CreateTokenizer();
    const string pattern = "Name: {Name}";

    // Act
    var t1 = tokenizer.Compile(pattern);
    var t2 = tokenizer.Compile(pattern);

    // Assert
    Assert.Equal(t1.Id, t2.Id);
}

[Fact]
public void GivenDifferentPatterns_WhenCompiled_ThenIdsAreDifferent()
{
    // Arrange
    var tokenizer = CreateTokenizer();

    // Act
    var t1 = tokenizer.Compile("Name: {Name}");
    var t2 = tokenizer.Compile("Age: {Age}");

    // Assert
    Assert.NotEqual(t1.Id, t2.Id);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateTests"`
Expected: FAIL — `Id` property does not exist on `Template`

- [ ] **Step 3: Add Id property to Template and update constructor**

In `src/Tokenizer/Template.cs`, add the `Id` property and a new constructor overload that accepts a pattern:

```csharp
// Add using at top
using Tokens.Extensions;
```

Add new property after the `Name` property:

```csharp
/// <summary>
/// Content-based identity derived from the raw pattern string hash.
/// Two templates compiled from the same pattern string have the same Id.
/// </summary>
public ulong Id { get; }
```

Add a new internal constructor that accepts the pattern string:

```csharp
/// <summary>
/// Creates a new template with a content-based Id, name, and options.
/// </summary>
internal Template(string pattern, string name, TokenizerOptions options)
{
    tokens = new List<Token>();
    hints = new List<Hint>();
    tags = new List<string>();
    Options = options;
    this.name = name;
    Id = pattern.ComputeHash();
}
```

Keep the existing public constructors (they pass `Id = 0` effectively) — they're used by tests and the TemplateBuilder. They will be cleaned up later.

- [ ] **Step 4: Update TemplateCompiler to pass pattern string**

In `src/Tokenizer/Compilation/TemplateCompiler.cs:88`, change:

```csharp
// Old
var template = new Template(name, preTemplate.Options);
```

to:

```csharp
// New
var template = new Template(content, name, preTemplate.Options);
```

- [ ] **Step 5: Update TemplateBuilder**

In `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs`, add a `_pattern` field and builder method:

```csharp
private string _pattern = string.Empty;

public TemplateBuilder WithPattern(string pattern)
{
    _pattern = pattern;
    return this;
}
```

The `Build()` method stays the same for now — it uses the existing public constructors (Id will be 0 for builder-created templates, which is fine for tests that don't care about Id).

- [ ] **Step 6: Run all tests to verify nothing is broken**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Template.cs src/Tokenizer/Compilation/TemplateCompiler.cs tests/Tokenizer.Tests/TemplateTests.cs tests/Tokenizer.Tests/Builders/TemplateBuilder.cs
git commit -m "feat: add content-based Template.Id property"
```

---

### Task 3: Rekey TemplateCollection by Id

**Files:**
- Modify: `src/Tokenizer/TemplateCollection.cs`
- Modify: `src/Tokenizer/TokenMatcher.cs:180,354` (simplify iteration)
- Modify: `src/Tokenizer/TokenMatcherResult.cs:44,91` (change tie-breaker from Name to Id)
- Modify: `tests/Tokenizer.Tests/TemplateCollectionTests.cs`
- Modify: `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs`

- [ ] **Step 1: Write the failing tests for TemplateCollection**

Replace `TestCollectionCount` and add new tests in `tests/Tokenizer.Tests/TemplateCollectionTests.cs`:

```csharp
[Fact]
public void GivenTemplateWithId_WhenAdded_ThenCanRetrieveById()
{
    // Arrange
    var tokenizer = CreateTokenizer();
    var template = tokenizer.Compile("Name: {Name}");
    var coll = new TemplateCollection();

    // Act
    coll.Add(template);

    // Assert
    Assert.True(coll.TryGet(template.Id, out var retrieved));
    Assert.Same(template, retrieved);
}

[Fact]
public void GivenTemplateWithName_WhenAdded_ThenCanRetrieveByName()
{
    // Arrange
    var tokenizer = CreateTokenizer();
    var template = tokenizer.Compile("Name: {Name}");
    template.Name = "my-template";
    var coll = new TemplateCollection();

    // Act
    coll.Add(template);

    // Assert
    Assert.NotNull(coll.Get("my-template"));
}

[Fact]
public void GivenSamePatternAddedTwice_WhenSecondHasDifferentName_ThenLastWriteWins()
{
    // Arrange
    var tokenizer = CreateTokenizer();
    var t1 = tokenizer.Compile("Name: {Name}");
    t1.Name = "first";
    var t2 = tokenizer.Compile("Name: {Name}");
    t2.Name = "second";
    var coll = new TemplateCollection();

    // Act
    coll.Add(t1);
    coll.Add(t2);

    // Assert — same Id, so last write wins
    Assert.Equal(1, coll.Count);
    Assert.Equal("second", coll.First().Name);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateCollectionTests"`
Expected: FAIL — `TryGet(ulong, ...)` overload does not exist

- [ ] **Step 3: Rekey TemplateCollection**

Rewrite `src/Tokenizer/TemplateCollection.cs`:

```csharp
using System.Collections;
using System.Collections.Concurrent;

namespace Tokens;

/// <summary>
/// Collection of <see cref="Template" /> objects.
/// </summary>
public class TemplateCollection : IReadOnlyCollection<Template>
{
    private readonly ConcurrentDictionary<ulong, Template> templates;

    /// <summary>
    /// Returns the number of templates in this collection
    /// </summary>
    public int Count => templates.Count;

    /// <summary>
    /// Creates a new instance of the <see cref="TemplateCollection"/> class.
    /// </summary>
    public TemplateCollection()
    {
        templates = new ConcurrentDictionary<ulong, Template>();
    }

    /// <summary>
    /// Adds a template to the collection.
    /// If a template with the same Id already exists, it will be replaced.
    /// </summary>
    public void Add(Template template)
    {
        templates.AddOrUpdate(template.Id, template, (key, existing) => template);
    }

    /// <summary>
    /// Tries to get the template with the given Id.
    /// </summary>
    public bool TryGet(ulong id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Template? template)
    {
        return templates.TryGetValue(id, out template);
    }

    /// <summary>
    /// Tries to get the template with the given name (linear scan).
    /// </summary>
    public bool TryGet(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Template? template)
    {
        foreach (var candidate in templates.Values)
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                template = candidate;
                return true;
            }
        }

        template = null;
        return false;
    }

    /// <summary>
    /// Gets the template with the given name. Returns null if not found.
    /// </summary>
    public Template? Get(string name)
    {
        return TryGet(name, out var template) ? template : null;
    }

    /// <summary>
    /// Clears all templates from this collection
    /// </summary>
    public void Clear()
    {
        templates.Clear();
    }

    /// <summary>
    /// Determines if any templates are in this collection that contain the given
    /// tag.
    /// </summary>
    public bool ContainsTag(string tag)
    {
        foreach (var template in this)
        {
            if (template.HasTag(tag))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if any templates in this collection contain all the given tags.
    /// </summary>
    public bool ContainsAllTags(params string[] tags)
    {
        foreach (var template in this)
        {
            if (template.HasTags(tags))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the templates in this collection.
    /// </summary>
    public IEnumerator<Template> GetEnumerator()
    {
        return templates.Values.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
```

- [ ] **Step 4: Simplify TokenMatcher iteration**

In `src/Tokenizer/TokenMatcher.cs`, change the `MatchCore` method (around line 180):

```csharp
// Old
foreach (var name in Templates.Names)
{
    if (!Templates.TryGet(name, out var template)) continue;
```

to:

```csharp
// New
foreach (var template in Templates)
{
```

Apply the same change in `MatchAsyncFromSeekableStream` (around line 354):

```csharp
// Old
foreach (var name in Templates.Names)
{
    if (!Templates.TryGet(name, out var template)) continue;
```

to:

```csharp
// New
foreach (var template in Templates)
{
```

- [ ] **Step 5: Change GetBestMatch tie-breaker to use Id**

In `src/Tokenizer/TokenMatcherResult.cs`, change both `GetBestMatch` methods:

```csharp
// Old (line 44)
.ThenBy(r => r.Template.Name)

// New
.ThenBy(r => r.Template.Id)
```

```csharp
// Old (line 91)
.ThenBy(r => r.Template.Name)

// New
.ThenBy(r => r.Template.Id)
```

- [ ] **Step 6: Fix TemplateCollectionTests that use old constructors**

In `tests/Tokenizer.Tests/TemplateCollectionTests.cs`, the existing tests that create `new Template(string.Empty)` directly will all get `Id = 0`, so adding multiple will collide. Update `TestCollectionCount` to use the compiler:

```csharp
[Fact]
public void TestCollectionCount()
{
    // Arrange
    var tokenizer = CreateTokenizer();

    // Act
    collection.Add(tokenizer.Compile("One: {One}"));
    collection.Add(tokenizer.Compile("Two: {Two}"));
    collection.Add(tokenizer.Compile("Three: {Three}"));

    // Assert
    Assert.Equal(3, collection.Count);

    collection.Clear();

    Assert.Empty(collection);
}
```

The tag-related tests (`TestCollectionContainsTagWhenTrue`, etc.) each add a single template, so `Id = 0` collisions don't matter. Leave them as-is.

- [ ] **Step 7: Fix TokenMatcherAsyncTests that use Templates.Names**

In `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs:166`, change:

```csharp
// Old
Assert.Single(matcher.Templates.Names);

// New
Assert.Single(matcher.Templates);
```

- [ ] **Step 8: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 9: Commit**

```bash
git add src/Tokenizer/TemplateCollection.cs src/Tokenizer/TokenMatcher.cs src/Tokenizer/TokenMatcherResult.cs tests/Tokenizer.Tests/TemplateCollectionTests.cs tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs
git commit -m "refactor: rekey TemplateCollection by content-based Id"
```

---

### Task 4: Remove TemplateCache

**Files:**
- Delete: `src/Tokenizer/Compilation/TemplateCache.cs`
- Delete: `tests/Tokenizer.Tests/Compilation/TemplateCacheTests.cs`
- Modify: `src/Tokenizer/TokenizerOptions.cs` (remove `CompilationCacheMaxSize`)
- Modify: `src/Tokenizer/ITokenizer.cs` (remove `ClearCompilationCache`)
- Modify: `src/Tokenizer/Tokenizer.cs` (remove cache field, simplify Compile)
- Modify: `tests/Tokenizer.Tests/Compilation/CompileApiTests.cs` (remove cache-related tests)

- [ ] **Step 1: Delete TemplateCache and its tests**

Delete `src/Tokenizer/Compilation/TemplateCache.cs` and `tests/Tokenizer.Tests/Compilation/TemplateCacheTests.cs`.

- [ ] **Step 2: Remove CompilationCacheMaxSize from TokenizerOptions**

In `src/Tokenizer/TokenizerOptions.cs`:

Remove the property (line 120):
```csharp
// Delete this
public int CompilationCacheMaxSize { get; init; } = 500;
```

Remove from copy constructor (line 35):
```csharp
// Delete this line
CompilationCacheMaxSize = original.CompilationCacheMaxSize;
```

Remove from `Equals` (line 178):
```csharp
// Delete this line
&& CompilationCacheMaxSize == other.CompilationCacheMaxSize;
```

Remove from `GetHashCode` (line 200):
```csharp
// Delete this line
hash = hash * 31 + CompilationCacheMaxSize.GetHashCode();
```

- [ ] **Step 3: Remove ClearCompilationCache from ITokenizer**

In `src/Tokenizer/ITokenizer.cs`, remove:
```csharp
// Delete these lines (around line 49-52)
/// <summary>
/// Clears the compilation cache, forcing subsequent calls to recompile patterns.
/// </summary>
void ClearCompilationCache();
```

- [ ] **Step 4: Remove cache from Tokenizer class**

In `src/Tokenizer/Tokenizer.cs`:

Remove the field (line 26):
```csharp
// Delete
private readonly TemplateCache compilationCache;
```

Remove cache creation from both constructors (lines 57, 75):
```csharp
// Delete these lines
compilationCache = new TemplateCache(Options.CompilationCacheMaxSize);
```

Remove the `using Tokens.Compilation;` import if no longer needed.

Change `Compile` method (line 271):
```csharp
// Old
public Template Compile(string pattern) => compilationCache.GetOrAdd(pattern, p => parser.Parse(p));

// New
public Template Compile(string pattern) => parser.Parse(pattern);
```

Remove `ClearCompilationCache` (line 277):
```csharp
// Delete
public void ClearCompilationCache() => compilationCache.Clear();
```

- [ ] **Step 5: Update CompileApiTests**

In `tests/Tokenizer.Tests/Compilation/CompileApiTests.cs`:

Remove the cache-related tests entirely:
- `GivenSamePatternCompiledTwice_WhenUsingStringOverload_ThenCacheReturnsSameTemplate`
- `GivenTokenizer_WhenClearingCache_ThenNextCompileReturnsNewInstance`
- `GivenCachingDisabled_WhenCompilingSamePattern_ThenReturnsNewInstanceEachTime`

Keep `GivenPattern_WhenCompiling_ThenReturnsTemplateWithTokens` and `GivenPatternAndName_WhenCompiling_ThenTemplateHasExplicitName` (the name one will be removed in Task 6).

- [ ] **Step 6: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git rm src/Tokenizer/Compilation/TemplateCache.cs tests/Tokenizer.Tests/Compilation/TemplateCacheTests.cs
git add src/Tokenizer/TokenizerOptions.cs src/Tokenizer/ITokenizer.cs src/Tokenizer/Tokenizer.cs tests/Tokenizer.Tests/Compilation/CompileApiTests.cs
git commit -m "refactor: remove TemplateCache and CompilationCacheMaxSize"
```

---

### Task 5: Remove Compile and Tokenize String Overloads

**Files:**
- Modify: `src/Tokenizer/ITokenizer.cs`
- Modify: `src/Tokenizer/Tokenizer.cs`
- Modify: `tests/Tokenizer.Tests/Compilation/CompileApiTests.cs`
- Modify: Multiple test files that use `Tokenize(string, string)`

- [ ] **Step 1: Remove overloads from ITokenizer**

In `src/Tokenizer/ITokenizer.cs`, remove:

```csharp
// Delete: Compile with name
Template Compile(string pattern, string name);

// Delete: Tokenize with string pattern
TokenizeResult Tokenize(string template, string input);

// Delete: Tokenize<T> with string pattern
TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new();

// Delete: CompileAsync with name (TextReader)
Task<Template> CompileAsync(TextReader reader, string name, CancellationToken ct = default);

// Delete: CompileAsync with name (Stream)
Task<Template> CompileAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default);
```

- [ ] **Step 2: Remove implementations from Tokenizer**

In `src/Tokenizer/Tokenizer.cs`, remove:

The `Compile(string pattern, string name)` method (line 274):
```csharp
// Delete
public Template Compile(string pattern, string name) => parser.Parse(pattern, name);
```

The `Tokenize(string template, string input)` method (lines 84-89):
```csharp
// Delete
public TokenizeResult Tokenize(string template, string input)
{
    var t = Compile(template);
    return Tokenize(t, input);
}
```

The `Tokenize<T>(string pattern, string input)` method (lines 115-120):
```csharp
// Delete
public TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new()
{
    var template = Compile(pattern);
    return Tokenize<T>(template, input);
}
```

The `CompileAsync(TextReader reader, string name, ...)` method (lines 287-291):
```csharp
// Delete
public async Task<Template> CompileAsync(TextReader reader, string name, CancellationToken ct = default)
{
    var content = await ReadToEndAsync(reader, ct, Options.MaxTemplateLength).ConfigureAwait(false);
    return parser.Parse(content, name);
}
```

The `CompileAsync(Stream input, Encoding encoding, string name, ...)` method (lines 302-307):
```csharp
// Delete
public async Task<Template> CompileAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default)
{
    using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024, leaveOpen: true);
    return await CompileAsync(reader, name, ct).ConfigureAwait(false);
}
```

- [ ] **Step 3: Update CompileApiTests — remove name overload test**

In `tests/Tokenizer.Tests/Compilation/CompileApiTests.cs`, remove:
```csharp
// Delete GivenPatternAndName_WhenCompiling_ThenTemplateHasExplicitName
```

- [ ] **Step 4: Update test files that use Tokenize(string, string)**

In each of these files, change `tokenizer.Tokenize("pattern", input)` to compile-then-tokenize:

**Pattern:** Every call like:
```csharp
var result = tokenizer.Tokenize("Name: {Name}", "Name: Alice");
```
becomes:
```csharp
var template = tokenizer.Compile("Name: {Name}");
var result = tokenizer.Tokenize(template, "Name: Alice");
```

Files to update (20 occurrences across 7 files):
- `tests/Tokenizer.Tests/TokenizerOptionsRecordTests.cs` (1 occurrence)
- `tests/Tokenizer.Tests/TokenPropertyImmutabilityTests.cs` (1 occurrence)
- `tests/Tokenizer.Tests/Integration/DependencyInjectionTests.cs` (2 occurrences)
- `tests/Tokenizer.Tests/AllocationOptimizationTests.cs` (1 occurrence)
- `tests/Tokenizer.Tests/Diagnostics/DiagnosticLoggingTests.cs` (2 occurrences)
- `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs` (7 occurrences)

Note: `TemplateLexerTests.cs` uses `lexer.Tokenize()` which is a different method on `TemplateLexer`, not `ITokenizer`. Leave those alone.

- [ ] **Step 5: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/ITokenizer.cs src/Tokenizer/Tokenizer.cs tests/
git commit -m "refactor: remove string-based Compile/Tokenize overloads from ITokenizer"
```

---

### Task 6: Consolidate TemplateCompiler to Single Compile Method

**Files:**
- Modify: `src/Tokenizer/Compilation/TemplateCompiler.cs`
- Modify: `src/Tokenizer/Template.cs` (simplify constructors, remove lazy Name getter and static counter)
- Modify: `src/Tokenizer/Tokenizer.cs` (update calls to parser)
- Modify: `tests/Tokenizer.Tests/TemplateTests.cs` (update constructor calls)
- Modify: `tests/Tokenizer.Tests/TemplateCollectionTests.cs` (update constructor calls)
- Modify: `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs` (update Build method)
- Modify: Various other test files that use `new Template(...)`

- [ ] **Step 1: Refactor TemplateCompiler — rename Parse to Compile, remove overloads**

In `src/Tokenizer/Compilation/TemplateCompiler.cs`:

Add a static counter:
```csharp
private static int templateCounter;
```

Remove all four `Parse` overloads (lines 36-54) and replace with a single `Compile` method. The core logic from `Parse(string content, string name)` stays, but:
- Remove the `name` parameter
- Generate name from front matter or counter
- Pass `content` to the Template constructor for Id computation

The new method signature:
```csharp
public Template Compile(string content)
```

Inside the method body, after creating `preTemplate` (line 86), change:
```csharp
// Old
var template = new Template(content, name, preTemplate.Options);

// ...later...
if (string.IsNullOrWhiteSpace(preTemplate.Name) == false)
{
    template.Name = preTemplate.Name;
```

to:
```csharp
var name = string.IsNullOrWhiteSpace(preTemplate.Name)
    ? $"Template_{Interlocked.Increment(ref templateCounter)}"
    : preTemplate.Name;

var template = new Template(content, name, preTemplate.Options);
```

Remove the front matter name override block (lines 96-103) since name is now set correctly at construction.

Delete the `GenerateTemplateName` method entirely (lines 450-496).

Remove the `TextReader` overloads (lines 36-47) — I/O stays in `Tokenizer`.

Update logging that referenced the `name` parameter to use `template.Name` instead (it's already set).

- [ ] **Step 2: Simplify Template constructors**

In `src/Tokenizer/Template.cs`:

Remove the static counter:
```csharp
// Delete
private static int templateCounter;
```

Remove the lazy Name getter and replace with simple auto-property:
```csharp
// Old
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

// New
public string Name { get; set; }
```

Remove the `private string name;` field.

Keep the existing public constructors `Template()` and `Template(string name)` and `Template(string name, TokenizerOptions options)` but simplify them — they now just set `Name` directly without the backing field. These are used by tests directly. Update them to set `Name = name` and `Id = 0`.

Keep the internal constructor `Template(string pattern, string name, TokenizerOptions options)` which sets `Id = pattern.ComputeHash()`.

- [ ] **Step 3: Update Tokenizer.cs to call Compile instead of Parse**

In `src/Tokenizer/Tokenizer.cs`:

```csharp
// Old (line 271 area)
public Template Compile(string pattern) => parser.Parse(pattern);

// New
public Template Compile(string pattern) => parser.Compile(pattern);
```

Update async methods similarly:
```csharp
// Old
return parser.Parse(content);

// New
return parser.Compile(content);
```

- [ ] **Step 4: Update ToString on Template**

The `ToString` now uses the property directly:
```csharp
public override string ToString()
{
    return !string.IsNullOrEmpty(Name) ? $"Template('{Name}')" : $"Template({Tokens.Count} tokens)";
}
```

- [ ] **Step 5: Update test files that use `new Template(string.Empty)` or `new Template("name")`**

These tests construct Template directly for tag/collection tests. They should keep working since the public constructors are retained. The key change is that `new Template(string.Empty)` will have `Name = string.Empty` instead of lazy-generating `Template_N`.

Update `tests/Tokenizer.Tests/TemplateTests.cs`:
- `GivenUnnamedTemplate_WhenToString_ThenReturnsTokenCount` — this test creates `new Template(string.Empty)` and expects `"Template(0 tokens)"`. With the simplified getter (no lazy generation), `Name` will be `string.Empty`, and `string.IsNullOrEmpty("")` is true, so it returns the token count format. This test still passes.

- [ ] **Step 6: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Compilation/TemplateCompiler.cs src/Tokenizer/Template.cs src/Tokenizer/Tokenizer.cs tests/
git commit -m "refactor: consolidate TemplateCompiler to single Compile(string) method"
```

---

### Task 7: Clean Up TokenMatcher Async Registration

**Files:**
- Modify: `src/Tokenizer/ITokenMatcher.cs`
- Modify: `src/Tokenizer/TokenMatcher.cs`
- Modify: `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs`

- [ ] **Step 1: Remove name-accepting async overloads from ITokenMatcher**

In `src/Tokenizer/ITokenMatcher.cs`, remove:

```csharp
// Delete (around line 60-61)
/// <summary>
/// Compiles and registers a template read from a <see cref="TextReader"/> with an explicit name.
/// </summary>
Task<ITokenMatcher> RegisterTemplateAsync(TextReader reader, string name, CancellationToken ct = default);

// Delete (around line 70-71)
/// <summary>
/// Compiles and registers a template read from a <see cref="Stream"/> with an explicit name.
/// </summary>
Task<ITokenMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default);
```

- [ ] **Step 2: Remove implementations from TokenMatcher**

In `src/Tokenizer/TokenMatcher.cs`, remove:

```csharp
// Delete RegisterTemplateAsync(TextReader, string name, ...)
public async Task<ITokenMatcher> RegisterTemplateAsync(TextReader reader, string name, CancellationToken ct = default)
{
    var template = await tokenizer.CompileAsync(reader, name, ct).ConfigureAwait(false);
    Templates.Add(template);
    return this;
}

// Delete RegisterTemplateAsync(Stream, Encoding, string name, ...)
public async Task<ITokenMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, string name, CancellationToken ct = default)
{
    var template = await tokenizer.CompileAsync(input, encoding, name, ct).ConfigureAwait(false);
    Templates.Add(template);
    return this;
}
```

Update the remaining async registration methods to use the new Compile-only API:

```csharp
public async Task<ITokenMatcher> RegisterTemplateAsync(TextReader reader, CancellationToken ct = default)
{
    var template = await tokenizer.CompileAsync(reader, ct).ConfigureAwait(false);
    Templates.Add(template);
    return this;
}

public async Task<ITokenMatcher> RegisterTemplateAsync(Stream input, Encoding encoding, CancellationToken ct = default)
{
    var template = await tokenizer.CompileAsync(input, encoding, ct).ConfigureAwait(false);
    Templates.Add(template);
    return this;
}
```

- [ ] **Step 3: Update TokenMatcherAsyncTests**

In `tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs`:

Update the test `GivenTextReader_WhenRegisterTemplateAsync_ThenTemplateIsRegistered` (line 103):
```csharp
// Old
await matcher.RegisterTemplateAsync(reader, "my-template");
Assert.True(matcher.Templates.TryGet("my-template", out _));

// New — register without name, verify template was added
await matcher.RegisterTemplateAsync(reader);
Assert.Equal(1, matcher.Templates.Count);
```

Update `GivenStream_WhenRegisterTemplateAsyncWithName_ThenTemplateHasName` (line 170) — this test should be removed since the name overload no longer exists. Or convert it to test the nameless overload:
```csharp
// Replace the test
[Fact]
public async Task GivenStream_WhenRegisterTemplateAsync_ThenTemplateIsRegistered()
{
    // Arrange
    var matcher = new TokenMatcher();
    var bytes = Encoding.UTF8.GetBytes("Name: {Name}");
    using var stream = new MemoryStream(bytes);

    // Act
    await matcher.RegisterTemplateAsync(stream, Encoding.UTF8);

    // Assert
    Assert.Equal(1, matcher.Templates.Count);
}
```

Update `GivenStream_WhenRegisterTemplateAsync_ThenTemplateIsRegistered` (line 155) — change assertion from `Assert.Single(matcher.Templates.Names)` to `Assert.Single(matcher.Templates)` (if not already done in Task 3).

- [ ] **Step 4: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/ITokenMatcher.cs src/Tokenizer/TokenMatcher.cs tests/Tokenizer.Tests/TokenMatcherAsyncTests.cs
git commit -m "refactor: remove name-accepting async registration overloads from TokenMatcher"
```

---

### Task 8: Final Verification and Cleanup

**Files:**
- All modified files from previous tasks

- [ ] **Step 1: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 2: Build in Release mode**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeded, 0 warnings (or only pre-existing warnings)

- [ ] **Step 3: Verify no stale references**

Search for any remaining references to removed APIs:
```bash
grep -r "ClearCompilationCache\|CompilationCacheMaxSize\|GenerateTemplateName\|Templates\.Names" src/ tests/ --include="*.cs"
```
Expected: No matches

- [ ] **Step 4: Commit any cleanup**

If any stale references were found, fix and commit:
```bash
git add -A
git commit -m "chore: clean up stale references from Template identity refactor"
```
