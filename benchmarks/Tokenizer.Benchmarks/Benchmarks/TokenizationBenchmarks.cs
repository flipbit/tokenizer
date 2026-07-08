using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures tokenization cost against pre-compiled templates.
/// Isolates the tokenization engine, hint processing, result building,
/// transformer execution, and validator execution.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class TokenizationBenchmarks
{
    private Tokenizer _tokenizer = null!;
    private Template _smallTemplate = null!;
    private Template _mediumTemplate = null!;
    private Template _largeTemplate = null!;
    private string _smallInput = null!;
    private string _mediumInput = null!;
    private string _largeInput = null!;

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
    }

    [Benchmark(Description = "Tokenize small (3 tokens)")]
    public SmallRecord? TokenizeSmall()
        => _tokenizer.Tokenize<SmallRecord>(_smallTemplate, _smallInput);

    [Benchmark(Description = "Tokenize medium (12 tokens)")]
    public MediumRecord? TokenizeMedium()
        => _tokenizer.Tokenize<MediumRecord>(_mediumTemplate, _mediumInput);

    [Benchmark(Description = "Tokenize large (39 tokens, front matter)")]
    public LargeRecord? TokenizeLarge()
        => _tokenizer.Tokenize<LargeRecord>(_largeTemplate, _largeInput);
}
