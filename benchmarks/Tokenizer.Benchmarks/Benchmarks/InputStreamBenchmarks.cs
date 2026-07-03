using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Compares tokenization performance across input source types:
/// string, TextReader, and Stream for small, medium, and large workloads.
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
        var parser = new TokenParser();
        smallTemplate = parser.Parse(WorkloadGenerator.SmallTemplate(), "small");
        mediumTemplate = parser.Parse(WorkloadGenerator.MediumTemplate(), "medium");
        largeTemplate = parser.Parse(WorkloadGenerator.LargeTemplate(), "large");
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

    [Benchmark(Description = "TextReader small")]
    public TokenizeResult TextReader_Small()
    {
        using var reader = new StringReader(smallInput);
        return tokenizer.Tokenize(smallTemplate, reader);
    }

    [Benchmark(Description = "TextReader medium")]
    public TokenizeResult TextReader_Medium()
    {
        using var reader = new StringReader(mediumInput);
        return tokenizer.Tokenize(mediumTemplate, reader);
    }

    [Benchmark(Description = "TextReader large")]
    public TokenizeResult TextReader_Large()
    {
        using var reader = new StringReader(largeInput);
        return tokenizer.Tokenize(largeTemplate, reader);
    }

    [Benchmark(Description = "Stream small")]
    public TokenizeResult Stream_Small()
    {
        using var stream = new MemoryStream(smallBytes);
        return tokenizer.Tokenize(smallTemplate, stream, Encoding.UTF8);
    }

    [Benchmark(Description = "Stream medium")]
    public TokenizeResult Stream_Medium()
    {
        using var stream = new MemoryStream(mediumBytes);
        return tokenizer.Tokenize(mediumTemplate, stream, Encoding.UTF8);
    }

    [Benchmark(Description = "Stream large")]
    public TokenizeResult Stream_Large()
    {
        using var stream = new MemoryStream(largeBytes);
        return tokenizer.Tokenize(largeTemplate, stream, Encoding.UTF8);
    }
}
