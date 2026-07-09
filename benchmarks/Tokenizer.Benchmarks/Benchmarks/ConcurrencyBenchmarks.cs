using System.Globalization;
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
    private TemplateMatcher _sharedMatcher = null!;

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

        _sharedMatcher = new TemplateMatcher();
        _sharedMatcher.RegisterTemplate(_mediumTemplateString, "matching");
        for (var i = 1; i <= 10; i++)
        {
            _sharedMatcher.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i.ToString(CultureInfo.InvariantCulture)}");
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

    [Benchmark(Description = "Parallel tokenize - shared TemplateMatcher instance")]
    public void ParallelTokenize_SharedMatcher()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _sharedMatcher.Tokenize<MediumRecord>(_mediumInput));
    }

    [Benchmark(Description = "Parallel tokenize - TemplateMatcher per thread")]
    public void ParallelTokenize_MatcherPerThread()
    {
        Parallel.For(0, ThreadCount * OperationsPerThread,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ =>
            {
                var matcher = new TemplateMatcher();
                matcher.RegisterTemplate(_mediumTemplateString, "matching");
                for (var i = 1; i <= 10; i++)
                {
                    matcher.RegisterTemplate(
                        WorkloadGenerator.NonMatchingTemplate(i),
                        $"non-matching-{i.ToString(CultureInfo.InvariantCulture)}");
                }
                matcher.Tokenize<MediumRecord>(_mediumInput);
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

    [Benchmark(Description = "Parallel tokenize async - shared TemplateMatcher instance")]
    public async Task ParallelTokenizeAsync_SharedMatcher()
    {
        var tasks = Enumerable.Range(0, ThreadCount * OperationsPerThread)
            .Select(async _ =>
            {
                using var reader = new StringReader(_mediumInput);
                return await _sharedMatcher.TokenizeAsync<MediumRecord>(reader);
            });
        await Task.WhenAll(tasks);
    }
}
