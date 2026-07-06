using Tokens.Diagnostics;

namespace Tokens;

/// <summary>
/// Holds the result of compiling a template pattern string,
/// including the compiled template and optional diagnostics.
/// </summary>
public sealed class CompilationResult
{
    /// <summary>
    /// The compiled template.
    /// </summary>
    public Template Template { get; }

    /// <summary>
    /// Structured diagnostic output from the compilation process.
    /// Null when <see cref="TokenizerOptions.EnableDiagnostics"/> is false.
    /// </summary>
    public DiagnosticResult? Diagnostics { get; }

    internal CompilationResult(Template template, DiagnosticResult? diagnostics)
    {
        Template = template;
        Diagnostics = diagnostics;
    }
}
