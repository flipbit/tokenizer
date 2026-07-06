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
    private Tokenizer tokenizer = null!;
    private Template smallTemplate = null!;
    private Template mediumTemplate = null!;
    private Template largeTemplate = null!;
    private string smallInput = null!;
    private string mediumInput = null!;
    private string largeInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        tokenizer = new Tokenizer();
        var parser = new TemplateCompiler(new TokenizerOptions());

        smallTemplate = parser.Parse(WorkloadGenerator.SmallTemplate(), "small");
        mediumTemplate = parser.Parse(WorkloadGenerator.MediumTemplate(), "medium");
        largeTemplate = parser.Parse(WorkloadGenerator.LargeTemplate(), "large");

        smallInput = WorkloadGenerator.SmallInput();
        mediumInput = WorkloadGenerator.MediumInput();
        largeInput = WorkloadGenerator.LargeInput();
    }

    [Benchmark(Description = "Tokenize small (3 tokens)")]
    public TokenizeResult<SmallRecord> TokenizeSmall()
        => tokenizer.Tokenize<SmallRecord>(smallTemplate, smallInput);

    [Benchmark(Description = "Tokenize medium (12 tokens)")]
    public TokenizeResult<MediumRecord> TokenizeMedium()
        => tokenizer.Tokenize<MediumRecord>(mediumTemplate, mediumInput);

    [Benchmark(Description = "Tokenize large (39 tokens, front matter)")]
    public TokenizeResult<LargeRecord> TokenizeLarge()
        => tokenizer.Tokenize<LargeRecord>(largeTemplate, largeInput);
}
