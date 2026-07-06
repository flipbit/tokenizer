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

    private TokenMatcher _matcher = null!;
    private string _mediumInput = null!;
    private byte[] _mediumBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _mediumInput = WorkloadGenerator.MediumInput();
        _mediumBytes = Encoding.UTF8.GetBytes(_mediumInput);

        _matcher = new TokenMatcher();
        _matcher.RegisterTemplate(WorkloadGenerator.MediumTemplate(), "matching");
        for (var i = 1; i < TemplateCount; i++)
        {
            _matcher.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i}");
        }
    }

    [Benchmark(Baseline = true, Description = "Match sync (string)")]
    public TokenMatcherResult<MediumRecord> Match_Sync()
        => _matcher.Match<MediumRecord>(_mediumInput);

    [Benchmark(Description = "MatchAsync (TextReader)")]
    public async Task<TokenMatcherResult<MediumRecord>> MatchAsync_TextReader()
    {
        using var reader = new StringReader(_mediumInput);
        return await _matcher.MatchAsync<MediumRecord>(reader);
    }

    [Benchmark(Description = "MatchAsync (seekable Stream)")]
    public async Task<TokenMatcherResult<MediumRecord>> MatchAsync_SeekableStream()
    {
        using var stream = new MemoryStream(_mediumBytes);
        return await _matcher.MatchAsync<MediumRecord>(stream, Encoding.UTF8);
    }
}
