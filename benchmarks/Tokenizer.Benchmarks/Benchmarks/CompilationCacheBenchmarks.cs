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
    private Tokenizer _tokenizer = null!;
    private Template _precompiledSmall = null!;
    private Template _precompiledMedium = null!;
    private Template _precompiledLarge = null!;
    private string _smallInput = null!;
    private string _mediumInput = null!;
    private string _largeInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tokenizer = new Tokenizer();

        _precompiledSmall = _tokenizer.Compile(WorkloadGenerator.SmallTemplate()).Template;
        _precompiledMedium = _tokenizer.Compile(WorkloadGenerator.MediumTemplate()).Template;
        _precompiledLarge = _tokenizer.Compile(WorkloadGenerator.LargeTemplate()).Template;

        _smallInput = WorkloadGenerator.SmallInput();
        _mediumInput = WorkloadGenerator.MediumInput();
        _largeInput = WorkloadGenerator.LargeInput();
    }

    [Benchmark(Description = "Pre-compiled: small (3 tokens)", Baseline = true)]
    public TokenizeResult<SmallRecord> PreCompiled_Small()
        => _tokenizer.Tokenize<SmallRecord>(_precompiledSmall, _smallInput);

    [Benchmark(Description = "Pre-compiled: medium (12 tokens)")]
    public TokenizeResult<MediumRecord> PreCompiled_Medium()
        => _tokenizer.Tokenize<MediumRecord>(_precompiledMedium, _mediumInput);

    [Benchmark(Description = "Pre-compiled: large (39 tokens)")]
    public TokenizeResult<LargeRecord> PreCompiled_Large()
        => _tokenizer.Tokenize<LargeRecord>(_precompiledLarge, _largeInput);

    [Benchmark(Description = "Concurrent tokenize: 8 threads, large")]
    public void ConcurrentTokenize()
    {
        Parallel.For(0, 8, _ =>
        {
            _tokenizer.Tokenize<LargeRecord>(_precompiledLarge, _largeInput);
        });
    }
}
