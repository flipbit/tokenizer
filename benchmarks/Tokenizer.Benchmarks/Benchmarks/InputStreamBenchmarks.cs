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
    private Tokenizer tokenizer = null!;
    private Template smallTemplate = null!;
    private Template mediumTemplate = null!;
    private Template largeTemplate = null!;
    private string smallInput = null!;
    private string mediumInput = null!;
    private string largeInput = null!;
    private byte[] smallBytes = null!;
    private byte[] mediumBytes = null!;
    private byte[] largeBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        tokenizer = new Tokenizer();
        var parser = new TemplateCompiler(new TokenizerOptions());
        smallTemplate = parser.Compile(WorkloadGenerator.SmallTemplate()).Template;
        mediumTemplate = parser.Compile(WorkloadGenerator.MediumTemplate()).Template;
        largeTemplate = parser.Compile(WorkloadGenerator.LargeTemplate()).Template;
        smallInput = WorkloadGenerator.SmallInput();
        mediumInput = WorkloadGenerator.MediumInput();
        largeInput = WorkloadGenerator.LargeInput();
        smallBytes = Encoding.UTF8.GetBytes(smallInput);
        mediumBytes = Encoding.UTF8.GetBytes(mediumInput);
        largeBytes = Encoding.UTF8.GetBytes(largeInput);
    }

    [Benchmark(Baseline = true, Description = "String small")]
    public TokenizeResult String_Small() => tokenizer.Tokenize(smallTemplate, smallInput);

    [Benchmark(Description = "String medium")]
    public TokenizeResult String_Medium() => tokenizer.Tokenize(mediumTemplate, mediumInput);

    [Benchmark(Description = "String large")]
    public TokenizeResult String_Large() => tokenizer.Tokenize(largeTemplate, largeInput);

    [Benchmark(Description = "TextReader async small")]
    public async Task<TokenizeResult> TextReaderAsync_Small()
    {
        using var reader = new StringReader(smallInput);
        return await tokenizer.TokenizeAsync(smallTemplate, reader);
    }

    [Benchmark(Description = "TextReader async medium")]
    public async Task<TokenizeResult> TextReaderAsync_Medium()
    {
        using var reader = new StringReader(mediumInput);
        return await tokenizer.TokenizeAsync(mediumTemplate, reader);
    }

    [Benchmark(Description = "TextReader async large")]
    public async Task<TokenizeResult> TextReaderAsync_Large()
    {
        using var reader = new StringReader(largeInput);
        return await tokenizer.TokenizeAsync(largeTemplate, reader);
    }

    [Benchmark(Description = "Stream async small")]
    public async Task<TokenizeResult> StreamAsync_Small()
    {
        using var stream = new MemoryStream(smallBytes);
        return await tokenizer.TokenizeAsync(smallTemplate, stream, Encoding.UTF8);
    }

    [Benchmark(Description = "Stream async medium")]
    public async Task<TokenizeResult> StreamAsync_Medium()
    {
        using var stream = new MemoryStream(mediumBytes);
        return await tokenizer.TokenizeAsync(mediumTemplate, stream, Encoding.UTF8);
    }

    [Benchmark(Description = "Stream async large")]
    public async Task<TokenizeResult> StreamAsync_Large()
    {
        using var stream = new MemoryStream(largeBytes);
        return await tokenizer.TokenizeAsync(largeTemplate, stream, Encoding.UTF8);
    }
}
