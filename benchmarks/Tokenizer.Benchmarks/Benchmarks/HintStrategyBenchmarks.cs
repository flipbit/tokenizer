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
    private string largeInput = null!;
    private string nonMatchingInput = null!;
    private Template largeTemplate = null!;
    private Tokenizer tokenizer = null!;

    [GlobalSetup]
    public void Setup()
    {
        largeInput = WorkloadGenerator.LargeInput();
        nonMatchingInput = "This input does not contain the hint text and should be rejected quickly.";

        tokenizer = new Tokenizer();
        largeTemplate = tokenizer.Compile(WorkloadGenerator.LargeTemplate(), "large");
    }

    [Benchmark(Baseline = true, Description = "Hints present — full tokenization")]
    public TokenizeResult HintsPresent() => tokenizer.Tokenize(largeTemplate, largeInput);

    [Benchmark(Description = "Hints missing — early rejection")]
    public TokenizeResult HintsMissing() => tokenizer.Tokenize(largeTemplate, nonMatchingInput);
}
