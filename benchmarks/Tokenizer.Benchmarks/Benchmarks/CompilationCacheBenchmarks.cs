using BenchmarkDotNet.Attributes;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures the impact of the compilation cache on tokenization throughput.
/// Compares cache hits, pre-compiled templates (baseline), no-cache, and concurrent access.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CompilationCacheBenchmarks
{
    private Tokenizer cachedTokenizer = null!;
    private Tokenizer uncachedTokenizer = null!;
    private Tokenizer precompiledTokenizer = null!;
    private Template precompiledSmall = null!;
    private Template precompiledMedium = null!;
    private Template precompiledLarge = null!;
    private string smallTemplate = null!;
    private string mediumTemplate = null!;
    private string largeTemplate = null!;
    private string smallInput = null!;
    private string mediumInput = null!;
    private string largeInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        cachedTokenizer = new Tokenizer();
        uncachedTokenizer = new Tokenizer(new TokenizerOptions { CompilationCacheMaxSize = 0 });
        precompiledTokenizer = new Tokenizer();

        smallTemplate = WorkloadGenerator.SmallTemplate();
        mediumTemplate = WorkloadGenerator.MediumTemplate();
        largeTemplate = WorkloadGenerator.LargeTemplate();

        smallInput = WorkloadGenerator.SmallInput();
        mediumInput = WorkloadGenerator.MediumInput();
        largeInput = WorkloadGenerator.LargeInput();

        precompiledSmall = precompiledTokenizer.Compile(smallTemplate, "small");
        precompiledMedium = precompiledTokenizer.Compile(mediumTemplate, "medium");
        precompiledLarge = precompiledTokenizer.Compile(largeTemplate, "large");

        cachedTokenizer.Compile(smallTemplate);
        cachedTokenizer.Compile(mediumTemplate);
        cachedTokenizer.Compile(largeTemplate);
    }

    [Benchmark(Description = "Cache hit: small (3 tokens)")]
    public TokenizeResult<SmallRecord> CacheHit_Small()
        => cachedTokenizer.Tokenize<SmallRecord>(smallTemplate, smallInput);

    [Benchmark(Description = "Cache hit: medium (12 tokens)")]
    public TokenizeResult<MediumRecord> CacheHit_Medium()
        => cachedTokenizer.Tokenize<MediumRecord>(mediumTemplate, mediumInput);

    [Benchmark(Description = "Cache hit: large (39 tokens)")]
    public TokenizeResult<LargeRecord> CacheHit_Large()
        => cachedTokenizer.Tokenize<LargeRecord>(largeTemplate, largeInput);

    [Benchmark(Description = "Pre-compiled: small (3 tokens)", Baseline = true)]
    public TokenizeResult<SmallRecord> PreCompiled_Small()
        => precompiledTokenizer.Tokenize<SmallRecord>(precompiledSmall, smallInput);

    [Benchmark(Description = "Pre-compiled: medium (12 tokens)")]
    public TokenizeResult<MediumRecord> PreCompiled_Medium()
        => precompiledTokenizer.Tokenize<MediumRecord>(precompiledMedium, mediumInput);

    [Benchmark(Description = "Pre-compiled: large (39 tokens)")]
    public TokenizeResult<LargeRecord> PreCompiled_Large()
        => precompiledTokenizer.Tokenize<LargeRecord>(precompiledLarge, largeInput);

    [Benchmark(Description = "No cache: small (3 tokens)")]
    public TokenizeResult<SmallRecord> NoCache_Small()
        => uncachedTokenizer.Tokenize<SmallRecord>(smallTemplate, smallInput);

    [Benchmark(Description = "No cache: medium (12 tokens)")]
    public TokenizeResult<MediumRecord> NoCache_Medium()
        => uncachedTokenizer.Tokenize<MediumRecord>(mediumTemplate, mediumInput);

    [Benchmark(Description = "No cache: large (39 tokens)")]
    public TokenizeResult<LargeRecord> NoCache_Large()
        => uncachedTokenizer.Tokenize<LargeRecord>(largeTemplate, largeInput);

    [Benchmark(Description = "Concurrent cache hit: 8 threads, large")]
    public void ConcurrentCacheHit()
    {
        Parallel.For(0, 8, _ =>
        {
            cachedTokenizer.Tokenize<LargeRecord>(largeTemplate, largeInput);
        });
    }
}
