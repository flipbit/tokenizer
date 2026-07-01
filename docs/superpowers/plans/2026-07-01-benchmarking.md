# Benchmarking Project Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a BenchmarkDotNet-based benchmark project measuring compilation, tokenization, multi-template matching, and concurrent usage with synthetic workloads covering all transformers and validators.

**Architecture:** Four benchmark classes (`CompilationBenchmarks`, `TokenizationBenchmarks`, `MatcherBenchmarks`, `ConcurrencyBenchmarks`) sharing a `WorkloadGenerator` that produces synthetic templates/inputs at three tiers (small/medium/large). A shared `BenchmarkConfig` applies `MemoryDiagnoser` and `ThreadingDiagnoser` to all benchmarks.

**Tech Stack:** C# / .NET 10.0, BenchmarkDotNet (latest stable), Tokenizer library via project reference

---

## File Structure

| File | Responsibility |
|------|---------------|
| `benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj` | Console app project, BenchmarkDotNet dependency, project reference |
| `benchmarks/Tokenizer.Benchmarks/Program.cs` | `BenchmarkSwitcher` entry point |
| `benchmarks/Tokenizer.Benchmarks/Config/BenchmarkConfig.cs` | Shared `ManualConfig` with diagnosers and exporters |
| `benchmarks/Tokenizer.Benchmarks/Data/SmallRecord.cs` | POCO for small tier (3-5 properties) |
| `benchmarks/Tokenizer.Benchmarks/Data/MediumRecord.cs` | POCO for medium tier (10-15 properties) |
| `benchmarks/Tokenizer.Benchmarks/Data/LargeRecord.cs` | POCO for large tier (30-50 properties) |
| `benchmarks/Tokenizer.Benchmarks/Data/WorkloadGenerator.cs` | Static class generating template strings and input text for all tiers |
| `benchmarks/Tokenizer.Benchmarks/Benchmarks/CompilationBenchmarks.cs` | `TokenParser.Parse()` benchmarks at three tiers |
| `benchmarks/Tokenizer.Benchmarks/Benchmarks/TokenizationBenchmarks.cs` | `Tokenizer.Tokenize<T>()` benchmarks at three tiers |
| `benchmarks/Tokenizer.Benchmarks/Benchmarks/MatcherBenchmarks.cs` | `TokenMatcher.Match<T>()` with `[Params]` template counts |
| `benchmarks/Tokenizer.Benchmarks/Benchmarks/ConcurrencyBenchmarks.cs` | Parallel stress tests with shared/per-thread instances |
| `src/Tokenizer/Properties/AssemblyInfo.cs` | Add `InternalsVisibleTo("Tokenizer.Benchmarks")` |
| `Tokenizer.sln` | Add benchmark project |

---

### Task 1: Project scaffolding and configuration

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj`
- Create: `benchmarks/Tokenizer.Benchmarks/Program.cs`
- Create: `benchmarks/Tokenizer.Benchmarks/Config/BenchmarkConfig.cs`
- Modify: `src/Tokenizer/Properties/AssemblyInfo.cs`
- Modify: `Tokenizer.sln`

- [ ] **Step 1: Create the project directory**

```bash
mkdir -p benchmarks/Tokenizer.Benchmarks/Config
mkdir -p benchmarks/Tokenizer.Benchmarks/Data
mkdir -p benchmarks/Tokenizer.Benchmarks/Benchmarks
```

- [ ] **Step 2: Create the .csproj file**

Create `benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Tokens</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Tokenizer\Tokenizer.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create Program.cs**

Create `benchmarks/Tokenizer.Benchmarks/Program.cs`:

```csharp
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

- [ ] **Step 4: Create BenchmarkConfig.cs**

Create `benchmarks/Tokenizer.Benchmarks/Config/BenchmarkConfig.cs`:

```csharp
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace Tokens.Config;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(ThreadingDiagnoser.Default);
        AddColumn(StatisticColumn.P95);
        AddExporter(MarkdownExporter.GitHub);
    }
}
```

- [ ] **Step 5: Add InternalsVisibleTo for the benchmark project**

In `src/Tokenizer/Properties/AssemblyInfo.cs`, add:

```csharp
[assembly: InternalsVisibleTo("Tokenizer.Benchmarks")]
```

This is needed because `TokenParser` is `internal` and `CompilationBenchmarks` needs to call `TokenParser.Parse()` directly to isolate compilation cost.

- [ ] **Step 6: Add the project to the solution**

```bash
dotnet sln Tokenizer.sln add benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj
```

- [ ] **Step 7: Verify the project builds**

```bash
dotnet build benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 8: Verify existing tests still pass**

```bash
dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 9: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj \
       benchmarks/Tokenizer.Benchmarks/Program.cs \
       benchmarks/Tokenizer.Benchmarks/Config/BenchmarkConfig.cs \
       src/Tokenizer/Properties/AssemblyInfo.cs \
       Tokenizer.sln
git commit -m "Add benchmark project scaffolding with BenchmarkDotNet"
```

---

### Task 2: Record POCOs

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Data/SmallRecord.cs`
- Create: `benchmarks/Tokenizer.Benchmarks/Data/MediumRecord.cs`
- Create: `benchmarks/Tokenizer.Benchmarks/Data/LargeRecord.cs`

These POCOs have properties that match the token names used in `WorkloadGenerator` templates (Task 3). Property names use the `Record.PropertyName` convention — the `Record` prefix is the class name BenchmarkDotNet deserializes to; the token names reference `Record.X` which maps to property `X` on the target type.

- [ ] **Step 1: Create SmallRecord.cs**

Create `benchmarks/Tokenizer.Benchmarks/Data/SmallRecord.cs`:

```csharp
namespace Tokens.Data;

/// <summary>
/// Target POCO for small-tier benchmarks (3-5 tokens).
/// Exercises: Trim, IsNotEmpty, ToUpper.
/// </summary>
public class SmallRecord
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create MediumRecord.cs**

Create `benchmarks/Tokenizer.Benchmarks/Data/MediumRecord.cs`:

```csharp
namespace Tokens.Data;

/// <summary>
/// Target POCO for medium-tier benchmarks (10-15 tokens).
/// Exercises: Trim, ToUpper, ToLower, ToDateTime, SubstringBefore,
/// SubstringAfter, Replace, IsNotEmpty, IsNumeric, IsEmail,
/// IsDomainName, IsDateTime, Contains, StartsWith.
/// </summary>
public class MediumRecord
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Count { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Create LargeRecord.cs**

Create `benchmarks/Tokenizer.Benchmarks/Data/LargeRecord.cs`:

```csharp
using System.Collections.Generic;

namespace Tokens.Data;

/// <summary>
/// Target POCO for large-tier benchmarks (30-50 tokens).
/// Exercises all transformers and validators.
/// </summary>
public class LargeRecord
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LooseUrl { get; set; } = string.Empty;
    public string AbsoluteUrl { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Count { get; set; } = string.Empty;
    public string Total { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public object Keywords { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
    public string Found { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Verify build**

```bash
dotnet build benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Data/SmallRecord.cs \
       benchmarks/Tokenizer.Benchmarks/Data/MediumRecord.cs \
       benchmarks/Tokenizer.Benchmarks/Data/LargeRecord.cs
git commit -m "Add benchmark target POCOs for small, medium, and large tiers"
```

---

### Task 3: WorkloadGenerator

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Data/WorkloadGenerator.cs`

The generator produces template pattern strings and matching input text. Templates use the Tokenizer's template syntax with front matter, tokens, transformers, and validators. Input text is crafted so that all validators pass and all transformers produce meaningful output.

**Important syntax rules** (from codebase analysis):
- Token format: `{ Target.Property }` or `{ Target.Property : Transformer, Validator }`
- Optional tokens use `?`: `{ Target.Property ? }`
- Repeating tokens: `{ Target.Property : Repeating }`
- Front matter between `---` markers with `name:`, `tag:`, `hint:`, `set:`, `terminateOnNewLine:` directives
- Transformer args in parens: `ToDateTime('yyyy-MM-dd')`, `Replace('x', 'y')`, `SubstringBefore('.')`
- Validator args in parens: `MaxLength(100)`, `MinLength(3)`, `Contains('x')`, `StartsWith('A')`, `EndsWith('z')`, `IsNot('bad')`

- [ ] **Step 1: Create WorkloadGenerator.cs**

Create `benchmarks/Tokenizer.Benchmarks/Data/WorkloadGenerator.cs`:

```csharp
namespace Tokens.Data;

/// <summary>
/// Generates synthetic template patterns and matching input text at three
/// workload tiers for benchmarking. All templates compile successfully and
/// all inputs produce successful tokenization with all validators passing.
/// </summary>
public static class WorkloadGenerator
{
    // ── Small tier: 3 tokens ──────────────────────────────────────────

    /// <summary>
    /// Small template: 3 tokens exercising Trim, IsNotEmpty, ToUpper.
    /// </summary>
    public static string SmallTemplate() =>
        """
        Name: { SmallRecord.Name : Trim, IsNotEmpty }
        Status: { SmallRecord.Status : ToUpper }
        Code: { SmallRecord.Code : Trim }
        """;

    /// <summary>
    /// Input text that matches <see cref="SmallTemplate"/>.
    /// </summary>
    public static string SmallInput() =>
        """
        Name: Alice Johnson
        Status: active
        Code: ABC-123
        """;

    // ── Medium tier: 12 tokens ────────────────────────────────────────

    /// <summary>
    /// Medium template: 12 tokens exercising Trim, ToUpper, ToLower,
    /// ToDateTime, SubstringBefore, SubstringAfter, Replace,
    /// IsNotEmpty, IsNumeric, IsEmail, IsDomainName, IsDateTime,
    /// Contains, StartsWith.
    /// </summary>
    public static string MediumTemplate() =>
        """
        Name: { MediumRecord.Name : Trim, IsNotEmpty }
        Email: { MediumRecord.Email : ToLower, IsEmail }
        Domain: { MediumRecord.Domain : ToLower, IsDomainName }
        Code: { MediumRecord.Code : ToUpper, StartsWith('R') }
        Count: { MediumRecord.Count : Trim, IsNumeric }
        Created: { MediumRecord.Created : Trim, ToDateTime('yyyy-MM-dd'), IsDateTime }
        Status: { MediumRecord.Status : ToUpper, Contains('ACT') }
        Description: { MediumRecord.Description : SubstringBefore('.') }
        Category: { MediumRecord.Category : SubstringAfter('-') }
        Reference: { MediumRecord.Reference : Replace('REF', 'R') }
        Tag: { MediumRecord.Tag : Trim }
        Origin: { MediumRecord.Origin : Trim }
        """;

    /// <summary>
    /// Input text that matches <see cref="MediumTemplate"/>.
    /// </summary>
    public static string MediumInput() =>
        """
        Name: Bob Smith
        Email: BOB@EXAMPLE.COM
        Domain: EXAMPLE.COM
        Code: ref-42
        Count: 12345
        Created: 2024-06-15
        Status: active
        Description: This is a test record. Extra text here.
        Category: type-electronics
        Reference: REF-001
        Tag: benchmark
        Origin: synthetic
        """;

    // ── Large tier: 39 tokens including repeating, front matter ──────

    /// <summary>
    /// Large template: 39 tokens exercising every transformer and validator.
    /// Includes front matter with name, tags, hints, and set directive.
    /// Includes repeating tokens.
    /// </summary>
    public static string LargeTemplate() =>
        """
        ---
        name: large-benchmark-template
        tag: benchmark
        tag: performance
        hint: Record Entry
        set: Found = Yes
        terminateOnNewLine: true
        ---

        Record Entry

        Name: { LargeRecord.Name : Trim, IsNotEmpty, MinLength(2) }
        Email: { LargeRecord.Email : ToLower, IsEmail }
        Phone: { LargeRecord.Phone : Trim, IsPhoneNumber }
        Domain: { LargeRecord.Domain : ToLower, IsDomainName }
        URL: { LargeRecord.Url : Trim, IsUrl }
        Loose URL: { LargeRecord.LooseUrl : Trim, IsLooseUrl }
        Absolute URL: { LargeRecord.AbsoluteUrl : Trim, IsLooseAbsoluteUrl }
        Code: { LargeRecord.Code : ToUpper, StartsWith('R'), MaxLength(20) }
        Count: { LargeRecord.Count : Trim, IsNumeric }
        Total: { LargeRecord.Total : Trim, IsNumeric, IsNot('0') }
        Created: { LargeRecord.Created : Trim, ToDateTime('yyyy-MM-dd'), IsDateTime }
        Updated: { LargeRecord.Updated : Trim, ToDateTimeUtc('yyyy-MM-dd') }
        Status: { LargeRecord.Status : ToUpper, Contains('ACT') }
        Type: { LargeRecord.Type : Trim, EndsWith('ry') }
        Description: { LargeRecord.Description : SubstringBefore('.'), MinLength(5) }
        Summary: { LargeRecord.Summary : SubstringAfter(':') }
        Notes: { LargeRecord.Notes : SubstringBeforeLast('.') }
        Category: { LargeRecord.Category : SubstringAfterLast('-') }
        SubCategory: { LargeRecord.SubCategory : Remove('#') }
        Reference: { LargeRecord.Reference : RemoveStart('REF-') }
        Identifier: { LargeRecord.Identifier : RemoveEnd('-ID') }
        Tag: { LargeRecord.Tag : Replace('_', '-') }
        Label: { LargeRecord.Label : Trim, IsNotEmpty }
        Origin: { LargeRecord.Origin : Trim, Contains('syn') }
        Source: { LargeRecord.Source : Trim }
        Target: { LargeRecord.Target : Trim }
        Priority: { LargeRecord.Priority : ToUpper }
        Level: { LargeRecord.Level : ToLower }
        Rating: { LargeRecord.Rating : Trim, IsNumeric }
        Score: { LargeRecord.Score : Trim, IsNumeric }
        Version: { LargeRecord.Version : Trim }
        Region: { LargeRecord.Region : Trim }
        Country: { LargeRecord.Country : Trim }
        City: { LargeRecord.City : Trim }
        Address: { LargeRecord.Address : Trim }
        PostalCode: { LargeRecord.PostalCode : Trim }
        Comment: { LargeRecord.Comment : Trim, MaxLength(200) }
        Keywords: { LargeRecord.Keywords : Split(',') }
        Items: { LargeRecord.Items : Trim, IsNotEmpty, Repeating }
        """;

    /// <summary>
    /// Input text that matches <see cref="LargeTemplate"/>.
    /// Values are chosen to pass all validators and exercise all transformers.
    /// </summary>
    public static string LargeInput() =>
        """
        Record Entry

        Name: Alice Johnson
        Email: ALICE@EXAMPLE.COM
        Phone: +1-555-0123
        Domain: EXAMPLE.COM
        URL: https://example.com/page
        Loose URL: example.com/page
        Absolute URL: https://example.com/absolute
        Code: ref-benchmark-01
        Count: 99999
        Total: 42
        Created: 2024-06-15
        Updated: 2024-07-20
        Status: active
        Type: category
        Description: Performance benchmark test record. Extra text here.
        Summary: Result: all tests passed
        Notes: First note. Second note. Final note.
        Category: type-sub-electronics
        SubCategory: test#value
        Reference: REF-001
        Identifier: BENCH-ID
        Tag: bench_mark
        Label: primary
        Origin: synthetic-data
        Source: generator
        Target: output
        Priority: high
        Level: INFO
        Rating: 95
        Score: 88
        Version: 2.0.1
        Region: us-east-1
        Country: United States
        City: New York
        Address: 123 Main Street
        PostalCode: 10001
        Comment: Synthetic benchmark record for performance testing
        Keywords: alpha,beta,gamma
        Items: item-alpha
        Items: item-beta
        Items: item-gamma
        """;

    // ── Non-matching templates for MatcherBenchmarks ─────────────────

    /// <summary>
    /// Generates a non-matching template with a unique hint that won't
    /// appear in the medium input. Used to fill TokenMatcher with
    /// templates that fail hint filtering quickly.
    /// </summary>
    public static string NonMatchingTemplate(int index) =>
        $"""
        ---
        name: non-matching-{index}
        hint: XYZZY-NOMATCH-{index}
        ---

        XYZZY-NOMATCH-{index}

        Field: {{ NonMatch.Field }}
        """;
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Data/WorkloadGenerator.cs
git commit -m "Add WorkloadGenerator with synthetic templates covering all transformers and validators"
```

---

### Task 4: CompilationBenchmarks

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Benchmarks/CompilationBenchmarks.cs`

Uses `TokenParser.Parse()` directly (via `InternalsVisibleTo`) to isolate compilation cost from tokenization.

- [ ] **Step 1: Create CompilationBenchmarks.cs**

Create `benchmarks/Tokenizer.Benchmarks/Benchmarks/CompilationBenchmarks.cs`:

```csharp
using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures template compilation cost: TokenParser.Parse() pipeline
/// (lexer -> parser -> AST -> definition -> front matter binding).
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CompilationBenchmarks
{
    private string smallTemplate = null!;
    private string mediumTemplate = null!;
    private string largeTemplate = null!;
    private TokenParser parser = null!;

    [GlobalSetup]
    public void Setup()
    {
        smallTemplate = WorkloadGenerator.SmallTemplate();
        mediumTemplate = WorkloadGenerator.MediumTemplate();
        largeTemplate = WorkloadGenerator.LargeTemplate();
        parser = new TokenParser();
    }

    [Benchmark(Description = "Compile small template (3 tokens)")]
    public Template CompileSmall() => parser.Parse(smallTemplate, "small");

    [Benchmark(Description = "Compile medium template (12 tokens)")]
    public Template CompileMedium() => parser.Parse(mediumTemplate, "medium");

    [Benchmark(Description = "Compile large template (39 tokens, front matter)")]
    public Template CompileLarge() => parser.Parse(largeTemplate, "large");
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release
```

Expected: Build succeeded.

- [ ] **Step 3: Verify dry run**

```bash
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks -- --filter *Compilation* --job dry
```

Expected: BenchmarkDotNet runs and produces results (dry run is fast, ~seconds). All three benchmarks execute without exceptions.

- [ ] **Step 4: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Benchmarks/CompilationBenchmarks.cs
git commit -m "Add CompilationBenchmarks measuring template parsing at three tiers"
```

---

### Task 5: TokenizationBenchmarks

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Benchmarks/TokenizationBenchmarks.cs`

Pre-compiles templates in `[GlobalSetup]` and benchmarks `Tokenizer.Tokenize<T>()` to measure tokenization engine cost in isolation.

- [ ] **Step 1: Create TokenizationBenchmarks.cs**

Create `benchmarks/Tokenizer.Benchmarks/Benchmarks/TokenizationBenchmarks.cs`:

```csharp
using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures tokenization cost against pre-compiled templates.
/// Isolates the tokenization engine, hint processing, result building,
/// transformer execution, and validator execution.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class TokenizationBenchmarks
{
    private Tokenizer tokenizer = null!;
    private Template smallTemplate = null!;
    private Template mediumTemplate = null!;
    private Template largeTemplate = null!;
    private string smallInput = null!;
    private string mediumInput = null!;
    private string largeInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        tokenizer = Tokenizer.Create();
        var parser = new TokenParser();

        smallTemplate = parser.Parse(WorkloadGenerator.SmallTemplate(), "small");
        mediumTemplate = parser.Parse(WorkloadGenerator.MediumTemplate(), "medium");
        largeTemplate = parser.Parse(WorkloadGenerator.LargeTemplate(), "large");

        smallInput = WorkloadGenerator.SmallInput();
        mediumInput = WorkloadGenerator.MediumInput();
        largeInput = WorkloadGenerator.LargeInput();
    }

    [Benchmark(Description = "Tokenize small (3 tokens)")]
    public TokenizeResult<SmallRecord> TokenizeSmall()
        => tokenizer.Tokenize<SmallRecord>(smallTemplate, smallInput);

    [Benchmark(Description = "Tokenize medium (12 tokens)")]
    public TokenizeResult<MediumRecord> TokenizeMedium()
        => tokenizer.Tokenize<MediumRecord>(mediumTemplate, mediumInput);

    [Benchmark(Description = "Tokenize large (39 tokens, front matter)")]
    public TokenizeResult<LargeRecord> TokenizeLarge()
        => tokenizer.Tokenize<LargeRecord>(largeTemplate, largeInput);
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release
```

Expected: Build succeeded.

- [ ] **Step 3: Verify dry run**

```bash
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks -- --filter *Tokenization* --job dry
```

Expected: All three benchmarks execute without exceptions.

- [ ] **Step 4: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Benchmarks/TokenizationBenchmarks.cs
git commit -m "Add TokenizationBenchmarks measuring tokenization at three tiers"
```

---

### Task 6: MatcherBenchmarks

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Benchmarks/MatcherBenchmarks.cs`

Measures `TokenMatcher.Match<T>()` with `[Params(5, 15, 50)]` template counts. Uses medium-tier templates. Tests both best-first and best-last positioning to measure hint filtering effectiveness.

- [ ] **Step 1: Create MatcherBenchmarks.cs**

Create `benchmarks/Tokenizer.Benchmarks/Benchmarks/MatcherBenchmarks.cs`:

```csharp
using BenchmarkDotNet.Attributes;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures TokenMatcher.Match() with varying numbers of registered templates.
/// Tests how match cost scales with template count and whether hint-based
/// filtering effectively prunes non-matching templates.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class MatcherBenchmarks
{
    [Params(5, 15, 50)]
    public int TemplateCount { get; set; }

    private TokenMatcher matcherBestFirst = null!;
    private TokenMatcher matcherBestLast = null!;
    private string mediumInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        mediumInput = WorkloadGenerator.MediumInput();

        var matchingTemplate = WorkloadGenerator.MediumTemplate();

        // Best-first: matching template registered first, then non-matching
        matcherBestFirst = new TokenMatcher();
        matcherBestFirst.RegisterTemplate(matchingTemplate, "matching");
        for (var i = 1; i < TemplateCount; i++)
        {
            matcherBestFirst.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i}");
        }

        // Best-last: non-matching templates first, matching template last
        matcherBestLast = new TokenMatcher();
        for (var i = 1; i < TemplateCount; i++)
        {
            matcherBestLast.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i}");
        }
        matcherBestLast.RegisterTemplate(matchingTemplate, "matching");
    }

    [Benchmark(Description = "Match best-first (matching template registered first)")]
    public TokenMatcherResult<MediumRecord> MatchBestFirst()
        => matcherBestFirst.Match<MediumRecord>(mediumInput);

    [Benchmark(Description = "Match best-last (matching template registered last)")]
    public TokenMatcherResult<MediumRecord> MatchBestLast()
        => matcherBestLast.Match<MediumRecord>(mediumInput);
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release
```

Expected: Build succeeded.

- [ ] **Step 3: Verify dry run**

```bash
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks -- --filter *Matcher* --job dry
```

Expected: All benchmark combinations (2 methods x 3 param values = 6 benchmarks) execute without exceptions.

- [ ] **Step 4: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Benchmarks/MatcherBenchmarks.cs
git commit -m "Add MatcherBenchmarks measuring multi-template matching with scaling"
```

---

### Task 7: ConcurrencyBenchmarks

**Files:**
- Create: `benchmarks/Tokenizer.Benchmarks/Benchmarks/ConcurrencyBenchmarks.cs`

Stress-tests thread safety using `Parallel.For` with configurable parallelism. Compares shared vs per-thread instances for both `Tokenizer` and `TokenMatcher`. Uses medium-tier workloads.

- [ ] **Step 1: Create ConcurrencyBenchmarks.cs**

Create `benchmarks/Tokenizer.Benchmarks/Benchmarks/ConcurrencyBenchmarks.cs`:

```csharp
using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Stress-tests thread safety with shared and per-thread instances.
/// ThreadingDiagnoser reports thread pool usage and lock contention.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ConcurrencyBenchmarks
{
    private const int OperationsPerThread = 50;

    [Params(2, 4, 8)]
    public int ThreadCount { get; set; }

    // Shared instances
    private Tokenizer sharedTokenizer = null!;
    private TokenMatcher sharedMatcher = null!;

    // Pre-compiled template and input
    private Template mediumTemplate = null!;
    private string mediumInput = null!;
    private string mediumTemplateString = null!;

    [GlobalSetup]
    public void Setup()
    {
        sharedTokenizer = Tokenizer.Create();
        var parser = new TokenParser();

        mediumTemplateString = WorkloadGenerator.MediumTemplate();
        mediumTemplate = parser.Parse(mediumTemplateString, "medium");
        mediumInput = WorkloadGenerator.MediumInput();

        sharedMatcher = new TokenMatcher();
        sharedMatcher.RegisterTemplate(mediumTemplateString, "matching");
        for (var i = 1; i <= 10; i++)
        {
            sharedMatcher.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i}");
        }
    }

    [Benchmark(Description = "Parallel tokenize - shared Tokenizer instance")]
    public void ParallelTokenize_SharedInstance()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => sharedTokenizer.Tokenize<MediumRecord>(mediumTemplate, mediumInput));
    }

    [Benchmark(Description = "Parallel tokenize - instance per thread")]
    public void ParallelTokenize_InstancePerThread()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ =>
            {
                var tokenizer = Tokenizer.Create();
                tokenizer.Tokenize<MediumRecord>(mediumTemplate, mediumInput);
            });
    }

    [Benchmark(Description = "Parallel match - shared TokenMatcher instance")]
    public void ParallelMatch_SharedInstance()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => sharedMatcher.Match<MediumRecord>(mediumInput));
    }

    [Benchmark(Description = "Parallel match - instance per thread")]
    public void ParallelMatch_InstancePerThread()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ =>
            {
                var matcher = new TokenMatcher();
                matcher.RegisterTemplate(mediumTemplateString, "matching");
                for (var i = 1; i <= 10; i++)
                {
                    matcher.RegisterTemplate(
                        WorkloadGenerator.NonMatchingTemplate(i),
                        $"non-matching-{i}");
                }
                matcher.Match<MediumRecord>(mediumInput);
            });
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -c Release
```

Expected: Build succeeded.

- [ ] **Step 3: Verify dry run**

```bash
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks -- --filter *Concurrency* --job dry
```

Expected: All benchmark combinations (4 methods x 3 param values = 12 benchmarks) execute without exceptions. No thread-safety crashes.

- [ ] **Step 4: Commit**

```bash
git add benchmarks/Tokenizer.Benchmarks/Benchmarks/ConcurrencyBenchmarks.cs
git commit -m "Add ConcurrencyBenchmarks stress-testing thread safety"
```

---

### Task 8: Full validation and final commit

- [ ] **Step 1: Verify all existing tests still pass**

```bash
dotnet test tests/Tokenizer.Tests/Tokenizer.Tests.csproj
```

Expected: All tests pass.

- [ ] **Step 2: Run full benchmark dry run**

```bash
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks -- --job dry
```

Expected: All benchmarks across all four classes execute without errors. BenchmarkDotNet prints summary tables.

- [ ] **Step 3: Verify solution builds clean**

```bash
dotnet build Tokenizer.sln -c Release
```

Expected: Build succeeded with 0 errors, 0 warnings.

- [ ] **Step 4: Commit any remaining changes**

If there were any fixups during validation:

```bash
git add -A
git status
git commit -m "Final validation fixes for benchmark project"
```
