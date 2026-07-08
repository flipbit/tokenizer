# Benchmarks

Instructions for AI agents working on benchmarks in this project.

## Framework

BenchmarkDotNet 0.15.8 targeting .NET 10.0. Project: `benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj`

## Running Benchmarks

```bash
# Run all benchmarks (Release mode required)
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj

# Run a specific benchmark class
dotnet run -c Release --project benchmarks/Tokenizer.Benchmarks/Tokenizer.Benchmarks.csproj -- --filter "*CompilationBenchmarks*"
```

## Benchmark Classes

| Class | What it measures |
|-------|-----------------|
| `CompilationBenchmarks` | Template pattern compilation throughput |
| `CompilationCacheBenchmarks` | Cache hit/miss performance |
| `TokenizationBenchmarks` | Core tokenization (string input) |
| `AsyncTokenizationBenchmarks` | Async tokenization (TextReader/Stream) |
| `MatcherBenchmarks` | Multi-template matching |
| `AsyncMatcherBenchmarks` | Async multi-template matching |
| `ConcurrencyBenchmarks` | Thread-safety and parallel throughput |
| `HintStrategyBenchmarks` | Hint pre-filtering strategies |
| `InputStreamBenchmarks` | Stream-based input processing |

## Baselines

Baselines are stored in `benchmarks/baselines/{yyyy-MM-dd}/` as GitHub-flavored Markdown reports.

When creating a new baseline:
1. Run the full benchmark suite
2. Copy the `*-report-github.md` files from `BenchmarkDotNet.Artifacts/results/` to `benchmarks/baselines/{today's date}/`
3. Commit the baseline

Compare against the most recent baseline to detect regressions.

## Configuration

Custom config in `Config/BenchmarkConfig.cs` adds:
- Memory diagnoser (allocations)
- Threading diagnoser
- P95 latency column
- Full JSON exporter
