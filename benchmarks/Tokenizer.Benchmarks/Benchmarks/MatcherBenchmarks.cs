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
