using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures template compilation cost: TokenParser.Parse() pipeline
/// (lexer -> parser -> AST -> definition -> front matter binding).
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CompilationBenchmarks
{
    private string smallTemplate = null!;
    private string mediumTemplate = null!;
    private string largeTemplate = null!;
    private TokenParser parser = null!;

    [GlobalSetup]
    public void Setup()
    {
        smallTemplate = WorkloadGenerator.SmallTemplate();
        mediumTemplate = WorkloadGenerator.MediumTemplate();
        largeTemplate = WorkloadGenerator.LargeTemplate();
        parser = new TokenParser();
    }

    [Benchmark(Description = "Compile small template (3 tokens)")]
    public Template CompileSmall() => parser.Parse(smallTemplate, "small");

    [Benchmark(Description = "Compile medium template (12 tokens)")]
    public Template CompileMedium() => parser.Parse(mediumTemplate, "medium");

    [Benchmark(Description = "Compile large template (39 tokens, front matter)")]
    public Template CompileLarge() => parser.Parse(largeTemplate, "large");
}
