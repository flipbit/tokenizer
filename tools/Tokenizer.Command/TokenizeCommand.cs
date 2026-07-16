using System.Text.Json;
using Tokens;

namespace Tokenizer.Command;

/// <summary>
/// Parses CLI arguments, runs the tokenizer, and writes JSON output to stdout.
/// Returns 0 on success, 1 on failure.
/// </summary>
internal static class TokenizeCommand
{
    internal static int Run(string[] args)
    {
        string? template = null;
        string? input = null;
        var diagnostics = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-t":
                case "--template":
                    if (i + 1 < args.Length)
                    {
                        template = args[++i];
                    }

                    break;
                case "-i":
                case "--input":
                    if (i + 1 < args.Length)
                    {
                        input = args[++i];
                    }

                    break;
                case "-d":
                case "--diagnostics":
                    diagnostics = true;
                    break;
                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;
            }
        }

        if (string.IsNullOrEmpty(template))
        {
            Console.Error.WriteLine("Error: --template is required");
            PrintUsage();
            return 1;
        }

        if (string.IsNullOrEmpty(input))
        {
            Console.Error.WriteLine("Error: --input is required");
            PrintUsage();
            return 1;
        }

        var options = new TokenizerOptions
        {
            EnableDiagnostics = diagnostics,
        };

        var tokenizer = new Tokens.Tokenizer(options);
        var compilation = tokenizer.Compile(template);
        var result = tokenizer.Tokenize(compilation.Template, input);

        var matches = result.Matches
            .Select(m => new MatchOutput { Name = m.Token.Name, Value = m.Value?.ToString() })
            .ToArray();

        var output = new TokenizeOutput
        {
            Success = result.Success,
            Matches = matches,
            Diagnostics = diagnostics ? result.Diagnostics?.RenderAlignment() : null,
        };

        Console.WriteLine(JsonSerializer.Serialize(output, TokenizeJsonContext.Default.TokenizeOutput));

        return result.Success ? 0 : 1;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: Tokenizer.Command --template <pattern> --input <text> [--diagnostics]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  -t, --template    Template pattern to match against");
        Console.Error.WriteLine("  -i, --input       Input text to tokenize");
        Console.Error.WriteLine("  -d, --diagnostics Include diagnostic events in output");
        Console.Error.WriteLine("  -h, --help        Show this help");
    }
}
