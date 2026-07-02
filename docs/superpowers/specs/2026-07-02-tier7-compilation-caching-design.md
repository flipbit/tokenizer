# Tier 7: Template Compilation Caching — Design Spec

## Overview

Prevent repeated parsing of the same template pattern by introducing an internal compilation cache, a public `Compile()` API, `TextReader` support for compilation, interface extraction for testability, and moving transformer/validator registration to `TokenizerOptions`.

## Approach

Dedicated internal `TemplateCache` class (Approach B). Cache is instance-scoped on `Tokenizer`, keyed by hash of template string, with LRU eviction. `TextReader` overloads bypass the cache. `ITokenizer` and `ITokenMatcher` interfaces extracted. Transformer/validator registration moves to `TokenizerOptions`.

---

## 1. Remove `Template.Content`

Remove the `Content` property from `Template` entirely. Breaking change.

**Current usages being replaced:**
- `Name` auto-generation (MD5 hash of `Content`) → replaced by static `Interlocked.Increment` counter
- `ToString()` → already uses `Name`, no change needed

**New internal property:**
- `internal string? CacheKey` — hash of source string, set during compilation from string overloads. `null` for `TextReader`-compiled templates. Used only by `TemplateCache`.

**`Template.Name` behavior:**
- User-provided name (via `Compile(pattern, name)` or front matter `name:` field) takes priority
- When no name is provided, auto-generated via static counter: `"Template_1"`, `"Template_2"`, etc.
- `Compile()` overloads without a `name` parameter get auto-generated names
- `Compile()` overloads with a `name` parameter use the provided name

---

## 2. `TemplateCache` (internal)

New internal class managing compiled template caching with LRU eviction.

### Data structures

```csharp
internal sealed class TemplateCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> cache;
    private readonly int maxSize;

    private sealed class CacheEntry
    {
        public required Template Template { get; init; }
        public long LastAccessed;  // updated via Interlocked.Exchange
    }
}
```

- **Key**: SHA256 hash (hex string, ~64 chars) of the template source string, computed once during compilation
- **Value**: `CacheEntry` containing compiled `Template` and LRU timestamp

### API

```csharp
internal Template GetOrAdd(string pattern, Func<string, Template> compile)
```

- Computes hash of `pattern`
- On cache hit: updates `LastAccessed`, returns cached `Template`
- On cache miss: calls `compile(pattern)`, stores result, evicts LRU entry if at capacity
- Thread-safe via `ConcurrentDictionary`

```csharp
internal void Clear()
```

- Removes all entries. Exposed publicly via `ITokenizer.ClearCompilationCache()`

```csharp
internal int Count { get; }
```

- Current number of cached entries. For diagnostics and testing.

### LRU eviction

When `Count >= maxSize` on insertion, find and remove the entry with the oldest `LastAccessed` via linear scan. Eviction is the cold path (only on cache-full insertions); lookups are the hot path and must remain fast.

### Cache interaction by overload

| Overload | Cache behavior |
|----------|---------------|
| `Compile(string)` | Cache-through (hash key) |
| `Compile(string, string)` | Cache-through (hash key) |
| `Compile(TextReader)` | Bypass cache |
| `Compile(TextReader, string)` | Bypass cache |
| `Tokenize(string, string)` | Cache-through via `Compile(string)` |
| `Tokenize(Template, string)` | No compilation, no cache interaction |

---

## 3. `TokenizerOptions` changes

### New property

```csharp
/// <summary>
/// Maximum number of compiled templates to cache. Default: 500.
/// Set to 0 to disable compilation caching.
/// </summary>
public int CompilationCacheMaxSize { get; init; } = 500;
```

### Transformer/validator registration moves here

Registration methods move from `Tokenizer` and `TokenMatcher` to `TokenizerOptions`:

```csharp
public record class TokenizerOptions
{
    // ... existing properties ...

    internal List<Type> Transformers { get; } = new();
    internal List<Type> Validators { get; } = new();

    public TokenizerOptions RegisterTransformer<T>() where T : ITokenTransformer
    {
        Transformers.Add(typeof(T));
        return this;
    }

    public TokenizerOptions RegisterValidator<T>() where T : ITokenValidator
    {
        Validators.Add(typeof(T));
        return this;
    }
}
```

**DI usage:**
```csharp
services.AddTokenizer(options =>
{
    options.CompilationCacheMaxSize = 500;
    options.RegisterTransformer<MyTransformer>();
    options.RegisterValidator<MyValidator>();
});
```

**Non-DI usage:**
```csharp
var options = new TokenizerOptions();
options.RegisterTransformer<MyTransformer>();
var tokenizer = new Tokenizer(options);
```

---

## 4. `ITokenizer` interface

Extracted from `Tokenizer`'s public API surface:

```csharp
public interface ITokenizer
{
    TokenizerOptions Options { get; }

    // Compilation — string (cache-through)
    Template Compile(string pattern);
    Template Compile(string pattern, string name);

    // Compilation — stream (cache bypass)
    Template Compile(TextReader reader);
    Template Compile(TextReader reader, string name);

    // Tokenization — pre-compiled template
    TokenizeResult Tokenize(Template template, string input);
    TokenizeResult<T> Tokenize<T>(Template template, string input)
        where T : class, new();

    // Tokenization — string pattern (uses cache internally)
    TokenizeResult Tokenize(string pattern, string input);
    TokenizeResult<T> Tokenize<T>(string pattern, string input)
        where T : class, new();

    // Cache management
    void ClearCompilationCache();
}
```

`Tokenizer` remains sealed, implements `ITokenizer`.

---

## 5. `ITokenMatcher` interface

Extracted from `TokenMatcher`'s public API surface:

```csharp
public interface ITokenMatcher
{
    TemplateCollection Templates { get; }

    // Registration — string (delegates to ITokenizer.Compile)
    ITokenMatcher RegisterTemplate(string content);
    ITokenMatcher RegisterTemplate(string content, string name);

    // Registration — stream (delegates to ITokenizer.Compile)
    ITokenMatcher RegisterTemplate(TextReader reader);
    ITokenMatcher RegisterTemplate(TextReader reader, string name);

    // Registration — pre-compiled
    ITokenMatcher RegisterTemplate(Template template);

    // Matching
    TokenMatcherResult Match(string input);
    TokenMatcherResult Match(string input, string[]? tags);
    TokenMatcherResult<T> Match<T>(string input)
        where T : class, new();
    TokenMatcherResult<T> Match<T>(string input, string[]? tags)
        where T : class, new();
}
```

---

## 6. `TokenMatcher` constructor changes

`TokenMatcher` supports both DI and non-DI construction:

```csharp
public sealed class TokenMatcher : ITokenMatcher
{
    private readonly ITokenizer tokenizer;

    // DI path — injected, shares the singleton Tokenizer
    public TokenMatcher(ITokenizer tokenizer)
    {
        this.tokenizer = tokenizer;
    }

    // Non-DI convenience — creates its own Tokenizer internally
    public TokenMatcher(TokenizerOptions options)
        : this(new Tokenizer(options)) { }

    public TokenMatcher()
        : this(new TokenizerOptions()) { }
}
```

**DI registration in `AddTokenizer()`:**
- `ITokenizer` → `Tokenizer` (singleton)
- `ITokenMatcher` → `TokenMatcher` (singleton, `ITokenizer` injected)
- Both share the same `Tokenizer` instance and compilation cache

**Removed from `TokenMatcher`:**
- `RegisterTransformer<T>()` — register on `TokenizerOptions` instead
- `RegisterValidator<T>()` — register on `TokenizerOptions` instead

---

## 7. `TextReader` compilation

### `TemplateLexer` changes

`TemplateLexer` currently accepts a `string` and indexes into it character-by-character. Add a `TextReader` code path:

- New internal constructor or method accepting `TextReader`
- `Scan()` reads via `reader.Read()` instead of `content[index]`
- `FileLocation` tracking works the same (line/column counters)

### `TokenParser` changes

New internal methods:

```csharp
internal Template Parse(TextReader reader)
internal Template Parse(TextReader reader, string name)
```

These follow the same compilation pipeline (lex → parse → AST → bind) but skip hash computation and cache interaction.

---

## 8. Benchmarks

New `CompilationCacheBenchmarks.cs` alongside existing suites:

| Benchmark | Measures |
|-----------|----------|
| `CacheHit_Small/Medium/Large` | Repeated tokenization with same string pattern — cache lookup cost |
| `CacheMiss_Small/Medium/Large` | Unique patterns each call — compilation + cache insertion cost |
| `CacheHit_vs_PreCompiled` | Cached string overload vs `Template` overload — cache overhead vs zero-cache baseline |
| `CacheEviction` | Exceed max size — eviction cost |
| `ConcurrentCacheAccess` | Parallel threads hitting same cached templates |

**Before/after comparison:** Run existing `CompilationBenchmarks` and `TokenizationBenchmarks` before and after to verify no regressions.

---

## 9. Breaking changes

| Change | Impact |
|--------|--------|
| `Template.Content` removed | Compile error for consumers accessing it |
| `Tokenizer` implements `ITokenizer` | Non-breaking (additive) |
| `TokenMatcher` constructor takes `ITokenizer` | Constructor signature change — breaking for manual construction |
| `TokenMatcher.RegisterTransformer<T>()` removed | Compile error — register on `TokenizerOptions` instead |
| `TokenMatcher.RegisterValidator<T>()` removed | Compile error — register on `TokenizerOptions` instead |
| `Tokenizer.RegisterTransformer<T>()` removed | Compile error — register on `TokenizerOptions` instead |
| `Tokenizer.RegisterValidator<T>()` removed | Compile error — register on `TokenizerOptions` instead |
| `ITokenMatcher` extracted | Non-breaking (additive) |
| `ITokenizer` extracted | Non-breaking (additive) |
| `Compile()` methods added | Non-breaking (additive) |
| `ClearCompilationCache()` added | Non-breaking (additive) |
| `CompilationCacheMaxSize` on options | Non-breaking (new property with default) |
| `RegisterTransformer<T>()` on options | Non-breaking (additive) |
| `RegisterValidator<T>()` on options | Non-breaking (additive) |
| DI registers `ITokenMatcher` | Non-breaking (additive) |
