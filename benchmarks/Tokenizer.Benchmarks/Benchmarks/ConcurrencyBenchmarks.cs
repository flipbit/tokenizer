using System.IO;
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
        sharedTokenizer = new Tokenizer();
        var parser = new TemplateCompiler();

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
                var tokenizer = new Tokenizer();
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

    [Benchmark(Description = "Parallel tokenize async - shared Tokenizer instance")]
    public async Task ParallelTokenizeAsync_SharedInstance()
    {
        var tasks = Enumerable.Range(0, ThreadCount * OperationsPerThread)
            .Select(_ =>
            {
                var reader = new StringReader(mediumInput);
                return sharedTokenizer.TokenizeAsync<MediumRecord>(mediumTemplate, reader);
            });
        await Task.WhenAll(tasks);
    }

    [Benchmark(Description = "Parallel match async - shared TokenMatcher instance")]
    public async Task ParallelMatchAsync_SharedInstance()
    {
        var tasks = Enumerable.Range(0, ThreadCount * OperationsPerThread)
            .Select(_ =>
            {
                var reader = new StringReader(mediumInput);
                return sharedMatcher.MatchAsync<MediumRecord>(reader);
            });
        await Task.WhenAll(tasks);
    }
}
