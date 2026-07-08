using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures TemplateMatcher async tokenization performance with seekable streams,
/// comparing against sync string matching at different template counts.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class AsyncMatcherBenchmarks
{
    [Params(5, 15, 50)]
    public int TemplateCount { get; set; }

    private TemplateMatcher _matcher = null!;
    private string _mediumInput = null!;
    private byte[] _mediumBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _mediumInput = WorkloadGenerator.MediumInput();
        _mediumBytes = Encoding.UTF8.GetBytes(_mediumInput);

        _matcher = new TemplateMatcher();
        _matcher.RegisterTemplate(WorkloadGenerator.MediumTemplate(), "matching");
        for (var i = 1; i < TemplateCount; i++)
        {
            _matcher.RegisterTemplate(
                WorkloadGenerator.NonMatchingTemplate(i),
                $"non-matching-{i.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    [Benchmark(Baseline = true, Description = "Tokenize sync (string)")]
    public MediumRecord? Tokenize_Sync()
        => _matcher.Tokenize<MediumRecord>(_mediumInput);

    [Benchmark(Description = "TokenizeAsync (TextReader)")]
    public async Task<MediumRecord?> TokenizeAsync_TextReader()
    {
        using var reader = new StringReader(_mediumInput);
        return await _matcher.TokenizeAsync<MediumRecord>(reader);
    }

    [Benchmark(Description = "TokenizeAsync (seekable Stream)")]
    public async Task<MediumRecord?> TokenizeAsync_SeekableStream()
    {
        using var stream = new MemoryStream(_mediumBytes);
        return await _matcher.TokenizeAsync<MediumRecord>(stream, Encoding.UTF8);
    }
}
