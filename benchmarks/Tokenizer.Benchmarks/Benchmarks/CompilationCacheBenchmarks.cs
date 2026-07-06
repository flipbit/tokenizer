using BenchmarkDotNet.Attributes;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures tokenization throughput with pre-compiled templates across
/// small, medium, and large workloads, including concurrent access.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CompilationCacheBenchmarks
{
    private Tokenizer tokenizer = null!;
    private Template precompiledSmall = null!;
    private Template precompiledMedium = null!;
    private Template precompiledLarge = null!;
    private string smallInput = null!;
    private string mediumInput = null!;
    private string largeInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        tokenizer = new Tokenizer();

        precompiledSmall = tokenizer.Compile(WorkloadGenerator.SmallTemplate()).Template;
        precompiledMedium = tokenizer.Compile(WorkloadGenerator.MediumTemplate()).Template;
        precompiledLarge = tokenizer.Compile(WorkloadGenerator.LargeTemplate()).Template;

        smallInput = WorkloadGenerator.SmallInput();
        mediumInput = WorkloadGenerator.MediumInput();
        largeInput = WorkloadGenerator.LargeInput();
    }

    [Benchmark(Description = "Pre-compiled: small (3 tokens)", Baseline = true)]
    public TokenizeResult<SmallRecord> PreCompiled_Small()
        => tokenizer.Tokenize<SmallRecord>(precompiledSmall, smallInput);

    [Benchmark(Description = "Pre-compiled: medium (12 tokens)")]
    public TokenizeResult<MediumRecord> PreCompiled_Medium()
        => tokenizer.Tokenize<MediumRecord>(precompiledMedium, mediumInput);

    [Benchmark(Description = "Pre-compiled: large (39 tokens)")]
    public TokenizeResult<LargeRecord> PreCompiled_Large()
        => tokenizer.Tokenize<LargeRecord>(precompiledLarge, largeInput);

    [Benchmark(Description = "Concurrent tokenize: 8 threads, large")]
    public void ConcurrentTokenize()
    {
        Parallel.For(0, 8, _ =>
        {
            tokenizer.Tokenize<LargeRecord>(precompiledLarge, largeInput);
        });
    }
}
