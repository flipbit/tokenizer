namespace Tokens.Diagnostics;

/// <summary>
/// Contains diagnostic events recorded during template compilation.
/// Separate from runtime <see cref="DiagnosticResult"/> which covers tokenization.
/// </summary>
public sealed class CompilationDiagnostics
{
    private readonly List<CompilationEvent> _events;

    internal CompilationDiagnostics()
    {
        _events = new List<CompilationEvent>();
    }

    /// <summary>
    /// All events recorded during compilation, in the order they occurred.
    /// </summary>
    public IReadOnlyList<CompilationEvent> Events => _events;

    internal void AddEvent(CompilationEvent evt) => _events.Add(evt);
}
