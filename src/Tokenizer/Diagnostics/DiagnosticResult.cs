namespace Tokens.Diagnostics;

/// <summary>
/// Contains all diagnostic information from a single tokenization call.
/// The primary API is <see cref="Tokens"/> which provides per-token narratives.
/// <see cref="RawEvents"/> retains the full event trace for power users.
/// </summary>
public sealed class DiagnosticResult
{
    private readonly List<DiagnosticEvent> _events;
    private readonly string? _inputContent;
    private IReadOnlyList<TokenDiagnostic>? _tokens;
    private string? _verdict;
    private string? _alignment;
    private string? _processingOrder;

    internal DiagnosticResult(string? inputContent)
    {
        _inputContent = inputContent;
        _events = new List<DiagnosticEvent>();
    }

    /// <summary>
    /// The input text that was tokenized. Used by hint generators for near-miss analysis.
    /// </summary>
    internal string? InputContent => _inputContent;

    /// <summary>
    /// Per-token diagnostic narratives — the primary diagnostic API.
    /// Each entry tells the complete story of one token: every consideration,
    /// every rejection, and the final outcome.
    /// </summary>
    public IReadOnlyList<TokenDiagnostic> Tokens
    {
        get
        {
            EnsureBuilt();
            return _tokens!;
        }
    }

    /// <summary>
    /// A human-readable verdict describing the overall outcome.
    /// E.g. "Matched 3 of 5 tokens (2 missed)."
    /// </summary>
    public string Verdict
    {
        get
        {
            EnsureBuilt();
            return _verdict!;
        }
    }

    /// <summary>
    /// All events recorded during this tokenization call, in the order they occurred.
    /// This is the raw event trace for power users and engine debugging.
    /// For most use cases, prefer <see cref="Tokens"/> instead.
    /// </summary>
    public IReadOnlyList<DiagnosticEvent> RawEvents => _events;

    internal void AddEvent(DiagnosticEvent evt) => _events.Add(evt);

    /// <summary>
    /// Renders an alignment view showing how the template tokens mapped onto the input text.
    /// The result is cached after the first call.
    /// </summary>
    public string RenderAlignment()
    {
        _alignment ??= AlignmentRenderer.Render(this, _inputContent);
        return _alignment;
    }

    /// <summary>
    /// Renders a chronological walk-through of every engine decision during tokenization.
    /// The result is cached after the first call.
    /// </summary>
    public string RenderProcessingOrder()
    {
        _processingOrder ??= ProcessingOrderRenderer.Render(this);
        return _processingOrder;
    }

    private void EnsureBuilt()
    {
        if (_tokens != null)
            return;

        var (tokens, verdict) = TokenDiagnosticBuilder.Build(this);
        _tokens = tokens;
        _verdict = verdict;
    }
}
