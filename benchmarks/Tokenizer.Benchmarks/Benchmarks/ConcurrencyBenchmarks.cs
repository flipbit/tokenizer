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
    private Tokenizer _sharedTokenizer = null!;
    private TokenMatcher _sharedMatcher = null!;

    // Pre-compiled template and input
    private Template _mediumTemplate = null!;
    private string _mediumInput = null!;
    private string _mediumTemplateString = null!;

    [GlobalSetup]
    public void Setup()
    {
        _sharedTokenizer = new Tokenizer();
        var parser = new TemplateCompiler(new TokenizerOptions());

        _mediumTemplateString = WorkloadGenerator.MediumTemplate();
        _mediumTemplate = parser.Compile(_mediumTemplateString).Template;
        _mediumInput = WorkloadGenerator.MediumInput();

        _sharedMatcher = new TokenMatcher();
        _sharedMatcher.RegisterTemplate(_mediumTemplateString, "matching");
        for (var i = 1; i <= 10; i++)
        {
            _sharedMatcher.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i}");
        }
    }

    [Benchmark(Description = "Parallel tokenize - shared Tokenizer instance")]
    public void ParallelTokenize_SharedInstance()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _sharedTokenizer.Tokenize<MediumRecord>(_mediumTemplate, _mediumInput));
    }

    [Benchmark(Description = "Parallel tokenize - instance per thread")]
    public void ParallelTokenize_InstancePerThread()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ =>
            {
                var tokenizer = new Tokenizer();
                tokenizer.Tokenize<MediumRecord>(_mediumTemplate, _mediumInput);
            });
    }

    [Benchmark(Description = "Parallel match - shared TokenMatcher instance")]
    public void ParallelMatch_SharedInstance()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _sharedMatcher.Match<MediumRecord>(_mediumInput));
    }

    [Benchmark(Description = "Parallel match - instance per thread")]
    public void ParallelMatch_InstancePerThread()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ =>
            {
                var matcher = new TokenMatcher();
                matcher.RegisterTemplate(_mediumTemplateString, "matching");
                for (var i = 1; i <= 10; i++)
                {
                    matcher.RegisterTemplate(
                        WorkloadGenerator.NonMatchingTemplate(i),
                        $"non-matching-{i}");
                }
                matcher.Match<MediumRecord>(_mediumInput);
            });
    }

    [Benchmark(Description = "Parallel tokenize async - shared Tokenizer instance")]
    public async Task ParallelTokenizeAsync_SharedInstance()
    {
        var tasks = Enumerable.Range(0, ThreadCount * OperationsPerThread)
            .Select(_ =>
            {
                var reader = new StringReader(_mediumInput);
                return _sharedTokenizer.TokenizeAsync<MediumRecord>(_mediumTemplate, reader);
            });
        await Task.WhenAll(tasks);
    }

    [Benchmark(Description = "Parallel match async - shared TokenMatcher instance")]
    public async Task ParallelMatchAsync_SharedInstance()
    {
        var tasks = Enumerable.Range(0, ThreadCount * OperationsPerThread)
            .Select(_ =>
            {
                var reader = new StringReader(_mediumInput);
                return _sharedMatcher.MatchAsync<MediumRecord>(reader);
            });
        await Task.WhenAll(tasks);
    }
}
