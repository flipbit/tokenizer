namespace Tokens.Diagnostics;

/// <summary>
/// Contains diagnostic events recorded during template compilation.
/// Separate from runtime <see cref="DiagnosticResult"/> which covers tokenization.
/// </summary>
public sealed class CompilationDiagnostics
{
    private readonly List<DiagnosticEvent> _events;

    internal CompilationDiagnostics()
    {
        _events = new List<DiagnosticEvent>();
    }

    /// <summary>
    /// All events recorded during compilation, in the order they occurred.
    /// </summary>
    public IReadOnlyList<DiagnosticEvent> Events => _events;

    internal void AddEvent(DiagnosticEvent evt) => _events.Add(evt);
}
