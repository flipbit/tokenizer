# Template Identity Refactor

## Problem

`Template.Name` serves multiple conflicting roles: user-facing label, collection dictionary key, logging identifier, and auto-generated content summary. The compilation pipeline (`TemplateCompiler`) eagerly generates names via a 50-line content-sniffing heuristic (`GenerateTemplateName`), and the `Template.Name` getter has a lazy fallback counter — all for a property that is only structurally meaningful within `TokenMatcher`'s `TemplateCollection`.

Additionally, `TemplateCache` provides compilation caching that is only useful when the same `Tokenizer` instance receives the same raw pattern string multiple times — a scenario better solved by the caller holding a compiled `Template` reference.

Several `ITokenizer` overloads (`Compile` with name, `Tokenize` with raw string) conflate compilation with naming or encourage recompilation in loops.

## Design Decisions

All decisions below were confirmed during brainstorming.

### Template.Id — content-based identity

- `Template` gets a new `public ulong Id { get; }` property, set once during compilation.
- Computed by hashing the raw pattern string using XxHash64 (.NET 8+) with FNV-1a 64-bit fallback (.NET Standard 2.0).
- The hash function is an extension method on `string` in `Tokens.Extensions.StringHashExtensions`, independently testable.
- The `Template` constructor accepts the raw pattern string to compute the Id. The pattern string is not stored — only hashed.
- Id is the structural identity of a template — two templates compiled from the same pattern string have the same Id.

### Template.Name — user-facing label only

- `Name` remains a public get/set property on `Template`.
- No lazy auto-generation in the getter. Name is always set during compilation:
  - If front matter contains `name:`, that value is used.
  - Otherwise, `Template_N` is generated from an atomic counter on `TemplateCompiler`.
- Users can override Name at any time via the public setter.
- Name has no structural role — it is not used for keying, deduplication, or identity.

### TemplateCollection — keyed by Id

- Internal dictionary changes from `ConcurrentDictionary<string, Template>` to `ConcurrentDictionary<ulong, Template>`.
- `Add(Template)` keys by `template.Id`. Last write wins on duplicate Id.
- `Names` property is removed.
- `Get(string name)` and `TryGet(string name, ...)` become convenience methods that scan values by name (linear search).
- `TryGet(ulong id, ...)` added for direct Id-based lookup.
- `Count`, `Clear`, `ContainsTag`, `ContainsAllTags`, and enumeration are unchanged.

### TemplateCache — removed entirely

- `TemplateCache.cs` is deleted.
- `CompilationCacheMaxSize` is removed from `TokenizerOptions` (including copy constructor, equality, and hash code).
- `ClearCompilationCache()` is removed from `ITokenizer` and `Tokenizer`.
- `Tokenizer.Compile(string pattern)` becomes a direct call to the compiler.

### TemplateCompiler — single entry point, no name parameter

- All `Parse` overloads are replaced by a single `Compile(string content)` method.
- `TextReader` overloads are removed — I/O buffering stays in `Tokenizer`, which reads to string then calls `Compile`.
- `GenerateTemplateName` (content-sniffing heuristic) is deleted.
- The atomic name counter moves from `Template` to `TemplateCompiler`.
- Front matter `name:` binding remains in the compilation pipeline.

### ITokenizer / Tokenizer — simplified API

**Removed:**
- `Compile(string pattern, string name)`
- `CompileAsync(TextReader reader, string name, CancellationToken)`
- `CompileAsync(Stream input, Encoding encoding, string name, CancellationToken)`
- `Tokenize(string template, string input)`
- `Tokenize<T>(string pattern, string input)`
- `ClearCompilationCache()`

**Retained:**
- `Compile(string pattern)` — delegates to `TemplateCompiler.Compile`
- `CompileAsync(TextReader reader, CancellationToken)` — async I/O, then `TemplateCompiler.Compile`
- `CompileAsync(Stream input, Encoding encoding, CancellationToken)` — async I/O, then `TemplateCompiler.Compile`
- `Tokenize(Template, string)` and `Tokenize<T>(Template, string)` — unchanged
- All async tokenize overloads — unchanged

### TokenMatcher — naming stays here

- `RegisterTemplate(string content, string name)` is retained. It compiles, sets Name, then adds to collection.
- `RegisterTemplate(string content)` compiles and adds (Name comes from front matter or compiler counter).
- `RegisterTemplate(Template)` adds a pre-compiled template directly.
- Async registration overloads that accept a name parameter are removed, consistent with the Tokenizer changes.
- Iteration in `MatchCore` / `MatchAsyncFromSeekableStream` simplifies from `foreach name in Names → TryGet` to `foreach template in Templates`.

## Implementation Strategy

Incremental commits, each self-contained with passing tests:

1. **Add hash extension method + Template.Id** — additive, nothing breaks
2. **Rekey TemplateCollection by Id** — add name convenience lookup, update TokenMatcher iteration
3. **Remove TemplateCache** — delete file, remove options/interface members
4. **Remove Compile and Tokenize overloads** — from ITokenizer/Tokenizer
5. **Consolidate TemplateCompiler** — single `Compile(string)` method, move name counter, delete `GenerateTemplateName`
6. **Clean up TokenMatcher async registration** — remove name-accepting async overloads

## Files Affected

| File | Change |
|------|--------|
| `src/Tokenizer/Extensions/StringHashExtensions.cs` | New — hash extension method |
| `src/Tokenizer/Template.cs` | Add `Id` property, simplify `Name` (remove lazy getter, remove static counter) |
| `src/Tokenizer/TemplateCollection.cs` | Rekey by Id, add name scan methods |
| `src/Tokenizer/Compilation/TemplateCache.cs` | Delete |
| `src/Tokenizer/Compilation/TemplateCompiler.cs` | Single `Compile(string)`, own name counter, delete `GenerateTemplateName` |
| `src/Tokenizer/TokenizerOptions.cs` | Remove `CompilationCacheMaxSize` |
| `src/Tokenizer/ITokenizer.cs` | Remove overloads + `ClearCompilationCache` |
| `src/Tokenizer/Tokenizer.cs` | Remove overloads, cache field, simplify Compile |
| `src/Tokenizer/TokenMatcher.cs` | Simplify iteration, remove async name overloads |
| `src/Tokenizer/ITokenMatcher.cs` | Remove async name overloads |
| `tests/Tokenizer.Tests/` | Update all affected tests |
