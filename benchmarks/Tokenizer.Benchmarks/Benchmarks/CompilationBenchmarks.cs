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
    private string _smallTemplate = null!;
    private string _mediumTemplate = null!;
    private string _largeTemplate = null!;
    private TemplateCompiler _parser = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallTemplate = WorkloadGenerator.SmallTemplate();
        _mediumTemplate = WorkloadGenerator.MediumTemplate();
        _largeTemplate = WorkloadGenerator.LargeTemplate();
        _parser = new TemplateCompiler(new TokenizerOptions());
    }

    [Benchmark(Description = "Compile small template (3 tokens)")]
    public Template CompileSmall() => _parser.Compile(_smallTemplate).Template;

    [Benchmark(Description = "Compile medium template (12 tokens)")]
    public Template CompileMedium() => _parser.Compile(_mediumTemplate).Template;

    [Benchmark(Description = "Compile large template (39 tokens, front matter)")]
    public Template CompileLarge() => _parser.Compile(_largeTemplate).Template;
}
