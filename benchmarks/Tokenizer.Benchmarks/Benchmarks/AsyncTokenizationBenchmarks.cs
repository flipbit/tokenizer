using System.IO;
using BenchmarkDotNet.Attributes;
using Tokens.Compilation;
using Tokens.Config;
using Tokens.Data;

namespace Tokens.Benchmarks;

/// <summary>
/// Measures async tokenization and compilation overhead compared to sync paths.
/// Uses StringReader so I/O is instant — isolates the async machinery cost.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class AsyncTokenizationBenchmarks
{
    private Tokenizer tokenizer = null!;
    private Template mediumTemplate = null!;
    private string mediumInput = null!;
    private string mediumTemplateString = null!;

    [GlobalSetup]
    public void Setup()
    {
        tokenizer = new Tokenizer();
        var parser = new TemplateCompiler();
        mediumTemplateString = WorkloadGenerator.MediumTemplate();
        mediumTemplate = parser.Parse(mediumTemplateString, "medium");
        mediumInput = WorkloadGenerator.MediumInput();
    }

    [Benchmark(Baseline = true, Description = "Tokenize sync (string)")]
    public TokenizeResult<MediumRecord> Tokenize_Sync()
        => tokenizer.Tokenize<MediumRecord>(mediumTemplate, mediumInput);

    [Benchmark(Description = "TokenizeAsync (StringReader)")]
    public async Task<TokenizeResult<MediumRecord>> TokenizeAsync_StringReader()
    {
        using var reader = new StringReader(mediumInput);
        return await tokenizer.TokenizeAsync<MediumRecord>(mediumTemplate, reader);
    }

    [Benchmark(Description = "Compile sync (string)")]
    public Template Compile_Sync()
        => new TemplateCompiler().Parse(mediumTemplateString, "medium");

    [Benchmark(Description = "CompileAsync (StringReader)")]
    public async Task<Template> CompileAsync_StringReader()
    {
        using var reader = new StringReader(mediumTemplateString);
        return await tokenizer.CompileAsync(reader, "medium");
    }
}
