# Tier 3: Immutability and Options — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert `TokenizerOptions` to a record class with the idiomatic `IOptions<T>` pattern, replace static factory methods with constructors, and make `TemplateCollection` enumerable.

**Architecture:** `TokenizerOptions` becomes a `record class` with `get; set;` properties (compatible with `Configure<T>()` and config binding). `Tokenizer` exposes public constructors instead of static `Create()` methods. DI registration uses `IOptions<TokenizerOptions>`. `TemplateCollection` implements `IReadOnlyCollection<Template>`.

**Tech Stack:** C#, .NET Standard 2.0 / .NET 8.0+ dual-target, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Options.ConfigurationExtensions`, xUnit

---

### Task 1: Add `Microsoft.Extensions.Options` Package References

**Files:**
- Modify: `src/Tokenizer/Tokenizer.csproj:17-20`
- Modify: `tests/Tokenizer.Tests/Tokenizer.Tests.csproj:11-13`

This task adds the necessary NuGet packages before any code changes.

- [ ] **Step 1: Add package references to the main project**

Add to `src/Tokenizer/Tokenizer.csproj` in the first `<ItemGroup>`:

```xml
<PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="8.0.0" />
```

- [ ] **Step 2: Add `Microsoft.Extensions.Configuration.Abstractions` to the test project**

Add to `tests/Tokenizer.Tests/Tokenizer.Tests.csproj` in the `<ItemGroup>` with other package references:

```xml
<PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
```

- [ ] **Step 3: Verify the solution builds**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Tokenizer.csproj tests/Tokenizer.Tests/Tokenizer.Tests.csproj
git commit -m "Add Microsoft.Extensions.Options package references for Tier 3"
```

---

### Task 2: Convert `TokenizerOptions` to a Record Class

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs`

Convert from `sealed class` to `record class`, move defaults to property initializers, remove `Defaults` and `Clone()`.

- [ ] **Step 1: Write failing test — record equality semantics**

Add a new test file `tests/Tokenizer.Tests/TokenizerOptionsRecordTests.cs`:

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tests;

public class TokenizerOptionsRecordTests : TokenizerTestBase
{
    public TokenizerOptionsRecordTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenTwoDefaultOptions_WhenCompared_ThenAreEqual()
    {
        // Arrange
        var options1 = new TokenizerOptions();
        var options2 = new TokenizerOptions();

        // Act & Assert
        Assert.Equal(options1, options2);
    }

    [Fact]
    public void GivenOptions_WhenCopiedWithModification_ThenOriginalIsUnchanged()
    {
        // Arrange
        var original = new TokenizerOptions();

        // Act
        var modified = original with { TrimTrailingWhiteSpace = false };

        // Assert
        Assert.True(original.TrimTrailingWhiteSpace);
        Assert.False(modified.TrimTrailingWhiteSpace);
        Assert.NotEqual(original, modified);
    }

    [Fact]
    public void GivenDefaultOptions_ThenDefaultValuesAreCorrect()
    {
        // Arrange & Act
        var options = new TokenizerOptions();

        // Assert
        Assert.False(options.IgnoreMissingProperties);
        Assert.False(options.EnableDiagnostics);
        Assert.True(options.TrimLeadingWhitespaceInTokenPreamble);
        Assert.False(options.TrimPreambleBeforeNewLine);
        Assert.True(options.TrimTrailingWhiteSpace);
        Assert.False(options.OutOfOrderTokens);
        Assert.Equal(System.StringComparison.InvariantCulture, options.TokenStringComparison);
        Assert.False(options.TerminateOnNewLine);
        Assert.Equal(1_048_576, options.MaxInputLength);
        Assert.Equal(65_536, options.MaxTemplateLength);
        Assert.Equal(500, options.MaxTokenCount);
        Assert.Equal(0, options.MaxIterations);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsRecordTests"`
Expected: `GivenTwoDefaultOptions_WhenCompared_ThenAreEqual` fails because `class` uses reference equality.

- [ ] **Step 3: Convert `TokenizerOptions` to a record class**

Replace the entire contents of `src/Tokenizer/TokenizerOptions.cs` with:

```csharp
using System;

namespace Tokens;

/// <summary>
/// Options for the <see cref="Tokenizer"/>.
/// </summary>
public record class TokenizerOptions
{
    /// <summary>
    /// When true, tokens that do not map to a property on the target object are silently ignored.
    /// </summary>
    public bool IgnoreMissingProperties { get; set; }

    /// <summary>
    /// When true, tokenization results include a <see cref="Diagnostics.TokenizationDiagnostics"/>
    /// property with a structured trace of every matching decision, a mismatch summary
    /// with adaptive hints, and a visual alignment diff.
    /// Default: false. Has no performance impact when disabled.
    /// </summary>
    public bool EnableDiagnostics { get; set; }

    /// <summary>
    /// When true, leading whitespace in the static text preceding a token is trimmed before matching.
    /// </summary>
    public bool TrimLeadingWhitespaceInTokenPreamble { get; set; } = true;

    /// <summary>
    /// When true, any portion of a token preamble that appears before a newline is discarded.
    /// </summary>
    public bool TrimPreambleBeforeNewLine { get; set; }

    public bool TrimTrailingWhiteSpace { get; set; } = true;

    /// <summary>
    /// When true, tokens may be matched in any order rather than strictly left-to-right.
    /// </summary>
    public bool OutOfOrderTokens { get; set; }

    /// <summary>
    /// Determines the <see cref="StringComparison"/> type to use when matching Token names to object properties
    /// </summary>
    public StringComparison TokenStringComparison { get; set; } = StringComparison.InvariantCulture;

    /// <summary>
    /// If set, token values will be extracted up till the first new line character.
    /// </summary>
    public bool TerminateOnNewLine { get; set; }

    /// <summary>
    /// Maximum allowed length for input text. Default: 1,048,576 (1MB).
    /// Set to 0 to disable.
    /// </summary>
    public int MaxInputLength { get; set; } = 1_048_576;

    /// <summary>
    /// Maximum allowed length for template pattern text. Default: 65,536 (64KB).
    /// Set to 0 to disable.
    /// </summary>
    public int MaxTemplateLength { get; set; } = 65_536;

    /// <summary>
    /// Maximum number of tokens allowed in a template. Default: 500.
    /// Set to 0 to disable.
    /// </summary>
    public int MaxTokenCount { get; set; } = 500;

    /// <summary>
    /// Maximum number of iterations in the tokenization loop.
    /// Default: 0 (auto-calculated as input.Length * 2).
    /// Set to a positive value to override.
    /// </summary>
    public int MaxIterations { get; set; }
}
```

- [ ] **Step 4: Run the new tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsRecordTests"`
Expected: All 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/TokenizerOptions.cs tests/Tokenizer.Tests/TokenizerOptionsRecordTests.cs
git commit -m "Convert TokenizerOptions to record class, remove Defaults and Clone()"
```

---

### Task 3: Fix All `TokenizerOptions.Defaults` and `.Clone()` Call Sites

**Files:**
- Modify: `src/Tokenizer/Compilation/TokenParser.cs:27`
- Modify: `src/Tokenizer/Compilation/Parsing/AstTemplateDefinitionParser.cs:24,31`
- Modify: `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs:18`
- Modify: `src/Tokenizer/Compilation/Definitions/TemplateDefinition.cs:24`
- Modify: `src/Tokenizer/TokenMatcher.cs:22`
- Modify: `src/Tokenizer/Tokenizer.cs:57`
- Modify: `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs:57`
- Modify: `tests/Tokenizer.Tests/TokenizerTestBase.cs:26`
- Modify: `tests/Tokenizer.Tests/TokenizerOptionsTests.cs` (multiple lines)
- Modify: `tests/Tokenizer.Tests/TokenTests.cs` (lines 35, 51, 67, 82)
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs` (lines 61, 108)
- Modify: `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs:99`
- Modify: `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs` (many lines)
- Modify: `tests/Tokenizer.Tests/Compilation/Definitions/TemplateDefinitionTests.cs:26`
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/ConcurrencyBenchmarks.cs`
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/TokenizationBenchmarks.cs`

This is a systematic find-and-replace task. Work through each file methodically.

- [ ] **Step 1: Fix source files — replace `TokenizerOptions.Defaults` with `new TokenizerOptions()`**

In `src/Tokenizer/Compilation/TokenParser.cs:27`, change:
```csharp
public TokenParser() : this(TokenizerOptions.Defaults)
```
to:
```csharp
public TokenParser() : this(new TokenizerOptions())
```

In `src/Tokenizer/Compilation/Parsing/AstTemplateDefinitionParser.cs:24`, change:
```csharp
return Parse(template, TokenizerOptions.Defaults);
```
to:
```csharp
return Parse(template, new TokenizerOptions());
```

In `src/Tokenizer/TokenMatcher.cs:22`, change:
```csharp
public TokenMatcher() : this(TokenizerOptions.Defaults, (ILoggerFactory?)null)
```
to:
```csharp
public TokenMatcher() : this(new TokenizerOptions(), (ILoggerFactory?)null)
```

In `src/Tokenizer/Tokenizer.cs:57`, change:
```csharp
return Create(TokenizerOptions.Defaults, null);
```
to:
```csharp
return Create(new TokenizerOptions(), null);
```

- [ ] **Step 2: Fix source files — replace `.Clone()` with `with { }`**

In `src/Tokenizer/Compilation/Parsing/AstTemplateDefinitionParser.cs:31`, change:
```csharp
var result = new TemplateDefinition { Options = options.Clone() };
```
to:
```csharp
var result = new TemplateDefinition { Options = options with { } };
```

In `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs:18`, change:
```csharp
template.Options ??= TokenizerOptions.Defaults.Clone();
```
to:
```csharp
template.Options ??= new TokenizerOptions();
```

In `src/Tokenizer/Compilation/Definitions/TemplateDefinition.cs:24`, change:
```csharp
public TokenizerOptions Options { get; set; } = new TokenizerOptions();
```
This line is already correct — no change needed. The `new TokenizerOptions()` here creates defaults correctly.

- [ ] **Step 3: Fix test files — replace `TokenizerOptions.Defaults` with `new TokenizerOptions()`**

In `tests/Tokenizer.Tests/Builders/TemplateBuilder.cs:57`, change:
```csharp
_options = TokenizerOptions.Defaults;
```
to:
```csharp
_options = new TokenizerOptions();
```

In `tests/Tokenizer.Tests/TokenizerTestBase.cs:26`, change:
```csharp
return Tokenizer.Create(TokenizerOptions.Defaults, LoggerFactory);
```
to:
```csharp
return Tokenizer.Create(new TokenizerOptions(), LoggerFactory);
```

In `tests/Tokenizer.Tests/Compilation/Definitions/TemplateDefinitionTests.cs:26`, change:
```csharp
var options = TokenizerOptions.Defaults;
```
to:
```csharp
var options = new TokenizerOptions();
```

In `tests/Tokenizer.Tests/TokenTests.cs`, replace all 4 occurrences of `TokenizerOptions.Defaults` (lines 35, 51, 67, 82) with `new TokenizerOptions()`.

In `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineInternalTests.cs`, replace occurrences at lines 61 and 108:
```csharp
var options = TokenizerOptions.Defaults;
```
to:
```csharp
var options = new TokenizerOptions();
```

In `tests/Tokenizer.Tests/Tokenization/Engine/TokenizationEngineErrorTests.cs:99`:
```csharp
var options = TokenizerOptions.Defaults;
```
to:
```csharp
var options = new TokenizerOptions();
```

In `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs`, replace all occurrences of `TokenizerOptions.Defaults` (lines 14, 31, 47, 63, 78, 94, 110, 132, 153, 167, 182) with `new TokenizerOptions()`.

- [ ] **Step 4: Verify the full test suite passes**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass. Zero failures.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "Replace TokenizerOptions.Defaults with new TokenizerOptions() and .Clone() with 'with' expressions"
```

Note: `git add -A` is appropriate here because we've only modified existing files across many directories.

---

### Task 4: Replace `Tokenizer` Static Factories with Public Constructors

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs:36-82`
- Modify: `src/Tokenizer/TokenMatcher.cs:37`

- [ ] **Step 1: Write failing test — constructor-based creation**

Add to `tests/Tokenizer.Tests/TokenizerOptionsRecordTests.cs`:

```csharp
[Fact]
public void GivenNoArguments_WhenConstructingTokenizer_ThenUsesDefaultOptions()
{
    // Arrange & Act
    var tokenizer = new Tokenizer();

    // Assert
    Assert.NotNull(tokenizer.Options);
    Assert.True(tokenizer.Options.TrimTrailingWhiteSpace);
}

[Fact]
public void GivenCustomOptions_WhenConstructingTokenizer_ThenUsesProvidedOptions()
{
    // Arrange
    var options = new TokenizerOptions { TrimTrailingWhiteSpace = false };

    // Act
    var tokenizer = new Tokenizer(options);

    // Assert
    Assert.False(tokenizer.Options.TrimTrailingWhiteSpace);
}

[Fact]
public void GivenTokenizer_WhenTokenizing_ThenProducesResults()
{
    // Arrange
    var tokenizer = new Tokenizer();

    // Act
    var result = tokenizer.Tokenize("{name}", "John");

    // Assert
    Assert.True(result.Success);
    Assert.Equal("John", result.First("name"));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsRecordTests"`
Expected: Constructor tests fail — `Tokenizer` has no public constructors.

- [ ] **Step 3: Add public constructors and remove static factories**

Replace lines 33-82 in `src/Tokenizer/Tokenizer.cs` (from the `internal Tokenizer` constructor through the `Create` methods) with:

```csharp
    /// <summary>
    /// Creates a new Tokenizer with default options.
    /// </summary>
    public Tokenizer() : this(new TokenizerOptions())
    {
    }

    /// <summary>
    /// Creates a new Tokenizer with the specified options.
    /// </summary>
    public Tokenizer(TokenizerOptions options) : this(options, null)
    {
    }

    /// <summary>
    /// Creates a new Tokenizer with the specified options and logger factory.
    /// </summary>
    public Tokenizer(TokenizerOptions options, ILoggerFactory? loggerFactory)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        Options = options with { };
        log = loggerFactory.CreateLogger<Tokenizer>();
        parser = new TokenParser(Options, loggerFactory.CreateLogger<TokenParser>());
        tokenizationEngine = new TokenizationEngine(loggerFactory.CreateLogger<TokenizationEngine>());
        hintProcessor = new HintProcessor(loggerFactory.CreateLogger<HintProcessor>());
        resultBuilder = new ResultBuilder(loggerFactory.CreateLogger<ResultBuilder>());
    }

    /// <summary>
    /// Internal constructor for dependency injection.
    /// </summary>
    internal Tokenizer(
        IOptions<TokenizerOptions> options,
        ILogger<Tokenizer> logger,
        TokenParser parser,
        ITokenizationEngine tokenizationEngine,
        IHintProcessor hintProcessor,
        IResultBuilder resultBuilder)
    {
        Options = options.Value with { };
        log = logger;
        this.parser = parser;
        this.tokenizationEngine = tokenizationEngine;
        this.hintProcessor = hintProcessor;
        this.resultBuilder = resultBuilder;
    }
```

Add the required using at the top of `Tokenizer.cs`:
```csharp
using Microsoft.Extensions.Options;
```

- [ ] **Step 4: Update `TokenMatcher.cs:37` to use constructor instead of `Create()`**

Change:
```csharp
tokenizer = Tokenizer.Create(options, loggerFactory);
```
to:
```csharp
tokenizer = new Tokenizer(options, loggerFactory);
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsRecordTests"`
Expected: All tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Tokenizer.cs src/Tokenizer/TokenMatcher.cs tests/Tokenizer.Tests/TokenizerOptionsRecordTests.cs
git commit -m "Replace Tokenizer static factories with public constructors"
```

---

### Task 5: Migrate All `Tokenizer.Create()` Call Sites

**Files:**
- Modify: `tests/Tokenizer.Tests/TokenizerTestBase.cs`
- Modify: `tests/Tokenizer.Tests/TokenizerOptionsTests.cs:42`
- Modify: `tests/Tokenizer.Tests/TokenizerTests.cs:576`
- Modify: `tests/Tokenizer.Tests/TokenPropertyImmutabilityTests.cs` (lines 11, 25)
- Modify: `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs` (many lines)
- Modify: `tests/Tokenizer.Tests/Transformers/RemoveEndTransformerTests.cs:91`
- Modify: `tests/Tokenizer.Tests/Transformers/RemoveStartTransformerTests.cs:91`
- Modify: `tests/Tokenizer.Tests/Transformers/SetTransformerTests.cs` (lines 59, 73)
- Modify: `tests/Tokenizer.Tests/Transformers/ToDateTimeUtcTransformerTests.cs` (lines 74, 88, 102, 126)
- Modify: `tests/Tokenizer.Tests/Validators/` (many files)
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/ConcurrencyBenchmarks.cs` (lines 32, 64)
- Modify: `benchmarks/Tokenizer.Benchmarks/Benchmarks/TokenizationBenchmarks.cs:27`

Systematic replacement of `Tokenizer.Create()` → `new Tokenizer()` and `Tokenizer.Create(options)` → `new Tokenizer(options)` and `Tokenizer.Create(options, loggerFactory)` → `new Tokenizer(options, loggerFactory)`.

- [ ] **Step 1: Update `TokenizerTestBase.cs`**

Change `CreateTokenizer()` (line 26):
```csharp
return Tokenizer.Create(new TokenizerOptions(), LoggerFactory);
```
to:
```csharp
return new Tokenizer(new TokenizerOptions(), LoggerFactory);
```

Change `CreateTokenizer(TokenizerOptions options)` (line 34):
```csharp
return Tokenizer.Create(options, LoggerFactory);
```
to:
```csharp
return new Tokenizer(options, LoggerFactory);
```

- [ ] **Step 2: Replace `Tokenizer.Create()` in test files**

Replace all occurrences of `Tokenizer.Create()` with `new Tokenizer()` in:
- `tests/Tokenizer.Tests/TokenizerOptionsTests.cs:42`
- `tests/Tokenizer.Tests/TokenizerTests.cs:576`
- `tests/Tokenizer.Tests/TokenPropertyImmutabilityTests.cs` (lines 11, 25)
- `tests/Tokenizer.Tests/Transformers/RemoveEndTransformerTests.cs:91`
- `tests/Tokenizer.Tests/Transformers/RemoveStartTransformerTests.cs:91`
- `tests/Tokenizer.Tests/Transformers/SetTransformerTests.cs` (lines 59, 73)
- `tests/Tokenizer.Tests/Transformers/ToDateTimeUtcTransformerTests.cs` (lines 74, 88, 102, 126)
- All validator test files in `tests/Tokenizer.Tests/Validators/`

Replace all occurrences of `Tokenizer.Create(options)` with `new Tokenizer(options)` in:
- `tests/Tokenizer.Tests/Safety/TokenizerSafetyLimitTests.cs` (lines 16, 33, 49, 65, 80, 96, 112, 134, 155, 168, 184, 198)

- [ ] **Step 3: Replace `Tokenizer.Create()` in benchmark files**

In `benchmarks/Tokenizer.Benchmarks/Benchmarks/ConcurrencyBenchmarks.cs`:
- Line 32: `Tokenizer.Create()` → `new Tokenizer()`
- Line 64: `Tokenizer.Create()` → `new Tokenizer()`

In `benchmarks/Tokenizer.Benchmarks/Benchmarks/TokenizationBenchmarks.cs`:
- Line 27: `Tokenizer.Create()` → `new Tokenizer()`

- [ ] **Step 4: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 5: Verify benchmarks build**

Run: `dotnet build ./benchmarks/Tokenizer.Benchmarks/ -c Release`
Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "Migrate all Tokenizer.Create() call sites to constructors"
```

---

### Task 6: Rewrite DI Registration with `IOptions<TokenizerOptions>`

**Files:**
- Modify: `src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs`

- [ ] **Step 1: Write failing tests for new DI overloads**

Add to `tests/Tokenizer.Tests/Integration/DependencyInjectionTests.cs`:

```csharp
using Microsoft.Extensions.Configuration;

// ... inside the class:

[Fact]
public void AddTokenizer_WithConfigurationSection_BindsOptions()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tokenizer:TrimTrailingWhiteSpace"] = "false",
            ["Tokenizer:OutOfOrderTokens"] = "true"
        })
        .Build();

    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddSerilog());

    // Act
    services.AddTokenizer(configuration.GetSection("Tokenizer"));
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var tokenizer = serviceProvider.GetRequiredService<Tokenizer>();
    Assert.False(tokenizer.Options.TrimTrailingWhiteSpace);
    Assert.True(tokenizer.Options.OutOfOrderTokens);
}

[Fact]
public void AddTokenizer_WithOptionsInstance_UsesProvidedOptions()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddSerilog());
    var options = new TokenizerOptions
    {
        MaxInputLength = 512,
        EnableDiagnostics = true
    };

    // Act
    services.AddTokenizer(options);
    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var tokenizer = serviceProvider.GetRequiredService<Tokenizer>();
    Assert.Equal(512, tokenizer.Options.MaxInputLength);
    Assert.True(tokenizer.Options.EnableDiagnostics);
}
```

Add the required using at the top:
```csharp
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DependencyInjectionTests"`
Expected: New tests fail — overloads don't exist yet.

- [ ] **Step 3: Rewrite `TokenizerServiceCollectionExtensions`**

Replace the entire contents of `src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs` with:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tokens.Tokenization;

namespace Tokens.Extensions;

/// <summary>
/// Extension methods for configuring Tokenizer services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class TokenizerServiceCollectionExtensions
{
    /// <summary>
    /// Adds Tokenizer services to the specified <see cref="IServiceCollection"/> with default options.
    /// </summary>
    public static IServiceCollection AddTokenizer(this IServiceCollection services)
    {
        return services.AddTokenizer(_ => { });
    }

    /// <summary>
    /// Adds Tokenizer services to the specified <see cref="IServiceCollection"/>
    /// configured via the provided delegate.
    /// </summary>
    public static IServiceCollection AddTokenizer(
        this IServiceCollection services,
        Action<TokenizerOptions> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        services.Configure(configure);
        RegisterCoreServices(services);
        return services;
    }

    /// <summary>
    /// Adds Tokenizer services to the specified <see cref="IServiceCollection"/>
    /// bound to a configuration section (e.g. from appsettings.json).
    /// </summary>
    public static IServiceCollection AddTokenizer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        services.Configure<TokenizerOptions>(configuration);
        RegisterCoreServices(services);
        return services;
    }

    /// <summary>
    /// Adds Tokenizer services to the specified <see cref="IServiceCollection"/>
    /// using a pre-constructed <see cref="TokenizerOptions"/> instance.
    /// </summary>
    public static IServiceCollection AddTokenizer(
        this IServiceCollection services,
        TokenizerOptions options)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (options == null) throw new ArgumentNullException(nameof(options));

        services.AddSingleton(Options.Create(options));
        RegisterCoreServices(services);
        return services;
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.TryAddSingleton<Compilation.TokenParser>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<TokenizerOptions>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Compilation.TokenParser>();
            return new Compilation.TokenParser(opts.Value, logger);
        });

        services.TryAddSingleton<ITokenizationEngine>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<TokenizationEngine>();
            return new TokenizationEngine(logger);
        });

        services.TryAddSingleton<IHintProcessor>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<HintProcessor>();
            return new HintProcessor(logger);
        });

        services.TryAddSingleton<IResultBuilder>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ResultBuilder>();
            return new ResultBuilder(logger);
        });

        services.TryAddSingleton<Tokenizer>(sp =>
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
    }
}
```

- [ ] **Step 4: Run all DI tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DependencyInjectionTests"`
Expected: All tests pass (existing + new).

- [ ] **Step 5: Run the full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/Extensions/TokenizerServiceCollectionExtensions.cs tests/Tokenizer.Tests/Integration/DependencyInjectionTests.cs
git commit -m "Adopt IOptions<TokenizerOptions> pattern in DI registration"
```

---

### Task 7: Update `FrontMatterBinder` to Use `with` Expressions

**Files:**
- Modify: `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs:68-113`

The FrontMatterBinder currently mutates `template.Options` property by property. It should instead build a new options instance with only the front-matter-specified values overridden.

- [ ] **Step 1: Write a failing test for partial override behavior**

Add a new test file `tests/Tokenizer.Tests/Compilation/Binders/FrontMatterBinderTests.cs`:

```csharp
using Tokens.Compilation;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tests.Compilation.Binders;

public class FrontMatterBinderTests : TokenizerTestBase
{
    public FrontMatterBinderTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenFrontMatterWithPartialOverrides_WhenParsed_ThenUnspecifiedOptionsRetainDefaults()
    {
        // Arrange — only override TrimPreambleBeforeNewLine, leave everything else at defaults
        const string content = "---\nTrimPreambleBeforeNewLine: true\n---\nHello { Name }";
        var parser = new TokenParser(new TokenizerOptions { TrimTrailingWhiteSpace = false });

        // Act
        var template = parser.Parse(content);

        // Assert — TrimPreambleBeforeNewLine is overridden by front matter
        Assert.True(template.Options.TrimPreambleBeforeNewLine);
        // TrimTrailingWhiteSpace should retain the value from the parser's options, not reset to default
        Assert.False(template.Options.TrimTrailingWhiteSpace);
    }

    [Fact]
    public void GivenFrontMatterOptions_WhenParsed_ThenOriginalOptionsAreUnchanged()
    {
        // Arrange
        var originalOptions = new TokenizerOptions();
        const string content = "---\nOutOfOrder: true\nTerminateOnNewLine: true\n---\nHello { Name }";
        var parser = new TokenParser(originalOptions);

        // Act
        var template = parser.Parse(content);

        // Assert — template has overridden values
        Assert.True(template.Options.OutOfOrderTokens);
        Assert.True(template.Options.TerminateOnNewLine);
        // Original options should be unchanged
        Assert.False(originalOptions.OutOfOrderTokens);
        Assert.False(originalOptions.TerminateOnNewLine);
    }
}
```

- [ ] **Step 2: Run test to verify current behavior**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FrontMatterBinderTests"`
Expected: Tests should pass with current implementation (since Clone already copies). If they pass, we know the refactor must preserve this behavior.

- [ ] **Step 3: Refactor `FrontMatterBinder.ApplyOption` to use `with` expression**

Replace the `ApplyOption` method in `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs` (lines 68-114) with:

```csharp
    private static void ApplyOption(TemplateDefinition template, FrontMatterEntry entry)
    {
        var key = (entry.Key ?? string.Empty).Trim().ToLowerInvariant();
        var rawName = entry.Key ?? string.Empty;
        var value = entry.Value ?? string.Empty;

        switch (key)
        {
            case "trimleadingwhitespace":
                template.Options = template.Options with
                {
                    TrimLeadingWhitespaceInTokenPreamble = ParseBoolean(value, rawName, entry)
                };
                break;
            case "trimtrailingwhitespace":
                template.Options = template.Options with
                {
                    TrimTrailingWhiteSpace = ParseBoolean(value, rawName, entry)
                };
                break;
            case "trimpreamblebeforenewline":
                template.Options = template.Options with
                {
                    TrimPreambleBeforeNewLine = ParseBoolean(value, rawName, entry)
                };
                break;
            case "outoforder":
                template.Options = template.Options with
                {
                    OutOfOrderTokens = ParseBoolean(value, rawName, entry)
                };
                break;
            case "terminateonnewline":
                template.Options = template.Options with
                {
                    TerminateOnNewLine = ParseBoolean(value, rawName, entry)
                };
                break;
            case "ignoremissingproperties":
                template.Options = template.Options with
                {
                    IgnoreMissingProperties = ParseBoolean(value, rawName, entry)
                };
                break;
            case "casesensitive":
                template.Options = template.Options with
                {
                    TokenStringComparison = ParseBoolean(value, rawName, entry)
                        ? System.StringComparison.InvariantCulture
                        : System.StringComparison.InvariantCultureIgnoreCase
                };
                break;
            case "name":
                template.Name = value.Trim();
                break;
            case "hint":
                template.Hints.Add(new Hint(Text: entry.RawValue.Trim(), Optional: false));
                break;
            case "hint?":
                template.Hints.Add(new Hint(Text: entry.RawValue.Trim(), Optional: true));
                break;
            case "tag":
                template.Tags.Add(value.Trim());
                break;
            default:
                throw new ParsingException($"Unknown front matter option: {rawName}", entry.Location);
        }
    }
```

- [ ] **Step 4: Run tests to verify behavior is preserved**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FrontMatterBinderTests|TokenizerOptionsTests"`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs tests/Tokenizer.Tests/Compilation/Binders/FrontMatterBinderTests.cs
git commit -m "Refactor FrontMatterBinder to use 'with' expressions instead of mutation"
```

---

### Task 8: Implement `IReadOnlyCollection<Template>` on `TemplateCollection`

**Files:**
- Modify: `src/Tokenizer/TemplateCollection.cs`

- [ ] **Step 1: Write failing tests**

Add a new test file `tests/Tokenizer.Tests/TemplateCollectionTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Tokens.Builders;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tests;

public class TemplateCollectionTests : TokenizerTestBase
{
    public TemplateCollectionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenCollectionWithTemplates_WhenEnumerated_ThenReturnsAllTemplates()
    {
        // Arrange
        var collection = new TemplateCollection();
        var template1 = new TemplateBuilder().WithName("first").WithContent("a").Build();
        var template2 = new TemplateBuilder().WithName("second").WithContent("b").Build();
        collection.Add(template1);
        collection.Add(template2);

        // Act
        var templates = collection.ToList();

        // Assert
        Assert.Equal(2, templates.Count);
        Assert.Contains(templates, t => t.Name == "first");
        Assert.Contains(templates, t => t.Name == "second");
    }

    [Fact]
    public void GivenEmptyCollection_WhenEnumerated_ThenReturnsEmpty()
    {
        // Arrange
        var collection = new TemplateCollection();

        // Act
        var templates = collection.ToList();

        // Assert
        Assert.Empty(templates);
    }

    [Fact]
    public void GivenCollection_WhenUsedWithLinq_ThenSupportsLinqOperations()
    {
        // Arrange
        var collection = new TemplateCollection();
        collection.Add(new TemplateBuilder().WithName("alpha").WithContent("a").Build());
        collection.Add(new TemplateBuilder().WithName("beta").WithContent("b").Build());

        // Act
        var names = collection.Select(t => t.Name).OrderBy(n => n).ToList();

        // Assert
        Assert.Equal(new[] { "alpha", "beta" }, names);
    }

    [Fact]
    public void GivenCollection_WhenCastToInterface_ThenIsIReadOnlyCollection()
    {
        // Arrange & Act
        IReadOnlyCollection<Template> collection = new TemplateCollection();

        // Assert
        Assert.NotNull(collection);
        Assert.Equal(0, collection.Count);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateCollectionTests"`
Expected: Compilation error — `TemplateCollection` doesn't implement `IEnumerable<Template>`, so `ToList()` and `Select()` fail.

- [ ] **Step 3: Implement `IReadOnlyCollection<Template>`**

Modify `src/Tokenizer/TemplateCollection.cs`:

Add to the using statements:
```csharp
using System.Collections;
```

Change the class declaration from:
```csharp
public class TemplateCollection
```
to:
```csharp
public class TemplateCollection : IReadOnlyCollection<Template>
```

Add the enumerator methods at the end of the class, before the closing brace:

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TemplateCollectionTests"`
Expected: All 4 tests PASS.

- [ ] **Step 5: Run the full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tokenizer/TemplateCollection.cs tests/Tokenizer.Tests/TemplateCollectionTests.cs
git commit -m "Implement IReadOnlyCollection<Template> on TemplateCollection"
```

---

### Task 9: Clean Up `TokenParser.Options` Mutability

**Files:**
- Modify: `src/Tokenizer/Compilation/TokenParser.cs:25`
- Modify: `tests/Tokenizer.Tests/TokenizerOptionsTests.cs`

The `TokenParser.Options` property currently has a public setter, which allows mutation after construction. This should be locked down.

- [ ] **Step 1: Change `TokenParser.Options` setter to private**

In `src/Tokenizer/Compilation/TokenParser.cs:25`, change:
```csharp
public TokenizerOptions Options { get; set; }
```
to:
```csharp
public TokenizerOptions Options { get; private set; }
```

- [ ] **Step 2: Fix test code that directly mutates `parser.Options`**

In `tests/Tokenizer.Tests/TokenizerOptionsTests.cs`, the tests at lines 21, 58, 74, 91, 107 set `parser.Options.TrimPreambleBeforeNewLine = true/false`. Since `TokenizerOptions` is now a record with `get; set;`, these mutations of the property values still compile — it's the setter on `TokenParser.Options` we locked down, not the record properties. So these tests should still work because they're mutating the options object's properties, not replacing the `Options` property on the parser.

Verify this by running:

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TokenizerOptionsTests"`
Expected: All tests pass.

- [ ] **Step 3: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Make TokenParser.Options setter private to prevent external reassignment"
```

---

### Task 10: Final Verification and Build

- [ ] **Step 1: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass. Zero failures.

- [ ] **Step 2: Build release configuration**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`
Expected: Build succeeds with no warnings related to the changes.

- [ ] **Step 3: Build benchmarks**

Run: `dotnet build ./benchmarks/Tokenizer.Benchmarks/ -c Release`
Expected: Build succeeds.

- [ ] **Step 4: Verify no remaining references to removed APIs**

Run a grep to confirm no lingering references:

```bash
grep -rn "TokenizerOptions\.Defaults" src/ tests/ benchmarks/ --include="*.cs"
grep -rn "Tokenizer\.Create(" src/ tests/ benchmarks/ --include="*.cs"
grep -rn "\.Clone()" src/Tokenizer/TokenizerOptions.cs
```

Expected: Zero matches for all three commands.

- [ ] **Step 5: Commit any remaining fixes if needed**
