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
    private Tokenizer _tokenizer = null!;
    private Template _mediumTemplate = null!;
    private string _mediumInput = null!;
    private string _mediumTemplateString = null!;

    [GlobalSetup]
    public void Setup()
    {
        _tokenizer = new Tokenizer();
        var parser = new TemplateCompiler(new TokenizerOptions());
        _mediumTemplateString = WorkloadGenerator.MediumTemplate();
        _mediumTemplate = parser.Compile(_mediumTemplateString).Template;
        _mediumInput = WorkloadGenerator.MediumInput();
    }

    [Benchmark(Baseline = true, Description = "Tokenize sync (string)")]
    public TokenizeResult<MediumRecord> Tokenize_Sync()
        => _tokenizer.Tokenize<MediumRecord>(_mediumTemplate, _mediumInput);

    [Benchmark(Description = "TokenizeAsync (StringReader)")]
    public async Task<TokenizeResult<MediumRecord>> TokenizeAsync_StringReader()
    {
        using var reader = new StringReader(_mediumInput);
        return await _tokenizer.TokenizeAsync<MediumRecord>(_mediumTemplate, reader);
    }

    [Benchmark(Description = "Compile sync (string)")]
    public Template Compile_Sync()
        => new TemplateCompiler(new TokenizerOptions()).Compile(_mediumTemplateString).Template;

    [Benchmark(Description = "CompileAsync (StringReader)")]
    public async Task<Template> CompileAsync_StringReader()
    {
        using var reader = new StringReader(_mediumTemplateString);
        return (await _tokenizer.CompileAsync(reader)).Template;
    }
}
