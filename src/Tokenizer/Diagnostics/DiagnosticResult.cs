namespace Tokens.Diagnostics;

/// <summary>
/// Contains all diagnostic information from a single tokenization call.
/// The primary API is <see cref="Tokens"/> which provides per-token narratives.
/// <see cref="RawEvents"/> retains the full event trace for power users.
/// </summary>
/// <remarks>
/// Thread safety: this type is not thread-safe. Designed for single-threaded
/// access after tokenization completes.
/// </remarks>
public sealed class DiagnosticResult
{
    private readonly List<DiagnosticEvent> _events;
    private readonly string? _inputContent;
    private IReadOnlyList<TokenDiagnostic>? _tokens;
    private string? _verdict;
    private string? _alignment;
    private string? _processingOrder;
    private int _matchedCount;
    private int _missedCount;
    private int _totalCount;

    internal Dictionary<string, List<DiagnosticEvent>>? RejectionsPerToken { get; set; }
    internal Dictionary<string, List<DiagnosticEvent>>? DecoratorSuccessesPerToken { get; set; }
    internal string[]? CachedInputLines { get; set; }

    internal DiagnosticResult(string? inputContent, bool outOfOrderTokens = false, HashSet<string>? optionalTokenNames = null)
    {
        _inputContent = inputContent;
        _events = new List<DiagnosticEvent>();
        OutOfOrderTokens = outOfOrderTokens;
        OptionalTokenNames = optionalTokenNames ?? new HashSet<string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// The input text that was tokenized. Used by hint generators for near-miss analysis.
    /// </summary>
    internal string? InputContent => _inputContent;

    /// <summary>
    /// Whether the template uses out-of-order token matching.
    /// </summary>
    internal bool OutOfOrderTokens { get; }

    /// <summary>
    /// Token names that are optional (won't block subsequent tokens in ordered mode).
    /// </summary>
    internal HashSet<string> OptionalTokenNames { get; }

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
    /// Number of tokens that were successfully matched.
    /// </summary>
    public int MatchedCount { get { EnsureBuilt(); return _matchedCount; } }

    /// <summary>
    /// Number of tokens that were missed (not matched).
    /// </summary>
    public int MissedCount { get { EnsureBuilt(); return _missedCount; } }

    /// <summary>
    /// Total number of tokens in the template.
    /// </summary>
    public int TotalCount { get { EnsureBuilt(); return _totalCount; } }

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

        var (tokens, verdict, matchedCount, missedCount, totalCount) = TokenDiagnosticBuilder.Build(this);
        _tokens = tokens;
        _verdict = verdict;
        _matchedCount = matchedCount;
        _missedCount = missedCount;
        _totalCount = totalCount;
    }
}
