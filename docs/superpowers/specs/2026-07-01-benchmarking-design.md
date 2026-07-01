# Benchmarking Project Design

## Purpose

Establish a performance baseline for the Tokenizer library to guard against regressions and guide future performance work. Measures compilation, tokenization, multi-template matching, and concurrent usage across varied workload sizes.

## Project Setup

- **Location:** `benchmarks/Tokenizer.Benchmarks/`
- **Type:** Console application (BenchmarkDotNet)
- **Target framework:** `net10.0`
- **RootNamespace:** `Tokens`
- **Dependencies:** BenchmarkDotNet (latest stable), project reference to `src/Tokenizer/Tokenizer.csproj`
- **Added to:** `Tokenizer.sln`
- **CI integration:** None — local developer tool only, run manually

### Directory Structure

```
benchmarks/
  Tokenizer.Benchmarks/
    Tokenizer.Benchmarks.csproj
    Program.cs
    Config/
      BenchmarkConfig.cs
    Data/
      WorkloadGenerator.cs
      SmallRecord.cs
      MediumRecord.cs
      LargeRecord.cs
    Benchmarks/
      CompilationBenchmarks.cs
      TokenizationBenchmarks.cs
      MatcherBenchmarks.cs
      ConcurrencyBenchmarks.cs
```

## Configuration

### BenchmarkConfig

Shared configuration applied to all benchmark classes via `[Config(typeof(BenchmarkConfig))]`:

- `MemoryDiagnoser.Default` — GC allocations and bytes allocated per operation
- `ThreadingDiagnoser.Default` — thread pool usage and lock contention metrics
- `StatisticColumn.P95` — 95th percentile latency column
- `MarkdownExporter.GitHub` — GitHub-flavored markdown results export

### Entry Point

`BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)` — allows running all benchmarks or filtering by class/method name.

### Running

```bash
# All benchmarks
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks

# Specific class
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks -- --filter *Tokenization*

# Quick dry run to verify setup
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks -- --job dry
```

## WorkloadGenerator

Static class producing synthetic templates and matching input text at three tiers. All templates are valid, compilable patterns that tokenize successfully against their companion input.

### Tiers

**Small (3-5 tokens, ~5 lines input):**
Simple extraction with minimal transformers/validators. Establishes a floor for per-operation cost.

**Medium (10-15 tokens, ~20 lines input):**
Mixed transformers and validators. Represents a typical real-world template.

**Large (30-50 tokens, ~60 lines input):**
Exercises all transformers and validators. Includes repeating tokens and front matter with hints and tags. Stress-tests the full pipeline.

### Transformer Coverage

All templates collectively exercise every registered transformer:
- `Trim`
- `ToUpper`, `ToLower`
- `ToDateTime`, `ToDateTimeUtc`
- `SubstringBefore`, `SubstringAfter`
- `SubstringBeforeLast`, `SubstringAfterLast`
- `Remove`, `RemoveStart`, `RemoveEnd`
- `Replace`
- `Split`
- `Set`

### Validator Coverage

All templates collectively exercise every registered validator:
- `IsNotEmpty`
- `IsNumeric`
- `IsEmail`
- `IsUrl`, `IsLooseUrl`, `IsLooseAbsoluteUrl`
- `IsDomainName`
- `IsPhoneNumber`
- `IsDateTime`
- `Contains`, `StartsWith`, `EndsWith`
- `MinLength`, `MaxLength`
- `IsNot`

### Target Types

Simple POCOs (`SmallRecord`, `MediumRecord`, `LargeRecord`) with properties matching the token names in each tier's template. Defined as separate files in the `Data/` directory.

### Input Generation

Each tier has a companion method returning input text with values that pass all validators and produce meaningful transformer output.

## Benchmark Classes

### CompilationBenchmarks

Measures template parsing/compilation cost — the `TokenParser.Parse()` pipeline (lexer → parser → AST → definition → front matter binding).

- **Methods:** `CompileSmall()`, `CompileMedium()`, `CompileLarge()`
- **Setup:** `[GlobalSetup]` stores raw template strings from `WorkloadGenerator`
- **Reveals:** Whether compilation cost scales linearly with token count; front matter parsing overhead on large templates

### TokenizationBenchmarks

Measures tokenization of input against a pre-compiled template — the `Tokenizer.Tokenize<T>()` path. Isolates runtime cost from compilation cost.

- **Methods:** `TokenizeSmall()`, `TokenizeMedium()`, `TokenizeLarge()`
- **Setup:** `[GlobalSetup]` compiles templates, stores them with matching input strings, creates a `Tokenizer` instance
- **Reveals:** Cost of the tokenization engine, hint processing, result building, transformer execution, and validator execution per operation

### MatcherBenchmarks

Measures `TokenMatcher.Match<T>()` with varying numbers of registered templates.

- **Methods:** `MatchBestFirst()`, `MatchBestLast()`
  - Best-matching template registered first vs last, to measure short-circuit vs full-scan behavior
- **Params:** `[Params(5, 15, 50)]` template count
  - Medium-tier templates registered; one matching template, the rest non-matching (different hint text or structure so they fail early)
- **Setup:** `[GlobalSetup]` registers N templates into a `TokenMatcher`, positions the matching template based on the method
- **Reveals:** How match cost scales with template count; whether hint-based filtering effectively prunes non-matching templates

### ConcurrencyBenchmarks

Stress-tests thread safety using `Parallel.ForEach` with configurable degree of parallelism.

- **Methods:**
  - `ParallelTokenize_SharedInstance()` — single `Tokenizer` shared across threads
  - `ParallelTokenize_InstancePerThread()` — separate `Tokenizer` per thread
  - `ParallelMatch_SharedInstance()` — single `TokenMatcher` shared across threads
  - `ParallelMatch_InstancePerThread()` — separate `TokenMatcher` per thread
- **Params:** `[Params(2, 4, 8)]` thread count
- **Setup:** `[GlobalSetup]` creates shared instances and pre-compiles templates. Uses medium-tier workloads.
- **Reveals:** Whether shared instances are safe under contention; lock contention via `ThreadingDiagnoser`; scaling characteristics with thread count
