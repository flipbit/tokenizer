using System.Text;
using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Compares tokenization performance across input source types:
/// string (sync), TextReader (async), and Stream (async) for small, medium, and large workloads.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class InputStreamBenchmarks
{
    private Tokenizer _tokenizer = null!;
    private Template _smallTemplate = null!;
    private Template _mediumTemplate = null!;
    private Template _largeTemplate = null!;
    private string _smallInput = null!;
    private string _mediumInput = null!;
    private string _largeInput = null!;
    private byte[] _smallBytes = null!;
    private byte[] _mediumBytes = null!;
    private byte[] _largeBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tokenizer = new Tokenizer();
        var parser = new TemplateCompiler(new TokenizerOptions());
        _smallTemplate = parser.Compile(WorkloadGenerator.SmallTemplate()).Template;
        _mediumTemplate = parser.Compile(WorkloadGenerator.MediumTemplate()).Template;
        _largeTemplate = parser.Compile(WorkloadGenerator.LargeTemplate()).Template;
        _smallInput = WorkloadGenerator.SmallInput();
        _mediumInput = WorkloadGenerator.MediumInput();
        _largeInput = WorkloadGenerator.LargeInput();
        _smallBytes = Encoding.UTF8.GetBytes(_smallInput);
        _mediumBytes = Encoding.UTF8.GetBytes(_mediumInput);
        _largeBytes = Encoding.UTF8.GetBytes(_largeInput);
    }

    [Benchmark(Baseline = true, Description = "String small")]
    public TokenizeResult String_Small() => _tokenizer.Tokenize(_smallTemplate, _smallInput);

    [Benchmark(Description = "String medium")]
    public TokenizeResult String_Medium() => _tokenizer.Tokenize(_mediumTemplate, _mediumInput);

    [Benchmark(Description = "String large")]
    public TokenizeResult String_Large() => _tokenizer.Tokenize(_largeTemplate, _largeInput);

    [Benchmark(Description = "TextReader async small")]
    public async Task<TokenizeResult> TextReaderAsync_Small()
    {
        using var reader = new StringReader(_smallInput);
        return await _tokenizer.TokenizeAsync(_smallTemplate, reader);
    }

    [Benchmark(Description = "TextReader async medium")]
    public async Task<TokenizeResult> TextReaderAsync_Medium()
    {
        using var reader = new StringReader(_mediumInput);
        return await _tokenizer.TokenizeAsync(_mediumTemplate, reader);
    }

    [Benchmark(Description = "TextReader async large")]
    public async Task<TokenizeResult> TextReaderAsync_Large()
    {
        using var reader = new StringReader(_largeInput);
        return await _tokenizer.TokenizeAsync(_largeTemplate, reader);
    }

    [Benchmark(Description = "Stream async small")]
    public async Task<TokenizeResult> StreamAsync_Small()
    {
        using var stream = new MemoryStream(_smallBytes);
        return await _tokenizer.TokenizeAsync(_smallTemplate, stream, Encoding.UTF8);
    }

    [Benchmark(Description = "Stream async medium")]
    public async Task<TokenizeResult> StreamAsync_Medium()
    {
        using var stream = new MemoryStream(_mediumBytes);
        return await _tokenizer.TokenizeAsync(_mediumTemplate, stream, Encoding.UTF8);
    }

    [Benchmark(Description = "Stream async large")]
    public async Task<TokenizeResult> StreamAsync_Large()
    {
        using var stream = new MemoryStream(_largeBytes);
        return await _tokenizer.TokenizeAsync(_largeTemplate, stream, Encoding.UTF8);
    }
}
