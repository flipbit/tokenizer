using System.Globalization;
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

    private TokenMatcher _matcherBestFirst = null!;
    private TokenMatcher _matcherBestLast = null!;
    private string _mediumInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        _mediumInput = WorkloadGenerator.MediumInput();

        var matchingTemplate = WorkloadGenerator.MediumTemplate();

        // Best-first: matching template registered first, then non-matching
        _matcherBestFirst = new TokenMatcher();
        _matcherBestFirst.RegisterTemplate(matchingTemplate, "matching");
        for (var i = 1; i < TemplateCount; i++)
        {
            _matcherBestFirst.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i.ToString(CultureInfo.InvariantCulture)}");
        }

        // Best-last: non-matching templates first, matching template last
        _matcherBestLast = new TokenMatcher();
        for (var i = 1; i < TemplateCount; i++)
        {
            _matcherBestLast.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i.ToString(CultureInfo.InvariantCulture)}");
        }
        _matcherBestLast.RegisterTemplate(matchingTemplate, "matching");
    }

    [Benchmark(Description = "Match best-first (matching template registered first)")]
    public TokenMatcherResult<MediumRecord> MatchBestFirst()
        => _matcherBestFirst.Match<MediumRecord>(_mediumInput);

    [Benchmark(Description = "Match best-last (matching template registered last)")]
    public TokenMatcherResult<MediumRecord> MatchBestLast()
        => _matcherBestLast.Match<MediumRecord>(_mediumInput);
}
