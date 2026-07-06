using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures template compilation cost: TemplateCompiler.Parse() pipeline
/// (lexer -> parser -> AST -> definition -> front matter binding).
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CompilationBenchmarks
{
    private string smallTemplate = null!;
    private string mediumTemplate = null!;
    private string largeTemplate = null!;
    private TemplateCompiler parser = null!;

    [GlobalSetup]
    public void Setup()
    {
        smallTemplate = WorkloadGenerator.SmallTemplate();
        mediumTemplate = WorkloadGenerator.MediumTemplate();
        largeTemplate = WorkloadGenerator.LargeTemplate();
        parser = new TemplateCompiler(new TokenizerOptions());
    }

    [Benchmark(Description = "Compile small template (3 tokens)")]
    public Template CompileSmall() => parser.Compile(smallTemplate).Template;

    [Benchmark(Description = "Compile medium template (12 tokens)")]
    public Template CompileMedium() => parser.Compile(mediumTemplate).Template;

    [Benchmark(Description = "Compile large template (39 tokens, front matter)")]
    public Template CompileLarge() => parser.Compile(largeTemplate).Template;
}
