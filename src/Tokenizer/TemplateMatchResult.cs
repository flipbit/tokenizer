namespace Tokens;

/// <summary>
/// Contains the result of running a tokenization against multiple registered
/// templates with the <see cref="TemplateMatcher"/>.
/// </summary>
public sealed class TemplateMatchResult
{
    private readonly List<TokenizeResult> _results;

    /// <summary>
    /// Initializes a new, empty <see cref="TemplateMatchResult"/>.
    /// </summary>
    public TemplateMatchResult()
    {
        _results = new List<TokenizeResult>();
    }

    /// <summary>
    /// Contains the result of processing each template against the input text.
    /// </summary>
    public IReadOnlyList<TokenizeResult> Results => _results;

    /// <summary>
    /// Returns the best matching result.
    /// </summary>
    public TokenizeResult? BestMatch { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether any template produced a successful match.
    /// </summary>
    public bool Success => BestMatch != null;

    internal void AddResult(TokenizeResult result)
    {
        _results.Add(result);
    }

    internal TokenizeResult? GetBestMatch() => _results
        .Where(r => r.Success)
        .OrderByDescending(r => r.Hints.Matches.Count)
        .ThenByDescending(r => r.Tokens.Matches.Count)
        .ThenBy(r => r.Template.Tokens.Count)
        .ThenBy(r => r.Template.Id)
        .FirstOrDefault();
}
