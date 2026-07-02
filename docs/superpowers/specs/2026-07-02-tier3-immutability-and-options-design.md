# Tier 3: Immutability and Options — Design Spec

## Goal

Bring `TokenizerOptions`, DI registration, and `TemplateCollection` in line with idiomatic .NET patterns. Lock down mutability by convention, adopt `IOptions<T>`, and make collections enumerable.

## Scope

1. Convert `TokenizerOptions` to a record class
2. Adopt `IOptions<TokenizerOptions>` in DI registration
3. Replace static factory methods with constructors on `Tokenizer`
4. Update `FrontMatterBinder` to use `with` expressions instead of clone-and-mutate
5. Implement `IReadOnlyCollection<Template>` on `TemplateCollection`

## Out of Scope

- Backward compatibility shims or `[Obsolete]` migration bridges
- Changes to `Template`, `TokenParser`, or the compilation pipeline beyond what's needed to consume the new options shape
- Options validation (`IValidateOptions<T>`)

---

## 1. `TokenizerOptions` Record Class

### Current State

- `public sealed class TokenizerOptions` with get/set properties, a default constructor, and a `Clone()` method
- `public static TokenizerOptions Defaults => new TokenizerOptions();` allocates on every access
- 12 properties, all mutable

### Changes

- Convert from `sealed class` to `public record class TokenizerOptions`
- Keep `get; set;` on all properties (required for `Configure<T>()` delegate pattern and `IConfiguration.Bind()` compatibility)
- Default values remain as property initializers — `new TokenizerOptions()` produces the defaults
- Remove `public static TokenizerOptions Defaults` — no migration path, just delete
- Remove `Clone()` method — `with` expressions replace it
- Remove the explicit default constructor if the record's synthesized constructor is equivalent; keep it only if the body does work beyond setting defaults

### Resulting Shape

```csharp
public record class TokenizerOptions
{
    public bool IgnoreMissingProperties { get; set; }
    public bool EnableDiagnostics { get; set; }
    public bool TrimLeadingWhitespaceInTokenPreamble { get; set; } = true;
    public bool TrimPreambleBeforeNewLine { get; set; }
    public bool TrimTrailingWhiteSpace { get; set; } = true;
    public bool OutOfOrderTokens { get; set; }
    public StringComparison TokenStringComparison { get; set; } = StringComparison.InvariantCulture;
    public bool TerminateOnNewLine { get; set; }
    public int MaxInputLength { get; set; } = 1_048_576;
    public int MaxTemplateLength { get; set; } = 65_536;
    public int MaxTokenCount { get; set; } = 500;
    public int MaxIterations { get; set; }
}
```

### Migration Impact

- All call sites using `TokenizerOptions.Defaults` must change to `new TokenizerOptions()`
- All call sites using `.Clone()` must change to `with { }` expressions
- Record equality semantics replace reference equality — this is desirable for options objects

---

## 2. DI Registration with `IOptions<TokenizerOptions>`

### Current State

`AddTokenizer(Action<TokenizerOptions> configure)` creates a `TokenizerOptions` instance, calls the delegate, and registers the instance as a raw singleton. Internal services resolve `TokenizerOptions` directly from the container.

### Changes

Three registration overloads on `IServiceCollection`:

```csharp
// a) Lambda configuration
public static IServiceCollection AddTokenizer(
    this IServiceCollection services,
    Action<TokenizerOptions> configure);

// b) Configuration source binding
public static IServiceCollection AddTokenizer(
    this IServiceCollection services,
    IConfiguration configuration);

// c) Pre-built instance
public static IServiceCollection AddTokenizer(
    this IServiceCollection services,
    TokenizerOptions options);
```

A parameterless `AddTokenizer()` overload remains for default configuration.

**Implementation details:**

- Overload (a): calls `services.Configure<TokenizerOptions>(configure)`
- Overload (b): calls `services.Configure<TokenizerOptions>(configuration.Bind)`
- Overload (c): calls `services.AddSingleton(Options.Create(options))`
- Parameterless: calls `services.Configure<TokenizerOptions>(_ => { })`
- All internal service registrations resolve `IOptions<TokenizerOptions>` instead of raw `TokenizerOptions`
- `Tokenizer` is registered with its DI constructor that accepts `IOptions<TokenizerOptions>`

### Package Dependencies

`Microsoft.Extensions.Options.ConfigurationExtensions` is needed for config binding. Verify this is already a transitive dependency or add it explicitly.

---

## 3. `Tokenizer` Constructors Replace Static Factories

### Current State

Three static factory methods:
- `Tokenizer.Create()`
- `Tokenizer.Create(TokenizerOptions options)`
- `Tokenizer.Create(TokenizerOptions options, ILoggerFactory? loggerFactory)`

### Changes

Replace with constructors:

```csharp
// Non-DI: default options
public Tokenizer();

// Non-DI: custom options
public Tokenizer(TokenizerOptions options);

// DI constructor
public Tokenizer(IOptions<TokenizerOptions> options, ILoggerFactory? loggerFactory = null);
```

- The parameterless constructor creates `new TokenizerOptions()` and delegates to the options constructor
- The `TokenizerOptions` constructor wraps in `Options.Create()` and delegates to the DI constructor
- The DI constructor is the single implementation path — creates internal components (`TokenParser`, `TokenizationEngine`, `HintProcessor`, `ResultBuilder`)
- Each `Tokenizer` instance holds its own options copy (via `with { }` from the provided options)
- Remove all three `Create()` static methods

### Migration Impact

- `Tokenizer.Create()` → `new Tokenizer()`
- `Tokenizer.Create(options)` → `new Tokenizer(options)`
- `Tokenizer.Create(options, loggerFactory)` → `new Tokenizer(Options.Create(options), loggerFactory)`

---

## 4. `FrontMatterBinder` Uses `with` Expressions

### Current State

```csharp
var templateOptions = options.Clone();
templateOptions.TrimTrailingWhiteSpace = ParseBoolean(...);
templateOptions.OutOfOrderTokens = ParseBoolean(...);
// ... etc
```

### Changes

Build a new options instance using `with`:

```csharp
var templateOptions = options with
{
    TrimTrailingWhiteSpace = ParseBoolean(...) ?? options.TrimTrailingWhiteSpace,
    OutOfOrderTokens = ParseBoolean(...) ?? options.OutOfOrderTokens,
    // ... each property only overridden if front matter specifies it
};
```

Each property in the `with` expression falls back to the source value if the front matter key is absent. `ParseBoolean` (and similar helpers like `ParseEnum`) must return nullable types (`bool?`, `StringComparison?`) to distinguish "not specified" from "specified as false/value." Only properties with non-null parsed values are overridden in the `with` expression; all others retain the source value.

---

## 5. `TemplateCollection` Implements `IReadOnlyCollection<Template>`

### Current State

`public class TemplateCollection` with no interface implementations. Has `Count` property and internal `ConcurrentDictionary<string, Template>`.

### Changes

- Implement `IReadOnlyCollection<Template>` (which inherits `IEnumerable<Template>` and `IEnumerable`)
- Add `GetEnumerator()` that iterates the dictionary's `Values`
- Existing `Count` property satisfies the interface contract

```csharp
public class TemplateCollection : IReadOnlyCollection<Template>
{
    public IEnumerator<Template> GetEnumerator()
        => _templates.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
```

- Users can now `foreach` and LINQ over template collections

---

## Testing Strategy

- Existing tests updated to use constructors instead of `Tokenizer.Create()`
- Existing tests updated to replace `TokenizerOptions.Defaults` with `new TokenizerOptions()`
- New tests for DI registration: verify all three overloads resolve a working `Tokenizer`
- New tests for `TemplateCollection` enumeration
- New tests for `FrontMatterBinder` `with`-expression copy behavior (partial overrides preserve source values)
- Record equality tests for `TokenizerOptions`
