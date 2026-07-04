using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures TokenMatcher async matching performance with seekable streams,
/// comparing against sync string matching at different template counts.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class AsyncMatcherBenchmarks
{
    [Params(5, 15, 50)]
    public int TemplateCount { get; set; }

    private TokenMatcher matcher = null!;
    private string mediumInput = null!;
    private byte[] mediumBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        mediumInput = WorkloadGenerator.MediumInput();
        mediumBytes = Encoding.UTF8.GetBytes(mediumInput);

        matcher = new TokenMatcher();
        matcher.RegisterTemplate(WorkloadGenerator.MediumTemplate(), "matching");
        for (var i = 1; i < TemplateCount; i++)
        {
            matcher.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i}");
        }
    }

    [Benchmark(Baseline = true, Description = "Match sync (string)")]
    public TokenMatcherResult<MediumRecord> Match_Sync()
        => matcher.Match<MediumRecord>(mediumInput);

    [Benchmark(Description = "MatchAsync (TextReader)")]
    public async Task<TokenMatcherResult<MediumRecord>> MatchAsync_TextReader()
    {
        using var reader = new StringReader(mediumInput);
        return await matcher.MatchAsync<MediumRecord>(reader);
    }

    [Benchmark(Description = "MatchAsync (seekable Stream)")]
    public async Task<TokenMatcherResult<MediumRecord>> MatchAsync_SeekableStream()
    {
        using var stream = new MemoryStream(mediumBytes);
        return await matcher.MatchAsync<MediumRecord>(stream, Encoding.UTF8);
    }
}
