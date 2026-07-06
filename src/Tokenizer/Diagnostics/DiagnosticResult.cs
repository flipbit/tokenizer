namespace Tokens.Diagnostics;

/// <summary>
/// Contains all diagnostic events collected during a single tokenization call,
/// together with derived views and rendered output for debugging.
/// </summary>
public class DiagnosticResult
{
    private static readonly DiagnosticEventType[] FailureTypes =
    {
        DiagnosticEventType.ValidatorFailed,
        DiagnosticEventType.TransformerFailed,
        DiagnosticEventType.TokenAssignmentFailed,
        DiagnosticEventType.TokenMissed,
        DiagnosticEventType.HintMissing,
        DiagnosticEventType.BacktrackStarted,
        DiagnosticEventType.RepeatingTokenDisabled,
        DiagnosticEventType.SingleUseTokenRemoved,
    };

    private readonly string? _inputContent;
    private DiagnosticSummary? _summary;
    private string? _alignment;

    internal DiagnosticResult(string? inputContent)
    {
        _inputContent = inputContent;
        Events = new List<DiagnosticEvent>();
    }

    /// <summary>
    /// The input text that was tokenized. Used by hint generators for near-miss analysis.
    /// </summary>
    internal string? InputContent => _inputContent;

    /// <summary>
    /// All events recorded during this tokenization call, in the order they occurred.
    /// </summary>
    public List<DiagnosticEvent> Events { get; }

    /// <summary>
    /// A high-level summary computed lazily from the collected events.
    /// </summary>
    public DiagnosticSummary Summary
    {
        get
        {
            _summary ??= DiagnosticSummaryBuilder.Build(this);
            return _summary;
        }
    }

    /// <summary>
    /// All events whose type indicates a failure or missed match.
    /// </summary>
    public IEnumerable<DiagnosticEvent> Failures =>
        Events.Where(e => FailureTypes.Contains(e.Type));

    /// <summary>
    /// All events associated with the named token.
    /// </summary>
    /// <param name="name">The token name to filter by.</param>
    public IEnumerable<DiagnosticEvent> ForToken(string name) =>
        Events.Where(e => e.TokenName == name);

    /// <summary>
    /// The first failure event in the event list, or null if there are none.
    /// </summary>
    public DiagnosticEvent? FirstFailure =>
        Events.Find(e => FailureTypes.Contains(e.Type));

    /// <summary>
    /// Renders an alignment view showing how the template tokens mapped onto the input text.
    /// The result is cached after the first call.
    /// </summary>
    public string RenderAlignment()
    {
        _alignment ??= AlignmentRenderer.Render(this, _inputContent);
        return _alignment;
    }
}
