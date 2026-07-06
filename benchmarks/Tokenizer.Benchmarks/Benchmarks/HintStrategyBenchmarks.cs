using BenchmarkDotNet.Attributes;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures hint processing performance using a template with hints.
/// Uses the large template which has <c>hint: Record Entry</c> in its front matter.
/// Compares matched (tokenization proceeds) vs rejected (hints missing, early exit).
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class HintStrategyBenchmarks
{
    private string _largeInput = null!;
    private string _nonMatchingInput = null!;
    private Template _largeTemplate = null!;
    private Tokenizer _tokenizer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _largeInput = WorkloadGenerator.LargeInput();
        _nonMatchingInput = "This input does not contain the hint text and should be rejected quickly.";

        _tokenizer = new Tokenizer();
        _largeTemplate = _tokenizer.Compile(WorkloadGenerator.LargeTemplate()).Template;
    }

    [Benchmark(Baseline = true, Description = "Hints present — full tokenization")]
    public TokenizeResult HintsPresent() => _tokenizer.Tokenize(_largeTemplate, _largeInput);

    [Benchmark(Description = "Hints missing — early rejection")]
    public TokenizeResult HintsMissing() => _tokenizer.Tokenize(_largeTemplate, _nonMatchingInput);
}
